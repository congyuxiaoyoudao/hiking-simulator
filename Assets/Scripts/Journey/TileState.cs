using System;
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
            SoilWater = Mathf.Clamp01(humidity) * MaxWater;
            EvaporationRate = Mathf.Max(0, evaporationRate);
            Height = Mathf.Clamp(height, 0, 1);
            SurfaceWater = Mathf.Clamp(surfaceWater, 0, 30);
            Temperature = Mathf.Clamp(temperature, 0, 20);
        }

        public TileSnapshot Snapshot() => new TileSnapshot(this);

        public void AddWater(float quantity)
        {
            if (quantity <= 0) return;
            // Fill surface storage first, then soil; excess beyond both capacities is runoff.
            float surfaceAdded = Mathf.Min(quantity, 30 - SurfaceWater);
            SurfaceWater += surfaceAdded;
            SoilWater = Mathf.Min(MaxWater, SoilWater + quantity - surfaceAdded);
        }

        // One click is one discrete step. Infiltration conserves water; evaporation removes it.
        public void Step(float infiltrationPerStep, PlantCatalog plantCatalog = null)
        {
            var before = Snapshot();
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
            LastChange = Snapshot() - before;
        }
    }

    public struct TileSnapshot
    {
        public float SoilContent, MaxWater, SoilHumidity, EvaporationRate, SurfaceWater, Temperature;
        public int PlantCount, Height;
        public TileSnapshot(TileState tile)
        {
            SoilContent = tile.SoilContent; MaxWater = tile.MaxWater; PlantCount = tile.PlantCount;
            SoilHumidity = tile.SoilHumidity; EvaporationRate = tile.EvaporationRate;
            Height = tile.Height; SurfaceWater = tile.SurfaceWater; Temperature = tile.Temperature;
        }
        public static TileSnapshot operator -(TileSnapshot a, TileSnapshot b) => new TileSnapshot
        {
            SoilContent = a.SoilContent - b.SoilContent, MaxWater = a.MaxWater - b.MaxWater,
            PlantCount = a.PlantCount - b.PlantCount, SoilHumidity = a.SoilHumidity - b.SoilHumidity,
            EvaporationRate = a.EvaporationRate - b.EvaporationRate, Height = a.Height - b.Height,
            SurfaceWater = a.SurfaceWater - b.SurfaceWater, Temperature = a.Temperature - b.Temperature
        };
    }
}
