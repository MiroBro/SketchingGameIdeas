using UnityEngine;
using UnityEngine.InputSystem;

namespace UnderwaterKingdom
{
    public class UnderwaterPlayer : MonoBehaviour
    {
        public float moveSpeed = 5f;
        public SpriteRenderer spriteRenderer;
        public int pearls = 10;
        public bool hasCrown = true;

        private float worldMinY = -15f;
        private float worldMaxY = 15f;

        void Start()
        {
            CreateVisual();
        }

        void CreateVisual()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            // Create a simple crown/player sprite
            Texture2D texture = new Texture2D(32, 32);
            Color[] pixels = new Color[32 * 32];

            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    bool isFilled = false;

                    // Body (oval)
                    float dx = (x - 16) / 8f;
                    float dy = (y - 12) / 10f;
                    if (dx * dx + dy * dy < 1f)
                    {
                        isFilled = true;
                    }

                    // Crown points
                    if (y >= 24 && y < 30)
                    {
                        if ((x >= 8 && x < 12) || (x >= 14 && x < 18) || (x >= 20 && x < 24))
                        {
                            isFilled = true;
                        }
                    }
                    if (y >= 22 && y < 26 && x >= 8 && x < 24)
                    {
                        isFilled = true;
                    }

                    pixels[y * 32 + x] = isFilled ? new Color(1f, 0.85f, 0.2f) : Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            texture.filterMode = FilterMode.Point;

            spriteRenderer.sprite = Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);
            spriteRenderer.sortingOrder = 10;
        }

        void Update()
        {
            if (!hasCrown) return;

            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            float vertical = 0f;
            float horizontal = 0f;

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) vertical = 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) vertical = -1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) horizontal = -1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) horizontal = 1f;

            Vector3 movement = new Vector3(horizontal * 0.3f, vertical, 0) * moveSpeed * Time.deltaTime;
            transform.position += movement;

            // Clamp position
            float clampedY = Mathf.Clamp(transform.position.y, worldMinY, worldMaxY);
            float clampedX = Mathf.Clamp(transform.position.x, -3f, 3f);
            transform.position = new Vector3(clampedX, clampedY, 0);
        }

        public void SetWorldBounds(float minY, float maxY)
        {
            worldMinY = minY;
            worldMaxY = maxY;
        }

        public bool SpendPearls(int amount)
        {
            if (pearls >= amount)
            {
                pearls -= amount;
                return true;
            }
            return false;
        }

        public void AddPearls(int amount)
        {
            pearls += amount;
        }

        public void LoseCrown()
        {
            hasCrown = false;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            }
        }
    }
}
