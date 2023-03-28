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
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace RoomsIntoSpaces
{
    public partial class RoomsIntoSpacesWPF : Window
    {
        public RevitLinkInstance SelectedRevitLinkInstance;
        ObservableCollection<MatchingParametersItem> SpaceTextParametersCol;
        ObservableCollection<MatchingParametersItem> SpaceDoubleParametersCol;
        ObservableCollection<MatchingParametersItem> SpaceIntParametersCol;

        List<Parameter> RoomTextParametersList;
        List<Parameter> RoomDoubleParametersList;
        List<Parameter> RoomIntParametersList;

        List<MatchingParametersSerializedItem> MatchingParametersItemSerialized;

        public RoomsIntoSpacesWPF(List<RevitLinkInstance> revitLinkInstanceList, List<Space> spaceList)
        {
            RoomTextParametersList = new List<Parameter>();
            RoomDoubleParametersList = new List<Parameter>();
            RoomIntParametersList = new List<Parameter>();
            InitializeComponent();

            listBox_RevitLinkInstance.ItemsSource = revitLinkInstanceList;
            listBox_RevitLinkInstance.DisplayMemberPath = "Name";
            listBox_RevitLinkInstance.SelectedItem = listBox_RevitLinkInstance.Items[0];

            if (spaceList.Count != 0)
            {
                GetSpaceTextParameters(spaceList);
                GetSpaceDoubleParameters(spaceList);
                GetSpaceIntParameters(spaceList);
            }
        }

        private void GetSpaceTextParameters(List<Space> spaceList)
        {
            List<Parameter> spaceParametersList = spaceList.First().Parameters.Cast<Parameter>().ToList();
            List<Parameter> spaceTextParametersList = spaceParametersList
                .Where(p => p.StorageType == StorageType.String)
                .OrderBy(p => p.Definition.Name, new AlphanumComparatorFastString())
                .ToList();
            SpaceTextParametersCol = new ObservableCollection<MatchingParametersItem>();

            foreach (Parameter spaceParameter in spaceTextParametersList)
            {
                MatchingParametersItem item = new MatchingParametersItem()
                {
                    SpaceParameter = spaceParameter,
                    RoomParameter = null
                };
                SpaceTextParametersCol.Add(item);
            }
            dataGrid_TextParams.ItemsSource = SpaceTextParametersCol;
        }
        private void GetSpaceDoubleParameters(List<Space> spaceList)
        {
            List<Parameter> spaceParametersList = spaceList.First().Parameters.Cast<Parameter>().ToList();
            List<Parameter> spaceDoubleParametersList = spaceParametersList
                .Where(p => p.StorageType == StorageType.Double)
                .OrderBy(p => p.Definition.Name, new AlphanumComparatorFastString())
                .ToList();
            SpaceDoubleParametersCol = new ObservableCollection<MatchingParametersItem>();

            foreach (Parameter spaceParameter in spaceDoubleParametersList)
            {
                MatchingParametersItem item = new MatchingParametersItem()
                {
                    SpaceParameter = spaceParameter,
                    RoomParameter = null
                };
                SpaceDoubleParametersCol.Add(item);
            }
            dataGrid_DoubleParams.ItemsSource = SpaceDoubleParametersCol;
        }
        private void GetSpaceIntParameters(List<Space> spaceList)
        {
            List<Parameter> spaceParametersList = spaceList.First().Parameters.Cast<Parameter>().ToList();
            List<Parameter> spaceIntParametersList = spaceParametersList
                .Where(p => p.StorageType == StorageType.Integer)
                .OrderBy(p => p.Definition.Name, new AlphanumComparatorFastString())
                .ToList();
            SpaceIntParametersCol = new ObservableCollection<MatchingParametersItem>();

            foreach (Parameter spaceParameter in spaceIntParametersList)
            {
                MatchingParametersItem item = new MatchingParametersItem()
                {
                    SpaceParameter = spaceParameter,
                    RoomParameter = null
                };
                SpaceIntParametersCol.Add(item);
            }
            dataGrid_IntParams.ItemsSource = SpaceIntParametersCol;
        }

        private void btn_Ok_Click(object sender, RoutedEventArgs e)
        {
            SelectedRevitLinkInstance = listBox_RevitLinkInstance.SelectedItem as RevitLinkInstance;
            this.DialogResult = true;
            this.Close();
        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Space)
            {
                SelectedRevitLinkInstance = listBox_RevitLinkInstance.SelectedItem as RevitLinkInstance;
                this.DialogResult = true;
                this.Close();
            }

            else if (e.Key == Key.Escape)
            {
                this.DialogResult = false;
                this.Close();
            }
        }
        private void btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void listBox_RevitLinkInstance_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            RevitLinkInstance selectedRevitLinkInstance = listBox_RevitLinkInstance.SelectedItem as RevitLinkInstance;
            Document linkDoc = selectedRevitLinkInstance.GetLinkDocument();

            List<Room> roomListFromLink = new FilteredElementCollector(linkDoc)
                .WhereElementIsNotElementType()
                .OfClass(typeof(SpatialElement))
                .Where(r => r.GetType() == typeof(Room))
                .Cast<Room>()
                .ToList();
            if (roomListFromLink.Count != 0)
            {
                List<Parameter> roomParametersList = roomListFromLink.First().Parameters.Cast<Parameter>().ToList();
                RoomTextParametersList = roomParametersList
                    .Where(p => p.StorageType == StorageType.String)
                    .OrderBy(p => p.Definition.Name, new AlphanumComparatorFastString())
                    .ToList();
                dataGridComboBoxRoomTextParams.ItemsSource = RoomTextParametersList;
                dataGridComboBoxRoomTextParams.DisplayMemberPath = "Definition.Name";

                RoomDoubleParametersList = roomParametersList
                    .Where(p => p.StorageType == StorageType.Double)
                    .OrderBy(p => p.Definition.Name, new AlphanumComparatorFastString())
                    .ToList();
                dataGridComboBoxRoomDoubleParams.ItemsSource = RoomDoubleParametersList;
                dataGridComboBoxRoomDoubleParams.DisplayMemberPath = "Definition.Name";

                RoomIntParametersList = roomParametersList
                    .Where(p => p.StorageType == StorageType.Integer)
                    .OrderBy(p => p.Definition.Name, new AlphanumComparatorFastString())
                    .ToList();
                dataGridComboBoxRoomIntParams.ItemsSource = RoomIntParametersList;
                dataGridComboBoxRoomIntParams.DisplayMemberPath = "Definition.Name";
            }
        }

        //Сохранить
        private void btn_Save_Click(object sender, RoutedEventArgs e)
        {
            MatchingParametersItemSerialized = new List<MatchingParametersSerializedItem>();
            MatchingParametersItemSerialized.AddRange(GetMatchingParametersNotNull(SpaceTextParametersCol));
            MatchingParametersItemSerialized.AddRange(GetMatchingParametersNotNull(SpaceDoubleParametersCol));
            MatchingParametersItemSerialized.AddRange(GetMatchingParametersNotNull(SpaceIntParametersCol));

            var saveDialog = new System.Windows.Forms.SaveFileDialog();
            saveDialog.Filter = "json files (*.json)|*.json";
            System.Windows.Forms.DialogResult result = saveDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                string jsonFilePath = saveDialog.FileName;
                using (FileStream fs = new FileStream(jsonFilePath, FileMode.Create))
                {
                    fs.Close();
                }
                JsonSerializer serializer = new JsonSerializer();
                using (StreamWriter sw = new StreamWriter(jsonFilePath))
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    serializer.Formatting = Formatting.Indented;
                    serializer.Serialize(writer, MatchingParametersItemSerialized);
                }
            }
        }

        private void btn_Open_Click(object sender, RoutedEventArgs e)
        {
            var openDialog = new System.Windows.Forms.OpenFileDialog();
            openDialog.Filter = "json files (*.json)|*.json";
            System.Windows.Forms.DialogResult result = openDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                string jsonFilePath = openDialog.FileName;
                if (File.Exists(jsonFilePath))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    using (StreamReader sr = new StreamReader(jsonFilePath))
                    using (JsonTextReader reader = new JsonTextReader(sr))
                    {
                        List<MatchingParametersSerializedItem> tmp = (List<MatchingParametersSerializedItem>)serializer
                            .Deserialize(reader, typeof(List<MatchingParametersSerializedItem>));
                        if (tmp != null && tmp.Count != 0)
                        {
                            foreach (MatchingParametersSerializedItem itm in tmp)
                            {
                                MatchingParametersItem MatchingTextParametersItem = SpaceTextParametersCol.FirstOrDefault(p => p.SpaceParameter.Definition.Name == itm.SpaceParameterName
                                    && (int)p.SpaceParameter.Definition.ParameterGroup == itm.SpaceParameterParameterGroup
                                    && (int)p.SpaceParameter.Definition.ParameterType == itm.SpaceParameterParameterType);
                                if(MatchingTextParametersItem != null)
                                {
                                    Parameter roomParameter = RoomTextParametersList.FirstOrDefault(p => p.Definition.Name == itm.RoomParameterName
                                        && (int)p.Definition.ParameterGroup == itm.RoomParameterParameterGroup
                                        && (int)p.Definition.ParameterType == itm.RoomParameterParameterType);
                                    if(roomParameter != null)
                                    {
                                        MatchingTextParametersItem.RoomParameter = roomParameter;
                                    }
                                }

                                MatchingParametersItem MatchingDoubleParametersItem = SpaceDoubleParametersCol.FirstOrDefault(p => p.SpaceParameter.Definition.Name == itm.SpaceParameterName
                                    && (int)p.SpaceParameter.Definition.ParameterGroup == itm.SpaceParameterParameterGroup
                                    && (int)p.SpaceParameter.Definition.ParameterType == itm.SpaceParameterParameterType);
                                if (MatchingDoubleParametersItem != null)
                                {
                                    Parameter roomParameter = RoomDoubleParametersList.FirstOrDefault(p => p.Definition.Name == itm.RoomParameterName
                                        && (int)p.Definition.ParameterGroup == itm.RoomParameterParameterGroup
                                        && (int)p.Definition.ParameterType == itm.RoomParameterParameterType);
                                    if (roomParameter != null)
                                    {
                                        MatchingDoubleParametersItem.RoomParameter = roomParameter;
                                    }
                                }

                                MatchingParametersItem MatchingIntParametersItem = SpaceIntParametersCol.FirstOrDefault(p => p.SpaceParameter.Definition.Name == itm.SpaceParameterName
                                    && (int)p.SpaceParameter.Definition.ParameterGroup == itm.SpaceParameterParameterGroup
                                    && (int)p.SpaceParameter.Definition.ParameterType == itm.SpaceParameterParameterType);
                                if (MatchingIntParametersItem != null)
                                {
                                    Parameter roomParameter = RoomIntParametersList.FirstOrDefault(p => p.Definition.Name == itm.RoomParameterName
                                        && (int)p.Definition.ParameterGroup == itm.RoomParameterParameterGroup
                                        && (int)p.Definition.ParameterType == itm.RoomParameterParameterType);
                                    if (roomParameter != null)
                                    {
                                        MatchingIntParametersItem.RoomParameter = roomParameter;
                                    }
                                }

                            }
                            dataGrid_TextParams.ItemsSource = null;
                            dataGrid_TextParams.ItemsSource = SpaceTextParametersCol;

                            dataGrid_DoubleParams.ItemsSource = null;
                            dataGrid_DoubleParams.ItemsSource = SpaceDoubleParametersCol;

                            dataGrid_IntParams.ItemsSource = null;
                            dataGrid_IntParams.ItemsSource = SpaceIntParametersCol;
                        }
                    }
                }
            }
        }

        private List<MatchingParametersSerializedItem> GetMatchingParametersNotNull(ObservableCollection<MatchingParametersItem> collection)
        {
            List<MatchingParametersSerializedItem> serializedCollection = new List<MatchingParametersSerializedItem>();

            foreach (var item in collection)
            {
                if (item.RoomParameter != null)
                {
                    serializedCollection.Add(new MatchingParametersSerializedItem
                    {
                        SpaceParameterName = item.SpaceParameter.Definition.Name,
                        SpaceParameterParameterGroup = (int)item.SpaceParameter.Definition.ParameterGroup,
                        SpaceParameterParameterType = (int)item.SpaceParameter.Definition.ParameterType,

                        RoomParameterName = item.RoomParameter.Definition.Name,
                        RoomParameterParameterGroup = (int)item.RoomParameter.Definition.ParameterGroup,
                        RoomParameterParameterType = (int)item.RoomParameter.Definition.ParameterType
                    }); 
                }
            }

            return serializedCollection;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            double widthTextParams = dataGridTextColumnTextParams.Width.DesiredValue;
            double widthDoubleParams = dataGridTextColumnDoubleParams.Width.DesiredValue;
            double widthIntParams = dataGridTextColumnIntParams.Width.DesiredValue;

            List<double> maxWidthList = new List<double> { widthTextParams, widthDoubleParams, widthIntParams };
            double maxWidth = maxWidthList.Max();

            dataGridTextColumnTextParams.Width = maxWidth;
            dataGridTextColumnDoubleParams.Width = maxWidth;
            dataGridTextColumnIntParams.Width = maxWidth;
        }
    }
}
