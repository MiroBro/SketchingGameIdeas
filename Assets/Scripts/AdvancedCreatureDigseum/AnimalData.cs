using UnityEngine;

namespace AdvancedCreatureDigseum
{
    [System.Serializable]
    public class AnimalTraits
    {
        // Body shape (0-1 range)
        public float BodyLength;      // 0 = round/ball, 1 = long
        public float BodyHeight;      // 0 = flat, 1 = tall
        public float NeckLength;      // 0 = no neck, 1 = giraffe-like
        public float LegLength;       // 0 = no legs, 1 = long legs
        public float LegCount;        // 0 = 0 legs, 1 = 4+ legs
        public float TailLength;      // 0 = no tail, 1 = long tail
        public float HeadSize;        // 0 = tiny, 1 = big
        public float EarSize;         // 0 = no ears, 1 = big ears
        public float HasFins;         // 0 = no fins, 1 = fins
        public float HasWings;        // 0 = no wings, 1 = wings

        // Colors
        public Color BodyColor;
        public Color AccentColor;

        public static AnimalTraits Mix(AnimalTraits a, AnimalTraits b)
        {
            AnimalTraits result = new AnimalTraits();

            result.BodyLength = MixValue(a.BodyLength, b.BodyLength);
            result.BodyHeight = MixValue(a.BodyHeight, b.BodyHeight);
            result.NeckLength = MixValue(a.NeckLength, b.NeckLength);
            result.LegLength = MixValue(a.LegLength, b.LegLength);
            result.LegCount = MixValue(a.LegCount, b.LegCount);
            result.TailLength = MixValue(a.TailLength, b.TailLength);
            result.HeadSize = MixValue(a.HeadSize, b.HeadSize);
            result.EarSize = MixValue(a.EarSize, b.EarSize);
            result.HasFins = MixValue(a.HasFins, b.HasFins);
            result.HasWings = MixValue(a.HasWings, b.HasWings);

            result.BodyColor = Color.Lerp(a.BodyColor, b.BodyColor, Random.Range(0.3f, 0.7f));
            result.AccentColor = Color.Lerp(a.AccentColor, b.AccentColor, Random.Range(0.3f, 0.7f));

            // Slight mutations
            if (Random.value < 0.15f)
            {
                result.BodyColor = new Color(
                    Mathf.Clamp01(result.BodyColor.r + Random.Range(-0.15f, 0.15f)),
                    Mathf.Clamp01(result.BodyColor.g + Random.Range(-0.15f, 0.15f)),
                    Mathf.Clamp01(result.BodyColor.b + Random.Range(-0.15f, 0.15f))
                );
            }

            return result;
        }

        static float MixValue(float a, float b)
        {
            float r = Random.value;
            float result;
            if (r < 0.4f) result = a;
            else if (r < 0.8f) result = b;
            else result = (a + b) / 2f;

            // Small mutation
            result += Random.Range(-0.1f, 0.1f);
            return Mathf.Clamp01(result);
        }
    }

    [System.Serializable]
    public class AnimalData
    {
        public string Id;
        public string Name;
        public int BiomeIndex;
        public int Rarity; // 1-3, higher = rarer
        public AnimalTraits Traits;

        public AnimalData(string id, string name, int biome, int rarity, AnimalTraits traits)
        {
            Id = id;
            Name = name;
            BiomeIndex = biome;
            Rarity = rarity;
            Traits = traits;
        }
    }

    // Database of all animals
    public static class AnimalDatabase
    {
        public static AnimalData GetAnimal(string id)
        {
            foreach (var biome in BiomeDatabase.Biomes)
            {
                foreach (var animal in biome.Animals)
                {
                    if (animal.Id == id) return animal;
                }
            }
            return null;
        }
    }

    // Biome data
    [System.Serializable]
    public class BiomeData
    {
        public int Index;
        public string Name;
        public Color BackgroundColor;
        public Color FogColor;
        public int UnlockCost;
        public int Difficulty; // Affects energy cost
        public AnimalData[] Animals;

        public BiomeData(int index, string name, Color bgColor, Color fogColor, int unlockCost, int difficulty)
        {
            Index = index;
            Name = name;
            BackgroundColor = bgColor;
            FogColor = fogColor;
            UnlockCost = unlockCost;
            Difficulty = difficulty;
            Animals = new AnimalData[3];
        }
    }

    public static class BiomeDatabase
    {
        public static BiomeData[] Biomes;

        static BiomeDatabase()
        {
            InitializeBiomes();
        }

        static void InitializeBiomes()
        {
            Biomes = new BiomeData[10];

            // Biome costs balanced for 2-3 hour gameplay with exponential scaling
            // Biome 0: Meadow (starter)
            Biomes[0] = new BiomeData(0, "Meadow", new Color(0.4f, 0.7f, 0.3f), new Color(0.6f, 0.8f, 0.6f, 0.9f), 0, 1);
            Biomes[0].Animals[0] = new AnimalData("meadow_rabbit", "Rabbit", 0, 1,
                new AnimalTraits { BodyLength = 0.4f, BodyHeight = 0.4f, NeckLength = 0.1f, LegLength = 0.4f, LegCount = 0.8f, TailLength = 0.2f, HeadSize = 0.5f, EarSize = 0.9f, HasFins = 0f, HasWings = 0f, BodyColor = new Color(0.6f, 0.5f, 0.4f), AccentColor = Color.white });
            Biomes[0].Animals[1] = new AnimalData("meadow_deer", "Deer", 0, 2,
                new AnimalTraits { BodyLength = 0.7f, BodyHeight = 0.6f, NeckLength = 0.5f, LegLength = 0.8f, LegCount = 0.8f, TailLength = 0.2f, HeadSize = 0.4f, EarSize = 0.5f, HasFins = 0f, HasWings = 0f, BodyColor = new Color(0.6f, 0.4f, 0.2f), AccentColor = new Color(0.9f, 0.9f, 0.8f) });
            Biomes[0].Animals[2] = new AnimalData("meadow_fox", "Fox", 0, 2,
                new AnimalTraits { BodyLength = 0.6f, BodyHeight = 0.4f, NeckLength = 0.2f, LegLength = 0.5f, LegCount = 0.8f, TailLength = 0.9f, HeadSize = 0.4f, EarSize = 0.6f, HasFins = 0f, HasWings = 0f, BodyColor = new Color(0.9f, 0.5f, 0.2f), AccentColor = Color.white });

            // Biome 1: Forest
            Biomes[1] = new BiomeData(1, "Forest", new Color(0.2f, 0.4f, 0.2f), new Color(0.3f, 0.5f, 0.3f, 0.9f), 1000, 2);
            Biomes[1].Animals[0] = new AnimalData("forest_owl", "Owl", 1, 2,
                new AnimalTraits { BodyLength = 0.3f, BodyHeight = 0.5f, NeckLength = 0.1f, LegLength = 0.2f, LegCount = 0.4f, TailLength = 0.3f, HeadSize = 0.7f, EarSize = 0.4f, HasFins = 0f, HasWings = 0.9f, BodyColor = new Color(0.5f, 0.4f, 0.3f), AccentColor = new Color(0.9f, 0.8f, 0.6f) });
            Biomes[1].Animals[1] = new AnimalData("forest_bear", "Bear", 1, 3,
                new AnimalTraits { BodyLength = 0.7f, BodyHeight = 0.7f, NeckLength = 0.2f, LegLength = 0.4f, LegCount = 0.8f, TailLength = 0.1f, HeadSize = 0.5f, EarSize = 0.3f, HasFins = 0f, HasWings = 0f, BodyColor = new Color(0.3f, 0.2f, 0.15f), AccentColor = new Color(0.4f, 0.3f, 0.2f) });
            Biomes[1].Animals[2] = new AnimalData("forest_squirrel", "Squirrel", 1, 1,
                new AnimalTraits { BodyLength = 0.3f, BodyHeight = 0.3f, NeckLength = 0.1f, LegLength = 0.3f, LegCount = 0.8f, TailLength = 0.95f, HeadSize = 0.4f, EarSize = 0.4f, HasFins = 0f, HasWings = 0f, BodyColor = new Color(0.6f, 0.3f, 0.1f), AccentColor = new Color(0.9f, 0.8f, 0.7f) });

            // Biome 2: Lake
            Biomes[2] = new BiomeData(2, "Lake", new Color(0.2f, 0.4f, 0.7f), new Color(0.4f, 0.6f, 0.8f, 0.9f), 3000, 2);
            Biomes[2].Animals[0] = new AnimalData("lake_fish", "Goldfish", 2, 1,
                new AnimalTraits { BodyLength = 0.3f, BodyHeight = 0.4f, NeckLength = 0f, LegLength = 0f, LegCount = 0f, TailLength = 0.6f, HeadSize = 0.3f, EarSize = 0f, HasFins = 1f, HasWings = 0f, BodyColor = new Color(1f, 0.6f, 0.2f), AccentColor = new Color(1f, 0.8f, 0.4f) });
            Biomes[2].Animals[1] = new AnimalData("lake_frog", "Frog", 2, 1,
                new AnimalTraits { BodyLength = 0.3f, BodyHeight = 0.3f, NeckLength = 0f, LegLength = 0.6f, LegCount = 0.8f, TailLength = 0f, HeadSize = 0.5f, EarSize = 0f, HasFins = 0f, HasWings = 0f, BodyColor = new Color(0.3f, 0.7f, 0.2f), AccentColor = new Color(0.9f, 0.9f, 0.5f) });
            Biomes[2].Animals[2] = new AnimalData("lake_swan", "Swan", 2, 3,
                new AnimalTraits { BodyLength = 0.5f, BodyHeight = 0.4f, NeckLength = 0.95f, LegLength = 0.3f, LegCount = 0.4f, TailLength = 0.3f, HeadSize = 0.3f, EarSize = 0f, HasFins = 0f, HasWings = 0.7f, BodyColor = Color.white, AccentColor = new Color(1f, 0.6f, 0.2f) });

            // Biome 3: Desert
            Biomes[3] = new BiomeData(3, "Desert", new Color(0.9f, 0.8f, 0.5f), new Color(0.95f, 0.9f, 0.7f, 0.9f), 8000, 3);
            Biomes[3].Animals[0] = new AnimalData("desert_camel", "Camel", 3, 2,
                new AnimalTraits { BodyLength = 0.8f, BodyHeight = 0.7f, NeckLength = 0.7f, LegLength = 0.8f, LegCount = 0.8f, TailLength = 0.3f, HeadSize = 0.4f, EarSize = 0.3f, HasFins = 0f, HasWings = 0f, BodyColor = new Color(0.8f, 0.65f, 0.4f), AccentColor = new Color(0.6f, 0.5f, 0.3f) });
            Biomes[3].Animals[1] = new AnimalData("desert_scorpion", "Scorpion", 3, 2,
                new AnimalTraits { BodyLength = 0.5f, BodyHeight = 0.2f, NeckLength = 0f, LegLength = 0.3f, LegCount = 1f, TailLength = 0.9f, HeadSize = 0.3f, EarSize = 0f, HasFins = 0f, HasWings = 0f, BodyColor = new Color(0.3f, 0.2f, 0.1f), AccentColor = new Color(0.5f, 0.3f, 0.2f) });
            Biomes[3].Animals[2] = new AnimalData("desert_snake", "Rattlesnake", 3, 3,
                new AnimalTraits { BodyLength = 1f, BodyHeight = 0.1f, NeckLength = 0f, LegLength = 0f, LegCount = 0f, TailLength = 0.2f, HeadSize = 0.2f, EarSize = 0f, HasFins = 0f, HasWings = 0f, BodyColor = new Color(0.7f, 0.5f, 0.3f), AccentColor = new Color(0.4f, 0.3f, 0.2f) });

            // Biome 4: Tundra
            Biomes[4] = new BiomeData(4, "Frozen Tundra", new Color(0.8f, 0.9f, 1f), new Color(0.9f, 0.95f, 1f, 0.9f), 20000, 3);
            Biomes[4].Animals[0] = new AnimalData("tundra_penguin", "Penguin", 4, 2,
                new AnimalTraits { BodyLength = 0.3f, BodyHeight = 0.6f, NeckLength = 0.1f, LegLength = 0.2f, LegCount = 0.4f, TailLength = 0.1f, HeadSize = 0.4f, EarSize = 0f, HasFins = 0.3f, HasWings = 0.4f, BodyColor = new Color(0.1f, 0.1f, 0.15f), AccentColor = Color.white });
            Biomes[4].Animals[1] = new AnimalData("tundra_polarbear", "Polar Bear", 4, 3,
                new AnimalTraits { BodyLength = 0.8f, BodyHeight = 0.7f, NeckLength = 0.3f, LegLength = 0.4f, LegCount = 0.8f, TailLength = 0.1f, HeadSize = 0.5f, EarSize = 0.2f, HasFins = 0f, HasWings = 0f, BodyColor = new Color(0.95f, 0.95f, 0.9f), AccentColor = new Color(0.2f, 0.2f, 0.2f) });
            Biomes[4].Animals[2] = new AnimalData("tundra_seal", "Seal", 4, 1,
                new AnimalTraits { BodyLength = 0.7f, BodyHeight = 0.3f, NeckLength = 0.1f, LegLength = 0f, LegCount = 0f, TailLength = 0.3f, HeadSize = 0.4f, EarSize = 0f, HasFins = 0.8f, HasWings = 0f, BodyColor = new Color(0.5f, 0.5f, 0.55f), AccentColor = new Color(0.3f, 0.3f, 0.35f) });

            // Biome 5: Jungle
            Biomes[5] = new BiomeData(5, "Jungle", new Color(0.15f, 0.35f, 0.15f), new Color(0.2f, 0.5f, 0.2f, 0.9f), 50000, 4);
            Biomes[5].Animals[0] = new AnimalData("jungle_parrot", "Parrot", 5, 2,
                new AnimalTraits { BodyLength = 0.3f, BodyHeight = 0.4f, NeckLength = 0.1f, LegLength = 0.2f, LegCount = 0.4f, TailLength = 0.8f, HeadSize = 0.5f, EarSize = 0f, HasFins = 0f, HasWings = 0.9f, BodyColor = new Color(0.2f, 0.8f, 0.2f), AccentColor = new Color(1f, 0.2f, 0.2f) });
            Biomes[5].Animals[1] = new AnimalData("jungle_monkey", "Monkey", 5, 2,
                new AnimalTraits { BodyLength = 0.4f, BodyHeight = 0.5f, NeckLength = 0.1f, LegLength = 0.5f, LegCount = 0.8f, TailLength = 0.95f, HeadSize = 0.5f, EarSize = 0.4f, HasFins = 0f, HasWings = 0f, BodyColor = new Color(0.5f, 0.35f, 0.2f), AccentColor = new Color(0.9f, 0.75f, 0.6f) });
            Biomes[5].Animals[2] = new AnimalData("jungle_jaguar", "Jaguar", 5, 3,
                new AnimalTraits { BodyLength = 0.8f, BodyHeight = 0.5f, NeckLength = 0.2f, LegLength = 0.5f, LegCount = 0.8f, TailLength = 0.7f, HeadSize = 0.5f, EarSize = 0.3f, HasFins = 0f, HasWings = 0f, BodyColor = new Color(0.9f, 0.7f, 0.3f), AccentColor = new Color(0.1f, 0.1f, 0.1f) });

            // Biome 6: Mountains
            Biomes[6] = new BiomeData(6, "Mountains", new Color(0.5f, 0.5f, 0.55f), new Color(0.7f, 0.7f, 0.75f, 0.9f), 100000, 4);
            Biomes[6].Animals[0] = new AnimalData("mountain_goat", "Mountain Goat", 6, 1,
                new AnimalTraits { BodyLength = 0.6f, BodyHeight = 0.5f, NeckLength = 0.2f, LegLength = 0.6f, LegCount = 0.8f, TailLength = 0.1f, HeadSize = 0.4f, EarSize = 0.3f, HasFins = 0f, HasWings = 0f, BodyColor = new Color(0.9f, 0.9f, 0.85f), AccentColor = new Color(0.3f, 0.25f, 0.2f) });
            Biomes[6].Animals[1] = new AnimalData("mountain_eagle", "Eagle", 6, 3,
                new AnimalTraits { BodyLength = 0.5f, BodyHeight = 0.4f, NeckLength = 0.2f, LegLength = 0.3f, LegCount = 0.4f, TailLength = 0.4f, HeadSize = 0.4f, EarSize = 0f, HasFins = 0f, HasWings = 1f, BodyColor = new Color(0.4f, 0.25f, 0.1f), AccentColor = Color.white });
            Biomes[6].Animals[2] = new AnimalData("mountain_wolf", "Wolf", 6, 2,
                new AnimalTraits { BodyLength = 0.7f, BodyHeight = 0.5f, NeckLength = 0.2f, LegLength = 0.6f, LegCount = 0.8f, TailLength = 0.6f, HeadSize = 0.45f, EarSize = 0.5f, HasFins = 0f, HasWings = 0f, BodyColor = new Color(0.5f, 0.5f, 0.55f), AccentColor = new Color(0.3f, 0.3f, 0.35f) });

            // Biome 7: Ocean
            Biomes[7] = new BiomeData(7, "Deep Ocean", new Color(0.1f, 0.2f, 0.5f), new Color(0.2f, 0.3f, 0.6f, 0.9f), 200000, 5);
            Biomes[7].Animals[0] = new AnimalData("ocean_whale", "Whale", 7, 3,
                new AnimalTraits { BodyLength = 1f, BodyHeight = 0.5f, NeckLength = 0f, LegLength = 0f, LegCount = 0f, TailLength = 0.5f, HeadSize = 0.4f, EarSize = 0f, HasFins = 1f, HasWings = 0f, BodyColor = new Color(0.3f, 0.4f, 0.5f), AccentColor = new Color(0.9f, 0.9f, 0.95f) });
            Biomes[7].Animals[1] = new AnimalData("ocean_octopus", "Octopus", 7, 2,
                new AnimalTraits { BodyLength = 0.4f, BodyHeight = 0.5f, NeckLength = 0f, LegLength = 0.8f, LegCount = 1f, TailLength = 0f, HeadSize = 0.6f, EarSize = 0f, HasFins = 0f, HasWings = 0f, BodyColor = new Color(0.6f, 0.3f, 0.5f), AccentColor = new Color(0.8f, 0.5f, 0.7f) });
            Biomes[7].Animals[2] = new AnimalData("ocean_shark", "Shark", 7, 3,
                new AnimalTraits { BodyLength = 0.9f, BodyHeight = 0.4f, NeckLength = 0f, LegLength = 0f, LegCount = 0f, TailLength = 0.5f, HeadSize = 0.35f, EarSize = 0f, HasFins = 1f, HasWings = 0f, BodyColor = new Color(0.4f, 0.45f, 0.5f), AccentColor = new Color(0.9f, 0.9f, 0.9f) });

            // Biome 8: Volcano
            Biomes[8] = new BiomeData(8, "Volcano", new Color(0.3f, 0.15f, 0.1f), new Color(0.5f, 0.2f, 0.1f, 0.9f), 400000, 5);
            Biomes[8].Animals[0] = new AnimalData("volcano_phoenix", "Phoenix", 8, 3,
                new AnimalTraits { BodyLength = 0.5f, BodyHeight = 0.5f, NeckLength = 0.4f, LegLength = 0.4f, LegCount = 0.4f, TailLength = 0.9f, HeadSize = 0.4f, EarSize = 0.2f, HasFins = 0f, HasWings = 1f, BodyColor = new Color(1f, 0.4f, 0.1f), AccentColor = new Color(1f, 0.8f, 0.2f) });
            Biomes[8].Animals[1] = new AnimalData("volcano_salamander", "Fire Salamander", 8, 2,
                new AnimalTraits { BodyLength = 0.6f, BodyHeight = 0.2f, NeckLength = 0.1f, LegLength = 0.2f, LegCount = 0.8f, TailLength = 0.7f, HeadSize = 0.3f, EarSize = 0f, HasFins = 0f, HasWings = 0f, BodyColor = new Color(0.2f, 0.15f, 0.1f), AccentColor = new Color(1f, 0.5f, 0f) });
            Biomes[8].Animals[2] = new AnimalData("volcano_dragon", "Lava Dragon", 8, 3,
                new AnimalTraits { BodyLength = 0.8f, BodyHeight = 0.6f, NeckLength = 0.5f, LegLength = 0.5f, LegCount = 0.8f, TailLength = 0.8f, HeadSize = 0.5f, EarSize = 0.3f, HasFins = 0f, HasWings = 0.9f, BodyColor = new Color(0.5f, 0.1f, 0.05f), AccentColor = new Color(1f, 0.3f, 0f) });

            // Biome 9: Space (final)
            Biomes[9] = new BiomeData(9, "Space Station", new Color(0.05f, 0.05f, 0.15f), new Color(0.1f, 0.1f, 0.25f, 0.9f), 750000, 6);
            Biomes[9].Animals[0] = new AnimalData("space_alien", "Space Blob", 9, 2,
                new AnimalTraits { BodyLength = 0.5f, BodyHeight = 0.6f, NeckLength = 0.1f, LegLength = 0.1f, LegCount = 0.2f, TailLength = 0.3f, HeadSize = 0.7f, EarSize = 0.5f, HasFins = 0f, HasWings = 0f, BodyColor = new Color(0.4f, 0.9f, 0.5f), AccentColor = new Color(0.2f, 0.5f, 0.3f) });
            Biomes[9].Animals[1] = new AnimalData("space_starfish", "Cosmic Starfish", 9, 2,
                new AnimalTraits { BodyLength = 0.4f, BodyHeight = 0.4f, NeckLength = 0f, LegLength = 0.6f, LegCount = 1f, TailLength = 0f, HeadSize = 0.3f, EarSize = 0f, HasFins = 0f, HasWings = 0f, BodyColor = new Color(0.9f, 0.7f, 1f), AccentColor = new Color(1f, 0.9f, 0.5f) });
            Biomes[9].Animals[2] = new AnimalData("space_wyrm", "Void Wyrm", 9, 3,
                new AnimalTraits { BodyLength = 1f, BodyHeight = 0.3f, NeckLength = 0.3f, LegLength = 0f, LegCount = 0f, TailLength = 0.6f, HeadSize = 0.4f, EarSize = 0.2f, HasFins = 0.5f, HasWings = 0.3f, BodyColor = new Color(0.2f, 0.1f, 0.3f), AccentColor = new Color(0.6f, 0.3f, 0.9f) });
        }
    }
}
