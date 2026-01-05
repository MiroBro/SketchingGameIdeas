using UnityEngine;

namespace UnderwaterKingdom
{
    public class SeaDefender : MonoBehaviour
    {
        public float patrolSpeed = 2f;
        public float attackRange = 1.5f;
        public int damage = 10;
        public float attackCooldown = 1f;
        public float patrolRange = 3f;
        public int health = 50;

        private SpriteRenderer spriteRenderer;
        private float patrolCenter;
        private float patrolDirection = 1f;
        private float attackTimer;
        private SeaMonster currentTarget;
        private bool isRecruited = false;

        public void Initialize(float centerY, bool recruited = false)
        {
            patrolCenter = centerY;
            isRecruited = recruited;
            CreateVisual();
        }

        void CreateVisual()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
                if (spriteRenderer == null)
                {
                    spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                }
            }

            Texture2D texture = new Texture2D(24, 24);
            Color[] pixels = new Color[24 * 24];

            // Create fish defender shape
            for (int y = 0; y < 24; y++)
            {
                for (int x = 0; x < 24; x++)
                {
                    bool isFilled = false;

                    // Body (oval)
                    float dx = (x - 12) / 8f;
                    float dy = (y - 12) / 6f;
                    if (dx * dx + dy * dy < 1f)
                    {
                        isFilled = true;
                    }

                    // Tail
                    if (x < 6 && y >= 8 && y < 16)
                    {
                        float tailDist = Mathf.Abs(y - 12) / 4f;
                        if (x >= 6 - (4 - (int)(tailDist * 4)))
                        {
                            isFilled = true;
                        }
                    }

                    // Spear (weapon)
                    if (isRecruited && x >= 18 && x < 24 && y >= 10 && y < 14)
                    {
                        isFilled = true;
                    }

                    Color defenderColor = isRecruited ? new Color(0.3f, 0.7f, 1f) : new Color(0.5f, 0.5f, 0.5f);
                    pixels[y * 24 + x] = isFilled ? defenderColor : Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            texture.filterMode = FilterMode.Point;

            spriteRenderer.sprite = Sprite.Create(texture, new Rect(0, 0, 24, 24), new Vector2(0.5f, 0.5f), 24);
            spriteRenderer.sortingOrder = 5;
        }

        void Update()
        {
            if (!isRecruited)
            {
                // Idle behavior - bob up and down
                transform.position += new Vector3(0, Mathf.Sin(Time.time * 2f) * 0.01f, 0);
                return;
            }

            attackTimer -= Time.deltaTime;

            // Look for targets
            if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
            {
                FindTarget();
            }

            if (currentTarget != null)
            {
                // Move towards target
                float dirY = currentTarget.transform.position.y - transform.position.y;
                if (Mathf.Abs(dirY) > attackRange)
                {
                    transform.position += new Vector3(0, Mathf.Sign(dirY) * patrolSpeed * Time.deltaTime, 0);
                }
                else if (attackTimer <= 0)
                {
                    Attack();
                }
            }
            else
            {
                // Patrol
                Patrol();
            }
        }

        void Patrol()
        {
            transform.position += new Vector3(0, patrolDirection * patrolSpeed * 0.5f * Time.deltaTime, 0);

            if (transform.position.y > patrolCenter + patrolRange)
            {
                patrolDirection = -1f;
            }
            else if (transform.position.y < patrolCenter - patrolRange)
            {
                patrolDirection = 1f;
            }
        }

        void FindTarget()
        {
            SeaMonster[] monsters = FindObjectsByType<SeaMonster>(FindObjectsSortMode.None);
            float closestDist = float.MaxValue;

            foreach (var monster in monsters)
            {
                float dist = Vector3.Distance(transform.position, monster.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    currentTarget = monster;
                }
            }
        }

        void Attack()
        {
            if (currentTarget != null)
            {
                currentTarget.TakeDamage(damage);
                attackTimer = attackCooldown;
            }
        }

        public void TakeDamage(int dmg)
        {
            health -= dmg;
            if (health <= 0)
            {
                Destroy(gameObject);
            }
        }

        public void Recruit()
        {
            isRecruited = true;
            CreateVisual();
        }

        public bool IsRecruited()
        {
            return isRecruited;
        }

        public int GetRecruitCost()
        {
            return 3;
        }
    }
}
