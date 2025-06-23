using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace GUI_for_ATS
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Settings settings;
        private bool _isDirty = false; // Track if there are unsaved changes
        private bool _isInitializing = false;


        public MainWindow()
        {
            InitializeComponent();
            //this.Title = Assembly.GetExecutingAssembly().GetName().Name;
            settings = Settings.LoadSettings();
            
            _isInitializing = true;

            ShowAllData();
            DisableAllFullFields();


            _isInitializing = false;
        }



        private void DisableAllFullFields() {

            if (!string.IsNullOrWhiteSpace(TextBoxAzureClientId.Text))
                TextBoxAzureClientId.IsEnabled = false;
            else
                ButtonEnterAzureClientId.Content = "Hide value";

            if (!string.IsNullOrWhiteSpace(TextBoxAzureTenantId.Text))
                TextBoxAzureTenantId.IsEnabled = false;
            else 
                ButtonEnterAzureTenantId.Content = "Hide value";

            if (!string.IsNullOrWhiteSpace(TextBoxAzureClientSecret.Text))
                TextBoxAzureClientSecret.IsEnabled = false;
            else 
                ButtonEnterAzureClientSecret.Content = "Hide value";

            if (!string.IsNullOrWhiteSpace(TextBoxSigntoolPath.Text))
                TextBoxSigntoolPath.IsEnabled = false;

          
            if (!string.IsNullOrWhiteSpace(TextBoxDlibDllPath.Text))
                TextBoxDlibDllPath.IsEnabled = false;

            if (!string.IsNullOrWhiteSpace(TextBoxTimestampServer.Text))
                TextBoxTimestampServer.IsEnabled = false;


            if (!string.IsNullOrWhiteSpace(TextBoxMetadataJson.Text))
                TextBoxMetadataJson.IsEnabled = false;

           

        }

        private void ShowAllData()
        {
            if (settings != null)
            {
                TextBoxAzureClientId.Text = Helper.Decrypt(settings.AzureClientId);
                TextBoxAzureTenantId.Text = Helper.Decrypt(settings.AzureTenantId);
                TextBoxAzureClientSecret.Text = Helper.Decrypt(settings.AzureClientSecret);

                TextBoxSigntoolPath.Text = settings.SigntoolPath;
                TextBoxSigntoolPath.ToolTip = settings.SigntoolPath;

                TextBoxDlibDllPath.Text = settings.AzureCodeSigningDlibDllPath;
                TextBoxDlibDllPath.ToolTip = settings.AzureCodeSigningDlibDllPath;

                TextBoxTimestampServer.Text = settings.TimeStampServer;
                TextBoxTimestampServer.ToolTip = settings.TimeStampServer;

                TextBoxMetadataJson.Text = settings.MetadataJsonPath;
                TextBoxMetadataJson.ToolTip = settings.MetadataJsonPath;

                if (File.Exists(settings.LastFilePath))
                {
                    LabelFileName.Content = Path.GetFileName(settings.LastFilePath);
                    LabelFileName.ToolTip = Path.GetFileName(settings.LastFilePath);

                    LabelFilePath.Content = Path.GetFullPath(settings.LastFilePath);
                    LabelFilePath.ToolTip = Path.GetFullPath(settings.LastFilePath);

                    ImageFileIcon.Source = Helper.GetFileIcon(settings.LastFilePath);
                    ImageFileIcon.Source = ImageFileIcon.Source ?? Helper.CreatePlaceholderIcon();
                    ButtonRemoveFile.Visibility = Visibility.Visible;
                }
                else
                {
                    ClearFileInfo();
                }
            }
            else
            {
                ClearAllFields();
                settings = Settings.LoadSettings();
            }
           

    }

    private void ClearFileInfo()
        {
            LabelFileName.Content = LabelFileName.ToolTip = string.Empty;
            LabelFilePath.Content = LabelFilePath.ToolTip = string.Empty;
            ImageFileIcon.Source = null;
            ButtonRemoveFile.Visibility = Visibility.Hidden;
        }

        private void ClearAllFields()
        {
            TextBoxAzureClientId.Text = TextBoxAzureTenantId.Text = TextBoxAzureClientSecret.Text = string.Empty;
            TextBoxSigntoolPath.Text = string.Empty; 
            TextBoxSigntoolPath.ToolTip = string.Empty;
            TextBoxDlibDllPath.Text = string.Empty; 
            TextBoxDlibDllPath.ToolTip = string.Empty;
            TextBoxTimestampServer.Text = string.Empty;
            TextBoxTimestampServer.ToolTip = string.Empty;
            TextBoxMetadataJson.Text = string.Empty;
            TextBoxMetadataJson.ToolTip = string.Empty;
            ClearFileInfo();
        }

        // All button click handlers remain the same as in original code
        // (ButtonEnterAzureClientId_Click, ButtonEnterAzureTenantId_Click, etc.)
        // ...

        private async void RunCommand()
        {
            if (settings != null)
            {
                try
                {
                    Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });

                    string plainAzureClientId = Helper.Decrypt(settings.AzureClientId);
                    string plainAzureTenantId = Helper.Decrypt(settings.AzureTenantId);
                    string plainAzureClientSecret = Helper.Decrypt(settings.AzureClientSecret);

                    string envVariables = $"set \"AZURE_CLIENT_ID={plainAzureClientId}\" && " +
                                         $"set \"AZURE_TENANT_ID={plainAzureTenantId}\" && " +
                                         $"set \"AZURE_CLIENT_SECRET={plainAzureClientSecret}\" && ";

                    string command = $"{envVariables}\"{TextBoxSigntoolPath.Text}\" " +
                                    $"sign /v /debug /fd SHA256 /tr \"{TextBoxTimestampServer.Text}\" " +
                                    $"/td SHA256 /dlib \"{TextBoxDlibDllPath.Text}\" " +
                                    $"/dmdf \"{TextBoxMetadataJson.Text}\" \"{settings.LastFilePath}\"";

                    Dispatcher.Invoke(() => OutputBox.Document.Blocks.Clear());

                    await Task.Run(() =>
                    {
                        using (Process process = new Process())
                        {
                            process.StartInfo = new ProcessStartInfo
                            {
                                FileName = "cmd.exe",
                                Arguments = $"/C \"{command}\"",
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            };

                            process.OutputDataReceived += (sender, e) =>
                            {
                                if (!string.IsNullOrEmpty(e.Data))
                                    AppendOutput(e.Data);
                            };

                            process.ErrorDataReceived += (sender, e) =>
                            {
                                if (!string.IsNullOrEmpty(e.Data))
                                    AppendOutput(e.Data);
                            };

                            process.Start();
                            process.BeginOutputReadLine();
                            process.BeginErrorReadLine();
                            process.WaitForExit();
                        }
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => AppendOutput(ex.Message));
                }
                finally
                {
                    Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
                }
            }
        }

        private void AppendOutput(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            Dispatcher.Invoke(() =>
            {
                var paragraph = new Paragraph { LineHeight = 1 };
                var run = new Run(text);

                if (text.Contains("Number of files successfully Signed:"))
                {
                    var match = Regex.Match(text, @"\d+$");
                    if (match.Success && int.Parse(match.Value) > 0)
                        run.Foreground = Brushes.Green;
                }
                else if (text.Contains("Number of warnings:"))
                {
                    var match = Regex.Match(text, @"\d+$");
                    if (match.Success && int.Parse(match.Value) > 0)
                        run.Foreground = Brushes.Goldenrod;
                }
                else if (text.Contains("Number of errors:"))
                {
                    var match = Regex.Match(text, @"\d+$");
                    if (match.Success && int.Parse(match.Value) > 0)
                        run.Foreground = Brushes.OrangeRed;
                }
                else if (text.Contains("SignTool Error:") || text.Contains("Unhandled managed exception"))
                {
                    run.Foreground = Brushes.OrangeRed;
                }

                paragraph.Inlines.Add(run);
                OutputBox.Document.Blocks.Add(paragraph);
                OutputBox.ScrollToEnd();
            });
        }
        
        /// <summary>
        /// Lock the field
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private void ButtonEnterAzureClientId_Click(object sender, RoutedEventArgs e)
        {
           

            TextBoxAzureClientId.IsEnabled = !TextBoxAzureClientId.IsEnabled;

            // Optional: Change button content to reflect state
            var button = sender as Button; 
            if (button != null)
            {
                button.Content = TextBoxAzureClientId.IsEnabled ? "Lock" : "Show";

                if (button.Content.ToString() == "Lock")
                    return;
            }


            if (TextBoxAzureClientId.Text != null)
            {
                if (settings != null)
                {
                    if (TextBoxAzureClientId.Text == "")
                    {
                        if (settings.AzureClientId != "")
                        {
                            if (MessageBox.Show("Are you sure you want to delete the AZURE_CLIENT_ID value?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                            {
                                settings.AzureClientId = TextBoxAzureClientId.Text;
                                //Keyboard.ClearFocus();
                            }
                        }
                    }
                    else
                    {
                        settings.AzureClientId = Helper.Encrypt(TextBoxAzureClientId.Text);
                    }

                    settings.Save();
                    ShowAllData();
                }
                else
                {
                    MessageBox.Show("Settings is null.", "Error Saving Settings");
                }
            }


        }

        private void ButtonEnterAzureTenantId_Click(object sender, RoutedEventArgs e)
        {

            TextBoxAzureTenantId.IsEnabled = !TextBoxAzureTenantId.IsEnabled;

            // Optional: Change button content to reflect state
            var button = sender as Button;
            if (button != null)
            {
                button.Content = TextBoxAzureTenantId.IsEnabled ? "Lock" : "Show";
                if (button.Content.ToString() == "Lock")
                    return;
            }
        

            if (TextBoxAzureTenantId.Text != null)
            {
                if (settings != null)
                {
                    if (TextBoxAzureTenantId.Text == "")
                    {
                        if (settings.AzureTenantId != "")
                        {
                            if (MessageBox.Show("Are you sure you want to delete the AZURE_TENANT_ID value?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                            {
                                settings.AzureTenantId = TextBoxAzureTenantId.Text;
                                //Keyboard.ClearFocus();
                            }
                        }
                    }
                    else
                    {
                        settings.AzureTenantId = Helper.Encrypt(TextBoxAzureTenantId.Text);
                    }

                    settings.Save();
                    ShowAllData();
                }
                else
                {
                    MessageBox.Show("Settings is null.","Error Saving Settings");
                }
            }
            
        }

        private void ButtonEnterAzureClientSecret_Click(object sender, RoutedEventArgs e)
        {
            TextBoxAzureClientSecret.IsEnabled = !TextBoxAzureClientSecret.IsEnabled;

            // Optional: Change button content to reflect state
            var button = sender as Button;
            if (button != null)
            {
                button.Content = TextBoxAzureClientSecret.IsEnabled ? "Lock" : "Show";
                if (button.Content.ToString() == "Lock")
                    return;
            }

            if (TextBoxAzureClientSecret.Text != null)
            {
                if (settings != null)
                {
                    if (TextBoxAzureClientSecret.Text == "")
                    {
                        if (settings.AzureClientSecret != "")
                        {
                            if (MessageBox.Show("Are you sure you want to delete the AZURE_CLIENT_SECRET value?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                            {
                                settings.AzureClientSecret = TextBoxAzureClientSecret.Text;
                                Keyboard.ClearFocus();
                            }
                        }
                    }
                    else
                    {
                        settings.AzureClientSecret = Helper.Encrypt(TextBoxAzureClientSecret.Text);
                    }

                    settings.Save();
                    ShowAllData();
                }
                else
                {
                    MessageBox.Show("Settings is null.", "Error Saving Settings");
                }
            }



        }

        private void ButtonEditSigntoolPath_Click(object sender, RoutedEventArgs e)
        {
            TextBoxSigntoolPath.IsEnabled = !TextBoxSigntoolPath.IsEnabled;

            // Optional: Change button content to reflect state
            var button = sender as Button;
            if (button != null)
            {
                button.Content = TextBoxSigntoolPath.IsEnabled ? "Lock" : "Show";
            }

            if (settings != null)
            {
                string path = settings.SigntoolPath;

                if (!string.IsNullOrEmpty(TextBoxSigntoolPath.Text))
                {
                    settings.SigntoolPath = TextBoxSigntoolPath.Text.Trim('"').Trim();
                    settings.Save();
                    ShowAllData();
                }
                
            }
            else
            {
                MessageBox.Show("Settings is null.", "Error Saving Settings");
            }

        }

        private void ButtonSelectSigntoolPath_Click(object sender, RoutedEventArgs e)
        {
            if (settings != null)
            {

                // Create and configure the OpenFileDialog
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Title = "Select signtool.exe",
                    Filter = "signtool.exe|signtool.exe|Executable Files (*.exe)|*.exe|All Files (*.*)|*.*",
                    InitialDirectory = GetInitialDirectorySignTool(),
                    CheckFileExists = true,
                    CheckPathExists = true
                };

                // Show the dialog and process the result
                if (openFileDialog.ShowDialog() == true)
                {
                    // Update the settings and UI with the selected path
                    settings.SigntoolPath = openFileDialog.FileName;
                    TextBoxSigntoolPath.Text = openFileDialog.FileName;
                    TextBoxSigntoolPath.ToolTip = openFileDialog.FileName;

                    settings.Save();
                    ShowAllData();

                }
            }
            else
            {
                MessageBox.Show("Settings is null.", "Error Saving Settings");
            }

        }

        private string GetInitialDirectorySignTool()
        {
            // Try to use the existing path if available
            if (!string.IsNullOrEmpty(settings.SigntoolPath) && File.Exists(settings.SigntoolPath))
            {
                return Path.GetDirectoryName(settings.SigntoolPath);
            }

            // Common locations where signtool might be found
            string[] commonPaths = {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Windows Kits", "10", "bin"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft SDKs", "Windows"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft SDKs", "Windows")
             };

            // Return the first valid path that exists
            foreach (var path in commonPaths)
            {
                if (Directory.Exists(path))
                {
                    return path;
                }
            }

            // Fallback to Desktop if no valid paths found
            return Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        }

        private void ButtonSelectDlibDllPath_Click(object sender, RoutedEventArgs e)
        {
            if (settings != null)
            {

                // Configure the OpenFileDialog
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Title = "Select Azure.CodeSigning.Dlib.dll",
                    Filter = "DLL Files (*.dll)|*.dll|All Files (*.*)|*.*",
                    InitialDirectory = GetInitialDirectoryDlibDllPath(),
                    CheckFileExists = true,
                    CheckPathExists = true,
                    Multiselect = false
                };

                // Show the dialog and process the result
                if (openFileDialog.ShowDialog() == true)
                {
                    // Update settings and UI
                    settings.AzureCodeSigningDlibDllPath = openFileDialog.FileName;
                    TextBoxDlibDllPath.Text = openFileDialog.FileName;
                    TextBoxDlibDllPath.ToolTip = openFileDialog.FileName;

                    settings.Save();
                    ShowAllData();
               
                }
            }
            else
            {
                MessageBox.Show("Settings is null.", "Error Saving Settings");
            }

        }

        private string GetInitialDirectoryDlibDllPath()
        {
            // 1. First try the existing path if it exists
            if (!string.IsNullOrEmpty(settings.AzureCodeSigningDlibDllPath))
            {
                string existingDir = Path.GetDirectoryName(settings.AzureCodeSigningDlibDllPath);
                if (Directory.Exists(existingDir))
                {
                    return existingDir;
                }
            }

            // 2. Fallback to common installation locations
            string[] commonPaths =
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft SDKs"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft SDKs"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles), "Microsoft Shared")
            };

            foreach (var path in commonPaths)
            {
                if (Directory.Exists(path))
                {
                    return path;
                }
            }

            // 3. Final fallback to user's Documents folder
            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }

        private void ButtonEditDlibDllPath_Click(object sender, RoutedEventArgs e)
        {
            TextBoxDlibDllPath.IsEnabled = !TextBoxDlibDllPath.IsEnabled;

            //if (!TextBoxDlibDllPath.IsEnabled)
            //    TextBoxDlibDllPath.Foreground = new SolidColorBrush(Colors.DarkGray);
            //else
            //    TextBoxDlibDllPath.Foreground = new SolidColorBrush(Colors.Black);

            // Optional: Change button content to reflect state
            var button = sender as Button;
            if (button != null)
            {
                button.Content = TextBoxDlibDllPath.IsEnabled ? "Lock" : "Show";
            }


            if (settings != null)
            {
                string path = settings.AzureCodeSigningDlibDllPath;

                
                if (!string.IsNullOrEmpty(TextBoxDlibDllPath.Text))
                {
                    settings.AzureCodeSigningDlibDllPath = TextBoxDlibDllPath.Text.Trim('"').Trim();
                    settings.Save();
                    ShowAllData();
                }
                
            }
            else
            {
                MessageBox.Show("Settings is null.", "Error Saving Settings");
            }
        }

        private void ButtonDefaultTimestamp_Click(object sender, RoutedEventArgs e)
        {
            TextBoxTimestampServer.Text = "http://timestamp.acs.microsoft.com";
        }

        private void ButtonEditTimestamp_Click(object sender, RoutedEventArgs e)
        {
            TextBoxTimestampServer.IsEnabled = !TextBoxTimestampServer.IsEnabled;

            // Optional: Change button content to reflect state
            var button = sender as Button;
            if (button != null)
            {
                button.Content = TextBoxTimestampServer.IsEnabled ? "Lock" : "Show";
            }

            if (settings != null)
            {
                //string initialTimeStampServer = settings.TimeStampServer;

                if (!string.IsNullOrEmpty(TextBoxTimestampServer.Text))
                {
                    settings.TimeStampServer = TextBoxTimestampServer.Text.Trim('"').Trim();
                    settings.Save();
                    ShowAllData();
                }
                
            }
            else
            {
                MessageBox.Show("Settings is null.", "Error Saving Settings");
            }
        }

        private void ButtonEditMetadataJson_Click(object sender, RoutedEventArgs e)
        {
            TextBoxMetadataJson.IsEnabled = !TextBoxMetadataJson.IsEnabled;

            // Optional: Change button content to reflect state
            var button = sender as Button;
            if (button != null)
            {
                button.Content = TextBoxMetadataJson.IsEnabled ? "Lock" : "Show";
            }
            _isDirty = true;


            if (settings != null)
            {
                
                if (!string.IsNullOrEmpty(TextBoxMetadataJson.Text))
                {
                    settings.MetadataJsonPath = TextBoxMetadataJson.Text.Trim('"').Trim();
                    settings.Save();
                    ShowAllData();
                }
               
            }
            else
            {
                MessageBox.Show("Settings is null.", "Error Saving Settings");
            }
        }

        private void ButtonSelectMetadataJson_Click(object sender, RoutedEventArgs e)
        {
            if (settings != null)
            {
                // Configure the OpenFileDialog for JSON files
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Title = "Select metadata.json File",
                    Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                    InitialDirectory = GetInitialDirectoryMetadataJson(),
                    CheckFileExists = true,
                    CheckPathExists = true,
                    Multiselect = false,
                    DefaultExt = ".json"
                };

                // Show the dialog and process the result
                if (openFileDialog.ShowDialog() == true)
                {
                    // Update settings and UI
                    settings.MetadataJsonPath = openFileDialog.FileName;
                    TextBoxMetadataJson.Text = openFileDialog.FileName;
                    TextBoxMetadataJson.ToolTip = openFileDialog.FileName;
                
                    settings.Save();
                    ShowAllData();
                    _isDirty = false; 

                }
                else 
                    _isDirty = true;
            }
            else
            {
                MessageBox.Show("Settings is null.", "Error Saving Settings");
            }
        }

        private string GetInitialDirectoryMetadataJson()
        {
            // 1. First try the existing path if it exists
            if (!string.IsNullOrEmpty(settings.MetadataJsonPath) && File.Exists(settings.MetadataJsonPath))
            {
                return Path.GetDirectoryName(settings.MetadataJsonPath);
            }

            // 2. Try the same directory as the last signed file (if exists)
            if (!string.IsNullOrEmpty(settings.LastFilePath) && File.Exists(settings.LastFilePath))
            {
                return Path.GetDirectoryName(settings.LastFilePath);
            }

            // 3. Fallback to user's Documents folder
            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }

     
        private void ButtonSelectFile_Click(object sender, RoutedEventArgs e)
        {

            if (settings != null)
            {
                // Configure the OpenFileDialog
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Title = "Select File to Sign",
                    Filter = GetFileFilterforSingingFile(),
                    InitialDirectory = GetInitialDirectoryForSigningFile(),
                    CheckFileExists = true,
                    CheckPathExists = true,
                    Multiselect = false,
                    ValidateNames = true
                };

                // Show the dialog and process the result
                if (openFileDialog.ShowDialog() == true)
                {
                    // Update settings and UI
                    settings.LastFilePath = openFileDialog.FileName;
                    settings.LastPath = Path.GetDirectoryName(openFileDialog.FileName);
                    settings.Save();
                    ShowAllData();
                    _isDirty = false;

                    UpdateFileUI(openFileDialog.FileName);
                
                }
                else
                    _isDirty = true;
            }
            else
            {
                MessageBox.Show("Settings is null.", "Error Saving Settings");
            }

        }

        private string GetInitialDirectoryForSigningFile()
        {
            // 1. First try the last used file path directory
            if (!string.IsNullOrEmpty(settings.LastFilePath) && File.Exists(settings.LastFilePath))
            {
                return Path.GetDirectoryName(settings.LastFilePath);
            }

            // 2. Then try the last used directory
            if (!string.IsNullOrEmpty(settings.LastPath) && Directory.Exists(settings.LastPath))
            {
                return settings.LastPath;
            }

            // 3. Fallback to user's Documents folder
            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }

        private string GetFileFilterforSingingFile()
        {
            return "Signable Files (*.exe, *.dll, *.msi, *.sys, *.cab, *.cat, *.ocx, *.xpi, *.ps1, *.psm1, *.vsix, *.nupkg, *.appx, *.msix)|" +
                   "*.exe;*.dll;*.msi;*.sys;*.cab;*.cat;*.ocx;*.xpi;*.ps1;*.psm1;*.vsix;*.nupkg;*.appx;*.msix|" +
                   "Executable Files (*.exe, *.dll, *.ocx)|*.exe;*.dll;*.ocx|" +
                   "Installation Files (*.msi, *.cab, *.appx, *.msix)|*.msi;*.cab;*.appx;*.msix|" +
                   "PowerShell Scripts (*.ps1, *.psm1)|*.ps1;*.psm1|" +
                   "All Files (*.*)|*.*";
        }

        private void UpdateFileUI(string filePath)
        {
            LabelFileName.Content = Path.GetFileName(filePath);
            LabelFileName.ToolTip = filePath;

            LabelFilePath.Content = Path.GetDirectoryName(filePath);
            LabelFilePath.ToolTip = Path.GetDirectoryName(filePath);

            ImageFileIcon.Source = Helper.GetFileIcon(filePath) ?? Helper.CreatePlaceholderIcon();
            ButtonRemoveFile.Visibility = Visibility.Visible;
        }

        private void ButtonSign_Click(object sender, RoutedEventArgs e)
        {
            ShowAllData();

            if (AreAllDataValid(true))
            {
                RunCommand();
            }
            else
            {
                // After the user has been given the opportunity to correct all data, everything will be verified again, and if any issues remain, an error message will be displayed.
                if (AreAllDataValid(false))
                {
                    RunCommand();
                }
                else
                {
                    MessageBox.Show("Please double-check all data and file paths to proceed with the signing process.", "Data Check Before Signing", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private bool AreAllDataValid(bool runSolution)
        {
            if (DoesFileExist(runSolution) &
                IsAzureClientIdEntered(runSolution) &
                IsAzureTenantIdEntered(runSolution) &
                IsAzureClientSecretEntered(runSolution) &
                DoesSigntoolExist(runSolution) &
                DoesDlibPathExist(runSolution) &
                IsTimestampServerAvailable(runSolution) &
                DoesMetadataJsonPathExist(runSolution))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool DoesFileExist(bool runSolution)
        {
            if (settings != null)
            {
                if (File.Exists(settings.LastFilePath))
                {
                    return true;
                }
                else
                {
                    if (runSolution)
                    {
                        MessageBox.Show("In the next step, please select the file to be signed.", "No file was selected to sign or the file no longer exists.", MessageBoxButton.OK, MessageBoxImage.Information);
                        ButtonSelectFile_Click(null, null);
                    }

                    return false;
                }
            }
            else
            {
                MessageBox.Show("Settings is null.", "Error Saving Settings");
                return false;
            }
        }

        private bool IsAzureClientIdEntered(bool runSolution)
        {
            if (settings != null)
            {
                if (!System.String.IsNullOrEmpty(settings.AzureClientId))
                {
                    return true;
                }
                else
                {
                    if (runSolution)
                    {
                        ButtonEnterAzureClientId_Click(null, null);
                    }

                    return false;
                }
            }
            else
            {
                MessageBox.Show("Settings is null.", "Error Saving Settings");
                return false;
            }
        }

        private bool IsAzureTenantIdEntered(bool runSolution)
        {
            if (settings != null)
            {
                if (!System.String.IsNullOrEmpty(settings.AzureTenantId))
                {
                    return true;
                }
                else
                {
                    if (runSolution)
                    {
                        ButtonEnterAzureTenantId_Click(null, null);
                    }

                    return false;
                }
            }
            else
            {
                MessageBox.Show("Settings is null.", "Error Saving Settings");
                return false;
            }
        }

        private bool IsAzureClientSecretEntered(bool runSolution)
        {
            if (settings != null)
            {
                if (!System.String.IsNullOrEmpty(settings.AzureClientSecret))
                {
                    return true;
                }
                else
                {
                    if (runSolution)
                    {
                        ButtonEnterAzureClientSecret_Click(null, null);
                    }

                    return false;
                }
            }
            else
            {
                MessageBox.Show("Settings is null.", "Error Saving Settings");
                return false;
            }
        }

        private bool DoesSigntoolExist(bool runSolution)
        {
            if (settings != null)
            {
                if (File.Exists(settings.SigntoolPath))
                {
                    return true;
                }
                else
                {
                    if (runSolution)
                    {
                        string message = "Please select the signtool.exe file in the next step.";

                        if (settings.SigntoolPath == "")
                        {
                            MessageBox.Show(message, "The signtool.exe file is required for signing.", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show(message, "The signtool.exe file does not exist.", MessageBoxButton.OK, MessageBoxImage.Information);
                        }

                        ButtonSelectSigntoolPath_Click(null, null);
                    }

                    return false;
                }
            }
            else
            {
                MessageBox.Show("Settings is null.", "Error Saving Settings");
                return false;
            }
        }

        private bool DoesDlibPathExist(bool runSolution)
        {
            if (settings != null)
            {
                if (File.Exists(settings.AzureCodeSigningDlibDllPath))
                {
                    return true;
                }
                else
                {
                    if (runSolution)
                    {
                        string message = "Please select the Azure.CodeSigning.Dlib.dll file in the next step.";

                        if (settings.AzureCodeSigningDlibDllPath == "")
                        {
                            MessageBox.Show(message, "The Azure.CodeSigning.Dlib.dll file is required for signing.", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show(message, "The Azure.CodeSigning.Dlib.dll file does not exist.", MessageBoxButton.OK, MessageBoxImage.Information);
                        }

                        ButtonSelectDlibDllPath_Click(null, null);
                    }

                    return false;
                }
            }
            else
            {
                MessageBox.Show("Settings is null.", "Error Saving Settings");
                return false;
            }
        }

        private bool IsTimestampServerAvailable(bool runSolution)
        {
            if (settings != null)
            {
                string url = settings.TimeStampServer;
                string caption = "The timestamp server is unavailable.";

                try
                {
                    // Using HttpClient properly for .NET 4.8
                    using (var client = new HttpClient())
                    {
                        client.Timeout = TimeSpan.FromSeconds(5);
                        client.DefaultRequestHeaders.UserAgent.ParseAdd("TimestampVerifier/1.0");

                        // Using GetAsync properly with .NET 4.8's async pattern
                        var task = client.GetAsync(url);
                        task.Wait(); // Blocking wait is acceptable here for sync method

                        if (task.Result.IsSuccessStatusCode)
                        {
                            return true;
                        }

                        if (runSolution)
                        {
                            MessageBox.Show(settings.TimeStampServer, caption, MessageBoxButton.OK, MessageBoxImage.Information);
                            ButtonEditTimestamp_Click(null, null);
                        }
                        return false;
                    }

                }
                catch (AggregateException ae)
                {
                    // Unwrap aggregate exception common with .NET 4.8 async
                    var baseEx = ae.GetBaseException();
                    if (runSolution)
                    {
                        MessageBox.Show(baseEx.Message + "\n\n" + settings.TimeStampServer, caption, MessageBoxButton.OK, MessageBoxImage.Information);
                        ButtonEditTimestamp_Click(null, null);
                    }
                    return false;
                }
                catch (Exception es)
                {
                    if (runSolution)
                    {
                        MessageBox.Show(es.Message + "\n\n" + settings.TimeStampServer, caption, MessageBoxButton.OK, MessageBoxImage.Information);
                        ButtonEditTimestamp_Click(null, null);
                    }
                    return false;
                }
            }
            else
            {
                MessageBox.Show("Settings is null.", "Error Saving Settings");
                return false;
            }
        }

      
        private bool DoesMetadataJsonPathExist(bool runSolution)
        {
            if (settings != null)
            {
                if (File.Exists(settings.MetadataJsonPath))
                {
                    return true;
                }
                else
                {
                    if (runSolution)
                    {
                        string message = "Please select the metadata.json file in the next step.";

                        if (settings.MetadataJsonPath == "")
                        {
                            MessageBox.Show(message, "The metadata.json file is required for signing.", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show(message, "The metadata.json file does not exist.", MessageBoxButton.OK, MessageBoxImage.Information);
                        }

                        ButtonSelectMetadataJson_Click(null, null);
                    }

                    return false;
                }
            }
            else
            {
                MessageBox.Show("Settings is null.", "Error Saving Settings");
                return false;
            }
        }
        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            
            // Save settings before exit
            settings.Save();

            _isDirty = false;

         
        }


        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            RequestApplicationExit();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!RequestApplicationExit())
            {
                e.Cancel = true; // Prevent closing if user cancels
            }
        }

        private bool RequestApplicationExit()
        {
            if (_isDirty)
            {
                var result = MessageBox.Show("You have unsaved changes. Save and Exit?",
                                           "Confirm Save & Exit",
                                           MessageBoxButton.YesNoCancel,
                                           MessageBoxImage.Warning);

                if (result == MessageBoxResult.Cancel)
                {
                    return false;
                }

                if (result == MessageBoxResult.No)
                {
                    // Optionally trigger save here
                    //settings.Save();
                    return false;
                }
            }

            // Save settings before exit
            settings.Save();

            _isDirty = true; 

            Application.Current.Shutdown();
            return true;
        }

        private void ButtonRemoveFile_Click(object sender, RoutedEventArgs e)
        {
            LabelFileName.Content = string.Empty; 
            LabelFileName.ToolTip = string.Empty;

            LabelFilePath.Content = string.Empty;
            LabelFilePath.ToolTip = string.Empty;

            ImageFileIcon.Source = Helper.CreatePlaceholderIcon();
            ButtonRemoveFile.Visibility = Visibility.Visible;

            if (settings != null)
            {
                if (settings.LastFilePath != null)
                {
                    string tempString = Path.GetDirectoryName(settings.LastFilePath);

                    if (!string.IsNullOrEmpty(tempString))
                    {
                        settings.LastPath = tempString;
                    }
                }

                settings.LastFilePath = "";
                settings.Save();
                ShowAllData();
            }
            else
            {
                MessageBox.Show("Settings is null.", "Error Saving Settings");
            }

        }

        private void TextBoxAzureClientId_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isInitializing) // Skip during initialization
            {
                _isDirty = true;
            }
        }

        private void TextBoxDlibDllPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isInitializing) // Skip during initialization
            {
                _isDirty = true;
            }
        }

        
        private void TextBoxTimestampServer_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isInitializing) // Skip during initialization
            {
                _isDirty = true;
            }
        }
        

        private void TextBoxMetadataJson_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isInitializing) // Skip during initialization
            {
                _isDirty = true;
            }
        }

        private void TextBoxSigntoolPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isInitializing) // Skip during initialization
            {
                _isDirty = true;
            }
        }



    }
}