using UnityEditor;
using UnityEngine;
using ByteDance.Union;

public class PangleAboutDialog : ScriptableWizard
{
    public static void ShowDialog()
    {
        DisplayWizard<PangleAboutDialog>("About Pangle SDK", "OK");
    }

    protected override bool DrawWizardGUI()
    {
        EditorGUILayout.LabelField("Pangle SDK version " + Pangle.PangleSdkVersion);

        EditorGUILayout.Space();
        if (GUILayout.Button("iOS Release Notes"))
            Application.OpenURL("https://github.com/bytedance/Bytedance-UnionAD");
        if (GUILayout.Button("Android Release Notes"))
            Application.OpenURL("https://github.com/JohnnyWangMiura/Pangle_Android_SDK_Integration_Guideline");

        return false;
    }

    private void OnWizardCreate() { }
}
