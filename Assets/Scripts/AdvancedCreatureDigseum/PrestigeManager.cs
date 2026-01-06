using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

namespace AdvancedCreatureDigseum
{
    public class PrestigeManager : MonoBehaviour
    {
        private Camera mainCamera;
        private TextMeshProUGUI pointsText;
        private TextMeshProUGUI prestigeCountText;
        private TextMeshProUGUI feedbackText;
        private float feedbackTimer;

        private List<GameObject> upgradeButtons = new List<GameObject>();
        private Transform scrollContent;

        void Start()
        {
            GameData.EnsureLoaded();
            PrestigeDatabase.RecalculateBonuses();

            mainCamera = Camera.main;
            mainCamera.backgroundColor = new Color(0.15f, 0.1f, 0.2f);

            CreateUI();
        }

        void Update()
        {
            UpdateUI();
            HandleInput();
            UpdateFeedback();
        }

        void CreateUI()
        {
            // Canvas setup
            GameObject canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();

            // Event system
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            // Title
            TextMeshProUGUI title = CreateText(canvasObj.transform, new Vector2(0, -30), "PRESTIGE LAB");
            title.fontSize = 36;
            title.alignment = TextAlignmentOptions.Center;
            title.color = new Color(0.8f, 0.6f, 1f);
            RectTransform titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);

            // Prestige Points display
            pointsText = CreateText(canvasObj.transform, new Vector2(0, -80), "Prestige Crystals: 0");
            pointsText.fontSize = 24;
            pointsText.alignment = TextAlignmentOptions.Center;
            pointsText.color = new Color(0.6f, 0.8f, 1f);
            RectTransform pointsRect = pointsText.GetComponent<RectTransform>();
            pointsRect.anchorMin = new Vector2(0.5f, 1f);
            pointsRect.anchorMax = new Vector2(0.5f, 1f);
            pointsRect.pivot = new Vector2(0.5f, 1f);
            pointsRect.sizeDelta = new Vector2(400, 30);

            // Prestige count display
            prestigeCountText = CreateText(canvasObj.transform, new Vector2(0, -110), "Prestige Count: 0");
            prestigeCountText.fontSize = 18;
            prestigeCountText.alignment = TextAlignmentOptions.Center;
            prestigeCountText.color = new Color(0.7f, 0.7f, 0.8f);
            RectTransform countRect = prestigeCountText.GetComponent<RectTransform>();
            countRect.anchorMin = new Vector2(0.5f, 1f);
            countRect.anchorMax = new Vector2(0.5f, 1f);
            countRect.pivot = new Vector2(0.5f, 1f);
            countRect.sizeDelta = new Vector2(400, 25);

            // Feedback text
            feedbackText = CreateText(canvasObj.transform, new Vector2(0, -140), "");
            feedbackText.fontSize = 18;
            feedbackText.alignment = TextAlignmentOptions.Center;
            RectTransform feedRect = feedbackText.GetComponent<RectTransform>();
            feedRect.anchorMin = new Vector2(0.5f, 1f);
            feedRect.anchorMax = new Vector2(0.5f, 1f);
            feedRect.pivot = new Vector2(0.5f, 1f);
            feedRect.sizeDelta = new Vector2(600, 30);

            // Navigation buttons (right side)
            float navY = -50;
            CreateNavButton(canvasObj.transform, new Vector2(-10, navY), "Exploration", "ACD_Exploration");
            CreateNavButton(canvasObj.transform, new Vector2(-10, navY - 40), "Zoo", "ACD_Zoo");
            CreateNavButton(canvasObj.transform, new Vector2(-10, navY - 80), "Fusion", "ACD_FusionLab");
            CreateNavButton(canvasObj.transform, new Vector2(-10, navY - 120), "Skills", "ACD_Skills");
            CreateNavButton(canvasObj.transform, new Vector2(-10, navY - 160), "Menu", "SampleScene");

            // Prestige button (left side, prominent)
            CreatePrestigeButton(canvasObj.transform);

            // Create scroll view for upgrades
            CreateUpgradeScrollView(canvasObj.transform);

            // Populate upgrades
            PopulateUpgrades();
        }

        void CreatePrestigeButton(Transform parent)
        {
            GameObject btnObj = new GameObject("PrestigeButton");
            btnObj.transform.SetParent(parent);

            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(20, -50);
            rect.sizeDelta = new Vector2(180, 80);
            rect.localScale = Vector3.one;

            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.6f, 0.3f, 0.7f);

            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(OnPrestigeClicked);

            // Button text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            textRect.localScale = Vector3.one;

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "PRESTIGE\n<size=14>Reset Progress</size>";
            text.fontSize = 20;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;

            // Info text below button
            TextMeshProUGUI info = CreateText(parent, new Vector2(20, -140), "Resets: Biomes, Skills\nKeeps: Zoo, Animals, Hybrids");
            info.fontSize = 12;
            info.color = new Color(0.7f, 0.7f, 0.7f);
            RectTransform infoRect = info.GetComponent<RectTransform>();
            infoRect.anchorMin = new Vector2(0, 1);
            infoRect.anchorMax = new Vector2(0, 1);
            infoRect.pivot = new Vector2(0, 1);
            infoRect.sizeDelta = new Vector2(180, 50);
        }

        void CreateUpgradeScrollView(Transform parent)
        {
            // Scroll view container
            GameObject scrollObj = new GameObject("ScrollView");
            scrollObj.transform.SetParent(parent);

            RectTransform scrollRect = scrollObj.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.15f, 0.05f);
            scrollRect.anchorMax = new Vector2(0.85f, 0.75f);
            scrollRect.offsetMin = Vector2.zero;
            scrollRect.offsetMax = Vector2.zero;
            scrollRect.localScale = Vector3.one;

            Image scrollBg = scrollObj.AddComponent<Image>();
            scrollBg.color = new Color(0.1f, 0.08f, 0.15f, 0.9f);

            ScrollRect scroll = scrollObj.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 30f;

            // Viewport
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollObj.transform);
            RectTransform viewRect = viewport.AddComponent<RectTransform>();
            viewRect.anchorMin = Vector2.zero;
            viewRect.anchorMax = Vector2.one;
            viewRect.offsetMin = new Vector2(10, 10);
            viewRect.offsetMax = new Vector2(-10, -10);
            viewRect.localScale = Vector3.one;

            viewport.AddComponent<Image>().color = Color.clear;
            viewport.AddComponent<Mask>().showMaskGraphic = false;

            scroll.viewport = viewRect;

            // Content
            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform);
            scrollContent = content.transform;

            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0, 0);
            contentRect.localScale = Vector3.one;

            VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10;
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = contentRect;
        }

        void PopulateUpgrades()
        {
            // Clear existing
            foreach (var btn in upgradeButtons)
            {
                if (btn != null) Destroy(btn);
            }
            upgradeButtons.Clear();

            // Group upgrades by category
            string[] categories = { "Income", "Dig", "Biome", "Currency", "Gold", "Energy", "Decorations", "Pastures" };
            Dictionary<string, List<PrestigeUpgrade>> grouped = new Dictionary<string, List<PrestigeUpgrade>>();

            foreach (var cat in categories)
            {
                grouped[cat] = new List<PrestigeUpgrade>();
            }

            foreach (var upgrade in PrestigeDatabase.Upgrades)
            {
                string cat = "Income";
                if (upgrade.Id.StartsWith("income")) cat = "Income";
                else if (upgrade.Id.StartsWith("dig")) cat = "Dig";
                else if (upgrade.Id.StartsWith("biome")) cat = "Biome";
                else if (upgrade.Id.StartsWith("currency")) cat = "Currency";
                else if (upgrade.Id.StartsWith("gold")) cat = "Gold";
                else if (upgrade.Id.StartsWith("energy")) cat = "Energy";
                else if (upgrade.Id.Contains("decoration") || upgrade.Id.Contains("bench") || upgrade.Id.Contains("lamp") || upgrade.Id.Contains("tree") || upgrade.Id.Contains("flowers") || upgrade.Id.Contains("fountain") || upgrade.Id.Contains("statue")) cat = "Decorations";
                else if (upgrade.Id.Contains("pasture")) cat = "Pastures";

                grouped[cat].Add(upgrade);
            }

            // Create category sections
            foreach (var cat in categories)
            {
                if (grouped[cat].Count == 0) continue;

                // Category header
                CreateCategoryHeader(cat);

                // Upgrades in this category
                foreach (var upgrade in grouped[cat])
                {
                    CreateUpgradeButton(upgrade);
                }
            }
        }

        void CreateCategoryHeader(string category)
        {
            GameObject headerObj = new GameObject($"Header_{category}");
            headerObj.transform.SetParent(scrollContent);

            RectTransform rect = headerObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 30);
            rect.localScale = Vector3.one;

            TextMeshProUGUI text = headerObj.AddComponent<TextMeshProUGUI>();
            text.text = $"=== {category.ToUpper()} ===";
            text.fontSize = 18;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.8f, 0.7f, 1f);

            upgradeButtons.Add(headerObj);
        }

        void CreateUpgradeButton(PrestigeUpgrade upgrade)
        {
            GameObject btnObj = new GameObject($"Upgrade_{upgrade.Id}");
            btnObj.transform.SetParent(scrollContent);

            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 70);
            rect.localScale = Vector3.one;

            Image img = btnObj.AddComponent<Image>();

            bool purchased = PrestigeDatabase.IsUpgradePurchased(upgrade.Id);
            bool canBuy = PrestigeDatabase.CanPurchaseUpgrade(upgrade.Id);

            if (purchased)
            {
                img.color = new Color(0.2f, 0.4f, 0.2f); // Green - purchased
            }
            else if (canBuy)
            {
                img.color = new Color(0.3f, 0.25f, 0.4f); // Purple - available
            }
            else
            {
                img.color = new Color(0.2f, 0.2f, 0.2f); // Gray - locked
            }

            Button btn = btnObj.AddComponent<Button>();
            string upgradeId = upgrade.Id;
            btn.onClick.AddListener(() => OnUpgradeClicked(upgradeId));

            // Name text
            GameObject nameObj = new GameObject("Name");
            nameObj.transform.SetParent(btnObj.transform);
            RectTransform nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0.5f);
            nameRect.anchorMax = new Vector2(0.7f, 1);
            nameRect.offsetMin = new Vector2(10, 5);
            nameRect.offsetMax = new Vector2(-5, -5);
            nameRect.localScale = Vector3.one;

            TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
            nameText.text = upgrade.Name;
            nameText.fontSize = 18;
            nameText.alignment = TextAlignmentOptions.MidlineLeft;
            nameText.color = purchased ? new Color(0.6f, 0.8f, 0.6f) : Color.white;

            // Description text
            GameObject descObj = new GameObject("Desc");
            descObj.transform.SetParent(btnObj.transform);
            RectTransform descRect = descObj.AddComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0, 0);
            descRect.anchorMax = new Vector2(0.7f, 0.5f);
            descRect.offsetMin = new Vector2(10, 5);
            descRect.offsetMax = new Vector2(-5, -5);
            descRect.localScale = Vector3.one;

            TextMeshProUGUI descText = descObj.AddComponent<TextMeshProUGUI>();
            descText.text = upgrade.Description;
            descText.fontSize = 14;
            descText.alignment = TextAlignmentOptions.MidlineLeft;
            descText.color = new Color(0.7f, 0.7f, 0.7f);

            // Cost text
            GameObject costObj = new GameObject("Cost");
            costObj.transform.SetParent(btnObj.transform);
            RectTransform costRect = costObj.AddComponent<RectTransform>();
            costRect.anchorMin = new Vector2(0.7f, 0);
            costRect.anchorMax = new Vector2(1, 1);
            costRect.offsetMin = new Vector2(5, 5);
            costRect.offsetMax = new Vector2(-10, -5);
            costRect.localScale = Vector3.one;

            TextMeshProUGUI costText = costObj.AddComponent<TextMeshProUGUI>();
            if (purchased)
            {
                costText.text = "OWNED";
                costText.color = new Color(0.5f, 0.8f, 0.5f);
            }
            else
            {
                costText.text = $"{upgrade.Cost} PC";
                costText.color = canBuy ? new Color(0.6f, 0.8f, 1f) : new Color(0.5f, 0.5f, 0.5f);
            }
            costText.fontSize = 20;
            costText.alignment = TextAlignmentOptions.Center;

            upgradeButtons.Add(btnObj);
        }

        void OnUpgradeClicked(string upgradeId)
        {
            var upgrade = PrestigeDatabase.GetUpgrade(upgradeId);
            if (upgrade == null) return;

            if (PrestigeDatabase.IsUpgradePurchased(upgradeId))
            {
                ShowFeedback("Already purchased!", Color.yellow);
                return;
            }

            if (GameData.PrestigeCount == 0)
            {
                ShowFeedback("Must prestige at least once to buy upgrades!", Color.red);
                return;
            }

            if (!PrestigeDatabase.CanPurchaseUpgrade(upgradeId))
            {
                // Check if it's a prerequisite issue
                foreach (var prereq in upgrade.Prerequisites)
                {
                    if (!PrestigeDatabase.IsUpgradePurchased(prereq))
                    {
                        var prereqUpgrade = PrestigeDatabase.GetUpgrade(prereq);
                        ShowFeedback($"Requires: {prereqUpgrade?.Name ?? prereq}", Color.red);
                        return;
                    }
                }
                ShowFeedback($"Need {upgrade.Cost} Prestige Crystals!", Color.red);
                return;
            }

            if (PrestigeDatabase.PurchaseUpgrade(upgradeId))
            {
                ShowFeedback($"Purchased {upgrade.Name}!", new Color(0.5f, 1f, 0.5f));
                PopulateUpgrades(); // Refresh UI
            }
        }

        void OnPrestigeClicked()
        {
            if (GameData.UnlockedBiomes.Count <= 1 && GameData.PrestigeCount == 0)
            {
                ShowFeedback("Unlock more biomes before your first prestige!", Color.red);
                return;
            }

            // Confirm prestige
            GameData.DoPrestige();
            ShowFeedback($"Prestige #{GameData.PrestigeCount} complete! Progress reset.", new Color(0.8f, 0.6f, 1f));
            PopulateUpgrades(); // Refresh UI
        }

        void CreateNavButton(Transform parent, Vector2 position, string label, string sceneName)
        {
            GameObject btnObj = new GameObject($"Nav_{label}");
            btnObj.transform.SetParent(parent);

            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(1, 1);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(120, 35);
            rect.localScale = Vector3.one;

            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.3f, 0.25f, 0.35f);

            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => SceneManager.LoadScene(sceneName));

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            textRect.localScale = Vector3.one;

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 16;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
        }

        TextMeshProUGUI CreateText(Transform parent, Vector2 position, string content)
        {
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(parent);

            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(300, 30);
            rect.localScale = Vector3.one;

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = 18;
            text.color = Color.white;

            return text;
        }

        void UpdateUI()
        {
            if (pointsText != null)
            {
                pointsText.text = $"Prestige Crystals: {GameData.PrestigePoints}";
            }

            if (prestigeCountText != null)
            {
                string bonusInfo = "";
                if (GameData.PrestigeIncomeBonus > 0) bonusInfo += $" | +{GameData.PrestigeIncomeBonus}% Income";
                if (GameData.PrestigeDigBonus > 0) bonusInfo += $" | +{GameData.PrestigeDigBonus}% Dig";
                prestigeCountText.text = $"Prestige Count: {GameData.PrestigeCount}{bonusInfo}";
            }
        }

        void HandleInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.escapeKey.wasPressedThisFrame)
                {
                    SceneManager.LoadScene("SampleScene");
                }
                if (keyboard.digit1Key.wasPressedThisFrame)
                {
                    SceneManager.LoadScene("ACD_Exploration");
                }
                if (keyboard.digit2Key.wasPressedThisFrame)
                {
                    SceneManager.LoadScene("ACD_Zoo");
                }
            }
        }

        void ShowFeedback(string message, Color color)
        {
            if (feedbackText != null)
            {
                feedbackText.text = message;
                feedbackText.color = color;
                feedbackTimer = 3f;
            }
        }

        void UpdateFeedback()
        {
            if (feedbackTimer > 0)
            {
                feedbackTimer -= Time.deltaTime;
                if (feedbackTimer <= 0 && feedbackText != null)
                {
                    feedbackText.text = "";
                }
            }
        }
    }
}
