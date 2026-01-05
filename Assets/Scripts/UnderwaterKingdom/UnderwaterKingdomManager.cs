using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

namespace UnderwaterKingdom
{
    public class UnderwaterKingdomManager : MonoBehaviour
    {
        [Header("World Settings")]
        public float worldMinY = -15f;
        public float worldMaxY = 15f;
        public float dayDuration = 30f;
        public float nightDuration = 20f;

        [Header("Spawn Settings")]
        public int maxDefenders = 5;
        public int enemiesPerNight = 3;
        public float enemySpawnInterval = 2f;

        private UnderwaterPlayer player;
        private Camera mainCamera;
        private List<UnderwaterBuilding> buildings = new List<UnderwaterBuilding>();
        private List<SeaDefender> defenders = new List<SeaDefender>();

        private bool isNight = false;
        private float timeOfDay = 0f;
        private int currentDay = 1;
        private int enemiesToSpawn = 0;
        private float enemySpawnTimer = 0f;
        private bool gameOver = false;

        // UI
        private TextMeshProUGUI pearlsText;
        private TextMeshProUGUI dayText;
        private TextMeshProUGUI timeText;
        private TextMeshProUGUI instructionsText;
        private TextMeshProUGUI gameOverText;
        private Image dayNightIndicator;

        // Pearl resources scattered in world
        private List<GameObject> pearlNodes = new List<GameObject>();

        void Start()
        {
            SetupCamera();
            CreatePlayer();
            CreateUI();
            SpawnInitialResources();
            SpawnInitialDefenders();
        }

        void SetupCamera()
        {
            mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainCamera.backgroundColor = new Color(0.05f, 0.15f, 0.3f);
            }
        }

        void CreatePlayer()
        {
            GameObject playerObj = new GameObject("Player");
            player = playerObj.AddComponent<UnderwaterPlayer>();
            player.SetWorldBounds(worldMinY, worldMaxY);
            player.transform.position = new Vector3(0, 0, 0);
        }

        void CreateUI()
        {
            GameObject canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            // Pearls
            pearlsText = CreateUIText(canvasObj.transform, "PearlsText", new Vector2(10, -10), new Vector2(200, 30), "Pearls: 10");

            // Day counter
            dayText = CreateUIText(canvasObj.transform, "DayText", new Vector2(10, -40), new Vector2(200, 30), "Day 1");

            // Time of day
            timeText = CreateUIText(canvasObj.transform, "TimeText", new Vector2(10, -70), new Vector2(200, 30), "Daytime");

            // Day/Night indicator
            GameObject indicatorObj = new GameObject("DayNightIndicator");
            indicatorObj.transform.SetParent(canvasObj.transform);
            RectTransform indRect = indicatorObj.AddComponent<RectTransform>();
            indRect.anchorMin = new Vector2(0, 1);
            indRect.anchorMax = new Vector2(0, 1);
            indRect.pivot = new Vector2(0, 1);
            indRect.anchoredPosition = new Vector2(220, -10);
            indRect.sizeDelta = new Vector2(30, 30);
            dayNightIndicator = indicatorObj.AddComponent<Image>();
            dayNightIndicator.color = Color.yellow;

            // Instructions
            instructionsText = CreateUIText(canvasObj.transform, "Instructions", new Vector2(-10, -10), new Vector2(350, 200),
                "UNDERWATER KINGDOM\n\nW/S or Up/Down: Move\n1: Build Wall (5 pearls)\n2: Build Farm (10 pearls)\n3: Build Tower (15 pearls)\nClick defenders to recruit (3 pearls)\nCollect pearls from nodes");
            RectTransform instRect = instructionsText.GetComponent<RectTransform>();
            instRect.anchorMin = new Vector2(1, 1);
            instRect.anchorMax = new Vector2(1, 1);
            instRect.pivot = new Vector2(1, 1);
            instructionsText.fontSize = 14;

            // Game over text
            gameOverText = CreateUIText(canvasObj.transform, "GameOver", Vector2.zero, new Vector2(400, 150), "");
            RectTransform goRect = gameOverText.GetComponent<RectTransform>();
            goRect.anchorMin = new Vector2(0.5f, 0.5f);
            goRect.anchorMax = new Vector2(0.5f, 0.5f);
            goRect.pivot = new Vector2(0.5f, 0.5f);
            gameOverText.alignment = TextAlignmentOptions.Center;
            gameOverText.fontSize = 32;
            gameOverText.gameObject.SetActive(false);
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

        void SpawnInitialResources()
        {
            // Create pearl nodes scattered in the world
            for (int i = 0; i < 10; i++)
            {
                float y = Random.Range(worldMinY + 2f, worldMaxY - 2f);
                float x = Random.Range(-2f, 2f);
                CreatePearlNode(new Vector3(x, y, 0));
            }
        }

        void SpawnInitialDefenders()
        {
            // Spawn a few unrecruited defenders
            for (int i = 0; i < 3; i++)
            {
                float y = Random.Range(-5f, 5f);
                CreateDefender(y, false);
            }
        }

        void CreatePearlNode(Vector3 position)
        {
            GameObject pearl = new GameObject("PearlNode");
            pearl.transform.position = position;

            SpriteRenderer sr = pearl.AddComponent<SpriteRenderer>();
            Texture2D texture = new Texture2D(16, 16);
            Color[] pixels = new Color[16 * 16];

            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    float dx = (x - 8) / 6f;
                    float dy = (y - 8) / 6f;
                    bool isFilled = dx * dx + dy * dy < 1f;
                    pixels[y * 16 + x] = isFilled ? new Color(0.95f, 0.95f, 0.85f) : Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            texture.filterMode = FilterMode.Point;
            sr.sprite = Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16);
            sr.sortingOrder = 2;

            CircleCollider2D collider = pearl.AddComponent<CircleCollider2D>();
            collider.radius = 0.3f;
            collider.isTrigger = true;

            pearl.AddComponent<PearlNode>();
            pearlNodes.Add(pearl);
        }

        SeaDefender CreateDefender(float yPos, bool recruited)
        {
            GameObject defenderObj = new GameObject("Defender");
            defenderObj.transform.position = new Vector3(Random.Range(-1f, 1f), yPos, 0);

            SeaDefender defender = defenderObj.AddComponent<SeaDefender>();
            defender.Initialize(yPos, recruited);

            BoxCollider2D collider = defenderObj.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.8f, 0.8f);

            defenders.Add(defender);
            return defender;
        }

        void Update()
        {
            if (gameOver) return;

            UpdateDayNightCycle();
            HandleInput();
            HandleCollisions();
            UpdateUI();
            UpdateCamera();

            if (isNight)
            {
                SpawnEnemies();
            }

            // Check game over
            if (player != null && !player.hasCrown)
            {
                GameOver();
            }
        }

        void UpdateDayNightCycle()
        {
            float cycleDuration = isNight ? nightDuration : dayDuration;
            timeOfDay += Time.deltaTime;

            if (timeOfDay >= cycleDuration)
            {
                timeOfDay = 0f;
                isNight = !isNight;

                if (isNight)
                {
                    StartNight();
                }
                else
                {
                    currentDay++;
                    // Respawn some resources during day
                    if (pearlNodes.Count < 8)
                    {
                        float y = Random.Range(worldMinY + 2f, worldMaxY - 2f);
                        CreatePearlNode(new Vector3(Random.Range(-2f, 2f), y, 0));
                    }
                }
            }
        }

        void StartNight()
        {
            enemiesToSpawn = enemiesPerNight + currentDay;
            enemySpawnTimer = 0f;
        }

        void SpawnEnemies()
        {
            if (enemiesToSpawn <= 0) return;

            enemySpawnTimer += Time.deltaTime;
            if (enemySpawnTimer >= enemySpawnInterval)
            {
                enemySpawnTimer = 0f;
                enemiesToSpawn--;

                // Spawn from above or below randomly
                bool fromAbove = Random.value > 0.5f;
                float spawnY = fromAbove ? worldMaxY + 2f : worldMinY - 2f;

                GameObject monsterObj = new GameObject("SeaMonster");
                monsterObj.transform.position = new Vector3(Random.Range(-1f, 1f), spawnY, 0);

                SeaMonster monster = monsterObj.AddComponent<SeaMonster>();
                monster.Initialize(fromAbove);
                monster.health += currentDay * 5; // Scale with days
            }
        }

        void HandleInput()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;

            // Building hotkeys
            if (keyboard != null)
            {
                if (keyboard.digit1Key.wasPressedThisFrame)
                {
                    TryBuildBuilding(BuildingType.CoralWall);
                }
                if (keyboard.digit2Key.wasPressedThisFrame)
                {
                    TryBuildBuilding(BuildingType.PearlFarm);
                }
                if (keyboard.digit3Key.wasPressedThisFrame)
                {
                    TryBuildBuilding(BuildingType.DefenderPost);
                }
            }

            // Click to recruit defenders
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                Vector3 mousePos = mainCamera.ScreenToWorldPoint(mouse.position.ReadValue());
                RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

                if (hit.collider != null)
                {
                    SeaDefender defender = hit.collider.GetComponent<SeaDefender>();
                    if (defender != null && !defender.IsRecruited())
                    {
                        TryRecruitDefender(defender);
                    }
                }
            }
        }

        void TryBuildBuilding(BuildingType type)
        {
            int cost = GetBuildingCost(type);
            if (player.SpendPearls(cost))
            {
                GameObject buildingObj = new GameObject($"Building_{type}");
                buildingObj.transform.position = new Vector3(2.5f, player.transform.position.y, 0);

                UnderwaterBuilding building = buildingObj.AddComponent<UnderwaterBuilding>();
                building.Initialize(type, player.transform.position.y);

                buildings.Add(building);
            }
        }

        void TryRecruitDefender(SeaDefender defender)
        {
            if (player.SpendPearls(defender.GetRecruitCost()))
            {
                defender.Recruit();
            }
        }

        int GetBuildingCost(BuildingType type)
        {
            switch (type)
            {
                case BuildingType.CoralWall: return 5;
                case BuildingType.PearlFarm: return 10;
                case BuildingType.DefenderPost: return 15;
                default: return 10;
            }
        }

        void HandleCollisions()
        {
            if (player == null) return;

            // Check for pearl collection
            Collider2D[] hits = Physics2D.OverlapCircleAll(player.transform.position, 0.5f);
            foreach (var hit in hits)
            {
                if (hit.GetComponent<PearlNode>() != null)
                {
                    player.AddPearls(1);
                    pearlNodes.Remove(hit.gameObject);
                    Destroy(hit.gameObject);
                }
            }

            // Pearl farms generate pearls over time
            foreach (var building in buildings)
            {
                if (building != null && building.buildingType == BuildingType.PearlFarm)
                {
                    // Passive income during day
                    if (!isNight && Random.value < 0.01f)
                    {
                        player.AddPearls(1);
                    }
                }
            }
        }

        void UpdateCamera()
        {
            if (mainCamera != null && player != null)
            {
                Vector3 targetPos = new Vector3(0, player.transform.position.y, -10);
                mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, targetPos, 5f * Time.deltaTime);

                // Day/night background color
                Color dayColor = new Color(0.1f, 0.3f, 0.5f);
                Color nightColor = new Color(0.02f, 0.05f, 0.15f);
                mainCamera.backgroundColor = Color.Lerp(dayColor, nightColor, isNight ? 1f : 0f);
            }
        }

        void UpdateUI()
        {
            if (pearlsText != null)
                pearlsText.text = $"Pearls: {player.pearls}";

            if (dayText != null)
                dayText.text = $"Day {currentDay}";

            if (timeText != null)
            {
                float progress = timeOfDay / (isNight ? nightDuration : dayDuration);
                string timeStr = isNight ? "NIGHT" : "Day";
                timeText.text = $"{timeStr} ({(int)(progress * 100)}%)";
            }

            if (dayNightIndicator != null)
            {
                dayNightIndicator.color = isNight ? new Color(0.2f, 0.2f, 0.5f) : Color.yellow;
            }
        }

        void GameOver()
        {
            gameOver = true;
            if (gameOverText != null)
            {
                gameOverText.text = $"GAME OVER!\n\nThe crown was taken!\n\nYou survived {currentDay} days";
                gameOverText.gameObject.SetActive(true);
            }
        }
    }
}
