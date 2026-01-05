using UnityEngine;

namespace UnderwaterKingdom
{
    public enum BuildingType
    {
        CoralWall,
        PearlFarm,
        DefenderPost
    }

    public class UnderwaterBuilding : MonoBehaviour
    {
        public BuildingType buildingType;
        public int health = 100;
        public int maxHealth = 100;
        public int level = 1;
        public float yPosition;

        private SpriteRenderer spriteRenderer;

        public void Initialize(BuildingType type, float yPos)
        {
            buildingType = type;
            yPosition = yPos;
            CreateVisual();
        }

        void CreateVisual()
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

            Texture2D texture = new Texture2D(48, 48);
            Color[] pixels = new Color[48 * 48];
            Color buildingColor = GetBuildingColor();

            for (int y = 0; y < 48; y++)
            {
                for (int x = 0; x < 48; x++)
                {
                    bool isFilled = false;

                    switch (buildingType)
                    {
                        case BuildingType.CoralWall:
                            // Wall shape
                            if (x >= 8 && x < 40 && y >= 4 && y < 44)
                            {
                                isFilled = true;
                            }
                            break;

                        case BuildingType.PearlFarm:
                            // Farm dome shape
                            float dx = (x - 24) / 18f;
                            float dy = (y - 16) / 14f;
                            if (dx * dx + dy * dy < 1f && y >= 4)
                            {
                                isFilled = true;
                            }
                            break;

                        case BuildingType.DefenderPost:
                            // Tower shape
                            if (x >= 16 && x < 32 && y >= 4 && y < 40)
                            {
                                isFilled = true;
                            }
                            // Flag
                            if (x >= 28 && x < 40 && y >= 36 && y < 44)
                            {
                                isFilled = true;
                            }
                            break;
                    }

                    pixels[y * 48 + x] = isFilled ? buildingColor : Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            texture.filterMode = FilterMode.Point;

            spriteRenderer.sprite = Sprite.Create(texture, new Rect(0, 0, 48, 48), new Vector2(0.5f, 0.5f), 48);
            spriteRenderer.sortingOrder = 1;
        }

        Color GetBuildingColor()
        {
            switch (buildingType)
            {
                case BuildingType.CoralWall:
                    return new Color(0.9f, 0.5f, 0.5f); // Coral pink
                case BuildingType.PearlFarm:
                    return new Color(0.5f, 0.9f, 0.7f); // Sea green
                case BuildingType.DefenderPost:
                    return new Color(0.6f, 0.6f, 0.9f); // Blue-ish
                default:
                    return Color.white;
            }
        }

        public void TakeDamage(int damage)
        {
            health -= damage;
            UpdateHealthVisual();

            if (health <= 0)
            {
                Destroy(gameObject);
            }
        }

        void UpdateHealthVisual()
        {
            float healthPercent = (float)health / maxHealth;
            if (spriteRenderer != null)
            {
                Color c = GetBuildingColor();
                c *= (0.5f + 0.5f * healthPercent);
                spriteRenderer.color = c;
            }
        }

        public int GetCost()
        {
            switch (buildingType)
            {
                case BuildingType.CoralWall: return 5;
                case BuildingType.PearlFarm: return 10;
                case BuildingType.DefenderPost: return 15;
                default: return 10;
            }
        }
    }
}
