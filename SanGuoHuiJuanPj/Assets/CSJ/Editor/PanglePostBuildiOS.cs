#if UNITY_IOS
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

// ReSharper disable once CheckNamespace
namespace PangleInternal.Editor.Postbuild
{
    public static class PanglePostBuildiOS
    {
        [PostProcessBuild(10001)]
        public static void OnPostprocessBuild(BuildTarget buildTarget, string buildPath)
        {

        }

        private static void PrepareProject(string buildPath)
        {
            var projPath = Path.Combine(buildPath, "Unity-iPhone.xcodeproj/project.pbxproj");
            var project = new PBXProject();
            project.ReadFromString(File.ReadAllText(projPath));

            var targets = GetTargets(project);

            // The Pangle iOS SDK now includes Swift, so these properties ensure Xcode handles that properly.
            project.UpdateBuildProperty(targets, "SWIFT_VERSION", new[] {"5.0"}, null);
            new[] {
                "GCC_ENABLE_OBJC_EXCEPTIONS"
            }.ToList().ForEach(name =>
                project.UpdateBuildProperty(targets, name, Yes, No));

            File.WriteAllText(projPath, project.WriteToString());
        }

        #region Helpers

        private static readonly string[] Yes = {"YES"};
        private static readonly string[] No = {"NO"};

        private static IEnumerable<string> GetTargets(PBXProject project)
        {
            var targets = new[] {
                project.ProjectGuid(),
                GetMainTarget(project),
                GetUnityFrameworkTarget(project)
            };
            return targets.Where(target => target != null);
        }

        private static string GetMainTarget(PBXProject project)
        {
            return
#if UNITY_2019_3_OR_NEWER
                project.GetUnityMainTargetGuid()
#else
                project.TargetGuidByName("Unity-iPhone")
#endif
                ;
        }

        private static string GetUnityFrameworkTarget(PBXProject project)
        {
            return
#if UNITY_2019_3_OR_NEWER
                project.GetUnityFrameworkTargetGuid()
#else
                project.TargetGuidByName("UnityFramework")
#endif
                ;
        }

        #endregion
    }
}
#endif
