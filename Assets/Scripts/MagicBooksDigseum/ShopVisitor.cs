using UnityEngine;

namespace MagicBooksDigseum
{
    public class ShopVisitor : MonoBehaviour
    {
        public float moveSpeed = 1f;
        public float browseTime = 3f;
        public int basePayment = 1;

        private SpriteRenderer spriteRenderer;
        private Vector3 targetPosition;
        private float browseTimer;
        private bool isBrowsing = false;
        private bool isLeaving = false;
        private Vector3 exitPosition;
        private System.Action<int> onPayment;

        public void Initialize(Vector3 target, Vector3 exit, int booksDisplayed, System.Action<int> paymentCallback)
        {
            targetPosition = target;
            exitPosition = exit;
            onPayment = paymentCallback;
            basePayment = Mathf.Max(1, booksDisplayed / 2);
            CreateVisual();
        }

        void CreateVisual()
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

            Texture2D texture = new Texture2D(16, 24);
            Color[] pixels = new Color[16 * 24];

            // Random visitor color
            Color bodyColor = new Color(
                Random.Range(0.3f, 0.7f),
                Random.Range(0.3f, 0.7f),
                Random.Range(0.3f, 0.7f)
            );

            for (int y = 0; y < 24; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    bool isFilled = false;

                    // Head (circle)
                    float dx = (x - 8) / 4f;
                    float dy = (y - 18) / 4f;
                    if (dx * dx + dy * dy < 1f)
                    {
                        pixels[y * 16 + x] = new Color(0.9f, 0.75f, 0.6f); // Skin color
                        continue;
                    }

                    // Body
                    if (x >= 4 && x < 12 && y >= 4 && y < 14)
                    {
                        isFilled = true;
                    }

                    // Legs
                    if ((x >= 4 && x < 7 || x >= 9 && x < 12) && y >= 0 && y < 4)
                    {
                        isFilled = true;
                    }

                    pixels[y * 16 + x] = isFilled ? bodyColor : Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            texture.filterMode = FilterMode.Point;

            spriteRenderer.sprite = Sprite.Create(texture, new Rect(0, 0, 16, 24), new Vector2(0.5f, 0f), 24);
            spriteRenderer.sortingOrder = 3;
        }

        void Update()
        {
            if (isLeaving)
            {
                // Move toward exit
                transform.position = Vector3.MoveTowards(transform.position, exitPosition, moveSpeed * Time.deltaTime);

                if (Vector3.Distance(transform.position, exitPosition) < 0.1f)
                {
                    Destroy(gameObject);
                }
            }
            else if (isBrowsing)
            {
                browseTimer -= Time.deltaTime;

                // Slight bobbing while browsing
                transform.position += new Vector3(0, Mathf.Sin(Time.time * 3f) * 0.002f, 0);

                if (browseTimer <= 0)
                {
                    // Pay and leave
                    onPayment?.Invoke(basePayment);
                    isLeaving = true;
                }
            }
            else
            {
                // Move toward browse position
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

                if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
                {
                    isBrowsing = true;
                    browseTimer = browseTime;
                }
            }
        }
    }
}
