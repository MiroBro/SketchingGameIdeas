using UnityEngine;

namespace MagicBooksDigseum
{
    public class DigTile : MonoBehaviour
    {
        public int durability = 3;
        public int maxDurability = 3;
        public bool hasTreasure = false;
        public MagicBookData hiddenBook;
        public Vector2Int gridPosition;

        private SpriteRenderer spriteRenderer;
        private bool isDug = false;

        public void Initialize(Vector2Int pos, int dur, bool treasure = false, MagicBookData book = null)
        {
            gridPosition = pos;
            durability = dur;
            maxDurability = dur;
            hasTreasure = treasure;
            hiddenBook = book;
            CreateVisual();
        }

        void CreateVisual()
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            UpdateVisual();
        }

        public void UpdateVisual()
        {
            if (spriteRenderer == null) return;

            Texture2D texture = new Texture2D(32, 32);
            Color[] pixels = new Color[32 * 32];

            float durPercent = (float)durability / maxDurability;
            Color dirtColor = Color.Lerp(new Color(0.4f, 0.3f, 0.2f), new Color(0.6f, 0.45f, 0.3f), 1f - durPercent);

            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    if (isDug)
                    {
                        pixels[y * 32 + x] = new Color(0.2f, 0.15f, 0.1f, 0.3f);
                    }
                    else
                    {
                        // Add some texture variation
                        float noise = (Mathf.PerlinNoise(x * 0.3f + gridPosition.x, y * 0.3f + gridPosition.y) - 0.5f) * 0.2f;
                        Color c = dirtColor + new Color(noise, noise * 0.8f, noise * 0.5f, 0);

                        // Border
                        bool isBorder = x < 1 || x > 30 || y < 1 || y > 30;
                        if (isBorder)
                        {
                            c *= 0.7f;
                        }

                        // Show cracks based on durability
                        if (durability < maxDurability)
                        {
                            int cracks = maxDurability - durability;
                            for (int i = 0; i < cracks; i++)
                            {
                                int crackX = (7 + i * 11) % 32;
                                int crackY = (5 + i * 13) % 32;
                                if (Mathf.Abs(x - crackX) < 2 && Mathf.Abs(y - crackY) < 3)
                                {
                                    c = c * 0.5f;
                                }
                            }
                        }

                        pixels[y * 32 + x] = c;
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            texture.filterMode = FilterMode.Point;

            spriteRenderer.sprite = Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);
            spriteRenderer.sortingOrder = 0;
        }

        public MagicBookData Dig(int power)
        {
            if (isDug) return null;

            durability -= power;
            UpdateVisual();

            if (durability <= 0)
            {
                isDug = true;
                UpdateVisual();

                if (hasTreasure && hiddenBook != null)
                {
                    hiddenBook.isDiscovered = true;
                    return hiddenBook;
                }
            }

            return null;
        }

        public bool IsDug()
        {
            return isDug;
        }
    }
}
