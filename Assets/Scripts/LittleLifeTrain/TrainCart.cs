using UnityEngine;
using System.Collections.Generic;

namespace LittleLifeTrain
{
    public class TrainCart : MonoBehaviour
    {
        private TrainNPCData owner;
        private TrainNPC npcVisual;
        private List<string> furniture = new List<string>();
        private List<GameObject> furnitureObjects = new List<GameObject>();
        private SpriteRenderer cartRenderer;

        public void Initialize(TrainNPCData npcData, int cartIndex)
        {
            owner = npcData;
            CreateCartVisual();
            CreateNPC();
        }

        void CreateCartVisual()
        {
            cartRenderer = gameObject.AddComponent<SpriteRenderer>();

            int width = 80;
            int height = 48;
            Texture2D texture = new Texture2D(width, height);
            Color[] pixels = new Color[width * height];

            Color cartColor = new Color(0.5f, 0.4f, 0.3f);
            Color wheelColor = new Color(0.2f, 0.2f, 0.2f);
            Color windowColor = new Color(0.7f, 0.85f, 1f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color c = Color.clear;

                    // Cart body
                    if (y >= 10 && y < 44 && x >= 2 && x < width - 2)
                    {
                        c = cartColor;

                        // Roof
                        if (y >= 40)
                        {
                            c = cartColor * 0.7f;
                        }

                        // Window
                        if (y >= 25 && y < 38 && x >= 10 && x < 35)
                        {
                            c = windowColor;
                        }

                        // Door
                        if (y >= 12 && y < 38 && x >= 55 && x < 72)
                        {
                            c = cartColor * 0.85f;
                            if (x == 55 || x == 71 || y == 12 || y == 37)
                            {
                                c = cartColor * 0.6f;
                            }
                        }
                    }

                    // Wheels
                    float wheel1Dist = Mathf.Sqrt(Mathf.Pow(x - 18, 2) + Mathf.Pow(y - 6, 2));
                    float wheel2Dist = Mathf.Sqrt(Mathf.Pow(x - 62, 2) + Mathf.Pow(y - 6, 2));
                    if (wheel1Dist < 6 || wheel2Dist < 6)
                    {
                        c = wheelColor;
                    }

                    // Connector
                    if (y >= 18 && y < 24 && (x < 4 || x >= width - 4))
                    {
                        c = new Color(0.3f, 0.3f, 0.3f);
                    }

                    pixels[y * width + x] = c;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            texture.filterMode = FilterMode.Point;

            cartRenderer.sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0f), 32);
            cartRenderer.sortingOrder = 0;
        }

        void CreateNPC()
        {
            GameObject npcObj = new GameObject("NPC");
            npcObj.transform.parent = transform;
            npcObj.transform.localPosition = new Vector3(-0.3f, 0.3f, 0);

            npcVisual = npcObj.AddComponent<TrainNPC>();
            npcVisual.Initialize(owner);
        }

        public void AddFurniture(string furnitureName, Vector3 localPos)
        {
            furniture.Add(furnitureName);

            GameObject furnObj = new GameObject(furnitureName);
            furnObj.transform.parent = transform;
            furnObj.transform.localPosition = localPos;

            SpriteRenderer sr = furnObj.AddComponent<SpriteRenderer>();
            sr.sprite = CreateFurnitureSprite(furnitureName);
            sr.sortingOrder = 5;

            furnitureObjects.Add(furnObj);
        }

        Sprite CreateFurnitureSprite(string name)
        {
            int size = 16;
            Texture2D tex = new Texture2D(size, size);
            Color[] pixels = new Color[size * size];

            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.clear;

            Color mainColor = GetFurnitureColor(name);

            switch (name)
            {
                case "Bed":
                    // Bed frame
                    for (int y = 0; y < 6; y++)
                        for (int x = 1; x < 15; x++)
                            pixels[y * size + x] = mainColor;
                    // Pillow
                    for (int y = 6; y < 10; y++)
                        for (int x = 1; x < 5; x++)
                            pixels[y * size + x] = Color.white;
                    break;

                case "Chair":
                    // Seat
                    for (int y = 4; y < 8; y++)
                        for (int x = 3; x < 13; x++)
                            pixels[y * size + x] = mainColor;
                    // Back
                    for (int y = 8; y < 14; y++)
                        for (int x = 3; x < 6; x++)
                            pixels[y * size + x] = mainColor;
                    // Legs
                    for (int y = 0; y < 4; y++)
                    {
                        pixels[y * size + 4] = mainColor * 0.7f;
                        pixels[y * size + 11] = mainColor * 0.7f;
                    }
                    break;

                case "Table":
                    // Top
                    for (int y = 8; y < 12; y++)
                        for (int x = 1; x < 15; x++)
                            pixels[y * size + x] = mainColor;
                    // Legs
                    for (int y = 0; y < 8; y++)
                    {
                        pixels[y * size + 2] = mainColor * 0.7f;
                        pixels[y * size + 3] = mainColor * 0.7f;
                        pixels[y * size + 12] = mainColor * 0.7f;
                        pixels[y * size + 13] = mainColor * 0.7f;
                    }
                    break;

                case "Lamp":
                    // Base
                    for (int x = 6; x < 10; x++)
                        pixels[0 * size + x] = mainColor * 0.5f;
                    // Pole
                    for (int y = 1; y < 10; y++)
                    {
                        pixels[y * size + 7] = mainColor * 0.7f;
                        pixels[y * size + 8] = mainColor * 0.7f;
                    }
                    // Shade
                    for (int y = 10; y < 15; y++)
                        for (int x = 4; x < 12; x++)
                            pixels[y * size + x] = new Color(1f, 1f, 0.7f);
                    break;

                case "Plant":
                    // Pot
                    for (int y = 0; y < 5; y++)
                        for (int x = 5; x < 11; x++)
                            pixels[y * size + x] = new Color(0.6f, 0.3f, 0.2f);
                    // Leaves
                    for (int y = 5; y < 14; y++)
                        for (int x = 3; x < 13; x++)
                        {
                            float dx = (x - 8) / 5f;
                            float dy = (y - 10) / 5f;
                            if (dx * dx + dy * dy < 1f)
                                pixels[y * size + x] = mainColor;
                        }
                    break;

                case "Bookshelf":
                    // Frame
                    for (int y = 0; y < 14; y++)
                    {
                        for (int x = 1; x < 15; x++)
                        {
                            if (x == 1 || x == 14 || y == 0 || y == 6 || y == 13)
                                pixels[y * size + x] = mainColor;
                        }
                    }
                    // Books
                    Color[] bookColors = { Color.red, Color.blue, Color.green, new Color(0.5f, 0.3f, 0.1f) };
                    for (int bx = 3; bx < 14; bx += 2)
                    {
                        Color bc = bookColors[Random.Range(0, bookColors.Length)];
                        for (int by = 1; by < 5; by++)
                            pixels[by * size + bx] = bc;
                        for (int by = 7; by < 12; by++)
                            pixels[by * size + bx] = bc;
                    }
                    break;
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;

            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0f), 16);
        }

        Color GetFurnitureColor(string name)
        {
            switch (name)
            {
                case "Bed": return new Color(0.8f, 0.6f, 0.5f);
                case "Chair": return new Color(0.6f, 0.4f, 0.3f);
                case "Table": return new Color(0.5f, 0.35f, 0.25f);
                case "Lamp": return new Color(0.7f, 0.7f, 0.5f);
                case "Plant": return new Color(0.3f, 0.7f, 0.3f);
                case "Bookshelf": return new Color(0.45f, 0.3f, 0.2f);
                default: return Color.gray;
            }
        }

        public TrainNPCData GetOwner()
        {
            return owner;
        }

        public string[] GetFurniture()
        {
            return furniture.ToArray();
        }

        public int GetFurnitureCount()
        {
            return furniture.Count;
        }

        public int CalculateIncome()
        {
            if (npcVisual != null)
            {
                return npcVisual.CalculateIncome(furniture.ToArray());
            }
            return owner.baseIncome;
        }
    }
}
