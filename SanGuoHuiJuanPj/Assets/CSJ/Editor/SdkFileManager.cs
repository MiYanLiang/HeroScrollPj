using System.IO;
using UnityEditor;
using UnityEngine;

namespace PangleAdapterScripts.Editor
{
    public static class SdkFileManager
    {
        private static readonly string[] OldFilePath =
        {
            "Plugins/Android/volley.jar",
            "Plugins/Android/android-gif-drawable-1.2.6.aar",
            "Plugins/Android/res/xml/file_paths.xml",
            "Plugins/Android/res/xml/network_config.xml",
            "PangleAdapterScripts"
        };

        private const string JavaFileDirectory = "PangleAdapterScripts/Scripts/Android";

        public static void DeleteOldFile()
        {
            Debug.Log("Start delete old files");

            foreach (var t in OldFilePath)
            {
                AssetDatabase.DeleteAsset($"Assets/{t}");
                Debug.Log($"Delete Assets/{t}");
            }

            DeleteJavaFile();
            AssetDatabase.Refresh();
            Debug.Log($"Delete completed");
        }

        private static void DeleteJavaFile()
        {
            var javaDirectory = new DirectoryInfo(Path.Combine(Application.dataPath, JavaFileDirectory));
            if (!javaDirectory.Exists)
            {
                return;
            }

            foreach (var fileInfo in javaDirectory.GetFiles())
            {
                if (fileInfo.Extension != ".java") continue;
                var filePath = $"Assets/{JavaFileDirectory}/{fileInfo.Name}";
                AssetDatabase.DeleteAsset(filePath);
                Debug.Log($"Delete Assets/{filePath}");
            }
        }
    }
}