using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForestFriendsQuest
{
    [Serializable]
    public class ProceduralBiome
    {
        public string id;
        public string title;
        public string mood;
        public string dominantColorHex;
        public string weatherTableSeed;
        public string[] regionalCreatures;
    }

    [Serializable]
    public class PlacedChest
    {
        public string chestId;
        public float posX;
        public float posY;
        public string contents;
        public bool isRare;
    }

    public class ProceduralForestGenerator : MonoBehaviour
    {
        private readonly string[] _biomes = new[]
        {
            "Whispering Meadow", "Moonlit Creek", "Elderwood Grove",
            "Crystal Caverns", "Forgotten Ruins", "Firefly Marsh",
            "Ancient Observatory", "Skyroot Canopy", "Druid Sanctuary"
        };

        private readonly string[] _colors = new[]
        {
            "#2F7A56", "#21513E", "#2A595B",
            "#4D2D5B", "#6B4A35", "#164A47",
            "#25375A", "#4B6B40", "#1C3C30"
        };

        private readonly string[][] _creatures = new[]
        {
            new[] { "pip", "mimi" },
            new[] { "tomo", "luma" },
            new[] { "pip", "tomo" },
            new[] { "luma", "mimi" }
        };

        public ProceduralBiome GenerateBiome(string seed, int rank)
        {
            var hash = Mathf.Abs(seed.GetHashCode());
            var biomeIndex = (hash + rank) % _biomes.Length;

            return new ProceduralBiome
            {
                id = "proc-biome-" + biomeIndex,
                title = _biomes[biomeIndex],
                mood = "A cozy procedural expanse filled with " + (biomeIndex % 2 == 0 ? "soft moss and birdsong." : "ancient mystery and gentle streams."),
                dominantColorHex = _colors[biomeIndex % _colors.Length],
                weatherTableSeed = seed + "_weather_" + biomeIndex,
                regionalCreatures = _creatures[biomeIndex % _creatures.Length]
            };
        }

        public List<PlacedChest> PopulateChests(int density, string seed)
        {
            var list = new List<PlacedChest>();
            var pseudoRand = new System.Random(seed.GetHashCode());

            for (var i = 0; i < density; i++)
            {
                var isRare = pseudoRand.Next(100) < 15; // 15% rare chest chance
                var contents = isRare ? "river_crystal" : "elderwood";
                if (isRare && pseudoRand.Next(100) < 40) contents = "firefly_dust";
                else if (isRare && pseudoRand.Next(100) < 20) contents = "ancient_sap";

                list.Add(new PlacedChest
                {
                    chestId = "chest_" + i,
                    posX = (float)(pseudoRand.NextDouble() * 500.0 - 250.0),
                    posY = (float)(pseudoRand.NextDouble() * 360.0 - 180.0),
                    contents = contents,
                    isRare = isRare
                });
            }

            return list;
        }

        public PathCellData[] GenerateProceduralPathBoard(string seed, int columns, int rows, out string[] sequence)
        {
            var cellCount = columns * rows;
            var cells = new PathCellData[cellCount];
            var pseudoRand = new System.Random(seed.GetHashCode());

            var labels = new[] { "Leaf", "Stone", "Root", "Moss", "Fern", "Bloom", "Branch", "Sap", "Pebble" };

            // Fill cell labels
            for (var i = 0; i < cellCount; i++)
            {
                var label = labels[pseudoRand.Next(labels.Length)];
                cells[i] = new PathCellData
                {
                    id = "p-cell-" + i,
                    label = label + " " + (i + 1)
                };
            }

            // Construct a procedural path traversal route sequence (guaranteeing neighbors)
            var seqList = new List<string>();
            var curCol = 0;
            var curRow = 0;
            seqList.Add("p-cell-0");

            while (curCol < columns - 1 || curRow < rows - 1)
            {
                // Deciding random steps
                var moveRight = pseudoRand.Next(2) == 0;
                if (moveRight && curCol < columns - 1)
                {
                    curCol++;
                }
                else if (curRow < rows - 1)
                {
                    curRow++;
                }
                else if (curCol < columns - 1)
                {
                    curCol++;
                }

                var nextCellIndex = curRow * columns + curCol;
                var cellId = "p-cell-" + nextCellIndex;
                if (!seqList.Contains(cellId))
                {
                    seqList.Add(cellId);
                }
            }

            sequence = seqList.ToArray();
            return cells;
        }
    }
}
