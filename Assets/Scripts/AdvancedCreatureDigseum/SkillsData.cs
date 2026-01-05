using System.Collections.Generic;
using UnityEngine;

namespace AdvancedCreatureDigseum
{
    public enum SkillType
    {
        DigPower,
        DigRadius,
        MaxEnergy,
        EnergyRegen,
        UnlockDecoration,
        UnlockPasture,
        FinishGame
    }

    [System.Serializable]
    public class SkillUpgrade
    {
        public string Id;
        public string Name;
        public string Description;
        public SkillType Type;
        public int Cost;
        public int MaxLevel;
        public string UnlockItem; // For decoration/pasture unlocks
        public string Category; // For grouping tiered upgrades
        public int Tier; // 1, 2, 3, etc.

        public SkillUpgrade(string id, string name, string desc, SkillType type, int cost, int maxLevel = 5, string unlockItem = "", string category = "", int tier = 1)
        {
            Id = id;
            Name = name;
            Description = desc;
            Type = type;
            Cost = cost;
            MaxLevel = maxLevel;
            UnlockItem = unlockItem;
            Category = string.IsNullOrEmpty(category) ? id : category;
            Tier = tier;
        }
    }

    public static class SkillDatabase
    {
        public static List<SkillUpgrade> Skills;

        static SkillDatabase()
        {
            InitializeSkills();
        }

        static void InitializeSkills()
        {
            // Economy balanced for 2-3 hour gameplay with exponential progression
            Skills = new List<SkillUpgrade>
            {
                // Dig upgrades - essential for progression, scales with game (tiered)
                new SkillUpgrade("dig_power_1", "Dig Power", "Reveal more per click", SkillType.DigPower, 500, 1, "", "dig_power", 1),
                new SkillUpgrade("dig_power_2", "Dig Power", "Reveal even more per click", SkillType.DigPower, 2500, 1, "", "dig_power", 2),
                new SkillUpgrade("dig_power_3", "Dig Power", "Maximum dig power", SkillType.DigPower, 15000, 1, "", "dig_power", 3),
                new SkillUpgrade("dig_power_4", "Dig Power", "Expert explorer", SkillType.DigPower, 75000, 1, "", "dig_power", 4),
                new SkillUpgrade("dig_power_5", "Dig Power", "Master explorer", SkillType.DigPower, 300000, 1, "", "dig_power", 5),

                new SkillUpgrade("dig_radius_1", "Wide Search", "Increase search radius", SkillType.DigRadius, 1000, 1, "", "dig_radius", 1),
                new SkillUpgrade("dig_radius_2", "Wide Search", "Larger search area", SkillType.DigRadius, 8000, 1, "", "dig_radius", 2),
                new SkillUpgrade("dig_radius_3", "Wide Search", "Massive search area", SkillType.DigRadius, 50000, 1, "", "dig_radius", 3),

                new SkillUpgrade("energy_max_1", "Energy Tank", "+50 max energy", SkillType.MaxEnergy, 750, 1, "", "energy_max", 1),
                new SkillUpgrade("energy_max_2", "Energy Tank", "+100 max energy", SkillType.MaxEnergy, 4000, 1, "", "energy_max", 2),
                new SkillUpgrade("energy_max_3", "Energy Tank", "+150 max energy", SkillType.MaxEnergy, 20000, 1, "", "energy_max", 3),
                new SkillUpgrade("energy_max_4", "Energy Tank", "+200 max energy", SkillType.MaxEnergy, 100000, 1, "", "energy_max", 4),

                new SkillUpgrade("energy_regen_1", "Quick Recovery", "Faster energy regen", SkillType.EnergyRegen, 1500, 1, "", "energy_regen", 1),
                new SkillUpgrade("energy_regen_2", "Quick Recovery", "Even faster regen", SkillType.EnergyRegen, 12000, 1, "", "energy_regen", 2),
                new SkillUpgrade("energy_regen_3", "Quick Recovery", "Maximum regen speed", SkillType.EnergyRegen, 60000, 1, "", "energy_regen", 3),

                // Decoration unlocks - early to mid game purchases
                new SkillUpgrade("unlock_bench", "Unlock Bench", "Benches for visitors (+1 income)", SkillType.UnlockDecoration, 800, 1, "Bench"),
                new SkillUpgrade("unlock_lamp", "Unlock Lamp", "Light up your zoo (+1 income)", SkillType.UnlockDecoration, 1200, 1, "Lamp"),
                new SkillUpgrade("unlock_tree", "Unlock Tree", "Nature in your zoo (+2 income)", SkillType.UnlockDecoration, 2000, 1, "Tree"),
                new SkillUpgrade("unlock_flowers", "Unlock Flowers", "Colorful flowers (+2 income)", SkillType.UnlockDecoration, 3000, 1, "Flowers"),
                new SkillUpgrade("unlock_fountain", "Unlock Fountain", "Beautiful water feature (+5 income)", SkillType.UnlockDecoration, 10000, 1, "Fountain"),
                new SkillUpgrade("unlock_statue", "Unlock Statue", "Impress visitors (+10 income)", SkillType.UnlockDecoration, 35000, 1, "Statue"),

                // Pasture unlocks - mid to late game, key for income scaling
                new SkillUpgrade("unlock_grass_pasture", "Grass Pasture", "+20% income bonus", SkillType.UnlockPasture, 5000, 1, "GrassPasture"),
                new SkillUpgrade("unlock_water_pasture", "Water Pasture", "+30% income bonus", SkillType.UnlockPasture, 25000, 1, "WaterPasture"),
                new SkillUpgrade("unlock_luxury_pasture", "Luxury Pasture", "+50% income bonus", SkillType.UnlockPasture, 150000, 1, "LuxuryPasture"),

                // Final skill - requires finding all 30 animals + large gold investment
                new SkillUpgrade("finish_game", "Finish the Game!", "Complete your adventure", SkillType.FinishGame, 1000000, 1)
            };
        }

        // Get current level for a tiered category
        public static int GetCategoryLevel(string category)
        {
            int level = 0;
            foreach (var skill in Skills)
            {
                if (skill.Category == category && PurchasedSkills.Contains(skill.Id))
                {
                    level = Mathf.Max(level, skill.Tier);
                }
            }
            return level;
        }

        // Get max level for a category
        public static int GetCategoryMaxLevel(string category)
        {
            int max = 0;
            foreach (var skill in Skills)
            {
                if (skill.Category == category)
                {
                    max = Mathf.Max(max, skill.Tier);
                }
            }
            return max;
        }

        // Get next skill to purchase in a category
        public static SkillUpgrade GetNextSkillInCategory(string category)
        {
            int currentLevel = GetCategoryLevel(category);
            foreach (var skill in Skills)
            {
                if (skill.Category == category && skill.Tier == currentLevel + 1)
                {
                    return skill;
                }
            }
            return null; // Max level reached
        }

        // Get all unique categories (for tiered skills only)
        public static List<string> GetTieredCategories()
        {
            HashSet<string> categories = new HashSet<string>();
            foreach (var skill in Skills)
            {
                // Only include categories with multiple tiers
                if (skill.Tier > 0 && (skill.Type == SkillType.DigPower || skill.Type == SkillType.DigRadius ||
                    skill.Type == SkillType.MaxEnergy || skill.Type == SkillType.EnergyRegen))
                {
                    categories.Add(skill.Category);
                }
            }
            return new List<string>(categories);
        }

        // Get non-tiered skills (decorations, pastures, finish)
        public static List<SkillUpgrade> GetNonTieredSkills()
        {
            List<SkillUpgrade> result = new List<SkillUpgrade>();
            foreach (var skill in Skills)
            {
                if (skill.Type == SkillType.UnlockDecoration || skill.Type == SkillType.UnlockPasture ||
                    skill.Type == SkillType.FinishGame)
                {
                    result.Add(skill);
                }
            }
            return result;
        }

        // Track purchased skills
        public static HashSet<string> PurchasedSkills = new HashSet<string>();

        public static bool IsSkillPurchased(string skillId)
        {
            return PurchasedSkills.Contains(skillId);
        }

        public static bool CanPurchaseSkill(string skillId)
        {
            if (PurchasedSkills.Contains(skillId)) return false;

            var skill = Skills.Find(s => s.Id == skillId);
            if (skill == null) return false;

            // Check if finish game skill - requires all animals found
            if (skill.Type == SkillType.FinishGame)
            {
                // Need at least one of each animal
                int totalAnimals = 0;
                foreach (var biome in BiomeDatabase.Biomes)
                {
                    foreach (var animal in biome.Animals)
                    {
                        if (GameData.FoundAnimals.ContainsKey(animal.Id))
                            totalAnimals++;
                    }
                }
                if (totalAnimals < 30) return false;
            }

            return GameData.Gold >= skill.Cost;
        }

        public static bool PurchaseSkill(string skillId)
        {
            if (!CanPurchaseSkill(skillId)) return false;

            var skill = Skills.Find(s => s.Id == skillId);
            if (skill == null) return false;

            GameData.Gold -= skill.Cost;
            PurchasedSkills.Add(skillId);

            // Apply skill effect
            switch (skill.Type)
            {
                case SkillType.DigPower:
                    GameData.DigPower++;
                    break;
                case SkillType.DigRadius:
                    GameData.DigRadius++;
                    break;
                case SkillType.MaxEnergy:
                    GameData.MaxEnergy += 50;
                    break;
                case SkillType.EnergyRegen:
                    GameData.EnergyRegen += 1f;
                    break;
                case SkillType.UnlockDecoration:
                    GameData.UnlockedDecorations.Add(skill.UnlockItem);
                    break;
                case SkillType.UnlockPasture:
                    GameData.UnlockedPastures.Add(skill.UnlockItem);
                    break;
                case SkillType.FinishGame:
                    GameData.GameFinished = true;
                    break;
            }

            return true;
        }
    }
}
