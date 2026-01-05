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
        public int gridWidth = 20;
        public int gridHeight = 15;
        public float tileSize = 0.5f;

        private int currentBiomeIndex = 0;
        private BiomeData currentBiome;
        private bool[,] revealedTiles;
        private Dictionary<Vector2Int, AnimalData> hiddenAnimals;
        private Camera mainCamera;

        // UI
        private TextMeshProUGUI energyText;
        private TextMeshProUGUI goldText;
        private TextMeshProUGUI biomeText;
        private TextMeshProUGUI foundText;
        private TextMeshProUGUI feedbackText;
        private float feedbackTimer;

        // Fog visual
        private GameObject fogParent;
        private SpriteRenderer[,] fogSprites;
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
                "Click to reveal fog\n" +
                "Find hidden animals!\n\n" +
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

            // Initialize fog grid
            revealedTiles = new bool[gridWidth, gridHeight];
            fogSprites = new SpriteRenderer[gridWidth, gridHeight];
            hiddenAnimals = new Dictionary<Vector2Int, AnimalData>();

            fogParent = new GameObject("Fog");

            float startX = -(gridWidth - 1) * tileSize / 2f;
            float startY = -(gridHeight - 1) * tileSize / 2f;

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

            // Create fog tiles
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
                    for (int i = 0; i < 256; i++)
                    {
                        fogPixels[i] = currentBiome.FogColor * Random.Range(0.9f, 1.1f);
                    }
                    fogTex.SetPixels(fogPixels);
                    fogTex.Apply();
                    fogTex.filterMode = FilterMode.Point;
                    sr.sprite = Sprite.Create(fogTex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 32);
                    sr.sortingOrder = 1;

                    fogSprites[x, y] = sr;
                    revealedTiles[x, y] = false;
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

            // Check energy
            int energyCost = currentBiome.Difficulty * 5;
            if (GameData.CurrentEnergy < energyCost)
            {
                ShowFeedback("Not enough energy! Press R to refresh biome.", Color.red);
                return;
            }

            GameData.CurrentEnergy -= energyCost;

            // Reveal tiles based on radius and power
            int revealed = 0;
            for (int dx = -GameData.DigRadius; dx <= GameData.DigRadius; dx++)
            {
                for (int dy = -GameData.DigRadius; dy <= GameData.DigRadius; dy++)
                {
                    if (dx * dx + dy * dy <= GameData.DigRadius * GameData.DigRadius)
                    {
                        int tx = gridX + dx;
                        int ty = gridY + dy;

                        if (tx >= 0 && tx < gridWidth && ty >= 0 && ty < gridHeight)
                        {
                            if (!revealedTiles[tx, ty])
                            {
                                RevealTile(tx, ty);
                                revealed++;
                            }
                        }
                    }
                }
            }
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

            // Award gold
            int goldReward = 10 + animal.Rarity * 5;
            GameData.Gold += goldReward;

            int timesFound = GameData.FoundAnimals[animal.Id];
            ShowFeedback($"Found {animal.Name}! (+{goldReward}g) [x{timesFound}]", Color.yellow);

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
