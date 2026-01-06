using System.Collections.Generic;
using UnityEngine;

namespace AdvancedCreatureDigseum
{
    public enum PrestigeUpgradeType
    {
        IncomeBonus,        // +X% income from zoo
        DigBonus,           // +X% dig power effectiveness
        StartBiome,         // Start with biome X unlocked
        CurrencyBonus,      // +X% prestige currency drops
        UnlockDecoration,   // Permanently unlock a decoration
        UnlockPasture,      // Permanently unlock a pasture
        StartingGold,       // +X starting gold after prestige
        EnergyBonus         // +X% max energy
    }

    [System.Serializable]
    public class PrestigeUpgrade
    {
        public string Id;
        public string Name;
        public string Description;
        public PrestigeUpgradeType Type;
        public int Cost;
        public float Value; // The bonus amount
        public string UnlockItem; // For decoration/pasture unlocks
        public string[] Prerequisites; // Required upgrades before this one

        public PrestigeUpgrade(string id, string name, string desc, PrestigeUpgradeType type, int cost, float value = 0, string unlockItem = "", string[] prereqs = null)
        {
            Id = id;
            Name = name;
            Description = desc;
            Type = type;
            Cost = cost;
            Value = value;
            UnlockItem = unlockItem;
            Prerequisites = prereqs ?? new string[0];
        }
    }

    public static class PrestigeDatabase
    {
        public static List<PrestigeUpgrade> Upgrades;
        private static bool initialized = false;

        static PrestigeDatabase()
        {
            InitializeUpgrades();
        }

        public static void EnsureInitialized()
        {
            if (!initialized || Upgrades == null || Upgrades.Count == 0)
            {
                InitializeUpgrades();
            }
        }

        static void InitializeUpgrades()
        {
            Upgrades = new List<PrestigeUpgrade>
            {
                // === INCOME BONUSES ===
                new PrestigeUpgrade("income_1", "Zoo Marketing I", "+10% zoo income", PrestigeUpgradeType.IncomeBonus, 5, 10f),
                new PrestigeUpgrade("income_2", "Zoo Marketing II", "+15% zoo income", PrestigeUpgradeType.IncomeBonus, 15, 15f, "", new[] { "income_1" }),
                new PrestigeUpgrade("income_3", "Zoo Marketing III", "+25% zoo income", PrestigeUpgradeType.IncomeBonus, 40, 25f, "", new[] { "income_2" }),
                new PrestigeUpgrade("income_4", "Zoo Empire", "+50% zoo income", PrestigeUpgradeType.IncomeBonus, 100, 50f, "", new[] { "income_3" }),

                // === DIG BONUSES ===
                new PrestigeUpgrade("dig_1", "Better Tools I", "+20% dig effectiveness", PrestigeUpgradeType.DigBonus, 5, 20f),
                new PrestigeUpgrade("dig_2", "Better Tools II", "+30% dig effectiveness", PrestigeUpgradeType.DigBonus, 15, 30f, "", new[] { "dig_1" }),
                new PrestigeUpgrade("dig_3", "Master Excavator", "+50% dig effectiveness", PrestigeUpgradeType.DigBonus, 50, 50f, "", new[] { "dig_2" }),

                // === STARTING BIOMES ===
                new PrestigeUpgrade("biome_1", "Head Start I", "Start with Biome 2 unlocked", PrestigeUpgradeType.StartBiome, 10, 1f),
                new PrestigeUpgrade("biome_2", "Head Start II", "Start with Biome 3 unlocked", PrestigeUpgradeType.StartBiome, 25, 2f, "", new[] { "biome_1" }),
                new PrestigeUpgrade("biome_3", "Head Start III", "Start with Biome 4 unlocked", PrestigeUpgradeType.StartBiome, 60, 3f, "", new[] { "biome_2" }),

                // === PRESTIGE CURRENCY BONUSES ===
                new PrestigeUpgrade("currency_1", "Crystal Detector I", "+25% prestige crystals", PrestigeUpgradeType.CurrencyBonus, 8, 25f),
                new PrestigeUpgrade("currency_2", "Crystal Detector II", "+50% prestige crystals", PrestigeUpgradeType.CurrencyBonus, 25, 50f, "", new[] { "currency_1" }),
                new PrestigeUpgrade("currency_3", "Crystal Magnet", "+100% prestige crystals", PrestigeUpgradeType.CurrencyBonus, 75, 100f, "", new[] { "currency_2" }),

                // === STARTING GOLD ===
                new PrestigeUpgrade("gold_1", "Savings I", "+200 starting gold", PrestigeUpgradeType.StartingGold, 3, 200f),
                new PrestigeUpgrade("gold_2", "Savings II", "+500 starting gold", PrestigeUpgradeType.StartingGold, 10, 500f, "", new[] { "gold_1" }),
                new PrestigeUpgrade("gold_3", "Trust Fund", "+1000 starting gold", PrestigeUpgradeType.StartingGold, 30, 1000f, "", new[] { "gold_2" }),

                // === ENERGY BONUSES ===
                new PrestigeUpgrade("energy_1", "Endurance I", "+25% max energy", PrestigeUpgradeType.EnergyBonus, 6, 25f),
                new PrestigeUpgrade("energy_2", "Endurance II", "+50% max energy", PrestigeUpgradeType.EnergyBonus, 20, 50f, "", new[] { "energy_1" }),

                // === DECORATION UNLOCKS (Permanent) ===
                new PrestigeUpgrade("prestige_unlock_bench", "Permanent Bench", "Bench unlocked after prestige", PrestigeUpgradeType.UnlockDecoration, 4, 0, "Bench"),
                new PrestigeUpgrade("prestige_unlock_lamp", "Permanent Lamp", "Lamp unlocked after prestige", PrestigeUpgradeType.UnlockDecoration, 6, 0, "Lamp"),
                new PrestigeUpgrade("prestige_unlock_tree", "Permanent Tree", "Tree unlocked after prestige", PrestigeUpgradeType.UnlockDecoration, 10, 0, "Tree"),
                new PrestigeUpgrade("prestige_unlock_flowers", "Permanent Flowers", "Flowers unlocked after prestige", PrestigeUpgradeType.UnlockDecoration, 15, 0, "Flowers"),
                new PrestigeUpgrade("prestige_unlock_fountain", "Permanent Fountain", "Fountain unlocked after prestige", PrestigeUpgradeType.UnlockDecoration, 30, 0, "Fountain"),
                new PrestigeUpgrade("prestige_unlock_statue", "Permanent Statue", "Statue unlocked after prestige", PrestigeUpgradeType.UnlockDecoration, 50, 0, "Statue"),

                // === PASTURE UNLOCKS (Permanent) ===
                new PrestigeUpgrade("prestige_unlock_grass_pasture", "Permanent Grass Pasture", "Grass Pasture unlocked after prestige", PrestigeUpgradeType.UnlockPasture, 20, 0, "GrassPasture"),
                new PrestigeUpgrade("prestige_unlock_water_pasture", "Permanent Water Pasture", "Water Pasture unlocked after prestige", PrestigeUpgradeType.UnlockPasture, 45, 0, "WaterPasture"),
                new PrestigeUpgrade("prestige_unlock_luxury_pasture", "Permanent Luxury Pasture", "Luxury Pasture unlocked after prestige", PrestigeUpgradeType.UnlockPasture, 80, 0, "LuxuryPasture"),
            };
            initialized = true;
        }

        public static PrestigeUpgrade GetUpgrade(string id)
        {
            EnsureInitialized();
            return Upgrades.Find(u => u.Id == id);
        }

        public static bool IsUpgradePurchased(string id)
        {
            return GameData.PrestigeUpgrades.Contains(id);
        }

        public static bool CanPurchaseUpgrade(string id)
        {
            if (IsUpgradePurchased(id)) return false;

            var upgrade = GetUpgrade(id);
            if (upgrade == null) return false;

            // Check prerequisites
            foreach (var prereq in upgrade.Prerequisites)
            {
                if (!IsUpgradePurchased(prereq)) return false;
            }

            // Check cost
            return GameData.PrestigePoints >= upgrade.Cost;
        }

        public static bool PurchaseUpgrade(string id)
        {
            if (!CanPurchaseUpgrade(id)) return false;

            var upgrade = GetUpgrade(id);
            if (upgrade == null) return false;

            GameData.PrestigePoints -= upgrade.Cost;
            GameData.PrestigeUpgrades.Add(id);

            // Apply the upgrade effect
            ApplyUpgrade(upgrade);

            GameData.SaveGame();
            return true;
        }

        public static void ApplyUpgrade(PrestigeUpgrade upgrade)
        {
            switch (upgrade.Type)
            {
                case PrestigeUpgradeType.IncomeBonus:
                    GameData.PrestigeIncomeBonus += upgrade.Value;
                    break;
                case PrestigeUpgradeType.DigBonus:
                    GameData.PrestigeDigBonus += upgrade.Value;
                    break;
                case PrestigeUpgradeType.StartBiome:
                    GameData.PrestigeStartBiome = Mathf.Max(GameData.PrestigeStartBiome, (int)upgrade.Value);
                    break;
                case PrestigeUpgradeType.CurrencyBonus:
                    GameData.PrestigeCurrencyBonus += upgrade.Value;
                    break;
                case PrestigeUpgradeType.UnlockDecoration:
                    // Decoration unlocks are handled in DoPrestige()
                    break;
                case PrestigeUpgradeType.UnlockPasture:
                    // Pasture unlocks are handled in DoPrestige()
                    break;
                case PrestigeUpgradeType.StartingGold:
                    // Starting gold is calculated in DoPrestige()
                    break;
                case PrestigeUpgradeType.EnergyBonus:
                    // Energy bonus is applied when calculating max energy
                    break;
            }
        }

        // Recalculate all bonuses from purchased upgrades (called on load)
        public static void RecalculateBonuses()
        {
            EnsureInitialized();

            GameData.PrestigeIncomeBonus = 0f;
            GameData.PrestigeDigBonus = 0f;
            GameData.PrestigeStartBiome = 0;
            GameData.PrestigeCurrencyBonus = 0f;

            foreach (var upgradeId in GameData.PrestigeUpgrades)
            {
                var upgrade = GetUpgrade(upgradeId);
                if (upgrade != null)
                {
                    ApplyUpgrade(upgrade);
                }
            }
        }

        // Get total starting gold bonus from prestige upgrades
        public static int GetStartingGoldBonus()
        {
            EnsureInitialized();
            int bonus = 0;
            foreach (var upgradeId in GameData.PrestigeUpgrades)
            {
                var upgrade = GetUpgrade(upgradeId);
                if (upgrade != null && upgrade.Type == PrestigeUpgradeType.StartingGold)
                {
                    bonus += (int)upgrade.Value;
                }
            }
            return bonus;
        }

        // Get total energy bonus percentage from prestige upgrades
        public static float GetEnergyBonusPercent()
        {
            EnsureInitialized();
            float bonus = 0f;
            foreach (var upgradeId in GameData.PrestigeUpgrades)
            {
                var upgrade = GetUpgrade(upgradeId);
                if (upgrade != null && upgrade.Type == PrestigeUpgradeType.EnergyBonus)
                {
                    bonus += upgrade.Value;
                }
            }
            return bonus;
        }

        // Get all upgrades (useful for iteration)
        public static List<PrestigeUpgrade> GetAllUpgrades()
        {
            EnsureInitialized();
            return Upgrades;
        }
    }
}
