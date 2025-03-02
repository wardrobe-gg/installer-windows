using System;
using System.IO;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Security.Principal;
using System.Diagnostics;

namespace WardrobeInstaller
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly string HOSTS_INSERT = "51.68.220.202 s.optifine.net # INSERTED BY WARDROBE";
        private static readonly string HOSTS_FILEPATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers/etc/hosts");
        private static readonly string LOG_DIRECTORY = Environment.GetEnvironmentVariable("APPDATA") + "\\Wardrobe Installer\\logs";

        public MainWindow()
        {
            Directory.CreateDirectory(LOG_DIRECTORY);
            File.WriteAllText(LOG_DIRECTORY + "\\latest.log", "Wardrobe Installer Logs");
            if (!IsUserAdministrator())
            {
                using (StreamWriter logs = File.AppendText(LOG_DIRECTORY + "\\latest.log"))
                {
                    logs.WriteLine("Attempted to launch without administrative privileges, aborting!");
                }
                MessageBox.Show("You need to run the Wardrobe Installer as an Administrator!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
            }
            InitializeComponent();
        }
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private async void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            using (StreamWriter logs = File.AppendText(LOG_DIRECTORY + "\\latest.log"))
            {
                logs.WriteLine("Exiting...");
            }
            Environment.Exit(0);
        }

        private async void LauncherOptifineSelection_MouseDown(object sender, MouseButtonEventArgs e)
        {
            LauncherSelectFrame.Visibility = Visibility.Collapsed;
            ConfigureOptifine.Visibility = Visibility.Visible;
            using (StreamWriter logs = File.AppendText(LOG_DIRECTORY + "\\latest.log"))
            {
                logs.WriteLine("Optifine installation selected");
            }
        }

        private void ConfigureOptifineReturnArrow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ConfigureOptifine.Visibility = Visibility.Collapsed;
            LauncherSelectFrame.Visibility = Visibility.Visible;
        }

        private void FAQLink_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Process.Start("https://www.wardrobe.gg/faq");
        }

        private void InstallOptifineButton_Click(object sender, RoutedEventArgs e)
        {
            if(IsWardrobeInstalled())
            {
                using (StreamWriter logs = File.AppendText(LOG_DIRECTORY + "\\latest.log"))
                {
                    logs.WriteLine("User attempted to install Wardrobe for Optifine, but it already exists (hosts file entry present).");
                }
                //MessageBox.Show("You already have Wardrobe for OptiFine installed!", "Wardrobe Installer", MessageBoxButton.OK, MessageBoxImage.Information);
                ConfigureOptifine.Visibility = Visibility.Collapsed;
                FinalizeLabel.Text = "Installation\r\nCancelled";
                FinalizeErrorMessage.Text = "Wardrobe is already installed on\r\nyour system!";
                FinalizeErrorMessage.Visibility = Visibility.Visible;
                FinalizeInstallation.Visibility = Visibility.Visible;
                return;
            }
            // Install 4 Optifine
            try
            {
                InstallWardrobe(1);
            } 
            catch(Exception ex)
            {
                using (StreamWriter logs = File.AppendText(LOG_DIRECTORY + "\\latest.log"))
                {
                    logs.WriteLine("Something went wrong when installing Wardrobe for Optifine!");
                    logs.Write(ex + "\n");
                }

                ConfigureOptifine.Visibility = Visibility.Collapsed;
                // Change labels to error before showing Panel
                FinalizeLabel.Visibility = Visibility.Hidden;
                FinalizeFailLabel.Visibility = Visibility.Visible;
                FinalizeErrorMessage.Visibility = Visibility.Visible;
                // Show Finalization page
                FinalizeInstallation.Visibility = Visibility.Visible;
                return;
            }

            // Installed logic here
            // TODO: Add visibility toggle for "Wardrobe is installed" Page.
            ConfigureOptifine.Visibility = Visibility.Collapsed;
            FinalizeInstallation.Visibility = Visibility.Visible;
        }
        private void UninstallOptifineButton_Click(object sender, RoutedEventArgs e)
        {
            if (!IsWardrobeInstalled())
            {
                //MessageBox.Show("Wardrobe is not installed on your system.", "Wardrobe Installer", MessageBoxButton.OK, MessageBoxImage.Warning);
                ConfigureOptifine.Visibility = Visibility.Collapsed;
                FinalizeLabel.Text = "Operation\r\nCancelled";
                FinalizeErrorMessage.Text = "Wardrobe was not found on\r\nyour system!";
                FinalizeErrorMessage.Visibility = Visibility.Visible;
                FinalizeInstallation.Visibility = Visibility.Visible;
                return;
            }
            try
            {
                UninstallWardrobe(1);
            }
            catch (Exception ex)
            {
                using (StreamWriter logs = File.AppendText(LOG_DIRECTORY + "\\latest.log"))
                {
                    logs.WriteLine("Something went wrong when installing Wardrobe for Optifine!");
                    logs.Write(ex + "\n");
                }
                ConfigureOptifine.Visibility = Visibility.Collapsed;
                // Change labels to error before showing Panel
                FinalizeLabel.Visibility = Visibility.Hidden;
                FinalizeFailLabel.Visibility = Visibility.Visible;
                // FinalizeErrorMessage.Visibility = Visibility.Visible;
                // Show Finalization page
                FinalizeInstallation.Visibility = Visibility.Visible;
                return;
            }
            ConfigureOptifine.Visibility = Visibility.Collapsed;
            FinalizeLabel.Text = "Wardrobe was\r\nuninstalled\r\nsuccessfully!";
            FinalizeInstallation.Visibility = Visibility.Visible;
            return;

        }

        private void InstallAnotherButton_Click(object sender, RoutedEventArgs e)
        {
            using (StreamWriter logs = File.AppendText(LOG_DIRECTORY + "\\latest.log"))
            {
                logs.WriteLine("User chose to install another version.");
            }
            FinalizeInstallation.Visibility = Visibility.Collapsed;
            // Resetting the Finalize page to defaults
            FinalizeFailLabel.Visibility = Visibility.Hidden;
            FinalizeLabel.Visibility = Visibility.Visible;
            FinalizeLabel.Text = "Wardrobe was\r\nsuccessfully\r\ninstalled!";
            FinalizeErrorMessage.Visibility = Visibility.Hidden;
            // Go back to the selection frame
            LauncherSelectFrame.Visibility = Visibility.Visible;
        }


        // Logical functions
        public static bool IsUserAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private bool IsWardrobeInstalled()
        {
            try
            {
                string hostsContents = File.ReadAllText(HOSTS_FILEPATH);
                return hostsContents.Contains(HOSTS_INSERT);
            }
            catch(Exception)
            {
                return false;
            }
        }

        private void InstallWardrobe(int location, String version = null)
        {
            switch(location)
            {
                // Optifine
                case 1:
                    // Enable showing optifine capes
                    try
                    {
                        string ofOptionsPath = Environment.GetEnvironmentVariable("APPDATA") + "\\.minecraft\\optionsof.txt";
                        if (File.Exists(ofOptionsPath))
                        {
                            var ofOptions = File.ReadAllLines(ofOptionsPath);
                            for (int i = 0; i < ofOptions.Length; i++)
                            {
                                if (ofOptions[i].StartsWith("ofShowCapes"))
                                {
                                    ofOptions[i] = "ofShowCapes:true";
                                }
                            }

                            File.WriteAllLines(ofOptionsPath, ofOptions);
                        }
                    }
                    catch (Exception ex)
                    {
                        using (StreamWriter logs = File.AppendText(LOG_DIRECTORY + "\\latest.log"))
                        {
                            logs.Write(ex + "\n");
                        }
                    }
                    // Change Minecraft settings to show capes
                    try
                    {
                        string optionsPath = Environment.GetEnvironmentVariable("APPDATA") + "\\.minecraft\\options.txt";

                        if (File.Exists(optionsPath))
                        {
                            var options = File.ReadAllLines(optionsPath);
                            for (int i = 0; i < options.Length; i++)
                            {
                                if (options[i].StartsWith("modelPart_cape"))
                                {
                                    options[i] = "modelPart_cape:true";
                                }
                            }

                            File.WriteAllLines(optionsPath, options);
                        }
                    }
                    catch (Exception ex)
                    {
                        // ex + "\n"
                        using (StreamWriter logs = File.AppendText(LOG_DIRECTORY + "\\latest.log"))
                        {
                            logs.Write(ex + "\n");
                        }
                    }
                    // Check if the hosts file exists
                    if (!File.Exists(HOSTS_FILEPATH))
                    {
                        using (StreamWriter hosts = File.AppendText(HOSTS_FILEPATH))
                        {
                            hosts.WriteLine(HOSTS_INSERT);
                        }
                        return;
                    }

                    // Unlock the file fron system write-protection
                    File.SetAttributes(HOSTS_FILEPATH, FileAttributes.Normal);

                    // Insert to hosts file
                    using (StreamWriter hosts = File.AppendText(HOSTS_FILEPATH))
                    {
                        hosts.WriteLine();
                        hosts.WriteLine(HOSTS_INSERT);
                    }

                    // Relock the file
                    File.SetAttributes(HOSTS_FILEPATH, FileAttributes.ReadOnly | FileAttributes.System);

                    // End
                    using (StreamWriter logs = File.AppendText(LOG_DIRECTORY + "\\latest.log"))
                    {
                        logs.WriteLine("Wardrobe for Optifine was successfully installed.");
                    }
                    return;
                // Fabric
                case 2:
                    MessageBox.Show("Fabric is not yet supported. Please check back later!", "Wardrobe Installer", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                // Forge
                case 3:
                    MessageBox.Show("Forge is not yet supported. Please check back later!", "Wardrobe Installer", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                default:
                    MessageBox.Show("Unknown installation location encountered. Please contact Wardrobe Support for more help.", "Wardrobe Installer", MessageBoxButton.OK, MessageBoxImage.Error);
                    using (StreamWriter logs = File.AppendText(LOG_DIRECTORY + "\\latest.log"))
                    {
                        logs.WriteLine("Error - Unknown install location: " + location);
                    }
                    return;
            }
        }

        private void UninstallWardrobe(int location)
        {
            switch (location)
            {
                // Optifine
                case 1:
                    File.SetAttributes(HOSTS_FILEPATH, FileAttributes.Normal);
                    var hostsFileContent = File.ReadAllLines(HOSTS_FILEPATH);
                    var filteredLines = hostsFileContent.Where(line => !line.Contains(HOSTS_INSERT));
                    File.WriteAllText(HOSTS_FILEPATH, String.Join("\n", filteredLines).Trim() + "\n");
                    return;
                default:
                    MessageBox.Show("Unknown uninstallation location encountered. Please contact Wardrobe Support for more help.", "Wardrobe Installer", MessageBoxButton.OK, MessageBoxImage.Error);
                    using (StreamWriter logs = File.AppendText(LOG_DIRECTORY + "\\latest.log"))
                    {
                        logs.WriteLine("Error - Unknown uninstall location: " + location);
                    }
                    return;
            }
        }
    }
}
