using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace PangleAdapterScripts.Editor
{
    public static class IosProcessScripts
    {
        private const string FileExtension = ".xcconfig";
        private const string BUAdSDK = "-framework \"BUAdSDK\"";
        private const string BUFoundation = "-framework \"BUFoundation\"";
        private const string BUCNAuxiliary = "-framework \"BUCNAuxiliary\"";

        [PostProcessBuild(10002)]
        public static void OnPostprocessBuild(BuildTarget buildTarget, string path)
        {
            var pods = new DirectoryInfo(Path.Combine(path, "Pods"));
            var targetSupportFiles =
                new DirectoryInfo(Path.Combine(pods.FullName, "Target Support Files"));

            if (!targetSupportFiles.Exists)
            {
                Debug.Log($"directory {targetSupportFiles.FullName} not exist");
                return;
            }

            var infos = GETFiles(targetSupportFiles);

            foreach (var fileInfo in infos)
            {
                if (fileInfo.DirectoryName.Contains("UnityFramework"))
                {
                    Debug.Log("Skip "+ fileInfo.Name);
                    continue;
                }
                Debug.Log("Processing "+ fileInfo.Name);
                ModifyFile(fileInfo);
            }
        }

        private static List<FileInfo> GETFiles(DirectoryInfo directoryInfo)
        {
            if (!directoryInfo.Exists)
            {
                return null;
            }

            var fileList = new List<FileInfo>();
            GETFiles(directoryInfo, fileList);
            return fileList;
        }

        private static void GETFiles(DirectoryInfo directoryInfo, List<FileInfo> infos)
        {
            if (!directoryInfo.Exists)
            {
                Debug.Log($"{directoryInfo.Name} not exist");
                return;
            }

            infos.AddRange(directoryInfo.GetFiles().Where(fileInfo => fileInfo.Extension == FileExtension));

            foreach (var directory in directoryInfo.GetDirectories())
            {
                GETFiles(directory, infos);
            }
        }


        private static void ModifyFile(FileInfo fileInfo)
        {
            if (!fileInfo.Exists) return;
            var str = File.ReadAllText(fileInfo.FullName);
            str = str.Replace(BUAdSDK, "");
            str = str.Replace(BUFoundation, "");
            str = str.Replace(BUCNAuxiliary, "");
            File.WriteAllText(fileInfo.FullName, str);
        }
    }
}
