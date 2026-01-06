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

        // Fusion candidates can be animal OR hybrid
        private AnimalData selectedAnimal1;
        private AnimalData selectedAnimal2;
        private HybridData selectedHybrid1;
        private HybridData selectedHybrid2;
        private int selectedSlot = 0; // 0 = none, 1 = slot1 filled, 2 = both filled

        private GameObject parent1Preview;
        private GameObject parent2Preview;
        private GameObject resultPreview;

        // UI
        private TextMeshProUGUI goldText;
        private TextMeshProUGUI selectionText;
        private TextMeshProUGUI hybridListText;
        private TextMeshProUGUI feedbackText;
        private float feedbackTimer;
        private Transform canvasTransform;
        private TextMeshProUGUI animalPageText;
        private TextMeshProUGUI hybridPageText;

        private int currentPage = 0;
        private int animalsPerPage = 12;
        private int hybridPage = 0;
        private int hybridsPerPage = 6;

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
            canvasTransform = canvasObj.transform;
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
                "<color=#ff0>★★★ = Fuseable!</color>\n" +
                "Find 6 of each animal\n" +
                "to unlock fusion.\n\n" +
                "SPACE: Fuse pair\n" +
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

            // Create scroll UI for animals (bottom left)
            CreateScrollControls(canvasObj.transform, new Vector2(120, 60), "Animals", true);

            // Create scroll UI for hybrids (bottom right)
            CreateScrollControls(canvasObj.transform, new Vector2(-120, 60), "Hybrids", false);
        }

        void CreateScrollControls(Transform parent, Vector2 position, string label, bool isAnimalScroll)
        {
            // Container
            GameObject container = new GameObject($"Scroll_{label}");
            container.transform.SetParent(parent);
            RectTransform containerRect = container.AddComponent<RectTransform>();

            if (isAnimalScroll)
            {
                containerRect.anchorMin = new Vector2(0, 0);
                containerRect.anchorMax = new Vector2(0, 0);
                containerRect.pivot = new Vector2(0, 0);
            }
            else
            {
                containerRect.anchorMin = new Vector2(1, 0);
                containerRect.anchorMax = new Vector2(1, 0);
                containerRect.pivot = new Vector2(1, 0);
            }
            containerRect.anchoredPosition = position;
            containerRect.sizeDelta = new Vector2(200, 40);
            containerRect.localScale = Vector3.one;

            // Left arrow button
            CreateScrollButton(container.transform, new Vector2(0, 0), "<", () => {
                if (isAnimalScroll) PrevAnimalPage();
                else PrevHybridPage();
            });

            // Page text
            TextMeshProUGUI pageText = CreateText(container.transform, new Vector2(40, 0), "1/1");
            pageText.fontSize = 16;
            pageText.alignment = TextAlignmentOptions.Center;
            RectTransform pageRect = pageText.GetComponent<RectTransform>();
            pageRect.anchorMin = new Vector2(0, 0);
            pageRect.anchorMax = new Vector2(0, 0);
            pageRect.pivot = new Vector2(0, 0);
            pageRect.sizeDelta = new Vector2(80, 30);

            if (isAnimalScroll)
                animalPageText = pageText;
            else
                hybridPageText = pageText;

            // Right arrow button
            CreateScrollButton(container.transform, new Vector2(120, 0), ">", () => {
                if (isAnimalScroll) NextAnimalPage();
                else NextHybridPage();
            });

            // Label
            TextMeshProUGUI labelText = CreateText(container.transform, new Vector2(40, 25), label);
            labelText.fontSize = 12;
            labelText.alignment = TextAlignmentOptions.Center;
            RectTransform labelRect = labelText.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0);
            labelRect.anchorMax = new Vector2(0, 0);
            labelRect.pivot = new Vector2(0, 0);
            labelRect.sizeDelta = new Vector2(80, 20);
        }

        void CreateScrollButton(Transform parent, Vector2 position, string text, System.Action onClick)
        {
            GameObject btnObj = new GameObject($"ScrollBtn_{text}");
            btnObj.transform.SetParent(parent);

            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(0, 0);
            rect.pivot = new Vector2(0, 0);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(35, 30);
            rect.localScale = Vector3.one;

            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.4f, 0.35f, 0.5f);

            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick());

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            textRect.localScale = Vector3.one;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 18;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
        }

        void PrevAnimalPage()
        {
            currentPage = Mathf.Max(0, currentPage - 1);
            DisplayAnimals();
            UpdatePageIndicators();
        }

        void NextAnimalPage()
        {
            int totalAnimals = GetTotalAvailableAnimals();
            int maxPage = Mathf.Max(0, (totalAnimals - 1) / animalsPerPage);
            currentPage = Mathf.Min(maxPage, currentPage + 1);
            DisplayAnimals();
            UpdatePageIndicators();
        }

        void PrevHybridPage()
        {
            hybridPage = Mathf.Max(0, hybridPage - 1);
            DisplayHybrids();
            UpdatePageIndicators();
        }

        void NextHybridPage()
        {
            var availableHybrids = GameData.GetAvailableHybridsForFusion();
            int maxPage = Mathf.Max(0, (availableHybrids.Count - 1) / hybridsPerPage);
            hybridPage = Mathf.Min(maxPage, hybridPage + 1);
            DisplayHybrids();
            UpdatePageIndicators();
        }

        int GetTotalAvailableAnimals()
        {
            int total = 0;
            foreach (var biome in BiomeDatabase.Biomes)
                foreach (var animal in biome.Animals)
                    if (GameData.CanUseAnimal(animal.Id))
                        total++;
            return total;
        }

        void UpdatePageIndicators()
        {
            if (animalPageText != null)
            {
                int totalAnimals = GetTotalAvailableAnimals();
                int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)totalAnimals / animalsPerPage));
                animalPageText.text = $"{currentPage + 1}/{totalPages}";
            }

            if (hybridPageText != null)
            {
                var availableHybrids = GameData.GetAvailableHybridsForFusion();
                int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)availableHybrids.Count / hybridsPerPage));
                hybridPageText.text = $"{hybridPage + 1}/{totalPages}";
            }
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

                // Check star level for fusion eligibility
                int starLevel = GameData.GetAnimalStarLevel(animal.Id);
                bool canFuse = starLevel >= 3;

                // Gray out non-fuseable animals
                if (!canFuse)
                {
                    renderer.SetTint(new Color(0.5f, 0.5f, 0.5f, 0.7f));
                }

                // Show star badge (top-left)
                GameObject starObj = new GameObject("Stars");
                starObj.transform.parent = animalObj.transform;
                starObj.transform.localPosition = new Vector3(-0.5f, 0.8f, 0);
                TextMeshPro starText = starObj.AddComponent<TextMeshPro>();
                starText.text = GameData.GetStarString(starLevel);
                starText.fontSize = 1.8f;
                starText.alignment = TextAlignmentOptions.Center;
                starText.sortingOrder = 10;
                starText.color = canFuse ? new Color(1f, 0.9f, 0.3f) : new Color(0.6f, 0.6f, 0.6f);

                // Show count badge (top-right)
                GameObject countObj = new GameObject("Count");
                countObj.transform.parent = animalObj.transform;
                countObj.transform.localPosition = new Vector3(0.6f, 0.7f, 0);
                TextMeshPro countText = countObj.AddComponent<TextMeshPro>();
                countText.text = $"x{GameData.GetAnimalFoundCount(animal.Id)}";
                countText.fontSize = 2;
                countText.alignment = TextAlignmentOptions.Center;
                countText.sortingOrder = 10;
                countText.color = new Color(1f, 1f, 0.5f);

                // Show name label below (with fusion status)
                GameObject nameObj = new GameObject("Name");
                nameObj.transform.parent = animalObj.transform;
                nameObj.transform.localPosition = new Vector3(0, -1f, 0);
                TextMeshPro nameText = nameObj.AddComponent<TextMeshPro>();
                nameText.text = canFuse ? animal.Name : $"<color=#888>{animal.Name}</color>";
                nameText.fontSize = 1.5f;
                nameText.alignment = TextAlignmentOptions.Center;
                nameText.sortingOrder = 10;
                nameText.color = Color.white;

                animalDisplays.Add(animalObj);

                col++;
                if (col >= 4)
                {
                    col = 0;
                    row++;
                }
            }

            UpdatePageIndicators();
        }

        void DisplayHybrids()
        {
            foreach (var obj in hybridDisplays)
            {
                if (obj != null) Destroy(obj);
            }
            hybridDisplays.Clear();

            // Only show hybrids available for fusion (not placed)
            var availableHybrids = GameData.GetAvailableHybridsForFusion();

            int startIndex = hybridPage * 6;
            int endIndex = Mathf.Min(startIndex + 6, availableHybrids.Count);

            float startX = 3f;
            float startY = -1.5f;

            for (int i = startIndex; i < endIndex; i++)
            {
                HybridData hybrid = availableHybrids[i];

                float x = startX + (i - startIndex) % 2 * 1.2f;
                float y = startY - (i - startIndex) / 2 * 1.4f;

                GameObject hybridObj = new GameObject($"Hybrid_{hybrid.Id}");
                hybridObj.transform.position = new Vector3(x, y, 0);
                hybridObj.transform.localScale = Vector3.one * 0.5f;

                CreatureRenderer renderer = hybridObj.AddComponent<CreatureRenderer>();
                renderer.RenderHybrid(hybrid);

                // Add click handler for fusion
                BoxCollider2D col2d = hybridObj.AddComponent<BoxCollider2D>();
                col2d.size = new Vector2(1.5f, 1.5f);

                HybridClickHandler handler = hybridObj.AddComponent<HybridClickHandler>();
                handler.Hybrid = hybrid;
                handler.Manager = this;

                // Show generation badge
                GameObject genObj = new GameObject("Gen");
                genObj.transform.parent = hybridObj.transform;
                genObj.transform.localPosition = new Vector3(0.6f, 0.7f, 0);
                TextMeshPro genText = genObj.AddComponent<TextMeshPro>();
                genText.text = $"G{hybrid.Generation}";
                genText.fontSize = 2;
                genText.alignment = TextAlignmentOptions.Center;
                genText.sortingOrder = 10;
                genText.color = new Color(0.5f, 1f, 0.5f); // Green for generation

                // Show name label below
                GameObject nameObj = new GameObject("Name");
                nameObj.transform.parent = hybridObj.transform;
                nameObj.transform.localPosition = new Vector3(0, -1.2f, 0);
                TextMeshPro nameText = nameObj.AddComponent<TextMeshPro>();
                // Truncate long names
                string displayName = hybrid.Name.Length > 10 ? hybrid.Name.Substring(0, 8) + ".." : hybrid.Name;
                nameText.text = displayName;
                nameText.fontSize = 1.3f;
                nameText.alignment = TextAlignmentOptions.Center;
                nameText.sortingOrder = 10;
                nameText.color = Color.white;

                // Show value below name
                GameObject valueObj = new GameObject("Value");
                valueObj.transform.parent = hybridObj.transform;
                valueObj.transform.localPosition = new Vector3(0, -1.6f, 0);
                TextMeshPro valueText = valueObj.AddComponent<TextMeshPro>();
                valueText.text = $"${hybrid.GetTotalValue()}";
                valueText.fontSize = 1.2f;
                valueText.alignment = TextAlignmentOptions.Center;
                valueText.sortingOrder = 10;
                valueText.color = new Color(1f, 0.9f, 0.3f); // Gold color

                hybridDisplays.Add(hybridObj);
            }

            // Update hybrid list text
            if (hybridListText != null)
            {
                int placedCount = GameData.Hybrids.Count - availableHybrids.Count;
                string list = $"Hybrids: {availableHybrids.Count} available\n";
                list += $"({placedCount} placed in zoo)\n\n";
                list += "Click hybrids to fuse them!\n";
                list += "Higher generations = more value!";
                hybridListText.text = list;
            }

            UpdatePageIndicators();
        }

        public void SelectAnimal(AnimalData animal)
        {
            // Check if animal has 3 stars (required for fusion)
            if (!GameData.CanAnimalFuse(animal.Id))
            {
                int starLevel = GameData.GetAnimalStarLevel(animal.Id);
                int needed = GameData.GetFindsForNextStar(animal.Id);
                if (starLevel < 3)
                {
                    ShowFeedback($"{animal.Name} needs ★★★ to fuse! (Find {6 - GameData.GetHistoricalFindCount(animal.Id)} more)", Color.red);
                }
                return;
            }

            if (!GameData.CanUseAnimal(animal.Id))
            {
                ShowFeedback($"No more {animal.Name} available!", Color.red);
                return;
            }

            if (selectedSlot == 0)
            {
                // First selection
                selectedAnimal1 = animal;
                selectedHybrid1 = null;
                selectedSlot = 1;
                UpdateParentPreviews();
                ShowFeedback($"Selected {animal.Name} as Parent 1", Color.cyan);
            }
            else if (selectedSlot == 1)
            {
                // Second selection - check for same animal case
                bool isSameAnimal = selectedAnimal1 != null && animal.Id == selectedAnimal1.Id;
                if (isSameAnimal && GameData.GetAnimalFoundCount(animal.Id) < 2)
                {
                    ShowFeedback($"Need 2 of {animal.Name} to fuse with itself!", Color.red);
                    return;
                }

                selectedAnimal2 = animal;
                selectedHybrid2 = null;
                selectedSlot = 2;
                UpdateParentPreviews();
                ShowFeedback($"Selected {animal.Name} as Parent 2. Press SPACE!", Color.cyan);
            }
            else
            {
                // Already have 2 selected - deselect
                ClearSelection();
            }
        }

        public void SelectHybrid(HybridData hybrid)
        {
            if (!GameData.IsHybridAvailableForFusion(hybrid.Id))
            {
                ShowFeedback($"{hybrid.Name} is placed in zoo!", Color.red);
                return;
            }

            if (selectedSlot == 0)
            {
                selectedHybrid1 = hybrid;
                selectedAnimal1 = null;
                selectedSlot = 1;
                UpdateParentPreviews();
                ShowFeedback($"Selected {hybrid.Name} (G{hybrid.Generation}) as Parent 1", Color.cyan);
            }
            else if (selectedSlot == 1)
            {
                // Check if same hybrid
                if (selectedHybrid1 != null && hybrid.Id == selectedHybrid1.Id)
                {
                    ShowFeedback("Can't fuse a hybrid with itself!", Color.red);
                    return;
                }

                selectedHybrid2 = hybrid;
                selectedAnimal2 = null;
                selectedSlot = 2;
                UpdateParentPreviews();
                ShowFeedback($"Selected {hybrid.Name} (G{hybrid.Generation}) as Parent 2. Press SPACE!", Color.cyan);
            }
            else
            {
                ClearSelection();
            }
        }

        void UpdateParentPreviews()
        {
            if (parent1Preview != null) Destroy(parent1Preview);
            if (parent2Preview != null) Destroy(parent2Preview);

            // Parent 1
            if (selectedAnimal1 != null)
            {
                parent1Preview = new GameObject("Parent1Preview");
                parent1Preview.transform.position = new Vector3(-2, 1, 0);
                parent1Preview.transform.localScale = Vector3.one * 0.8f;
                CreatureRenderer r1 = parent1Preview.AddComponent<CreatureRenderer>();
                r1.RenderAnimal(selectedAnimal1);
            }
            else if (selectedHybrid1 != null)
            {
                parent1Preview = new GameObject("Parent1Preview");
                parent1Preview.transform.position = new Vector3(-2, 1, 0);
                parent1Preview.transform.localScale = Vector3.one * 0.8f;
                CreatureRenderer r1 = parent1Preview.AddComponent<CreatureRenderer>();
                r1.RenderHybrid(selectedHybrid1);
            }

            // Parent 2
            if (selectedAnimal2 != null)
            {
                parent2Preview = new GameObject("Parent2Preview");
                parent2Preview.transform.position = new Vector3(2, 1, 0);
                parent2Preview.transform.localScale = Vector3.one * 0.8f;
                CreatureRenderer r2 = parent2Preview.AddComponent<CreatureRenderer>();
                r2.RenderAnimal(selectedAnimal2);
            }
            else if (selectedHybrid2 != null)
            {
                parent2Preview = new GameObject("Parent2Preview");
                parent2Preview.transform.position = new Vector3(2, 1, 0);
                parent2Preview.transform.localScale = Vector3.one * 0.8f;
                CreatureRenderer r2 = parent2Preview.AddComponent<CreatureRenderer>();
                r2.RenderHybrid(selectedHybrid2);
            }
        }

        void ClearSelection()
        {
            selectedAnimal1 = null;
            selectedAnimal2 = null;
            selectedHybrid1 = null;
            selectedHybrid2 = null;
            selectedSlot = 0;
            if (parent1Preview != null) Destroy(parent1Preview);
            if (parent2Preview != null) Destroy(parent2Preview);
            if (resultPreview != null) Destroy(resultPreview);
            ShowFeedback("Selection cleared", Color.yellow);
        }

        void TryFuse()
        {
            if (selectedSlot < 2)
            {
                ShowFeedback("Select 2 creatures first!", Color.red);
                return;
            }

            // Validate selections still valid
            if (selectedAnimal1 != null && !GameData.CanUseAnimal(selectedAnimal1.Id))
            {
                ShowFeedback($"No more {selectedAnimal1.Name} available!", Color.red);
                ClearSelection();
                DisplayAnimals();
                return;
            }
            if (selectedAnimal2 != null && !GameData.CanUseAnimal(selectedAnimal2.Id))
            {
                ShowFeedback($"No more {selectedAnimal2.Name} available!", Color.red);
                ClearSelection();
                DisplayAnimals();
                return;
            }
            if (selectedHybrid1 != null && !GameData.IsHybridAvailableForFusion(selectedHybrid1.Id))
            {
                ShowFeedback($"{selectedHybrid1.Name} no longer available!", Color.red);
                ClearSelection();
                DisplayHybrids();
                return;
            }
            if (selectedHybrid2 != null && !GameData.IsHybridAvailableForFusion(selectedHybrid2.Id))
            {
                ShowFeedback($"{selectedHybrid2.Name} no longer available!", Color.red);
                ClearSelection();
                DisplayHybrids();
                return;
            }

            // Same animal check
            if (selectedAnimal1 != null && selectedAnimal2 != null &&
                selectedAnimal1.Id == selectedAnimal2.Id &&
                GameData.GetAnimalFoundCount(selectedAnimal1.Id) < 2)
            {
                ShowFeedback($"Need 2 of {selectedAnimal1.Name}!", Color.red);
                return;
            }

            // Create the hybrid based on what's selected
            HybridData newHybrid = null;

            if (selectedAnimal1 != null && selectedAnimal2 != null)
            {
                // Animal + Animal
                GameData.UseAnimal(selectedAnimal1.Id);
                GameData.UseAnimal(selectedAnimal2.Id);
                newHybrid = new HybridData(selectedAnimal1, selectedAnimal2);
            }
            else if (selectedAnimal1 != null && selectedHybrid2 != null)
            {
                // Animal + Hybrid
                GameData.UseAnimal(selectedAnimal1.Id);
                GameData.RemoveHybrid(selectedHybrid2.Id);
                newHybrid = new HybridData(selectedAnimal1, selectedHybrid2);
            }
            else if (selectedHybrid1 != null && selectedAnimal2 != null)
            {
                // Hybrid + Animal
                GameData.RemoveHybrid(selectedHybrid1.Id);
                GameData.UseAnimal(selectedAnimal2.Id);
                newHybrid = new HybridData(selectedHybrid1, selectedAnimal2);
            }
            else if (selectedHybrid1 != null && selectedHybrid2 != null)
            {
                // Hybrid + Hybrid
                GameData.RemoveHybrid(selectedHybrid1.Id);
                GameData.RemoveHybrid(selectedHybrid2.Id);
                newHybrid = new HybridData(selectedHybrid1, selectedHybrid2);
            }

            if (newHybrid == null)
            {
                ShowFeedback("Invalid selection!", Color.red);
                return;
            }

            GameData.Hybrids.Add(newHybrid);

            // Show result
            if (resultPreview != null) Destroy(resultPreview);
            resultPreview = new GameObject("ResultPreview");
            resultPreview.transform.position = new Vector3(0, 1, 0);
            resultPreview.transform.localScale = Vector3.one;
            CreatureRenderer renderer = resultPreview.AddComponent<CreatureRenderer>();
            renderer.RenderHybrid(newHybrid);

            ShowFeedback($"Created {newHybrid.Name} (G{newHybrid.Generation})! Value: {newHybrid.GetTotalValue()}g", Color.green);

            // Clear selection
            selectedAnimal1 = null;
            selectedAnimal2 = null;
            selectedHybrid1 = null;
            selectedHybrid2 = null;
            selectedSlot = 0;
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

                // Pagination - use centralized methods
                if (keyboard.aKey.wasPressedThisFrame)
                    PrevAnimalPage();
                if (keyboard.dKey.wasPressedThisFrame)
                    NextAnimalPage();
                if (keyboard.wKey.wasPressedThisFrame)
                    PrevHybridPage();
                if (keyboard.sKey.wasPressedThisFrame)
                    NextHybridPage();
            }

            // Click detection
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                Vector3 worldPos = mainCamera.ScreenToWorldPoint(mouse.position.ReadValue());
                RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);

                if (hit.collider != null)
                {
                    AnimalClickHandler animalHandler = hit.collider.GetComponent<AnimalClickHandler>();
                    if (animalHandler != null)
                    {
                        SelectAnimal(animalHandler.Animal);
                    }

                    HybridClickHandler hybridHandler = hit.collider.GetComponent<HybridClickHandler>();
                    if (hybridHandler != null)
                    {
                        SelectHybrid(hybridHandler.Hybrid);
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
                string p1 = "None";
                string p2 = "None";

                if (selectedAnimal1 != null)
                    p1 = $"{selectedAnimal1.Name} (x{GameData.GetAnimalFoundCount(selectedAnimal1.Id)})";
                else if (selectedHybrid1 != null)
                    p1 = $"{selectedHybrid1.Name} (G{selectedHybrid1.Generation})";

                if (selectedAnimal2 != null)
                    p2 = $"{selectedAnimal2.Name} (x{GameData.GetAnimalFoundCount(selectedAnimal2.Id)})";
                else if (selectedHybrid2 != null)
                    p2 = $"{selectedHybrid2.Name} (G{selectedHybrid2.Generation})";

                selectionText.text = $"Parent 1: {p1}\nParent 2: {p2}";
            }
        }
    }

    public class AnimalClickHandler : MonoBehaviour
    {
        public AnimalData Animal;
        public FusionLabManager Manager;
    }

    public class HybridClickHandler : MonoBehaviour
    {
        public HybridData Hybrid;
        public FusionLabManager Manager;
    }
}
