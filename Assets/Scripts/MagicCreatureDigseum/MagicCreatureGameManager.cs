using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

namespace MagicCreatureDigseum
{
    public enum CreatureGameMode
    {
        Digging,
        Breeding,
        Zoo
    }

    public class MagicCreatureGameManager : MonoBehaviour
    {
        [Header("Game Settings")]
        public int gridWidth = 6;
        public int gridHeight = 5;
        public float tileSize = 1.2f;
        public int creatureCount = 4;

        [Header("Player Stats")]
        public int gold = 0;
        public int digPower = 1;
        public int digPowerCost = 50;

        private CreatureGameMode currentMode = CreatureGameMode.Digging;
        private DigTileCreature[,] digGrid;
        private List<CreatureData> discoveredCreatures = new List<CreatureData>();
        private List<CreatureData> zooCreatures = new List<CreatureData>();
        private List<GameObject> zooCreatureObjects = new List<GameObject>();

        // Breeding
        private CreatureData breedParent1;
        private CreatureData breedParent2;
        private List<GameObject> breedingDisplayObjects = new List<GameObject>();

        private Camera mainCamera;
        private GameObject digAreaParent;
        private GameObject breedingAreaParent;
        private GameObject zooAreaParent;

        // UI
        private TextMeshProUGUI goldText;
        private TextMeshProUGUI modeText;
        private TextMeshProUGUI statsText;
        private TextMeshProUGUI feedbackText;
        private float feedbackTimer;

        // Zoo income
        private float incomeTimer;
        private float incomeInterval = 3f;

        void Start()
        {
            mainCamera = Camera.main;
            CreateUI();
            InitializeDigArea();
            InitializeBreedingArea();
            InitializeZooArea();
            SetMode(CreatureGameMode.Digging);
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

            goldText = CreateUIText(canvasObj.transform, "GoldText", new Vector2(10, -10), new Vector2(200, 40), "Gold: 0");
            goldText.fontSize = 24;

            modeText = CreateUIText(canvasObj.transform, "ModeText", new Vector2(10, -50), new Vector2(300, 40), "Mode: Digging");
            modeText.fontSize = 20;

            statsText = CreateUIText(canvasObj.transform, "StatsText", new Vector2(10, -90), new Vector2(400, 150), "");
            statsText.fontSize = 14;

            // Instructions
            TextMeshProUGUI instructions = CreateUIText(canvasObj.transform, "Instructions", new Vector2(-10, -10), new Vector2(400, 300),
                "MAGIC CREATURE DIGSEUM\n\n" +
                "1: Dig Mode - Find creatures\n" +
                "2: Breed Mode - Combine 2 creatures\n" +
                "3: Zoo Mode - Display & earn gold\n\n" +
                "Click tiles to dig\n" +
                "In Breed: Click 2 creatures, then SPACE\n" +
                "U: Upgrade dig power\n" +
                "R: Reset dig site");
            RectTransform instRect = instructions.GetComponent<RectTransform>();
            instRect.anchorMin = new Vector2(1, 1);
            instRect.anchorMax = new Vector2(1, 1);
            instRect.pivot = new Vector2(1, 1);
            instructions.fontSize = 14;

            feedbackText = CreateUIText(canvasObj.transform, "FeedbackText", Vector2.zero, new Vector2(500, 50), "");
            RectTransform fbRect = feedbackText.GetComponent<RectTransform>();
            fbRect.anchorMin = new Vector2(0.5f, 0.5f);
            fbRect.anchorMax = new Vector2(0.5f, 0.5f);
            fbRect.pivot = new Vector2(0.5f, 0.5f);
            feedbackText.alignment = TextAlignmentOptions.Center;
            feedbackText.fontSize = 24;
        }

        TextMeshProUGUI CreateUIText(Transform parent, string name, Vector2 position, Vector2 size, string text)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent);

            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 20;
            tmp.color = Color.white;

            return tmp;
        }

        void InitializeDigArea()
        {
            digAreaParent = new GameObject("DigArea");
            digGrid = new DigTileCreature[gridWidth, gridHeight];

            float startX = -(gridWidth - 1) * tileSize / 2f;
            float startY = -(gridHeight - 1) * tileSize / 2f;

            List<Vector2Int> creaturePositions = new List<Vector2Int>();
            while (creaturePositions.Count < creatureCount)
            {
                Vector2Int pos = new Vector2Int(Random.Range(0, gridWidth), Random.Range(0, gridHeight));
                if (!creaturePositions.Contains(pos))
                {
                    creaturePositions.Add(pos);
                }
            }

            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    Vector2Int gridPos = new Vector2Int(x, y);
                    bool hasCreature = creaturePositions.Contains(gridPos);
                    CreatureData creature = hasCreature ? CreatureData.GenerateRandom() : null;

                    GameObject tileObj = new GameObject($"Tile_{x}_{y}");
                    tileObj.transform.parent = digAreaParent.transform;
                    tileObj.transform.position = new Vector3(startX + x * tileSize, startY + y * tileSize, 0);

                    DigTileCreature tile = tileObj.AddComponent<DigTileCreature>();
                    int durability = Random.Range(2, 5);
                    tile.Initialize(gridPos, durability, hasCreature, creature);

                    BoxCollider2D collider = tileObj.AddComponent<BoxCollider2D>();
                    collider.size = new Vector2(tileSize * 0.9f, tileSize * 0.9f);

                    digGrid[x, y] = tile;
                }
            }
        }

        void InitializeBreedingArea()
        {
            breedingAreaParent = new GameObject("BreedingArea");

            // Create breeding platform
            GameObject platform = new GameObject("Platform");
            platform.transform.parent = breedingAreaParent.transform;
            SpriteRenderer sr = platform.AddComponent<SpriteRenderer>();

            Texture2D tex = new Texture2D(200, 100);
            Color[] pixels = new Color[200 * 100];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color(0.3f, 0.2f, 0.4f);
            }
            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 200, 100), new Vector2(0.5f, 0.5f), 50);
            sr.sortingOrder = -1;

            breedingAreaParent.SetActive(false);
        }

        void InitializeZooArea()
        {
            zooAreaParent = new GameObject("ZooArea");

            // Create zoo ground
            GameObject ground = new GameObject("Ground");
            ground.transform.parent = zooAreaParent.transform;
            SpriteRenderer sr = ground.AddComponent<SpriteRenderer>();

            Texture2D tex = new Texture2D(300, 200);
            Color[] pixels = new Color[300 * 200];
            for (int i = 0; i < pixels.Length; i++)
            {
                int x = i % 300;
                int y = i / 300;
                bool isGrass = ((x / 20) + (y / 20)) % 2 == 0;
                pixels[i] = isGrass ? new Color(0.2f, 0.5f, 0.2f) : new Color(0.25f, 0.55f, 0.25f);
            }
            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 300, 200), new Vector2(0.5f, 0.5f), 50);
            sr.sortingOrder = -1;

            // Create enclosures
            for (int i = 0; i < 6; i++)
            {
                float x = -2f + (i % 3) * 2f;
                float y = -0.5f + (i / 3) * 2f;
                CreateEnclosure(new Vector3(x, y, 0));
            }

            zooAreaParent.SetActive(false);
        }

        void CreateEnclosure(Vector3 position)
        {
            GameObject enclosure = new GameObject("Enclosure");
            enclosure.transform.parent = zooAreaParent.transform;
            enclosure.transform.position = position;

            SpriteRenderer sr = enclosure.AddComponent<SpriteRenderer>();
            Texture2D tex = new Texture2D(48, 48);
            Color[] pixels = new Color[48 * 48];
            Color fenceColor = new Color(0.4f, 0.3f, 0.2f);

            for (int y = 0; y < 48; y++)
            {
                for (int x = 0; x < 48; x++)
                {
                    bool isFence = x < 3 || x >= 45 || y < 3 || y >= 45;
                    pixels[y * 48 + x] = isFence ? fenceColor : new Color(0.3f, 0.6f, 0.3f, 0.5f);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 48, 48), new Vector2(0.5f, 0.5f), 32);
            sr.sortingOrder = 0;
        }

        void Update()
        {
            HandleInput();
            UpdateUI();
            UpdateFeedback();

            if (currentMode == CreatureGameMode.Zoo)
            {
                UpdateZooIncome();
            }
        }

        void HandleInput()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;

            if (keyboard != null)
            {
                if (keyboard.digit1Key.wasPressedThisFrame)
                    SetMode(CreatureGameMode.Digging);
                if (keyboard.digit2Key.wasPressedThisFrame)
                    SetMode(CreatureGameMode.Breeding);
                if (keyboard.digit3Key.wasPressedThisFrame)
                    SetMode(CreatureGameMode.Zoo);

                if (keyboard.uKey.wasPressedThisFrame)
                    UpgradeDigPower();
                if (keyboard.rKey.wasPressedThisFrame && currentMode == CreatureGameMode.Digging)
                    ResetDigSite();

                if (keyboard.spaceKey.wasPressedThisFrame && currentMode == CreatureGameMode.Breeding)
                    TryBreed();
            }

            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                Vector3 mousePos = mainCamera.ScreenToWorldPoint(mouse.position.ReadValue());
                RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

                if (hit.collider != null)
                {
                    if (currentMode == CreatureGameMode.Digging)
                    {
                        DigTileCreature tile = hit.collider.GetComponent<DigTileCreature>();
                        if (tile != null && !tile.IsDug())
                        {
                            CreatureData found = tile.Dig(digPower);
                            if (found != null)
                            {
                                discoveredCreatures.Add(found);
                                ShowFeedback($"Found {found.name}!", Color.green);
                            }
                        }
                    }
                    else if (currentMode == CreatureGameMode.Breeding)
                    {
                        CreatureVisual cv = hit.collider.GetComponent<CreatureVisual>();
                        if (cv != null)
                        {
                            SelectForBreeding(cv.GetData());
                        }
                    }
                }
            }

            // Right click in zoo to add creature
            if (mouse != null && mouse.rightButton.wasPressedThisFrame && currentMode == CreatureGameMode.Zoo)
            {
                AddCreatureToZoo();
            }
        }

        void SetMode(CreatureGameMode mode)
        {
            currentMode = mode;

            digAreaParent.SetActive(mode == CreatureGameMode.Digging);
            breedingAreaParent.SetActive(mode == CreatureGameMode.Breeding);
            zooAreaParent.SetActive(mode == CreatureGameMode.Zoo);

            if (mode == CreatureGameMode.Digging)
            {
                mainCamera.backgroundColor = new Color(0.3f, 0.25f, 0.2f);
            }
            else if (mode == CreatureGameMode.Breeding)
            {
                mainCamera.backgroundColor = new Color(0.2f, 0.15f, 0.3f);
                breedParent1 = null;
                breedParent2 = null;
                UpdateBreedingDisplay();
            }
            else
            {
                mainCamera.backgroundColor = new Color(0.15f, 0.3f, 0.2f);
                UpdateZooDisplay();
            }
        }

        void SelectForBreeding(CreatureData creature)
        {
            if (breedParent1 == null)
            {
                breedParent1 = creature;
                ShowFeedback($"Selected {creature.name} as Parent 1", Color.cyan);
            }
            else if (breedParent2 == null && creature.id != breedParent1.id)
            {
                breedParent2 = creature;
                ShowFeedback($"Selected {creature.name} as Parent 2. Press SPACE to breed!", Color.cyan);
            }
            UpdateBreedingDisplay();
        }

        void UpdateBreedingDisplay()
        {
            foreach (var obj in breedingDisplayObjects)
            {
                if (obj != null) Destroy(obj);
            }
            breedingDisplayObjects.Clear();

            // Show all discovered creatures
            float startX = -3f;
            for (int i = 0; i < discoveredCreatures.Count; i++)
            {
                float x = startX + (i % 6) * 1.2f;
                float y = -1.5f + (i / 6) * 1.5f;

                GameObject creatureObj = new GameObject($"Creature_{i}");
                creatureObj.transform.parent = breedingAreaParent.transform;
                creatureObj.transform.position = new Vector3(x, y, 0);

                CreatureVisual cv = creatureObj.AddComponent<CreatureVisual>();
                cv.Initialize(discoveredCreatures[i]);

                BoxCollider2D col = creatureObj.AddComponent<BoxCollider2D>();
                col.size = new Vector2(1f, 1f);

                // Highlight selected
                if ((breedParent1 != null && discoveredCreatures[i].id == breedParent1.id) ||
                    (breedParent2 != null && discoveredCreatures[i].id == breedParent2.id))
                {
                    GameObject highlight = new GameObject("Highlight");
                    highlight.transform.parent = creatureObj.transform;
                    highlight.transform.localPosition = Vector3.zero;
                    SpriteRenderer hsr = highlight.AddComponent<SpriteRenderer>();
                    Texture2D htex = new Texture2D(4, 4);
                    Color[] hpx = new Color[16];
                    for (int p = 0; p < 16; p++) hpx[p] = new Color(1f, 1f, 0f, 0.3f);
                    htex.SetPixels(hpx);
                    htex.Apply();
                    hsr.sprite = Sprite.Create(htex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
                    hsr.sortingOrder = 4;
                    highlight.transform.localScale = new Vector3(15f, 15f, 1f);
                }

                breedingDisplayObjects.Add(creatureObj);
            }
        }

        void TryBreed()
        {
            if (breedParent1 != null && breedParent2 != null)
            {
                CreatureData offspring = CreatureData.Breed(breedParent1, breedParent2);
                discoveredCreatures.Add(offspring);
                ShowFeedback($"Created {offspring.name}! (Gen {offspring.generation})", Color.magenta);
                breedParent1 = null;
                breedParent2 = null;
                UpdateBreedingDisplay();
            }
            else
            {
                ShowFeedback("Select 2 different creatures first!", Color.red);
            }
        }

        void AddCreatureToZoo()
        {
            // Find first creature not in zoo
            foreach (var creature in discoveredCreatures)
            {
                if (!zooCreatures.Contains(creature))
                {
                    zooCreatures.Add(creature);
                    ShowFeedback($"Added {creature.name} to zoo!", Color.green);
                    UpdateZooDisplay();
                    return;
                }
            }
            ShowFeedback("No more creatures to add!", Color.yellow);
        }

        void UpdateZooDisplay()
        {
            foreach (var obj in zooCreatureObjects)
            {
                if (obj != null) Destroy(obj);
            }
            zooCreatureObjects.Clear();

            for (int i = 0; i < zooCreatures.Count && i < 6; i++)
            {
                float x = -2f + (i % 3) * 2f;
                float y = -0.5f + (i / 3) * 2f;

                GameObject creatureObj = new GameObject($"ZooCreature_{i}");
                creatureObj.transform.parent = zooAreaParent.transform;
                creatureObj.transform.position = new Vector3(x, y, 0);
                creatureObj.transform.localScale = Vector3.one * 0.8f;

                CreatureVisual cv = creatureObj.AddComponent<CreatureVisual>();
                cv.Initialize(zooCreatures[i]);

                zooCreatureObjects.Add(creatureObj);
            }
        }

        void UpdateZooIncome()
        {
            if (zooCreatures.Count == 0) return;

            incomeTimer += Time.deltaTime;
            if (incomeTimer >= incomeInterval)
            {
                incomeTimer = 0f;
                int totalValue = 0;
                foreach (var creature in zooCreatures)
                {
                    totalValue += creature.value;
                }
                int income = Mathf.Max(1, totalValue / 10);
                gold += income;
            }
        }

        void UpgradeDigPower()
        {
            if (gold >= digPowerCost)
            {
                gold -= digPowerCost;
                digPower++;
                digPowerCost = Mathf.RoundToInt(digPowerCost * 1.5f);
                ShowFeedback($"Dig Power upgraded to {digPower}!", Color.green);
            }
            else
            {
                ShowFeedback($"Not enough gold! Need {digPowerCost}", Color.red);
            }
        }

        void ResetDigSite()
        {
            foreach (var tile in digGrid)
            {
                if (tile != null) Destroy(tile.gameObject);
            }

            List<Vector2Int> creaturePositions = new List<Vector2Int>();
            int newCount = creatureCount + discoveredCreatures.Count / 2;

            while (creaturePositions.Count < newCount && creaturePositions.Count < gridWidth * gridHeight)
            {
                Vector2Int pos = new Vector2Int(Random.Range(0, gridWidth), Random.Range(0, gridHeight));
                if (!creaturePositions.Contains(pos))
                {
                    creaturePositions.Add(pos);
                }
            }

            float startX = -(gridWidth - 1) * tileSize / 2f;
            float startY = -(gridHeight - 1) * tileSize / 2f;

            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    Vector2Int gridPos = new Vector2Int(x, y);
                    bool hasCreature = creaturePositions.Contains(gridPos);
                    CreatureData creature = hasCreature ? CreatureData.GenerateRandom() : null;

                    GameObject tileObj = new GameObject($"Tile_{x}_{y}");
                    tileObj.transform.parent = digAreaParent.transform;
                    tileObj.transform.position = new Vector3(startX + x * tileSize, startY + y * tileSize, 0);

                    DigTileCreature tile = tileObj.AddComponent<DigTileCreature>();
                    int durability = Random.Range(2, 5);
                    tile.Initialize(gridPos, durability, hasCreature, creature);

                    BoxCollider2D collider = tileObj.AddComponent<BoxCollider2D>();
                    collider.size = new Vector2(tileSize * 0.9f, tileSize * 0.9f);

                    digGrid[x, y] = tile;
                }
            }

            ShowFeedback("Dig site reset!", Color.cyan);
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
                goldText.text = $"Gold: {gold}";

            if (modeText != null)
                modeText.text = $"Mode: {currentMode}";

            if (statsText != null)
            {
                string stats = $"Dig Power: {digPower} (Upgrade: {digPowerCost}g)\n";
                stats += $"Creatures Found: {discoveredCreatures.Count}\n";
                stats += $"In Zoo: {zooCreatures.Count}\n\n";

                if (currentMode == CreatureGameMode.Breeding)
                {
                    stats += $"Parent 1: {(breedParent1 != null ? breedParent1.name : "None")}\n";
                    stats += $"Parent 2: {(breedParent2 != null ? breedParent2.name : "None")}";
                }
                else if (currentMode == CreatureGameMode.Zoo)
                {
                    int totalValue = 0;
                    foreach (var c in zooCreatures) totalValue += c.value;
                    stats += $"Zoo Value: {totalValue}\n";
                    stats += "Right-click to add creatures!";
                }

                statsText.text = stats;
            }
        }
    }
}
