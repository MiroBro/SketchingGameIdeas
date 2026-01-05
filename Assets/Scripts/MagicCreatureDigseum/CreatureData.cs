using UnityEngine;

namespace MagicCreatureDigseum
{
    [System.Serializable]
    public class CreatureData
    {
        public string name;
        public int id;
        public int generation;

        // Visual traits (0-1 range for mixing)
        public float bodyWidth;
        public float bodyHeight;
        public float neckLength;
        public float legCount;
        public float tailLength;
        public float headSize;
        public float earSize;
        public float eyeCount;

        // Colors
        public Color bodyColor;
        public Color accentColor;

        // Value based on rarity/uniqueness
        public int value;

        public static CreatureData GenerateRandom()
        {
            CreatureData creature = new CreatureData();
            creature.id = Random.Range(1000, 9999);
            creature.generation = 1;
            creature.name = GenerateName();

            creature.bodyWidth = Random.Range(0.3f, 1f);
            creature.bodyHeight = Random.Range(0.3f, 1f);
            creature.neckLength = Random.Range(0f, 1f);
            creature.legCount = Random.Range(0f, 1f);
            creature.tailLength = Random.Range(0f, 1f);
            creature.headSize = Random.Range(0.3f, 1f);
            creature.earSize = Random.Range(0f, 1f);
            creature.eyeCount = Random.Range(0.2f, 1f);

            creature.bodyColor = new Color(Random.Range(0.2f, 1f), Random.Range(0.2f, 1f), Random.Range(0.2f, 1f));
            creature.accentColor = new Color(Random.Range(0.2f, 1f), Random.Range(0.2f, 1f), Random.Range(0.2f, 1f));

            creature.value = CalculateValue(creature);

            return creature;
        }

        public static CreatureData Breed(CreatureData parent1, CreatureData parent2)
        {
            CreatureData offspring = new CreatureData();
            offspring.id = Random.Range(1000, 9999);
            offspring.generation = Mathf.Max(parent1.generation, parent2.generation) + 1;
            offspring.name = GenerateName();

            // Mix traits with some randomness
            offspring.bodyWidth = MixTrait(parent1.bodyWidth, parent2.bodyWidth);
            offspring.bodyHeight = MixTrait(parent1.bodyHeight, parent2.bodyHeight);
            offspring.neckLength = MixTrait(parent1.neckLength, parent2.neckLength);
            offspring.legCount = MixTrait(parent1.legCount, parent2.legCount);
            offspring.tailLength = MixTrait(parent1.tailLength, parent2.tailLength);
            offspring.headSize = MixTrait(parent1.headSize, parent2.headSize);
            offspring.earSize = MixTrait(parent1.earSize, parent2.earSize);
            offspring.eyeCount = MixTrait(parent1.eyeCount, parent2.eyeCount);

            // Mix colors
            offspring.bodyColor = Color.Lerp(parent1.bodyColor, parent2.bodyColor, Random.Range(0.3f, 0.7f));
            offspring.accentColor = Color.Lerp(parent1.accentColor, parent2.accentColor, Random.Range(0.3f, 0.7f));

            // Add slight mutation
            if (Random.value < 0.2f)
            {
                offspring.bodyColor = new Color(
                    Mathf.Clamp01(offspring.bodyColor.r + Random.Range(-0.2f, 0.2f)),
                    Mathf.Clamp01(offspring.bodyColor.g + Random.Range(-0.2f, 0.2f)),
                    Mathf.Clamp01(offspring.bodyColor.b + Random.Range(-0.2f, 0.2f))
                );
            }

            offspring.value = CalculateValue(offspring);

            return offspring;
        }

        static float MixTrait(float a, float b)
        {
            // Randomly pick from parent or average with mutation
            float result;
            float r = Random.value;
            if (r < 0.4f)
                result = a;
            else if (r < 0.8f)
                result = b;
            else
                result = (a + b) / 2f;

            // Small mutation
            result += Random.Range(-0.1f, 0.1f);
            return Mathf.Clamp01(result);
        }

        static int CalculateValue(CreatureData creature)
        {
            // Value based on extremes and generation
            float extremeScore = 0;
            extremeScore += Mathf.Abs(creature.bodyWidth - 0.5f) * 2f;
            extremeScore += Mathf.Abs(creature.bodyHeight - 0.5f) * 2f;
            extremeScore += creature.neckLength;
            extremeScore += creature.tailLength;
            extremeScore += creature.earSize;

            int baseValue = 10 + (int)(extremeScore * 20);
            baseValue += creature.generation * 5;

            return baseValue;
        }

        static string GenerateName()
        {
            string[] prefixes = { "Fluff", "Spark", "Glim", "Wob", "Zix", "Bloop", "Snor", "Pip", "Mog", "Quirk" };
            string[] suffixes = { "ling", "ox", "us", "ara", "ini", "ble", "wump", "kin", "let", "zoid" };
            return prefixes[Random.Range(0, prefixes.Length)] + suffixes[Random.Range(0, suffixes.Length)];
        }
    }
}
