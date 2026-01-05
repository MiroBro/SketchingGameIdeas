using UnityEngine;

namespace MagicCreatureDigseum
{
    public class DigTileCreature : MonoBehaviour
    {
        private Vector2Int gridPosition;
        private int durability;
        private int maxDurability;
        private bool hasCreature;
        private CreatureData hiddenCreature;
        private bool isDug = false;

        private SpriteRenderer spriteRenderer;

        public void Initialize(Vector2Int pos, int dur, bool hasHiddenCreature, CreatureData creature)
        {
            gridPosition = pos;
            durability = dur;
            maxDurability = dur;
            hasCreature = hasHiddenCreature;
            hiddenCreature = creature;

            CreateVisual();
        }

        void CreateVisual()
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            UpdateVisual();
        }

        void UpdateVisual()
        {
            int size = 32;
            Texture2D texture = new Texture2D(size, size);
            Color[] pixels = new Color[size * size];

            float durPercent = (float)durability / maxDurability;

            Color dirtColor = Color.Lerp(new Color(0.4f, 0.3f, 0.2f), new Color(0.6f, 0.5f, 0.35f), 1f - durPercent);

            if (isDug)
            {
                dirtColor = new Color(0.3f, 0.25f, 0.2f);
            }

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isBorder = x < 2 || x >= size - 2 || y < 2 || y >= size - 2;
                    Color c = isBorder ? dirtColor * 0.7f : dirtColor;

                    // Add some noise
                    if (!isDug && Random.value < 0.1f)
                    {
                        c *= Random.Range(0.8f, 1.2f);
                    }

                    // Hint for creatures (sparkle)
                    if (hasCreature && !isDug && Random.value < 0.02f)
                    {
                        c = Color.Lerp(c, Color.magenta, 0.3f);
                    }

                    pixels[y * size + x] = c;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            texture.filterMode = FilterMode.Point;

            spriteRenderer.sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            spriteRenderer.sortingOrder = 1;
        }

        public CreatureData Dig(int power)
        {
            if (isDug) return null;

            durability -= power;

            if (durability <= 0)
            {
                isDug = true;
                UpdateVisual();

                if (hasCreature && hiddenCreature != null)
                {
                    // Show creature briefly
                    GameObject creatureObj = new GameObject("FoundCreature");
                    creatureObj.transform.position = transform.position;
                    CreatureVisual cv = creatureObj.AddComponent<CreatureVisual>();
                    cv.Initialize(hiddenCreature);
                    Destroy(creatureObj, 1f);

                    return hiddenCreature;
                }
            }
            else
            {
                UpdateVisual();
            }

            return null;
        }

        public bool IsDug()
        {
            return isDug;
        }
    }
}
