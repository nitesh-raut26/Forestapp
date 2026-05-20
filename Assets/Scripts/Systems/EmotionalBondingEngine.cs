using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForestFriendsQuest
{
    public enum CreatureMood
    {
        Cheerful,
        Cozy,
        Shy,
        Sleepy,
        Curious
    }

    [Serializable]
    public class CreatureBondState
    {
        public string creatureId;
        public int bondLevel = 1;
        public int trustProgress = 0;
        public CreatureMood currentMood = CreatureMood.Cozy;
        public List<string> unlockedMemories = new List<string>();
    }

    public class EmotionalBondingEngine : MonoBehaviour
    {
        private readonly Dictionary<string, CreatureBondState> _states = new Dictionary<string, CreatureBondState>();
        private readonly Dictionary<string, string> _favoriteTreats = new Dictionary<string, string>
        {
            { "pip", "Amber Pinecone" },
            { "mimi", "Sunberry" },
            { "tomo", "River Moss" },
            { "luma", "Glowing Sap" },
            { "nori", "Wild fern" },
            { "sol", "Midnight Truffle" }
        };

        private readonly Dictionary<string, string[]> _memories = new Dictionary<string, string[]>
        {
            {
                "pip", new[]
                {
                    "Pip remembers the rainy afternoon you shared under the hollow oak log.",
                    "Pip recalls how you guided the tiny fireflies to light up the dark marsh trails.",
                    "Pip holds a memory of a shining leaf star discovered during a morning run."
                }
            },
            {
                "mimi", new[]
                {
                    "Mimi remembers singing her first summer canopy melody for you.",
                    "Mimi recalls when you saved her tiny feathered nest from a windy storm.",
                    "Mimi holds a cozy memory of resting on your shoulder during sunset."
                }
            },
            {
                "tomo", new[]
                {
                    "Tomo remembers swimming across the shimmering Moonlit Creek together.",
                    "Tomo recalls your soft touch when cleaning river algae off his ancient shell.",
                    "Tomo holds a warm memory of sleeping beside the campground crackle."
                }
            },
            {
                "luma", new[]
                {
                    "Luma remembers dancing around your fingertips in the marsh.",
                    "Luma recalls glowing brightly to help you spot a hidden chest inside the Crystal Caverns.",
                    "Luma holds a memory of lighting up the starry Druid sanctuary for a bedtime story."
                }
            }
        };

        public CreatureBondState GetBondState(string creatureId)
        {
            if (string.IsNullOrEmpty(creatureId)) return null;

            if (!_states.TryGetValue(creatureId, out var state))
            {
                state = new CreatureBondState { creatureId = creatureId };
                _states[creatureId] = state;
            }

            return state;
        }

        public string GetFavoriteTreat(string creatureId)
        {
            return _favoriteTreats.TryGetValue(creatureId, out var treat) ? treat : "Forest berry";
        }

        public void FeedTreat(string creatureId, string treatName, out bool lovedIt)
        {
            var state = GetBondState(creatureId);
            lovedIt = false;

            if (state == null) return;

            var favorite = GetFavoriteTreat(creatureId);
            var points = 10;

            if (favorite.ToLower() == treatName.ToLower())
            {
                points = 25;
                lovedIt = true;
                state.currentMood = CreatureMood.Cheerful;
            }
            else
            {
                state.currentMood = CreatureMood.Curious;
            }

            AddTrust(creatureId, points);
        }

        public void AddTrust(string creatureId, int amount)
        {
            var state = GetBondState(creatureId);
            if (state == null) return;

            state.trustProgress += amount;
            var required = state.bondLevel * 50;

            if (state.trustProgress >= required)
            {
                state.trustProgress -= required;
                state.bondLevel++;
                
                // Unlock a new memory on level up!
                UnlockNextMemory(state);
            }
        }

        private void UnlockNextMemory(CreatureBondState state)
        {
            if (_memories.TryGetValue(state.creatureId, out var list))
            {
                foreach (var mem in list)
                {
                    if (!state.unlockedMemories.Contains(mem))
                    {
                        state.unlockedMemories.Add(mem);
                        break;
                    }
                }
            }
        }

        public string GetDynamicGreeting(string creatureId, TimeOfDay time)
        {
            var state = GetBondState(creatureId);
            var mood = state != null ? state.currentMood.ToString() : "Cozy";

            switch (mood)
            {
                case "Cheerful":
                    return "Happy to see you! Ready for a brilliant quest?";
                case "Sleepy":
                    return "Yawn... The forest breeze makes me feel so snug.";
                case "Curious":
                    return "What secrets will we discover along the trail today?";
                default:
                    if (time == TimeOfDay.Night)
                    {
                        return "Shh... The forest is dreaming. Sweet dreams, explorer.";
                    }
                    return "Hello there! The meadow path is calling us.";
            }
        }

        public void ShiftCreatureMoods()
        {
            var moods = (CreatureMood[])Enum.GetValues(typeof(CreatureMood));
            foreach (var pair in _states)
            {
                var rand = UnityEngine.Random.Range(0, moods.Length);
                pair.Value.currentMood = moods[rand];
            }
        }

        public void HydrateFromSave(ForestSaveData saveData)
        {
            if (saveData == null) return;
            GetBondState("pip").bondLevel = saveData.pipBond;
            GetBondState("mimi").bondLevel = saveData.mimiBond;
            GetBondState("tomo").bondLevel = saveData.tomoBond;
            GetBondState("luma").bondLevel = saveData.lumaBond;
        }

        public void SyncToSave(ForestSaveData saveData)
        {
            if (saveData == null) return;
            saveData.pipBond = GetBondState("pip").bondLevel;
            saveData.mimiBond = GetBondState("mimi").bondLevel;
            saveData.tomoBond = GetBondState("tomo").bondLevel;
            saveData.lumaBond = GetBondState("luma").bondLevel;
        }
    }
}
