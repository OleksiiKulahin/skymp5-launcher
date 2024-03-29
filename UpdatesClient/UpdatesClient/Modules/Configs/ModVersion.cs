﻿using Newtonsoft.Json;
using System;
using System.IO;
using UpdatesClient.Core;
using UpdatesClient.Modules.Configs.Helpers;
using UpdatesClient.Modules.Configs.Models;
using Res = UpdatesClient.Properties.Resources;

namespace UpdatesClient.Modules.Configs
{
    internal class ModVersion
    {
        private static readonly object sync = new object();
        private static ModVersionModel model = new ModVersionModel();

        public static string Version
        {
            get { return model.Version; }
            set { model.Version = value; }
        }
        public static bool? HasRuFixConsole
        {
            get { return model.HasRuFixConsole; }
            set { model.HasRuFixConsole = value; }
        }
        public static DateTime LastDmpReported
        {
            get { return model.LastDmpReported ?? default; }
            set { model.LastDmpReported = value; }
        }

        internal static void Load()
        {
            try
            {
                if (string.IsNullOrEmpty(Settings.PathToSkyrim)) return;

                string path = $"{Settings.PathToSkyrim}\\version.json";
                if (File.Exists(path))
                {
                    lock (sync)
                        model = JsonConvert.DeserializeObject<ModVersionModel>(File.ReadAllText(path));
                }
                else  
                {
                    model = new ModVersionModel();
                }
            }
            catch (Exception e)
            {
                Logger.Error("Version_Load", e);
                model = new ModVersionModel();
            }
            ExperimentalFunctions.Use("ModVerLoad", () =>
            {
                if (model == null) model = new ModVersionModel();
            });
        }
        internal static void Save()
        {
            try
            {
                if (string.IsNullOrEmpty(Settings.PathToSkyrim)) return;
                string path = $"{Settings.PathToSkyrim}\\version.json";

                if (File.Exists(path) && File.GetAttributes(path) != FileAttributes.Normal) File.SetAttributes(path, FileAttributes.Normal);

                lock (sync)
                    File.WriteAllText(path, JsonConvert.SerializeObject(model));
            }
            catch (Exception e)
            {
                NotifyController.Show(UI.Controllers.PopupNotify.Error, Res.Error, Res.ErrorSaveFileBusy);
                Logger.Error("Version_Save", e);
            }
        }
        internal static void Reset()
        {
            try
            {
                if (string.IsNullOrEmpty(Settings.PathToSkyrim)) return;
                string path = $"{Settings.PathToSkyrim}\\version.json";
                lock (sync)
                    if (File.Exists(path)) File.Delete(path);
                model = new ModVersionModel();
            }
            catch (Exception e)
            {
                Logger.Error("Version_Reset", e);
            }
        }
    }
}
