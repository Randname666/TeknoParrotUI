﻿using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Shapes;
using System.Reflection;
using System.Net;
using System.IO;
using System.IO.Compression;
using System.ComponentModel;

namespace TeknoParrotUi.Views
{
    /// <summary>
    /// Interaction logic for DownloadWindow.xaml
    /// </summary>
    public partial class DownloadWindow : MetroWindow
    {
        WebClient wc = new WebClient();
        public string currentGame;
        public DownloadWindow()
        {
            InitializeComponent();
            using (var wc = new WebClient())
            versionText.Text = wc.DownloadString("https://teknoparrot.com/api/version");
            
        }

        /// <summary>
        ///  Show the progress of the download in a progressbar
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            // In case you don't have a progressBar Log the value instead 
            // Console.WriteLine(e.ProgressPercentage);
            progressBar.Value = e.ProgressPercentage;
        }

        private void wc_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                statusText.Text = "Download Cancelled";
                versionText.Visibility = Visibility.Hidden;
                return;
            }

            if (e.Error != null) // We have an error! Retry a few times, then abort.
            {
                statusText.Text = "Error Downloading";
                versionText.Visibility = Visibility.Hidden;
                return;
            }

            statusText.Text = "Download Complete";
            extractUpdate();
        }

        private void Download()
        {
            File.Delete(Environment.GetEnvironmentVariable("TEMP") + "\\teknoparrot.zip");

            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            // This will download a large image from the web, you can change the value
            // i.e a textbox : textBox1.Text

            using (wc)
            {
                wc.Headers.Add("Referer", "https://teknoparrot.com/download");
                wc.DownloadProgressChanged += wc_DownloadProgressChanged;
                wc.DownloadFileCompleted += wc_DownloadFileCompleted;
                wc.DownloadFileAsync(new Uri("https://teknoparrot.com/files/TeknoParrot_" + versionText.Text + ".zip"), Environment.GetEnvironmentVariable("TEMP") + "\\teknoparrot.zip");
            }
        }

        private void cancelDownload()
        {
            wc.CancelAsync();
        }

        private void UpdateCleanup()
        {
            try
            {
                foreach (string file in Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.bak").Where(item => item.EndsWith(".bak")))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {

                    }
                }
                foreach (string file in Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "\\GameProfiles", "*.bak").Where(item => item.EndsWith(".bak")))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {

                    }
                }
                foreach (string file in Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "\\Icons", "*.bak").Where(item => item.EndsWith(".bak")))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {

                    }
                }
            }
            catch
            {
                
            }
        }

        private void Extract(ZipArchive archive, string destinationDirectoryName, bool overwrite)
        {
            try
            {

                int count = 0;
                int current = 0;
                if(MainWindow._parrotData.LastPlayed == null)
                {
                    currentGame = "abc";
                }
                else
                {
                    currentGame = MainWindow._parrotData.LastPlayed;
                }
                
                
                foreach (ZipArchiveEntry file in archive.Entries)
                {
                    Console.WriteLine(file.Name);
                    if (file.Name == "")
                    {
                        //issa directory
                        count += 1;
                    }
                    else if (file.Name == currentGame + ".png")
                    {
                        count += 1;
                    }
                    else
                    {
                        count += 1;
                        try
                        {
                            File.Move(file.FullName, file.FullName + ".bak");
                        }
                        catch
                        {
                            //most likely either the file doesn't exist (so it's new in this release) or it's in use so we'll skip it
                        }
                    }
                }

                foreach (ZipArchiveEntry file in archive.Entries)
                {
                    if (file.Name == currentGame + ".png")
                    {
                        current += 1;
                    }
                    else
                    {
                        Console.WriteLine(file.Name);
                        string completeFileName = System.IO.Path.Combine(destinationDirectoryName, file.FullName);
                        if (file.Name == "")
                        { //Assuming Empty for Directory
                            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(completeFileName));
                            continue;
                        }
                        try
                        {
                            file.ExtractToFile(completeFileName, true);
                        }
                        catch
                        {
                            //most likely the file is in use, this should've been solved by moving in use files.

                        }
                        current += 1;
                        progressBar.Value = (current / count) * 100;
                    }
                }
                UpdateCleanup();
                
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);

                
            }
        }

        private void extractUpdate()
        {
            //this initial cleanup is to remove left over files
            UpdateCleanup();
            progressBar.Value = 0;
            statusText.Text = "Extracting update...";
            versionText.Visibility = Visibility.Hidden;
            ZipArchive archive = ZipFile.OpenRead(Environment.GetEnvironmentVariable("TEMP") + "\\teknoparrot.zip");
            string myExeDir = AppDomain.CurrentDomain.BaseDirectory;
            

            Extract(archive, myExeDir, true);
            if (MessageBox.Show(
                                $"Would you like to restart me to finish the update? Otherwise, I will close TeknoParrotUi for you to reopen.",
                                "Update Complete", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
            {
                string[] psargs = Environment.GetCommandLineArgs();
                System.Diagnostics.Process.Start(Application.ResourceAssembly.Location,psargs[0]);
                Application.Current.Shutdown();
            }
            else
            {
                Application.Current.Shutdown();
            }
        }

        private void MetroWindow_ContentRendered(object sender, EventArgs e)
        {
            try
            {
                Download();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            cancelDownload();
        }
    }
}
