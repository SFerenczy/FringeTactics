using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace FringeTactics;

/// <summary>
/// Loads and provides access to faction definitions from data files.
/// </summary>
public static class FactionRegistry
{
    private static Dictionary<string, Faction> factions;
    private static bool isLoaded = false;

    public static IReadOnlyDictionary<string, Faction> Factions
    {
        get
        {
            EnsureLoaded();
            return factions;
        }
    }

    public static Faction Get(string id)
    {
        EnsureLoaded();
        return factions.TryGetValue(id, out var faction) ? faction : null;
    }

    public static IEnumerable<Faction> GetAll()
    {
        EnsureLoaded();
        return factions.Values;
    }

    private static void EnsureLoaded()
    {
        if (isLoaded) return;
        Load();
    }

    private static void Load()
    {
        factions = new Dictionary<string, Faction>();

        try
        {
            var file = FileAccess.Open("res://data/factions.json", FileAccess.ModeFlags.Read);
            if (file == null)
            {
                GD.PrintErr("[FactionRegistry] Could not open factions.json");
                LoadDefaults();
                return;
            }

            var json = file.GetAsText();
            file.Close();

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var data = JsonSerializer.Deserialize<FactionFileData>(json, options);

            if (data?.Factions == null)
            {
                GD.PrintErr("[FactionRegistry] Invalid factions.json format");
                LoadDefaults();
                return;
            }

            foreach (var fd in data.Factions)
            {
                var factionType = Enum.TryParse<FactionType>(fd.Type, out var ft) ? ft : FactionType.Neutral;
                var faction = new Faction(fd.Id, fd.Name, factionType)
                {
                    Color = new Color(fd.Color?.R ?? 0.5f, fd.Color?.G ?? 0.5f, fd.Color?.B ?? 0.5f),
                    HostilityDefault = fd.HostilityDefault,
                    Metrics = new FactionMetrics
                    {
                        MilitaryStrength = fd.Metrics?.MilitaryStrength ?? 3,
                        EconomicPower = fd.Metrics?.EconomicPower ?? 3,
                        Influence = fd.Metrics?.Influence ?? 3,
                        Desperation = fd.Metrics?.Desperation ?? 1,
                        Corruption = fd.Metrics?.Corruption ?? 2
                    }
                };
                factions[fd.Id] = faction;
            }

            GD.Print($"[FactionRegistry] Loaded {factions.Count} factions");
        }
        catch (Exception e)
        {
            GD.PrintErr($"[FactionRegistry] Error loading factions: {e.Message}");
            LoadDefaults();
        }

        isLoaded = true;
    }

    private static void LoadDefaults()
    {
        factions["corp"] = new Faction("corp", "Helix Corp", FactionType.Corporate)
        {
            Color = new Color(0.2f, 0.4f, 0.8f),
            Metrics = new FactionMetrics { MilitaryStrength = 3, EconomicPower = 5, Influence = 4 }
        };
        factions["rebels"] = new Faction("rebels", "Free Colonies", FactionType.Independent)
        {
            Color = new Color(0.2f, 0.7f, 0.3f),
            Metrics = new FactionMetrics { MilitaryStrength = 2, EconomicPower = 2, Influence = 2 }
        };
        factions["pirates"] = new Faction("pirates", "Red Claw", FactionType.Criminal)
        {
            Color = new Color(0.8f, 0.2f, 0.2f),
            Metrics = new FactionMetrics { MilitaryStrength = 3, EconomicPower = 2, Influence = 1 }
        };
        isLoaded = true;
    }
}

// JSON deserialization classes
internal class FactionFileData
{
    public List<FactionJsonData> Factions { get; set; }
}

internal class FactionJsonData
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public ColorJsonData Color { get; set; }
    public int HostilityDefault { get; set; }
    public FactionMetricsJsonData Metrics { get; set; }
}

internal class ColorJsonData
{
    public float R { get; set; }
    public float G { get; set; }
    public float B { get; set; }
}

internal class FactionMetricsJsonData
{
    public int MilitaryStrength { get; set; }
    public int EconomicPower { get; set; }
    public int Influence { get; set; }
    public int Desperation { get; set; }
    public int Corruption { get; set; }
}
