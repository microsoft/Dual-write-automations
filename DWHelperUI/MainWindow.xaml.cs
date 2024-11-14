// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using CommandLine;
using DWLibary;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Wpf.Ui.Appearance;
using static DWLibary.DWEnums;

namespace DWHelperUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    

    public partial class MainWindow
    {
        private Process process;/// 
        ConcurrentQueue<string> outputQueue;//= new ConcurrentQueue<string>();

        public MainWindow()
        {
            InitializeComponent();
            checkUpgrade();
            checkCreateEncryption();
            initConfigFiles();
            initEnums();
            initFormSettings();
            outputQueue = new ConcurrentQueue<string>();
        }

        private void checkUpgrade()
        {
            if (Properties.Settings.Default.upgradeRequired)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.upgradeRequired = false;
                Properties.Settings.Default.Save();
            }
        }

        //Used to decrypt the password properly when the password is bound to the passwordbox
        private void checkCreateEncryption()
        {

            if(string.IsNullOrWhiteSpace(Properties.Settings.Default.EncryptionKey) 
                ||  string.IsNullOrWhiteSpace(Properties.Settings.Default.EncryptionIv))
            {

                List<string> keyIv = EncryptionKeyGenerator.GenerateAndStoreKeys();

                Properties.Settings.Default.EncryptionKey = keyIv[0];
                Properties.Settings.Default.EncryptionIv = keyIv[1];
                Properties.Settings.Default.Save();


            }

            EncryptionHelper helper = new EncryptionHelper(Properties.Settings.Default.EncryptionKey, Properties.Settings.Default.EncryptionIv);

            //Decrypt for use
            if(!string.IsNullOrWhiteSpace(password.Password))
            {
                password.Password = helper.Decrypt(Properties.Settings.Default.password);
            }
        

        }

        private void initFormSettings()
        {
            StopProcess.IsEnabled = false;

        }

        private void showHideSettings(bool hide)
        {
            Visibility settingsVisibility;
            Visibility outputVisibility;

            if (hide)
            {
                settingsVisibility = Visibility.Collapsed;
            }
            else
            {
                settingsVisibility = Visibility.Visible;

            }
            newConfigSection.Visibility = settingsVisibility;
            adowikiuploadpanel.Visibility = settingsVisibility;
            applySolutionPanel.Visibility = settingsVisibility;
            runSettings.Visibility = settingsVisibility;
            runSettingsDetail.Visibility = settingsVisibility;
            configSettings.Visibility = settingsVisibility;
            actions.Visibility = settingsVisibility;
            logSettings.Visibility = settingsVisibility;

            if (hide)
            {
                outputLog.Height = 400;
            }
            else
            {
                outputLog.Height = 150;
            }


        }

        private void StartProcess_Click(object sender, RoutedEventArgs e)
        {

           

            if (!isValidStart())
            {
               // outputLog.AppendText("")
                return;
            }
            showHideSettings(true);
            outputQueue = new ConcurrentQueue<string>();

            process = new Process();
            process.StartInfo.FileName = "DWHelperCMD.exe";
            process.StartInfo.Arguments = string.Join(" ", getArgsList());
            process.EnableRaisingEvents= true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;

            //process.OutputDataReceived += new DataReceivedEventHandler(OutputHandler);
            //process.OutputDataReceived += (sendingProcess, outLine) =>
            //{
            //    if (!String.IsNullOrEmpty(outLine.Data))
            //    {
            //        outputQueue.Enqueue(outLine.Data);
            //    }
            //};
            process.OutputDataReceived += (sendingProcess, outLine) =>
            {
                if (!String.IsNullOrEmpty(outLine.Data))

                {
                    var data = outLine.Data.ToString();
                    data = data.Replace("info: DWHelper.DWHostedService[0]", "");
                    data = data.Replace("info: Microsoft.Hosting.Lifetime[0]", "");
                    data = data.Replace("fail: DWHelper.DWHostedService[0]", "ERROR");


                    outputQueue.Enqueue(data);
                }
            };

            process.Exited += Process_Exited;
      
            
            process.Start();
            process.BeginOutputReadLine();

            runThreadProcessLog();


            StartProcess.IsEnabled = false;
            StopProcess.IsEnabled = true;

        }

        private void runThreadProcessLog()
        {
            new Thread(() =>
            {
                Thread.Sleep(1000);
                while (!process.HasExited || !outputQueue.IsEmpty)
                {
                    while (outputQueue.TryDequeue(out string line))
                    {
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            if (line != null && line.Length > 0)
                            {
                                outputLog.AppendText($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}: {line}");
                                outputLog.ScrollToEnd();
                            }
                        });
                        Console.WriteLine(line);
                        // Do something with the output, e.g. store it in a variable or list
                    }
                    Thread.Sleep(500);
                }

                string smth = "Thread ended";

            }).Start();
        }


        private void Process_Exited(object? sender, EventArgs e)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                StartProcess.IsEnabled = true;
                StopProcess.IsEnabled = false;
                initConfigFiles();
                showHideSettings(false);
            });

            
           // throw new NotImplementedException();
        }

        private bool isValidStart()
        {
            bool ret = true;

            validateUri();

            if (envURL.Text == String.Empty)
            {
                outputLog.AppendText(Environment.NewLine + "URL is empty and needs to be specified!");
                outputLog.ScrollToEnd();
                ret = false;
            }

            return ret;
        }

        private List<string> getArgsList()
        {
            List<string> ret = new List<string>();

            ret.Add("-u");
            ret.Add($"\"{username.Text}\"");
            ret.Add("-p");
            ret.Add($"\"{password.Password}\"");

            ret.Add("-e");
            ret.Add($"\"{envURL.Text}\"");

            DWEnums.RunMode localMode = (DWEnums.RunMode)runMode.SelectedValue;

            if (localMode != null && localMode != DWEnums.RunMode.none)
            {
                ret.Add("--runmode");
                ret.Add($"\"{localMode.ToString()}\"");

                if(localMode == DWEnums.RunMode.export)
                {
                    ret.Add("-s");
                    ret.Add($"\"{exportStatus.Text}\"");
                }

            }


            DWEnums.CatchUpSyncOption catchUpLocal = (CatchUpSyncOption)Enum.Parse(typeof(CatchUpSyncOption),catchUpSetting.SelectedValue.ToString());

            if (catchUpLocal != null && catchUpLocal != DWEnums.CatchUpSyncOption.Default)
            {
                ret.Add("--catchupsetting");
                ret.Add($"\"{catchUpLocal.ToString()}\"");
            }

            if (localMode == DWEnums.RunMode.compare)
            {
                ret.Add("-t");
                ret.Add($"\"{targetFO.Text}\"");
            }

            if (localMode == DWEnums.RunMode.resetLink)
            {
                ret.Add("--forceReset");
            }

            DWEnums.ExportOptions localExportOption = (DWEnums.ExportOptions)exportOption.SelectedValue;
            if (localExportOption != DWEnums.ExportOptions.Default)
            {
                ret.Add("-o");
                ret.Add($"\"{localExportOption}\"");
            }

            if(disablePrivateBrowser.IsChecked == true)
            {
                ret.Add("--notinprivate");
            }
            

            if (applySolutions.IsChecked == false)
            {
                ret.Add("--nosolutions");
            }

            if (adowikiupload.IsChecked == true)
            {
                ret.Add("--useadowikiupload");
            }
            string configName = customConfigFile.SelectedItem == null ? "" : customConfigFile.SelectedItem.ToString();

            if (configName != null && configName != String.Empty)
            {
                //if default dont add to parameters
                if(configName != "DWHelperCMD.dll.config")
                {
                    ret.Add("-c");
                    ret.Add(($"\"{configName}\""));
                }

                if(localMode == DWEnums.RunMode.export && newConfigFileName.Text != String.Empty)
                {
                    ret.Add("-n");
                    ret.Add(($"\"{newConfigFileName.Text}\""));
                }
            }

            if(logLevel.SelectedItem != null)
            {
                ret.Add("-l");
                ret.Add(($"\"{logLevel.SelectedValue}\""));
            }


            return ret; 
        }


        private void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (!String.IsNullOrEmpty(outLine.Data))
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    var data = outLine.Data.ToString(); 
                    data = data.Replace("info: DWHelper.DWHostedService[0]", "");
                    data = data.Replace("info: Microsoft.Hosting.Lifetime[0]", "");
                    data = data.Replace("info: Microsoft.Hosting.Lifetime[0]", "ERROR!");

                    if (data != null && data.Length > 0)
                    {
                        outputLog.AppendText($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}: {data}");
                        outputLog.ScrollToEnd();
                    }
                });
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            //make sure PW is encrpyted 
            EncryptionHelper helper = new EncryptionHelper(Properties.Settings.Default.EncryptionKey, Properties.Settings.Default.EncryptionIv);


            Properties.Settings.Default.password = helper.Encrypt(password.Password);

            Properties.Settings.Default.exportOption = exportOption.SelectedValue.ToString();
            Properties.Settings.Default.runmode = runMode.SelectedValue.ToString();
            Properties.Settings.Default.Save();
            base.OnClosing(e);
        }

        private void validateUri()
        {
            string ret = String.Empty;
           
            try
            {
                if (envURL.Text == String.Empty)
                    return;

                UriBuilder builder = new UriBuilder(envURL.Text);
                if (!builder.Uri.AbsoluteUri.ToUpper().Contains("DYNAMICS.COM"))
                {
                    MessageBox.Show("URL is not valid");
                    envURL.Text = String.Empty;
                    return;
                }

                ret = builder.Uri.Host.Replace("www.", "");

                envURL.Text = ret;
            }
            catch
            {
                MessageBox.Show("URL is not valid");
                envURL.Text = String.Empty;
            }

            return;
        }

        private void addEnvironment_Click(object sender, RoutedEventArgs e)
        {

            validateUri();
            if (envURL.Text == String.Empty)
                return;

            StringCollection collection = new StringCollection();
            collection.AddRange(envList.Items.Cast<string>().ToArray());

            if (!collection.Contains(envURL.Text))
            {
                collection.Add(envURL.Text);

                Properties.Settings.Default.envList = collection;
                envList.Items.Refresh();
            }
        }

        private void envList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if(envList.SelectedItem== null) return;

            string selectedItem = envList.SelectedItem.ToString();

            envURL.Text = selectedItem;

            setHyperlinkURL();
        }

        private void initConfigFiles()
        {

            string[] configFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.config");
            List<string> configList = new List<string>();
            foreach (string configFile in configFiles)
            {
               FileInfo info = new FileInfo(configFile);

                if (info.Name == "DWHelperUI.dll.config")
                    continue;

               configList.Add(info.Name);
            }

            customConfigFile.ItemsSource = configList;
           

        }


        private void removeList_Click(object sender, RoutedEventArgs e)
        {
            validateUri();

            StringCollection collection = new StringCollection();
            collection.AddRange(envList.Items.Cast<string>().ToArray());
            collection.Remove(envURL.Text);
            Properties.Settings.Default.envList = collection;
            envList.Items.Refresh();

        }

        private void envURL_LostFocus(object sender, RoutedEventArgs e)
        {
            validateUri();
            setHyperlinkURL();
        }

        private void StopProcess_Click(object sender, RoutedEventArgs e)
        {
            if(process!= null) 
                process.Kill();

            foreach(Process p in Process.GetProcessesByName("msedgedriver"))
            {
                p.Kill();
            }

            StopProcess.IsEnabled = false;
            StartProcess.IsEnabled= true;

        }
        private void initEnums()
        {

            //Init RunMode
            runMode.Items.Clear();
            runMode.ItemsSource = System.Enum.GetValues(typeof(DWLibary.DWEnums.RunMode));

            runMode.ItemsSource = Enum.GetValues(typeof(DWEnums.RunMode))
            .Cast<DWEnums.RunMode>()
            .Select(rm => new
            {
                Value = rm,
                Description = DWEnums.DescriptionAttr<DWEnums.RunMode>(rm)
            });
            DWEnums.RunMode selected; 
            Enum.TryParse<DWEnums.RunMode>(Properties.Settings.Default.runmode,true, out selected);
            foreach (dynamic item in runMode.Items)
            {
                if (item.Value == selected)
                    runMode.SelectedItem = item;
            }
   


            //Get Export Options
            exportOption.Items.Clear();
            exportOption.ItemsSource = System.Enum.GetValues(typeof(DWLibary.DWEnums.ExportOptions));

            exportOption.ItemsSource = Enum.GetValues(typeof(DWEnums.ExportOptions))
            .Cast<DWEnums.ExportOptions>()
            .Select(rm => new
            {
                Value = rm,
                Description = DWEnums.DescriptionAttr<DWEnums.ExportOptions>(rm)
            });

            DWEnums.ExportOptions selectedExportOptions;
            Enum.TryParse<DWEnums.ExportOptions>(Properties.Settings.Default.exportOption, true, out selectedExportOptions);
            foreach (dynamic item in exportOption.Items)
            {
                if (item.Value == selectedExportOptions)
                    exportOption.SelectedItem = item;
            }



            catchUpSetting.Items.Clear();


            catchUpSetting.ItemsSource = Enum.GetValues(typeof(DWEnums.CatchUpSyncOption))
            .Cast<DWEnums.CatchUpSyncOption>()
            .Select(rm => new
            {
                Value = rm,
                Description = DWEnums.DescriptionAttr<DWEnums.CatchUpSyncOption>(rm)
            });


        }
        private void setDefaultVisibility()
        {
            applySolutions.IsEnabled = true;
            applySolutionPanel.Visibility = Visibility.Visible;
            adowikiupload.IsEnabled = true;
            adowikiuploadpanel.Visibility = Visibility.Visible;
            exportSettings.Visibility = Visibility.Collapsed;
            newConfigSection.Visibility = Visibility.Collapsed;
            compareSettings.Visibility = Visibility.Collapsed;
            forceResetSection.Visibility = Visibility.Collapsed;
            CatchUpSettings.Visibility = Visibility.Collapsed;
        }

        private void comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DWEnums.RunMode selectedRunMode = (DWEnums.RunMode)runMode.SelectedValue;

            switch(selectedRunMode)
            {
                case DWEnums.RunMode.export:
                    setDefaultVisibility();
                    exportSettings.Visibility = Visibility.Visible;
                    applySolutions.IsEnabled = false;
                    applySolutionPanel.Visibility = Visibility.Collapsed;
                    adowikiupload.IsEnabled = false;
                    adowikiuploadpanel.Visibility = Visibility.Collapsed;
                    newConfigSection.Visibility = Visibility.Visible;
                    break;

                case DWEnums.RunMode.compare:
                    setDefaultVisibility();
                    compareSettings.Visibility = Visibility.Visible;
                    applySolutions.IsEnabled = false;
                    applySolutionPanel.Visibility = Visibility.Collapsed;
                    adowikiupload.IsEnabled = false;
                    adowikiuploadpanel.Visibility = Visibility.Collapsed;
                    break;

                case DWEnums.RunMode.wikiUpload:
                    setDefaultVisibility();
                    applySolutions.IsEnabled = false;
                    applySolutionPanel.Visibility = Visibility.Collapsed;
                    adowikiupload.IsEnabled = false;
                    adowikiuploadpanel.Visibility = Visibility.Collapsed;
                    break;

                case DWEnums.RunMode.resetLink:
                    setDefaultVisibility();
                    applySolutions.IsEnabled = false;
                    applySolutionPanel.Visibility = Visibility.Collapsed;
                    adowikiupload.IsEnabled = false;
                    adowikiuploadpanel.Visibility = Visibility.Collapsed;
                    forceResetSection.Visibility = Visibility.Visible;
                    break;

                case DWEnums.RunMode.start:
                    setDefaultVisibility();
                    CatchUpSettings.Visibility = Visibility.Visible;
                    break;


                default:
                    setDefaultVisibility();
                    break;
            }

            if(selectedRunMode != DWEnums.RunMode.export)
            {
            }
            else
            {
                exportSettings.Visibility = Visibility.Visible;
            }



        }

        
        private void editConfigFile_Click(object sender, RoutedEventArgs e)
        {

            EditConfigForm configForm = new EditConfigForm(customConfigFile.SelectedItem.ToString());
            configForm.ShowDialog();

            setHyperlinkURL();

        }

        //private void Close_Click(object sender, RoutedEventArgs e)
        //{
        //    this.Close();
        //}


        private void showLogs_Click(object sender, RoutedEventArgs e)
        {
            string currentDirectory = Directory.GetCurrentDirectory();
            string logDirectory = System.IO.Path.Combine(currentDirectory, "Logs");

            if (Directory.Exists(logDirectory))
            {
                Process.Start("explorer.exe", logDirectory);
            }
            else
            {
                MessageBox.Show("Logs folder does not exist in the current directory");
            }
        }

        private void setHyperlinkURL()
        {
            try
            {
                if(customConfigFile.SelectedItem != null)
                    GlobalVar.configFileName = customConfigFile.SelectedItem.ToString();

                GlobalVar.initConfig();
                GlobalVar.setdataintegratorURL();

                UriBuilder builder = new UriBuilder(GlobalVar.dataintegratorURL);
                builder.Path = $"dualWrite";
                builder.Query = $"axenv={envURL.Text}";

                dataintegratorURL.NavigateUri = builder.Uri;

                dataintegratorURL.Inlines.Clear();
                dataintegratorURL.Inlines.Add(builder.Uri.AbsoluteUri);

            }
            catch
            {

            }
        }

        private void dataintegratorURL_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            string url = e.Uri.ToString();
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        }

        private void customConfigFile_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            setHyperlinkURL();
        }

        private void showLastLog_Click(object sender, RoutedEventArgs e)
        {
            string currentDirectory = Directory.GetCurrentDirectory();
            string logDirectory = System.IO.Path.Combine(currentDirectory, "Logs");
            var file = Directory.GetFiles(logDirectory, "LOG-*").OrderByDescending(f => File.GetCreationTime(f)).First();
            
            if(file != null)
            {
                Process.Start(new ProcessStartInfo(file) { UseShellExecute = true });
            }
        }

        private void OnLightThemeRadioButtonChecked(object sender, RoutedEventArgs e)
        {
            Wpf.Ui.Appearance.ApplicationThemeManager.Apply(ApplicationTheme.Light);
        }

        private void OnDarkThemeRadioButtonChecked(object sender, RoutedEventArgs e)
        {
            Wpf.Ui.Appearance.ApplicationThemeManager.Apply(ApplicationTheme.Dark);
        }
    }
}
