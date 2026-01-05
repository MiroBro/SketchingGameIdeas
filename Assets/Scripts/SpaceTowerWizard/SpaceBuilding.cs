using UnityEngine;

namespace SpaceTowerWizard
{
    [System.Serializable]
    public class BuildingData
    {
        public string name;
        public string description;
        public int baseCost;
        public float baseProduction;
        public ResourceType producesResource;
        public ResourceType costResource;
        public int unlockRequirement;
        public Color buildingColor;
        public bool isUnlocked;
        public int level;
        public bool isFinalBuilding;
    }

    public enum ResourceType
    {
        Energy,
        Minerals,
        Credits,
        TechPoints
    }

    public class SpaceBuilding : MonoBehaviour
    {
        public BuildingData data;
        public int buildingIndex;
        public SpriteRenderer spriteRenderer;
        public SpriteRenderer glowRenderer;

        private float productionTimer;
        private bool isProducing;

        public void Initialize(BuildingData buildingData, int index)
        {
            data = buildingData;
            buildingIndex = index;
            UpdateVisual();
        }

        void Update()
        {
            if (data.level > 0 && isProducing)
            {
                productionTimer += Time.deltaTime;
            }
        }

        public void UpdateVisual()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
                if (spriteRenderer == null)
                    spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            spriteRenderer.sprite = CreateBuildingSprite();

            if (data.isUnlocked && data.level > 0)
            {
                spriteRenderer.color = data.buildingColor;
            }
            else if (data.isUnlocked)
            {
                spriteRenderer.color = new Color(data.buildingColor.r * 0.3f, data.buildingColor.g * 0.3f, data.buildingColor.b * 0.3f, 1f);
            }
            else
            {
                spriteRenderer.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            }
        }

        private Sprite CreateBuildingSprite()
        {
            int size = 64;
            Texture2D texture = new Texture2D(size, size);
            Color[] pixels = new Color[size * size];

            // Create a building shape based on index
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isFilled = false;

                    // Base rectangle
                    int margin = 8;
                    int height = Mathf.Min(size - margin, 20 + buildingIndex * 6);
                    int startY = margin;

                    if (x >= margin && x < size - margin && y >= startY && y < startY + height)
                    {
                        isFilled = true;
                    }

                    // Add antenna/spire for higher buildings
                    if (buildingIndex >= 3)
                    {
                        int spireWidth = 4;
                        int spireHeight = 10 + buildingIndex * 2;
                        int spireX = size / 2 - spireWidth / 2;
                        int spireY = startY + height;

                        if (x >= spireX && x < spireX + spireWidth && y >= spireY && y < spireY + spireHeight)
                        {
                            isFilled = true;
                        }
                    }

                    bool isBorder = false;
                    if (isFilled)
                    {
                        // Check if this is a border pixel
                        if (x == margin || x == size - margin - 1 || y == startY || y == startY + height - 1)
                        {
                            isBorder = true;
                        }
                    }

                    pixels[y * size + x] = isFilled ? (isBorder ? Color.white : new Color(0.8f, 0.8f, 0.8f)) : Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            texture.filterMode = FilterMode.Point;

            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0f), 64);
        }

        public float GetProduction()
        {
            if (data.level <= 0) return 0;
            return data.baseProduction * data.level * (1 + data.level * 0.1f);
        }

        public int GetUpgradeCost()
        {
            return Mathf.RoundToInt(data.baseCost * Mathf.Pow(1.5f, data.level));
        }

        public void StartProducing()
        {
            isProducing = true;
        }

        public void StopProducing()
        {
            isProducing = false;
        }
    }
}
