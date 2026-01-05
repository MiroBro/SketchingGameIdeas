using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

namespace SpaceTowerWizard
{
    public class SpaceTowerGameManager : MonoBehaviour
    {
        [Header("Resources")]
        public float energy = 100;
        public float minerals = 0;
        public float credits = 0;
        public float techPoints = 0;

        [Header("Settings")]
        public float baseProductionRate = 1f;
        public float clickPower = 5f;

        private List<BuildingData> buildings = new List<BuildingData>();
        private List<SpaceBuilding> buildingObjects = new List<SpaceBuilding>();
        private bool gameWon = false;

        // UI References
        private TextMeshProUGUI energyText;
        private TextMeshProUGUI mineralsText;
        private TextMeshProUGUI creditsText;
        private TextMeshProUGUI techPointsText;
        private TextMeshProUGUI selectedBuildingText;
        private TextMeshProUGUI gameMessageText;
        private GameObject winPanel;

        private int selectedBuildingIndex = -1;
        private Camera mainCamera;

        void Start()
        {
            mainCamera = Camera.main;
            InitializeBuildings();
            CreateUI();
            SpawnBuildings();
        }

        void InitializeBuildings()
        {
            buildings = new List<BuildingData>
            {
                new BuildingData
                {
                    name = "Solar Panel",
                    description = "Generates Energy",
                    baseCost = 10,
                    baseProduction = 1f,
                    producesResource = ResourceType.Energy,
                    costResource = ResourceType.Energy,
                    unlockRequirement = 0,
                    buildingColor = new Color(1f, 0.9f, 0.2f),
                    isUnlocked = true,
                    level = 1
                },
                new BuildingData
                {
                    name = "Mining Drone",
                    description = "Extracts Minerals",
                    baseCost = 50,
                    baseProduction = 0.5f,
                    producesResource = ResourceType.Minerals,
                    costResource = ResourceType.Energy,
                    unlockRequirement = 20,
                    buildingColor = new Color(0.6f, 0.4f, 0.2f),
                    isUnlocked = false,
                    level = 0
                },
                new BuildingData
                {
                    name = "Trading Post",
                    description = "Generates Credits",
                    baseCost = 100,
                    baseProduction = 0.3f,
                    producesResource = ResourceType.Credits,
                    costResource = ResourceType.Minerals,
                    unlockRequirement = 50,
                    buildingColor = new Color(0.2f, 0.8f, 0.2f),
                    isUnlocked = false,
                    level = 0
                },
                new BuildingData
                {
                    name = "Research Lab",
                    description = "Produces Tech Points",
                    baseCost = 200,
                    baseProduction = 0.2f,
                    producesResource = ResourceType.TechPoints,
                    costResource = ResourceType.Credits,
                    unlockRequirement = 100,
                    buildingColor = new Color(0.4f, 0.4f, 1f),
                    isUnlocked = false,
                    level = 0
                },
                new BuildingData
                {
                    name = "Space Dock",
                    description = "Advanced production",
                    baseCost = 500,
                    baseProduction = 1f,
                    producesResource = ResourceType.Credits,
                    costResource = ResourceType.TechPoints,
                    unlockRequirement = 50,
                    buildingColor = new Color(0.8f, 0.4f, 0.8f),
                    isUnlocked = false,
                    level = 0
                },
                new BuildingData
                {
                    name = "Warp Core",
                    description = "Powers the hyperspace engine",
                    baseCost = 1000,
                    baseProduction = 2f,
                    producesResource = ResourceType.TechPoints,
                    costResource = ResourceType.TechPoints,
                    unlockRequirement = 200,
                    buildingColor = new Color(0.2f, 0.9f, 0.9f),
                    isUnlocked = false,
                    level = 0
                },
                new BuildingData
                {
                    name = "HYPERSPACE DRIVE",
                    description = "WIN THE GAME!",
                    baseCost = 5000,
                    baseProduction = 0f,
                    producesResource = ResourceType.TechPoints,
                    costResource = ResourceType.TechPoints,
                    unlockRequirement = 500,
                    buildingColor = new Color(1f, 0.3f, 0.1f),
                    isUnlocked = false,
                    level = 0,
                    isFinalBuilding = true
                }
            };
        }

        void SpawnBuildings()
        {
            float spacing = 2f;
            float startX = -(buildings.Count - 1) * spacing / 2f;

            for (int i = 0; i < buildings.Count; i++)
            {
                GameObject buildingObj = new GameObject(buildings[i].name);
                buildingObj.transform.position = new Vector3(startX + i * spacing, -2f, 0);

                SpaceBuilding building = buildingObj.AddComponent<SpaceBuilding>();
                building.Initialize(buildings[i], i);
                buildingObjects.Add(building);

                // Add collider for clicking
                BoxCollider2D collider = buildingObj.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(1.5f, 2f);
                collider.offset = new Vector2(0, 1f);
            }
        }

        void CreateUI()
        {
            GameObject canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            // Resource panel (top left)
            energyText = CreateUIText(canvasObj.transform, "EnergyText", new Vector2(10, -10), new Vector2(200, 30), "Energy: 100");
            mineralsText = CreateUIText(canvasObj.transform, "MineralsText", new Vector2(10, -40), new Vector2(200, 30), "Minerals: 0");
            creditsText = CreateUIText(canvasObj.transform, "CreditsText", new Vector2(10, -70), new Vector2(200, 30), "Credits: 0");
            techPointsText = CreateUIText(canvasObj.transform, "TechPointsText", new Vector2(10, -100), new Vector2(200, 30), "Tech Points: 0");

            // Selected building info (bottom)
            selectedBuildingText = CreateUIText(canvasObj.transform, "SelectedText", new Vector2(10, 10), new Vector2(600, 80), "Click a building to select it\nLeft Click: Upgrade | Right Click: Generate resources manually");
            RectTransform selRect = selectedBuildingText.GetComponent<RectTransform>();
            selRect.anchorMin = new Vector2(0, 0);
            selRect.anchorMax = new Vector2(0, 0);
            selRect.pivot = new Vector2(0, 0);
            selectedBuildingText.fontSize = 16;

            // Game message (center top)
            gameMessageText = CreateUIText(canvasObj.transform, "GameMessage", new Vector2(0, -20), new Vector2(400, 50), "");
            RectTransform msgRect = gameMessageText.GetComponent<RectTransform>();
            msgRect.anchorMin = new Vector2(0.5f, 1);
            msgRect.anchorMax = new Vector2(0.5f, 1);
            msgRect.pivot = new Vector2(0.5f, 1);
            gameMessageText.alignment = TextAlignmentOptions.Center;
            gameMessageText.fontSize = 18;

            // Instructions (top right)
            TextMeshProUGUI instructions = CreateUIText(canvasObj.transform, "Instructions", new Vector2(-10, -10), new Vector2(300, 150),
                "SPACE TOWER WIZARD\n\nBuild your space colony!\nUnlock all buildings.\nBuild the Hyperspace Drive to WIN!");
            RectTransform instRect = instructions.GetComponent<RectTransform>();
            instRect.anchorMin = new Vector2(1, 1);
            instRect.anchorMax = new Vector2(1, 1);
            instRect.pivot = new Vector2(1, 1);
            instructions.fontSize = 14;

            // Win panel (hidden initially)
            winPanel = new GameObject("WinPanel");
            winPanel.transform.SetParent(canvasObj.transform);
            RectTransform winRect = winPanel.AddComponent<RectTransform>();
            winRect.anchorMin = Vector2.zero;
            winRect.anchorMax = Vector2.one;
            winRect.sizeDelta = Vector2.zero;

            Image winBg = winPanel.AddComponent<Image>();
            winBg.color = new Color(0, 0, 0, 0.8f);

            TextMeshProUGUI winText = CreateUIText(winPanel.transform, "WinText", Vector2.zero, new Vector2(500, 200),
                "HYPERSPACE JUMP ACTIVATED!\n\nYOU WIN!\n\nYour colony has reached the stars!");
            RectTransform winTextRect = winText.GetComponent<RectTransform>();
            winTextRect.anchorMin = new Vector2(0.5f, 0.5f);
            winTextRect.anchorMax = new Vector2(0.5f, 0.5f);
            winTextRect.pivot = new Vector2(0.5f, 0.5f);
            winText.alignment = TextAlignmentOptions.Center;
            winText.fontSize = 32;
            winText.color = Color.cyan;

            winPanel.SetActive(false);
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

            UpdateProduction();
            CheckUnlocks();
            HandleInput();
            UpdateUI();
        }

        void UpdateProduction()
        {
            foreach (var building in buildings)
            {
                if (building.level > 0 && building.isUnlocked)
                {
                    float production = building.baseProduction * building.level * (1 + building.level * 0.1f) * Time.deltaTime;

                    switch (building.producesResource)
                    {
                        case ResourceType.Energy:
                            energy += production;
                            break;
                        case ResourceType.Minerals:
                            minerals += production;
                            break;
                        case ResourceType.Credits:
                            credits += production;
                            break;
                        case ResourceType.TechPoints:
                            techPoints += production;
                            break;
                    }
                }
            }
        }

        void CheckUnlocks()
        {
            for (int i = 0; i < buildings.Count; i++)
            {
                if (!buildings[i].isUnlocked)
                {
                    float requirement = buildings[i].unlockRequirement;
                    bool canUnlock = false;

                    // Check previous building's resource production
                    if (i > 0)
                    {
                        switch (buildings[i - 1].producesResource)
                        {
                            case ResourceType.Energy:
                                canUnlock = energy >= requirement;
                                break;
                            case ResourceType.Minerals:
                                canUnlock = minerals >= requirement;
                                break;
                            case ResourceType.Credits:
                                canUnlock = credits >= requirement;
                                break;
                            case ResourceType.TechPoints:
                                canUnlock = techPoints >= requirement;
                                break;
                        }
                    }

                    if (canUnlock)
                    {
                        buildings[i].isUnlocked = true;
                        buildingObjects[i].UpdateVisual();
                        ShowMessage($"{buildings[i].name} UNLOCKED!");
                    }
                }
            }
        }

        void HandleInput()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            bool leftClick = mouse.leftButton.wasPressedThisFrame;
            bool rightClick = mouse.rightButton.wasPressedThisFrame;

            if (leftClick || rightClick)
            {
                Vector3 mousePos = mainCamera.ScreenToWorldPoint(mouse.position.ReadValue());
                RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

                if (hit.collider != null)
                {
                    SpaceBuilding building = hit.collider.GetComponent<SpaceBuilding>();
                    if (building != null)
                    {
                        selectedBuildingIndex = building.buildingIndex;

                        if (leftClick)
                        {
                            TryUpgradeBuilding(building.buildingIndex);
                        }
                        else if (rightClick)
                        {
                            ManualProduce(building.buildingIndex);
                        }
                    }
                }
            }
        }

        void TryUpgradeBuilding(int index)
        {
            BuildingData building = buildings[index];

            if (!building.isUnlocked)
            {
                ShowMessage("Building not unlocked yet!");
                return;
            }

            int cost = Mathf.RoundToInt(building.baseCost * Mathf.Pow(1.5f, building.level));

            bool canAfford = false;
            switch (building.costResource)
            {
                case ResourceType.Energy:
                    canAfford = energy >= cost;
                    if (canAfford) energy -= cost;
                    break;
                case ResourceType.Minerals:
                    canAfford = minerals >= cost;
                    if (canAfford) minerals -= cost;
                    break;
                case ResourceType.Credits:
                    canAfford = credits >= cost;
                    if (canAfford) credits -= cost;
                    break;
                case ResourceType.TechPoints:
                    canAfford = techPoints >= cost;
                    if (canAfford) techPoints -= cost;
                    break;
            }

            if (canAfford)
            {
                building.level++;
                buildingObjects[index].UpdateVisual();
                ShowMessage($"{building.name} upgraded to Level {building.level}!");

                // Check win condition
                if (building.isFinalBuilding && building.level >= 1)
                {
                    WinGame();
                }
            }
            else
            {
                ShowMessage($"Need {cost} {building.costResource}!");
            }
        }

        void ManualProduce(int index)
        {
            BuildingData building = buildings[index];

            if (!building.isUnlocked || building.level <= 0)
            {
                ShowMessage("Build this first!");
                return;
            }

            float amount = clickPower * building.level;

            switch (building.producesResource)
            {
                case ResourceType.Energy:
                    energy += amount;
                    break;
                case ResourceType.Minerals:
                    minerals += amount;
                    break;
                case ResourceType.Credits:
                    credits += amount;
                    break;
                case ResourceType.TechPoints:
                    techPoints += amount;
                    break;
            }

            ShowMessage($"+{amount} {building.producesResource}!");
        }

        void ShowMessage(string message)
        {
            if (gameMessageText != null)
            {
                gameMessageText.text = message;
                CancelInvoke(nameof(ClearMessage));
                Invoke(nameof(ClearMessage), 2f);
            }
        }

        void ClearMessage()
        {
            if (gameMessageText != null)
                gameMessageText.text = "";
        }

        void UpdateUI()
        {
            if (energyText != null)
                energyText.text = $"Energy: {Mathf.FloorToInt(energy)}";
            if (mineralsText != null)
                mineralsText.text = $"Minerals: {Mathf.FloorToInt(minerals)}";
            if (creditsText != null)
                creditsText.text = $"Credits: {Mathf.FloorToInt(credits)}";
            if (techPointsText != null)
                techPointsText.text = $"Tech Points: {Mathf.FloorToInt(techPoints)}";

            if (selectedBuildingIndex >= 0 && selectedBuildingIndex < buildings.Count)
            {
                BuildingData b = buildings[selectedBuildingIndex];
                int cost = Mathf.RoundToInt(b.baseCost * Mathf.Pow(1.5f, b.level));
                float prod = b.level > 0 ? b.baseProduction * b.level * (1 + b.level * 0.1f) : 0;

                string status = b.isUnlocked ? (b.level > 0 ? $"Level {b.level}" : "Not built") : "LOCKED";
                selectedBuildingText.text = $"{b.name} - {status}\n{b.description}\nProduces: {prod:F1} {b.producesResource}/sec | Upgrade Cost: {cost} {b.costResource}";
            }
        }

        void WinGame()
        {
            gameWon = true;
            if (winPanel != null)
                winPanel.SetActive(true);
        }
    }
}
