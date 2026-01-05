using UnityEngine;

namespace PlantHusbandry
{
    [System.Serializable]
    public class PlantData
    {
        public string name;
        public int id;
        public int generation;

        // Visual traits (0-1 range)
        public float stemHeight;
        public float stemThickness;
        public float leafSize;
        public float leafCount;
        public float flowerSize;
        public float petalCount;
        public float fruitSize;
        public float thorniness;

        // Colors
        public Color stemColor;
        public Color leafColor;
        public Color flowerColor;

        // Value
        public int value;
        public bool isSold;

        public static PlantData GenerateRandom()
        {
            PlantData plant = new PlantData();
            plant.id = Random.Range(1000, 9999);
            plant.generation = 1;
            plant.name = GenerateName();

            plant.stemHeight = Random.Range(0.3f, 1f);
            plant.stemThickness = Random.Range(0.2f, 0.8f);
            plant.leafSize = Random.Range(0.2f, 1f);
            plant.leafCount = Random.Range(0.2f, 1f);
            plant.flowerSize = Random.Range(0.2f, 1f);
            plant.petalCount = Random.Range(0.2f, 1f);
            plant.fruitSize = Random.Range(0f, 0.8f);
            plant.thorniness = Random.Range(0f, 0.6f);

            plant.stemColor = new Color(
                Random.Range(0.2f, 0.5f),
                Random.Range(0.4f, 0.8f),
                Random.Range(0.1f, 0.4f)
            );
            plant.leafColor = new Color(
                Random.Range(0.1f, 0.5f),
                Random.Range(0.5f, 1f),
                Random.Range(0.1f, 0.4f)
            );
            plant.flowerColor = new Color(
                Random.Range(0.5f, 1f),
                Random.Range(0.2f, 1f),
                Random.Range(0.5f, 1f)
            );

            plant.value = CalculateValue(plant);
            plant.isSold = false;

            return plant;
        }

        public static PlantData Breed(PlantData parent1, PlantData parent2)
        {
            PlantData offspring = new PlantData();
            offspring.id = Random.Range(1000, 9999);
            offspring.generation = Mathf.Max(parent1.generation, parent2.generation) + 1;
            offspring.name = GenerateName();

            // Mix traits
            offspring.stemHeight = MixTrait(parent1.stemHeight, parent2.stemHeight);
            offspring.stemThickness = MixTrait(parent1.stemThickness, parent2.stemThickness);
            offspring.leafSize = MixTrait(parent1.leafSize, parent2.leafSize);
            offspring.leafCount = MixTrait(parent1.leafCount, parent2.leafCount);
            offspring.flowerSize = MixTrait(parent1.flowerSize, parent2.flowerSize);
            offspring.petalCount = MixTrait(parent1.petalCount, parent2.petalCount);
            offspring.fruitSize = MixTrait(parent1.fruitSize, parent2.fruitSize);
            offspring.thorniness = MixTrait(parent1.thorniness, parent2.thorniness);

            // Mix colors with possible mutation
            offspring.stemColor = MixColor(parent1.stemColor, parent2.stemColor);
            offspring.leafColor = MixColor(parent1.leafColor, parent2.leafColor);
            offspring.flowerColor = MixColor(parent1.flowerColor, parent2.flowerColor);

            offspring.value = CalculateValue(offspring);
            offspring.isSold = false;

            return offspring;
        }

        static float MixTrait(float a, float b)
        {
            float result;
            float r = Random.value;
            if (r < 0.35f)
                result = a;
            else if (r < 0.7f)
                result = b;
            else
                result = (a + b) / 2f;

            // Mutation chance
            if (Random.value < 0.15f)
            {
                result += Random.Range(-0.2f, 0.2f);
            }

            return Mathf.Clamp01(result);
        }

        static Color MixColor(Color a, Color b)
        {
            Color result = Color.Lerp(a, b, Random.Range(0.3f, 0.7f));

            // Mutation
            if (Random.value < 0.2f)
            {
                result.r = Mathf.Clamp01(result.r + Random.Range(-0.2f, 0.2f));
                result.g = Mathf.Clamp01(result.g + Random.Range(-0.2f, 0.2f));
                result.b = Mathf.Clamp01(result.b + Random.Range(-0.2f, 0.2f));
            }

            return result;
        }

        static int CalculateValue(PlantData plant)
        {
            float score = 0;

            // Extreme traits are more valuable
            score += Mathf.Abs(plant.stemHeight - 0.5f) * 20;
            score += Mathf.Abs(plant.leafSize - 0.5f) * 15;
            score += Mathf.Abs(plant.flowerSize - 0.5f) * 25;
            score += plant.petalCount * 10;
            score += plant.fruitSize * 20;
            score += plant.thorniness * 10;

            // Generation bonus
            score += plant.generation * 8;

            // Color uniqueness (how far from green)
            float colorUniqueness = Mathf.Abs(plant.flowerColor.r - plant.flowerColor.g) +
                                   Mathf.Abs(plant.flowerColor.b - plant.flowerColor.g);
            score += colorUniqueness * 15;

            return Mathf.Max(5, (int)score);
        }

        static string GenerateName()
        {
            string[] prefixes = { "Luna", "Solar", "Mystic", "Crystal", "Shadow", "Golden", "Silver", "Twilight", "Dawn", "Frost" };
            string[] types = { "Rose", "Lily", "Fern", "Orchid", "Vine", "Bloom", "Blossom", "Sprout", "Petal", "Thorn" };
            return prefixes[Random.Range(0, prefixes.Length)] + " " + types[Random.Range(0, types.Length)];
        }
    }
}
