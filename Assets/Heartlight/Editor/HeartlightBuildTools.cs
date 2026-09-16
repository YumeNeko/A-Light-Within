using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Heartlight.Editor
{
    public static class HeartlightBuildTools
    {
        const string ScenePath="Assets/Heartlight/Scenes/Heartlight.unity";

        [MenuItem("心灯/验证当前场景")]
        public static void ValidateSavedScene(){EditorSceneManager.OpenScene(ScenePath);ValidateScene();}

        [MenuItem("心灯/构建 Windows 试玩版")]
        public static void BuildWindows()
        {
            if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            ValidateSavedScene();
            string output=Path.GetFullPath(Path.Combine(Application.dataPath,"..","Builds","Windows"));
            Directory.CreateDirectory(output);
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName=Path.Combine(output,"A Light Within.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Build failed: "+report.summary.result);
            File.Copy(Path.Combine(Application.dataPath,"../Documentation/Sources/Noto_Font_License.txt"),Path.Combine(output,"Noto_Font_License.txt"),true);
            File.Copy(Path.Combine(Application.dataPath,"../Documentation/Sources/Kenney_Protagonist_License.txt"),Path.Combine(output,"Kenney_Protagonist_License.txt"),true);
            Debug.Log("HEARTLIGHT_BUILD_OK "+output);
        }

        static void ValidateScene()
        {
            var director=Object.FindFirstObjectByType<HeartlightDirector>();
            if(director==null||director.nodes==null||director.nodes.Length!=18)throw new Exception("Expected one director with 18 puzzle nodes");
            var ids=new HashSet<string>();
            foreach(var node in director.nodes)
            {
                if(node==null||node.definition==null||!ids.Add(node.definition.id))throw new Exception("Missing or duplicate puzzle node");
                if(node.definition.radius<=0||node.definition.prerequisites.Any(string.IsNullOrEmpty))throw new Exception("Invalid puzzle parameters: "+node.definition.id);
            }
            foreach(var node in director.nodes)foreach(string prerequisite in node.definition.prerequisites)
                if(!ids.Contains(prerequisite)&&prerequisite!="memory_arrival"&&prerequisite!="crossing"&&prerequisite!="pursuit")throw new Exception("Unknown prerequisite: "+prerequisite);
            if(director.chineseFont==null||!director.chineseFont.HasCharacter('心'))throw new Exception("Chinese font missing");
            var traveler=director.GetComponent<TravelerAnimator>();
            if(traveler==null||traveler.idle==null||traveler.run==null||traveler.jump==null)throw new Exception("Traveler animation missing");
            var skin=traveler.model.GetComponentInChildren<SkinnedMeshRenderer>();
            if(skin==null||skin.bounds.size.y<.9f||skin.bounds.size.y>1.9f)throw new Exception("Traveler renderer has incorrect scale");
            if(Object.FindObjectsByType<HeartlightUI>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length!=1)throw new Exception("Expected one Heartlight UI");
            Debug.Log("HEARTLIGHT_STATIC_OK: standalone scene, nodes, Chinese font and traveler validated");
        }
    }
}
