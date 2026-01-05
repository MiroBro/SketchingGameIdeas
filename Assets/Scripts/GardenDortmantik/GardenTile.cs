using UnityEngine;

namespace GardenDortmantik
{
    public enum GardenTileType
    {
        Flowers,
        Grass,
        Trees,
        Water,
        Path,
        Vegetables
    }

    public class GardenTile : MonoBehaviour
    {
        public GardenTileType tileType;
        public Vector2Int gridPosition;
        public SpriteRenderer spriteRenderer;

        private static readonly Color[] TileColors = new Color[]
        {
            new Color(1f, 0.5f, 0.7f),    // Flowers - Pink
            new Color(0.4f, 0.8f, 0.3f),  // Grass - Green
            new Color(0.2f, 0.5f, 0.2f),  // Trees - Dark Green
            new Color(0.3f, 0.6f, 0.9f),  // Water - Blue
            new Color(0.7f, 0.6f, 0.4f),  // Path - Brown
            new Color(0.9f, 0.6f, 0.2f)   // Vegetables - Orange
        };

        public void Initialize(GardenTileType type, Vector2Int position)
        {
            tileType = type;
            gridPosition = position;
            UpdateVisual();
        }

        public void UpdateVisual()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
                if (spriteRenderer == null)
                    spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            spriteRenderer.sprite = CreateSquareSprite();
            spriteRenderer.color = TileColors[(int)tileType];
        }

        private Sprite CreateSquareSprite()
        {
            Texture2D texture = new Texture2D(64, 64);
            Color[] pixels = new Color[64 * 64];

            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    bool isBorder = x < 2 || x > 61 || y < 2 || y > 61;
                    pixels[y * 64 + x] = isBorder ? Color.black : Color.white;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64);
        }

        public int CalculateMatchBonus(GardenTile[] neighbors)
        {
            int bonus = 0;
            foreach (var neighbor in neighbors)
            {
                if (neighbor == null) continue;

                if (neighbor.tileType == tileType)
                {
                    bonus += 10; // Same type bonus
                }
                else if (AreTypesCompatible(tileType, neighbor.tileType))
                {
                    bonus += 5; // Compatible types bonus
                }
            }
            return bonus;
        }

        private bool AreTypesCompatible(GardenTileType a, GardenTileType b)
        {
            // Define compatible pairs
            return (a == GardenTileType.Flowers && b == GardenTileType.Grass) ||
                   (a == GardenTileType.Grass && b == GardenTileType.Flowers) ||
                   (a == GardenTileType.Trees && b == GardenTileType.Grass) ||
                   (a == GardenTileType.Grass && b == GardenTileType.Trees) ||
                   (a == GardenTileType.Water && b == GardenTileType.Grass) ||
                   (a == GardenTileType.Grass && b == GardenTileType.Water) ||
                   (a == GardenTileType.Vegetables && b == GardenTileType.Water) ||
                   (a == GardenTileType.Water && b == GardenTileType.Vegetables) ||
                   (a == GardenTileType.Path && b == GardenTileType.Flowers) ||
                   (a == GardenTileType.Flowers && b == GardenTileType.Path);
        }

        public string GetTileSymbol()
        {
            switch (tileType)
            {
                case GardenTileType.Flowers: return "F";
                case GardenTileType.Grass: return "G";
                case GardenTileType.Trees: return "T";
                case GardenTileType.Water: return "W";
                case GardenTileType.Path: return "P";
                case GardenTileType.Vegetables: return "V";
                default: return "?";
            }
        }
    }
}
