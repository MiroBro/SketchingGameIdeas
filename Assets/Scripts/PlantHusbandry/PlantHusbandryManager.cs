using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

namespace PlantHusbandry
{
    public class PlantHusbandryManager : MonoBehaviour
    {
        [Header("Game Settings")]
        public int startingPlants = 3;
        public int newPlantCost = 50;
        public float incomeInterval = 4f;

        [Header("Player Stats")]
        public int gold = 100;

        private Camera mainCamera;
        private List<PlantData> ownedPlants = new List<PlantData>();
        private List<PlantData> displayedPlants = new List<PlantData>();
        private List<GameObject> plantDisplayObjects = new List<GameObject>();

        private PlantData breedParent1;
        private PlantData breedParent2;

        // UI
        private TextMeshProUGUI goldText;
        private TextMeshProUGUI statsText;
        private TextMeshProUGUI plantInfoText;
        private TextMeshProUGUI feedbackText;
        private float feedbackTimer;
        private float incomeTimer;

        private GameObject greenhouseParent;
        private GameObject shopParent;

        private int selectedPlantIndex = -1;

        void Start()
        {
            mainCamera = Camera.main;
            mainCamera.backgroundColor = new Color(0.3f, 0.5f, 0.3f);

            CreateGreenhouse();
            CreateShop();
            CreateUI();
            SpawnStartingPlants();
        }

        void CreateGreenhouse()
        {
            greenhouseParent = new GameObject("Greenhouse");

            // Floor
            GameObject floor = new GameObject("Floor");
            floor.transform.parent = greenhouseParent.transform;
            SpriteRenderer sr = floor.AddComponent<SpriteRenderer>();

            Texture2D tex = new Texture2D(256, 128);
            Color[] pixels = new Color[256 * 128];
            for (int i = 0; i < pixels.Length; i++)
            {
                int x = i % 256;
                int y = i / 256;
                bool tile = ((x / 16) + (y / 16)) % 2 == 0;
                pixels[i] = tile ? new Color(0.6f, 0.5f, 0.4f) : new Color(0.55f, 0.45f, 0.35f);
            }
            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 256, 128), new Vector2(0.5f, 0.5f), 64);
            sr.sortingOrder = -1;

            // Shelves
            for (int i = 0; i < 6; i++)
            {
                float x = -2.5f + (i % 3) * 2.5f;
                float y = -0.5f + (i / 3) * 2f;
                CreateShelf(greenhouseParent.transform, new Vector3(x, y, 0));
            }
        }

        void CreateShelf(Transform parent, Vector3 position)
        {
            GameObject shelf = new GameObject("Shelf");
            shelf.transform.parent = parent;
            shelf.transform.position = position;

            SpriteRenderer sr = shelf.AddComponent<SpriteRenderer>();
            Texture2D tex = new Texture2D(64, 16);
            Color[] pixels = new Color[64 * 16];
            Color woodColor = new Color(0.4f, 0.25f, 0.15f);

            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    if (y >= 10)
                    {
                        pixels[y * 64 + x] = woodColor;
                    }
                    else if (x < 4 || x >= 60)
                    {
                        pixels[y * 64 + x] = woodColor * 0.8f;
                    }
                    else
                    {
                        pixels[y * 64 + x] = Color.clear;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 64, 16), new Vector2(0.5f, 0f), 32);
            sr.sortingOrder = 0;
        }

        void CreateShop()
        {
            shopParent = new GameObject("Shop");
            shopParent.transform.position = new Vector3(6, 0, 0);

            // Shop counter
            GameObject counter = new GameObject("Counter");
            counter.transform.parent = shopParent.transform;
            counter.transform.localPosition = Vector3.zero;

            SpriteRenderer sr = counter.AddComponent<SpriteRenderer>();
            Texture2D tex = new Texture2D(96, 48);
            Color[] pixels = new Color[96 * 48];

            for (int y = 0; y < 48; y++)
            {
                for (int x = 0; x < 96; x++)
                {
                    bool isCounter = y < 30;
                    pixels[y * 96 + x] = isCounter ? new Color(0.5f, 0.35f, 0.2f) : Color.clear;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 96, 48), new Vector2(0.5f, 0f), 48);
            sr.sortingOrder = 0;
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

            goldText = CreateUIText(canvasObj.transform, "GoldText", new Vector2(10, -10), new Vector2(200, 40), "Gold: 100");
            goldText.fontSize = 24;

            statsText = CreateUIText(canvasObj.transform, "StatsText", new Vector2(10, -50), new Vector2(350, 120), "");
            statsText.fontSize = 16;

            plantInfoText = CreateUIText(canvasObj.transform, "PlantInfo", new Vector2(10, -170), new Vector2(400, 300), "");
            plantInfoText.fontSize = 14;

            TextMeshProUGUI instructions = CreateUIText(canvasObj.transform, "Instructions", new Vector2(-10, -10), new Vector2(420, 380),
                "PLANT HUSBANDRY\n\n" +
                "Click plants to select\n" +
                "Click 2 plants then SPACE to breed\n\n" +
                "N: Buy new random plant (50g)\n" +
                "S: Sell selected plant\n" +
                "D: Display selected in shop\n" +
                "R: Remove from display\n\n" +
                "Displayed plants earn passive income!\n" +
                "Rare hybrids = more money!\n\n" +
                "Traits mix from both parents.\n" +
                "Higher generations are more valuable!");
            RectTransform instRect = instructions.GetComponent<RectTransform>();
            instRect.anchorMin = new Vector2(1, 1);
            instRect.anchorMax = new Vector2(1, 1);
            instRect.pivot = new Vector2(1, 1);
            instructions.fontSize = 14;

            feedbackText = CreateUIText(canvasObj.transform, "FeedbackText", Vector2.zero, new Vector2(600, 50), "");
            RectTransform fbRect = feedbackText.GetComponent<RectTransform>();
            fbRect.anchorMin = new Vector2(0.5f, 0.5f);
            fbRect.anchorMax = new Vector2(0.5f, 0.5f);
            fbRect.pivot = new Vector2(0.5f, 0.5f);
            feedbackText.alignment = TextAlignmentOptions.Center;
            feedbackText.fontSize = 26;
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

        void SpawnStartingPlants()
        {
            for (int i = 0; i < startingPlants; i++)
            {
                ownedPlants.Add(PlantData.GenerateRandom());
            }
            UpdatePlantDisplay();
        }

        void Update()
        {
            HandleInput();
            UpdateIncome();
            UpdateUI();
            UpdateFeedback();
        }

        void HandleInput()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;

            if (keyboard != null)
            {
                if (keyboard.nKey.wasPressedThisFrame)
                    BuyNewPlant();
                if (keyboard.sKey.wasPressedThisFrame)
                    SellSelectedPlant();
                if (keyboard.dKey.wasPressedThisFrame)
                    DisplaySelectedPlant();
                if (keyboard.rKey.wasPressedThisFrame)
                    RemoveFromDisplay();
                if (keyboard.spaceKey.wasPressedThisFrame)
                    TryBreed();
            }

            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                Vector3 mousePos = mainCamera.ScreenToWorldPoint(mouse.position.ReadValue());
                RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

                if (hit.collider != null)
                {
                    PlantVisual pv = hit.collider.GetComponent<PlantVisual>();
                    if (pv != null)
                    {
                        SelectPlant(pv.GetData());
                    }
                }
            }
        }

        void SelectPlant(PlantData plant)
        {
            // Find index
            int index = ownedPlants.IndexOf(plant);
            if (index < 0) return;

            selectedPlantIndex = index;

            // Handle breeding selection
            if (breedParent1 == null)
            {
                breedParent1 = plant;
                ShowFeedback($"Selected {plant.name} as Parent 1", Color.cyan);
            }
            else if (breedParent2 == null && plant.id != breedParent1.id)
            {
                breedParent2 = plant;
                ShowFeedback($"Selected {plant.name} as Parent 2. Press SPACE!", Color.cyan);
            }
            else if (plant.id == breedParent1.id || plant.id == breedParent2?.id)
            {
                // Deselect
                breedParent1 = null;
                breedParent2 = null;
                ShowFeedback("Selection cleared", Color.yellow);
            }
            else
            {
                // Start new selection
                breedParent1 = plant;
                breedParent2 = null;
                ShowFeedback($"Selected {plant.name} as Parent 1", Color.cyan);
            }

            UpdatePlantDisplay();
        }

        void TryBreed()
        {
            if (breedParent1 != null && breedParent2 != null)
            {
                PlantData offspring = PlantData.Breed(breedParent1, breedParent2);
                ownedPlants.Add(offspring);
                ShowFeedback($"Created {offspring.name}! (Gen {offspring.generation}, Value: {offspring.value}g)", Color.green);
                breedParent1 = null;
                breedParent2 = null;
                UpdatePlantDisplay();
            }
            else
            {
                ShowFeedback("Select 2 different plants first!", Color.red);
            }
        }

        void BuyNewPlant()
        {
            if (gold >= newPlantCost)
            {
                gold -= newPlantCost;
                PlantData newPlant = PlantData.GenerateRandom();
                ownedPlants.Add(newPlant);
                ShowFeedback($"Bought {newPlant.name}!", Color.green);
                UpdatePlantDisplay();
            }
            else
            {
                ShowFeedback($"Need {newPlantCost} gold!", Color.red);
            }
        }

        void SellSelectedPlant()
        {
            if (selectedPlantIndex < 0 || selectedPlantIndex >= ownedPlants.Count)
            {
                ShowFeedback("Select a plant first!", Color.red);
                return;
            }

            PlantData plant = ownedPlants[selectedPlantIndex];
            if (displayedPlants.Contains(plant))
            {
                displayedPlants.Remove(plant);
            }

            gold += plant.value;
            ShowFeedback($"Sold {plant.name} for {plant.value}g!", Color.green);
            ownedPlants.RemoveAt(selectedPlantIndex);
            selectedPlantIndex = -1;
            breedParent1 = null;
            breedParent2 = null;
            UpdatePlantDisplay();
        }

        void DisplaySelectedPlant()
        {
            if (selectedPlantIndex < 0 || selectedPlantIndex >= ownedPlants.Count)
            {
                ShowFeedback("Select a plant first!", Color.red);
                return;
            }

            PlantData plant = ownedPlants[selectedPlantIndex];
            if (displayedPlants.Contains(plant))
            {
                ShowFeedback("Already displayed!", Color.yellow);
                return;
            }

            if (displayedPlants.Count >= 4)
            {
                ShowFeedback("Shop display is full! (max 4)", Color.yellow);
                return;
            }

            displayedPlants.Add(plant);
            ShowFeedback($"Displaying {plant.name} in shop!", Color.green);
            UpdatePlantDisplay();
        }

        void RemoveFromDisplay()
        {
            if (selectedPlantIndex < 0 || selectedPlantIndex >= ownedPlants.Count)
            {
                ShowFeedback("Select a plant first!", Color.red);
                return;
            }

            PlantData plant = ownedPlants[selectedPlantIndex];
            if (displayedPlants.Contains(plant))
            {
                displayedPlants.Remove(plant);
                ShowFeedback($"Removed {plant.name} from display", Color.yellow);
                UpdatePlantDisplay();
            }
            else
            {
                ShowFeedback("Plant is not displayed!", Color.red);
            }
        }

        void UpdatePlantDisplay()
        {
            // Clear old displays
            foreach (var obj in plantDisplayObjects)
            {
                if (obj != null) Destroy(obj);
            }
            plantDisplayObjects.Clear();

            // Display owned plants in greenhouse
            for (int i = 0; i < ownedPlants.Count; i++)
            {
                float x = -2.5f + (i % 3) * 2.5f;
                float y = (i / 3) * 2f;

                GameObject plantObj = new GameObject($"Plant_{i}");
                plantObj.transform.parent = greenhouseParent.transform;
                plantObj.transform.position = new Vector3(x, y, 0);

                PlantVisual pv = plantObj.AddComponent<PlantVisual>();
                pv.Initialize(ownedPlants[i]);

                BoxCollider2D col = plantObj.AddComponent<BoxCollider2D>();
                col.size = new Vector2(1f, 1.5f);
                col.offset = new Vector2(0, 0.75f);

                // Highlight if selected for breeding
                bool isSelected = (breedParent1 != null && ownedPlants[i].id == breedParent1.id) ||
                                 (breedParent2 != null && ownedPlants[i].id == breedParent2.id);

                if (isSelected)
                {
                    GameObject highlight = new GameObject("Highlight");
                    highlight.transform.parent = plantObj.transform;
                    highlight.transform.localPosition = new Vector3(0, 0.5f, 0);
                    SpriteRenderer hsr = highlight.AddComponent<SpriteRenderer>();
                    Texture2D htex = new Texture2D(4, 4);
                    Color[] hpx = new Color[16];
                    for (int p = 0; p < 16; p++) hpx[p] = new Color(1f, 1f, 0f, 0.3f);
                    htex.SetPixels(hpx);
                    htex.Apply();
                    hsr.sprite = Sprite.Create(htex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
                    hsr.sortingOrder = 4;
                    highlight.transform.localScale = new Vector3(15f, 20f, 1f);
                }

                // Show if displayed
                if (displayedPlants.Contains(ownedPlants[i]))
                {
                    GameObject star = new GameObject("DisplayStar");
                    star.transform.parent = plantObj.transform;
                    star.transform.localPosition = new Vector3(0.3f, 1.2f, 0);
                    TextMeshPro tm = star.AddComponent<TextMeshPro>();
                    tm.text = "*";
                    tm.fontSize = 3;
                    tm.color = Color.yellow;
                    tm.alignment = TextAlignmentOptions.Center;
                    tm.sortingOrder = 10;
                }

                plantDisplayObjects.Add(plantObj);
            }

            // Display plants in shop
            for (int i = 0; i < displayedPlants.Count; i++)
            {
                float x = 5f + i * 0.8f;

                GameObject shopPlant = new GameObject($"ShopPlant_{i}");
                shopPlant.transform.parent = shopParent.transform;
                shopPlant.transform.position = new Vector3(x, 0.5f, 0);
                shopPlant.transform.localScale = Vector3.one * 0.6f;

                PlantVisual pv = shopPlant.AddComponent<PlantVisual>();
                pv.Initialize(displayedPlants[i]);

                plantDisplayObjects.Add(shopPlant);
            }
        }

        void UpdateIncome()
        {
            if (displayedPlants.Count == 0) return;

            incomeTimer += Time.deltaTime;
            if (incomeTimer >= incomeInterval)
            {
                incomeTimer = 0f;
                int totalValue = 0;
                foreach (var plant in displayedPlants)
                {
                    totalValue += plant.value;
                }
                int income = Mathf.Max(1, totalValue / 5);
                gold += income;
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

            if (statsText != null)
            {
                string stats = $"Plants Owned: {ownedPlants.Count}\n";
                stats += $"Displayed: {displayedPlants.Count}/4\n";

                int displayValue = 0;
                foreach (var p in displayedPlants) displayValue += p.value;
                stats += $"Display Value: {displayValue}\n";
                stats += $"Income: ~{Mathf.Max(1, displayValue / 5)}/4s";

                statsText.text = stats;
            }

            if (plantInfoText != null)
            {
                string info = "";
                if (breedParent1 != null)
                {
                    info += $"Parent 1: {breedParent1.name} (Gen {breedParent1.generation})\n";
                }
                if (breedParent2 != null)
                {
                    info += $"Parent 2: {breedParent2.name} (Gen {breedParent2.generation})\n";
                }

                if (selectedPlantIndex >= 0 && selectedPlantIndex < ownedPlants.Count)
                {
                    var p = ownedPlants[selectedPlantIndex];
                    info += $"\nSelected: {p.name}\n";
                    info += $"Generation: {p.generation}\n";
                    info += $"Value: {p.value}g\n";
                    info += $"Stem: {(p.stemHeight > 0.6f ? "Tall" : "Short")}, ";
                    info += $"{(p.stemThickness > 0.5f ? "Thick" : "Thin")}\n";
                    info += $"Leaves: {(p.leafSize > 0.5f ? "Large" : "Small")}, ";
                    info += $"{(int)(2 + p.leafCount * 4)} count\n";
                    info += $"Flower: {(p.flowerSize > 0.5f ? "Big" : "Small")}, ";
                    info += $"{(int)(4 + p.petalCount * 8)} petals\n";
                    if (p.fruitSize > 0.2f) info += "Has fruit!\n";
                    if (p.thorniness > 0.3f) info += "Has thorns!\n";
                }

                plantInfoText.text = info;
            }
        }
    }
}
