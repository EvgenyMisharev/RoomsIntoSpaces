/*
 * Copyright (c) <2023> <Misharev Evgeny>
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
 * 1. Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer 
 *    in the documentation and/or other materials provided with the distribution.
 * 3. Neither the name of the <organization> nor the names of its contributors may be used to endorse or promote products derived 
 *    from this software without specific prior written permission.
 * 4. Redistributions are not allowed to be sold, in whole or in part, for any compensation of any kind.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, 
 * BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. 
 * IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, 
 * OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; 
 * OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT 
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *
 * Contact: <citrusbim@gmail.com> or <https://web.telegram.org/k/#@MisharevEvgeny>
 */
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoomsIntoSpaces
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    class RoomsIntoSpacesCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;
            Selection sel = commandData.Application.ActiveUIDocument.Selection;
            Document linkDoc = null;

            List<RevitLinkInstance> revitLinkInstanceList = new FilteredElementCollector(doc)
                .OfClass(typeof(RevitLinkInstance))
                .Cast<RevitLinkInstance>()
                .ToList();
            if (revitLinkInstanceList.Count == 0)
            {
                TaskDialog.Show("Ravit", "В проекте отсутствуют связанные файлы!");
                return Result.Cancelled;
            }

            List<Space> spaceList = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_MEPSpaces)
                .WhereElementIsNotElementType()
                .Cast<Space>()
                .Where(s => s.Area > 0)
                .ToList();

            RoomsIntoSpacesWPF roomsIntoSpacesWPF = new RoomsIntoSpacesWPF(revitLinkInstanceList, spaceList);
            roomsIntoSpacesWPF.ShowDialog();
            if (roomsIntoSpacesWPF.DialogResult != true)
            {
                return Result.Cancelled;
            }

            if (roomsIntoSpacesWPF.SelectedRevitLinkInstance == null)
            {
                TaskDialog.Show("Ravit", "Связанный файл не выбран!");
                return Result.Cancelled;
            }
            linkDoc = roomsIntoSpacesWPF.SelectedRevitLinkInstance.GetLinkDocument();
            Transform transform = roomsIntoSpacesWPF.SelectedRevitLinkInstance.GetTotalTransform();

            List<Level> docLvlList = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Levels)
                .WhereElementIsNotElementType()
                .Cast<Level>()
                .ToList();

            List<Room> roomListFromLink = new FilteredElementCollector(linkDoc)
                .WhereElementIsNotElementType()
                .OfClass(typeof(SpatialElement))
                .Where(e => e.GetType() == typeof(Room))
                .Cast<Room>()
                .Where(r => r.Area > 0)
                .OrderBy(r => (r.Location as LocationPoint).Point.Z)
                .ThenBy(r => r.Number, new AlphanumComparatorFastString())
                .ToList();

            using (Transaction t = new Transaction(doc))
            {
                t.Start("Помещения в пространства");
                foreach (Room room in roomListFromLink)
                {
                    // Получаем точку расположения помещения
                    XYZ location = (room.Location as LocationPoint).Point;

                    // Проверяем, есть ли уже пространство на этой точке
                    Space existingSpace = doc.GetSpaceAtPoint(location);
                    if(existingSpace != null)
                    {
                        ElementId existingSpaceId = existingSpace.Id;
                        if (existingSpaceId != ElementId.InvalidElementId)
                        {
                            existingSpace = doc.GetElement(existingSpaceId) as Space;

                            // Проверяем, совпадает ли геометрия помещения и пространства
                            GeometryElement roomGeometry = room.get_Geometry(new Options());
                            GeometryElement spaceGeometry = existingSpace.get_Geometry(new Options());

                            foreach (GeometryObject roomObject in roomGeometry)
                            {
                                Solid roomSolid = roomObject as Solid;
                                if (roomSolid != null)
                                {
                                    foreach (GeometryObject spaceObject in spaceGeometry)
                                    {
                                        Solid spaceSolid = spaceObject as Solid;
                                        if (spaceSolid != null && BooleanOperationsUtils.ExecuteBooleanOperation(roomSolid, spaceSolid, BooleanOperationsType.Intersect).Volume != 0)
                                        {
                                            // Если геометрия пересекается, то обновляем номер и имя пространства
                                            string spaceNumber = existingSpace.Number;
                                            string spaceName = existingSpace.Name;
                                            // Обновляем номер и имя пространства, если они отличаются от номера и имени помещения
                                            if (spaceNumber != room.get_Parameter(BuiltInParameter.ROOM_NUMBER).AsString() ||
                                                spaceName != room.get_Parameter(BuiltInParameter.ROOM_NAME).AsString())
                                            {
                                                existingSpace.get_Parameter(BuiltInParameter.ROOM_NUMBER).Set(room.get_Parameter(BuiltInParameter.ROOM_NUMBER).AsString());
                                                existingSpace.get_Parameter(BuiltInParameter.ROOM_NAME).Set(room.get_Parameter(BuiltInParameter.ROOM_NAME).AsString());
                                            }
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            //Ничего не делаем
                        }
                    }
                    else
                    {
                        UV roomUVLocation = new UV(location.X, location.Y);
                        Level closestRoomLevel = GetClosestRoomLevel(docLvlList, linkDoc, room);
                        // Создаем новое пространство на месте помещения
                        Space space = doc.Create.NewSpace(closestRoomLevel, roomUVLocation);
                        space.get_Parameter(BuiltInParameter.ROOM_NUMBER).Set(room.get_Parameter(BuiltInParameter.ROOM_NUMBER).AsString());
                        space.get_Parameter(BuiltInParameter.ROOM_NAME).Set(room.get_Parameter(BuiltInParameter.ROOM_NAME).AsString());
                    }
                 }    
                t.Commit();
            }
            return Result.Succeeded;
        }
        private static Level GetClosestRoomLevel(List<Level> docLvlList, Document linkDoc, Room room)
        {
            Level lvl = null;
            double linkFloorLevelElevation = (linkDoc.GetElement(room.LevelId) as Level).Elevation;
            double heightDifference = 10000000000;
            foreach (Level docLvl in docLvlList)
            {
                double tmpHeightDifference = Math.Abs(Math.Round(linkFloorLevelElevation, 6) - Math.Round(docLvl.Elevation, 6));
                if (tmpHeightDifference < heightDifference)
                {
                    heightDifference = tmpHeightDifference;
                    lvl = docLvl;
                }
            }
            return lvl;
        }
    }
}
