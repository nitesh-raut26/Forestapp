using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForestFriendsQuest
{
    [Serializable]
    public class CraftingBlueprint
    {
        public string id;
        public string title;
        public string emoji;
        public string detail;
        public int woodCost;
        public int crystalCost;
        public int dustCost;
        public int sapCost;
    }

    public class SanctuaryBuilderManager : MonoBehaviour
    {
        private readonly List<CraftingBlueprint> _blueprints = new List<CraftingBlueprint>
        {
            new CraftingBlueprint
            {
                id = "dream_catcher",
                title = "Celestial Dream-Catcher",
                emoji = "[Dream-Catcher]",
                detail = "Collects golden stars and plays soothing acoustic wind melodies.",
                woodCost = 4,
                crystalCost = 2,
                dustCost = 2,
                sapCost = 0
            },
            new CraftingBlueprint
            {
                id = "campfire",
                title = "Cozy Forest Campfire",
                emoji = "[Campfire]",
                detail = "Warm ambient lighting that dynamic characters sit around at night.",
                woodCost = 3,
                crystalCost = 0,
                dustCost = 3,
                sapCost = 1
            },
            new CraftingBlueprint
            {
                id = "totem",
                title = "Ancient Druid Totem",
                emoji = "[Totem]",
                detail = "Increases all regional friendship bond experience sweeps by 25%.",
                woodCost = 5,
                crystalCost = 3,
                dustCost = 1,
                sapCost = 2
            }
        };

        public List<CraftingBlueprint> Blueprints => _blueprints;

        public bool TryCraft(string blueprintId, ForestSaveData save, out string error)
        {
            error = "";
            var bp = _blueprints.Find(b => b.id == blueprintId);
            if (bp == null)
            {
                error = "Unknown blueprint recipe.";
                return false;
            }

            if (save.elderwood < bp.woodCost ||
                save.riverCrystals < bp.crystalCost ||
                save.fireflyDust < bp.dustCost ||
                save.ancientSap < bp.sapCost)
            {
                error = "Cauldron is short on raw materials! Complete more quests.";
                return false;
            }

            // Deduct alchemical ingredients
            save.elderwood -= bp.woodCost;
            save.riverCrystals -= bp.crystalCost;
            save.fireflyDust -= bp.dustCost;
            save.ancientSap -= bp.sapCost;

            // Add to crafted list
            var crafted = new List<string>(save.craftedItemIds ?? new string[0]);
            crafted.Add(bp.id);
            save.craftedItemIds = crafted.ToArray();

            return true;
        }

        public void WaterSeedling(ForestSaveData save, out string feedback, out string earnedSticker)
        {
            feedback = "";
            earnedSticker = "";

            if (save.sproutGrowth >= 4)
            {
                // Harvest Sticker Keepsake!
                save.sproutGrowth = 1; // Reset loop
                
                var rand = UnityEngine.Random.Range(0, 4);
                earnedSticker = rand == 0 ? "acorn" : rand == 1 ? "butterfly" : rand == 2 ? "mushroom" : "flower";

                feedback = "Golden Blossom harvested! Added one " + earnedSticker + " sticker keepsake to your meadow.";
            }
            else
            {
                save.sproutGrowth++;
                feedback = "Splish! The magical sprout grows taller and stronger.";
            }
        }
    }
}
