using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

namespace AdvancedCreatureDigseum
{
    public class ExplorationManager : MonoBehaviour
    {
        [Header("Grid Settings")]
        public int baseGridWidth = 12;   // Base size for first biome
        public int baseGridHeight = 10;
        public float tileSize = 0.5f;

        // Actual grid size (scales with biome)
        private int gridWidth;
        private int gridHeight;

        private int currentBiomeIndex = 0;
        private BiomeData currentBiome;
        private bool[,] revealedTiles;
        private int[,] tileHealth;  // Health remaining on each tile
        private int[,] tileMaxHealth; // Max health for visual feedback
        private Dictionary<Vector2Int, AnimalData> hiddenAnimals;
        private Camera mainCamera;

        // UI
        private TextMeshProUGUI energyText;
        private TextMeshProUGUI goldText;
        private TextMeshProUGUI biomeText;
        private TextMeshProUGUI foundText;
        private TextMeshProUGUI feedbackText;
        private float feedbackTimer;
        private Transform canvasTransform;

        // Refresh panel (shows when out of energy)
        private GameObject refreshPanel;
        private TextMeshProUGUI refreshSummaryText;
        private bool refreshPanelVisible = false;

        // Fog visual
        private GameObject fogParent;
        private SpriteRenderer[,] fogSprites;
        private Color[,] originalFogColors; // Store original colors for damage feedback
        private GameObject backgroundObj;
        private List<GameObject> foundAnimalObjects = new List<GameObject>();

        // Animals found this session
        private List<string> animalsFoundThisRun = new List<string>();

        // Autosave
        private float saveTimer = 0f;
        private float saveInterval = 10f;

        void Start()
        {
            GameData.EnsureLoaded();

            mainCamera = Camera.main;
            currentBiome = BiomeDatabase.Biomes[currentBiomeIndex];

            CreateUI();
            InitializeBiome();
        }

        void CreateUI()
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvasTransform = canvasObj.transform;
            Canvas canvas = canvasObj.AddComponent<Canvas>();
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

            // Top left stats
            goldText = CreateText(canvasObj.transform, new Vector2(10, -10), "Gold: 0");
            goldText.fontSize = 24;

            energyText = CreateText(canvasObj.transform, new Vector2(10, -45), "Energy: 100/100");
            energyText.fontSize = 20;

            biomeText = CreateText(canvasObj.transform, new Vector2(10, -80), "Biome: Meadow");
            biomeText.fontSize = 20;

            foundText = CreateText(canvasObj.transform, new Vector2(10, -120), "Animals Found: 0");
            foundText.fontSize = 16;
            RectTransform foundRect = foundText.GetComponent<RectTransform>();
            foundRect.sizeDelta = new Vector2(350, 60);

            // Title (right side)
            TextMeshProUGUI title = CreateText(canvasObj.transform, new Vector2(-10, -10), "EXPLORATION");
            RectTransform titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(1, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(1, 1);
            titleRect.sizeDelta = new Vector2(200, 35);
            title.fontSize = 24;
            title.alignment = TextAlignmentOptions.Right;

            // Navigation buttons
            float navY = -50;
            CreateNavButton(canvasObj.transform, new Vector2(-10, navY), "Fusion Lab", "ACD_FusionLab");
            CreateNavButton(canvasObj.transform, new Vector2(-10, navY - 40), "Zoo", "ACD_Zoo");
            CreateNavButton(canvasObj.transform, new Vector2(-10, navY - 80), "Skills", "ACD_Skills");
            CreateNavButton(canvasObj.transform, new Vector2(-10, navY - 120), "Menu", "SampleScene");

            // Instructions
            TextMeshProUGUI instructions = CreateText(canvasObj.transform, new Vector2(-10, navY - 170),
                "Click to dig tiles\n" +
                "Harder tiles need more\n" +
                "clicks to reveal!\n\n" +
                "R: Refresh biome\n" +
                "Q/E: Change biome\n" +
                "B: Buy biome\n\n" +
                "CHEATS:\n" +
                "F1: +1000 Gold\n" +
                "F2: All biomes\n" +
                "F3: All animals\n" +
                "F4: Max skills");
            RectTransform instRect = instructions.GetComponent<RectTransform>();
            instRect.anchorMin = new Vector2(1, 1);
            instRect.anchorMax = new Vector2(1, 1);
            instRect.pivot = new Vector2(1, 1);
            instRect.sizeDelta = new Vector2(200, 280);
            instructions.fontSize = 12;

            // Biome selector at bottom
            CreateBiomeButtons(canvasObj.transform);

            // Feedback text (center)
            feedbackText = CreateText(canvasObj.transform, Vector2.zero, "");
            RectTransform fbRect = feedbackText.GetComponent<RectTransform>();
            fbRect.anchorMin = new Vector2(0.5f, 0.5f);
            fbRect.anchorMax = new Vector2(0.5f, 0.5f);
            fbRect.pivot = new Vector2(0.5f, 0.5f);
            fbRect.sizeDelta = new Vector2(600, 100);
            feedbackText.alignment = TextAlignmentOptions.Center;
            feedbackText.fontSize = 32;

            // Create refresh panel (hidden by default)
            CreateRefreshPanel(canvasObj.transform);
        }

        void CreateRefreshPanel(Transform parent)
        {
            // Panel background
            refreshPanel = new GameObject("RefreshPanel");
            refreshPanel.transform.SetParent(parent);

            RectTransform panelRect = refreshPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(450, 350);
            panelRect.localScale = Vector3.one;

            Image panelImg = refreshPanel.AddComponent<Image>();
            panelImg.color = new Color(0.15f, 0.12f, 0.2f, 0.95f);

            // Title
            TextMeshProUGUI titleText = CreatePanelText(refreshPanel.transform, new Vector2(0, 140), "Energy Depleted!");
            titleText.fontSize = 28;
            titleText.color = new Color(1f, 0.8f, 0.3f);
            titleText.alignment = TextAlignmentOptions.Center;
            RectTransform titleRect = titleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.sizeDelta = new Vector2(400, 40);

            // Summary text
            refreshSummaryText = CreatePanelText(refreshPanel.transform, new Vector2(0, 20), "");
            refreshSummaryText.fontSize = 16;
            refreshSummaryText.alignment = TextAlignmentOptions.Center;
            RectTransform summaryRect = refreshSummaryText.GetComponent<RectTransform>();
            summaryRect.anchorMin = new Vector2(0.5f, 0.5f);
            summaryRect.anchorMax = new Vector2(0.5f, 0.5f);
            summaryRect.pivot = new Vector2(0.5f, 0.5f);
            summaryRect.sizeDelta = new Vector2(400, 180);

            // Refresh button
            GameObject btnObj = new GameObject("RefreshButton");
            btnObj.transform.SetParent(refreshPanel.transform);

            RectTransform btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0.5f);
            btnRect.anchorMax = new Vector2(0.5f, 0.5f);
            btnRect.pivot = new Vector2(0.5f, 0.5f);
            btnRect.anchoredPosition = new Vector2(0, -130);
            btnRect.sizeDelta = new Vector2(200, 50);
            btnRect.localScale = Vector3.one;

            Image btnImg = btnObj.AddComponent<Image>();
            btnImg.color = new Color(0.3f, 0.6f, 0.3f);

            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => OnRefreshButtonClicked());

            GameObject btnTextObj = new GameObject("Text");
            btnTextObj.transform.SetParent(btnObj.transform);
            RectTransform btnTextRect = btnTextObj.AddComponent<RectTransform>();
            btnTextRect.anchorMin = Vector2.zero;
            btnTextRect.anchorMax = Vector2.one;
            btnTextRect.offsetMin = Vector2.zero;
            btnTextRect.offsetMax = Vector2.zero;
            btnTextRect.localScale = Vector3.one;

            TextMeshProUGUI btnText = btnTextObj.AddComponent<TextMeshProUGUI>();
            btnText.text = "Refresh Biome";
            btnText.fontSize = 22;
            btnText.color = Color.white;
            btnText.alignment = TextAlignmentOptions.Center;
            btnText.raycastTarget = false;

            // Hide panel initially
            refreshPanel.SetActive(false);
        }

        TextMeshProUGUI CreatePanelText(Transform parent, Vector2 position, string text)
        {
            GameObject textObj = new GameObject("PanelText");
            textObj.transform.SetParent(parent);

            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.localScale = Vector3.one;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 18;
            tmp.color = Color.white;

            return tmp;
        }

        void OnRefreshButtonClicked()
        {
            HideRefreshPanel();
            RefreshBiome();
        }

        void ShowRefreshPanel()
        {
            if (refreshPanel == null || refreshPanelVisible) return;

            refreshPanelVisible = true;
            refreshPanel.SetActive(true);

            // Build summary of animals found this run
            string summary = "";

            if (animalsFoundThisRun.Count == 0)
            {
                summary = "No animals found this run.\n\nKeep exploring to discover creatures!";
            }
            else
            {
                summary = $"<color=#8f8>Animals Found This Run: {animalsFoundThisRun.Count}</color>\n\n";

                // Count unique animals and their bonuses
                Dictionary<string, int> foundCounts = new Dictionary<string, int>();
                foreach (string animalId in animalsFoundThisRun)
                {
                    if (!foundCounts.ContainsKey(animalId))
                        foundCounts[animalId] = 0;
                    foundCounts[animalId]++;
                }

                foreach (var kvp in foundCounts)
                {
                    AnimalData animal = AnimalDatabase.GetAnimal(kvp.Key);
                    if (animal != null)
                    {
                        int historicalCount = GameData.GetHistoricalFindCount(kvp.Key);
                        int bonusPercent = historicalCount * 10;
                        summary += $"{animal.Name} x{kvp.Value}";
                        if (historicalCount > 1)
                        {
                            summary += $" <color=#ff8>(+{bonusPercent}% hybrid bonus)</color>";
                        }
                        summary += "\n";
                    }
                }

                summary += $"\n<color=#aaf>Historical finds boost hybrid values!</color>";
            }

            refreshSummaryText.text = summary;
        }

        void HideRefreshPanel()
        {
            if (refreshPanel == null) return;
            refreshPanelVisible = false;
            refreshPanel.SetActive(false);
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
            rect.sizeDelta = new Vector2(350, 40);

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

        // Biome button references for updating visuals
        private List<BiomeButtonRef> biomeButtons = new List<BiomeButtonRef>();

        void CreateBiomeButtons(Transform parent)
        {
            float startX = 60;
            for (int i = 0; i < 10; i++)
            {
                int biomeIndex = i;
                BiomeData biome = BiomeDatabase.Biomes[i];

                // Main button container
                GameObject btnObj = new GameObject($"BiomeBtn_{i}");
                btnObj.transform.SetParent(parent);

                RectTransform rect = btnObj.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0, 0);
                rect.anchorMax = new Vector2(0, 0);
                rect.pivot = new Vector2(0, 0);
                rect.anchoredPosition = new Vector2(startX + i * 95, 10);
                rect.sizeDelta = new Vector2(90, 70);
                rect.localScale = Vector3.one;

                Image img = btnObj.AddComponent<Image>();
                img.color = biome.BackgroundColor;

                Button btn = btnObj.AddComponent<Button>();
                btn.onClick.AddListener(() => TrySelectBiome(biomeIndex));

                // Biome name/number text
                GameObject textObj = new GameObject("Text");
                textObj.transform.SetParent(btnObj.transform);
                RectTransform textRect = textObj.AddComponent<RectTransform>();
                textRect.anchorMin = new Vector2(0, 0.5f);
                textRect.anchorMax = new Vector2(1, 1);
                textRect.offsetMin = new Vector2(2, 0);
                textRect.offsetMax = new Vector2(-2, -2);
                textRect.localScale = Vector3.one;

                TextMeshProUGUI nameTmp = textObj.AddComponent<TextMeshProUGUI>();
                nameTmp.text = $"{i + 1}. {biome.Name}";
                nameTmp.fontSize = 10;
                nameTmp.color = Color.white;
                nameTmp.alignment = TextAlignmentOptions.Center;
                nameTmp.raycastTarget = false;

                // Cost text
                GameObject costObj = new GameObject("Cost");
                costObj.transform.SetParent(btnObj.transform);
                RectTransform costRect = costObj.AddComponent<RectTransform>();
                costRect.anchorMin = new Vector2(0, 0.25f);
                costRect.anchorMax = new Vector2(1, 0.5f);
                costRect.offsetMin = new Vector2(2, 0);
                costRect.offsetMax = new Vector2(-2, 0);
                costRect.localScale = Vector3.one;

                TextMeshProUGUI costTmp = costObj.AddComponent<TextMeshProUGUI>();
                costTmp.text = biome.UnlockCost > 0 ? $"{biome.UnlockCost}g" : "FREE";
                costTmp.fontSize = 9;
                costTmp.color = new Color(1f, 0.9f, 0.5f);
                costTmp.alignment = TextAlignmentOptions.Center;
                costTmp.raycastTarget = false;

                // Buy button (only for non-free biomes)
                GameObject buyBtnObj = new GameObject("BuyBtn");
                buyBtnObj.transform.SetParent(btnObj.transform);
                RectTransform buyRect = buyBtnObj.AddComponent<RectTransform>();
                buyRect.anchorMin = new Vector2(0.1f, 0);
                buyRect.anchorMax = new Vector2(0.9f, 0.25f);
                buyRect.offsetMin = Vector2.zero;
                buyRect.offsetMax = Vector2.zero;
                buyRect.localScale = Vector3.one;

                Image buyImg = buyBtnObj.AddComponent<Image>();
                buyImg.color = new Color(0.3f, 0.5f, 0.3f);

                Button buyBtn = buyBtnObj.AddComponent<Button>();
                buyBtn.onClick.AddListener(() => TryUnlockBiome(biomeIndex));

                GameObject buyTextObj = new GameObject("BuyText");
                buyTextObj.transform.SetParent(buyBtnObj.transform);
                RectTransform buyTextRect = buyTextObj.AddComponent<RectTransform>();
                buyTextRect.anchorMin = Vector2.zero;
                buyTextRect.anchorMax = Vector2.one;
                buyTextRect.offsetMin = Vector2.zero;
                buyTextRect.offsetMax = Vector2.zero;
                buyTextRect.localScale = Vector3.one;

                TextMeshProUGUI buyTmp = buyTextObj.AddComponent<TextMeshProUGUI>();
                buyTmp.text = "BUY";
                buyTmp.fontSize = 9;
                buyTmp.color = Color.white;
                buyTmp.alignment = TextAlignmentOptions.Center;
                buyTmp.raycastTarget = false;

                // Store reference for updating
                BiomeButtonRef btnRef = btnObj.AddComponent<BiomeButtonRef>();
                btnRef.BiomeIndex = biomeIndex;
                btnRef.ButtonImage = img;
                btnRef.CostText = costTmp;
                btnRef.BuyButton = buyBtnObj;
                btnRef.OriginalColor = biome.BackgroundColor;
                biomeButtons.Add(btnRef);
            }

            // Initial update of button states
            UpdateBiomeButtonStates();
        }

        void UpdateBiomeButtonStates()
        {
            foreach (var btnRef in biomeButtons)
            {
                bool isUnlocked = GameData.UnlockedBiomes.Contains(btnRef.BiomeIndex);
                bool isCurrentBiome = btnRef.BiomeIndex == currentBiomeIndex;

                if (isUnlocked)
                {
                    // Unlocked - show normal color, hide buy button
                    btnRef.ButtonImage.color = isCurrentBiome
                        ? btnRef.OriginalColor * 1.3f  // Brighter for current
                        : btnRef.OriginalColor;
                    btnRef.CostText.text = "UNLOCKED";
                    btnRef.CostText.color = Color.green;
                    btnRef.BuyButton.SetActive(false);
                }
                else
                {
                    // Locked - red tint, show buy button
                    btnRef.ButtonImage.color = new Color(0.5f, 0.2f, 0.2f);
                    int cost = BiomeDatabase.Biomes[btnRef.BiomeIndex].UnlockCost;
                    btnRef.CostText.text = $"{cost}g";
                    btnRef.CostText.color = GameData.Gold >= cost
                        ? new Color(1f, 0.9f, 0.5f)  // Can afford
                        : Color.red;  // Can't afford
                    btnRef.BuyButton.SetActive(true);
                }
            }
        }

        void InitializeBiome()
        {
            // Clear old objects
            if (fogParent != null) Destroy(fogParent);
            if (backgroundObj != null) Destroy(backgroundObj);
            foreach (var obj in foundAnimalObjects)
            {
                if (obj != null) Destroy(obj);
            }
            foundAnimalObjects.Clear();

            currentBiome = BiomeDatabase.Biomes[currentBiomeIndex];
            mainCamera.backgroundColor = currentBiome.BackgroundColor;

            // Calculate grid size based on biome index (scales up with progression)
            // Biome 0: base size, Biome 9: nearly double
            float sizeMultiplier = 1f + currentBiomeIndex * 0.12f;
            gridWidth = Mathf.RoundToInt(baseGridWidth * sizeMultiplier);
            gridHeight = Mathf.RoundToInt(baseGridHeight * sizeMultiplier);

            // Reset energy to max
            GameData.CurrentEnergy = GameData.MaxEnergy;

            // Create background
            backgroundObj = new GameObject("Background");
            SpriteRenderer bgSr = backgroundObj.AddComponent<SpriteRenderer>();
            Texture2D bgTex = new Texture2D(gridWidth * 8, gridHeight * 8);
            Color[] bgPixels = new Color[bgTex.width * bgTex.height];

            for (int i = 0; i < bgPixels.Length; i++)
            {
                float noise = Random.Range(0.9f, 1.1f);
                bgPixels[i] = currentBiome.BackgroundColor * noise;
            }
            bgTex.SetPixels(bgPixels);
            bgTex.Apply();
            bgTex.filterMode = FilterMode.Point;
            bgSr.sprite = Sprite.Create(bgTex, new Rect(0, 0, bgTex.width, bgTex.height), new Vector2(0.5f, 0.5f), 16);
            bgSr.sortingOrder = -10;

            // Initialize fog grid with health system
            revealedTiles = new bool[gridWidth, gridHeight];
            tileHealth = new int[gridWidth, gridHeight];
            tileMaxHealth = new int[gridWidth, gridHeight];
            fogSprites = new SpriteRenderer[gridWidth, gridHeight];
            originalFogColors = new Color[gridWidth, gridHeight];
            hiddenAnimals = new Dictionary<Vector2Int, AnimalData>();

            fogParent = new GameObject("Fog");

            float startX = -(gridWidth - 1) * tileSize / 2f;
            float startY = -(gridHeight - 1) * tileSize / 2f;

            // Calculate base tile health from biome difficulty
            // Difficulty 1 = 1 health (1 click), Difficulty 6 = 6 health (multiple clicks)
            int baseTileHealth = currentBiome.Difficulty;

            // Place hidden animals
            List<Vector2Int> animalPositions = new List<Vector2Int>();
            foreach (var animal in currentBiome.Animals)
            {
                // Each animal appears 1-3 times based on rarity
                int count = 4 - animal.Rarity;
                for (int c = 0; c < count; c++)
                {
                    Vector2Int pos;
                    int attempts = 0;
                    do
                    {
                        pos = new Vector2Int(Random.Range(0, gridWidth), Random.Range(0, gridHeight));
                        attempts++;
                    } while (animalPositions.Contains(pos) && attempts < 100);

                    if (!animalPositions.Contains(pos))
                    {
                        animalPositions.Add(pos);
                        hiddenAnimals[pos] = animal;
                    }
                }
            }

            // Create fog tiles with health
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    GameObject fogTile = new GameObject($"Fog_{x}_{y}");
                    fogTile.transform.parent = fogParent.transform;
                    fogTile.transform.position = new Vector3(startX + x * tileSize, startY + y * tileSize, 0);

                    SpriteRenderer sr = fogTile.AddComponent<SpriteRenderer>();
                    Texture2D fogTex = new Texture2D(16, 16);
                    Color[] fogPixels = new Color[256];
                    Color baseFogColor = currentBiome.FogColor * Random.Range(0.9f, 1.1f);
                    for (int i = 0; i < 256; i++)
                    {
                        fogPixels[i] = baseFogColor;
                    }
                    fogTex.SetPixels(fogPixels);
                    fogTex.Apply();
                    fogTex.filterMode = FilterMode.Point;
                    sr.sprite = Sprite.Create(fogTex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 32);
                    sr.sortingOrder = 1;

                    fogSprites[x, y] = sr;
                    originalFogColors[x, y] = baseFogColor;
                    revealedTiles[x, y] = false;

                    // Set tile health - tiles with animals have bonus health
                    Vector2Int pos = new Vector2Int(x, y);
                    int health = baseTileHealth;
                    if (hiddenAnimals.ContainsKey(pos))
                    {
                        // Animals get extra health based on rarity
                        health += hiddenAnimals[pos].Rarity;
                    }
                    tileHealth[x, y] = health;
                    tileMaxHealth[x, y] = health;
                }
            }

            animalsFoundThisRun.Clear();
        }

        void RefreshBiome()
        {
            // Simply reinitialize the biome - this resets fog and repositions animals
            InitializeBiome();
            ShowFeedback("Biome refreshed! Energy restored.", Color.green);
        }

        void TrySelectBiome(int index)
        {
            if (index == currentBiomeIndex)
            {
                // Re-entering same biome is essentially a refresh
                RefreshBiome();
                return;
            }

            if (!GameData.UnlockedBiomes.Contains(index))
            {
                int cost = BiomeDatabase.Biomes[index].UnlockCost;
                ShowFeedback($"{BiomeDatabase.Biomes[index].Name} is locked! Click BUY ({cost}g)", Color.red);
                return;
            }

            currentBiomeIndex = index;
            InitializeBiome();
            UpdateBiomeButtonStates();
        }

        void TryUnlockBiome(int index)
        {
            if (GameData.UnlockedBiomes.Contains(index))
            {
                ShowFeedback("Already unlocked!", Color.yellow);
                return;
            }

            int cost = BiomeDatabase.Biomes[index].UnlockCost;
            if (GameData.Gold >= cost)
            {
                GameData.Gold -= cost;
                GameData.UnlockedBiomes.Add(index);
                ShowFeedback($"Unlocked {BiomeDatabase.Biomes[index].Name}!", Color.green);

                // Switch to the newly unlocked biome
                currentBiomeIndex = index;
                InitializeBiome();
                UpdateBiomeButtonStates();
                GameData.SaveGame();
            }
            else
            {
                ShowFeedback($"Need {cost} gold to unlock!", Color.red);
            }
        }

        void TryUnlockCurrentBiome()
        {
            if (GameData.UnlockedBiomes.Contains(currentBiomeIndex))
            {
                ShowFeedback("Already unlocked!", Color.yellow);
                return;
            }

            int cost = currentBiome.UnlockCost;
            if (GameData.Gold >= cost)
            {
                GameData.Gold -= cost;
                GameData.UnlockedBiomes.Add(currentBiomeIndex);
                ShowFeedback($"Unlocked {currentBiome.Name}!", Color.green);
                InitializeBiome();
                GameData.SaveGame();
            }
            else
            {
                ShowFeedback($"Need {cost} gold to unlock!", Color.red);
            }
        }

        void Update()
        {
            // NO automatic energy regen - player must refresh biome
            HandleInput();
            UpdateUI();
            UpdateFeedback();

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
            var mouse = Mouse.current;

            if (keyboard != null)
            {
                // Scene switching
                if (keyboard.tabKey.wasPressedThisFrame)
                    SceneManager.LoadScene("ACD_FusionLab");
                if (keyboard.zKey.wasPressedThisFrame)
                    SceneManager.LoadScene("ACD_Zoo");
                if (keyboard.xKey.wasPressedThisFrame)
                    SceneManager.LoadScene("ACD_Skills");
                if (keyboard.escapeKey.wasPressedThisFrame)
                    SceneManager.LoadScene("SampleScene");

                // Biome switching
                if (keyboard.qKey.wasPressedThisFrame)
                {
                    int newIndex = Mathf.Max(0, currentBiomeIndex - 1);
                    if (newIndex != currentBiomeIndex)
                        TrySelectBiome(newIndex);
                }
                if (keyboard.eKey.wasPressedThisFrame)
                {
                    int newIndex = Mathf.Min(9, currentBiomeIndex + 1);
                    if (newIndex != currentBiomeIndex)
                        TrySelectBiome(newIndex);
                }

                // Refresh biome (R key)
                if (keyboard.rKey.wasPressedThisFrame)
                {
                    RefreshBiome();
                }

                // Buy biome (B key)
                if (keyboard.bKey.wasPressedThisFrame)
                {
                    TryUnlockCurrentBiome();
                }

                // Cheats
                if (keyboard.f1Key.wasPressedThisFrame)
                {
                    GameData.CheatAddGold(1000);
                    ShowFeedback("+1000 Gold!", Color.yellow);
                }
                if (keyboard.f2Key.wasPressedThisFrame)
                {
                    GameData.CheatUnlockAllBiomes();
                    ShowFeedback("All biomes unlocked!", Color.yellow);
                }
                if (keyboard.f3Key.wasPressedThisFrame)
                {
                    GameData.CheatFindAllAnimals();
                    ShowFeedback("All animals found!", Color.yellow);
                }
                if (keyboard.f4Key.wasPressedThisFrame)
                {
                    GameData.CheatMaxSkills();
                    ShowFeedback("All skills maxed!", Color.yellow);
                }
            }

            // Click to reveal fog
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                TryRevealFog(mouse.position.ReadValue());
            }
        }

        void TryRevealFog(Vector2 screenPos)
        {
            // Check if biome is unlocked
            if (!GameData.UnlockedBiomes.Contains(currentBiomeIndex))
            {
                ShowFeedback($"Unlock {currentBiome.Name} first! (Press B)", Color.red);
                return;
            }

            Vector3 worldPos = mainCamera.ScreenToWorldPoint(screenPos);

            float startX = -(gridWidth - 1) * tileSize / 2f;
            float startY = -(gridHeight - 1) * tileSize / 2f;

            int gridX = Mathf.RoundToInt((worldPos.x - startX) / tileSize);
            int gridY = Mathf.RoundToInt((worldPos.y - startY) / tileSize);

            if (gridX < 0 || gridX >= gridWidth || gridY < 0 || gridY >= gridHeight)
                return;

            // Check energy - base cost scaled by difficulty
            int energyCost = 5 + currentBiome.Difficulty * 2;
            if (GameData.CurrentEnergy < energyCost)
            {
                // Show refresh panel with summary instead of just text
                ShowRefreshPanel();
                return;
            }

            GameData.CurrentEnergy -= energyCost;

            // Deal damage to tiles based on radius - spread damage system
            // Center tile takes full DigPower, surrounding tiles take reduced damage
            int revealed = 0;
            int damaged = 0;

            // Balanced spread radius: DigRadius 1=0, 2=1, 3=1, 4=2, 5=2, etc.
            int spreadRadius = GameData.DigRadius / 2;

            for (int dx = -spreadRadius; dx <= spreadRadius; dx++)
            {
                for (int dy = -spreadRadius; dy <= spreadRadius; dy++)
                {
                    float distSq = dx * dx + dy * dy;
                    // Circular check
                    if (distSq <= spreadRadius * spreadRadius + 0.5f)
                    {
                        int tx = gridX + dx;
                        int ty = gridY + dy;

                        if (tx >= 0 && tx < gridWidth && ty >= 0 && ty < gridHeight)
                        {
                            if (!revealedTiles[tx, ty])
                            {
                                // Calculate damage based on distance from center
                                // Center (0,0) = full damage, edges = reduced damage
                                float dist = Mathf.Sqrt(distSq);
                                // 80% falloff at edges, so edges do 20% damage
                                float damageMultiplier = 1f - (dist / (spreadRadius + 1f)) * 0.8f;
                                int damage = Mathf.Max(1, Mathf.RoundToInt(GameData.DigPower * damageMultiplier));

                                // Apply damage
                                tileHealth[tx, ty] -= damage;
                                damaged++;

                                if (tileHealth[tx, ty] <= 0)
                                {
                                    // Tile is destroyed - reveal it
                                    RevealTile(tx, ty);
                                    revealed++;
                                }
                                else
                                {
                                    // Tile is damaged but not destroyed - show visual feedback
                                    UpdateTileDamageVisual(tx, ty);
                                }
                            }
                        }
                    }
                }
            }

            // Show damage feedback if no tiles were revealed but some were damaged
            if (revealed == 0 && damaged > 0)
            {
                int centerHealth = tileHealth[gridX, gridY];
                if (centerHealth > 0)
                {
                    ShowFeedback($"Digging... ({centerHealth} HP left)", new Color(1f, 0.8f, 0.4f));
                }
            }
        }

        void UpdateTileDamageVisual(int x, int y)
        {
            if (fogSprites[x, y] == null) return;

            // Calculate damage percentage
            float healthPercent = (float)tileHealth[x, y] / tileMaxHealth[x, y];

            // Darken and crack the tile as it takes damage
            Color originalColor = originalFogColors[x, y];

            // Mix between original color and a cracked/dark version
            Color damagedColor = Color.Lerp(
                new Color(0.3f, 0.2f, 0.1f, originalColor.a), // Dark cracked color
                originalColor,
                healthPercent
            );

            // Also reduce alpha slightly as damage increases
            damagedColor.a = originalColor.a * (0.5f + healthPercent * 0.5f);

            fogSprites[x, y].color = damagedColor;
        }

        void RevealTile(int x, int y)
        {
            revealedTiles[x, y] = true;

            if (fogSprites[x, y] != null)
            {
                fogSprites[x, y].gameObject.SetActive(false);
            }

            // Check for animal
            Vector2Int pos = new Vector2Int(x, y);
            if (hiddenAnimals.ContainsKey(pos))
            {
                AnimalData animal = hiddenAnimals[pos];
                FoundAnimal(animal, x, y);
                hiddenAnimals.Remove(pos);
            }
        }

        void FoundAnimal(AnimalData animal, int x, int y)
        {
            // Add to collection
            if (!GameData.FoundAnimals.ContainsKey(animal.Id))
                GameData.FoundAnimals[animal.Id] = 0;
            GameData.FoundAnimals[animal.Id]++;

            animalsFoundThisRun.Add(animal.Id);

            // Create visual
            float startX = -(gridWidth - 1) * tileSize / 2f;
            float startY = -(gridHeight - 1) * tileSize / 2f;

            GameObject animalObj = new GameObject($"Found_{animal.Name}");
            animalObj.transform.position = new Vector3(startX + x * tileSize, startY + y * tileSize, 0);
            animalObj.transform.localScale = Vector3.one * 0.5f;

            CreatureRenderer renderer = animalObj.AddComponent<CreatureRenderer>();
            renderer.RenderAnimal(animal);

            foundAnimalObjects.Add(animalObj);

            // Award gold - scales with biome and rarity
            // Later biomes give SIGNIFICANTLY more gold (quadratic scaling)
            int biomeLevel = animal.BiomeIndex + 1;  // 1-10
            int biomeBonus = biomeLevel * biomeLevel * 8;  // 8, 32, 72, 128, 200, 288, 392, 512, 648, 800
            int rarityBonus = animal.Rarity * biomeLevel * 5;  // Rarity also scales with biome
            int goldReward = 5 + biomeBonus + rarityBonus;
            GameData.Gold += goldReward;

            // Track historical finds for hybrid value bonuses
            GameData.RecordHistoricalFind(animal.Id);

            int timesFound = GameData.GetHistoricalFindCount(animal.Id);
            ShowFeedback($"Found {animal.Name}! (+{goldReward}g) [x{timesFound} total]", Color.yellow);

            // Auto-save when finding animals
            GameData.SaveGame();
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
                {
                    feedbackText.text = "";
                }
            }
        }

        void UpdateUI()
        {
            if (goldText != null)
                goldText.text = $"Gold: {GameData.Gold}";

            if (energyText != null)
                energyText.text = $"Energy: {(int)GameData.CurrentEnergy}/{GameData.MaxEnergy}";

            if (biomeText != null)
            {
                string status = "";
                if (!GameData.UnlockedBiomes.Contains(currentBiomeIndex))
                    status = $" [LOCKED - {currentBiome.UnlockCost}g]";
                else
                    status = $" [{gridWidth}x{gridHeight}, HP:{currentBiome.Difficulty}]";
                biomeText.text = $"Biome: {currentBiome.Name}{status}";
            }

            if (foundText != null)
            {
                int totalFound = 0;
                foreach (var kvp in GameData.FoundAnimals)
                    totalFound += kvp.Value;
                foundText.text = $"Animals: {GameData.FoundAnimals.Count}/30 unique\n({totalFound} total in inventory)";
            }

            // Update biome button colors based on gold/unlock status
            UpdateBiomeButtonStates();
        }
    }

    // Helper class to track biome button UI references
    public class BiomeButtonRef : MonoBehaviour
    {
        public int BiomeIndex;
        public Image ButtonImage;
        public TextMeshProUGUI CostText;
        public GameObject BuyButton;
        public Color OriginalColor;
    }
}
