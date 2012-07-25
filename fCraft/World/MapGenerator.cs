// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.ComponentModel;
using System.Linq;
using System.Xml.Linq;
using JetBrains.Annotations;

namespace fCraft
{

    /// <summary> Map generator themes. A theme defines what type of blocks are used to fill the map. </summary>
    public enum MapGenTheme
    {
        Arctic,
        Desert,
        Forest,
        Hell,
        Swamp
    }


    /// <summary> Map generator template. Templates define landscape shapes and features. </summary>
    public enum MapGenTemplate
    {
        Archipelago,
        Atoll,
        Bay,
        Dunes,
        Flat,
        Hills,
        Ice,
        Island,
        Lake,
        Mountains,
        Peninsula,
        Random,
        River,
        Streams
    }


    public sealed class MapGenerator
    {
        readonly MapGeneratorArgs args;
        readonly Random rand;
        readonly Noise noise;
        float[,] heightmap, blendmap, slopemap;

        const float CliffsideBlockThreshold = 0.01f;

        // theme-dependent vars
        Block bWaterSurface, bGroundSurface, bWater, bGround, bSeaFloor, bBedrock, bDeepWaterSurface, bCliff;

        int groundThickness = 5;
        const int SeaFloorThickness = 3;

        public MapGenerator([NotNull] MapGeneratorArgs generatorArgs)
        {
            if (generatorArgs == null) throw new ArgumentNullException("generatorArgs");
            args = generatorArgs;
            args.Validate();

            if (!args.CustomWaterLevel)
            {
                args.WaterLevel = (args.MapHeight - 1) / 2;
            }

            rand = new Random(args.Seed);
            noise = new Noise(args.Seed, NoiseInterpolationMode.Bicubic);
            ApplyTheme(args.Theme);
            EstimateComplexity();
        }


        public Map Generate()
        {
            GenerateHeightmap();
            return GenerateMap();
        }


        const int FlatgrassDirtLevel = 5;
        [NotNull]
        public static Map GenerateFlatgrass(int width, int length, int height)
        {
            Map map = new Map(null, width, length, height, true);
            map.Blocks.MemSet((byte)Block.Stone, 0, width * length * (height / 2 - FlatgrassDirtLevel));
            map.Blocks.MemSet((byte)Block.Dirt, width * length * (height / 2 - FlatgrassDirtLevel), width * length * (FlatgrassDirtLevel - 1));
            map.Blocks.MemSet((byte)Block.Grass, width * length * (height / 2 - 1), width * length);
            return map;
        }

        [NotNull]
        public static Map GenerateEmpty(int width, int length, int height)
        {
            return new Map(null, width, length, height, true);
        }

        [NotNull]
        public static Map GenerateOcean(int width, int length, int height)
        {
            Map map = new Map(null, width, length, height, true);
            map.Blocks.MemSet((byte)Block.Sand, 0, width * length);
            map.Blocks.MemSet((byte)Block.Water, width * length, width * length * (height / 2 - 1));
            return map;
        }


        #region Progress Reporting

        public event ProgressChangedEventHandler ProgressChanged;

        int progressTotalEstimate, progressRunningTotal;


        void EstimateComplexity()
        {
            // heightmap creation
            progressTotalEstimate = 10;
            if (args.UseBias) progressTotalEstimate += 2;
            if (args.LayeredHeightmap) progressTotalEstimate += 10;
            if (args.MarbledHeightmap) progressTotalEstimate++;
            if (args.InvertHeightmap) progressTotalEstimate++;

            // heightmap processing
            if (args.MatchWaterCoverage) progressTotalEstimate += 2;
            if (args.BelowFuncExponent != 1 || args.AboveFuncExponent != 1) progressTotalEstimate += 5;
            if (args.CliffSmoothing) progressTotalEstimate += 2;
            progressTotalEstimate += 2; // slope
            if (args.MaxHeightVariation > 0 || args.MaxDepthVariation > 0) progressTotalEstimate += 5;

            // filling
            progressTotalEstimate += 15;

            // post processing
            if (args.AddCaves) progressTotalEstimate += 5;
            if (args.AddOre) progressTotalEstimate += 3;
            if (args.AddBeaches) progressTotalEstimate += 5;
            if (args.AddTrees) progressTotalEstimate += 5;
        }


        void ReportProgress(int relativeIncrease, [NotNull] string message)
        {
            if (message == null) throw new ArgumentNullException("message");
            var h = ProgressChanged;
            if (h != null)
            {
                h(this, new ProgressChangedEventArgs((100 * progressRunningTotal / progressTotalEstimate), message));
            }
            progressRunningTotal += relativeIncrease;
        }

        #endregion


        #region Heightmap Processing

        void GenerateHeightmap()
        {
            ReportProgress(10, "Heightmap: Priming");
            heightmap = new float[args.MapWidth, args.MapLength];

            noise.PerlinNoise(heightmap, args.FeatureScale, args.DetailScale, args.Roughness, 0, 0);

            if (args.UseBias && !args.DelayBias)
            {
                ReportProgress(2, "Heightmap: Biasing");
                Noise.Normalize(heightmap);
                ApplyBias();
            }

            Noise.Normalize(heightmap);

            if (args.LayeredHeightmap)
            {
                ReportProgress(10, "Heightmap: Layering");

                // needs a new Noise object to randomize second map
                float[,] heightmap2 = new float[args.MapWidth, args.MapLength];
                new Noise(rand.Next(), NoiseInterpolationMode.Bicubic).PerlinNoise(heightmap2, 0, args.DetailScale, args.Roughness, 0, 0);
                Noise.Normalize(heightmap2);

                // make a blendmap
                blendmap = new float[args.MapWidth, args.MapLength];
                int blendmapDetailSize = (int)Math.Log(Math.Max(args.MapWidth, args.MapLength), 2) - 2;
                new Noise(rand.Next(), NoiseInterpolationMode.Cosine).PerlinNoise(blendmap, 3, blendmapDetailSize, 0.5f, 0, 0);
                Noise.Normalize(blendmap);
                float cliffSteepness = Math.Max(args.MapWidth, args.MapLength) / 6f;
                Noise.ScaleAndClip(blendmap, cliffSteepness);

                Noise.Blend(heightmap, heightmap2, blendmap);
            }

            if (args.MarbledHeightmap)
            {
                ReportProgress(1, "Heightmap: Marbling");
                Noise.Marble(heightmap);
            }

            if (args.InvertHeightmap)
            {
                ReportProgress(1, "Heightmap: Inverting");
                Noise.Invert(heightmap);
            }

            if (args.UseBias && args.DelayBias)
            {
                ReportProgress(2, "Heightmap: Biasing");
                Noise.Normalize(heightmap);
                ApplyBias();
            }
            Noise.Normalize(heightmap);
        }


        void ApplyBias()
        {
            // set corners and midpoint
            float[] corners = new float[4];
            int c = 0;
            for (int i = 0; i < args.RaisedCorners; i++)
            {
                corners[c++] = args.Bias;
            }
            for (int i = 0; i < args.LoweredCorners; i++)
            {
                corners[c++] = -args.Bias;
            }
            float midpoint = (args.MidPoint * args.Bias);

            // shuffle corners
            corners = corners.OrderBy(r => rand.Next()).ToArray();

            // overlay the bias
            Noise.ApplyBias(heightmap, corners[0], corners[1], corners[2], corners[3], midpoint);
        }

        #endregion


        #region Map Processing

        public Map GenerateMap()
        {
            Map map = new Map(null, args.MapWidth, args.MapLength, args.MapHeight, true);


            // Match water coverage
            float desiredWaterLevel = .5f;
            if (args.MatchWaterCoverage)
            {
                ReportProgress(2, "Heightmap Processing: Matching water coverage");
                desiredWaterLevel = Noise.FindThreshold(heightmap, args.WaterCoverage);
            }


            // Calculate above/below water multipliers
            float aboveWaterMultiplier = 0;
            if (desiredWaterLevel != 1)
            {
                aboveWaterMultiplier = (args.MaxHeight / (1 - desiredWaterLevel));
            }


            // Apply power functions to above/below water parts of the heightmap
            if (args.BelowFuncExponent != 1 || args.AboveFuncExponent != 1)
            {
                ReportProgress(5, "Heightmap Processing: Adjusting slope");
                for (int x = heightmap.GetLength(0) - 1; x >= 0; x--)
                {
                    for (int y = heightmap.GetLength(1) - 1; y >= 0; y--)
                    {
                        if (heightmap[x, y] < desiredWaterLevel)
                        {
                            float normalizedDepth = 1 - heightmap[x, y] / desiredWaterLevel;
                            heightmap[x, y] = desiredWaterLevel - (float)Math.Pow(normalizedDepth, args.BelowFuncExponent) * desiredWaterLevel;
                        }
                        else
                        {
                            float normalizedHeight = (heightmap[x, y] - desiredWaterLevel) / (1 - desiredWaterLevel);
                            heightmap[x, y] = desiredWaterLevel + (float)Math.Pow(normalizedHeight, args.AboveFuncExponent) * (1 - desiredWaterLevel);
                        }
                    }
                }
            }

            // Calculate the slope
            if (args.CliffSmoothing)
            {
                ReportProgress(2, "Heightmap Processing: Smoothing");
                slopemap = Noise.CalculateSlope(Noise.GaussianBlur5X5(heightmap));
            }
            else
            {
                slopemap = Noise.CalculateSlope(heightmap);
            }

            float[,] altmap = null;
            if (args.MaxHeightVariation != 0 || args.MaxDepthVariation != 0)
            {
                ReportProgress(5, "Heightmap Processing: Randomizing");
                altmap = new float[map.Width, map.Length];
                int blendmapDetailSize = (int)Math.Log(Math.Max(args.MapWidth, args.MapLength), 2) - 2;
                new Noise(rand.Next(), NoiseInterpolationMode.Cosine).PerlinNoise(altmap, 3, blendmapDetailSize, 0.5f, 0, 0);
                Noise.Normalize(altmap, -1, 1);
            }

            int snowStartThreshold = args.SnowAltitude - args.SnowTransition;
            int snowThreshold = args.SnowAltitude;

            ReportProgress(10, "Filling");
            for (int x = heightmap.GetLength(0) - 1; x >= 0; x--)
            {
                for (int y = heightmap.GetLength(1) - 1; y >= 0; y--)
                {
                    int level;
                    float slope;
                    if (heightmap[x, y] < desiredWaterLevel)
                    {
                        float depth = args.MaxDepth;
                        if (altmap != null)
                        {
                            depth += altmap[x, y] * args.MaxDepthVariation;
                        }
                        slope = slopemap[x, y] * depth;
                        level = args.WaterLevel - (int)Math.Round(Math.Pow(1 - heightmap[x, y] / desiredWaterLevel, args.BelowFuncExponent) * depth);

                        if (args.AddWater)
                        {
                            if (args.WaterLevel - level > 3)
                            {
                                map.SetBlock(x, y, args.WaterLevel, bDeepWaterSurface);
                            }
                            else
                            {
                                map.SetBlock(x, y, args.WaterLevel, bWaterSurface);
                            }
                            for (int i = args.WaterLevel; i > level; i--)
                            {
                                map.SetBlock(x, y, i, bWater);
                            }
                            for (int i = level; i >= 0; i--)
                            {
                                if (level - i < SeaFloorThickness)
                                {
                                    map.SetBlock(x, y, i, bSeaFloor);
                                }
                                else
                                {
                                    map.SetBlock(x, y, i, bBedrock);
                                }
                            }
                        }
                        else
                        {
                            if (blendmap != null && blendmap[x, y] > .25 && blendmap[x, y] < .75)
                            {
                                map.SetBlock(x, y, level, bCliff);
                            }
                            else
                            {
                                if (slope < args.CliffThreshold)
                                {
                                    map.SetBlock(x, y, level, bGroundSurface);
                                }
                                else
                                {
                                    map.SetBlock(x, y, level, bCliff);
                                }
                            }

                            for (int i = level - 1; i >= 0; i--)
                            {
                                if (level - i < groundThickness)
                                {
                                    if (blendmap != null && blendmap[x, y] > CliffsideBlockThreshold && blendmap[x, y] < (1 - CliffsideBlockThreshold))
                                    {
                                        map.SetBlock(x, y, i, bCliff);
                                    }
                                    else
                                    {
                                        if (slope < args.CliffThreshold)
                                        {
                                            map.SetBlock(x, y, i, bGround);
                                        }
                                        else
                                        {
                                            map.SetBlock(x, y, i, bCliff);
                                        }
                                    }
                                }
                                else
                                {
                                    map.SetBlock(x, y, i, bBedrock);
                                }
                            }
                        }

                    }
                    else
                    {
                        float height;
                        if (altmap != null)
                        {
                            height = args.MaxHeight + altmap[x, y] * args.MaxHeightVariation;
                        }
                        else
                        {
                            height = args.MaxHeight;
                        }
                        slope = slopemap[x, y] * height;
                        if (height != 0)
                        {
                            level = args.WaterLevel + (int)Math.Round(Math.Pow(heightmap[x, y] - desiredWaterLevel, args.AboveFuncExponent) * aboveWaterMultiplier / args.MaxHeight * height);
                        }
                        else
                        {
                            level = args.WaterLevel;
                        }

                        bool snow = args.AddSnow &&
                                    (level > snowThreshold ||
                                    (level > snowStartThreshold && rand.NextDouble() < (level - snowStartThreshold) / (double)(snowThreshold - snowStartThreshold)));

                        if (blendmap != null && blendmap[x, y] > .25 && blendmap[x, y] < .75)
                        {
                            map.SetBlock(x, y, level, bCliff);
                        }
                        else
                        {
                            if (slope < args.CliffThreshold)
                            {
                                map.SetBlock(x, y, level, (snow ? Block.White : bGroundSurface));
                            }
                            else
                            {
                                map.SetBlock(x, y, level, bCliff);
                            }
                        }

                        for (int i = level - 1; i >= 0; i--)
                        {
                            if (level - i < groundThickness)
                            {
                                if (blendmap != null && blendmap[x, y] > CliffsideBlockThreshold && blendmap[x, y] < (1 - CliffsideBlockThreshold))
                                {
                                    map.SetBlock(x, y, i, bCliff);
                                }
                                else
                                {
                                    if (slope < args.CliffThreshold)
                                    {
                                        if (snow)
                                        {
                                            map.SetBlock(x, y, i, Block.White);
                                        }
                                        else
                                        {
                                            map.SetBlock(x, y, i, bGround);
                                        }
                                    }
                                    else
                                    {
                                        map.SetBlock(x, y, i, bCliff);
                                    }
                                }
                            }
                            else
                            {
                                map.SetBlock(x, y, i, bBedrock);
                            }
                        }
                    }
                }
            }

            if (args.AddCaves || args.AddOre)
            {
                AddCaves(map);
            }

            if (args.AddBeaches)
            {
                ReportProgress(5, "Processing: Adding beaches");
                AddBeaches(map);
            }

            if (args.AddTrees)
            {
                ReportProgress(5, "Processing: Planting trees");
                if (args.AddGiantTrees)
                {
                    Map outMap = new Map(null, map.Width, map.Length, map.Height, false) { Blocks = (byte[])map.Blocks.Clone() };
                    var foresterArgs = new ForesterArgs
                    {
                        Map = map,
                        Rand = rand,
                        TreeCount = (int)(map.Width * map.Length * 4 / (1024f * (args.TreeSpacingMax + args.TreeSpacingMin) / 2)),
                        Operation = Forester.ForesterOperation.Add,
                        PlantOn = bGroundSurface
                    };
                    foresterArgs.BlockPlacing += (sender, e) => outMap.SetBlock(e.Coordinate, e.Block);
                    Forester.Generate(foresterArgs);
                    map = outMap;
                }
                GenerateTrees(map);
            }

            ReportProgress(0, "Generation complete");

            map.Metadata["_Origin", "GeneratorName"] = "fCraft";
            map.Metadata["_Origin", "GeneratorVersion"] = Updater.CurrentRelease.VersionString;
            map.Metadata["_Origin", "GeneratorParams"] = args.Serialize().ToString(SaveOptions.DisableFormatting);
            return map;
        }


        #region Caves

        // Cave generation method from Omen 0.70, used with osici's permission
        static void AddSingleCave(Random rand, Map map, byte bedrockType, byte fillingType, int length, double maxDiameter)
        {

            int startX = rand.Next(0, map.Width);
            int startY = rand.Next(0, map.Length);
            int startZ = rand.Next(0, map.Height);

            int k1;
            for (k1 = 0; map.Blocks[startX + map.Width * map.Length * (map.Height - 1 - startZ) + map.Width * startY] != bedrockType && k1 < 10000; k1++)
            {
                startX = rand.Next(0, map.Width);
                startY = rand.Next(0, map.Length);
                startZ = rand.Next(0, map.Height);
            }

            if (k1 >= 10000)
                return;

            int x = startX;
            int y = startY;
            int z = startZ;

            for (int k2 = 0; k2 < length; k2++)
            {
                int diameter = (int)(maxDiameter * rand.NextDouble() * map.Width);
                if (diameter < 1) diameter = 2;
                int radius = diameter / 2;
                if (radius == 0) radius = 1;
                x += (int)(0.7 * (rand.NextDouble() - 0.5D) * diameter);
                y += (int)(0.7 * (rand.NextDouble() - 0.5D) * diameter);
                z += (int)(0.7 * (rand.NextDouble() - 0.5D) * diameter);

                for (int j3 = 0; j3 < diameter; j3++)
                {
                    for (int k3 = 0; k3 < diameter; k3++)
                    {
                        for (int l3 = 0; l3 < diameter; l3++)
                        {
                            if ((j3 - radius) * (j3 - radius) + (k3 - radius) * (k3 - radius) + (l3 - radius) * (l3 - radius) >= radius * radius ||
                                x + j3 >= map.Width || z + k3 >= map.Height || y + l3 >= map.Length ||
                                x + j3 < 0 || z + k3 < 0 || y + l3 < 0)
                            {
                                continue;
                            }

                            int index = x + j3 + map.Width * map.Length * (map.Height - 1 - (z + k3)) + map.Width * (y + l3);

                            if (map.Blocks[index] == bedrockType)
                            {
                                map.Blocks[index] = fillingType;
                            }
                            if ((fillingType == 10 || fillingType == 11 || fillingType == 8 || fillingType == 9) &&
                                z + k3 < startZ)
                            {
                                map.Blocks[index] = 0;
                            }
                        }
                    }
                }
            }
        }

        static void AddSingleVein(Random rand, Map map, byte bedrockType, byte fillingType, int k, double maxDiameter, int l)
        {
            AddSingleVein(rand, map, bedrockType, fillingType, k, maxDiameter, l, 10);
        }

        static void AddSingleVein(Random rand, Map map, byte bedrockType, byte fillingType, int k, double maxDiameter, int l, int i1)
        {

            int j1 = rand.Next(0, map.Width);
            int k1 = rand.Next(0, map.Height);
            int l1 = rand.Next(0, map.Length);

            double thirteenOverK = 1 / (double)k;

            for (int i2 = 0; i2 < i1; i2++)
            {
                int j2 = j1 + (int)(.5 * (rand.NextDouble() - .5) * map.Width);
                int k2 = k1 + (int)(.5 * (rand.NextDouble() - .5) * map.Height);
                int l2 = l1 + (int)(.5 * (rand.NextDouble() - .5) * map.Length);
                for (int l3 = 0; l3 < k; l3++)
                {
                    int diameter = (int)(maxDiameter * rand.NextDouble() * map.Width);
                    if (diameter < 1) diameter = 2;
                    int radius = diameter / 2;
                    if (radius == 0) radius = 1;
                    int i3 = (int)((1 - thirteenOverK) * j1 + thirteenOverK * j2 + (l * radius) * (rand.NextDouble() - .5));
                    int j3 = (int)((1 - thirteenOverK) * k1 + thirteenOverK * k2 + (l * radius) * (rand.NextDouble() - .5));
                    int k3 = (int)((1 - thirteenOverK) * l1 + thirteenOverK * l2 + (l * radius) * (rand.NextDouble() - .5));
                    for (int k4 = 0; k4 < diameter; k4++)
                    {
                        for (int l4 = 0; l4 < diameter; l4++)
                        {
                            for (int i5 = 0; i5 < diameter; i5++)
                            {
                                if ((k4 - radius) * (k4 - radius) + (l4 - radius) * (l4 - radius) + (i5 - radius) * (i5 - radius) < radius * radius &&
                                    i3 + k4 < map.Width && j3 + l4 < map.Height && k3 + i5 < map.Length &&
                                    i3 + k4 >= 0 && j3 + l4 >= 0 && k3 + i5 >= 0)
                                {

                                    int index = i3 + k4 + map.Width * map.Length * (map.Height - 1 - (j3 + l4)) + map.Width * (k3 + i5);

                                    if (map.Blocks[index] == bedrockType)
                                    {
                                        map.Blocks[index] = fillingType;
                                    }
                                }
                            }
                        }
                    }
                }
                j1 = j2;
                k1 = k2;
                l1 = l2;
            }
        }

        static void SealLiquids(Map map, byte sealantType)
        {
            for (int x = 1; x < map.Width - 1; x++)
            {
                for (int z = 1; z < map.Height; z++)
                {
                    for (int y = 1; y < map.Length - 1; y++)
                    {
                        int index = map.Index(x, y, z);
                        if ((map.Blocks[index] == 10 || map.Blocks[index] == 11 || map.Blocks[index] == 8 || map.Blocks[index] == 9) &&
                            (map.GetBlock(x - 1, y, z) == Block.Air || map.GetBlock(x + 1, y, z) == Block.Air ||
                            map.GetBlock(x, y - 1, z) == Block.Air || map.GetBlock(x, y + 1, z) == Block.Air ||
                            map.GetBlock(x, y, z - 1) == Block.Air))
                        {
                            map.Blocks[index] = sealantType;
                        }
                    }
                }
            }
        }

        public void AddCaves(Map map)
        {
            if (args.AddCaves)
            {
                ReportProgress(5, "Processing: Adding caves");
                for (int i1 = 0; i1 < 36 * args.CaveDensity; i1++)
                    AddSingleCave(rand, map, (byte)bBedrock, (byte)Block.Air, 30, 0.05 * args.CaveSize);

                for (int j1 = 0; j1 < 9 * args.CaveDensity; j1++)
                    AddSingleVein(rand, map, (byte)bBedrock, (byte)Block.Air, 500, 0.015 * args.CaveSize, 1);

                for (int k1 = 0; k1 < 30 * args.CaveDensity; k1++)
                    AddSingleVein(rand, map, (byte)bBedrock, (byte)Block.Air, 300, 0.03 * args.CaveSize, 1, 20);


                if (args.AddCaveLava)
                {
                    for (int i = 0; i < 8 * args.CaveDensity; i++)
                    {
                        AddSingleCave(rand, map, (byte)bBedrock, (byte)Block.Lava, 30, 0.05 * args.CaveSize);
                    }
                    for (int j = 0; j < 3 * args.CaveDensity; j++)
                    {
                        AddSingleVein(rand, map, (byte)bBedrock, (byte)Block.Lava, 1000, 0.015 * args.CaveSize, 1);
                    }
                }


                if (args.AddCaveWater)
                {
                    for (int k = 0; k < 8 * args.CaveDensity; k++)
                    {
                        AddSingleCave(rand, map, (byte)bBedrock, (byte)Block.Water, 30, 0.05 * args.CaveSize);
                    }
                    for (int l = 0; l < 3 * args.CaveDensity; l++)
                    {
                        AddSingleVein(rand, map, (byte)bBedrock, (byte)Block.Water, 1000, 0.015 * args.CaveSize, 1);
                    }
                }

                SealLiquids(map, (byte)bBedrock);
            }


            if (args.AddOre)
            {
                ReportProgress(3, "Processing: Adding ore");
                for (int l1 = 0; l1 < 12 * args.CaveDensity; l1++)
                {
                    AddSingleCave(rand, map, (byte)bBedrock, (byte)Block.Coal, 500, 0.03);
                }

                for (int i2 = 0; i2 < 32 * args.CaveDensity; i2++)
                {
                    AddSingleVein(rand, map, (byte)bBedrock, (byte)Block.Coal, 200, 0.015, 1);
                    AddSingleCave(rand, map, (byte)bBedrock, (byte)Block.IronOre, 500, 0.02);
                }

                for (int k2 = 0; k2 < 8 * args.CaveDensity; k2++)
                {
                    AddSingleVein(rand, map, (byte)bBedrock, (byte)Block.IronOre, 200, 0.015, 1);
                    AddSingleVein(rand, map, (byte)bBedrock, (byte)Block.GoldOre, 200, 0.0145, 1);
                }

                for (int l2 = 0; l2 < 20 * args.CaveDensity; l2++)
                {
                    AddSingleCave(rand, map, (byte)bBedrock, (byte)Block.GoldOre, 400, 0.0175);
                }
            }
        }

        #endregion


        void AddBeaches([NotNull] Map map)
        {
            if (map == null) throw new ArgumentNullException("map");
            int beachExtentSqr = (args.BeachExtent + 1) * (args.BeachExtent + 1);
            for (int x = 0; x < map.Width; x++)
            {
                for (int y = 0; y < map.Length; y++)
                {
                    for (int z = args.WaterLevel; z <= args.WaterLevel + args.BeachHeight; z++)
                    {
                        if (map.GetBlock(x, y, z) != bGroundSurface) continue;
                        bool found = false;
                        for (int dx = -args.BeachExtent; !found && dx <= args.BeachExtent; dx++)
                        {
                            for (int dy = -args.BeachExtent; !found && dy <= args.BeachExtent; dy++)
                            {
                                for (int dz = -args.BeachHeight; dz <= 0; dz++)
                                {
                                    if (dx * dx + dy * dy + dz * dz > beachExtentSqr) continue;
                                    int xx = x + dx;
                                    int yy = y + dy;
                                    int zz = z + dz;
                                    if (xx < 0 || xx >= map.Width || yy < 0 || yy >= map.Length || zz < 0 ||
                                        zz >= map.Height) continue;
                                    Block block = map.GetBlock(xx, yy, zz);
                                    if (block == bWater || block == bWaterSurface)
                                    {
                                        found = true;
                                        break;
                                    }
                                }
                            }
                        }
                        if (found)
                        {
                            map.SetBlock(x, y, z, bSeaFloor);
                            if (z > 0 && map.GetBlock(x, y, z - 1) == bGround)
                            {
                                map.SetBlock(x, y, z - 1, bSeaFloor);
                            }
                        }
                    }
                }
            }
        }


        void GenerateTrees([NotNull] Map map)
        {
            if (map == null) throw new ArgumentNullException("map");
            int minHeight = args.TreeHeightMin;
            int maxHeight = args.TreeHeightMax;
            int minTrunkPadding = args.TreeSpacingMin;
            int maxTrunkPadding = args.TreeSpacingMax;
            const int topLayers = 2;
            const double odds = 0.618;

            Random rn = new Random();

            map.CalculateShadows();

            for (int x = 0; x < map.Width; x += rn.Next(minTrunkPadding, maxTrunkPadding + 1))
            {
                for (int y = 0; y < map.Length; y += rn.Next(minTrunkPadding, maxTrunkPadding + 1))
                {
                    int nx = x + rn.Next(-(minTrunkPadding / 2), (maxTrunkPadding / 2) + 1);
                    int ny = y + rn.Next(-(minTrunkPadding / 2), (maxTrunkPadding / 2) + 1);
                    if (nx < 0 || nx >= map.Width || ny < 0 || ny >= map.Length) continue;
                    int nz = map.Shadows[nx, ny];

                    if ((map.GetBlock(nx, ny, nz) == bGroundSurface) && slopemap[nx, ny] < .5)
                    {
                        // Pick a random height for the tree between Min and Max,
                        // discarding this tree if it would breach the top of the map
                        int nh;
                        if ((nh = rn.Next(minHeight, maxHeight + 1)) + nz + nh / 2 > map.Height)
                            continue;

                        // Generate the trunk of the tree
                        for (int z = 1; z <= nh; z++)
                            map.SetBlock(nx, ny, nz + z, Block.Log);

                        for (int i = -1; i < nh / 2; i++)
                        {
                            // Should we draw thin (2x2) or thicker (4x4) foliage
                            int radius = (i >= (nh / 2) - topLayers) ? 1 : 2;
                            // Draw the foliage
                            for (int xoff = -radius; xoff < radius + 1; xoff++)
                            {
                                for (int yoff = -radius; yoff < radius + 1; yoff++)
                                {
                                    // Drop random leaves from the edges
                                    if (rn.NextDouble() > odds && Math.Abs(xoff) == Math.Abs(yoff) && Math.Abs(xoff) == radius)
                                        continue;
                                    // By default only replace an existing block if its air
                                    if (map.GetBlock(nx + xoff, ny + yoff, nz + nh + i) == Block.Air)
                                        map.SetBlock(nx + xoff, ny + yoff, nz + nh + i, Block.Leaves);
                                }
                            }
                        }
                    }
                }
            }
        }

        #endregion


        #region Themes / Templates

        void ApplyTheme(MapGenTheme theme)
        {
            args.Theme = theme;
            switch (theme)
            {
                case MapGenTheme.Arctic:
                    bWaterSurface = Block.Glass;
                    bDeepWaterSurface = Block.Water;
                    bGroundSurface = Block.White;
                    bWater = Block.Water;
                    bGround = Block.White;
                    bSeaFloor = Block.White;
                    bBedrock = Block.Stone;
                    bCliff = Block.Stone;
                    groundThickness = 1;
                    break;
                case MapGenTheme.Desert:
                    bWaterSurface = Block.Water;
                    bDeepWaterSurface = Block.Water;
                    bGroundSurface = Block.Sand;
                    bWater = Block.Water;
                    bGround = Block.Sand;
                    bSeaFloor = Block.Sand;
                    bBedrock = Block.Stone;
                    bCliff = Block.Gravel;
                    break;
                case MapGenTheme.Hell:
                    bWaterSurface = Block.Lava;
                    bDeepWaterSurface = Block.Lava;
                    bGroundSurface = Block.Obsidian;
                    bWater = Block.Lava;
                    bGround = Block.Stone;
                    bSeaFloor = Block.Obsidian;
                    bBedrock = Block.Stone;
                    bCliff = Block.Stone;
                    break;
                case MapGenTheme.Forest:
                    bWaterSurface = Block.Water;
                    bDeepWaterSurface = Block.Water;
                    bGroundSurface = Block.Grass;
                    bWater = Block.Water;
                    bGround = Block.Dirt;
                    bSeaFloor = Block.Sand;
                    bBedrock = Block.Stone;
                    bCliff = Block.Stone;
                    break;
                case MapGenTheme.Swamp:
                    bWaterSurface = Block.Water;
                    bDeepWaterSurface = Block.Water;
                    bGroundSurface = Block.Dirt;
                    bWater = Block.Water;
                    bGround = Block.Dirt;
                    bSeaFloor = Block.Leaves;
                    bBedrock = Block.Stone;
                    bCliff = Block.Stone;
                    break;
            }
        }


        public static MapGeneratorArgs MakeTemplate(MapGenTemplate template)
        {
            switch (template)
            {
                case MapGenTemplate.Archipelago:
                    return new MapGeneratorArgs
                    {
                        MaxHeight = 8,
                        MaxDepth = 20,
                        FeatureScale = 3,
                        Roughness = .46f,
                        MatchWaterCoverage = true,
                        WaterCoverage = .85f
                    };

                case MapGenTemplate.Atoll:
                    return new MapGeneratorArgs
                    {
                        Theme = MapGenTheme.Desert,
                        MaxHeight = 2,
                        MaxDepth = 39,
                        UseBias = true,
                        Bias = .9f,
                        MidPoint = 1,
                        LoweredCorners = 4,
                        FeatureScale = 2,
                        DetailScale = 5,
                        MarbledHeightmap = true,
                        InvertHeightmap = true,
                        MatchWaterCoverage = true,
                        WaterCoverage = .95f
                    };

                case MapGenTemplate.Bay:
                    return new MapGeneratorArgs
                    {
                        MaxHeight = 22,
                        MaxDepth = 12,
                        UseBias = true,
                        Bias = 1,
                        MidPoint = -1,
                        RaisedCorners = 3,
                        LoweredCorners = 1,
                        TreeSpacingMax = 12,
                        TreeSpacingMin = 6,
                        MarbledHeightmap = true,
                        DelayBias = true
                    };

                case MapGenTemplate.Dunes:
                    return new MapGeneratorArgs
                    {
                        AddTrees = false,
                        AddWater = false,
                        Theme = MapGenTheme.Desert,
                        MaxHeight = 12,
                        MaxDepth = 7,
                        FeatureScale = 2,
                        DetailScale = 3,
                        Roughness = .44f,
                        MarbledHeightmap = true,
                        InvertHeightmap = true
                    };

                case MapGenTemplate.Hills:
                    return new MapGeneratorArgs
                    {
                        AddWater = false,
                        MaxHeight = 8,
                        MaxDepth = 8,
                        FeatureScale = 2,
                        TreeSpacingMin = 7,
                        TreeSpacingMax = 13
                    };

                case MapGenTemplate.Ice:
                    return new MapGeneratorArgs
                    {
                        AddTrees = false,
                        Theme = MapGenTheme.Arctic,
                        MaxHeight = 2,
                        MaxDepth = 2032,
                        FeatureScale = 2,
                        DetailScale = 7,
                        Roughness = .64f,
                        MarbledHeightmap = true,
                        MatchWaterCoverage = true,
                        WaterCoverage = .3f,
                        MaxHeightVariation = 0
                    };

                case MapGenTemplate.Island:
                    return new MapGeneratorArgs
                    {
                        MaxHeight = 16,
                        MaxDepth = 39,
                        UseBias = true,
                        Bias = .7f,
                        MidPoint = 1,
                        LoweredCorners = 4,
                        FeatureScale = 3,
                        DetailScale = 7,
                        MarbledHeightmap = true,
                        DelayBias = true,
                        AddBeaches = true,
                        Roughness = 0.45f
                    };

                case MapGenTemplate.Lake:
                    return new MapGeneratorArgs
                    {
                        MaxHeight = 14,
                        MaxDepth = 20,
                        UseBias = true,
                        Bias = .65f,
                        MidPoint = -1,
                        RaisedCorners = 4,
                        FeatureScale = 2,
                        Roughness = .56f,
                        MatchWaterCoverage = true,
                        WaterCoverage = .3f
                    };

                case MapGenTemplate.Mountains:
                    return new MapGeneratorArgs
                    {
                        AddWater = false,
                        MaxHeight = 40,
                        MaxDepth = 10,
                        FeatureScale = 1,
                        DetailScale = 7,
                        MarbledHeightmap = true,
                        AddSnow = true,
                        MatchWaterCoverage = true,
                        WaterCoverage = .5f,
                        Roughness = .55f,
                        CliffThreshold = .9f
                    };

                case MapGenTemplate.Random:
                    return new MapGeneratorArgs();

                case MapGenTemplate.River:
                    return new MapGeneratorArgs
                    {
                        MaxHeight = 22,
                        MaxDepth = 8,
                        FeatureScale = 0,
                        DetailScale = 6,
                        MarbledHeightmap = true,
                        MatchWaterCoverage = true,
                        WaterCoverage = .31f
                    };

                case MapGenTemplate.Streams:
                    return new MapGeneratorArgs
                    {
                        MaxHeight = 5,
                        MaxDepth = 4,
                        FeatureScale = 2,
                        DetailScale = 7,
                        Roughness = .55f,
                        MarbledHeightmap = true,
                        MatchWaterCoverage = true,
                        WaterCoverage = .25f,
                        TreeSpacingMin = 8,
                        TreeSpacingMax = 14
                    };

                case MapGenTemplate.Peninsula:
                    return new MapGeneratorArgs
                    {
                        MaxHeight = 22,
                        MaxDepth = 12,
                        UseBias = true,
                        Bias = .5f,
                        MidPoint = -1,
                        RaisedCorners = 3,
                        LoweredCorners = 1,
                        TreeSpacingMax = 12,
                        TreeSpacingMin = 6,
                        InvertHeightmap = true,
                        WaterCoverage = .5f
                    };

                case MapGenTemplate.Flat:
                    return new MapGeneratorArgs
                    {
                        MaxHeight = 0,
                        MaxDepth = 0,
                        MaxHeightVariation = 0,
                        AddWater = false,
                        DetailScale = 0,
                        FeatureScale = 0,
                        AddCliffs = false
                    };

                default:
                    throw new ArgumentOutOfRangeException("template");
            }
        }

        #endregion
    }
}