using UnityEngine;

namespace MagicBooksDigseum
{
    public enum BookRarity
    {
        Common,
        Uncommon,
        Rare,
        Legendary
    }

    [System.Serializable]
    public class MagicBookData
    {
        public string name;
        public BookRarity rarity;
        public int value;
        public Color bookColor;
        public bool isDiscovered;
        public bool isDisplayed;
    }

    public class MagicBook : MonoBehaviour
    {
        public MagicBookData data;
        private SpriteRenderer spriteRenderer;

        public void Initialize(MagicBookData bookData)
        {
            data = bookData;
            CreateVisual();
        }

        void CreateVisual()
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

            Texture2D texture = new Texture2D(24, 32);
            Color[] pixels = new Color[24 * 32];

            Color bookColor = GetRarityColor();

            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 24; x++)
                {
                    bool isFilled = false;

                    // Book cover
                    if (x >= 2 && x < 22 && y >= 2 && y < 30)
                    {
                        isFilled = true;
                    }

                    // Spine
                    if (x >= 0 && x < 4 && y >= 2 && y < 30)
                    {
                        isFilled = true;
                        bookColor = data.bookColor * 0.7f;
                    }

                    // Pages (lighter stripe)
                    if (x >= 4 && x < 6 && y >= 4 && y < 28)
                    {
                        pixels[y * 24 + x] = new Color(0.95f, 0.93f, 0.85f);
                        continue;
                    }

                    // Magic glow for rare books
                    if (data.rarity >= BookRarity.Rare && y >= 12 && y < 20 && x >= 10 && x < 18)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), new Vector2(14, 16)) / 4f;
                        if (dist < 1f)
                        {
                            Color glowColor = data.rarity == BookRarity.Legendary ?
                                new Color(1f, 0.9f, 0.3f) : new Color(0.5f, 0.8f, 1f);
                            pixels[y * 24 + x] = Color.Lerp(glowColor, bookColor, dist);
                            continue;
                        }
                    }

                    pixels[y * 24 + x] = isFilled ? GetRarityColor() : Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            texture.filterMode = FilterMode.Point;

            spriteRenderer.sprite = Sprite.Create(texture, new Rect(0, 0, 24, 32), new Vector2(0.5f, 0.5f), 32);
            spriteRenderer.sortingOrder = 5;
        }

        Color GetRarityColor()
        {
            switch (data.rarity)
            {
                case BookRarity.Common: return new Color(0.5f, 0.35f, 0.2f);
                case BookRarity.Uncommon: return new Color(0.3f, 0.5f, 0.3f);
                case BookRarity.Rare: return new Color(0.3f, 0.4f, 0.7f);
                case BookRarity.Legendary: return new Color(0.7f, 0.5f, 0.2f);
                default: return Color.gray;
            }
        }

        public static MagicBookData GenerateRandomBook()
        {
            float roll = Random.value;
            BookRarity rarity;
            int value;
            string[] prefixes;

            if (roll < 0.5f)
            {
                rarity = BookRarity.Common;
                value = Random.Range(5, 15);
                prefixes = new string[] { "Basic", "Simple", "Old", "Dusty" };
            }
            else if (roll < 0.8f)
            {
                rarity = BookRarity.Uncommon;
                value = Random.Range(20, 40);
                prefixes = new string[] { "Enchanted", "Mystic", "Ancient", "Curious" };
            }
            else if (roll < 0.95f)
            {
                rarity = BookRarity.Rare;
                value = Random.Range(50, 100);
                prefixes = new string[] { "Arcane", "Forbidden", "Sacred", "Ethereal" };
            }
            else
            {
                rarity = BookRarity.Legendary;
                value = Random.Range(150, 300);
                prefixes = new string[] { "Legendary", "Divine", "Primordial", "Cosmic" };
            }

            string[] subjects = new string[] { "Spells", "Potions", "Creatures", "Artifacts", "Dimensions", "Elements" };

            return new MagicBookData
            {
                name = $"{prefixes[Random.Range(0, prefixes.Length)]} Book of {subjects[Random.Range(0, subjects.Length)]}",
                rarity = rarity,
                value = value,
                bookColor = GetRandomBookColor(rarity),
                isDiscovered = false,
                isDisplayed = false
            };
        }

        static Color GetRandomBookColor(BookRarity rarity)
        {
            switch (rarity)
            {
                case BookRarity.Common:
                    return new Color(
                        Random.Range(0.4f, 0.6f),
                        Random.Range(0.25f, 0.4f),
                        Random.Range(0.15f, 0.3f)
                    );
                case BookRarity.Uncommon:
                    return new Color(
                        Random.Range(0.2f, 0.4f),
                        Random.Range(0.4f, 0.6f),
                        Random.Range(0.2f, 0.4f)
                    );
                case BookRarity.Rare:
                    return new Color(
                        Random.Range(0.2f, 0.4f),
                        Random.Range(0.3f, 0.5f),
                        Random.Range(0.6f, 0.8f)
                    );
                case BookRarity.Legendary:
                    return new Color(
                        Random.Range(0.7f, 0.9f),
                        Random.Range(0.5f, 0.7f),
                        Random.Range(0.1f, 0.3f)
                    );
                default:
                    return Color.gray;
            }
        }
    }
}
