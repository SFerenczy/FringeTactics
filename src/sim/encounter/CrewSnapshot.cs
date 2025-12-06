using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Lightweight crew snapshot for encounter evaluation.
/// Captures effective stats at a point in time.
/// </summary>
public class CrewSnapshot
{
    public int Id { get; set; }
    public string Name { get; set; }
    public List<string> TraitIds { get; set; } = new();
    public int Grit { get; set; }
    public int Reflexes { get; set; }
    public int Aim { get; set; }
    public int Tech { get; set; }
    public int Savvy { get; set; }
    public int Resolve { get; set; }

    public int GetStat(CrewStatType stat) => stat switch
    {
        CrewStatType.Grit => Grit,
        CrewStatType.Reflexes => Reflexes,
        CrewStatType.Aim => Aim,
        CrewStatType.Tech => Tech,
        CrewStatType.Savvy => Savvy,
        CrewStatType.Resolve => Resolve,
        _ => 0
    };

    public static CrewSnapshot From(CrewMember crew)
    {
        if (crew == null) return null;

        return new CrewSnapshot
        {
            Id = crew.Id,
            Name = crew.Name,
            TraitIds = new List<string>(crew.TraitIds ?? new List<string>()),
            Grit = crew.GetEffectiveStat(CrewStatType.Grit),
            Reflexes = crew.GetEffectiveStat(CrewStatType.Reflexes),
            Aim = crew.GetEffectiveStat(CrewStatType.Aim),
            Tech = crew.GetEffectiveStat(CrewStatType.Tech),
            Savvy = crew.GetEffectiveStat(CrewStatType.Savvy),
            Resolve = crew.GetEffectiveStat(CrewStatType.Resolve)
        };
    }
}
