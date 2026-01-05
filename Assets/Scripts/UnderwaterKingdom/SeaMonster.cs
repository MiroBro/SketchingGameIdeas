using UnityEngine;

namespace UnderwaterKingdom
{
    public class SeaMonster : MonoBehaviour
    {
        public float moveSpeed = 2f;
        public int damage = 20;
        public float attackCooldown = 1.5f;
        public int health = 30;
        public bool comingFromAbove = true;

        private SpriteRenderer spriteRenderer;
        private float attackTimer;
        private Transform targetBuilding;
        private Transform playerTarget;

        public void Initialize(bool fromAbove)
        {
            comingFromAbove = fromAbove;
            CreateVisual();
        }

        void CreateVisual()
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

            Texture2D texture = new Texture2D(32, 32);
            Color[] pixels = new Color[32 * 32];

            // Create monster shape (spiky, scary)
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    bool isFilled = false;

                    // Body
                    float dx = (x - 16) / 10f;
                    float dy = (y - 16) / 8f;
                    if (dx * dx + dy * dy < 1f)
                    {
                        isFilled = true;
                    }

                    // Spikes/tentacles
                    if (y < 8 || y >= 24)
                    {
                        int spikeIndex = x / 6;
                        int spikeX = x % 6;
                        if (spikeX >= 2 && spikeX < 4)
                        {
                            int spikeHeight = (spikeIndex % 2 == 0) ? 6 : 4;
                            if (y < 8 && y >= 8 - spikeHeight)
                            {
                                isFilled = true;
                            }
                            if (y >= 24 && y < 24 + spikeHeight)
                            {
                                isFilled = true;
                            }
                        }
                    }

                    // Eyes (menacing)
                    if ((x >= 10 && x < 14 && y >= 14 && y < 18) ||
                        (x >= 18 && x < 22 && y >= 14 && y < 18))
                    {
                        pixels[y * 32 + x] = Color.red;
                        continue;
                    }

                    pixels[y * 32 + x] = isFilled ? new Color(0.3f, 0.1f, 0.3f) : Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            texture.filterMode = FilterMode.Point;

            spriteRenderer.sprite = Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);
            spriteRenderer.sortingOrder = 4;

            // Flip if coming from below
            if (!comingFromAbove)
            {
                spriteRenderer.flipY = true;
            }
        }

        void Update()
        {
            attackTimer -= Time.deltaTime;

            // Move towards center (settlement area)
            float direction = comingFromAbove ? -1f : 1f;

            // Check for buildings or player to attack
            FindTarget();

            if (targetBuilding != null)
            {
                float distToBuilding = Mathf.Abs(transform.position.y - targetBuilding.position.y);
                if (distToBuilding < 1f)
                {
                    if (attackTimer <= 0)
                    {
                        AttackBuilding();
                    }
                }
                else
                {
                    transform.position += new Vector3(0, direction * moveSpeed * Time.deltaTime, 0);
                }
            }
            else if (playerTarget != null)
            {
                float distToPlayer = Vector3.Distance(transform.position, playerTarget.position);
                if (distToPlayer < 1f)
                {
                    if (attackTimer <= 0)
                    {
                        AttackPlayer();
                    }
                }
                else
                {
                    transform.position += new Vector3(0, direction * moveSpeed * Time.deltaTime, 0);
                }
            }
            else
            {
                // Just move toward center
                transform.position += new Vector3(0, direction * moveSpeed * Time.deltaTime, 0);
            }
        }

        void FindTarget()
        {
            // Find nearest building
            UnderwaterBuilding[] buildings = FindObjectsByType<UnderwaterBuilding>(FindObjectsSortMode.None);
            float closestDist = float.MaxValue;
            targetBuilding = null;

            foreach (var building in buildings)
            {
                // Only target buildings in our path
                bool inPath = comingFromAbove ?
                    (building.transform.position.y < transform.position.y) :
                    (building.transform.position.y > transform.position.y);

                if (inPath)
                {
                    float dist = Mathf.Abs(building.transform.position.y - transform.position.y);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        targetBuilding = building.transform;
                    }
                }
            }

            // Find player if no buildings
            if (targetBuilding == null)
            {
                UnderwaterPlayer player = FindAnyObjectByType<UnderwaterPlayer>();
                if (player != null && player.hasCrown)
                {
                    playerTarget = player.transform;
                }
            }
        }

        void AttackBuilding()
        {
            if (targetBuilding != null)
            {
                UnderwaterBuilding building = targetBuilding.GetComponent<UnderwaterBuilding>();
                if (building != null)
                {
                    building.TakeDamage(damage);
                }
                attackTimer = attackCooldown;
            }
        }

        void AttackPlayer()
        {
            if (playerTarget != null)
            {
                UnderwaterPlayer player = playerTarget.GetComponent<UnderwaterPlayer>();
                if (player != null && player.hasCrown)
                {
                    player.LoseCrown();
                }
                attackTimer = attackCooldown;
            }
        }

        public void TakeDamage(int dmg)
        {
            health -= dmg;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.red;
                Invoke(nameof(ResetColor), 0.1f);
            }

            if (health <= 0)
            {
                Destroy(gameObject);
            }
        }

        void ResetColor()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.white;
            }
        }
    }
}
