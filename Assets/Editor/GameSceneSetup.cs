using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public class GameSceneSetup
{
    static GameSceneSetup()
    {
        EditorSceneManager.sceneOpened += OnSceneOpened;
    }

    static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        SetupSceneIfNeeded(scene);
    }

    [MenuItem("Games/Setup Current Scene")]
    static void SetupCurrentScene()
    {
        SetupSceneIfNeeded(SceneManager.GetActiveScene());
    }

    [MenuItem("Games/Setup All Game Scenes")]
    static void SetupAllScenes()
    {
        string[] sceneNames = { "GardenDortmantik", "SpaceTowerWizard", "UnderwaterKingdom", "MagicBooksDigseum", "MagicCreatureDigseum", "LittleLifeTrain", "PlantHusbandry", "ACD_Exploration", "ACD_FusionLab", "ACD_Zoo", "ACD_Skills" };

        foreach (string sceneName in sceneNames)
        {
            string scenePath = $"Assets/Scenes/{sceneName}.unity";
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            SetupSceneIfNeeded(scene);
            EditorSceneManager.SaveScene(scene);
        }

        // Return to SampleScene
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
        Debug.Log("All game scenes have been set up!");
    }

    static void SetupSceneIfNeeded(Scene scene)
    {
        switch (scene.name)
        {
            case "GardenDortmantik":
                SetupGardenDortmantik();
                break;
            case "SpaceTowerWizard":
                SetupSpaceTowerWizard();
                break;
            case "UnderwaterKingdom":
                SetupUnderwaterKingdom();
                break;
            case "MagicBooksDigseum":
                SetupMagicBooksDigseum();
                break;
            case "MagicCreatureDigseum":
                SetupMagicCreatureDigseum();
                break;
            case "LittleLifeTrain":
                SetupLittleLifeTrain();
                break;
            case "PlantHusbandry":
                SetupPlantHusbandry();
                break;
            case "ACD_Exploration":
                SetupACDExploration();
                break;
            case "ACD_FusionLab":
                SetupACDFusionLab();
                break;
            case "ACD_Zoo":
                SetupACDZoo();
                break;
            case "ACD_Skills":
                SetupACDSkills();
                break;
            case "SampleScene":
                SetupSampleScene();
                break;
        }
    }

    static void SetupGardenDortmantik()
    {
        // Find or create GameManager
        var manager = Object.FindAnyObjectByType<GardenDortmantik.GardenGameManager>();
        if (manager == null)
        {
            GameObject managerObj = new GameObject("GardenGameManager");
            manager = managerObj.AddComponent<GardenDortmantik.GardenGameManager>();
            EditorUtility.SetDirty(managerObj);
            Debug.Log("Garden Dortmantik: Added GardenGameManager");
        }
    }

    static void SetupSpaceTowerWizard()
    {
        var manager = Object.FindAnyObjectByType<SpaceTowerWizard.SpaceTowerGameManager>();
        if (manager == null)
        {
            GameObject managerObj = new GameObject("SpaceTowerGameManager");
            manager = managerObj.AddComponent<SpaceTowerWizard.SpaceTowerGameManager>();
            EditorUtility.SetDirty(managerObj);
            Debug.Log("Space Tower Wizard: Added SpaceTowerGameManager");
        }
    }

    static void SetupUnderwaterKingdom()
    {
        var manager = Object.FindAnyObjectByType<UnderwaterKingdom.UnderwaterKingdomManager>();
        if (manager == null)
        {
            GameObject managerObj = new GameObject("UnderwaterKingdomManager");
            manager = managerObj.AddComponent<UnderwaterKingdom.UnderwaterKingdomManager>();
            EditorUtility.SetDirty(managerObj);
            Debug.Log("Underwater Kingdom: Added UnderwaterKingdomManager");
        }
    }

    static void SetupMagicBooksDigseum()
    {
        var manager = Object.FindAnyObjectByType<MagicBooksDigseum.DigsemGameManager>();
        if (manager == null)
        {
            GameObject managerObj = new GameObject("DigsemGameManager");
            manager = managerObj.AddComponent<MagicBooksDigseum.DigsemGameManager>();
            EditorUtility.SetDirty(managerObj);
            Debug.Log("Magic Books Digseum: Added DigsemGameManager");
        }
    }

    static void SetupMagicCreatureDigseum()
    {
        var manager = Object.FindAnyObjectByType<MagicCreatureDigseum.MagicCreatureGameManager>();
        if (manager == null)
        {
            GameObject managerObj = new GameObject("MagicCreatureGameManager");
            manager = managerObj.AddComponent<MagicCreatureDigseum.MagicCreatureGameManager>();
            EditorUtility.SetDirty(managerObj);
            Debug.Log("Magic Creature Digseum: Added MagicCreatureGameManager");
        }
    }

    static void SetupLittleLifeTrain()
    {
        var manager = Object.FindAnyObjectByType<LittleLifeTrain.LittleLifeTrainManager>();
        if (manager == null)
        {
            GameObject managerObj = new GameObject("LittleLifeTrainManager");
            manager = managerObj.AddComponent<LittleLifeTrain.LittleLifeTrainManager>();
            EditorUtility.SetDirty(managerObj);
            Debug.Log("Little Life Train: Added LittleLifeTrainManager");
        }
    }

    static void SetupPlantHusbandry()
    {
        var manager = Object.FindAnyObjectByType<PlantHusbandry.PlantHusbandryManager>();
        if (manager == null)
        {
            GameObject managerObj = new GameObject("PlantHusbandryManager");
            manager = managerObj.AddComponent<PlantHusbandry.PlantHusbandryManager>();
            EditorUtility.SetDirty(managerObj);
            Debug.Log("Plant Husbandry: Added PlantHusbandryManager");
        }
    }

    static void SetupACDExploration()
    {
        var manager = Object.FindAnyObjectByType<AdvancedCreatureDigseum.ExplorationManager>();
        if (manager == null)
        {
            GameObject managerObj = new GameObject("ExplorationManager");
            manager = managerObj.AddComponent<AdvancedCreatureDigseum.ExplorationManager>();
            EditorUtility.SetDirty(managerObj);
            Debug.Log("ACD Exploration: Added ExplorationManager");
        }
    }

    static void SetupACDFusionLab()
    {
        var manager = Object.FindAnyObjectByType<AdvancedCreatureDigseum.FusionLabManager>();
        if (manager == null)
        {
            GameObject managerObj = new GameObject("FusionLabManager");
            manager = managerObj.AddComponent<AdvancedCreatureDigseum.FusionLabManager>();
            EditorUtility.SetDirty(managerObj);
            Debug.Log("ACD Fusion Lab: Added FusionLabManager");
        }
    }

    static void SetupACDZoo()
    {
        var manager = Object.FindAnyObjectByType<AdvancedCreatureDigseum.ZooManager>();
        if (manager == null)
        {
            GameObject managerObj = new GameObject("ZooManager");
            manager = managerObj.AddComponent<AdvancedCreatureDigseum.ZooManager>();
            EditorUtility.SetDirty(managerObj);
            Debug.Log("ACD Zoo: Added ZooManager");
        }
    }

    static void SetupACDSkills()
    {
        var manager = Object.FindAnyObjectByType<AdvancedCreatureDigseum.SkillsManager>();
        if (manager == null)
        {
            GameObject managerObj = new GameObject("SkillsManager");
            manager = managerObj.AddComponent<AdvancedCreatureDigseum.SkillsManager>();
            EditorUtility.SetDirty(managerObj);
            Debug.Log("ACD Skills: Added SkillsManager");
        }
    }

    static void SetupSampleScene()
    {
        var selector = Object.FindAnyObjectByType<SceneSelector>();
        if (selector == null)
        {
            GameObject selectorObj = new GameObject("SceneSelector");
            selector = selectorObj.AddComponent<SceneSelector>();
            EditorUtility.SetDirty(selectorObj);
            Debug.Log("SampleScene: Added SceneSelector");
        }
    }
}
