using PangleAdapterScripts.Editor;
using UnityEditor;
using UnityEngine;

public class PangleMenu
{
    [MenuItem("Pangle/About Pangle SDK", false, 0)]
    public static void About()
    {
        PangleAboutDialog.ShowDialog();
    }

    [MenuItem("Pangle/Documentation...", false, 1)]
    public static void Documentation()
    {
        Application.OpenURL("https://www.pangle.cn/");
    }

    [MenuItem("Pangle/Report Issue...", false, 2)]
    public static void ReportIssue()
    {
        Application.OpenURL("https://www.pangle.cn/");
    }

    [MenuItem("Pangle/Manage SDKs...", false, 3)]
    public static void SdkManagerProd()
    {
        PangleSDKManager.ShowSDKManager(stage:false);
    }

    [MenuItem("Pangle/Delete old files", false, 4)]
    public static void DeleteOldFiles()
    {
        SdkFileManager.DeleteOldFile();
    }
}
