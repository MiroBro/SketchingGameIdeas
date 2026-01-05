using UnityEngine;

namespace PlantHusbandry
{
    public class PlantVisual : MonoBehaviour
    {
        private PlantData data;
        private SpriteRenderer spriteRenderer;

        public void Initialize(PlantData plantData)
        {
            data = plantData;
            CreateVisual();
        }

        void CreateVisual()
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

            int texSize = 48;
            Texture2D texture = new Texture2D(texSize, texSize);
            Color[] pixels = new Color[texSize * texSize];

            // Clear
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.clear;

            int centerX = texSize / 2;

            // Calculate sizes based on traits
            int stemH = (int)(10 + data.stemHeight * 20);
            int stemW = (int)(2 + data.stemThickness * 4);
            int leafS = (int)(4 + data.leafSize * 10);
            int numLeaves = (int)(2 + data.leafCount * 4);
            int flowerR = (int)(4 + data.flowerSize * 10);
            int numPetals = (int)(4 + data.petalCount * 8);
            int fruitR = (int)(data.fruitSize * 6);
            int numThorns = (int)(data.thorniness * 8);

            // Draw pot (base)
            for (int y = 0; y < 8; y++)
            {
                int potWidth = 8 - y / 2;
                for (int x = centerX - potWidth; x < centerX + potWidth; x++)
                {
                    if (x >= 0 && x < texSize)
                    {
                        pixels[y * texSize + x] = new Color(0.5f, 0.3f, 0.2f);
                    }
                }
            }

            // Draw stem
            int stemStartY = 8;
            for (int y = stemStartY; y < stemStartY + stemH; y++)
            {
                for (int x = centerX - stemW / 2; x < centerX + stemW / 2 + 1; x++)
                {
                    if (x >= 0 && x < texSize && y >= 0 && y < texSize)
                    {
                        pixels[y * texSize + x] = data.stemColor;
                    }
                }
            }

            // Draw thorns on stem
            for (int t = 0; t < numThorns; t++)
            {
                int thornY = stemStartY + 3 + (stemH - 6) * t / Mathf.Max(1, numThorns - 1);
                bool leftSide = t % 2 == 0;
                int thornX = leftSide ? centerX - stemW / 2 - 2 : centerX + stemW / 2 + 2;
                if (thornX >= 0 && thornX < texSize && thornY >= 0 && thornY < texSize)
                {
                    pixels[thornY * texSize + thornX] = data.stemColor * 0.7f;
                    if (leftSide && thornX + 1 < texSize)
                        pixels[thornY * texSize + thornX + 1] = data.stemColor * 0.7f;
                    else if (!leftSide && thornX - 1 >= 0)
                        pixels[thornY * texSize + thornX - 1] = data.stemColor * 0.7f;
                }
            }

            // Draw leaves along stem
            for (int l = 0; l < numLeaves; l++)
            {
                int leafY = stemStartY + 4 + (stemH - 8) * l / Mathf.Max(1, numLeaves);
                bool leftSide = l % 2 == 0;

                for (int ly = 0; ly < leafS; ly++)
                {
                    int leafWidth = leafS - ly;
                    for (int lx = 0; lx < leafWidth; lx++)
                    {
                        int px = leftSide ? centerX - stemW / 2 - 2 - lx : centerX + stemW / 2 + 2 + lx;
                        int py = leafY + ly - leafS / 2;
                        if (px >= 0 && px < texSize && py >= 0 && py < texSize)
                        {
                            pixels[py * texSize + px] = data.leafColor;
                        }
                    }
                }
            }

            // Draw flower at top
            int flowerCenterY = stemStartY + stemH + flowerR;
            if (flowerCenterY >= texSize) flowerCenterY = texSize - flowerR - 1;

            // Draw petals
            for (int p = 0; p < numPetals; p++)
            {
                float angle = (float)p / numPetals * Mathf.PI * 2f;
                int petalCX = centerX + (int)(Mathf.Cos(angle) * flowerR * 0.7f);
                int petalCY = flowerCenterY + (int)(Mathf.Sin(angle) * flowerR * 0.7f);

                // Draw petal as small ellipse
                int petalR = flowerR / 2 + 1;
                for (int py = -petalR; py <= petalR; py++)
                {
                    for (int px = -petalR; px <= petalR; px++)
                    {
                        if (px * px + py * py <= petalR * petalR)
                        {
                            int finalX = petalCX + px;
                            int finalY = petalCY + py;
                            if (finalX >= 0 && finalX < texSize && finalY >= 0 && finalY < texSize)
                            {
                                pixels[finalY * texSize + finalX] = data.flowerColor;
                            }
                        }
                    }
                }
            }

            // Draw flower center
            int centerR = Mathf.Max(2, flowerR / 3);
            for (int cy = -centerR; cy <= centerR; cy++)
            {
                for (int cx = -centerR; cx <= centerR; cx++)
                {
                    if (cx * cx + cy * cy <= centerR * centerR)
                    {
                        int finalX = centerX + cx;
                        int finalY = flowerCenterY + cy;
                        if (finalX >= 0 && finalX < texSize && finalY >= 0 && finalY < texSize)
                        {
                            pixels[finalY * texSize + finalX] = Color.Lerp(data.flowerColor, Color.yellow, 0.7f);
                        }
                    }
                }
            }

            // Draw fruit if present
            if (fruitR > 1)
            {
                int fruitY = stemStartY + stemH / 2;
                int fruitX = centerX + stemW + fruitR;

                for (int fy = -fruitR; fy <= fruitR; fy++)
                {
                    for (int fx = -fruitR; fx <= fruitR; fx++)
                    {
                        if (fx * fx + fy * fy <= fruitR * fruitR)
                        {
                            int finalX = fruitX + fx;
                            int finalY = fruitY + fy;
                            if (finalX >= 0 && finalX < texSize && finalY >= 0 && finalY < texSize)
                            {
                                Color fruitColor = Color.Lerp(data.flowerColor, Color.red, 0.5f);
                                pixels[finalY * texSize + finalX] = fruitColor;
                            }
                        }
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            texture.filterMode = FilterMode.Point;

            spriteRenderer.sprite = Sprite.Create(texture, new Rect(0, 0, texSize, texSize), new Vector2(0.5f, 0f), texSize);
            spriteRenderer.sortingOrder = 5;
        }

        public PlantData GetData()
        {
            return data;
        }
    }
}
