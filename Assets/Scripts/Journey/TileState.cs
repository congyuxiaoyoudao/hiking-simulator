using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hiking.Journey
{
    // Percentages are stored as fractions (0..1); water amounts use the same unit.
    [Serializable]
    public sealed class TileState
    {
        public StationTheme Category { get; private set; }
        public float SoilContent { get; private set; }
        public float MaxWater => SoilContent * 100f + 10f;
        public int PlantCount { get; private set; }
        public PlantInstance[] NaturalPlants { get; private set; }
        public List<SeedInstance> Seeds { get; } = new List<SeedInstance>();
        public int SeedCount => Seeds.Count;
        public int FreePlantCapacity => Mathf.Max(0, 5 - PlantCount - SeedCount);
        public float SoilWater { get; private set; }
        public float SoilHumidity => SoilWater / MaxWater;
        public float EvaporationRate { get; private set; }
        public int Height { get; private set; }
        public float SurfaceWater { get; private set; }
        public float Temperature { get; private set; }
        public TileSnapshot LastChange { get; private set; }
        public string CategoryName => Category == StationTheme.Snow ? "雪地" : Category == StationTheme.Grass ? "平原" : "沙漠";

        public TileState(StationTheme category, float soilContent, int plants, float humidity,
            float evaporationRate, int height, float surfaceWater, float temperature, PlantInstance[] naturalPlants = null)
        {
            Category = category;
            SoilContent = Mathf.Clamp01(soilContent);
            NaturalPlants = naturalPlants ?? Array.Empty<PlantInstance>();
            PlantCount = naturalPlants == null ? Mathf.Clamp(plants, 0, 5) : naturalPlants.Length;
            for (int i = 0; i < NaturalPlants.Length; i++)
                NaturalPlants[i].positionOffset = (i - (NaturalPlants.Length - 1) * .5f) * .16f;
            SoilWater = Mathf.Clamp01(humidity) * MaxWater;
            EvaporationRate = Mathf.Max(0, evaporationRate);
            Height = Mathf.Clamp(height, 0, 1);
            SurfaceWater = Mathf.Clamp(surfaceWater, 0, 30);
            Temperature = Mathf.Clamp(temperature, 0, 20);
        }

        public TileSnapshot Snapshot() => new TileSnapshot(this);

        public bool TryAddSeed(string plantId, int currentStep)
        {
            if (string.IsNullOrWhiteSpace(plantId) || FreePlantCapacity <= 0) return false;
            Seeds.Add(new SeedInstance(plantId, currentStep));
            return true;
        }

        float FreePlantPosition()
        {
            float[] positions = { 0, -.16f, .16f, -.32f, .32f };
            foreach (float position in positions)
                if (Array.TrueForAll(NaturalPlants, plant => Mathf.Abs(plant.positionOffset - position) > .03f))
                    return position;
            return 0;
        }

        public void AddWater(float quantity)
        {
            if (quantity <= 0) return;
            // Placement fills surface storage only; overflow runs off this tile.
            SurfaceWater = Mathf.Min(30, SurfaceWater + quantity);
        }

        internal void ApplyNeighborFlow(float soilContent, float soilWater, float temperature)
        {
            SoilContent = Mathf.Clamp01(soilContent);
            Temperature = Mathf.Clamp(temperature, 0, 20);
            float overflow = Mathf.Max(0, soilWater - MaxWater);
            SoilWater = Mathf.Clamp(soilWater, 0, MaxWater);
            // Lost capacity releases excess soil water to the surface; a full surface runs off.
            SurfaceWater = Mathf.Min(30, SurfaceWater + overflow);
        }

        // One step is one simulated second. Neighbor flow is applied before this method.
        public void Step(float infiltrationPerStep, PlantCatalog plantCatalog = null, int currentStep = 0,
            TileSnapshot? beforeNeighborFlow = null)
        {
            var before = beforeNeighborFlow ?? Snapshot();
            float infiltrated = Mathf.Min(SurfaceWater, Mathf.Min(MaxWater - SoilWater,
                Mathf.Max(0, infiltrationPerStep) * SoilContent));
            SurfaceWater -= infiltrated;
            SoilWater += infiltrated;
            float loss = EvaporationRate * (1f + Temperature / 20f);
            float surfaceLoss = Mathf.Min(SurfaceWater, loss);
            SurfaceWater -= surfaceLoss;
            SoilWater = Mathf.Max(0, SoilWater - (loss - surfaceLoss));
            if (plantCatalog != null && NaturalPlants.Length > 0)
            {
                foreach (var plant in NaturalPlants)
                {
                    var definition = plantCatalog.Find(plant.plantId);
                    int allowedStage = definition == null || !definition.Suits(Category) ? -1 :
                        definition.HighestSuitableStage(SoilContent * 100f, SoilHumidity * 100f);
                    int ceiling = allowedStage < 0 ? 0 : definition.LifeCeiling(allowedStage);
                    int previousLife = plant.life;
                    if (SoilWater < .1f || plant.life > ceiling)
                        plant.life = Mathf.Max(0, plant.life - 1);
                    else if (plant.life < ceiling)
                    {
                        SoilWater -= .1f;
                        plant.life++;
                        while (plant.stageIndex + 1 <= allowedStage &&
                            plant.life >= definition.stages[plant.stageIndex + 1].minLife)
                            plant.stageIndex++;
                    }
                    plant.lastLifeChange = plant.life - previousLife;
                }
                NaturalPlants = Array.FindAll(NaturalPlants, plant => plant.life > 0);
                PlantCount = NaturalPlants.Length;
            }
            if (plantCatalog != null)
            {
                for (int i = Seeds.Count - 1; i >= 0; i--)
                {
                    var seed = Seeds[i];
                    if (seed.createdStep >= currentStep) continue;
                    var definition = plantCatalog.Find(seed.plantId);
                    bool canGerminate = definition != null && definition.Suits(Category) &&
                        definition.HighestSuitableStage(SoilContent * 100f, SoilHumidity * 100f) >= 0 &&
                        SoilWater >= .1f;
                    if (canGerminate && PlantCount < 5)
                    {
                        SoilWater = Mathf.Max(0, SoilWater - .1f);
                        var plant = new PlantInstance(seed.plantId) { positionOffset = FreePlantPosition() };
                        var grown = new PlantInstance[NaturalPlants.Length + 1];
                        Array.Copy(NaturalPlants, grown, NaturalPlants.Length);
                        grown[grown.Length - 1] = plant;
                        NaturalPlants = grown;
                        PlantCount++;
                        Seeds.RemoveAt(i);
                    }
                    else if (--seed.remainingSteps <= 0) Seeds.RemoveAt(i);
                }
            }
            LastChange = Snapshot() - before;
        }
    }

    public static class TileNeighborFlow
    {
        // All edge transfers use the same pre-step values, including station boundaries.
        public static void Exchange(TileState[] tiles, float temperatureRate, float soilWaterRate, float soilContentRate)
        {
            int count = tiles.Length;
            var soil = new float[count];
            var water = new float[count];
            var temperature = new float[count];
            var soilMax = new float[count];
            var temperatureMax = new float[count];
            for (int i = 0; i < count; i++)
            {
                soil[i] = tiles[i].SoilContent;
                water[i] = tiles[i].SoilWater;
                temperature[i] = tiles[i].Temperature;
                soilMax[i] = 1;
                temperatureMax[i] = 20;
            }
            var soilChange = Changes(soil, soilMax, soilContentRate);
            var waterMax = new float[count];
            for (int i = 0; i < count; i++) waterMax[i] = (soil[i] + soilChange[i]) * 100f + 10f;
            var waterChange = Changes(water, waterMax, soilWaterRate);
            var temperatureChange = Changes(temperature, temperatureMax, temperatureRate);
            for (int i = 0; i < count; i++)
                tiles[i].ApplyNeighborFlow(soil[i] + soilChange[i], water[i] + waterChange[i],
                    temperature[i] + temperatureChange[i]);
        }

        static float[] Changes(float[] values, float[] maximums, float rate)
        {
            int count = values.Length;
            var edges = new float[Mathf.Max(0, count - 1)];
            var outgoing = new float[count];
            var incoming = new float[count];
            for (int i = 0; i < edges.Length; i++)
            {
                float difference = values[i] - values[i + 1];
                // One third of a small difference prevents a tile with two neighbors from overshooting.
                float amount = Mathf.Min(Mathf.Max(0, rate), Mathf.Abs(difference) / 3f);
                edges[i] = difference > 0 ? amount : -amount;
                if (edges[i] > 0) { outgoing[i] += amount; incoming[i + 1] += amount; }
                else { outgoing[i + 1] += amount; incoming[i] += amount; }
            }
            var outgoingScale = new float[count];
            var incomingScale = new float[count];
            for (int i = 0; i < count; i++)
            {
                outgoingScale[i] = outgoing[i] > 0 ? Mathf.Min(1, Mathf.Max(0, values[i]) / outgoing[i]) : 1;
                incomingScale[i] = incoming[i] > 0 ?
                    Mathf.Min(1, Mathf.Max(0, maximums[i] - values[i]) / incoming[i]) : 1;
            }
            var change = new float[count];
            for (int i = 0; i < edges.Length; i++)
            {
                float amount = edges[i] > 0 ? edges[i] * Mathf.Min(outgoingScale[i], incomingScale[i + 1]) :
                    edges[i] * Mathf.Min(outgoingScale[i + 1], incomingScale[i]);
                change[i] -= amount;
                change[i + 1] += amount;
            }
            return change;
        }
    }

    public struct TileSnapshot
    {
        public float SoilContent, MaxWater, SoilWater, SoilHumidity, EvaporationRate, SurfaceWater, Temperature;
        public int PlantCount, SeedCount, Height;
        public TileSnapshot(TileState tile)
        {
            SoilContent = tile.SoilContent; MaxWater = tile.MaxWater; PlantCount = tile.PlantCount; SeedCount = tile.SeedCount;
            SoilWater = tile.SoilWater; SoilHumidity = tile.SoilHumidity; EvaporationRate = tile.EvaporationRate;
            Height = tile.Height; SurfaceWater = tile.SurfaceWater; Temperature = tile.Temperature;
        }
        public static TileSnapshot operator -(TileSnapshot a, TileSnapshot b) => new TileSnapshot
        {
            SoilContent = a.SoilContent - b.SoilContent, MaxWater = a.MaxWater - b.MaxWater,
            PlantCount = a.PlantCount - b.PlantCount, SeedCount = a.SeedCount - b.SeedCount,
            SoilWater = a.SoilWater - b.SoilWater, SoilHumidity = a.SoilHumidity - b.SoilHumidity,
            EvaporationRate = a.EvaporationRate - b.EvaporationRate, Height = a.Height - b.Height,
            SurfaceWater = a.SurfaceWater - b.SurfaceWater, Temperature = a.Temperature - b.Temperature
        };
    }
}
