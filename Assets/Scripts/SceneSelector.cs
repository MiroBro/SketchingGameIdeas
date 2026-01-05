using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

public class SceneSelector : MonoBehaviour
{
    private bool showMenu = true;

    void Start()
    {
        // Only show menu in build, or if this is SampleScene
        if (SceneManager.GetActiveScene().name != "SampleScene")
        {
            showMenu = false;
        }
    }

    void OnGUI()
    {
        if (!showMenu) return;

        // Create a centered box for the menu
        float boxWidth = 400;
        float boxHeight = 700;
        float boxX = (Screen.width - boxWidth) / 2;
        float boxY = (Screen.height - boxHeight) / 2;

        GUI.Box(new Rect(boxX, boxY, boxWidth, boxHeight), "");

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 28;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.fontStyle = FontStyle.Bold;

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 18;
        buttonStyle.padding = new RectOffset(20, 20, 10, 10);

        GUIStyle descStyle = new GUIStyle(GUI.skin.label);
        descStyle.fontSize = 12;
        descStyle.alignment = TextAnchor.MiddleCenter;
        descStyle.wordWrap = true;

        // Title
        GUI.Label(new Rect(boxX, boxY + 20, boxWidth, 40), "GAME CONCEPTS", titleStyle);

        float buttonY = boxY + 70;
        float buttonHeight = 50;
        float descHeight = 25;
        float spacing = 10;

        // Game 1
        if (GUI.Button(new Rect(boxX + 20, buttonY, boxWidth - 40, buttonHeight), "Garden Dortmantik", buttonStyle))
        {
            LoadScene("GardenDortmantik");
        }
        GUI.Label(new Rect(boxX + 20, buttonY + buttonHeight, boxWidth - 40, descHeight), "Tile-placement game with garden theme", descStyle);
        buttonY += buttonHeight + descHeight + spacing;

        // Game 2
        if (GUI.Button(new Rect(boxX + 20, buttonY, boxWidth - 40, buttonHeight), "Space Tower Wizard", buttonStyle))
        {
            LoadScene("SpaceTowerWizard");
        }
        GUI.Label(new Rect(boxX + 20, buttonY + buttonHeight, boxWidth - 40, descHeight), "Idle upgrade game in space", descStyle);
        buttonY += buttonHeight + descHeight + spacing;

        // Game 3
        if (GUI.Button(new Rect(boxX + 20, buttonY, boxWidth - 40, buttonHeight), "Underwater Kingdom", buttonStyle))
        {
            LoadScene("UnderwaterKingdom");
        }
        GUI.Label(new Rect(boxX + 20, buttonY + buttonHeight, boxWidth - 40, descHeight), "Kingdom clone with vertical movement", descStyle);
        buttonY += buttonHeight + descHeight + spacing;

        // Game 4
        if (GUI.Button(new Rect(boxX + 20, buttonY, boxWidth - 40, buttonHeight), "Magic Books Digseum", buttonStyle))
        {
            LoadScene("MagicBooksDigseum");
        }
        GUI.Label(new Rect(boxX + 20, buttonY + buttonHeight, boxWidth - 40, descHeight), "Dig for magic books, build your shop", descStyle);
        buttonY += buttonHeight + descHeight + spacing;

        // Game 5
        if (GUI.Button(new Rect(boxX + 20, buttonY, boxWidth - 40, buttonHeight), "Magic Creature Digseum", buttonStyle))
        {
            LoadScene("MagicCreatureDigseum");
        }
        GUI.Label(new Rect(boxX + 20, buttonY + buttonHeight, boxWidth - 40, descHeight), "Dig creatures, breed hybrids, build a zoo", descStyle);
        buttonY += buttonHeight + descHeight + spacing;

        // Game 6
        if (GUI.Button(new Rect(boxX + 20, buttonY, boxWidth - 40, buttonHeight), "Little Life Train", buttonStyle))
        {
            LoadScene("LittleLifeTrain");
        }
        GUI.Label(new Rect(boxX + 20, buttonY + buttonHeight, boxWidth - 40, descHeight), "Pick up passengers, decorate their carts", descStyle);
        buttonY += buttonHeight + descHeight + spacing;

        // Game 7
        if (GUI.Button(new Rect(boxX + 20, buttonY, boxWidth - 40, buttonHeight), "Plant Husbandry", buttonStyle))
        {
            LoadScene("PlantHusbandry");
        }
        GUI.Label(new Rect(boxX + 20, buttonY + buttonHeight, boxWidth - 40, descHeight), "Breed unique plants, sell exotic hybrids", descStyle);
        buttonY += buttonHeight + descHeight + spacing;

        // Advanced Game
        if (GUI.Button(new Rect(boxX + 20, buttonY, boxWidth - 40, buttonHeight), "Advanced Creature Digseum", buttonStyle))
        {
            LoadScene("ACD_Exploration");
        }
        GUI.Label(new Rect(boxX + 20, buttonY + buttonHeight, boxWidth - 40, descHeight), "Multi-scene: Explore, fuse, build zoo (F1-F4 cheats)", descStyle);
    }

    void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    void Update()
    {
        // Press Escape to return to menu from any scene
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            if (SceneManager.GetActiveScene().name != "SampleScene")
            {
                SceneManager.LoadScene("SampleScene");
            }
        }
    }
}
