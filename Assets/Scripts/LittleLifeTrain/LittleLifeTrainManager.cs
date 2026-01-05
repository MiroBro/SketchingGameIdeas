using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

namespace LittleLifeTrain
{
    public class LittleLifeTrainManager : MonoBehaviour
    {
        [Header("Game Settings")]
        public int npcsToWin = 4;
        public int furniturePerCartToWin = 3;
        public float trainSpeed = 1f;

        [Header("Player Stats")]
        public int gold = 50;

        private Camera mainCamera;
        private GameObject trainParent;
        private GameObject locomotiveObj;
        private List<TrainCart> carts = new List<TrainCart>();
        private List<TrainNPCData> waitingNPCs = new List<TrainNPCData>();

        private int selectedCartIndex = -1;
        private string selectedFurniture = "";

        // Furniture costs
        private Dictionary<string, int> furnitureCosts = new Dictionary<string, int>()
        {
            { "Bed", 30 },
            { "Chair", 15 },
            { "Table", 20 },
            { "Lamp", 10 },
            { "Plant", 8 },
            { "Bookshelf", 25 }
        };

        // UI
        private TextMeshProUGUI goldText;
        private TextMeshProUGUI statusText;
        private TextMeshProUGUI npcInfoText;
        private TextMeshProUGUI instructionsText;
        private TextMeshProUGUI feedbackText;
        private TextMeshProUGUI winText;
        private float feedbackTimer;

        private float backgroundOffset = 0f;
        private GameObject background;
        private float incomeTimer;
        private bool gameWon = false;

        void Start()
        {
            mainCamera = Camera.main;
            mainCamera.backgroundColor = new Color(0.5f, 0.7f, 0.9f);

            CreateBackground();
            CreateTrain();
            SpawnWaitingNPCs();
            CreateUI();
        }

        void CreateBackground()
        {
            background = new GameObject("Background");
            SpriteRenderer sr = background.AddComponent<SpriteRenderer>();

            int width = 512;
            int height = 256;
            Texture2D tex = new Texture2D(width, height);
            Color[] pixels = new Color[width * height];

            Color skyColor = new Color(0.5f, 0.7f, 0.9f);
            Color groundColor = new Color(0.4f, 0.6f, 0.3f);
            Color trackColor = new Color(0.3f, 0.25f, 0.2f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (y < 60)
                    {
                        // Ground
                        pixels[y * width + x] = groundColor;

                        // Track
                        if (y >= 30 && y < 35)
                        {
                            pixels[y * width + x] = trackColor;
                        }
                        // Rails
                        if ((y == 35 || y == 38) && (x % 8 < 6))
                        {
                            pixels[y * width + x] = new Color(0.5f, 0.5f, 0.5f);
                        }
                        // Ties
                        if (y >= 30 && y < 40 && x % 20 < 4)
                        {
                            pixels[y * width + x] = new Color(0.35f, 0.25f, 0.15f);
                        }
                    }
                    else
                    {
                        // Sky with clouds
                        pixels[y * width + x] = skyColor;

                        // Simple clouds
                        if (y > 150 && y < 200)
                        {
                            if ((x + y * 2) % 100 < 30)
                            {
                                pixels[y * width + x] = Color.Lerp(skyColor, Color.white, 0.5f);
                            }
                        }
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Repeat;

            sr.sprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 64);
            sr.sortingOrder = -10;
            background.transform.position = new Vector3(0, 0, 10);
            background.transform.localScale = new Vector3(3, 2, 1);
        }

        void CreateTrain()
        {
            trainParent = new GameObject("Train");
            trainParent.transform.position = new Vector3(-3, -1.5f, 0);

            // Create locomotive
            locomotiveObj = new GameObject("Locomotive");
            locomotiveObj.transform.parent = trainParent.transform;
            locomotiveObj.transform.localPosition = Vector3.zero;

            SpriteRenderer locoSr = locomotiveObj.AddComponent<SpriteRenderer>();

            int width = 96;
            int height = 64;
            Texture2D tex = new Texture2D(width, height);
            Color[] pixels = new Color[width * height];

            Color bodyColor = new Color(0.2f, 0.3f, 0.5f);
            Color accentColor = new Color(0.8f, 0.2f, 0.2f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color c = Color.clear;

                    // Main body
                    if (y >= 15 && y < 50 && x >= 20 && x < 90)
                    {
                        c = bodyColor;
                    }

                    // Cabin
                    if (y >= 20 && y < 55 && x >= 10 && x < 35)
                    {
                        c = bodyColor * 1.1f;
                        // Window
                        if (y >= 35 && y < 50 && x >= 15 && x < 30)
                        {
                            c = new Color(0.7f, 0.85f, 1f);
                        }
                    }

                    // Boiler (front cylinder)
                    float boilerDist = Mathf.Sqrt(Mathf.Pow(x - 65, 2) + Mathf.Pow(y - 32, 2));
                    if (boilerDist < 18 && x >= 50)
                    {
                        c = bodyColor * 0.9f;
                    }

                    // Smokestack
                    if (y >= 50 && y < 62 && x >= 70 && x < 80)
                    {
                        c = accentColor;
                    }

                    // Wheels
                    float wheel1 = Mathf.Sqrt(Mathf.Pow(x - 25, 2) + Mathf.Pow(y - 10, 2));
                    float wheel2 = Mathf.Sqrt(Mathf.Pow(x - 55, 2) + Mathf.Pow(y - 10, 2));
                    float wheel3 = Mathf.Sqrt(Mathf.Pow(x - 80, 2) + Mathf.Pow(y - 10, 2));
                    if (wheel1 < 8 || wheel2 < 8 || wheel3 < 8)
                    {
                        c = new Color(0.15f, 0.15f, 0.15f);
                    }

                    // Cowcatcher
                    if (y >= 5 && y < 20 && x >= 85 && x < 96)
                    {
                        if ((y + x) % 4 < 2)
                        {
                            c = accentColor;
                        }
                    }

                    pixels[y * width + x] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;

            locoSr.sprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(1f, 0f), 32);
            locoSr.sortingOrder = 1;
        }

        void SpawnWaitingNPCs()
        {
            for (int i = 0; i < 6; i++)
            {
                waitingNPCs.Add(TrainNPCData.GenerateRandom());
            }
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

            goldText = CreateUIText(canvasObj.transform, "GoldText", new Vector2(10, -10), new Vector2(200, 40), "Gold: 50");
            goldText.fontSize = 24;

            statusText = CreateUIText(canvasObj.transform, "StatusText", new Vector2(10, -50), new Vector2(400, 80), "");
            statusText.fontSize = 16;

            npcInfoText = CreateUIText(canvasObj.transform, "NPCInfo", new Vector2(10, -130), new Vector2(400, 200), "");
            npcInfoText.fontSize = 14;

            instructionsText = CreateUIText(canvasObj.transform, "Instructions", new Vector2(-10, -10), new Vector2(400, 350),
                "LITTLE LIFE TRAIN\n\n" +
                "Goal: Pick up 4 NPCs and decorate\neach cart with 3+ furniture!\n\n" +
                "SPACE: Pick up waiting NPC\n" +
                "1-6: Select furniture to place\n" +
                "Left/Right: Select cart\n" +
                "ENTER: Place furniture in cart\n\n" +
                "Furniture:\n" +
                "1-Bed(30) 2-Chair(15) 3-Table(20)\n" +
                "4-Lamp(10) 5-Plant(8) 6-Bookshelf(25)");
            RectTransform instRect = instructionsText.GetComponent<RectTransform>();
            instRect.anchorMin = new Vector2(1, 1);
            instRect.anchorMax = new Vector2(1, 1);
            instRect.pivot = new Vector2(1, 1);
            instructionsText.fontSize = 14;

            feedbackText = CreateUIText(canvasObj.transform, "FeedbackText", Vector2.zero, new Vector2(600, 50), "");
            RectTransform fbRect = feedbackText.GetComponent<RectTransform>();
            fbRect.anchorMin = new Vector2(0.5f, 0.5f);
            fbRect.anchorMax = new Vector2(0.5f, 0.5f);
            fbRect.pivot = new Vector2(0.5f, 0.5f);
            feedbackText.alignment = TextAlignmentOptions.Center;
            feedbackText.fontSize = 28;

            winText = CreateUIText(canvasObj.transform, "WinText", Vector2.zero, new Vector2(600, 200), "");
            RectTransform winRect = winText.GetComponent<RectTransform>();
            winRect.anchorMin = new Vector2(0.5f, 0.5f);
            winRect.anchorMax = new Vector2(0.5f, 0.5f);
            winRect.pivot = new Vector2(0.5f, 0.5f);
            winText.alignment = TextAlignmentOptions.Center;
            winText.fontSize = 36;
            winText.gameObject.SetActive(false);
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

        void Update()
        {
            if (gameWon) return;

            HandleInput();
            UpdateTrain();
            UpdateIncome();
            UpdateUI();
            UpdateFeedback();
            CheckWinCondition();
        }

        void HandleInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            // Pick up NPC
            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                PickUpNPC();
            }

            // Select cart
            if (keyboard.leftArrowKey.wasPressedThisFrame)
            {
                selectedCartIndex = Mathf.Max(0, selectedCartIndex - 1);
                if (carts.Count == 0) selectedCartIndex = -1;
            }
            if (keyboard.rightArrowKey.wasPressedThisFrame)
            {
                selectedCartIndex = Mathf.Min(carts.Count - 1, selectedCartIndex + 1);
            }

            // Select furniture
            if (keyboard.digit1Key.wasPressedThisFrame) selectedFurniture = "Bed";
            if (keyboard.digit2Key.wasPressedThisFrame) selectedFurniture = "Chair";
            if (keyboard.digit3Key.wasPressedThisFrame) selectedFurniture = "Table";
            if (keyboard.digit4Key.wasPressedThisFrame) selectedFurniture = "Lamp";
            if (keyboard.digit5Key.wasPressedThisFrame) selectedFurniture = "Plant";
            if (keyboard.digit6Key.wasPressedThisFrame) selectedFurniture = "Bookshelf";

            // Place furniture
            if (keyboard.enterKey.wasPressedThisFrame)
            {
                PlaceFurniture();
            }
        }

        void PickUpNPC()
        {
            if (waitingNPCs.Count == 0)
            {
                ShowFeedback("No NPCs waiting!", Color.yellow);
                return;
            }

            if (carts.Count >= npcsToWin)
            {
                ShowFeedback("Train is full!", Color.yellow);
                return;
            }

            TrainNPCData npc = waitingNPCs[0];
            waitingNPCs.RemoveAt(0);

            // Create new cart
            GameObject cartObj = new GameObject($"Cart_{carts.Count}");
            cartObj.transform.parent = trainParent.transform;
            float cartX = -2.5f * (carts.Count + 1);
            cartObj.transform.localPosition = new Vector3(cartX, 0, 0);

            TrainCart cart = cartObj.AddComponent<TrainCart>();
            cart.Initialize(npc, carts.Count);
            carts.Add(cart);

            selectedCartIndex = carts.Count - 1;

            ShowFeedback($"Picked up {npc.name}!", Color.green);
        }

        void PlaceFurniture()
        {
            if (selectedCartIndex < 0 || selectedCartIndex >= carts.Count)
            {
                ShowFeedback("Select a cart first!", Color.red);
                return;
            }

            if (string.IsNullOrEmpty(selectedFurniture))
            {
                ShowFeedback("Select furniture first (1-6)!", Color.red);
                return;
            }

            int cost = furnitureCosts[selectedFurniture];
            if (gold < cost)
            {
                ShowFeedback($"Need {cost} gold for {selectedFurniture}!", Color.red);
                return;
            }

            TrainCart cart = carts[selectedCartIndex];
            if (cart.GetFurnitureCount() >= 6)
            {
                ShowFeedback("Cart is full!", Color.yellow);
                return;
            }

            gold -= cost;

            // Random position in cart
            float fx = Random.Range(-0.8f, 0.5f);
            float fy = 0.4f;
            cart.AddFurniture(selectedFurniture, new Vector3(fx, fy, 0));

            ShowFeedback($"Placed {selectedFurniture} in {cart.GetOwner().name}'s cart!", Color.green);
        }

        void UpdateTrain()
        {
            // Animate background scrolling
            backgroundOffset += trainSpeed * Time.deltaTime * 0.1f;
            if (background != null)
            {
                Material mat = background.GetComponent<SpriteRenderer>().material;
                // Simple parallax effect by moving background
            }

            // Bob the train slightly
            if (trainParent != null)
            {
                float bob = Mathf.Sin(Time.time * 3f) * 0.02f;
                trainParent.transform.position = new Vector3(-3, -1.5f + bob, 0);
            }
        }

        void UpdateIncome()
        {
            if (carts.Count == 0) return;

            incomeTimer += Time.deltaTime;
            if (incomeTimer >= 3f)
            {
                incomeTimer = 0f;
                int totalIncome = 0;
                foreach (var cart in carts)
                {
                    totalIncome += cart.CalculateIncome();
                }
                gold += totalIncome;
            }
        }

        void CheckWinCondition()
        {
            if (carts.Count < npcsToWin) return;

            bool allDecorated = true;
            foreach (var cart in carts)
            {
                if (cart.GetFurnitureCount() < furniturePerCartToWin)
                {
                    allDecorated = false;
                    break;
                }
            }

            if (allDecorated)
            {
                gameWon = true;
                winText.text = "YOU WIN!\n\nAll passengers are happy!\nYour train journey continues...";
                winText.color = Color.yellow;
                winText.gameObject.SetActive(true);
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
                {
                    feedbackText.text = "";
                }
            }
        }

        void UpdateUI()
        {
            if (goldText != null)
                goldText.text = $"Gold: {gold}";

            if (statusText != null)
            {
                string status = $"Passengers: {carts.Count}/{npcsToWin}\n";
                status += $"Selected Cart: {(selectedCartIndex >= 0 ? (selectedCartIndex + 1).ToString() : "None")}\n";
                status += $"Selected Furniture: {(string.IsNullOrEmpty(selectedFurniture) ? "None" : selectedFurniture)}";
                if (!string.IsNullOrEmpty(selectedFurniture))
                {
                    status += $" ({furnitureCosts[selectedFurniture]}g)";
                }
                statusText.text = status;
            }

            if (npcInfoText != null)
            {
                string info = "";
                if (waitingNPCs.Count > 0)
                {
                    info += $"Next NPC: {waitingNPCs[0].name}\n";
                    info += "Likes: ";
                    foreach (var pref in waitingNPCs[0].preferences)
                    {
                        info += $"{pref.furnitureName} ";
                    }
                    info += "\n\n";
                }

                if (selectedCartIndex >= 0 && selectedCartIndex < carts.Count)
                {
                    var cart = carts[selectedCartIndex];
                    var owner = cart.GetOwner();
                    info += $"Cart {selectedCartIndex + 1}: {owner.name}\n";
                    info += $"Furniture: {cart.GetFurnitureCount()}/{furniturePerCartToWin} needed\n";
                    info += $"Income: {cart.CalculateIncome()}/3s\n";
                    info += "Likes: ";
                    foreach (var pref in owner.preferences)
                    {
                        info += $"{pref.furnitureName} ";
                    }
                }

                npcInfoText.text = info;
            }
        }
    }
}
