﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpdatesClient.Core
{
    public static class IO
    {
        public static void FileSetNormalAttribute(string path)
        {
            if (File.Exists(path) && File.GetAttributes(path) != FileAttributes.Normal) File.SetAttributes(path, FileAttributes.Normal);
        }


        public static void CreateDirectory(string path)
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }
        public static void RemoveDirectory(string path)
        {
            if (Directory.Exists(path)) Directory.Delete(path, true);
        }
        public static void RecursiveCopy(string pathFrom, string pathTo)
        {
            foreach (DirectoryInfo dir in new DirectoryInfo(pathFrom).GetDirectories())
            {
                CreateDirectory($"{pathTo}\\{dir.Name}");
                RecursiveCopy(dir.FullName, $"{pathTo}\\{dir.Name}");
            }

            foreach (string file in Directory.GetFiles(pathFrom))
            {
                string NameFile = file.Substring(file.LastIndexOf('\\'), file.Length - file.LastIndexOf('\\'));
                string pathToDestFile = $"{pathTo}\\{NameFile}";

                FileSetNormalAttribute(pathToDestFile);

                File.Copy(file, pathToDestFile, true);
            }
        }
        public static void RecursiveHandleFile(string directory, Action<string> action)
        {
            foreach (DirectoryInfo dir in new DirectoryInfo(directory).GetDirectories())
                RecursiveHandleFile(dir.FullName, action);
            
            foreach (string file in Directory.GetFiles(directory))
                action.Invoke(file);
        }
    }
}
