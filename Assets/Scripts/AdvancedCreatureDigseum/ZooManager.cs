using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

namespace AdvancedCreatureDigseum
{
    public class ZooManager : MonoBehaviour
    {
        private Camera mainCamera;
        private GameObject zooParent;
        private List<GameObject> placedObjects = new List<GameObject>();
        private List<GameObject> visitorObjects = new List<GameObject>();

        // Placement mode
        private string selectedPlacement = "";
        private int selectedHybridIndex = -1;
        private int selectedDecoStyle = 0; // Current decoration style (0-2)
        private GameObject placementPreview;
        private SpriteRenderer previewRenderer;
        private bool canPlaceAtCurrentPos = false;

        // Destroy mode
        private bool destroyMode = false;

        // Hybrid selection UI
        private GameObject hybridSelectionPanel;
        private List<GameObject> hybridSelectionButtons = new List<GameObject>();
        private int hybridScrollOffset = 0;
        private const int MAX_VISIBLE_HYBRIDS = 6;

        // Pasture customization UI
        private GameObject pastureCustomizePanel;
        private PlacedHybrid selectedPastureForCustomize;
        private List<GameObject> styleButtons = new List<GameObject>();

        // Decoration customization UI
        private GameObject decoCustomizePanel;
        private PlacedDecoration selectedDecoForCustomize;
        private TextMeshProUGUI decoCustomizeTitleText;
        private List<GameObject> decoStyleButtons = new List<GameObject>();

        // Grid
        private int gridWidth = 16;
        private int gridHeight = 12;
        private float cellSize = 0.8f;

        // UI
        private TextMeshProUGUI goldText;
        private TextMeshProUGUI incomeText;
        private TextMeshProUGUI modeText;
        private TextMeshProUGUI feedbackText;
        private float feedbackTimer;

        // Decoration button tracking for dynamic coloring
        private Dictionary<string, Image> decoButtonImages = new Dictionary<string, Image>();

        // Visitors
        private float visitorSpawnTimer;
        private int maxVisitors = 10;

        // Autosave
        private float saveTimer = 0f;
        private float saveInterval = 10f;

        void Start()
        {
            GameData.EnsureLoaded();

            mainCamera = Camera.main;
            mainCamera.backgroundColor = new Color(0.3f, 0.5f, 0.3f);
            mainCamera.orthographicSize = 6;

            CreateZooGround();
            CreateUI();
            RebuildZoo();

            // Pre-populate visitors based on zoo income/popularity
            PrePopulateVisitors();
        }

        void PrePopulateVisitors()
        {
            // Calculate max visitors based on zoo income - more income = more visitors
            int income = GameData.CalculateTotalIdleIncome();
            // Base 2 visitors, plus 1 per 10 income, capped at 20
            maxVisitors = Mathf.Clamp(2 + income / 10, 2, 20);

            // Pre-spawn visitors (half of max) at random positions inside the zoo
            int initialVisitors = maxVisitors / 2;
            if (income == 0) initialVisitors = 0; // No visitors if no income

            for (int i = 0; i < initialVisitors; i++)
            {
                SpawnVisitorAtRandomPosition();
            }
        }

        void SpawnVisitorAtRandomPosition()
        {
            GameObject visitor = new GameObject("Visitor");
            visitor.transform.parent = zooParent.transform;

            // Spawn at random position within zoo bounds
            float x = Random.Range(-5f, 5f);
            float y = Random.Range(-4f, 4f);
            visitor.transform.position = new Vector3(x, y, 0);

            SpriteRenderer sr = visitor.AddComponent<SpriteRenderer>();
            Texture2D tex = new Texture2D(8, 16);
            Color[] pixels = new Color[8 * 16];

            Color bodyColor = new Color(
                Random.Range(0.3f, 0.8f),
                Random.Range(0.3f, 0.8f),
                Random.Range(0.3f, 0.8f)
            );
            Color headColor = new Color(0.9f, 0.75f, 0.6f);

            for (int py = 0; py < 16; py++)
            {
                for (int px = 0; px < 8; px++)
                {
                    if (py < 10)
                    {
                        if (px >= 2 && px < 6)
                            pixels[py * 8 + px] = bodyColor;
                    }
                    else if (py < 14)
                    {
                        if (px >= 2 && px < 6)
                            pixels[py * 8 + px] = headColor;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 8, 16), new Vector2(0.5f, 0f), 16);
            sr.sortingOrder = 5;

            VisitorBehavior behavior = visitor.AddComponent<VisitorBehavior>();
            behavior.Initialize(GameData.PlacedHybrids);

            visitorObjects.Add(visitor);
        }

        void CreateZooGround()
        {
            zooParent = new GameObject("Zoo");

            // Ground
            GameObject ground = new GameObject("Ground");
            ground.transform.parent = zooParent.transform;
            SpriteRenderer sr = ground.AddComponent<SpriteRenderer>();

            int texW = gridWidth * 16;
            int texH = gridHeight * 16;
            Texture2D tex = new Texture2D(texW, texH);
            Color[] pixels = new Color[texW * texH];

            for (int y = 0; y < texH; y++)
            {
                for (int x = 0; x < texW; x++)
                {
                    bool isPath = (x / 16 + y / 16) % 4 == 0;
                    float noise = Random.Range(0.9f, 1.1f);
                    if (isPath)
                        pixels[y * texW + x] = new Color(0.5f, 0.45f, 0.35f) * noise;
                    else
                        pixels[y * texW + x] = new Color(0.35f, 0.55f, 0.3f) * noise;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, texW, texH), new Vector2(0.5f, 0.5f), 16);
            sr.sortingOrder = -10;
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

            goldText = CreateText(canvasObj.transform, new Vector2(10, -10), "Gold: 0");
            goldText.fontSize = 24;

            incomeText = CreateText(canvasObj.transform, new Vector2(10, -45), "Income: 0/2s");
            incomeText.fontSize = 18;

            modeText = CreateText(canvasObj.transform, new Vector2(10, -80), "Mode: View");
            modeText.fontSize = 16;
            RectTransform modeRect = modeText.GetComponent<RectTransform>();
            modeRect.sizeDelta = new Vector2(400, 60);

            // Build menu
            TextMeshProUGUI buildMenu = CreateText(canvasObj.transform, new Vector2(10, -150),
                "BUILD:\n" +
                "Use buttons on right\n" +
                "or P: Pasture, H: Hybrid\n\n" +
                "Right-click: Cancel\n" +
                "Left-click: Place");
            RectTransform buildRect = buildMenu.GetComponent<RectTransform>();
            buildRect.sizeDelta = new Vector2(250, 150);
            buildMenu.fontSize = 14;

            // Title (right side)
            TextMeshProUGUI title = CreateText(canvasObj.transform, new Vector2(-10, -10), "MAGICAL ZOO");
            RectTransform titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(1, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(1, 1);
            titleRect.sizeDelta = new Vector2(200, 35);
            title.fontSize = 24;
            title.alignment = TextAlignmentOptions.Right;

            // Scene navigation buttons (right side)
            float navY = -50;
            CreateNavButton(canvasObj.transform, new Vector2(-10, navY), "Exploration", "ACD_Exploration");
            CreateNavButton(canvasObj.transform, new Vector2(-10, navY - 40), "Fusion Lab", "ACD_FusionLab");
            CreateNavButton(canvasObj.transform, new Vector2(-10, navY - 80), "Skills", "ACD_Skills");
            CreateNavButton(canvasObj.transform, new Vector2(-10, navY - 120), "Menu", "SampleScene");

            // Placement buttons section
            TextMeshProUGUI placeLabel = CreateText(canvasObj.transform, new Vector2(-10, navY - 170), "PLACE:");
            RectTransform placeLabelRect = placeLabel.GetComponent<RectTransform>();
            placeLabelRect.anchorMin = new Vector2(1, 1);
            placeLabelRect.anchorMax = new Vector2(1, 1);
            placeLabelRect.pivot = new Vector2(1, 1);
            placeLabelRect.sizeDelta = new Vector2(200, 25);
            placeLabel.fontSize = 16;
            placeLabel.alignment = TextAlignmentOptions.Right;

            float placeY = navY - 200;
            CreatePlacementButton(canvasObj.transform, new Vector2(-115, placeY), "Pasture", "pasture");
            CreatePlacementButton(canvasObj.transform, new Vector2(-10, placeY), "Hybrid", "hybrid");

            // Destroy mode button
            CreateDestroyButton(canvasObj.transform, new Vector2(-62, placeY - 38));

            // Decoration buttons section
            TextMeshProUGUI decoLabel = CreateText(canvasObj.transform, new Vector2(-10, placeY - 80), "DECORATIONS:");
            RectTransform decoLabelRect = decoLabel.GetComponent<RectTransform>();
            decoLabelRect.anchorMin = new Vector2(1, 1);
            decoLabelRect.anchorMax = new Vector2(1, 1);
            decoLabelRect.pivot = new Vector2(1, 1);
            decoLabelRect.sizeDelta = new Vector2(200, 25);
            decoLabel.fontSize = 16;
            decoLabel.alignment = TextAlignmentOptions.Right;

            float decoY = placeY - 110;
            CreateDecoButton(canvasObj.transform, new Vector2(-115, decoY), "Fence", 25);
            CreateDecoButton(canvasObj.transform, new Vector2(-10, decoY), "Path", 25);
            CreateDecoButton(canvasObj.transform, new Vector2(-115, decoY - 35), "Bench", 100);
            CreateDecoButton(canvasObj.transform, new Vector2(-10, decoY - 35), "Lamp", 150);
            CreateDecoButton(canvasObj.transform, new Vector2(-115, decoY - 70), "Tree", 300);
            CreateDecoButton(canvasObj.transform, new Vector2(-10, decoY - 70), "Flowers", 400);
            CreateDecoButton(canvasObj.transform, new Vector2(-115, decoY - 105), "Fountain", 2000);
            CreateDecoButton(canvasObj.transform, new Vector2(-10, decoY - 105), "Statue", 8000);

            // Create hybrid selection panel (hidden by default)
            CreateHybridSelectionPanel(canvasObj.transform);

            // Create pasture customization panel (hidden by default)
            CreatePastureCustomizePanel(canvasObj.transform);

            // Create decoration customization panel (hidden by default)
            CreateDecoCustomizePanel(canvasObj.transform);

            // Feedback
            feedbackText = CreateText(canvasObj.transform, Vector2.zero, "");
            RectTransform fbRect = feedbackText.GetComponent<RectTransform>();
            fbRect.anchorMin = new Vector2(0.5f, 0);
            fbRect.anchorMax = new Vector2(0.5f, 0);
            fbRect.pivot = new Vector2(0.5f, 0);
            fbRect.anchoredPosition = new Vector2(0, 100);
            fbRect.sizeDelta = new Vector2(600, 60);
            feedbackText.alignment = TextAlignmentOptions.Center;
            feedbackText.fontSize = 24;
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
            rect.sizeDelta = new Vector2(300, 40);

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

        void CreatePlacementButton(Transform parent, Vector2 position, string label, string placementType)
        {
            GameObject btnObj = new GameObject($"PlaceBtn_{label}");
            btnObj.transform.SetParent(parent);

            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(1, 1);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(100, 32);
            rect.localScale = Vector3.one;

            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.25f, 0.4f, 0.3f);

            Button btn = btnObj.AddComponent<Button>();
            string type = placementType;
            btn.onClick.AddListener(() => {
                if (type == "pasture") StartPasturePlacement();
                else if (type == "hybrid") StartHybridPlacement();
            });

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

        void CreateDecoButton(Transform parent, Vector2 position, string decoType, int cost)
        {
            GameObject btnObj = new GameObject($"DecoBtn_{decoType}");
            btnObj.transform.SetParent(parent);

            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(1, 1);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(100, 30);
            rect.localScale = Vector3.one;

            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.35f, 0.3f, 0.25f);

            // Store reference for dynamic coloring
            decoButtonImages[decoType] = img;

            Button btn = btnObj.AddComponent<Button>();
            string type = decoType;
            btn.onClick.AddListener(() => TrySelectDecoration(type));

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(5, 0);
            textRect.offsetMax = new Vector2(-5, 0);
            textRect.localScale = Vector3.one;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = $"{decoType}\n<size=10>{cost}g</size>";
            tmp.fontSize = 12;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
        }

        void CreateDestroyButton(Transform parent, Vector2 position)
        {
            GameObject btnObj = new GameObject("DestroyBtn");
            btnObj.transform.SetParent(parent);

            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(100, 30);
            rect.localScale = Vector3.one;

            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.5f, 0.2f, 0.2f);

            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(ToggleDestroyMode);

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            textRect.localScale = Vector3.one;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "Destroy";
            tmp.fontSize = 14;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

            // Store reference for color updates
            DestroyButtonRef btnRef = btnObj.AddComponent<DestroyButtonRef>();
            btnRef.ButtonImage = img;
        }

        void CreateHybridSelectionPanel(Transform parent)
        {
            hybridSelectionPanel = new GameObject("HybridSelectionPanel");
            hybridSelectionPanel.transform.SetParent(parent);

            RectTransform panelRect = hybridSelectionPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(400, 320);
            panelRect.localScale = Vector3.one;

            Image bgImg = hybridSelectionPanel.AddComponent<Image>();
            bgImg.color = new Color(0.15f, 0.15f, 0.2f, 0.95f);

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(hybridSelectionPanel.transform);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -5);
            titleRect.sizeDelta = new Vector2(0, 30);
            titleRect.localScale = Vector3.one;
            TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "Select Hybrid to Place";
            titleTmp.fontSize = 18;
            titleTmp.color = Color.white;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.raycastTarget = false;

            // Scroll buttons
            CreateScrollButton(hybridSelectionPanel.transform, new Vector2(-170, 0), "^", -1);
            CreateScrollButton(hybridSelectionPanel.transform, new Vector2(170, 0), "v", 1);

            // Close button
            GameObject closeObj = new GameObject("CloseBtn");
            closeObj.transform.SetParent(hybridSelectionPanel.transform);
            RectTransform closeRect = closeObj.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1, 1);
            closeRect.anchorMax = new Vector2(1, 1);
            closeRect.pivot = new Vector2(1, 1);
            closeRect.anchoredPosition = new Vector2(-5, -5);
            closeRect.sizeDelta = new Vector2(30, 30);
            closeRect.localScale = Vector3.one;
            Image closeImg = closeObj.AddComponent<Image>();
            closeImg.color = new Color(0.6f, 0.2f, 0.2f);
            Button closeBtn = closeObj.AddComponent<Button>();
            closeBtn.onClick.AddListener(CloseHybridSelection);
            GameObject closeTextObj = new GameObject("Text");
            closeTextObj.transform.SetParent(closeObj.transform);
            RectTransform closeTextRect = closeTextObj.AddComponent<RectTransform>();
            closeTextRect.anchorMin = Vector2.zero;
            closeTextRect.anchorMax = Vector2.one;
            closeTextRect.offsetMin = Vector2.zero;
            closeTextRect.offsetMax = Vector2.zero;
            closeTextRect.localScale = Vector3.one;
            TextMeshProUGUI closeTmp = closeTextObj.AddComponent<TextMeshProUGUI>();
            closeTmp.text = "X";
            closeTmp.fontSize = 16;
            closeTmp.color = Color.white;
            closeTmp.alignment = TextAlignmentOptions.Center;
            closeTmp.raycastTarget = false;

            hybridSelectionPanel.SetActive(false);
        }

        void CreateScrollButton(Transform parent, Vector2 position, string label, int direction)
        {
            GameObject btnObj = new GameObject($"ScrollBtn_{label}");
            btnObj.transform.SetParent(parent);

            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(30, 200);
            rect.localScale = Vector3.one;

            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.3f, 0.3f, 0.4f);

            Button btn = btnObj.AddComponent<Button>();
            int dir = direction;
            btn.onClick.AddListener(() => ScrollHybrids(dir));

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
            tmp.fontSize = 20;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
        }

        void CreatePastureCustomizePanel(Transform parent)
        {
            pastureCustomizePanel = new GameObject("PastureCustomizePanel");
            pastureCustomizePanel.transform.SetParent(parent);

            RectTransform panelRect = pastureCustomizePanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(350, 280);
            panelRect.localScale = Vector3.one;

            Image bgImg = pastureCustomizePanel.AddComponent<Image>();
            bgImg.color = new Color(0.15f, 0.15f, 0.2f, 0.95f);

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(pastureCustomizePanel.transform);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -10);
            titleRect.sizeDelta = new Vector2(0, 30);
            titleRect.localScale = Vector3.one;
            TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "Customize Pasture";
            titleTmp.fontSize = 20;
            titleTmp.color = Color.white;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.raycastTarget = false;

            // Style label
            GameObject styleLabelObj = new GameObject("StyleLabel");
            styleLabelObj.transform.SetParent(pastureCustomizePanel.transform);
            RectTransform slRect = styleLabelObj.AddComponent<RectTransform>();
            slRect.anchorMin = new Vector2(0, 1);
            slRect.anchorMax = new Vector2(1, 1);
            slRect.pivot = new Vector2(0.5f, 1);
            slRect.anchoredPosition = new Vector2(0, -50);
            slRect.sizeDelta = new Vector2(0, 25);
            slRect.localScale = Vector3.one;
            TextMeshProUGUI slTmp = styleLabelObj.AddComponent<TextMeshProUGUI>();
            slTmp.text = "Choose Style:";
            slTmp.fontSize = 16;
            slTmp.color = new Color(0.8f, 0.8f, 0.8f);
            slTmp.alignment = TextAlignmentOptions.Center;
            slTmp.raycastTarget = false;

            // Style buttons (3 color options)
            string[] styleNames = { "Classic", "Ocean", "Sunset" };
            Color[] styleColors = {
                new Color(0.3f, 0.6f, 0.3f),  // Classic green
                new Color(0.2f, 0.4f, 0.6f),  // Ocean blue
                new Color(0.6f, 0.4f, 0.3f)   // Sunset orange
            };

            for (int i = 0; i < 3; i++)
            {
                int styleIndex = i;
                GameObject styleBtnObj = new GameObject($"StyleBtn_{i}");
                styleBtnObj.transform.SetParent(pastureCustomizePanel.transform);

                RectTransform sRect = styleBtnObj.AddComponent<RectTransform>();
                sRect.anchorMin = new Vector2(0.5f, 0.5f);
                sRect.anchorMax = new Vector2(0.5f, 0.5f);
                sRect.pivot = new Vector2(0.5f, 0.5f);
                sRect.anchoredPosition = new Vector2(-100 + i * 100, 30);
                sRect.sizeDelta = new Vector2(90, 60);
                sRect.localScale = Vector3.one;

                Image styleImg = styleBtnObj.AddComponent<Image>();
                styleImg.color = styleColors[i];

                Button styleBtn = styleBtnObj.AddComponent<Button>();
                styleBtn.onClick.AddListener(() => SetPastureStyle(styleIndex));

                GameObject styleTextObj = new GameObject("Text");
                styleTextObj.transform.SetParent(styleBtnObj.transform);
                RectTransform stRect = styleTextObj.AddComponent<RectTransform>();
                stRect.anchorMin = Vector2.zero;
                stRect.anchorMax = Vector2.one;
                stRect.offsetMin = Vector2.zero;
                stRect.offsetMax = Vector2.zero;
                stRect.localScale = Vector3.one;
                TextMeshProUGUI stTmp = styleTextObj.AddComponent<TextMeshProUGUI>();
                stTmp.text = styleNames[i];
                stTmp.fontSize = 14;
                stTmp.color = Color.white;
                stTmp.alignment = TextAlignmentOptions.Center;
                stTmp.raycastTarget = false;

                styleButtons.Add(styleBtnObj);
            }

            // Upgrade button
            GameObject upgradeBtnObj = new GameObject("UpgradeBtn");
            upgradeBtnObj.transform.SetParent(pastureCustomizePanel.transform);
            RectTransform ubRect = upgradeBtnObj.AddComponent<RectTransform>();
            ubRect.anchorMin = new Vector2(0.5f, 0);
            ubRect.anchorMax = new Vector2(0.5f, 0);
            ubRect.pivot = new Vector2(0.5f, 0);
            ubRect.anchoredPosition = new Vector2(0, 60);
            ubRect.sizeDelta = new Vector2(200, 45);
            ubRect.localScale = Vector3.one;

            Image ubImg = upgradeBtnObj.AddComponent<Image>();
            ubImg.color = new Color(0.3f, 0.5f, 0.3f);

            Button ubBtn = upgradeBtnObj.AddComponent<Button>();
            ubBtn.onClick.AddListener(UpgradeSelectedPasture);

            GameObject ubTextObj = new GameObject("Text");
            ubTextObj.transform.SetParent(upgradeBtnObj.transform);
            RectTransform ubtRect = ubTextObj.AddComponent<RectTransform>();
            ubtRect.anchorMin = Vector2.zero;
            ubtRect.anchorMax = Vector2.one;
            ubtRect.offsetMin = Vector2.zero;
            ubtRect.offsetMax = Vector2.zero;
            ubtRect.localScale = Vector3.one;
            TextMeshProUGUI ubtTmp = ubTextObj.AddComponent<TextMeshProUGUI>();
            ubtTmp.text = "Upgrade Capacity";
            ubtTmp.fontSize = 16;
            ubtTmp.color = Color.white;
            ubtTmp.alignment = TextAlignmentOptions.Center;
            ubtTmp.raycastTarget = false;

            // Store reference for text updates
            PastureCustomizeRef pcRef = upgradeBtnObj.AddComponent<PastureCustomizeRef>();
            pcRef.UpgradeText = ubtTmp;
            pcRef.UpgradeButtonImage = ubImg;

            // Close button
            GameObject closeObj = new GameObject("CloseBtn");
            closeObj.transform.SetParent(pastureCustomizePanel.transform);
            RectTransform closeRect = closeObj.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1, 1);
            closeRect.anchorMax = new Vector2(1, 1);
            closeRect.pivot = new Vector2(1, 1);
            closeRect.anchoredPosition = new Vector2(-5, -5);
            closeRect.sizeDelta = new Vector2(30, 30);
            closeRect.localScale = Vector3.one;
            Image closeImg = closeObj.AddComponent<Image>();
            closeImg.color = new Color(0.6f, 0.2f, 0.2f);
            Button closeBtn = closeObj.AddComponent<Button>();
            closeBtn.onClick.AddListener(ClosePastureCustomize);
            GameObject closeTextObj = new GameObject("Text");
            closeTextObj.transform.SetParent(closeObj.transform);
            RectTransform closeTextRect = closeTextObj.AddComponent<RectTransform>();
            closeTextRect.anchorMin = Vector2.zero;
            closeTextRect.anchorMax = Vector2.one;
            closeTextRect.offsetMin = Vector2.zero;
            closeTextRect.offsetMax = Vector2.zero;
            closeTextRect.localScale = Vector3.one;
            TextMeshProUGUI closeTmp = closeTextObj.AddComponent<TextMeshProUGUI>();
            closeTmp.text = "X";
            closeTmp.fontSize = 16;
            closeTmp.color = Color.white;
            closeTmp.alignment = TextAlignmentOptions.Center;
            closeTmp.raycastTarget = false;

            pastureCustomizePanel.SetActive(false);
        }

        void OpenPastureCustomize(PlacedHybrid pasture)
        {
            selectedPastureForCustomize = pasture;
            UpdatePastureCustomizeUI();
            pastureCustomizePanel.SetActive(true);
        }

        void ClosePastureCustomize()
        {
            pastureCustomizePanel.SetActive(false);
            selectedPastureForCustomize = null;
        }

        void UpdatePastureCustomizeUI()
        {
            if (selectedPastureForCustomize == null) return;

            var pcRef = FindFirstObjectByType<PastureCustomizeRef>();
            if (pcRef != null)
            {
                if (selectedPastureForCustomize.CanUpgrade())
                {
                    int cost = selectedPastureForCustomize.GetUpgradeCost();
                    pcRef.UpgradeText.text = $"Upgrade Capacity ({selectedPastureForCustomize.Capacity}/3)\n{cost}g";
                    pcRef.UpgradeButtonImage.color = GameData.Gold >= cost
                        ? new Color(0.3f, 0.5f, 0.3f)
                        : new Color(0.4f, 0.3f, 0.3f);
                }
                else
                {
                    pcRef.UpgradeText.text = "Max Capacity (3/3)";
                    pcRef.UpgradeButtonImage.color = new Color(0.3f, 0.3f, 0.3f);
                }
            }

            // Highlight current style
            Color[] styleColors = {
                new Color(0.3f, 0.6f, 0.3f),
                new Color(0.2f, 0.4f, 0.6f),
                new Color(0.6f, 0.4f, 0.3f)
            };
            for (int i = 0; i < styleButtons.Count; i++)
            {
                var img = styleButtons[i].GetComponent<Image>();
                if (i == selectedPastureForCustomize.Style)
                {
                    // Brighten selected style
                    img.color = styleColors[i] * 1.3f;
                }
                else
                {
                    img.color = styleColors[i];
                }
            }
        }

        void SetPastureStyle(int styleIndex)
        {
            if (selectedPastureForCustomize == null) return;
            selectedPastureForCustomize.Style = styleIndex;
            UpdatePastureCustomizeUI();
            RebuildZoo();
            GameData.SaveGame();
            ShowFeedback($"Style changed!", Color.green);
        }

        void UpgradeSelectedPasture()
        {
            if (selectedPastureForCustomize == null) return;

            if (!selectedPastureForCustomize.CanUpgrade())
            {
                ShowFeedback("Already at max capacity!", Color.yellow);
                return;
            }

            int cost = selectedPastureForCustomize.GetUpgradeCost();
            if (GameData.Gold < cost)
            {
                ShowFeedback($"Need {cost}g to upgrade!", Color.red);
                return;
            }

            GameData.Gold -= cost;
            selectedPastureForCustomize.Capacity++;
            UpdatePastureCustomizeUI();
            RebuildZoo();
            GameData.SaveGame();
            ShowFeedback($"Pasture upgraded to capacity {selectedPastureForCustomize.Capacity}!", Color.green);
        }

        // ===== DECORATION CUSTOMIZATION =====
        void CreateDecoCustomizePanel(Transform parent)
        {
            decoCustomizePanel = new GameObject("DecoCustomizePanel");
            decoCustomizePanel.transform.SetParent(parent);

            RectTransform panelRect = decoCustomizePanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(300, 200);
            panelRect.localScale = Vector3.one;

            Image bgImg = decoCustomizePanel.AddComponent<Image>();
            bgImg.color = new Color(0.15f, 0.15f, 0.2f, 0.95f);

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(decoCustomizePanel.transform);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -10);
            titleRect.sizeDelta = new Vector2(0, 35);
            titleRect.localScale = Vector3.one;

            decoCustomizeTitleText = titleObj.AddComponent<TextMeshProUGUI>();
            decoCustomizeTitleText.text = "Customize Decoration";
            decoCustomizeTitleText.fontSize = 20;
            decoCustomizeTitleText.color = Color.white;
            decoCustomizeTitleText.alignment = TextAlignmentOptions.Center;

            // Style label
            GameObject styleLabelObj = new GameObject("StyleLabel");
            styleLabelObj.transform.SetParent(decoCustomizePanel.transform);
            RectTransform slRect = styleLabelObj.AddComponent<RectTransform>();
            slRect.anchorMin = new Vector2(0, 1);
            slRect.anchorMax = new Vector2(1, 1);
            slRect.pivot = new Vector2(0.5f, 1);
            slRect.anchoredPosition = new Vector2(0, -50);
            slRect.sizeDelta = new Vector2(0, 25);
            slRect.localScale = Vector3.one;

            TextMeshProUGUI slText = styleLabelObj.AddComponent<TextMeshProUGUI>();
            slText.text = "Select Style:";
            slText.fontSize = 16;
            slText.color = Color.white;
            slText.alignment = TextAlignmentOptions.Center;

            // Style buttons - 3 styles in a row
            string[] styleNames = { "Classic", "Ocean", "Sunset" };
            Color[] styleColors = {
                new Color(0.5f, 0.4f, 0.3f),
                new Color(0.3f, 0.45f, 0.5f),
                new Color(0.55f, 0.35f, 0.25f)
            };

            for (int i = 0; i < 3; i++)
            {
                int styleIndex = i;
                GameObject styleBtnObj = new GameObject($"DecoStyleBtn_{i}");
                styleBtnObj.transform.SetParent(decoCustomizePanel.transform);

                RectTransform sRect = styleBtnObj.AddComponent<RectTransform>();
                sRect.anchorMin = new Vector2(0.5f, 0.5f);
                sRect.anchorMax = new Vector2(0.5f, 0.5f);
                sRect.pivot = new Vector2(0.5f, 0.5f);
                sRect.anchoredPosition = new Vector2(-80 + i * 80, 0);
                sRect.sizeDelta = new Vector2(70, 50);
                sRect.localScale = Vector3.one;

                Image sImg = styleBtnObj.AddComponent<Image>();
                sImg.color = styleColors[i];

                Button sBtn = styleBtnObj.AddComponent<Button>();
                sBtn.onClick.AddListener(() => SetDecoStyle(styleIndex));

                GameObject sTextObj = new GameObject("Text");
                sTextObj.transform.SetParent(styleBtnObj.transform);
                RectTransform stRect = sTextObj.AddComponent<RectTransform>();
                stRect.anchorMin = Vector2.zero;
                stRect.anchorMax = Vector2.one;
                stRect.offsetMin = Vector2.zero;
                stRect.offsetMax = Vector2.zero;
                stRect.localScale = Vector3.one;

                TextMeshProUGUI stText = sTextObj.AddComponent<TextMeshProUGUI>();
                stText.text = styleNames[i];
                stText.fontSize = 12;
                stText.color = Color.white;
                stText.alignment = TextAlignmentOptions.Center;
                stText.raycastTarget = false;

                decoStyleButtons.Add(styleBtnObj);
            }

            // Close button
            GameObject closeObj = new GameObject("CloseBtn");
            closeObj.transform.SetParent(decoCustomizePanel.transform);
            RectTransform closeRect = closeObj.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1, 1);
            closeRect.anchorMax = new Vector2(1, 1);
            closeRect.pivot = new Vector2(1, 1);
            closeRect.anchoredPosition = new Vector2(-5, -5);
            closeRect.sizeDelta = new Vector2(30, 30);
            closeRect.localScale = Vector3.one;

            Image closeImg = closeObj.AddComponent<Image>();
            closeImg.color = new Color(0.5f, 0.3f, 0.3f);

            Button closeBtn = closeObj.AddComponent<Button>();
            closeBtn.onClick.AddListener(() => CloseDecoCustomize());

            GameObject closeTextObj = new GameObject("Text");
            closeTextObj.transform.SetParent(closeObj.transform);
            RectTransform ctRect = closeTextObj.AddComponent<RectTransform>();
            ctRect.anchorMin = Vector2.zero;
            ctRect.anchorMax = Vector2.one;
            ctRect.offsetMin = Vector2.zero;
            ctRect.offsetMax = Vector2.zero;
            ctRect.localScale = Vector3.one;

            TextMeshProUGUI ctText = closeTextObj.AddComponent<TextMeshProUGUI>();
            ctText.text = "X";
            ctText.fontSize = 18;
            ctText.color = Color.white;
            ctText.alignment = TextAlignmentOptions.Center;
            ctText.raycastTarget = false;

            decoCustomizePanel.SetActive(false);
        }

        void OpenDecoCustomize(PlacedDecoration deco)
        {
            selectedDecoForCustomize = deco;
            if (decoCustomizeTitleText != null)
            {
                decoCustomizeTitleText.text = $"Customize {deco.DecorationType}";
            }
            UpdateDecoCustomizeUI();
            decoCustomizePanel.SetActive(true);
        }

        void CloseDecoCustomize()
        {
            decoCustomizePanel.SetActive(false);
            selectedDecoForCustomize = null;
        }

        void UpdateDecoCustomizeUI()
        {
            if (selectedDecoForCustomize == null) return;

            Color[] styleColors = {
                new Color(0.5f, 0.4f, 0.3f),
                new Color(0.3f, 0.45f, 0.5f),
                new Color(0.55f, 0.35f, 0.25f)
            };

            for (int i = 0; i < decoStyleButtons.Count; i++)
            {
                var img = decoStyleButtons[i].GetComponent<Image>();
                if (i == selectedDecoForCustomize.Style)
                {
                    img.color = styleColors[i] * 1.4f;
                }
                else
                {
                    img.color = styleColors[i];
                }
            }
        }

        void SetDecoStyle(int styleIndex)
        {
            if (selectedDecoForCustomize == null) return;
            selectedDecoForCustomize.Style = styleIndex;
            UpdateDecoCustomizeUI();
            RebuildZoo();
            GameData.SaveGame();
            string[] styleNames = { "Classic", "Ocean", "Sunset" };
            ShowFeedback($"Style: {styleNames[styleIndex]}!", Color.green);
        }

        void TryCustomizeDecoration(Vector2 position)
        {
            float snappedX = Mathf.Round(position.x / cellSize) * cellSize;
            float snappedY = Mathf.Round(position.y / cellSize) * cellSize;
            Vector2 snappedPos = new Vector2(snappedX, snappedY);

            foreach (var deco in GameData.PlacedDecorations)
            {
                if (Vector2.Distance(deco.Position, snappedPos) < cellSize)
                {
                    OpenDecoCustomize(deco);
                    return;
                }
            }
        }

        void RebuildZoo()
        {
            // Clear old
            foreach (var obj in placedObjects)
            {
                if (obj != null) Destroy(obj);
            }
            placedObjects.Clear();

            // Rebuild pastures with hybrids
            foreach (var placed in GameData.PlacedHybrids)
            {
                CreatePasture(placed.Position, placed.PastureType, placed);
            }

            // Rebuild decorations with their styles
            foreach (var deco in GameData.PlacedDecorations)
            {
                CreateDecoration(deco.Position, deco.DecorationType, deco.Style);
            }
        }

        void CreatePasture(Vector2 position, string pastureType, PlacedHybrid placedData)
        {
            GameObject pasture = new GameObject($"Pasture_{position}");
            pasture.transform.parent = zooParent.transform;
            pasture.transform.position = new Vector3(position.x, position.y, 0);

            SpriteRenderer sr = pasture.AddComponent<SpriteRenderer>();
            Texture2D tex = new Texture2D(48, 48);
            Color[] pixels = new Color[48 * 48];

            Color grassColor = GetPastureColor(pastureType, placedData.Style, placedData.Capacity);
            Color fenceColor = GetFenceColor(placedData.Style, placedData.Capacity);

            // Fence thickness increases with capacity
            int fenceThickness = 2 + placedData.Capacity;

            for (int y = 0; y < 48; y++)
            {
                for (int x = 0; x < 48; x++)
                {
                    bool isFence = x < fenceThickness || x >= 48 - fenceThickness ||
                                   y < fenceThickness || y >= 48 - fenceThickness;

                    if (isFence)
                    {
                        // Add decorative corners for higher capacity
                        bool isCorner = (x < fenceThickness + 2 && y < fenceThickness + 2) ||
                                       (x < fenceThickness + 2 && y >= 48 - fenceThickness - 2) ||
                                       (x >= 48 - fenceThickness - 2 && y < fenceThickness + 2) ||
                                       (x >= 48 - fenceThickness - 2 && y >= 48 - fenceThickness - 2);

                        if (isCorner && placedData.Capacity >= 2)
                        {
                            // Golden corner accents for upgraded pastures
                            pixels[y * 48 + x] = Color.Lerp(fenceColor, new Color(0.9f, 0.8f, 0.5f), 0.5f);
                        }
                        else
                        {
                            pixels[y * 48 + x] = fenceColor * Random.Range(0.95f, 1.05f);
                        }
                    }
                    else
                    {
                        // Add some texture variation to grass
                        float variation = Random.Range(0.9f, 1.1f);
                        // Add subtle pattern for high capacity
                        if (placedData.Capacity >= 3 && (x + y) % 8 < 2)
                        {
                            variation *= 1.1f; // Subtle diamond pattern
                        }
                        pixels[y * 48 + x] = grassColor * variation;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 48, 48), new Vector2(0.5f, 0.5f), 32);
            sr.sortingOrder = 0;

            placedObjects.Add(pasture);

            // Add hybrids (now supports multiple)
            if (placedData.HybridIds.Count > 0)
            {
                float[] offsets = GetHybridOffsets(placedData.HybridIds.Count);
                for (int i = 0; i < placedData.HybridIds.Count; i++)
                {
                    var hybrid = GameData.Hybrids.Find(h => h.Id == placedData.HybridIds[i]);
                    if (hybrid != null)
                    {
                        GameObject hybridObj = new GameObject($"Hybrid_{i}");
                        hybridObj.transform.parent = pasture.transform;
                        hybridObj.transform.localPosition = new Vector3(offsets[i], 0, 0);
                        float scale = placedData.HybridIds.Count > 1 ? 0.45f : 0.6f;
                        hybridObj.transform.localScale = Vector3.one * scale;

                        CreatureRenderer renderer = hybridObj.AddComponent<CreatureRenderer>();
                        renderer.RenderHybrid(hybrid);
                    }
                }
            }

            // Store reference for upgrade/destroy interaction
            PastureRef pRef = pasture.AddComponent<PastureRef>();
            pRef.PlacedData = placedData;
        }

        float[] GetHybridOffsets(int count)
        {
            switch (count)
            {
                case 1: return new float[] { 0f };
                case 2: return new float[] { -0.3f, 0.3f };
                case 3: return new float[] { -0.4f, 0f, 0.4f };
                default: return new float[] { 0f };
            }
        }

        Color GetPastureColor(string type, int style, int capacity)
        {
            // Base colors per style
            Color[] styleBaseColors = {
                new Color(0.35f, 0.55f, 0.35f),  // Classic green
                new Color(0.25f, 0.4f, 0.55f),   // Ocean blue
                new Color(0.55f, 0.4f, 0.3f)    // Sunset orange
            };

            Color baseColor = styleBaseColors[Mathf.Clamp(style, 0, 2)];

            // Apply pasture type bonus (brighter/richer)
            switch (type)
            {
                case "GrassPasture":
                    baseColor *= 1.1f;
                    break;
                case "WaterPasture":
                    baseColor *= 1.2f;
                    break;
                case "LuxuryPasture":
                    baseColor *= 1.3f;
                    break;
            }

            // Capacity makes it look fancier (slightly brighter and more saturated)
            float capacityBonus = 1f + (capacity - 1) * 0.1f;
            baseColor *= capacityBonus;

            return baseColor;
        }

        Color GetFenceColor(int style, int capacity)
        {
            // Fence colors upgrade with capacity
            Color[] baseFenceColors = {
                new Color(0.5f, 0.35f, 0.2f),   // Classic brown
                new Color(0.35f, 0.45f, 0.5f),  // Ocean gray-blue
                new Color(0.6f, 0.35f, 0.25f)   // Sunset reddish-brown
            };

            Color fence = baseFenceColors[Mathf.Clamp(style, 0, 2)];

            // Higher capacity = fancier fence (more golden/bright)
            if (capacity >= 2)
            {
                fence = Color.Lerp(fence, new Color(0.7f, 0.6f, 0.4f), 0.3f);
            }
            if (capacity >= 3)
            {
                fence = Color.Lerp(fence, new Color(0.8f, 0.7f, 0.5f), 0.3f);
            }

            return fence;
        }

        void CreateDecoration(Vector2 position, string decoType, int style = 0)
        {
            GameObject deco = new GameObject($"Deco_{decoType}");
            deco.transform.parent = zooParent.transform;
            deco.transform.position = new Vector3(position.x, position.y, 0);

            SpriteRenderer sr = deco.AddComponent<SpriteRenderer>();
            sr.sprite = CreateDecorationSprite(decoType, style);
            sr.sortingOrder = 1;

            placedObjects.Add(deco);
        }

        // Get decoration colors based on style
        Color GetDecorationPrimaryColor(string type, int style)
        {
            // Style 0: Classic, Style 1: Ocean/Cool, Style 2: Sunset/Warm
            Color[,] styleColors = new Color[8, 3]
            {
                // Fence: brown, gray-blue, red-brown
                { new Color(0.5f, 0.35f, 0.2f), new Color(0.4f, 0.45f, 0.5f), new Color(0.55f, 0.3f, 0.2f) },
                // Path: tan, slate, terracotta
                { new Color(0.55f, 0.5f, 0.4f), new Color(0.45f, 0.5f, 0.55f), new Color(0.6f, 0.45f, 0.35f) },
                // Bench: brown, dark gray, mahogany
                { new Color(0.5f, 0.35f, 0.2f), new Color(0.35f, 0.35f, 0.4f), new Color(0.55f, 0.25f, 0.2f) },
                // Lamp: gray post, silver, bronze
                { new Color(0.3f, 0.3f, 0.3f), new Color(0.5f, 0.52f, 0.55f), new Color(0.55f, 0.4f, 0.25f) },
                // Fountain: blue, teal, coral
                { new Color(0.4f, 0.6f, 0.9f), new Color(0.3f, 0.65f, 0.7f), new Color(0.9f, 0.5f, 0.45f) },
                // Statue: gray, blue-gray, gold
                { new Color(0.6f, 0.6f, 0.65f), new Color(0.5f, 0.55f, 0.65f), new Color(0.75f, 0.65f, 0.4f) },
                // Tree trunk: brown, gray, dark brown
                { new Color(0.4f, 0.25f, 0.15f), new Color(0.35f, 0.35f, 0.35f), new Color(0.35f, 0.2f, 0.1f) },
                // Flowers: mixed, blue/purple, red/orange
                { new Color(1f, 0.5f, 0.5f), new Color(0.5f, 0.5f, 1f), new Color(1f, 0.5f, 0.2f) }
            };

            int typeIndex = 0;
            switch (type)
            {
                case "Fence": typeIndex = 0; break;
                case "Path": typeIndex = 1; break;
                case "Bench": typeIndex = 2; break;
                case "Lamp": typeIndex = 3; break;
                case "Fountain": typeIndex = 4; break;
                case "Statue": typeIndex = 5; break;
                case "Tree": typeIndex = 6; break;
                case "Flowers": typeIndex = 7; break;
            }

            return styleColors[typeIndex, Mathf.Clamp(style, 0, 2)];
        }

        Color GetDecorationSecondaryColor(string type, int style)
        {
            // Secondary/accent colors for each style
            Color[,] styleColors = new Color[8, 3]
            {
                // Fence (same as primary)
                { new Color(0.5f, 0.35f, 0.2f), new Color(0.4f, 0.45f, 0.5f), new Color(0.55f, 0.3f, 0.2f) },
                // Path (same as primary)
                { new Color(0.55f, 0.5f, 0.4f), new Color(0.45f, 0.5f, 0.55f), new Color(0.6f, 0.45f, 0.35f) },
                // Bench legs: dark brown, dark gray, dark mahogany
                { new Color(0.4f, 0.3f, 0.15f), new Color(0.25f, 0.25f, 0.3f), new Color(0.4f, 0.2f, 0.15f) },
                // Lamp light: yellow, white-blue, orange
                { new Color(1f, 0.9f, 0.5f), new Color(0.8f, 0.9f, 1f), new Color(1f, 0.7f, 0.3f) },
                // Fountain (same as primary)
                { new Color(0.4f, 0.6f, 0.9f), new Color(0.3f, 0.65f, 0.7f), new Color(0.9f, 0.5f, 0.45f) },
                // Statue (same as primary)
                { new Color(0.6f, 0.6f, 0.65f), new Color(0.5f, 0.55f, 0.65f), new Color(0.75f, 0.65f, 0.4f) },
                // Tree leaves: green, blue-green, autumn
                { new Color(0.2f, 0.5f, 0.2f), new Color(0.2f, 0.45f, 0.4f), new Color(0.7f, 0.4f, 0.15f) },
                // Flowers secondary colors
                { new Color(1f, 1f, 0.3f), new Color(0.7f, 0.3f, 0.8f), new Color(1f, 0.3f, 0.3f) }
            };

            int typeIndex = 0;
            switch (type)
            {
                case "Fence": typeIndex = 0; break;
                case "Path": typeIndex = 1; break;
                case "Bench": typeIndex = 2; break;
                case "Lamp": typeIndex = 3; break;
                case "Fountain": typeIndex = 4; break;
                case "Statue": typeIndex = 5; break;
                case "Tree": typeIndex = 6; break;
                case "Flowers": typeIndex = 7; break;
            }

            return styleColors[typeIndex, Mathf.Clamp(style, 0, 2)];
        }

        Sprite CreateDecorationSprite(string type, int style = 0)
        {
            int size = 24;
            Texture2D tex = new Texture2D(size, size);
            Color[] pixels = new Color[size * size];

            Color primary = GetDecorationPrimaryColor(type, style);
            Color secondary = GetDecorationSecondaryColor(type, style);

            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.clear;

            switch (type)
            {
                case "Fence":
                    for (int x = 0; x < size; x++)
                    {
                        for (int y = 8; y < 16; y++)
                        {
                            if (x % 4 < 2 || y == 8 || y == 15)
                                pixels[y * size + x] = primary;
                        }
                    }
                    break;

                case "Path":
                    for (int y = 0; y < size; y++)
                        for (int x = 0; x < size; x++)
                            pixels[y * size + x] = primary * Random.Range(0.9f, 1.1f);
                    break;

                case "Bench":
                    for (int y = 6; y < 12; y++)
                        for (int x = 4; x < 20; x++)
                            pixels[y * size + x] = primary;
                    for (int y = 0; y < 6; y++)
                    {
                        pixels[y * size + 5] = secondary;
                        pixels[y * size + 18] = secondary;
                    }
                    break;

                case "Lamp":
                    for (int y = 0; y < 16; y++)
                        pixels[y * size + 12] = primary;
                    for (int y = 16; y < 22; y++)
                        for (int x = 8; x < 16; x++)
                            pixels[y * size + x] = secondary;
                    break;

                case "Fountain":
                    for (int y = 0; y < size; y++)
                    {
                        for (int x = 0; x < size; x++)
                        {
                            float dx = (x - 12) / 10f;
                            float dy = (y - 12) / 10f;
                            if (dx * dx + dy * dy < 1f)
                                pixels[y * size + x] = primary;
                        }
                    }
                    break;

                case "Statue":
                    for (int y = 0; y < 8; y++)
                        for (int x = 8; x < 16; x++)
                            pixels[y * size + x] = primary;
                    for (int y = 8; y < 20; y++)
                        for (int x = 10; x < 14; x++)
                            pixels[y * size + x] = primary;
                    break;

                case "Tree":
                    for (int y = 0; y < 8; y++)
                        for (int x = 10; x < 14; x++)
                            pixels[y * size + x] = primary; // trunk
                    for (int y = 8; y < 22; y++)
                    {
                        for (int x = 0; x < size; x++)
                        {
                            float dx = (x - 12) / 10f;
                            float dy = (y - 15) / 7f;
                            if (dx * dx + dy * dy < 1f)
                                pixels[y * size + x] = secondary * Random.Range(0.8f, 1.2f); // leaves
                        }
                    }
                    break;

                case "Flowers":
                    // Different flower color palettes per style
                    Color[][] flowerPalettes = {
                        new Color[] { Color.red, Color.yellow, new Color(1f, 0.5f, 0.8f), Color.white }, // Classic
                        new Color[] { new Color(0.5f, 0.5f, 1f), new Color(0.7f, 0.3f, 0.8f), new Color(0.3f, 0.7f, 0.9f), Color.white }, // Cool
                        new Color[] { Color.red, new Color(1f, 0.5f, 0f), new Color(1f, 0.8f, 0f), new Color(1f, 0.3f, 0.3f) } // Warm
                    };
                    Color[] flowerColors = flowerPalettes[Mathf.Clamp(style, 0, 2)];
                    for (int f = 0; f < 5; f++)
                    {
                        int fx = Random.Range(4, 20);
                        int fy = Random.Range(4, 20);
                        Color fc = flowerColors[Random.Range(0, flowerColors.Length)];
                        for (int dx = -2; dx <= 2; dx++)
                            for (int dy = -2; dy <= 2; dy++)
                                if (dx * dx + dy * dy <= 4)
                                    SetPixelSafe(pixels, size, fx + dx, fy + dy, fc);
                    }
                    break;
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32);
        }

        void SetPixelSafe(Color[] pixels, int size, int x, int y, Color color)
        {
            if (x >= 0 && x < size && y >= 0 && y < size)
                pixels[y * size + x] = color;
        }

        void Update()
        {
            HandleInput();
            UpdateIdleIncome();
            UpdateVisitors();
            UpdateUI();
            UpdateFeedback();
            UpdatePlacementPreview();

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
                // Scene switching (only if not placing)
                if (string.IsNullOrEmpty(selectedPlacement))
                {
                    if (keyboard.tabKey.wasPressedThisFrame)
                        SceneManager.LoadScene("ACD_FusionLab");
                    if (keyboard.digit1Key.wasPressedThisFrame)
                        SceneManager.LoadScene("ACD_Exploration");
                    if (keyboard.xKey.wasPressedThisFrame)
                        SceneManager.LoadScene("ACD_Skills");
                }

                // Cancel placement with ESC
                if (keyboard.escapeKey.wasPressedThisFrame)
                {
                    if (string.IsNullOrEmpty(selectedPlacement))
                        SceneManager.LoadScene("SampleScene");
                    else
                        CancelPlacement();
                }

                // Cycle decoration style with S while placing a decoration
                if (keyboard.sKey.wasPressedThisFrame && !string.IsNullOrEmpty(selectedPlacement))
                {
                    // Check if it's a decoration (not a pasture or hybrid)
                    if (selectedPlacement != "BasicPasture" && selectedPlacement != "GrassPasture" &&
                        selectedPlacement != "WaterPasture" && selectedPlacement != "LuxuryPasture" &&
                        selectedPlacement != "Hybrid")
                    {
                        selectedDecoStyle = (selectedDecoStyle + 1) % 3;
                        string[] styleNames = { "Classic", "Ocean", "Sunset" };
                        ShowFeedback($"Style: {styleNames[selectedDecoStyle]}", Color.cyan);
                        UpdatePlacementPreview(); // Refresh preview with new style
                    }
                }

                // Placement modes (when not already placing)
                if (string.IsNullOrEmpty(selectedPlacement))
                {
                    if (keyboard.pKey.wasPressedThisFrame)
                        StartPasturePlacement();
                    if (keyboard.hKey.wasPressedThisFrame)
                        StartHybridPlacement();
                    // Decorations are now selected via UI buttons
                }
            }

            if (mouse != null)
            {
                // Right-click to cancel placement or destroy mode
                if (mouse.rightButton.wasPressedThisFrame)
                {
                    if (!string.IsNullOrEmpty(selectedPlacement))
                        CancelPlacement();
                    else if (destroyMode)
                        ToggleDestroyMode();
                }

                // Left-click handling
                if (mouse.leftButton.wasPressedThisFrame)
                {
                    Vector3 worldPos = mainCamera.ScreenToWorldPoint(mouse.position.ReadValue());
                    Vector2 clickPos = new Vector2(worldPos.x, worldPos.y);

                    if (destroyMode)
                    {
                        TryDestroy(clickPos);
                    }
                    else if (!string.IsNullOrEmpty(selectedPlacement))
                    {
                        if (canPlaceAtCurrentPos)
                        {
                            TryPlace(clickPos);
                        }
                        else
                        {
                            ShowFeedback("Cannot place here!", Color.red);
                        }
                    }
                    else
                    {
                        // Check if clicking on a pasture for upgrade, or decoration for customize
                        if (!TryUpgradePasture(clickPos))
                        {
                            TryCustomizeDecoration(clickPos);
                        }
                    }
                }
            }
        }

        void ToggleDestroyMode()
        {
            destroyMode = !destroyMode;
            CancelPlacement();

            // Update destroy button color
            var btnRef = FindFirstObjectByType<DestroyButtonRef>();
            if (btnRef != null)
            {
                btnRef.ButtonImage.color = destroyMode ? new Color(0.8f, 0.2f, 0.2f) : new Color(0.5f, 0.2f, 0.2f);
            }

            if (destroyMode)
            {
                ShowFeedback("DESTROY MODE - Click to remove objects (50% refund)", Color.red);
            }
            else
            {
                ShowFeedback("Destroy mode disabled", Color.yellow);
            }
        }

        void TryDestroy(Vector2 position)
        {
            float snappedX = Mathf.Round(position.x / cellSize) * cellSize;
            float snappedY = Mathf.Round(position.y / cellSize) * cellSize;
            Vector2 snappedPos = new Vector2(snappedX, snappedY);

            // Check decorations first
            for (int i = GameData.PlacedDecorations.Count - 1; i >= 0; i--)
            {
                if (Vector2.Distance(GameData.PlacedDecorations[i].Position, snappedPos) < cellSize * 0.5f)
                {
                    int refund = GetDecorationCost(GameData.PlacedDecorations[i].DecorationType) / 2;
                    GameData.Gold += refund;
                    ShowFeedback($"Removed {GameData.PlacedDecorations[i].DecorationType} (+{refund}g)", Color.yellow);
                    GameData.PlacedDecorations.RemoveAt(i);
                    RebuildZoo();
                    GameData.SaveGame();
                    return;
                }
            }

            // Check pastures
            for (int i = GameData.PlacedHybrids.Count - 1; i >= 0; i--)
            {
                if (Vector2.Distance(GameData.PlacedHybrids[i].Position, snappedPos) < cellSize)
                {
                    int refund = GetPastureCost(GameData.PlacedHybrids[i].PastureType) / 2;
                    // Also refund upgrade costs
                    if (GameData.PlacedHybrids[i].Capacity > 1)
                        refund += 250; // Half of 500
                    if (GameData.PlacedHybrids[i].Capacity > 2)
                        refund += 1250; // Half of 2500
                    GameData.Gold += refund;
                    ShowFeedback($"Removed pasture (+{refund}g) - Hybrids returned to inventory", Color.yellow);
                    GameData.PlacedHybrids.RemoveAt(i);
                    RebuildZoo();
                    GameData.SaveGame();
                    return;
                }
            }

            ShowFeedback("Nothing to destroy here", Color.gray);
        }

        bool TryUpgradePasture(Vector2 position)
        {
            float snappedX = Mathf.Round(position.x / cellSize) * cellSize;
            float snappedY = Mathf.Round(position.y / cellSize) * cellSize;
            Vector2 snappedPos = new Vector2(snappedX, snappedY);

            foreach (var placed in GameData.PlacedHybrids)
            {
                if (Vector2.Distance(placed.Position, snappedPos) < cellSize)
                {
                    // Open customization panel instead of directly upgrading
                    OpenPastureCustomize(placed);
                    return true;
                }
            }
            return false;
        }

        void StartPasturePlacement()
        {
            // Cycle through available pastures
            string[] pastures = { "BasicPasture", "GrassPasture", "WaterPasture", "LuxuryPasture" };
            foreach (var p in pastures)
            {
                if (GameData.UnlockedPastures.Contains(p))
                {
                    selectedPlacement = p;
                    selectedHybridIndex = -1;
                    CreatePreviewObject();
                    ShowFeedback($"Placing {p} - Click to place, Right-click to cancel", Color.cyan);
                    return;
                }
            }
        }

        void StartHybridPlacement()
        {
            if (GameData.Hybrids.Count == 0)
            {
                ShowFeedback("No hybrids to place! Create some first.", Color.red);
                return;
            }

            // Show hybrid selection panel
            OpenHybridSelection();
        }

        void OpenHybridSelection()
        {
            hybridScrollOffset = 0;
            RefreshHybridSelectionButtons();
            hybridSelectionPanel.SetActive(true);
        }

        void CloseHybridSelection()
        {
            hybridSelectionPanel.SetActive(false);
            CancelPlacement();
        }

        void ScrollHybrids(int direction)
        {
            List<int> unplacedIndices = GetUnplacedHybridIndices();
            int maxOffset = Mathf.Max(0, unplacedIndices.Count - MAX_VISIBLE_HYBRIDS);
            hybridScrollOffset = Mathf.Clamp(hybridScrollOffset + direction, 0, maxOffset);
            RefreshHybridSelectionButtons();
        }

        List<int> GetUnplacedHybridIndices()
        {
            List<int> unplaced = new List<int>();
            for (int i = 0; i < GameData.Hybrids.Count; i++)
            {
                bool isPlaced = false;
                foreach (var ph in GameData.PlacedHybrids)
                {
                    if (ph.HybridIds.Contains(GameData.Hybrids[i].Id))
                    {
                        isPlaced = true;
                        break;
                    }
                }
                if (!isPlaced)
                {
                    unplaced.Add(i);
                }
            }
            return unplaced;
        }

        void RefreshHybridSelectionButtons()
        {
            // Clear existing buttons
            foreach (var btn in hybridSelectionButtons)
            {
                if (btn != null) Destroy(btn);
            }
            hybridSelectionButtons.Clear();

            List<int> unplacedIndices = GetUnplacedHybridIndices();

            if (unplacedIndices.Count == 0)
            {
                ShowFeedback("All hybrids are placed!", Color.yellow);
                CloseHybridSelection();
                return;
            }

            float startY = 100;
            for (int i = 0; i < MAX_VISIBLE_HYBRIDS && i + hybridScrollOffset < unplacedIndices.Count; i++)
            {
                int hybridIdx = unplacedIndices[i + hybridScrollOffset];
                var hybrid = GameData.Hybrids[hybridIdx];

                GameObject btnObj = new GameObject($"HybridBtn_{i}");
                btnObj.transform.SetParent(hybridSelectionPanel.transform);

                RectTransform rect = btnObj.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(0, startY - i * 45);
                rect.sizeDelta = new Vector2(280, 40);
                rect.localScale = Vector3.one;

                Image img = btnObj.AddComponent<Image>();
                img.color = new Color(0.25f, 0.3f, 0.35f);

                Button btn = btnObj.AddComponent<Button>();
                int idx = hybridIdx;
                btn.onClick.AddListener(() => SelectHybridForPlacement(idx));

                GameObject textObj = new GameObject("Text");
                textObj.transform.SetParent(btnObj.transform);
                RectTransform textRect = textObj.AddComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(10, 0);
                textRect.offsetMax = new Vector2(-10, 0);
                textRect.localScale = Vector3.one;

                TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
                tmp.text = $"{hybrid.Name} (Value: {hybrid.GetTotalValue()})";
                tmp.fontSize = 14;
                tmp.color = Color.white;
                tmp.alignment = TextAlignmentOptions.MidlineLeft;
                tmp.raycastTarget = false;

                hybridSelectionButtons.Add(btnObj);
            }
        }

        void SelectHybridForPlacement(int hybridIndex)
        {
            selectedHybridIndex = hybridIndex;
            selectedPlacement = "Hybrid";
            hybridSelectionPanel.SetActive(false);
            CreatePreviewObject();
            ShowFeedback($"Placing {GameData.Hybrids[hybridIndex].Name} - Click on pasture with room", Color.cyan);
        }

        void TrySelectDecoration(string decoType)
        {
            if (!GameData.UnlockedDecorations.Contains(decoType))
            {
                ShowFeedback($"{decoType} not unlocked! Buy in Skills shop.", Color.red);
                return;
            }

            selectedPlacement = decoType;
            selectedHybridIndex = -1;
            selectedDecoStyle = 0; // Reset to default style when starting new placement
            CreatePreviewObject();
            int cost = GetDecorationCost(decoType);
            ShowFeedback($"Placing {decoType} - S:Style, Click:Place ({cost}g), RightClick:Cancel", Color.cyan);
        }

        void CreatePreviewObject()
        {
            if (placementPreview != null)
                Destroy(placementPreview);

            placementPreview = new GameObject("Preview");
            previewRenderer = placementPreview.AddComponent<SpriteRenderer>();
            previewRenderer.sortingOrder = 100;

            // Create preview sprite based on what we're placing
            if (selectedPlacement.Contains("Pasture"))
            {
                Texture2D tex = new Texture2D(48, 48);
                Color[] pixels = new Color[48 * 48];
                // Preview uses default style (0) and capacity (1)
                Color grassColor = GetPastureColor(selectedPlacement, 0, 1);
                Color fenceColor = GetFenceColor(0, 1);

                for (int y = 0; y < 48; y++)
                {
                    for (int x = 0; x < 48; x++)
                    {
                        bool isFence = x < 3 || x >= 45 || y < 3 || y >= 45;
                        pixels[y * 48 + x] = isFence ? fenceColor : grassColor;
                    }
                }
                tex.SetPixels(pixels);
                tex.Apply();
                tex.filterMode = FilterMode.Point;
                previewRenderer.sprite = Sprite.Create(tex, new Rect(0, 0, 48, 48), new Vector2(0.5f, 0.5f), 32);
            }
            else if (selectedPlacement == "Hybrid")
            {
                // For hybrid, just show a simple marker
                Texture2D tex = new Texture2D(24, 24);
                Color[] pixels = new Color[24 * 24];
                for (int i = 0; i < pixels.Length; i++)
                {
                    int x = i % 24;
                    int y = i / 24;
                    float dx = (x - 12) / 10f;
                    float dy = (y - 12) / 10f;
                    pixels[i] = (dx * dx + dy * dy < 1f) ? new Color(0.8f, 0.6f, 1f) : Color.clear;
                }
                tex.SetPixels(pixels);
                tex.Apply();
                tex.filterMode = FilterMode.Point;
                previewRenderer.sprite = Sprite.Create(tex, new Rect(0, 0, 24, 24), new Vector2(0.5f, 0.5f), 32);
            }
            else
            {
                // Decoration - use current selected style
                previewRenderer.sprite = CreateDecorationSprite(selectedPlacement, selectedDecoStyle);
            }
        }

        void TryPlace(Vector2 position)
        {
            // Round to grid
            float snappedX = Mathf.Round(position.x / cellSize) * cellSize;
            float snappedY = Mathf.Round(position.y / cellSize) * cellSize;
            Vector2 snappedPos = new Vector2(snappedX, snappedY);

            if (selectedPlacement == "Hybrid")
            {
                // Check if clicking on a pasture with room
                foreach (var placed in GameData.PlacedHybrids)
                {
                    if (Vector2.Distance(placed.Position, snappedPos) < cellSize && placed.HasRoom())
                    {
                        // Place hybrid in pasture
                        placed.AddHybrid(GameData.Hybrids[selectedHybridIndex].Id);
                        ShowFeedback($"Placed {GameData.Hybrids[selectedHybridIndex].Name}! ({placed.GetHybridCount()}/{placed.Capacity})", Color.green);
                        CancelPlacement();
                        RebuildZoo();
                        GameData.SaveGame();
                        return;
                    }
                }
                ShowFeedback("Click on a pasture with room!", Color.red);
            }
            else if (selectedPlacement.Contains("Pasture"))
            {
                int cost = GetPastureCost(selectedPlacement);
                if (GameData.Gold < cost)
                {
                    ShowFeedback($"Need {cost} gold!", Color.red);
                    return;
                }

                GameData.Gold -= cost;
                PlacedHybrid newPasture = new PlacedHybrid
                {
                    Position = snappedPos,
                    PastureType = selectedPlacement,
                    Capacity = 1
                };
                GameData.PlacedHybrids.Add(newPasture);
                ShowFeedback($"Placed {selectedPlacement}!", Color.green);
                RebuildZoo();
                GameData.SaveGame();
            }
            else
            {
                // Decoration
                int cost = GetDecorationCost(selectedPlacement);
                if (GameData.Gold < cost)
                {
                    ShowFeedback($"Need {cost} gold!", Color.red);
                    return;
                }

                GameData.Gold -= cost;
                PlacedDecoration newDeco = new PlacedDecoration
                {
                    Position = snappedPos,
                    DecorationType = selectedPlacement,
                    Style = selectedDecoStyle
                };
                GameData.PlacedDecorations.Add(newDeco);
                string[] styleNames = { "Classic", "Ocean", "Sunset" };
                ShowFeedback($"Placed {selectedPlacement} ({styleNames[selectedDecoStyle]})!", Color.green);
                RebuildZoo();
                GameData.SaveGame();
            }
        }

        int GetDecorationCost(string type)
        {
            // Decoration costs balanced for 2-3 hour gameplay
            switch (type)
            {
                case "Fence": return 25;
                case "Path": return 25;
                case "Bench": return 100;
                case "Lamp": return 150;
                case "Tree": return 300;
                case "Flowers": return 400;
                case "Fountain": return 2000;
                case "Statue": return 8000;
                default: return 50;
            }
        }

        int GetPastureCost(string type)
        {
            // Pasture costs balanced for 2-3 hour gameplay
            switch (type)
            {
                case "BasicPasture": return 200;
                case "GrassPasture": return 1500;
                case "WaterPasture": return 8000;
                case "LuxuryPasture": return 40000;
                default: return 200;
            }
        }

        void CancelPlacement()
        {
            selectedPlacement = "";
            selectedHybridIndex = -1;
            if (placementPreview != null)
            {
                Destroy(placementPreview);
                placementPreview = null;
                previewRenderer = null;
            }
            ShowFeedback("Placement cancelled", Color.yellow);
        }

        void UpdatePlacementPreview()
        {
            if (string.IsNullOrEmpty(selectedPlacement) || placementPreview == null)
            {
                return;
            }

            var mouse = Mouse.current;
            if (mouse == null) return;

            Vector3 worldPos = mainCamera.ScreenToWorldPoint(mouse.position.ReadValue());
            float snappedX = Mathf.Round(worldPos.x / cellSize) * cellSize;
            float snappedY = Mathf.Round(worldPos.y / cellSize) * cellSize;
            Vector2 snappedPos = new Vector2(snappedX, snappedY);

            placementPreview.transform.position = new Vector3(snappedX, snappedY, 0);

            // Check if placement is valid
            canPlaceAtCurrentPos = CanPlaceAt(snappedPos);

            // Update preview color
            if (previewRenderer != null)
            {
                if (canPlaceAtCurrentPos)
                {
                    previewRenderer.color = new Color(0.3f, 1f, 0.3f, 0.7f); // Green tint
                }
                else
                {
                    previewRenderer.color = new Color(1f, 0.3f, 0.3f, 0.7f); // Red tint
                }
            }
        }

        bool CanPlaceAt(Vector2 position)
        {
            // Check bounds
            float halfWidth = gridWidth * cellSize / 2f;
            float halfHeight = gridHeight * cellSize / 2f;
            if (position.x < -halfWidth || position.x > halfWidth ||
                position.y < -halfHeight || position.y > halfHeight)
            {
                return false;
            }

            if (selectedPlacement == "Hybrid")
            {
                // Can only place on pastures with room
                foreach (var placed in GameData.PlacedHybrids)
                {
                    if (Vector2.Distance(placed.Position, position) < cellSize * 0.5f && placed.HasRoom())
                    {
                        return true;
                    }
                }
                return false;
            }
            else if (selectedPlacement.Contains("Pasture"))
            {
                // Check if overlapping existing pasture
                foreach (var placed in GameData.PlacedHybrids)
                {
                    if (Vector2.Distance(placed.Position, position) < cellSize * 1.2f)
                    {
                        return false;
                    }
                }
                // Check gold
                return GameData.Gold >= GetPastureCost(selectedPlacement);
            }
            else
            {
                // Decoration - check overlaps and gold
                foreach (var deco in GameData.PlacedDecorations)
                {
                    if (Vector2.Distance(deco.Position, position) < cellSize * 0.5f)
                    {
                        return false;
                    }
                }
                return GameData.Gold >= GetDecorationCost(selectedPlacement);
            }
        }

        void UpdateIdleIncome()
        {
            // Use global idle income tracking so it works across all scenes
            GameData.UpdateGlobalIdleIncome();
        }

        void UpdateVisitors()
        {
            // Clean up old visitors
            visitorObjects.RemoveAll(v => v == null);

            int numHybrids = 0;
            foreach (var p in GameData.PlacedHybrids)
            {
                numHybrids += p.GetHybridCount();
            }

            if (numHybrids == 0) return;

            // Dynamically update max visitors based on income
            int income = GameData.CalculateTotalIdleIncome();
            maxVisitors = Mathf.Clamp(2 + income / 10, 2, 20);

            visitorSpawnTimer += Time.deltaTime;
            if (visitorSpawnTimer >= 2f && visitorObjects.Count < maxVisitors)
            {
                visitorSpawnTimer = 0f;
                SpawnVisitor();
            }
        }

        void SpawnVisitor()
        {
            GameObject visitor = new GameObject("Visitor");
            visitor.transform.parent = zooParent.transform;

            // Spawn from edges
            float side = Random.value;
            Vector3 spawnPos;
            if (side < 0.25f)
                spawnPos = new Vector3(-7f, Random.Range(-4f, 4f), 0);
            else if (side < 0.5f)
                spawnPos = new Vector3(7f, Random.Range(-4f, 4f), 0);
            else if (side < 0.75f)
                spawnPos = new Vector3(Random.Range(-6f, 6f), -5f, 0);
            else
                spawnPos = new Vector3(Random.Range(-6f, 6f), 5f, 0);

            visitor.transform.position = spawnPos;

            SpriteRenderer sr = visitor.AddComponent<SpriteRenderer>();
            Texture2D tex = new Texture2D(16, 24);
            Color[] pixels = new Color[16 * 24];

            Color robeColor = new Color(Random.Range(0.3f, 0.8f), Random.Range(0.2f, 0.5f), Random.Range(0.5f, 1f));

            for (int y = 0; y < 24; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    pixels[y * 16 + x] = Color.clear;
                    // Robe
                    if (y < 16 && x >= 4 && x < 12)
                        pixels[y * 16 + x] = robeColor;
                    // Head
                    if (y >= 16 && y < 22)
                    {
                        float dx = (x - 8) / 4f;
                        float dy = (y - 19) / 3f;
                        if (dx * dx + dy * dy < 1f)
                            pixels[y * 16 + x] = new Color(0.9f, 0.8f, 0.7f);
                    }
                    // Hat
                    if (y >= 22 && x >= 5 && x < 11)
                        pixels[y * 16 + x] = robeColor * 0.7f;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 16, 24), new Vector2(0.5f, 0f), 24);
            sr.sortingOrder = 5;

            VisitorBehavior vb = visitor.AddComponent<VisitorBehavior>();
            vb.Initialize(GameData.PlacedHybrids);

            visitorObjects.Add(visitor);
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
                    feedbackText.text = "";
            }
        }

        void UpdateUI()
        {
            if (goldText != null)
                goldText.text = $"Gold: {GameData.Gold}";

            if (incomeText != null)
            {
                int income = GameData.CalculateTotalIdleIncome();
                incomeText.text = $"Income: {income}/2s";
            }

            if (modeText != null)
            {
                if (destroyMode)
                {
                    modeText.text = "Mode: DESTROY\nClick to remove objects";
                }
                else if (!string.IsNullOrEmpty(selectedPlacement))
                {
                    modeText.text = $"Mode: Placing {selectedPlacement}";
                }
                else
                {
                    modeText.text = $"Mode: View (Click pasture to customize)\nHybrids placed: {CountPlacedHybrids()}";
                }
            }

            UpdateDecoButtonColors();
        }

        void UpdateDecoButtonColors()
        {
            // Green: unlocked and can afford
            // Gray: not unlocked
            // Red: unlocked but can't afford
            Color greenColor = new Color(0.25f, 0.5f, 0.25f);
            Color grayColor = new Color(0.35f, 0.35f, 0.35f);
            Color redColor = new Color(0.5f, 0.25f, 0.25f);

            foreach (var kvp in decoButtonImages)
            {
                string decoType = kvp.Key;
                Image img = kvp.Value;
                if (img == null) continue;

                bool isUnlocked = GameData.UnlockedDecorations.Contains(decoType);
                int cost = GetDecorationCost(decoType);
                bool canAfford = GameData.Gold >= cost;

                if (!isUnlocked)
                {
                    img.color = grayColor;
                }
                else if (!canAfford)
                {
                    img.color = redColor;
                }
                else
                {
                    img.color = greenColor;
                }
            }
        }

        int CountPlacedHybrids()
        {
            int count = 0;
            foreach (var p in GameData.PlacedHybrids)
            {
                count += p.GetHybridCount();
            }
            return count;
        }
    }

    public class VisitorBehavior : MonoBehaviour
    {
        private Vector3 targetPos;
        private float speed = 1.5f;
        private float waitTimer;
        private List<PlacedHybrid> pastures;
        private int visitedCount = 0;
        private int maxVisits = 5;
        private float pastureRadius = 1.0f;

        // Anti-vibration: smooth direction and stuck detection
        private Vector3 smoothedDirection = Vector3.zero;
        private Vector3 lastPosition;
        private float stuckTimer = 0f;
        private const float STUCK_THRESHOLD = 0.1f;
        private const float STUCK_TIME = 1.5f;

        public void Initialize(List<PlacedHybrid> pastureList)
        {
            pastures = pastureList;
            lastPosition = transform.position;
            PickNewTarget();
        }

        void PickNewTarget()
        {
            // Try to visit a pasture with a hybrid (stand at edge, not inside)
            List<PlacedHybrid> validTargets = new List<PlacedHybrid>();
            if (pastures != null)
            {
                foreach (var p in pastures)
                {
                    if (p.GetHybridCount() > 0)
                    {
                        validTargets.Add(p);
                    }
                }
            }

            if (validTargets.Count > 0 && Random.value > 0.3f)
            {
                var target = validTargets[Random.Range(0, validTargets.Count)];
                // Find a clear viewing spot - try multiple angles
                float bestAngle = Random.Range(0f, Mathf.PI * 2f);
                float bestClearance = 0f;

                for (int i = 0; i < 8; i++)
                {
                    float testAngle = i * Mathf.PI * 0.25f;
                    float edgeDist = pastureRadius + 0.5f;
                    Vector3 testPos = new Vector3(
                        target.Position.x + Mathf.Cos(testAngle) * edgeDist,
                        target.Position.y + Mathf.Sin(testAngle) * edgeDist, 0);

                    float clearance = GetMinPastureClearance(testPos, target.Position);
                    if (clearance > bestClearance)
                    {
                        bestClearance = clearance;
                        bestAngle = testAngle;
                    }
                }

                float finalDist = pastureRadius + 0.5f;
                targetPos = new Vector3(
                    target.Position.x + Mathf.Cos(bestAngle) * finalDist,
                    target.Position.y + Mathf.Sin(bestAngle) * finalDist, 0);
            }
            else
            {
                // Random wander - find a clear spot
                int attempts = 0;
                do
                {
                    targetPos = new Vector3(Random.Range(-5f, 5f), Random.Range(-3.5f, 3.5f), 0);
                    attempts++;
                } while (IsInsideAnyPasture(targetPos) && attempts < 20);
            }

            waitTimer = 0f;
            stuckTimer = 0f;
        }

        float GetMinPastureClearance(Vector3 pos, Vector2 excludePasture)
        {
            if (pastures == null) return 10f;
            float minClearance = 10f;
            foreach (var p in pastures)
            {
                if (Vector2.Distance(p.Position, excludePasture) < 0.1f) continue;
                float dist = Vector2.Distance(new Vector2(pos.x, pos.y), p.Position);
                float clearance = dist - pastureRadius;
                if (clearance < minClearance) minClearance = clearance;
            }
            return minClearance;
        }

        bool IsInsideAnyPasture(Vector3 pos)
        {
            if (pastures == null) return false;
            foreach (var p in pastures)
            {
                float dist = Vector2.Distance(new Vector2(pos.x, pos.y), p.Position);
                if (dist < pastureRadius)
                    return true;
            }
            return false;
        }

        Vector3 GetAvoidanceDirection(Vector3 currentPos, Vector3 desiredDir)
        {
            if (pastures == null) return desiredDir;

            Vector3 avoidance = Vector3.zero;
            int nearbyCount = 0;

            foreach (var p in pastures)
            {
                Vector2 pasturePos = p.Position;
                float dist = Vector2.Distance(new Vector2(currentPos.x, currentPos.y), pasturePos);

                if (dist < pastureRadius + 0.6f)
                {
                    nearbyCount++;
                    Vector3 awayFromPasture = currentPos - new Vector3(pasturePos.x, pasturePos.y, 0);
                    if (awayFromPasture.magnitude > 0.01f)
                    {
                        awayFromPasture.Normalize();
                        float strength = 1f - (dist / (pastureRadius + 0.6f));
                        avoidance += awayFromPasture * strength * 2f;
                    }
                }
            }

            // If surrounded by multiple pastures, prioritize escaping over reaching target
            if (nearbyCount >= 2 && avoidance.magnitude > 0.5f)
            {
                return avoidance.normalized;
            }

            Vector3 result = desiredDir + avoidance;
            if (result.magnitude < 0.1f)
            {
                // Deadlock - move perpendicular to break tie
                return new Vector3(-desiredDir.y, desiredDir.x, 0).normalized;
            }
            return result.normalized;
        }

        void Update()
        {
            if (waitTimer > 0)
            {
                waitTimer -= Time.deltaTime;
                if (waitTimer <= 0)
                {
                    visitedCount++;
                    if (visitedCount >= maxVisits)
                    {
                        targetPos = new Vector3(transform.position.x > 0 ? 10f : -10f, transform.position.y, 0);
                    }
                    else
                    {
                        PickNewTarget();
                    }
                }
                return;
            }

            // Check if stuck
            float distMoved = Vector3.Distance(transform.position, lastPosition);
            if (distMoved < STUCK_THRESHOLD * Time.deltaTime * 60f)
            {
                stuckTimer += Time.deltaTime;
                if (stuckTimer > STUCK_TIME)
                {
                    // Stuck! Pick new target or teleport to safety
                    stuckTimer = 0f;
                    if (IsInsideAnyPasture(transform.position))
                    {
                        // Emergency teleport out
                        transform.position = new Vector3(
                            Random.Range(-4f, 4f),
                            Random.Range(-3f, 3f), 0);
                    }
                    PickNewTarget();
                    return;
                }
            }
            else
            {
                stuckTimer = 0f;
            }
            lastPosition = transform.position;

            Vector3 desiredDir = (targetPos - transform.position).normalized;
            Vector3 rawDir = GetAvoidanceDirection(transform.position, desiredDir);

            // Smooth direction to prevent vibration
            smoothedDirection = Vector3.Lerp(smoothedDirection, rawDir, Time.deltaTime * 5f);
            if (smoothedDirection.magnitude < 0.1f) smoothedDirection = rawDir;

            transform.position += smoothedDirection.normalized * speed * Time.deltaTime;

            if (smoothedDirection.x != 0)
            {
                transform.localScale = new Vector3(smoothedDirection.x > 0 ? 1 : -1, 1, 1);
            }

            if (Vector3.Distance(transform.position, targetPos) < 0.4f)
            {
                waitTimer = Random.Range(1.5f, 3f);
            }

            if (Mathf.Abs(transform.position.x) > 12f || Mathf.Abs(transform.position.y) > 8f)
            {
                Destroy(gameObject);
            }
        }
    }

    public class DestroyButtonRef : MonoBehaviour
    {
        public Image ButtonImage;
    }

    public class PastureRef : MonoBehaviour
    {
        public PlacedHybrid PlacedData;
    }

    public class PastureCustomizeRef : MonoBehaviour
    {
        public TextMeshProUGUI UpgradeText;
        public Image UpgradeButtonImage;
    }
}
