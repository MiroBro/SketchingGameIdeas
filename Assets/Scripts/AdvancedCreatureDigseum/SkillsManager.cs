using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

namespace AdvancedCreatureDigseum
{
    public class SkillsManager : MonoBehaviour
    {
        private Camera mainCamera;

        // UI
        private TextMeshProUGUI goldText;
        private TextMeshProUGUI statsText;
        private TextMeshProUGUI feedbackText;
        private float feedbackTimer;
        private GameObject skillButtonsParent;
        private TextMeshProUGUI winText;
        private Canvas canvas;

        // Autosave
        private float saveTimer = 0f;
        private float saveInterval = 10f;

        void Start()
        {
            GameData.EnsureLoaded();

            mainCamera = Camera.main;
            mainCamera.backgroundColor = new Color(0.15f, 0.1f, 0.2f);

            CreateUI();
            CreateSkillButtons();

            if (GameData.GameFinished)
            {
                ShowWinScreen();
            }
        }

        void CreateUI()
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();

            // EventSystem required for UI button interaction
            if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            goldText = CreateText(canvasObj.transform, new Vector2(10, -10), "Gold: 0");
            goldText.fontSize = 28;

            statsText = CreateText(canvasObj.transform, new Vector2(10, -60), "");
            statsText.fontSize = 16;
            RectTransform statsRect = statsText.GetComponent<RectTransform>();
            statsRect.sizeDelta = new Vector2(350, 250);

            // Title
            TextMeshProUGUI title = CreateText(canvasObj.transform, new Vector2(0, -20), "SKILLS & UPGRADES");
            RectTransform titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1);
            titleRect.anchorMax = new Vector2(0.5f, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            title.alignment = TextAlignmentOptions.Center;
            title.fontSize = 36;

            // Navigation buttons
            float navY = -10;
            CreateNavButton(canvasObj.transform, new Vector2(-10, navY), "Exploration", "ACD_Exploration");
            CreateNavButton(canvasObj.transform, new Vector2(-10, navY - 40), "Fusion Lab", "ACD_FusionLab");
            CreateNavButton(canvasObj.transform, new Vector2(-10, navY - 80), "Zoo", "ACD_Zoo");
            CreateNavButton(canvasObj.transform, new Vector2(-10, navY - 120), "Prestige", "ACD_Prestige");
            CreateNavButton(canvasObj.transform, new Vector2(-10, navY - 160), "Menu", "SampleScene");

            // Reset button
            GameObject resetBtnObj = new GameObject("ResetBtn");
            resetBtnObj.transform.SetParent(canvasObj.transform);
            RectTransform resetRect = resetBtnObj.AddComponent<RectTransform>();
            resetRect.anchorMin = new Vector2(1, 1);
            resetRect.anchorMax = new Vector2(1, 1);
            resetRect.pivot = new Vector2(1, 1);
            resetRect.anchoredPosition = new Vector2(-10, navY - 210);
            resetRect.sizeDelta = new Vector2(100, 32);
            resetRect.localScale = Vector3.one;
            Image resetImg = resetBtnObj.AddComponent<Image>();
            resetImg.color = new Color(0.5f, 0.2f, 0.2f);
            Button resetBtn = resetBtnObj.AddComponent<Button>();
            resetBtn.onClick.AddListener(() => {
                GameData.Reset();
                ShowFeedback("Save data reset!", Color.yellow);
                SceneManager.LoadScene("ACD_Exploration");
            });
            GameObject resetTextObj = new GameObject("Text");
            resetTextObj.transform.SetParent(resetBtnObj.transform);
            RectTransform resetTextRect = resetTextObj.AddComponent<RectTransform>();
            resetTextRect.anchorMin = Vector2.zero;
            resetTextRect.anchorMax = Vector2.one;
            resetTextRect.offsetMin = Vector2.zero;
            resetTextRect.offsetMax = Vector2.zero;
            resetTextRect.localScale = Vector3.one;
            TextMeshProUGUI resetTmp = resetTextObj.AddComponent<TextMeshProUGUI>();
            resetTmp.text = "Reset Save";
            resetTmp.fontSize = 12;
            resetTmp.color = Color.white;
            resetTmp.alignment = TextAlignmentOptions.Center;
            resetTmp.raycastTarget = false;

            // Feedback
            feedbackText = CreateText(canvasObj.transform, Vector2.zero, "");
            RectTransform fbRect = feedbackText.GetComponent<RectTransform>();
            fbRect.anchorMin = new Vector2(0.5f, 0);
            fbRect.anchorMax = new Vector2(0.5f, 0);
            fbRect.pivot = new Vector2(0.5f, 0);
            fbRect.anchoredPosition = new Vector2(0, 50);
            fbRect.sizeDelta = new Vector2(600, 60);
            feedbackText.alignment = TextAlignmentOptions.Center;
            feedbackText.fontSize = 24;

            // Win text
            winText = CreateText(canvasObj.transform, Vector2.zero, "");
            RectTransform winRect = winText.GetComponent<RectTransform>();
            winRect.anchorMin = new Vector2(0.5f, 0.5f);
            winRect.anchorMax = new Vector2(0.5f, 0.5f);
            winRect.pivot = new Vector2(0.5f, 0.5f);
            winRect.sizeDelta = new Vector2(800, 400);
            winText.alignment = TextAlignmentOptions.Center;
            winText.fontSize = 32;
            winText.gameObject.SetActive(false);

            // Create skill buttons parent with proper RectTransform
            skillButtonsParent = new GameObject("SkillButtons");
            skillButtonsParent.transform.SetParent(canvasObj.transform);
            RectTransform skillsRect = skillButtonsParent.AddComponent<RectTransform>();
            skillsRect.anchorMin = Vector2.zero;
            skillsRect.anchorMax = Vector2.one;
            skillsRect.offsetMin = Vector2.zero;
            skillsRect.offsetMax = Vector2.zero;
        }

        TextMeshProUGUI CreateText(Transform parent, Vector2 position, string text)
        {
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(parent);

            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(300, 40);
            rect.localScale = Vector3.one;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 20;
            tmp.color = Color.white;

            return tmp;
        }

        void CreateNavButton(Transform parent, Vector2 position, string label, string sceneName)
        {
            GameObject btnObj = new GameObject($"NavBtn_{label}");
            btnObj.transform.SetParent(parent);

            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(1, 1);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(100, 32);
            rect.localScale = Vector3.one;

            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.3f, 0.25f, 0.4f);

            Button btn = btnObj.AddComponent<Button>();
            string scene = sceneName;
            btn.onClick.AddListener(() => SceneManager.LoadScene(scene));

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            textRect.localScale = Vector3.one;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 14;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
        }

        void CreateSkillButtons()
        {
            float startX = 380;
            float startY = -80;
            int row = 0;
            int col = 0;
            int buttonsPerRow = 3;

            // Create tiered upgrade buttons (consolidated)
            var tieredCategories = SkillDatabase.GetTieredCategories();
            foreach (var category in tieredCategories)
            {
                CreateTieredSkillButton(category, startX + col * 320, startY - row * 95);

                col++;
                if (col >= buttonsPerRow)
                {
                    col = 0;
                    row++;
                }
            }

            // Create non-tiered skill buttons (decorations, pastures, finish)
            var nonTieredSkills = SkillDatabase.GetNonTieredSkills();
            foreach (var skill in nonTieredSkills)
            {
                CreateSkillButton(skill, startX + col * 320, startY - row * 95);

                col++;
                if (col >= buttonsPerRow)
                {
                    col = 0;
                    row++;
                }
            }
        }

        void CreateTieredSkillButton(string category, float x, float y)
        {
            var nextSkill = SkillDatabase.GetNextSkillInCategory(category);
            int currentLevel = SkillDatabase.GetCategoryLevel(category);
            int maxLevel = SkillDatabase.GetCategoryMaxLevel(category);

            // Get display name from the first skill in category
            string displayName = category.Replace("_", " ");
            displayName = char.ToUpper(displayName[0]) + displayName.Substring(1);
            foreach (var skill in SkillDatabase.Skills)
            {
                if (skill.Category == category)
                {
                    displayName = skill.Name;
                    break;
                }
            }

            GameObject btnObj = new GameObject($"TieredSkill_{category}");
            btnObj.transform.SetParent(skillButtonsParent.transform);

            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(300, 85);
            rect.localScale = Vector3.one;

            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.25f, 0.2f, 0.35f);

            Button btn = btnObj.AddComponent<Button>();
            string cat = category;
            btn.onClick.AddListener(() => TryPurchaseTieredSkill(cat));

            // Name with level progress
            GameObject nameObj = new GameObject("Name");
            nameObj.transform.SetParent(btnObj.transform);
            RectTransform nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0.5f);
            nameRect.anchorMax = new Vector2(1, 1);
            nameRect.offsetMin = new Vector2(10, 0);
            nameRect.offsetMax = new Vector2(-10, -5);
            nameRect.localScale = Vector3.one;

            TextMeshProUGUI nameTmp = nameObj.AddComponent<TextMeshProUGUI>();
            nameTmp.text = $"{displayName} ({currentLevel}/{maxLevel})";
            nameTmp.fontSize = 16;
            nameTmp.fontStyle = FontStyles.Bold;
            nameTmp.color = Color.white;
            nameTmp.raycastTarget = false;

            // Description
            GameObject descObj = new GameObject("Desc");
            descObj.transform.SetParent(btnObj.transform);
            RectTransform descRect = descObj.AddComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0, 0);
            descRect.anchorMax = new Vector2(1, 0.5f);
            descRect.offsetMin = new Vector2(10, 5);
            descRect.offsetMax = new Vector2(-80, 0);
            descRect.localScale = Vector3.one;

            TextMeshProUGUI descTmp = descObj.AddComponent<TextMeshProUGUI>();
            descTmp.text = nextSkill != null ? nextSkill.Description : "Max level reached!";
            descTmp.fontSize = 12;
            descTmp.color = new Color(0.8f, 0.8f, 0.8f);
            descTmp.raycastTarget = false;

            // Cost
            GameObject costObj = new GameObject("Cost");
            costObj.transform.SetParent(btnObj.transform);
            RectTransform costRect = costObj.AddComponent<RectTransform>();
            costRect.anchorMin = new Vector2(1, 0);
            costRect.anchorMax = new Vector2(1, 0.5f);
            costRect.pivot = new Vector2(1, 0);
            costRect.anchoredPosition = new Vector2(-10, 5);
            costRect.sizeDelta = new Vector2(70, 30);
            costRect.localScale = Vector3.one;

            TextMeshProUGUI costTmp = costObj.AddComponent<TextMeshProUGUI>();
            costTmp.text = nextSkill != null ? $"{nextSkill.Cost}g" : "MAX";
            costTmp.fontSize = 14;
            costTmp.alignment = TextAlignmentOptions.Right;
            costTmp.color = new Color(1f, 0.9f, 0.5f);
            costTmp.raycastTarget = false;

            // Store reference for updates
            TieredSkillButtonRef btnRef = btnObj.AddComponent<TieredSkillButtonRef>();
            btnRef.Category = category;
            btnRef.ButtonImage = img;
            btnRef.NameText = nameTmp;
            btnRef.DescText = descTmp;
            btnRef.CostText = costTmp;
        }

        void CreateSkillButton(SkillUpgrade skill, float x, float y)
        {
            GameObject btnObj = new GameObject($"Skill_{skill.Id}");
            btnObj.transform.SetParent(skillButtonsParent.transform);

            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(300, 85);
            rect.localScale = Vector3.one;

            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.25f, 0.2f, 0.35f);

            Button btn = btnObj.AddComponent<Button>();
            string skillId = skill.Id;
            btn.onClick.AddListener(() => TryPurchaseSkill(skillId));

            // Name
            GameObject nameObj = new GameObject("Name");
            nameObj.transform.SetParent(btnObj.transform);
            RectTransform nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0.5f);
            nameRect.anchorMax = new Vector2(1, 1);
            nameRect.offsetMin = new Vector2(10, 0);
            nameRect.offsetMax = new Vector2(-10, -5);
            nameRect.localScale = Vector3.one;

            TextMeshProUGUI nameTmp = nameObj.AddComponent<TextMeshProUGUI>();
            nameTmp.text = skill.Name;
            nameTmp.fontSize = 16;
            nameTmp.fontStyle = FontStyles.Bold;
            nameTmp.color = Color.white;
            nameTmp.raycastTarget = false; // Don't block button clicks

            // Description
            GameObject descObj = new GameObject("Desc");
            descObj.transform.SetParent(btnObj.transform);
            RectTransform descRect = descObj.AddComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0, 0);
            descRect.anchorMax = new Vector2(1, 0.5f);
            descRect.offsetMin = new Vector2(10, 5);
            descRect.offsetMax = new Vector2(-80, 0);
            descRect.localScale = Vector3.one;

            TextMeshProUGUI descTmp = descObj.AddComponent<TextMeshProUGUI>();
            descTmp.text = skill.Description;
            descTmp.fontSize = 12;
            descTmp.color = new Color(0.8f, 0.8f, 0.8f);
            descTmp.raycastTarget = false; // Don't block button clicks

            // Cost
            GameObject costObj = new GameObject("Cost");
            costObj.transform.SetParent(btnObj.transform);
            RectTransform costRect = costObj.AddComponent<RectTransform>();
            costRect.anchorMin = new Vector2(1, 0);
            costRect.anchorMax = new Vector2(1, 0.5f);
            costRect.pivot = new Vector2(1, 0);
            costRect.anchoredPosition = new Vector2(-10, 5);
            costRect.sizeDelta = new Vector2(70, 30);
            costRect.localScale = Vector3.one;

            TextMeshProUGUI costTmp = costObj.AddComponent<TextMeshProUGUI>();
            costTmp.text = $"{skill.Cost}g";
            costTmp.fontSize = 14;
            costTmp.alignment = TextAlignmentOptions.Right;
            costTmp.color = new Color(1f, 0.9f, 0.5f);
            costTmp.raycastTarget = false; // Don't block button clicks

            // Store reference for updates
            SkillButtonRef btnRef = btnObj.AddComponent<SkillButtonRef>();
            btnRef.SkillId = skill.Id;
            btnRef.ButtonImage = img;
            btnRef.NameText = nameTmp;
            btnRef.CostText = costTmp;
        }

        void TryPurchaseTieredSkill(string category)
        {
            var nextSkill = SkillDatabase.GetNextSkillInCategory(category);
            if (nextSkill == null)
            {
                ShowFeedback("Already at max level!", Color.yellow);
                return;
            }

            if (GameData.Gold < nextSkill.Cost)
            {
                ShowFeedback($"Need {nextSkill.Cost} gold!", Color.red);
                return;
            }

            if (SkillDatabase.PurchaseSkill(nextSkill.Id))
            {
                int newLevel = SkillDatabase.GetCategoryLevel(category);
                int maxLevel = SkillDatabase.GetCategoryMaxLevel(category);
                ShowFeedback($"Upgraded {nextSkill.Name} to level {newLevel}/{maxLevel}!", Color.green);
                UpdateButtonStates();
                GameData.SaveGame();
            }
        }

        void TryPurchaseSkill(string skillId)
        {
            var skill = SkillDatabase.Skills.Find(s => s.Id == skillId);
            if (skill == null) return;

            if (SkillDatabase.IsSkillPurchased(skillId))
            {
                ShowFeedback("Already purchased!", Color.yellow);
                return;
            }

            if (skill.Type == SkillType.FinishGame)
            {
                // Check if all animals found
                int uniqueAnimals = GameData.FoundAnimals.Count;
                if (uniqueAnimals < 30)
                {
                    ShowFeedback($"Find all 30 animals first! ({uniqueAnimals}/30)", Color.red);
                    return;
                }
            }

            if (GameData.Gold < skill.Cost)
            {
                ShowFeedback($"Need {skill.Cost} gold!", Color.red);
                return;
            }

            if (SkillDatabase.PurchaseSkill(skillId))
            {
                ShowFeedback($"Purchased {skill.Name}!", Color.green);
                UpdateButtonStates();
                GameData.SaveGame();

                if (skill.Type == SkillType.FinishGame)
                {
                    ShowWinScreen();
                }
            }
        }

        void UpdateButtonStates()
        {
            // Update tiered skill buttons
            foreach (var btnRef in FindObjectsByType<TieredSkillButtonRef>(FindObjectsSortMode.None))
            {
                int currentLevel = SkillDatabase.GetCategoryLevel(btnRef.Category);
                int maxLevel = SkillDatabase.GetCategoryMaxLevel(btnRef.Category);
                var nextSkill = SkillDatabase.GetNextSkillInCategory(btnRef.Category);

                // Get display name
                string displayName = btnRef.Category;
                foreach (var skill in SkillDatabase.Skills)
                {
                    if (skill.Category == btnRef.Category)
                    {
                        displayName = skill.Name;
                        break;
                    }
                }

                btnRef.NameText.text = $"{displayName} ({currentLevel}/{maxLevel})";

                if (nextSkill == null)
                {
                    // Max level reached
                    btnRef.ButtonImage.color = new Color(0.2f, 0.4f, 0.2f);
                    btnRef.CostText.text = "MAX";
                    btnRef.CostText.color = Color.green;
                    btnRef.DescText.text = "Max level reached!";
                }
                else
                {
                    btnRef.DescText.text = nextSkill.Description;
                    btnRef.CostText.text = $"{nextSkill.Cost}g";

                    if (GameData.Gold >= nextSkill.Cost)
                    {
                        btnRef.ButtonImage.color = new Color(0.25f, 0.2f, 0.35f);
                        btnRef.CostText.color = new Color(1f, 0.9f, 0.5f);
                    }
                    else
                    {
                        btnRef.ButtonImage.color = new Color(0.3f, 0.2f, 0.2f);
                        btnRef.CostText.color = Color.red;
                    }
                }
            }

            // Update regular skill buttons
            foreach (var btnRef in FindObjectsByType<SkillButtonRef>(FindObjectsSortMode.None))
            {
                bool purchased = SkillDatabase.IsSkillPurchased(btnRef.SkillId);
                var skill = SkillDatabase.Skills.Find(s => s.Id == btnRef.SkillId);
                bool canAfford = skill != null && GameData.Gold >= skill.Cost;

                if (purchased)
                {
                    btnRef.ButtonImage.color = new Color(0.2f, 0.4f, 0.2f);
                    btnRef.CostText.text = "OWNED";
                    btnRef.CostText.color = Color.green;
                }
                else if (!canAfford)
                {
                    btnRef.ButtonImage.color = new Color(0.3f, 0.2f, 0.2f);
                    btnRef.CostText.color = Color.red;
                }
                else
                {
                    btnRef.ButtonImage.color = new Color(0.25f, 0.2f, 0.35f);
                    btnRef.CostText.color = new Color(1f, 0.9f, 0.5f);
                }
            }
        }

        void ShowWinScreen()
        {
            winText.gameObject.SetActive(true);
            winText.text = "CONGRATULATIONS!\n\n" +
                          "You have completed the Magical Creature Zoo!\n\n" +
                          "Animals Found: 30/30\n" +
                          $"Hybrids Created: {GameData.Hybrids.Count}\n" +
                          $"Final Gold: {GameData.Gold}\n\n" +
                          "Thank you for playing!";
            winText.color = new Color(1f, 0.9f, 0.5f);
        }

        void Update()
        {
            HandleInput();
            UpdateUI();
            UpdateFeedback();
            UpdateButtonStates();

            // Global idle income (zoo runs even when not viewing it)
            GameData.UpdateGlobalIdleIncome();

            // Autosave
            saveTimer += Time.deltaTime;
            if (saveTimer >= saveInterval)
            {
                saveTimer = 0f;
                GameData.SaveGame();
            }
        }

        void HandleInput()
        {
            var keyboard = Keyboard.current;

            if (keyboard != null)
            {
                if (keyboard.digit1Key.wasPressedThisFrame)
                    SceneManager.LoadScene("ACD_Exploration");
                if (keyboard.tabKey.wasPressedThisFrame)
                    SceneManager.LoadScene("ACD_FusionLab");
                if (keyboard.zKey.wasPressedThisFrame)
                    SceneManager.LoadScene("ACD_Zoo");
                if (keyboard.escapeKey.wasPressedThisFrame)
                    SceneManager.LoadScene("SampleScene");

                // Reset save
                if (keyboard.f5Key.wasPressedThisFrame)
                {
                    GameData.Reset();
                    ShowFeedback("Save data reset!", Color.yellow);
                    SceneManager.LoadScene("ACD_Exploration");
                }
            }
        }

        void ShowFeedback(string message, Color color)
        {
            if (feedbackText != null)
            {
                feedbackText.text = message;
                feedbackText.color = color;
                feedbackTimer = 2f;
            }
        }

        void UpdateFeedback()
        {
            if (feedbackTimer > 0)
            {
                feedbackTimer -= Time.deltaTime;
                if (feedbackTimer <= 0 && feedbackText != null)
                    feedbackText.text = "";
            }
        }

        void UpdateUI()
        {
            if (goldText != null)
                goldText.text = $"Gold: {GameData.Gold}";

            if (statsText != null)
            {
                string stats = "CURRENT STATS:\n\n";
                stats += $"Dig Power: {GameData.DigPower}\n";
                stats += $"Dig Radius: {GameData.DigRadius}\n";
                stats += $"Max Energy: {GameData.MaxEnergy}\n";
                stats += $"Energy Regen: {GameData.EnergyRegen}/s\n\n";
                stats += $"Animals Found: {GameData.FoundAnimals.Count}/30 unique\n";
                stats += $"Hybrids: {GameData.Hybrids.Count}\n";
                stats += $"Biomes Unlocked: {GameData.UnlockedBiomes.Count}/10";
                statsText.text = stats;
            }
        }
    }

    public class SkillButtonRef : MonoBehaviour
    {
        public string SkillId;
        public Image ButtonImage;
        public TextMeshProUGUI NameText;
        public TextMeshProUGUI CostText;
    }

    public class TieredSkillButtonRef : MonoBehaviour
    {
        public string Category;
        public Image ButtonImage;
        public TextMeshProUGUI NameText;
        public TextMeshProUGUI DescText;
        public TextMeshProUGUI CostText;
    }
}
