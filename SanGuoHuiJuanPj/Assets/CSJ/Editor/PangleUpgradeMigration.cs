using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using OS = PangleOSCommands;

public static class PangleUpgradeMigration
{
    private static readonly Dictionary<string,string> LocationMapping = new Dictionary<string, string>
    {
        // { "Pangle/Editor", "Pangle/Scripts/Editor" },
        // { "Pangle/Internal", "Pangle/Scripts/Internal" },
        // { "Pangle/ThirdParty", "Pangle/Scripts/ThirdParty" },
        // { "Pangle/Pangle.cs", "Pangle/Scripts/Pangle.cs" },
        // { "Pangle/PangleManager.cs", "Pangle/Scripts/PangleManager.cs" },
        // { "Pangle/Pangle-dark-logo.png", "Pangle/Sample/Pangle-dark-logo.png" },
        // { "Scenes/PangleDemoScene.unity", "Pangle/Sample/PangleDemoScene.unity" },
        // { "Scripts/GravityController.cs", "Pangle/Sample/GravityController.cs" },
        // { "Scripts/PangleDemoGUI.cs", "Pangle/Sample/PangleDemoGUI.cs" },
        // { "Scripts/PangleEventListener.cs", "Pangle/Sample/PangleEventListener.cs" },
        // { "Plugins/Android/Pangle", "Pangle/Plugins/Android/Pangle.plugin" },
        // { "Plugins/Android/README.txt", "Pangle/Plugins/Android/Pangle.plugin/README.txt" },
        // { "Plugins/iOS/PangleBinding.m", "Pangle/Plugins/iOS/PangleBinding.m" },
        // { "Plugins/iOS/PangleManager.h", "Pangle/Plugins/iOS/PangleManager.h" },
        // { "Plugins/iOS/PangleManager.mm", "Pangle/Plugins/iOS/PangleManager.mm" },
        // { "Plugins/iOS/PangleSDKFramework.framework", "Pangle/Plugins/iOS/PangleSDKFramework.framework" }
    };
    private static readonly Dictionary<string,string> ManualMapping = new Dictionary<string, string>
    {
        // { "Plugins/Android/res/drawable/ic_launcher_foreground.png", "Pangle/Plugins/Android/Pangle.plugin/res/drawable/" },
        // { "Plugins/Android/res/drawable-anydpi-v26/app_icon.xml", "Pangle/Plugins/Android/Pangle.plugin/res/drawable-anydpi-v26/" },
        // { "Plugins/Android/res/values/ic_launcher_background.xml", "Pangle/Plugins/Android/Pangle.plugin/res/values/" }
    };

    private static readonly string[] LegacyMediationDirs = {
        // "Assets/Plugins/Android/Pangle-support",
        // "Assets/Plugins/iOS/Pangle-Mediation-Adapters",
        // "Assets/Pangle/Plugins/iOS/PangleSDKFramework.framework"
    };

    private static readonly string[] RedundantJars = {
        // "Assets/Pangle/Plugins/Android/Pangle-sdk-base.aar",
        // "Assets/Pangle/Plugins/Android/Pangle-sdk-banner.aar",
        // "Assets/Pangle/Plugins/Android/Pangle-sdk-fullscreen.aar",
        // "Assets/Pangle/Plugins/Android/Pangle-sdk-native-static.aar",
        // "Assets/Pangle/Plugins/Android/Pangle-sdk-interstitial.aar",
        // "Assets/Pangle/Plugins/Android/Pangle-sdk-rewardedvideo.aar",
        // "Assets/Pangle/Plugins/Android/Pangle.plugin/libs/Pangle-sdk-banner.jar",
        // "Assets/Pangle/Plugins/Android/Pangle.plugin/libs/Pangle-sdk-base.jar",
        // "Assets/Pangle/Plugins/Android/Pangle.plugin/libs/Pangle-sdk-interstitial.jar",
        // "Assets/Pangle/Plugins/Android/Pangle.plugin/libs/Pangle-sdk-native-static.jar",
        // "Assets/Pangle/Plugins/Android/Pangle.plugin/libs/Pangle-sdk-rewardedvideo.jar",
        // "Assets/Pangle/Plugins/Android/Pangle.plugin/libs/Pangle-unity-wrappers.jar",
        // "Assets/Pangle/Plugins/Android/Pangle.plugin/libs/Pangle-unity-plugins.jar",
        // "Assets/Pangle/Plugins/Android/Pangle.plugin/libs/Pangle-volley-2.0.0.jar"
    };

    private const string RedundantDir = "Assets/CSJ/PangleUnitypackage";
    private const string RedundantLib = "Assets/Plugins/Android/libs/android-support-v4-23.1.1.jar";
    private const string MigrationBegin = "==========> Beginning Pangle Upgrade Migration";
    private const string MigrationEnd = "==========> Finished Pangle Upgrade Migration";
    private const string ManualMigrationNote =
        "The following files cannot be automatically migrated since they may contain non-Pangle code.\nPlease review this list and manually migrate if needed:\n";
    private const string ManualFollowUpWarning =
        "Some files were not able to be migrated! Please scroll up and handle manually as needed.";

    public static bool LegacyPanglePresent()
    {
        return DoesExist(RedundantLib)
               || RedundantJars.Select(DoesExist).Any(p => p)
               || LocationMapping.Keys.Select(p => DoesExist("Assets/" + p)).Any(p => p);
    }

    public static bool LegacyMediationPresent()
    {
        return LegacyMediationDirs.Select(DoesExist).Any(p => p);
    }

    public static void DoSDKMigration()
    {
        Debug.Log(MigrationBegin);

        if (DoesExist(RedundantDir)) {
            OS.RmDir(RedundantDir);
        }
        if (DoesExist(RedundantLib)) {
            OS.Rm(RedundantLib);
            var redundantJarDir = RedundantLib.Remove(RedundantLib.LastIndexOf("/", StringComparison.Ordinal)) + "/";
            var redundantJarDirContents = OS.GetFileSystemEntries(redundantJarDir);
            if (!redundantJarDirContents.Any()) OS.RmDir(redundantJarDir);
        }

        foreach (var jar in RedundantJars.Where(DoesExist)) OS.Rm(jar);

        var allSucceeded = true;

        foreach (var entry in LocationMapping) {
            var source = Path.Combine("Assets", entry.Key);
            var dest = Path.Combine("Assets", entry.Value);
            if (!DoesExist(source)) continue;
            allSucceeded &= OS.Mv(source, dest);
        }

        var showNote = false;
        var migrationNote = new StringBuilder(ManualMigrationNote);
        foreach (var entry in ManualMapping)
            if (DoesExist(Path.Combine("Assets", entry.Key))) {
                showNote = true;
                migrationNote.AppendFormat("'{0}' to '{1}'\n", entry.Key, entry.Value);
            }
        if (showNote)
            Debug.LogWarning(migrationNote);

        if (!allSucceeded)
            Debug.LogWarning(ManualFollowUpWarning);
        AssetDatabase.Refresh();
        Debug.Log(MigrationEnd);
    }

    private static bool DoesExist(string path)
    {
        return File.Exists(path)
               || Directory.Exists(path)
               || path.EndsWith("/*") && Directory.Exists(Path.GetDirectoryName(path));
    }
}
