using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

namespace AdvancedCreatureDigseum
{
    public class FusionLabManager : MonoBehaviour
    {
        private Camera mainCamera;
        private List<GameObject> animalDisplays = new List<GameObject>();
        private List<GameObject> hybridDisplays = new List<GameObject>();

        private AnimalData selectedParent1;
        private AnimalData selectedParent2;
        private GameObject parent1Preview;
        private GameObject parent2Preview;
        private GameObject resultPreview;

        // UI
        private TextMeshProUGUI goldText;
        private TextMeshProUGUI selectionText;
        private TextMeshProUGUI hybridListText;
        private TextMeshProUGUI feedbackText;
        private float feedbackTimer;

        private int currentPage = 0;
        private int animalsPerPage = 12;
        private int hybridPage = 0;

        // Autosave
        private float saveTimer = 0f;
        private float saveInterval = 10f;

        void Start()
        {
            GameData.EnsureLoaded();

            mainCamera = Camera.main;
            mainCamera.backgroundColor = new Color(0.2f, 0.15f, 0.25f);

            CreateUI();
            CreateLabVisuals();
            DisplayAnimals();
            DisplayHybrids();
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

            selectionText = CreateText(canvasObj.transform, new Vector2(10, -50), "Select 2 animals to fuse");
            selectionText.fontSize = 18;
            RectTransform selRect = selectionText.GetComponent<RectTransform>();
            selRect.sizeDelta = new Vector2(400, 100);

            hybridListText = CreateText(canvasObj.transform, new Vector2(10, -200), "");
            hybridListText.fontSize = 14;
            RectTransform hybRect = hybridListText.GetComponent<RectTransform>();
            hybRect.sizeDelta = new Vector2(350, 300);

            // Title and instructions
            TextMeshProUGUI title = CreateText(canvasObj.transform, new Vector2(-10, -10), "FUSION LAB");
            RectTransform titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(1, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(1, 1);
            titleRect.sizeDelta = new Vector2(200, 35);
            title.fontSize = 24;
            title.alignment = TextAlignmentOptions.Right;

            // Navigation buttons
            float navY = -50;
            CreateNavButton(canvasObj.transform, new Vector2(-10, navY), "Exploration", "ACD_Exploration");
            CreateNavButton(canvasObj.transform, new Vector2(-10, navY - 40), "Zoo", "ACD_Zoo");
            CreateNavButton(canvasObj.transform, new Vector2(-10, navY - 80), "Skills", "ACD_Skills");
            CreateNavButton(canvasObj.transform, new Vector2(-10, navY - 120), "Menu", "SampleScene");

            // Instructions
            TextMeshProUGUI instructions = CreateText(canvasObj.transform, new Vector2(-10, navY - 170),
                "Click animals to select\n" +
                "SPACE: Fuse pair\n" +
                "(Uses 1 of each animal)\n" +
                "C: Clear selection\n\n" +
                "A/D: Animal pages\n" +
                "W/S: Hybrid pages");
            RectTransform instRect = instructions.GetComponent<RectTransform>();
            instRect.anchorMin = new Vector2(1, 1);
            instRect.anchorMax = new Vector2(1, 1);
            instRect.pivot = new Vector2(1, 1);
            instRect.sizeDelta = new Vector2(200, 200);
            instructions.fontSize = 12;

            // Feedback
            feedbackText = CreateText(canvasObj.transform, Vector2.zero, "");
            RectTransform fbRect = feedbackText.GetComponent<RectTransform>();
            fbRect.anchorMin = new Vector2(0.5f, 0.5f);
            fbRect.anchorMax = new Vector2(0.5f, 0.5f);
            fbRect.pivot = new Vector2(0.5f, 0.5f);
            fbRect.sizeDelta = new Vector2(600, 60);
            feedbackText.alignment = TextAlignmentOptions.Center;
            feedbackText.fontSize = 28;
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

        void CreateLabVisuals()
        {
            // Fusion chamber in center
            GameObject chamber = new GameObject("FusionChamber");
            SpriteRenderer chamberSr = chamber.AddComponent<SpriteRenderer>();

            Texture2D chamberTex = new Texture2D(128, 128);
            Color[] pixels = new Color[128 * 128];
            Color chamberColor = new Color(0.3f, 0.2f, 0.4f);

            for (int y = 0; y < 128; y++)
            {
                for (int x = 0; x < 128; x++)
                {
                    float dx = (x - 64) / 64f;
                    float dy = (y - 64) / 64f;
                    float dist = dx * dx + dy * dy;

                    if (dist < 1f)
                    {
                        float glow = 1f - dist;
                        pixels[y * 128 + x] = Color.Lerp(chamberColor, new Color(0.5f, 0.3f, 0.8f), glow * 0.5f);
                    }
                    else
                    {
                        pixels[y * 128 + x] = Color.clear;
                    }
                }
            }

            chamberTex.SetPixels(pixels);
            chamberTex.Apply();
            chamberTex.filterMode = FilterMode.Point;
            chamberSr.sprite = Sprite.Create(chamberTex, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f), 64);
            chamberSr.sortingOrder = -1;
            chamber.transform.position = new Vector3(0, 1, 0);

            // Parent slots
            CreateSlot(new Vector3(-2, 1, 0), "Parent 1");
            CreateSlot(new Vector3(2, 1, 0), "Parent 2");
        }

        void CreateSlot(Vector3 position, string label)
        {
            GameObject slot = new GameObject($"Slot_{label}");
            slot.transform.position = position;

            SpriteRenderer sr = slot.AddComponent<SpriteRenderer>();
            Texture2D tex = new Texture2D(48, 48);
            Color[] pixels = new Color[48 * 48];

            for (int i = 0; i < pixels.Length; i++)
            {
                int x = i % 48;
                int y = i / 48;
                bool isBorder = x < 2 || x >= 46 || y < 2 || y >= 46;
                pixels[i] = isBorder ? new Color(0.5f, 0.4f, 0.6f) : new Color(0.2f, 0.15f, 0.25f, 0.5f);
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 48, 48), new Vector2(0.5f, 0.5f), 48);
            sr.sortingOrder = 0;
        }

        void DisplayAnimals()
        {
            // Clear old displays
            foreach (var obj in animalDisplays)
            {
                if (obj != null) Destroy(obj);
            }
            animalDisplays.Clear();

            // Get list of found animals with count > 0
            List<AnimalData> foundList = new List<AnimalData>();
            foreach (var biome in BiomeDatabase.Biomes)
            {
                foreach (var animal in biome.Animals)
                {
                    if (GameData.CanUseAnimal(animal.Id))
                    {
                        foundList.Add(animal);
                    }
                }
            }

            // Paginate
            int startIndex = currentPage * animalsPerPage;
            int endIndex = Mathf.Min(startIndex + animalsPerPage, foundList.Count);

            float startX = -4f;
            float startY = -1.5f;
            int col = 0;
            int row = 0;

            for (int i = startIndex; i < endIndex; i++)
            {
                AnimalData animal = foundList[i];

                float x = startX + col * 1.2f;
                float y = startY - row * 1.4f;

                GameObject animalObj = new GameObject($"Animal_{animal.Id}");
                animalObj.transform.position = new Vector3(x, y, 0);
                animalObj.transform.localScale = Vector3.one * 0.6f;

                CreatureRenderer renderer = animalObj.AddComponent<CreatureRenderer>();
                renderer.RenderAnimal(animal);

                BoxCollider2D col2d = animalObj.AddComponent<BoxCollider2D>();
                col2d.size = new Vector2(1.5f, 1.5f);

                // Store animal reference
                AnimalClickHandler handler = animalObj.AddComponent<AnimalClickHandler>();
                handler.Animal = animal;
                handler.Manager = this;

                // Show count
                GameObject countObj = new GameObject("Count");
                countObj.transform.parent = animalObj.transform;
                countObj.transform.localPosition = new Vector3(0.5f, 0.8f, 0);
                TextMeshPro countText = countObj.AddComponent<TextMeshPro>();
                countText.text = $"x{GameData.GetAnimalFoundCount(animal.Id)}";
                countText.fontSize = 2;
                countText.alignment = TextAlignmentOptions.Center;
                countText.sortingOrder = 10;

                animalDisplays.Add(animalObj);

                col++;
                if (col >= 4)
                {
                    col = 0;
                    row++;
                }
            }
        }

        void DisplayHybrids()
        {
            foreach (var obj in hybridDisplays)
            {
                if (obj != null) Destroy(obj);
            }
            hybridDisplays.Clear();

            int startIndex = hybridPage * 6;
            int endIndex = Mathf.Min(startIndex + 6, GameData.Hybrids.Count);

            float startX = 3f;
            float startY = -1.5f;

            for (int i = startIndex; i < endIndex; i++)
            {
                HybridData hybrid = GameData.Hybrids[i];

                float x = startX + (i - startIndex) % 2 * 1.2f;
                float y = startY - (i - startIndex) / 2 * 1.4f;

                GameObject hybridObj = new GameObject($"Hybrid_{hybrid.Id}");
                hybridObj.transform.position = new Vector3(x, y, 0);
                hybridObj.transform.localScale = Vector3.one * 0.5f;

                CreatureRenderer renderer = hybridObj.AddComponent<CreatureRenderer>();
                renderer.RenderHybrid(hybrid);

                hybridDisplays.Add(hybridObj);
            }

            // Update hybrid list text
            if (hybridListText != null)
            {
                string list = $"Hybrids ({GameData.Hybrids.Count}):\n";
                for (int i = startIndex; i < endIndex; i++)
                {
                    var h = GameData.Hybrids[i];
                    list += $"- {h.Name} (${h.GetTotalValue()})\n";
                }
                hybridListText.text = list;
            }
        }

        public void SelectAnimal(AnimalData animal)
        {
            // Check if animal still has available count
            if (!GameData.CanUseAnimal(animal.Id))
            {
                ShowFeedback($"No more {animal.Name} available!", Color.red);
                return;
            }

            if (selectedParent1 == null)
            {
                selectedParent1 = animal;
                UpdateParentPreviews();
                ShowFeedback($"Selected {animal.Name} as Parent 1", Color.cyan);
            }
            else if (selectedParent2 == null && animal.Id != selectedParent1.Id)
            {
                selectedParent2 = animal;
                UpdateParentPreviews();
                ShowFeedback($"Selected {animal.Name} as Parent 2. Press SPACE!", Color.cyan);
            }
            else if (animal.Id == selectedParent1.Id && selectedParent2 == null)
            {
                // Check if we have at least 2 of this animal to fuse with itself
                if (GameData.GetAnimalFoundCount(animal.Id) >= 2)
                {
                    selectedParent2 = animal;
                    UpdateParentPreviews();
                    ShowFeedback($"Selected same animal. Press SPACE to fuse!", Color.cyan);
                }
                else
                {
                    ShowFeedback($"Need 2 of {animal.Name} to fuse with itself!", Color.red);
                }
            }
            else if (animal.Id == selectedParent1?.Id || animal.Id == selectedParent2?.Id)
            {
                ClearSelection();
            }
        }

        void UpdateParentPreviews()
        {
            if (parent1Preview != null) Destroy(parent1Preview);
            if (parent2Preview != null) Destroy(parent2Preview);

            if (selectedParent1 != null)
            {
                parent1Preview = new GameObject("Parent1Preview");
                parent1Preview.transform.position = new Vector3(-2, 1, 0);
                parent1Preview.transform.localScale = Vector3.one * 0.8f;
                CreatureRenderer r1 = parent1Preview.AddComponent<CreatureRenderer>();
                r1.RenderAnimal(selectedParent1);
            }

            if (selectedParent2 != null)
            {
                parent2Preview = new GameObject("Parent2Preview");
                parent2Preview.transform.position = new Vector3(2, 1, 0);
                parent2Preview.transform.localScale = Vector3.one * 0.8f;
                CreatureRenderer r2 = parent2Preview.AddComponent<CreatureRenderer>();
                r2.RenderAnimal(selectedParent2);
            }
        }

        void ClearSelection()
        {
            selectedParent1 = null;
            selectedParent2 = null;
            if (parent1Preview != null) Destroy(parent1Preview);
            if (parent2Preview != null) Destroy(parent2Preview);
            if (resultPreview != null) Destroy(resultPreview);
            ShowFeedback("Selection cleared", Color.yellow);
        }

        void TryFuse()
        {
            if (selectedParent1 == null || selectedParent2 == null)
            {
                ShowFeedback("Select 2 animals first!", Color.red);
                return;
            }

            // Check if we still have the animals
            if (!GameData.CanUseAnimal(selectedParent1.Id))
            {
                ShowFeedback($"No more {selectedParent1.Name} available!", Color.red);
                ClearSelection();
                DisplayAnimals();
                return;
            }

            if (!GameData.CanUseAnimal(selectedParent2.Id))
            {
                ShowFeedback($"No more {selectedParent2.Name} available!", Color.red);
                ClearSelection();
                DisplayAnimals();
                return;
            }

            // If same animal, need 2
            if (selectedParent1.Id == selectedParent2.Id && GameData.GetAnimalFoundCount(selectedParent1.Id) < 2)
            {
                ShowFeedback($"Need 2 of {selectedParent1.Name}!", Color.red);
                return;
            }

            // Consume the animals
            GameData.UseAnimal(selectedParent1.Id);
            GameData.UseAnimal(selectedParent2.Id);

            // Create hybrid
            HybridData hybrid = new HybridData(selectedParent1, selectedParent2);
            GameData.Hybrids.Add(hybrid);

            // Show result
            if (resultPreview != null) Destroy(resultPreview);
            resultPreview = new GameObject("ResultPreview");
            resultPreview.transform.position = new Vector3(0, 1, 0);
            resultPreview.transform.localScale = Vector3.one;
            CreatureRenderer renderer = resultPreview.AddComponent<CreatureRenderer>();
            renderer.RenderHybrid(hybrid);

            ShowFeedback($"Created {hybrid.Name}! Value: {hybrid.GetTotalValue()}g", Color.green);

            // Clear selection
            selectedParent1 = null;
            selectedParent2 = null;
            if (parent1Preview != null) Destroy(parent1Preview);
            if (parent2Preview != null) Destroy(parent2Preview);

            // Refresh displays and save
            DisplayAnimals();
            DisplayHybrids();
            GameData.SaveGame();
        }

        void Update()
        {
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
                if (keyboard.digit1Key.wasPressedThisFrame)
                    SceneManager.LoadScene("ACD_Exploration");
                if (keyboard.zKey.wasPressedThisFrame)
                    SceneManager.LoadScene("ACD_Zoo");
                if (keyboard.xKey.wasPressedThisFrame)
                    SceneManager.LoadScene("ACD_Skills");
                if (keyboard.escapeKey.wasPressedThisFrame)
                    SceneManager.LoadScene("SampleScene");

                if (keyboard.spaceKey.wasPressedThisFrame)
                    TryFuse();
                if (keyboard.cKey.wasPressedThisFrame)
                    ClearSelection();

                // Pagination
                if (keyboard.aKey.wasPressedThisFrame)
                {
                    currentPage = Mathf.Max(0, currentPage - 1);
                    DisplayAnimals();
                }
                if (keyboard.dKey.wasPressedThisFrame)
                {
                    int totalAnimals = 0;
                    foreach (var biome in BiomeDatabase.Biomes)
                        foreach (var animal in biome.Animals)
                            if (GameData.CanUseAnimal(animal.Id))
                                totalAnimals++;
                    int maxPage = (totalAnimals - 1) / animalsPerPage;
                    currentPage = Mathf.Min(maxPage, currentPage + 1);
                    DisplayAnimals();
                }
                if (keyboard.wKey.wasPressedThisFrame)
                {
                    hybridPage = Mathf.Max(0, hybridPage - 1);
                    DisplayHybrids();
                }
                if (keyboard.sKey.wasPressedThisFrame)
                {
                    int maxPage = (GameData.Hybrids.Count - 1) / 6;
                    hybridPage = Mathf.Min(Mathf.Max(0, maxPage), hybridPage + 1);
                    DisplayHybrids();
                }
            }

            // Click detection
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                Vector3 worldPos = mainCamera.ScreenToWorldPoint(mouse.position.ReadValue());
                RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);

                if (hit.collider != null)
                {
                    AnimalClickHandler handler = hit.collider.GetComponent<AnimalClickHandler>();
                    if (handler != null)
                    {
                        SelectAnimal(handler.Animal);
                    }
                }
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
                    feedbackText.text = "";
            }
        }

        void UpdateUI()
        {
            if (goldText != null)
                goldText.text = $"Gold: {GameData.Gold}";

            if (selectionText != null)
            {
                string p1 = selectedParent1 != null ? $"{selectedParent1.Name} (x{GameData.GetAnimalFoundCount(selectedParent1.Id)})" : "None";
                string p2 = selectedParent2 != null ? $"{selectedParent2.Name} (x{GameData.GetAnimalFoundCount(selectedParent2.Id)})" : "None";
                selectionText.text = $"Parent 1: {p1}\nParent 2: {p2}";
            }
        }
    }

    public class AnimalClickHandler : MonoBehaviour
    {
        public AnimalData Animal;
        public FusionLabManager Manager;
    }
}
