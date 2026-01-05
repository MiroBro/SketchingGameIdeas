using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using TMPro;

namespace GardenDortmantik
{
    public class GardenGameManager : MonoBehaviour
    {
        [Header("Game Settings")]
        public int startingTiles = 30;
        public int bonusTilesPerMatch = 1;

        [Header("References")]
        public GardenGrid grid;
        public Camera mainCamera;

        [Header("UI References")]
        public TextMeshProUGUI scoreText;
        public TextMeshProUGUI tilesRemainingText;
        public TextMeshProUGUI currentTileText;
        public TextMeshProUGUI gameOverText;
        public GameObject previewTileObject;

        private int score = 0;
        private int tilesRemaining;
        private GardenTileType currentTileType;
        private Queue<GardenTileType> tileQueue = new Queue<GardenTileType>();
        private bool gameOver = false;
        private List<GameObject> placementIndicators = new List<GameObject>();
        private SpriteRenderer previewRenderer;

        void Start()
        {
            SetupGame();
        }

        void SetupGame()
        {
            if (grid == null)
            {
                GameObject gridObj = new GameObject("GardenGrid");
                grid = gridObj.AddComponent<GardenGrid>();
            }

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            CreateUI();
            grid.Initialize();

            // Fill tile queue
            for (int i = 0; i < startingTiles; i++)
            {
                tileQueue.Enqueue(GetRandomTileType());
            }

            tilesRemaining = tileQueue.Count;
            DrawNextTile();
            UpdateUI();
            UpdatePlacementIndicators();

            // Create preview tile
            CreatePreviewTile();
        }

        void CreateUI()
        {
            // Create Canvas
            GameObject canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // Score Text
            scoreText = CreateUIText(canvasObj.transform, "ScoreText", new Vector2(10, -10), new Vector2(200, 40), TextAlignmentOptions.TopLeft);
            scoreText.text = "Score: 0";

            // Tiles Remaining Text
            tilesRemainingText = CreateUIText(canvasObj.transform, "TilesText", new Vector2(10, -50), new Vector2(200, 40), TextAlignmentOptions.TopLeft);
            tilesRemainingText.text = "Tiles: 30";

            // Current Tile Text
            currentTileText = CreateUIText(canvasObj.transform, "CurrentTileText", new Vector2(10, -90), new Vector2(300, 40), TextAlignmentOptions.TopLeft);
            currentTileText.text = "Next: Flowers";

            // Game Over Text
            gameOverText = CreateUIText(canvasObj.transform, "GameOverText", new Vector2(0, 0), new Vector2(400, 100), TextAlignmentOptions.Center);
            gameOverText.text = "GAME OVER\nFinal Score: 0\nPress R to Restart";
            gameOverText.fontSize = 36;
            gameOverText.gameObject.SetActive(false);
            RectTransform goRect = gameOverText.GetComponent<RectTransform>();
            goRect.anchorMin = new Vector2(0.5f, 0.5f);
            goRect.anchorMax = new Vector2(0.5f, 0.5f);
            goRect.anchoredPosition = Vector2.zero;

            // Instructions
            TextMeshProUGUI instructions = CreateUIText(canvasObj.transform, "Instructions", new Vector2(-10, -10), new Vector2(350, 120), TextAlignmentOptions.TopRight);
            instructions.text = "GARDEN DORTMANTIK\n\nClick valid spots to place tiles\nMatch types for bonus points!\nScroll to zoom, WASD to pan";
            instructions.fontSize = 16;
            RectTransform instRect = instructions.GetComponent<RectTransform>();
            instRect.anchorMin = new Vector2(1, 1);
            instRect.anchorMax = new Vector2(1, 1);
            instRect.pivot = new Vector2(1, 1);
        }

        TextMeshProUGUI CreateUIText(Transform parent, string name, Vector2 position, Vector2 size, TextAlignmentOptions alignment)
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
            tmp.alignment = alignment;
            tmp.fontSize = 20;
            tmp.color = Color.white;

            return tmp;
        }

        void CreatePreviewTile()
        {
            previewTileObject = new GameObject("PreviewTile");
            previewRenderer = previewTileObject.AddComponent<SpriteRenderer>();
            previewRenderer.sortingOrder = 10;

            Texture2D texture = new Texture2D(64, 64);
            Color[] pixels = new Color[64 * 64];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color(1, 1, 1, 0.5f);
            }
            texture.SetPixels(pixels);
            texture.Apply();
            previewRenderer.sprite = Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64);
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (gameOver)
            {
                if (keyboard.rKey.wasPressedThisFrame)
                {
                    RestartGame();
                }
                return;
            }

            HandleCameraControls();
            HandleTilePlacement();
            UpdatePreviewPosition();
        }

        void HandleCameraControls()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null) return;

            // WASD pan
            float panSpeed = 5f;
            Vector3 pan = Vector3.zero;
            if (keyboard.wKey.isPressed) pan.y += panSpeed * Time.deltaTime;
            if (keyboard.sKey.isPressed) pan.y -= panSpeed * Time.deltaTime;
            if (keyboard.aKey.isPressed) pan.x -= panSpeed * Time.deltaTime;
            if (keyboard.dKey.isPressed) pan.x += panSpeed * Time.deltaTime;
            mainCamera.transform.position += pan;

            // Scroll zoom
            if (mouse != null)
            {
                float scroll = mouse.scroll.ReadValue().y / 120f;
                if (scroll != 0)
                {
                    mainCamera.orthographicSize = Mathf.Clamp(mainCamera.orthographicSize - scroll * 2f, 3f, 20f);
                }
            }
        }

        void HandleTilePlacement()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            if (mouse.leftButton.wasPressedThisFrame)
            {
                Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(mouse.position.ReadValue());
                mouseWorld.z = 0;
                Vector2Int gridPos = grid.WorldToGrid(mouseWorld);

                if (grid.CanPlaceTile(gridPos))
                {
                    PlaceTileAt(gridPos);
                }
            }
        }

        void UpdatePreviewPosition()
        {
            if (previewTileObject == null) return;

            var mouse = Mouse.current;
            if (mouse == null) return;

            Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(mouse.position.ReadValue());
            mouseWorld.z = 0;
            Vector2Int gridPos = grid.WorldToGrid(mouseWorld);

            if (grid.CanPlaceTile(gridPos))
            {
                previewTileObject.SetActive(true);
                previewTileObject.transform.position = grid.GridToWorld(gridPos);

                Color tileColor = GetTileColor(currentTileType);
                tileColor.a = 0.5f;
                previewRenderer.color = tileColor;
            }
            else
            {
                previewTileObject.SetActive(false);
            }
        }

        void PlaceTileAt(Vector2Int position)
        {
            GardenTile placedTile = grid.PlaceTile(currentTileType, position);
            if (placedTile == null) return;

            // Calculate score
            GardenTile[] neighbors = grid.GetNeighborTiles(position);
            int matchBonus = placedTile.CalculateMatchBonus(neighbors);
            int tileScore = 5 + matchBonus;
            score += tileScore;

            // Award bonus tiles for good matches
            if (matchBonus >= 20)
            {
                int bonusTiles = matchBonus / 10;
                for (int i = 0; i < bonusTiles; i++)
                {
                    tileQueue.Enqueue(GetRandomTileType());
                }
                tilesRemaining = tileQueue.Count;
            }

            DrawNextTile();
            UpdateUI();
            UpdatePlacementIndicators();
        }

        void DrawNextTile()
        {
            if (tileQueue.Count > 0)
            {
                currentTileType = tileQueue.Dequeue();
                tilesRemaining = tileQueue.Count;
            }
            else
            {
                EndGame();
            }
        }

        void UpdatePlacementIndicators()
        {
            // Clear old indicators
            foreach (var indicator in placementIndicators)
            {
                Destroy(indicator);
            }
            placementIndicators.Clear();

            if (gameOver) return;

            // Create new indicators for valid placements
            foreach (var pos in grid.GetValidPlacements())
            {
                GameObject indicator = new GameObject($"Indicator_{pos.x}_{pos.y}");
                indicator.transform.position = grid.GridToWorld(pos);

                SpriteRenderer sr = indicator.AddComponent<SpriteRenderer>();
                sr.sortingOrder = -1;

                Texture2D texture = new Texture2D(64, 64);
                Color[] pixels = new Color[64 * 64];
                for (int y = 0; y < 64; y++)
                {
                    for (int x = 0; x < 64; x++)
                    {
                        bool isBorder = x < 4 || x > 59 || y < 4 || y > 59;
                        pixels[y * 64 + x] = isBorder ? new Color(0.5f, 1f, 0.5f, 0.8f) : new Color(0.5f, 1f, 0.5f, 0.2f);
                    }
                }
                texture.SetPixels(pixels);
                texture.Apply();
                sr.sprite = Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64);

                placementIndicators.Add(indicator);
            }
        }

        void UpdateUI()
        {
            if (scoreText != null)
                scoreText.text = $"Score: {score}";

            if (tilesRemainingText != null)
                tilesRemainingText.text = $"Tiles: {tilesRemaining}";

            if (currentTileText != null)
                currentTileText.text = $"Next: {currentTileType}";
        }

        void EndGame()
        {
            gameOver = true;
            if (gameOverText != null)
            {
                gameOverText.text = $"GARDEN COMPLETE!\n\nFinal Score: {score}\nTiles Placed: {grid.GetTileCount()}\n\nPress R to Restart";
                gameOverText.gameObject.SetActive(true);
            }

            if (previewTileObject != null)
                previewTileObject.SetActive(false);
        }

        void RestartGame()
        {
            // Clear grid
            foreach (Transform child in grid.transform)
            {
                Destroy(child.gameObject);
            }

            // Reset state
            score = 0;
            gameOver = false;
            tileQueue.Clear();

            if (gameOverText != null)
                gameOverText.gameObject.SetActive(false);

            // Reinitialize
            grid.Initialize();

            for (int i = 0; i < startingTiles; i++)
            {
                tileQueue.Enqueue(GetRandomTileType());
            }

            tilesRemaining = tileQueue.Count;
            DrawNextTile();
            UpdateUI();
            UpdatePlacementIndicators();
        }

        GardenTileType GetRandomTileType()
        {
            return (GardenTileType)Random.Range(0, 6);
        }

        Color GetTileColor(GardenTileType type)
        {
            switch (type)
            {
                case GardenTileType.Flowers: return new Color(1f, 0.5f, 0.7f);
                case GardenTileType.Grass: return new Color(0.4f, 0.8f, 0.3f);
                case GardenTileType.Trees: return new Color(0.2f, 0.5f, 0.2f);
                case GardenTileType.Water: return new Color(0.3f, 0.6f, 0.9f);
                case GardenTileType.Path: return new Color(0.7f, 0.6f, 0.4f);
                case GardenTileType.Vegetables: return new Color(0.9f, 0.6f, 0.2f);
                default: return Color.white;
            }
        }
    }
}
