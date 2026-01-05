using UnityEditor;
using System.Collections.Generic;

public class BuildSettingsSetup
{
    [MenuItem("Games/Add All Scenes to Build Settings")]
    static void AddScenesToBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();

        // Add scenes in order
        string[] scenePaths = {
            "Assets/Scenes/SampleScene.unity",
            "Assets/Scenes/GardenDortmantik.unity",
            "Assets/Scenes/SpaceTowerWizard.unity",
            "Assets/Scenes/UnderwaterKingdom.unity",
            "Assets/Scenes/MagicBooksDigseum.unity",
            "Assets/Scenes/MagicCreatureDigseum.unity",
            "Assets/Scenes/LittleLifeTrain.unity",
            "Assets/Scenes/PlantHusbandry.unity",
            "Assets/Scenes/ACD_Exploration.unity",
            "Assets/Scenes/ACD_FusionLab.unity",
            "Assets/Scenes/ACD_Zoo.unity",
            "Assets/Scenes/ACD_Skills.unity"
        };

        foreach (string path in scenePaths)
        {
            scenes.Add(new EditorBuildSettingsScene(path, true));
        }

        EditorBuildSettings.scenes = scenes.ToArray();
        UnityEngine.Debug.Log("Build settings updated with all game scenes!");
    }
}
