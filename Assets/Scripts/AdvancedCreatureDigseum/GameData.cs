using UnityEngine;
using System.Collections.Generic;

namespace AdvancedCreatureDigseum
{
    // Persistent game data that survives scene changes
    public static class GameData
    {
        // Currency
        public static int Gold = 100;

        // Found animals: Dictionary<animalId, timesFound>
        public static Dictionary<string, int> FoundAnimals = new Dictionary<string, int>();

        // Created hybrids
        public static List<HybridData> Hybrids = new List<HybridData>();

        // Zoo placed hybrids
        public static List<PlacedHybrid> PlacedHybrids = new List<PlacedHybrid>();

        // Zoo decorations
        public static List<PlacedDecoration> PlacedDecorations = new List<PlacedDecoration>();

        // Unlocked biomes (0 is always unlocked)
        public static HashSet<int> UnlockedBiomes = new HashSet<int>() { 0 };

        // Skills
        public static int DigPower = 1;          // How much fog cleared per click
        public static int DigRadius = 1;         // Radius of fog clear
        public static int MaxEnergy = 100;       // Max energy for digging
        public static float EnergyRegen = 1f;    // Energy regen per second
        public static float CurrentEnergy = 100f;

        // Unlocked decorations
        public static HashSet<string> UnlockedDecorations = new HashSet<string>() { "Fence", "Path" };

        // Unlocked pastures
        public static HashSet<string> UnlockedPastures = new HashSet<string>() { "BasicPasture" };

        // Game completion
        public static bool GameFinished = false;

        // Idle income tracking
        public static float IdleIncomeTimer = 0f;
        public static float IdleIncomeInterval = 2f;
        public static long LastIncomeTimestamp = 0; // Unix timestamp of last income collection

        // Save tracking
        private static bool initialized = false;

        public static void EnsureLoaded()
        {
            if (!initialized)
            {
                LoadGame();
                initialized = true;
                // Process any accumulated offline income
                ProcessOfflineIncome();
            }
        }

        // Process income accumulated while player was away or in other scenes
        public static void ProcessOfflineIncome()
        {
            if (LastIncomeTimestamp == 0)
            {
                // First time - just set the timestamp
                LastIncomeTimestamp = GetCurrentTimestamp();
                return;
            }

            long currentTime = GetCurrentTimestamp();
            long secondsPassed = currentTime - LastIncomeTimestamp;

            if (secondsPassed > 0)
            {
                int incomePerInterval = CalculateTotalIdleIncome();
                if (incomePerInterval > 0)
                {
                    // Calculate how many intervals have passed
                    int intervalsPassed = (int)(secondsPassed / IdleIncomeInterval);
                    // Cap at 30 minutes worth (900 intervals at 2s each)
                    intervalsPassed = Mathf.Min(intervalsPassed, 900);

                    if (intervalsPassed > 0)
                    {
                        int totalIncome = incomePerInterval * intervalsPassed;
                        Gold += totalIncome;
                        Debug.Log($"[ACD] Collected offline income: {totalIncome}g ({intervalsPassed} intervals)");
                    }
                }
            }

            LastIncomeTimestamp = currentTime;
        }

        // Call this periodically in any scene to update income
        public static void UpdateGlobalIdleIncome()
        {
            IdleIncomeTimer += Time.deltaTime;
            if (IdleIncomeTimer >= IdleIncomeInterval)
            {
                IdleIncomeTimer = 0f;
                int income = CalculateTotalIdleIncome();
                if (income > 0)
                {
                    Gold += income;
                }
                LastIncomeTimestamp = GetCurrentTimestamp();
            }
        }

        static long GetCurrentTimestamp()
        {
            return System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        public static void Reset()
        {
            Gold = 100;
            FoundAnimals.Clear();
            Hybrids.Clear();
            PlacedHybrids.Clear();
            PlacedDecorations.Clear();
            UnlockedBiomes = new HashSet<int>() { 0 };
            DigPower = 1;
            DigRadius = 1;
            MaxEnergy = 100;
            EnergyRegen = 1f;
            CurrentEnergy = 100f;
            UnlockedDecorations = new HashSet<string>() { "Fence", "Path" };
            UnlockedPastures = new HashSet<string>() { "BasicPasture" };
            GameFinished = false;
            SkillDatabase.PurchasedSkills.Clear(); // Clear purchased skills on reset
            LastIncomeTimestamp = GetCurrentTimestamp(); // Reset timestamp
            SaveGame();
        }

        // ===== SAVE/LOAD SYSTEM =====
        public static void SaveGame()
        {
            PlayerPrefs.SetInt("ACD_Gold", Gold);
            PlayerPrefs.SetInt("ACD_DigPower", DigPower);
            PlayerPrefs.SetInt("ACD_DigRadius", DigRadius);
            PlayerPrefs.SetInt("ACD_MaxEnergy", MaxEnergy);
            PlayerPrefs.SetFloat("ACD_EnergyRegen", EnergyRegen);
            PlayerPrefs.SetFloat("ACD_CurrentEnergy", CurrentEnergy);
            PlayerPrefs.SetInt("ACD_GameFinished", GameFinished ? 1 : 0);

            // Save found animals
            string foundAnimalsJson = DictToJson(FoundAnimals);
            PlayerPrefs.SetString("ACD_FoundAnimals", foundAnimalsJson);

            // Save unlocked biomes
            string biomesJson = SetToJson(UnlockedBiomes);
            PlayerPrefs.SetString("ACD_UnlockedBiomes", biomesJson);

            // Save unlocked decorations
            string decosJson = StringSetToJson(UnlockedDecorations);
            PlayerPrefs.SetString("ACD_UnlockedDecorations", decosJson);

            // Save unlocked pastures
            string pasturesJson = StringSetToJson(UnlockedPastures);
            PlayerPrefs.SetString("ACD_UnlockedPastures", pasturesJson);

            // Save hybrids
            string hybridsJson = HybridsToJson(Hybrids);
            PlayerPrefs.SetString("ACD_Hybrids", hybridsJson);

            // Save placed hybrids
            string placedJson = PlacedHybridsToJson(PlacedHybrids);
            PlayerPrefs.SetString("ACD_PlacedHybrids", placedJson);

            // Save decorations
            string placedDecosJson = PlacedDecorationsToJson(PlacedDecorations);
            PlayerPrefs.SetString("ACD_PlacedDecorations", placedDecosJson);

            // Save purchased skills
            string skillsJson = StringSetToJson(SkillDatabase.PurchasedSkills);
            PlayerPrefs.SetString("ACD_PurchasedSkills", skillsJson);

            // Save last income timestamp
            PlayerPrefs.SetString("ACD_LastIncomeTimestamp", LastIncomeTimestamp.ToString());

            PlayerPrefs.SetInt("ACD_HasSave", 1);
            PlayerPrefs.Save();
            Debug.Log("[ACD] Game saved!");
        }

        public static void LoadGame()
        {
            if (!PlayerPrefs.HasKey("ACD_HasSave"))
            {
                Debug.Log("[ACD] No save found, starting fresh.");
                return;
            }

            Gold = PlayerPrefs.GetInt("ACD_Gold", 100);
            DigPower = PlayerPrefs.GetInt("ACD_DigPower", 1);
            DigRadius = PlayerPrefs.GetInt("ACD_DigRadius", 1);
            MaxEnergy = PlayerPrefs.GetInt("ACD_MaxEnergy", 100);
            EnergyRegen = PlayerPrefs.GetFloat("ACD_EnergyRegen", 1f);
            CurrentEnergy = PlayerPrefs.GetFloat("ACD_CurrentEnergy", 100f);
            GameFinished = PlayerPrefs.GetInt("ACD_GameFinished", 0) == 1;

            // Load found animals
            string foundAnimalsJson = PlayerPrefs.GetString("ACD_FoundAnimals", "");
            FoundAnimals = JsonToDict(foundAnimalsJson);

            // Load unlocked biomes
            string biomesJson = PlayerPrefs.GetString("ACD_UnlockedBiomes", "");
            UnlockedBiomes = JsonToSet(biomesJson);
            if (UnlockedBiomes.Count == 0) UnlockedBiomes.Add(0);

            // Load unlocked decorations
            string decosJson = PlayerPrefs.GetString("ACD_UnlockedDecorations", "");
            UnlockedDecorations = JsonToStringSet(decosJson);
            if (UnlockedDecorations.Count == 0) { UnlockedDecorations.Add("Fence"); UnlockedDecorations.Add("Path"); }

            // Load unlocked pastures
            string pasturesJson = PlayerPrefs.GetString("ACD_UnlockedPastures", "");
            UnlockedPastures = JsonToStringSet(pasturesJson);
            if (UnlockedPastures.Count == 0) UnlockedPastures.Add("BasicPasture");

            // Load hybrids
            string hybridsJson = PlayerPrefs.GetString("ACD_Hybrids", "");
            Hybrids = JsonToHybrids(hybridsJson);

            // Load placed hybrids
            string placedJson = PlayerPrefs.GetString("ACD_PlacedHybrids", "");
            PlacedHybrids = JsonToPlacedHybrids(placedJson);

            // Load decorations
            string placedDecosJson = PlayerPrefs.GetString("ACD_PlacedDecorations", "");
            PlacedDecorations = JsonToPlacedDecorations(placedDecosJson);

            // Load purchased skills
            string skillsJson = PlayerPrefs.GetString("ACD_PurchasedSkills", "");
            SkillDatabase.PurchasedSkills = JsonToStringSet(skillsJson);

            // Load last income timestamp
            string timestampStr = PlayerPrefs.GetString("ACD_LastIncomeTimestamp", "0");
            long.TryParse(timestampStr, out LastIncomeTimestamp);

            Debug.Log("[ACD] Game loaded!");
        }

        // Simple JSON helpers (avoiding Unity's JsonUtility limitations with dictionaries)
        static string DictToJson(Dictionary<string, int> dict)
        {
            List<string> entries = new List<string>();
            foreach (var kvp in dict)
                entries.Add($"{kvp.Key}:{kvp.Value}");
            return string.Join("|", entries);
        }

        static Dictionary<string, int> JsonToDict(string json)
        {
            var dict = new Dictionary<string, int>();
            if (string.IsNullOrEmpty(json)) return dict;
            string[] entries = json.Split('|');
            foreach (var entry in entries)
            {
                if (string.IsNullOrEmpty(entry)) continue;
                string[] parts = entry.Split(':');
                if (parts.Length == 2 && int.TryParse(parts[1], out int val))
                    dict[parts[0]] = val;
            }
            return dict;
        }

        static string SetToJson(HashSet<int> set)
        {
            List<string> entries = new List<string>();
            foreach (var i in set)
                entries.Add(i.ToString());
            return string.Join(",", entries);
        }

        static HashSet<int> JsonToSet(string json)
        {
            var set = new HashSet<int>();
            if (string.IsNullOrEmpty(json)) return set;
            string[] entries = json.Split(',');
            foreach (var entry in entries)
            {
                if (int.TryParse(entry, out int val))
                    set.Add(val);
            }
            return set;
        }

        static string StringSetToJson(HashSet<string> set)
        {
            return string.Join(",", set);
        }

        static HashSet<string> JsonToStringSet(string json)
        {
            var set = new HashSet<string>();
            if (string.IsNullOrEmpty(json)) return set;
            string[] entries = json.Split(',');
            foreach (var entry in entries)
            {
                if (!string.IsNullOrEmpty(entry))
                    set.Add(entry);
            }
            return set;
        }

        static string HybridsToJson(List<HybridData> hybrids)
        {
            List<string> entries = new List<string>();
            foreach (var h in hybrids)
            {
                // Format: Id;Name;Parent1Id;Parent2Id;BaseValue;Traits
                // Traits: BodyLength,BodyHeight,NeckLength,LegLength,LegCount,TailLength,HeadSize,EarSize,HasFins,HasWings,BodyColor.rgb,AccentColor.rgb
                string traits = $"{h.MixedTraits.BodyLength},{h.MixedTraits.BodyHeight},{h.MixedTraits.NeckLength},{h.MixedTraits.LegLength},{h.MixedTraits.LegCount},{h.MixedTraits.TailLength},{h.MixedTraits.HeadSize},{h.MixedTraits.EarSize},{h.MixedTraits.HasFins},{h.MixedTraits.HasWings},{h.MixedTraits.BodyColor.r},{h.MixedTraits.BodyColor.g},{h.MixedTraits.BodyColor.b},{h.MixedTraits.AccentColor.r},{h.MixedTraits.AccentColor.g},{h.MixedTraits.AccentColor.b}";
                entries.Add($"{h.Id};{h.Name};{h.Parent1Id};{h.Parent2Id};{h.BaseValue};{traits}");
            }
            return string.Join("|", entries);
        }

        static List<HybridData> JsonToHybrids(string json)
        {
            var list = new List<HybridData>();
            if (string.IsNullOrEmpty(json)) return list;
            string[] entries = json.Split('|');
            foreach (var entry in entries)
            {
                if (string.IsNullOrEmpty(entry)) continue;
                string[] parts = entry.Split(';');
                if (parts.Length >= 6)
                {
                    var h = new HybridData();
                    h.Id = parts[0];
                    h.Name = parts[1];
                    h.Parent1Id = parts[2];
                    h.Parent2Id = parts[3];
                    int.TryParse(parts[4], out h.BaseValue);

                    string[] traitParts = parts[5].Split(',');
                    if (traitParts.Length >= 16)
                    {
                        h.MixedTraits = new AnimalTraits();
                        float.TryParse(traitParts[0], out h.MixedTraits.BodyLength);
                        float.TryParse(traitParts[1], out h.MixedTraits.BodyHeight);
                        float.TryParse(traitParts[2], out h.MixedTraits.NeckLength);
                        float.TryParse(traitParts[3], out h.MixedTraits.LegLength);
                        float.TryParse(traitParts[4], out h.MixedTraits.LegCount);
                        float.TryParse(traitParts[5], out h.MixedTraits.TailLength);
                        float.TryParse(traitParts[6], out h.MixedTraits.HeadSize);
                        float.TryParse(traitParts[7], out h.MixedTraits.EarSize);
                        float.TryParse(traitParts[8], out h.MixedTraits.HasFins);
                        float.TryParse(traitParts[9], out h.MixedTraits.HasWings);
                        float r1, g1, b1, r2, g2, b2;
                        float.TryParse(traitParts[10], out r1);
                        float.TryParse(traitParts[11], out g1);
                        float.TryParse(traitParts[12], out b1);
                        float.TryParse(traitParts[13], out r2);
                        float.TryParse(traitParts[14], out g2);
                        float.TryParse(traitParts[15], out b2);
                        h.MixedTraits.BodyColor = new Color(r1, g1, b1);
                        h.MixedTraits.AccentColor = new Color(r2, g2, b2);
                    }
                    list.Add(h);
                }
            }
            return list;
        }

        static string PlacedHybridsToJson(List<PlacedHybrid> placed)
        {
            List<string> entries = new List<string>();
            foreach (var p in placed)
            {
                // Format: HybridIds(comma-sep);x,y;PastureType;Capacity;Style
                string hybridIdsStr = string.Join("~", p.HybridIds);
                entries.Add($"{hybridIdsStr};{p.Position.x},{p.Position.y};{p.PastureType};{p.Capacity};{p.Style}");
            }
            return string.Join("|", entries);
        }

        static List<PlacedHybrid> JsonToPlacedHybrids(string json)
        {
            var list = new List<PlacedHybrid>();
            if (string.IsNullOrEmpty(json)) return list;
            string[] entries = json.Split('|');
            foreach (var entry in entries)
            {
                if (string.IsNullOrEmpty(entry)) continue;
                string[] parts = entry.Split(';');
                if (parts.Length >= 3)
                {
                    var p = new PlacedHybrid();

                    // Parse hybrid IDs (new format uses ~ separator for multiple IDs)
                    string hybridIdsStr = parts[0];
                    if (!string.IsNullOrEmpty(hybridIdsStr))
                    {
                        if (hybridIdsStr.Contains("~"))
                        {
                            // New format: multiple IDs
                            string[] ids = hybridIdsStr.Split('~');
                            foreach (var id in ids)
                            {
                                if (!string.IsNullOrEmpty(id))
                                    p.HybridIds.Add(id);
                            }
                        }
                        else
                        {
                            // Old format: single ID (backwards compat)
                            p.HybridIds.Add(hybridIdsStr);
                        }
                    }

                    string[] posParts = parts[1].Split(',');
                    if (posParts.Length >= 2)
                    {
                        float.TryParse(posParts[0], out float x);
                        float.TryParse(posParts[1], out float y);
                        p.Position = new Vector2(x, y);
                    }
                    p.PastureType = parts[2];

                    // Parse capacity (default 1 for old saves)
                    if (parts.Length >= 4)
                    {
                        int.TryParse(parts[3], out p.Capacity);
                        if (p.Capacity < 1) p.Capacity = 1;
                    }
                    else
                    {
                        p.Capacity = 1;
                    }

                    // Parse style (default 0 for old saves)
                    if (parts.Length >= 5)
                    {
                        int.TryParse(parts[4], out p.Style);
                        p.Style = Mathf.Clamp(p.Style, 0, 2);
                    }
                    else
                    {
                        p.Style = 0;
                    }

                    list.Add(p);
                }
            }
            return list;
        }

        static string PlacedDecorationsToJson(List<PlacedDecoration> placed)
        {
            List<string> entries = new List<string>();
            foreach (var p in placed)
            {
                entries.Add($"{p.DecorationType};{p.Position.x},{p.Position.y}");
            }
            return string.Join("|", entries);
        }

        static List<PlacedDecoration> JsonToPlacedDecorations(string json)
        {
            var list = new List<PlacedDecoration>();
            if (string.IsNullOrEmpty(json)) return list;
            string[] entries = json.Split('|');
            foreach (var entry in entries)
            {
                if (string.IsNullOrEmpty(entry)) continue;
                string[] parts = entry.Split(';');
                if (parts.Length >= 2)
                {
                    var p = new PlacedDecoration();
                    p.DecorationType = parts[0];
                    string[] posParts = parts[1].Split(',');
                    if (posParts.Length >= 2)
                    {
                        float.TryParse(posParts[0], out float x);
                        float.TryParse(posParts[1], out float y);
                        p.Position = new Vector2(x, y);
                    }
                    list.Add(p);
                }
            }
            return list;
        }

        // Cheat functions
        public static void CheatAddGold(int amount)
        {
            Gold += amount;
            Debug.Log($"[CHEAT] Added {amount} gold. Total: {Gold}");
            SaveGame();
        }

        public static void CheatUnlockAllBiomes()
        {
            for (int i = 0; i < 10; i++)
            {
                UnlockedBiomes.Add(i);
            }
            Debug.Log("[CHEAT] All biomes unlocked!");
            SaveGame();
        }

        public static void CheatFindAllAnimals()
        {
            foreach (var biome in BiomeDatabase.Biomes)
            {
                foreach (var animal in biome.Animals)
                {
                    if (!FoundAnimals.ContainsKey(animal.Id))
                        FoundAnimals[animal.Id] = 0;
                    FoundAnimals[animal.Id] += 3;
                }
            }
            Debug.Log("[CHEAT] All animals found 3 times!");
            SaveGame();
        }

        public static void CheatMaxSkills()
        {
            DigPower = 5;
            DigRadius = 3;
            MaxEnergy = 500;
            EnergyRegen = 10f;
            CurrentEnergy = MaxEnergy;
            Debug.Log("[CHEAT] All skills maxed!");
            SaveGame();
        }

        public static void CheatUnlockAllDecorations()
        {
            string[] allDeco = { "Fence", "Path", "Bench", "Lamp", "Fountain", "Statue", "Tree", "Flowers" };
            foreach (var d in allDeco) UnlockedDecorations.Add(d);

            string[] allPastures = { "BasicPasture", "GrassPasture", "WaterPasture", "LuxuryPasture" };
            foreach (var p in allPastures) UnlockedPastures.Add(p);

            Debug.Log("[CHEAT] All decorations unlocked!");
            SaveGame();
        }

        public static int CalculateTotalIdleIncome()
        {
            int total = 0;
            foreach (var placed in PlacedHybrids)
            {
                total += placed.CalculateIncome();
            }
            // Decoration bonus - varies by type
            foreach (var deco in PlacedDecorations)
            {
                total += GetDecorationBonus(deco.DecorationType);
            }
            return Mathf.Max(0, total);
        }

        public static int GetDecorationBonus(string decoType)
        {
            switch (decoType)
            {
                case "Fence": return 1;
                case "Path": return 1;
                case "Bench": return 2;
                case "Lamp": return 2;
                case "Tree": return 3;
                case "Flowers": return 3;
                case "Fountain": return 8;
                case "Statue": return 15;
                default: return 1;
            }
        }

        public static int GetAnimalFoundCount(string animalId)
        {
            return FoundAnimals.ContainsKey(animalId) ? FoundAnimals[animalId] : 0;
        }

        public static bool CanUseAnimal(string animalId)
        {
            return FoundAnimals.ContainsKey(animalId) && FoundAnimals[animalId] > 0;
        }

        public static void UseAnimal(string animalId)
        {
            if (FoundAnimals.ContainsKey(animalId) && FoundAnimals[animalId] > 0)
            {
                FoundAnimals[animalId]--;
                if (FoundAnimals[animalId] <= 0)
                {
                    FoundAnimals.Remove(animalId);
                }
            }
        }
    }

    [System.Serializable]
    public class HybridData
    {
        public string Id;
        public string Name;
        public string Parent1Id;
        public string Parent2Id;
        public AnimalTraits MixedTraits;
        public int BaseValue;

        public HybridData() { }

        public HybridData(AnimalData parent1, AnimalData parent2)
        {
            Id = $"hybrid_{parent1.Id}_{parent2.Id}_{Random.Range(1000, 9999)}";
            Name = GenerateHybridName(parent1.Name, parent2.Name);
            Parent1Id = parent1.Id;
            Parent2Id = parent2.Id;
            MixedTraits = AnimalTraits.Mix(parent1.Traits, parent2.Traits);

            // Base value from rarity and trait extremes
            BaseValue = CalculateBaseValue(parent1, parent2);
        }

        string GenerateHybridName(string name1, string name2)
        {
            // Take first half of name1 and second half of name2
            int mid1 = name1.Length / 2;
            int mid2 = name2.Length / 2;
            return name1.Substring(0, mid1) + name2.Substring(mid2);
        }

        int CalculateBaseValue(AnimalData p1, AnimalData p2)
        {
            // Base income starts low for balanced 2-3 hour gameplay
            int value = 3;
            value += p1.Rarity * 2;
            value += p2.Rarity * 2;

            // Small bonus for mixing very different animals
            float traitDiff = Mathf.Abs(p1.Traits.BodyLength - p2.Traits.BodyLength) +
                             Mathf.Abs(p1.Traits.NeckLength - p2.Traits.NeckLength) +
                             Mathf.Abs(p1.Traits.LegLength - p2.Traits.LegLength);
            value += (int)(traitDiff * 3);

            return value;
        }

        public int GetTotalValue()
        {
            // Value increases based on how many times parents were found
            int p1Count = GameData.GetAnimalFoundCount(Parent1Id);
            int p2Count = GameData.GetAnimalFoundCount(Parent2Id);
            float multiplier = 1f + (p1Count + p2Count) * 0.1f;
            return (int)(BaseValue * multiplier);
        }
    }

    [System.Serializable]
    public class PlacedHybrid
    {
        public string HybridId; // Legacy - for backwards compat during load
        public List<string> HybridIds = new List<string>(); // Multiple hybrids per pasture
        public Vector2 Position;
        public string PastureType;
        public int Capacity = 1; // Upgradeable: 1 -> 2 -> 3
        public int Style = 0; // 0-2 for different color themes

        public int GetHybridCount()
        {
            return HybridIds.Count;
        }

        public bool HasRoom()
        {
            return HybridIds.Count < Capacity;
        }

        public void AddHybrid(string hybridId)
        {
            if (HasRoom() && !string.IsNullOrEmpty(hybridId))
            {
                HybridIds.Add(hybridId);
            }
        }

        public int CalculateIncome()
        {
            int totalIncome = 0;

            foreach (var hybridId in HybridIds)
            {
                var hybrid = GameData.Hybrids.Find(h => h.Id == hybridId);
                if (hybrid == null) continue;

                int income = hybrid.GetTotalValue();

                // Pasture bonus
                switch (PastureType)
                {
                    case "GrassPasture": income = (int)(income * 1.2f); break;
                    case "WaterPasture": income = (int)(income * 1.3f); break;
                    case "LuxuryPasture": income = (int)(income * 1.5f); break;
                }

                totalIncome += income;
            }

            return totalIncome;
        }

        public int GetUpgradeCost()
        {
            // Cost to upgrade capacity
            switch (Capacity)
            {
                case 1: return 500;   // 1 -> 2
                case 2: return 2500;  // 2 -> 3
                default: return 0;    // Already max
            }
        }

        public bool CanUpgrade()
        {
            return Capacity < 3;
        }
    }

    [System.Serializable]
    public class PlacedDecoration
    {
        public string DecorationType;
        public Vector2 Position;
    }
}
