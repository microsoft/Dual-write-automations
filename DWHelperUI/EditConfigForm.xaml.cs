// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using CommandLine;
using DWLibary;
using Microsoft.Identity.Client;
using Microsoft.VisualStudio.Services.CircuitBreaker;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace DWHelperUI
{
    /// <summary>
    /// Interaction logic for EditConfigForm.xaml
    /// </summary>
    public partial class EditConfigForm
    {
        public string configName { get; set; }
        ObservableCollection<MapConfig> mapConfigsContent;

        public EditConfigForm(string configName)
        {
            InitializeComponent();
            this.configName = configName;

            initConfig();
            getAppSettingsGridData();
            getSolutionsGridData();
            getGroupsGridData();
            getADOGridData();
            getMapsGridData();
        }

        private void initConfig()
        {
            GlobalVar.configFileName = configName;
            GlobalVar.initConfig();
        }

        private void getADOGridData()
        {
            List<ADOWikiParameter> gridContent = GlobalVar.dwSettings.ADOWikiParameters.Cast<ADOWikiParameter>().ToList();
            
            setVisibility<ADOWikiParameter>(adoSettings);

            adoSettings.ItemsSource = gridContent;

        }

        private void getAppSettingsGridData()
        {
            List<KeyValueConfigurationElement> gridContent = GlobalVar.config.AppSettings.Settings.Cast<KeyValueConfigurationElement>().ToList();

            setVisibility<KeyValueConfigurationElement>(appSettings);




            appSettings.ItemsSource = gridContent;

        }


        private void getSolutionsGridData()
        {
            List<Solution> gridContent = GlobalVar.dwSettings.Solutions.Cast<Solution>().ToList();

            setVisibility<Solution>(solutions);

            solutions.ItemsSource = gridContent;

        }

        private void getGroupsGridData()
        {
            List<Group> gridContent = GlobalVar.dwSettings.Groups.Cast<Group>().ToList();

            setVisibility<Group>(groups);
            
            groups.ItemsSource = gridContent;

        }

        private void setVisibility<T>(DataGrid _grid)
        {
            _grid.AutoGenerateColumns = false;
            var properties = typeof(T).GetProperties();

            foreach (var prop in properties)
            {
                dynamic column = new DataGridTextColumn();



                if (prop.Name == "executionMode")
                {
                    column = new DataGridTextColumn();
                }



                column.Binding = new Binding(prop.Name);
                column.Header = prop.Name;

                if (typeof(T).IsAssignableFrom(prop.DeclaringType) && prop.IsDefined(typeof(ConfigurationPropertyAttribute), false))
                {
                    column.Visibility = Visibility.Visible;
                }
                else
                {
                    column.Visibility = Visibility.Hidden;
                }
                _grid.Columns.Add(column);
            }
        }

        private void getMapsGridData()
        {

            mapConfigsContent = new ObservableCollection<MapConfig>(GlobalVar.dwSettings.MapConfigs.Cast<MapConfig>().ToList());
            setVisibility<MapConfig>(mapConfig);
            mapConfig.ItemsSource = mapConfigsContent;
           
        }

        private void saveSettings()
        {
            clearFilter();

            GlobalVar.dwSettings.ADOWikiParameters.Clear();

            foreach (var item in adoSettings.Items)
            {
                if (item is ADOWikiParameter)
                    GlobalVar.dwSettings.ADOWikiParameters.Add((ADOWikiParameter)item);
            }


            GlobalVar.dwSettings.MapConfigs.Clear();
            foreach (var item in mapConfig.Items)
            {
                if (item is MapConfig)
                    GlobalVar.dwSettings.MapConfigs.Add((MapConfig)item);
            }

            GlobalVar.dwSettings.Groups.Clear();
            foreach (var item in groups.Items)
            {
                if (item is Group)
                    GlobalVar.dwSettings.Groups.Add((Group)item);
            }


            GlobalVar.dwSettings.Solutions.Clear();
            foreach (var item in solutions.Items)
            {
                if (item is Solution)
                    GlobalVar.dwSettings.Solutions.Add((Solution)item);
            }


            GlobalVar.config.AppSettings.Settings.Clear();
            foreach (var item in appSettings.Items)
            {
                if (item is KeyValueConfigurationElement)
                    GlobalVar.config.AppSettings.Settings.Add((KeyValueConfigurationElement)item);
            }

            GlobalVar.config.Save(ConfigurationSaveMode.Modified);
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            saveSettings();

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Do you want to save your settings before closing?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
            switch (result)
            {
                case MessageBoxResult.Yes:
                    // Save settings and continue closing the window
                    saveSettings();
                    break;
                case MessageBoxResult.No:
                    // Do not save settings and continue closing the window
                    break;
            }
        }

        private void filterGrid()
        {
            var view = CollectionViewSource.GetDefaultView(mapConfig.ItemsSource);
            view.Filter = o =>
            {
                var item = o as MapConfig;
                var properties = item.GetType().GetProperties();
                foreach (var property in properties)
                {
                    var value = property.GetValue(item, null);
                    if (value != null && value.ToString().ToLower().Contains(SearchString.Text.ToLower()))
                    {
                        return true;
                    }
                }
                return false;
            };
        }
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                this.WindowState = WindowState.Normal;
            }
        }

        private void Filter_Click(object sender, RoutedEventArgs e)
        {
            filterGrid();
        }

        private void ClearFilter_Click(object sender, RoutedEventArgs e)
        {
            clearFilter();
        }

        private void clearFilter()
        {
            SearchString.Clear();
            filterGrid();
        }

        private void moveUp_Click(object sender, RoutedEventArgs e)
        {
           
            int selectedIndex = mapConfig.SelectedIndex;
            if (selectedIndex > 0)
            {
                int newIndex = selectedIndex - 1;

                MapConfig selectedItem = mapConfig.SelectedItem as MapConfig;
                if (selectedItem != null)
                {
                    mapConfigsContent.RemoveAt(selectedIndex);
                    mapConfigsContent.Insert(newIndex, selectedItem);
                    mapConfig.SelectedItem = mapConfig.Items[newIndex];
                    mapConfig.Focus();
                }
            }

        }

        private void moveDown_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = mapConfig.SelectedIndex;
            if (selectedIndex > 0)
            {
                
                int newIndex = selectedIndex + 1;
                MapConfig selectedItem = mapConfig.SelectedItem as MapConfig;

                if (newIndex < mapConfig.Items.Count && selectedItem != null)
                {
                    mapConfigsContent.RemoveAt(selectedIndex);
                    mapConfigsContent.Insert(newIndex, selectedItem);
                    mapConfig.SelectedItem = mapConfig.Items[newIndex];
                    mapConfig.Focus();
                }
            }
        }
    }



}
