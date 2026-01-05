using UnityEngine;

namespace LittleLifeTrain
{
    [System.Serializable]
    public class NPCPreference
    {
        public string furnitureName;
        public int happinessBonus;
    }

    [System.Serializable]
    public class TrainNPCData
    {
        public string name;
        public Color skinColor;
        public Color clothesColor;
        public int baseIncome;
        public NPCPreference[] preferences;

        public static TrainNPCData GenerateRandom()
        {
            string[] names = { "Felix", "Luna", "Oscar", "Bella", "Milo", "Cleo", "Max", "Ruby", "Leo", "Daisy" };

            TrainNPCData npc = new TrainNPCData();
            npc.name = names[Random.Range(0, names.Length)];
            npc.skinColor = new Color(
                Random.Range(0.7f, 1f),
                Random.Range(0.5f, 0.8f),
                Random.Range(0.4f, 0.7f)
            );
            npc.clothesColor = new Color(
                Random.Range(0.2f, 1f),
                Random.Range(0.2f, 1f),
                Random.Range(0.2f, 1f)
            );
            npc.baseIncome = Random.Range(5, 15);

            // Random preferences
            string[] allFurniture = { "Bed", "Chair", "Table", "Lamp", "Plant", "Bookshelf" };
            int numPrefs = Random.Range(2, 4);
            npc.preferences = new NPCPreference[numPrefs];

            for (int i = 0; i < numPrefs; i++)
            {
                npc.preferences[i] = new NPCPreference();
                npc.preferences[i].furnitureName = allFurniture[Random.Range(0, allFurniture.Length)];
                npc.preferences[i].happinessBonus = Random.Range(5, 15);
            }

            return npc;
        }
    }

    public class TrainNPC : MonoBehaviour
    {
        private TrainNPCData data;
        private SpriteRenderer spriteRenderer;
        private float bobTimer;

        public void Initialize(TrainNPCData npcData)
        {
            data = npcData;
            CreateVisual();
        }

        void CreateVisual()
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

            int size = 24;
            Texture2D texture = new Texture2D(size, size);
            Color[] pixels = new Color[size * size];

            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.clear;

            // Body (clothes)
            for (int y = 4; y < 14; y++)
            {
                for (int x = 8; x < 16; x++)
                {
                    pixels[y * size + x] = data.clothesColor;
                }
            }

            // Head
            for (int y = 14; y < 22; y++)
            {
                for (int x = 9; x < 15; x++)
                {
                    float dx = (x - 12) / 3f;
                    float dy = (y - 18) / 4f;
                    if (dx * dx + dy * dy < 1f)
                    {
                        pixels[y * size + x] = data.skinColor;
                    }
                }
            }

            // Eyes
            pixels[18 * size + 10] = Color.black;
            pixels[18 * size + 13] = Color.black;

            // Legs
            for (int y = 0; y < 4; y++)
            {
                pixels[y * size + 9] = data.clothesColor * 0.8f;
                pixels[y * size + 10] = data.clothesColor * 0.8f;
                pixels[y * size + 13] = data.clothesColor * 0.8f;
                pixels[y * size + 14] = data.clothesColor * 0.8f;
            }

            texture.SetPixels(pixels);
            texture.Apply();
            texture.filterMode = FilterMode.Point;

            spriteRenderer.sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0f), size);
            spriteRenderer.sortingOrder = 10;
        }

        void Update()
        {
            // Gentle bobbing
            bobTimer += Time.deltaTime;
            transform.localPosition = new Vector3(
                transform.localPosition.x,
                0.3f + Mathf.Sin(bobTimer * 2f) * 0.05f,
                transform.localPosition.z
            );
        }

        public TrainNPCData GetData()
        {
            return data;
        }

        public int CalculateIncome(string[] furnitureInCart)
        {
            int income = data.baseIncome;

            foreach (var pref in data.preferences)
            {
                foreach (var furniture in furnitureInCart)
                {
                    if (furniture == pref.furnitureName)
                    {
                        income += pref.happinessBonus;
                    }
                }
            }

            return income;
        }
    }
}
