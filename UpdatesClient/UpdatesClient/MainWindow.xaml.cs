﻿using BlendModeEffectLibrary;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using UpdatesClient.Core;
using UpdatesClient.Core.Models;
using UpdatesClient.Core.Network;
using UpdatesClient.Modules.Configs;
using UpdatesClient.Modules.GameManager;
using UpdatesClient.Modules.GameManager.AntiCheat;
using UpdatesClient.Modules.GameManager.Model;
using UpdatesClient.UI.Controllers;
using Yandex.Metrica;
using Res = UpdatesClient.Properties.Resources;

namespace UpdatesClient
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //Check file "SkyrimSE.exe"
        //Check file "skse64_loader.exe"
        //Base color: #FF04D9FF
        //War color: #FFFF7604
        //Er color: #FFFF0404

        private bool blockMainBtn = false;

        public MainWindow()
        {
            InitializeComponent();
            TitleWindow.MouseLeftButtonDown += (s, e) => DragMove();
            authorization.TitleWindow.MouseLeftButtonDown += (s, e) => DragMove();
            CloseBtn.Click += (s, e) =>
            {
                YandexMetrica.Config.CrashTracking = false;
                Application.Current.Shutdown();
            };
            authorization.CloseBtn.Click += (s, e) =>
            {
                YandexMetrica.Config.CrashTracking = false;
                Application.Current.Shutdown();
            };
            MinBtn.Click += (s, e) =>
            {
                WindowState = WindowState.Minimized;
            };
            authorization.MinBtn.Click += (s, e) =>
            {
                WindowState = WindowState.Minimized;
            };
            progressBar.Hide();
            userButton.LogoutBtn.Click += LogOut_Click;
            authorization.SignIn += Authorization_SignIn;

            wind.Loaded += Wind_Loaded;
        }
        private ImageBrush GetGridBackGround(FrameworkElement element)
        {
            Point relativePoint = element.TranslatePoint(new Point(0, 0), mainGrid);
            var image = (BitmapSource)((ImageBrush)wind.Background).ImageSource;
            double w = wind.Width / image.Width;
            double h = wind.Height / image.Height;
            var im = new CroppedBitmap(image, new Int32Rect((int)(relativePoint.X * w), (int)(relativePoint.Y * h), (int)(element.Width * w), (int)(element.Height * h)));
            return new ImageBrush(im);
        }
        private async void Authorization_SignIn()
        {
            try
            {
                await GetLogin();
                authorization.Visibility = Visibility.Collapsed;
            }
            catch
            {
                authorization.Visibility = Visibility.Visible;
                return;
            }
            CheckClientUpdates();
        }
        private async Task GetLogin()
        {
            var username = await Account.GetLogin();
            JObject jObject = JObject.Parse(username);
            string name = jObject["name"].ToString();
            Settings.UserName = name;
            userButton.Text = name;
        }
        private async void Wind_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                string pathToSkyrim = Settings.PathToSkyrim;
                ResultGameVerification result;
                do
                {
                    while (string.IsNullOrEmpty(pathToSkyrim) || !Directory.Exists(pathToSkyrim))
                    {
                        string path = GameVerification.GetGameFolder();
                        if (string.IsNullOrEmpty(path))
                        {
                            App.AppCurrent.Shutdown();
                            Close();
                            return;
                        }
                        pathToSkyrim = path;
                    }

                    result = GameVerification.VerifyGame(pathToSkyrim, null);
                    if (result.IsGameFound)
                    {
                        if (Settings.PathToSkyrim != pathToSkyrim)
                        {
                            Settings.PathToSkyrim = pathToSkyrim;
                            Settings.Save();
                        }
                        break;
                    }

                    pathToSkyrim = null;
                    MessageBox.Show(Res.SkyrimNotFound, Res.Error);
                } while (true);

                ModVersion.Load();
                FileWatcher.Init();

                if (!result.IsSKSEFound && MessageBox.Show(Res.SKSENotFound, Res.Warning, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    await InstallSKSE();
                }
                if (!result.IsRuFixConsoleFound
                        && ModVersion.HasRuFixConsole == null
                        && MessageBox.Show(Res.SSERFix, Res.Warning, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    await InstallRuFixConsole();
                    ModVersion.Save();
                }
                else
                {
                    ModVersion.HasRuFixConsole = result.IsRuFixConsoleFound;
                    ModVersion.Save();
                }

                FillServerList();

                try
                {
                    serverListBg.Effect = new OverlayEffect()
                    {
                        BInput = GetGridBackGround(serverList)
                    };
                }
                catch (Exception oe)
                {
                    Logger.Error("Wind_Loaded_OverlayEffect", oe);
                }
                
                Authorization_SignIn();
            }
            catch (Exception er)
            {
                Logger.FatalError("Wind_Loaded", er);
                MessageBox.Show(Res.InitError, Res.Error);
                Close();
            }
        }
        private async void FillServerList()
        {
            List<ServerModel> list = null;
            string servers;
            try
            {
                servers = await ServerModel.GetServers();
                ServerModel.Save(servers);
            }
            catch
            {
                servers = ServerModel.Load();
            }
            list = ServerModel.ParseServersToList(servers);
            list.RemoveAll(x => x.IsEmpty());
            serverList.ItemsSource = null;
            serverList.ItemsSource = list;
            serverList.SelectedItem = list.Find(x => x.ID == Settings.LastServerID);
        }
        private async Task InstallSKSE()
        {
            string url = await Net.GetUrlToSKSE();
            string destinationPath = $@"{Settings.PathToSkyrimTmp}{url.Substring(url.LastIndexOf('/'), url.Length - url.LastIndexOf('/'))}";

            bool ok = await DownloadFile(destinationPath, url, Res.DownloadingSKSE);

            if (ok)
            {
                progressBar.Show(true, Res.ExtractingSKSE);
                try
                {
                    await Task.Run(() => Unpacker.UnpackArchive(destinationPath,
                        Settings.PathToSkyrim, Path.GetFileNameWithoutExtension(destinationPath)));
                }
                catch (Exception e)
                {
                    Logger.Error("ExtractSKSE", e);
                    NotifyController.Show(e);
                    mainButton.ButtonStatus = MainButtonStatus.Retry;
                }
                progressBar.Hide();
            }
        }
        private async Task InstallRuFixConsole()
        {
            string url = Net.URL_Mod_RuFix;
            string destinationPath = $@"{Settings.PathToSkyrimTmp}{url.Substring(url.LastIndexOf('/'), url.Length - url.LastIndexOf('/'))}";

            bool ok = await DownloadFile(destinationPath, url, Res.DownloadingSSERuFixConsole);
            if (ok)
            {
                try
                {
                    progressBar.Show(true, Res.Extracting);
                    ModVersion.HasRuFixConsole = await Task.Run(() => Unpacker.UnpackArchive(destinationPath, Settings.PathToSkyrim + "\\Data"));
                    ModVersion.Save();
                    progressBar.Hide();
                }
                catch (Exception e)
                {
                    Logger.Error("ExtractRuFix", e);
                    NotifyController.Show(e);
                }
            }
        }
        private async void CheckClientUpdates()
        {
            progressBar.Show(true, Res.CheckingUpdates);
            try
            {
                if (await Net.UpdateAvailable()) mainButton.ButtonStatus = MainButtonStatus.Update;
                else mainButton.ButtonStatus = MainButtonStatus.Play;
            }
            catch (Exception e)
            {
                Logger.Error("CheckClient", e);
                NotifyController.Show(e);
                mainButton.ButtonStatus = MainButtonStatus.Retry;
            }
            progressBar.Hide();
            blockMainBtn = false;
        }
        private void MainBtn_Click(object sender, EventArgs e)
        {
            if (blockMainBtn) return;
            blockMainBtn = true;
            switch (mainButton.ButtonStatus)
            {
                case MainButtonStatus.Play:
                    Play();
                    break;
                case MainButtonStatus.Update:
                    UpdateClient();
                    break;
                case MainButtonStatus.Retry:
                    CheckClientUpdates();
                    break;
            }
        }
        private async void Play()
        {
            if (!File.Exists($"{Settings.PathToSkyrim}\\skse64_loader.exe"))
            {
                Wind_Loaded(null, null);
                blockMainBtn = false;
                return;
            }

            if (serverList.SelectedItem == null)
            {
                NotifyController.Show(PopupNotify.Error, Res.Warning, Res.SelectServer);
                blockMainBtn = false;
                return;
            }

            SetServer();
            ServerModel server = (ServerModel)serverList.SelectedItem;
            SetSession(await Account.GetSession(server.Address));
            SetMods();

            try
            {
                Hide();
                bool crash = await GameLauncher.StartGame();
                Show();

                if (crash)
                {
                    YandexMetrica.ReportEvent("CrashDetected");
                    await Task.Delay(500);
                    await ReportDmp();
                }
            }
            catch
            {
                YandexMetrica.ReportEvent("HasNotAccess");
                Close();
            }
            blockMainBtn = false;
        }
        private void SetMods()
        {
            string path = Settings.PathToLocalSkyrim + "Plugins.txt";
            string content = @"# This file is used by Skyrim to keep track of your downloaded content.
# Please do not modify this file.
*FarmSystem.esp";

            if (!Directory.Exists(Settings.PathToLocalSkyrim)) Directory.CreateDirectory(Settings.PathToLocalSkyrim);
            if (File.Exists(path)) File.SetAttributes(path, FileAttributes.Normal);

            try
            {
                File.WriteAllText(path, content);
            }
            catch (UnauthorizedAccessException)
            {
                FileAttributes attr = new FileInfo(path).Attributes;
                Logger.Error("Write_Plugin_UAException", new UnauthorizedAccessException($"UnAuthorizedAccessException: Unable to access file. Attributes: {attr}"));
            }
            catch (Exception e)
            {
                Logger.Error("Write_Plugin_txt", e);
            }
        }
        private void SetServer()
        {
            if (serverList.SelectedItem == null) return;
            if (!Directory.Exists(Path.GetDirectoryName(Settings.PathToSkympClientSettings))) 
                Directory.CreateDirectory(Path.GetDirectoryName(Settings.PathToSkympClientSettings));
            SkympClientSettingsModel oldServer;
            
            if (File.Exists(Settings.PathToSkympClientSettings))
            {
                try
                {
                    oldServer = JsonConvert.DeserializeObject<SkympClientSettingsModel>(File.ReadAllText(Settings.PathToSkympClientSettings));
                }
                catch (JsonSerializationException)
                {
                    NotifyController.Show(PopupNotify.Error, Res.Error, Res.ErrorReadSkyMPSettings);
                    return;
                }
                catch (JsonReaderException)
                {
                    NotifyController.Show(PopupNotify.Error, Res.Error, Res.ErrorReadSkyMPSettings);
                    return;
                }
            }
            else
            {
                oldServer = new SkympClientSettingsModel();
                oldServer.IsEnableConsole = false;
                oldServer.IsShowMe = false;
            }

            ServerModel newServer = (ServerModel)serverList.SelectedItem;
            if (newServer.IsSameServer(oldServer)) return;
            File.WriteAllText(Settings.PathToSkympClientSettings, JsonConvert.SerializeObject(newServer.ToSkympClientSettings(oldServer), Formatting.Indented));
            Settings.Save();
        }
        private void SetSession(object gameData)
        {
            try
            {
                SkympClientSettingsModel settingsModel = JsonConvert.DeserializeObject<SkympClientSettingsModel>(File.ReadAllText(Settings.PathToSkympClientSettings));
                settingsModel.GameData = gameData;
                File.WriteAllText(Settings.PathToSkympClientSettings, JsonConvert.SerializeObject(settingsModel, Formatting.Indented));
            }
            catch (UnauthorizedAccessException)
            {
                FileAttributes attr = new FileInfo(Settings.PathToSkympClientSettings).Attributes;
                Logger.Error("SetSession_UAException", new UnauthorizedAccessException($"UnAuthorizedAccessException: Unable to access file. Attributes: {attr}"));
            }
            catch (Exception e)
            {
                Logger.Error("SetSession", e);
            }
        }
        private async Task ReportDmp()
        {
            string pathToDmps = $@"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\My Games\Skyrim Special Edition\SKSE\Crashdumps\";
            if (!Directory.Exists(pathToDmps)) return;
            try
            {
                DateTime dt = ModVersion.LastDmpReported;
                string fileName = "";
                foreach (FileSystemInfo fileSI in new DirectoryInfo(pathToDmps).GetFileSystemInfos())
                {
                    if (fileSI.Extension == ".dmp")
                    {
                        if (dt < Convert.ToDateTime(fileSI.CreationTime))
                        {
                            dt = Convert.ToDateTime(fileSI.CreationTime);
                            fileName = fileSI.Name;
                        }
                    }
                }
                if (!string.IsNullOrEmpty(fileName))
                {
                    if (await Net.ReportDmp(pathToDmps + fileName))
                        YandexMetrica.ReportEvent("CrashReported");
                    else YandexMetrica.ReportEvent("CantReport");
                    ModVersion.LastDmpReported = dt;
                    ModVersion.Save();

                    await Task.Delay(3000);
                    File.Delete(pathToDmps + fileName);
                }
            }
            catch (Exception e)
            {
                if (!(e is WebException) && (e is SocketException))
                    Logger.Error("ReportDmp", e);
            }
        }
        private async void UpdateClient()
        {
            (string, string) url = await Net.GetUrlToClient();
            string destinationPath = $"{Settings.PathToSkyrimTmp}client.zip";

            try
            {
                if (File.Exists(destinationPath)) File.Delete(destinationPath);
            }
            catch (Exception e)
            {
                Logger.Error("DelClientZip", e);
            }
            
            bool ok = await DownloadFile(destinationPath, url.Item1, Res.DownloadingClient, url.Item2);

            if (ok)
            {
                progressBar.Show(true, Res.ExtractingClient);
                try
                {
                    if (await Task.Run(() => Unpacker.UnpackArchive(destinationPath, Settings.PathToSkyrim, "client")))
                    {
                        ModVersion.Version = url.Item2;
                        ModVersion.Save();
                        NotifyController.Show(PopupNotify.Normal, Res.InstallationCompleted, Res.HaveAGG);
                    }
                }
                catch (Exception e)
                {
                    Logger.Error("Extract", e);
                    NotifyController.Show(e);
                    mainButton.ButtonStatus = MainButtonStatus.Retry;
                    blockMainBtn = false;
                    return;
                }
                progressBar.Hide();
            }
            CheckClientUpdates();
            blockMainBtn = false;
        }
        private void Downloader_DownloadChanged(long downloaded, long size, double prDown)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Invoker)delegate
            {
                try
                {
                    progressBar.Size = size;
                    progressBar.Update(downloaded);
                }
                catch { }
            });
        }
        private async Task<bool> DownloadFile(string destinationPath, string url, string status, string vers = null, int c = 0)
        {
            progressBar.Show(false, $"{status}{(c != 0 ? $" ({Res.Attempt} №{c + 1})" : "")}", vers);

            Downloader downloader = new Downloader(destinationPath, url);
            downloader.DownloadChanged += Downloader_DownloadChanged;
            progressBar.Start();
            bool ok = await downloader.StartSync();
            progressBar.Stop();
            progressBar.Hide();

            if (!ok && c < 3) return await DownloadFile(destinationPath, url, status, vers, ++c);
            return ok;
        }
        private void RefreshServerList(object sender, RoutedEventArgs e)
        {
            FillServerList();
        }
        private void ServerList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (serverList.SelectedIndex != -1)
            {
                Settings.LastServerID = ((ServerModel)serverList.SelectedItem).ID;
            }
        }
        private void ServerList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DependencyObject source = (DependencyObject)e.OriginalSource;

            if (source is TextBlock block)
            {
                if (block.DataContext is ServerModel)
                {
                    MainBtn_Click(sender, e);
                }
            }
        }
        private void LogOut_Click(object sender, RoutedEventArgs e)
        {
            //TODO: аннулирование токена

            Settings.UserId = 0;
            Settings.UserToken = "";
            Settings.Save();

            authorization.Visibility = Visibility.Visible;
        }
    }
}
