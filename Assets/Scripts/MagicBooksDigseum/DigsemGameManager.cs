using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

namespace MagicBooksDigseum
{
    public enum GameMode
    {
        Digging,
        Shop
    }

    public class DigsemGameManager : MonoBehaviour
    {
        [Header("Game Settings")]
        public int gridWidth = 8;
        public int gridHeight = 6;
        public float tileSize = 1.1f;
        public int treasureCount = 5;

        [Header("Player Stats")]
        public int gold = 0;
        public int digPower = 1;
        public int digPowerCost = 50;

        [Header("Shop Settings")]
        public float visitorSpawnRate = 3f;
        public int maxVisitors = 5;

        private GameMode currentMode = GameMode.Digging;
        private DigTile[,] digGrid;
        private List<MagicBookData> discoveredBooks = new List<MagicBookData>();
        private List<MagicBookData> displayedBooks = new List<MagicBookData>();
        private List<GameObject> displayedBookObjects = new List<GameObject>();
        private List<ShopVisitor> visitors = new List<ShopVisitor>();

        private Camera mainCamera;
        private GameObject digAreaParent;
        private GameObject shopAreaParent;

        private float visitorTimer;

        // UI Elements
        private TextMeshProUGUI goldText;
        private TextMeshProUGUI modeText;
        private TextMeshProUGUI statsText;
        private TextMeshProUGUI inventoryText;
        private Button switchModeButton;
        private Button upgradeButton;
        private Button resetDigButton;
        private TextMeshProUGUI feedbackText;
        private float feedbackTimer;

        void Start()
        {
            mainCamera = Camera.main;
            CreateUI();
            InitializeDigArea();
            InitializeShopArea();
            SetMode(GameMode.Digging);
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

            // Gold display
            goldText = CreateUIText(canvasObj.transform, "GoldText", new Vector2(10, -10), new Vector2(200, 40), "Gold: 0");
            goldText.fontSize = 24;

            // Mode display
            modeText = CreateUIText(canvasObj.transform, "ModeText", new Vector2(10, -50), new Vector2(300, 40), "Mode: Digging");
            modeText.fontSize = 20;

            // Stats
            statsText = CreateUIText(canvasObj.transform, "StatsText", new Vector2(10, -90), new Vector2(300, 60), "Dig Power: 1");
            statsText.fontSize = 16;

            // Inventory
            inventoryText = CreateUIText(canvasObj.transform, "InventoryText", new Vector2(10, -150), new Vector2(350, 200), "Books Found: 0\nBooks Displayed: 0");
            inventoryText.fontSize = 14;

            // Instructions (right side)
            TextMeshProUGUI instructions = CreateUIText(canvasObj.transform, "Instructions", new Vector2(-10, -10), new Vector2(400, 250),
                "MAGIC BOOKS DIGSEUM\n\nDIGGING MODE:\nClick tiles to dig\nFind magic books!\n\nSHOP MODE:\nClick books in inventory to display\nVisitors pay based on collection\n\nSPACE: Switch Mode\nU: Upgrade Dig Power\nR: Reset Dig Site");
            RectTransform instRect = instructions.GetComponent<RectTransform>();
            instRect.anchorMin = new Vector2(1, 1);
            instRect.anchorMax = new Vector2(1, 1);
            instRect.pivot = new Vector2(1, 1);
            instructions.fontSize = 14;

            // Buttons panel (bottom)
            CreateButton(canvasObj.transform, "SwitchModeBtn", new Vector2(100, 50), new Vector2(150, 40), "Switch Mode (Space)", () => SwitchMode());
            CreateButton(canvasObj.transform, "UpgradeBtn", new Vector2(270, 50), new Vector2(150, 40), "Upgrade (U)", () => UpgradeDigPower());
            CreateButton(canvasObj.transform, "ResetBtn", new Vector2(440, 50), new Vector2(150, 40), "Reset Dig (R)", () => ResetDigSite());

            // Feedback text (center of screen)
            feedbackText = CreateUIText(canvasObj.transform, "FeedbackText", Vector2.zero, new Vector2(400, 50), "");
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

        void CreateButton(Transform parent, string name, Vector2 position, Vector2 size, string text, System.Action onClick)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent);

            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(0, 0);
            rect.pivot = new Vector2(0, 0);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.3f, 0.3f, 0.4f);

            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick());

            // Button text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 14;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
        }

        void InitializeDigArea()
        {
            digAreaParent = new GameObject("DigArea");
            digGrid = new DigTile[gridWidth, gridHeight];

            // Calculate positions to center on screen
            float startX = -(gridWidth - 1) * tileSize / 2f;
            float startY = -(gridHeight - 1) * tileSize / 2f + 1f;

            // Place treasure locations
            List<Vector2Int> treasurePositions = new List<Vector2Int>();
            while (treasurePositions.Count < treasureCount)
            {
                Vector2Int pos = new Vector2Int(Random.Range(0, gridWidth), Random.Range(0, gridHeight));
                if (!treasurePositions.Contains(pos))
                {
                    treasurePositions.Add(pos);
                }
            }

            // Create grid
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    Vector2Int gridPos = new Vector2Int(x, y);
                    bool hasTreasure = treasurePositions.Contains(gridPos);
                    MagicBookData book = hasTreasure ? MagicBook.GenerateRandomBook() : null;

                    GameObject tileObj = new GameObject($"Tile_{x}_{y}");
                    tileObj.transform.parent = digAreaParent.transform;
                    tileObj.transform.position = new Vector3(startX + x * tileSize, startY + y * tileSize, 0);

                    DigTile tile = tileObj.AddComponent<DigTile>();
                    int durability = Random.Range(2, 5);
                    tile.Initialize(gridPos, durability, hasTreasure, book);

                    BoxCollider2D collider = tileObj.AddComponent<BoxCollider2D>();
                    collider.size = new Vector2(tileSize * 0.9f, tileSize * 0.9f);

                    digGrid[x, y] = tile;
                }
            }
        }

        void InitializeShopArea()
        {
            shopAreaParent = new GameObject("ShopArea");

            // Create shop floor
            GameObject floor = new GameObject("Floor");
            floor.transform.parent = shopAreaParent.transform;
            SpriteRenderer floorSr = floor.AddComponent<SpriteRenderer>();

            Texture2D floorTex = new Texture2D(320, 200);
            Color[] pixels = new Color[320 * 200];
            for (int i = 0; i < pixels.Length; i++)
            {
                int x = i % 320;
                int y = i / 320;
                bool isWood = ((x / 20) + (y / 20)) % 2 == 0;
                pixels[i] = isWood ? new Color(0.5f, 0.35f, 0.2f) : new Color(0.45f, 0.3f, 0.18f);
            }
            floorTex.SetPixels(pixels);
            floorTex.Apply();
            floorTex.filterMode = FilterMode.Point;

            floorSr.sprite = Sprite.Create(floorTex, new Rect(0, 0, 320, 200), new Vector2(0.5f, 0.5f), 50);
            floorSr.sortingOrder = -1;
            floor.transform.position = new Vector3(0, 0, 0);

            // Create shelves
            for (int i = 0; i < 4; i++)
            {
                CreateShelf(new Vector3(-3f + i * 2f, 1.5f, 0));
            }

            shopAreaParent.SetActive(false);
        }

        void CreateShelf(Vector3 position)
        {
            GameObject shelf = new GameObject("Shelf");
            shelf.transform.parent = shopAreaParent.transform;
            shelf.transform.position = position;

            SpriteRenderer sr = shelf.AddComponent<SpriteRenderer>();

            Texture2D tex = new Texture2D(48, 64);
            Color[] pixels = new Color[48 * 64];
            Color woodColor = new Color(0.4f, 0.25f, 0.15f);

            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 48; x++)
                {
                    bool isFilled = false;

                    // Sides
                    if ((x < 4 || x >= 44) && y < 60)
                    {
                        isFilled = true;
                    }

                    // Shelves
                    if (x >= 4 && x < 44 && (y >= 0 && y < 4 || y >= 28 && y < 32 || y >= 56 && y < 60))
                    {
                        isFilled = true;
                    }

                    pixels[y * 48 + x] = isFilled ? woodColor : Color.clear;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;

            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 48, 64), new Vector2(0.5f, 0f), 48);
            sr.sortingOrder = 0;
        }

        void Update()
        {
            HandleInput();
            UpdateUI();
            UpdateFeedback();

            if (currentMode == GameMode.Shop)
            {
                UpdateShop();
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

        void HandleInput()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;

            // Keyboard shortcuts
            if (keyboard != null)
            {
                if (keyboard.spaceKey.wasPressedThisFrame)
                {
                    SwitchMode();
                }
                if (keyboard.uKey.wasPressedThisFrame)
                {
                    UpgradeDigPower();
                }
                if (keyboard.rKey.wasPressedThisFrame && currentMode == GameMode.Digging)
                {
                    ResetDigSite();
                }
            }

            // Mouse input
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                Vector3 mousePos = mainCamera.ScreenToWorldPoint(mouse.position.ReadValue());
                RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

                if (hit.collider != null)
                {
                    if (currentMode == GameMode.Digging)
                    {
                        DigTile tile = hit.collider.GetComponent<DigTile>();
                        if (tile != null && !tile.IsDug())
                        {
                            MagicBookData foundBook = tile.Dig(digPower);
                            if (foundBook != null)
                            {
                                discoveredBooks.Add(foundBook);
                            }
                        }
                    }
                }
            }

            // Right click in shop to display a book from inventory
            if (mouse != null && mouse.rightButton.wasPressedThisFrame && currentMode == GameMode.Shop)
            {
                TryDisplayNextBook();
            }
        }

        void SwitchMode()
        {
            SetMode(currentMode == GameMode.Digging ? GameMode.Shop : GameMode.Digging);
        }

        void SetMode(GameMode mode)
        {
            currentMode = mode;

            if (mode == GameMode.Digging)
            {
                digAreaParent.SetActive(true);
                shopAreaParent.SetActive(false);
                mainCamera.backgroundColor = new Color(0.3f, 0.25f, 0.2f);
            }
            else
            {
                digAreaParent.SetActive(false);
                shopAreaParent.SetActive(true);
                mainCamera.backgroundColor = new Color(0.4f, 0.35f, 0.3f);
                UpdateShopDisplay();
            }
        }

        void UpdateShop()
        {
            // Spawn visitors based on displayed books
            if (displayedBooks.Count > 0)
            {
                visitorTimer += Time.deltaTime;

                // Clean up null visitors
                visitors.RemoveAll(v => v == null);

                if (visitorTimer >= visitorSpawnRate && visitors.Count < maxVisitors)
                {
                    visitorTimer = 0;
                    SpawnVisitor();
                }
            }
        }

        void SpawnVisitor()
        {
            GameObject visitorObj = new GameObject("Visitor");
            visitorObj.transform.parent = shopAreaParent.transform;
            visitorObj.transform.position = new Vector3(-5f, -1.5f, 0);

            ShopVisitor visitor = visitorObj.AddComponent<ShopVisitor>();

            // Calculate total book value for payment
            int totalValue = 0;
            foreach (var book in displayedBooks)
            {
                totalValue += book.value;
            }
            int payment = Mathf.Max(1, totalValue / 10);

            Vector3 browsePos = new Vector3(Random.Range(-2f, 2f), Random.Range(-1f, 1f), 0);
            visitor.Initialize(browsePos, new Vector3(5f, -1.5f, 0), displayedBooks.Count, (amount) =>
            {
                gold += amount;
            });

            visitors.Add(visitor);
        }

        void TryDisplayNextBook()
        {
            // Find first undisplayed book
            foreach (var book in discoveredBooks)
            {
                if (!book.isDisplayed)
                {
                    DisplayBook(book);
                    break;
                }
            }
        }

        void DisplayBook(MagicBookData bookData)
        {
            bookData.isDisplayed = true;
            displayedBooks.Add(bookData);
            UpdateShopDisplay();
        }

        void UpdateShopDisplay()
        {
            // Clear old display
            foreach (var obj in displayedBookObjects)
            {
                if (obj != null) Destroy(obj);
            }
            displayedBookObjects.Clear();

            // Place books on shelves
            float startX = -3f;
            int index = 0;
            foreach (var bookData in displayedBooks)
            {
                if (index >= 12) break; // Max display

                int shelf = index / 4;
                int slot = index % 4;

                float x = startX + slot * 2f + Random.Range(-0.2f, 0.2f);
                float y = 1.8f + (shelf * 0.9f);

                GameObject bookObj = new GameObject($"DisplayedBook_{index}");
                bookObj.transform.parent = shopAreaParent.transform;
                bookObj.transform.position = new Vector3(x, y, 0);
                bookObj.transform.localScale = Vector3.one * 0.8f;

                MagicBook book = bookObj.AddComponent<MagicBook>();
                book.Initialize(bookData);

                displayedBookObjects.Add(bookObj);
                index++;
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
                ShowFeedback($"Not enough gold! Need {digPowerCost} gold.", Color.red);
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

        void ResetDigSite()
        {
            // Destroy old tiles
            foreach (var tile in digGrid)
            {
                if (tile != null)
                    Destroy(tile.gameObject);
            }

            // Regenerate
            List<Vector2Int> treasurePositions = new List<Vector2Int>();
            int newTreasureCount = treasureCount + discoveredBooks.Count / 3;

            while (treasurePositions.Count < newTreasureCount)
            {
                Vector2Int pos = new Vector2Int(Random.Range(0, gridWidth), Random.Range(0, gridHeight));
                if (!treasurePositions.Contains(pos))
                {
                    treasurePositions.Add(pos);
                }
            }

            float startX = -(gridWidth - 1) * tileSize / 2f;
            float startY = -(gridHeight - 1) * tileSize / 2f + 1f;

            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    Vector2Int gridPos = new Vector2Int(x, y);
                    bool hasTreasure = treasurePositions.Contains(gridPos);
                    MagicBookData book = hasTreasure ? MagicBook.GenerateRandomBook() : null;

                    GameObject tileObj = new GameObject($"Tile_{x}_{y}");
                    tileObj.transform.parent = digAreaParent.transform;
                    tileObj.transform.position = new Vector3(startX + x * tileSize, startY + y * tileSize, 0);

                    DigTile tile = tileObj.AddComponent<DigTile>();
                    int durability = Random.Range(2, 5);
                    tile.Initialize(gridPos, durability, hasTreasure, book);

                    BoxCollider2D collider = tileObj.AddComponent<BoxCollider2D>();
                    collider.size = new Vector2(tileSize * 0.9f, tileSize * 0.9f);

                    digGrid[x, y] = tile;
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
                statsText.text = $"Dig Power: {digPower}\nUpgrade Cost: {digPowerCost} gold";

            if (inventoryText != null)
            {
                int undisplayed = 0;
                foreach (var book in discoveredBooks)
                {
                    if (!book.isDisplayed) undisplayed++;
                }
                inventoryText.text = $"Books Found: {discoveredBooks.Count}\n" +
                                     $"Books Displayed: {displayedBooks.Count}\n" +
                                     $"In Inventory: {undisplayed}\n\n" +
                                     $"Right-Click in Shop to display books!";
            }
        }
    }
}
