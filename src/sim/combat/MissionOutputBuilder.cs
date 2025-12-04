using System.Collections.Generic;
using System.Linq;

namespace FringeTactics;

/// <summary>
/// Builds a MissionOutput from the final CombatState.
/// Stateless utility for extracting mission results.
/// </summary>
public static class MissionOutputBuilder
{
    /// <summary>
    /// Build complete mission output from combat state.
    /// </summary>
    /// <param name="combat">The completed combat state</param>
    /// <param name="outcome">How the mission ended</param>
    /// <param name="actorToCrewMap">Mapping from actor IDs to campaign crew IDs</param>
    public static MissionOutput Build(
        CombatState combat,
        MissionOutcome outcome,
        Dictionary<int, int> actorToCrewMap)
    {
        var output = new MissionOutput
        {
            Outcome = outcome,
            MissionId = combat.MissionConfig?.Id ?? "unknown",
            TicksElapsed = combat.TimeSystem.CurrentTick,
            MissionDurationSeconds = combat.TimeSystem.CurrentTick * TimeSystem.TickDuration,
            AlarmTriggered = combat.Perception.AlarmState == AlarmState.Alerted
        };

        // Count enemies
        foreach (var actor in combat.Actors)
        {
            if (actor.Type != ActorType.Enemy)
            {
                continue;
            }

            if (actor.State == ActorState.Dead)
            {
                output.EnemiesKilled++;
            }
            else
            {
                output.EnemiesRemaining++;
            }
        }

        // Build crew outcomes
        foreach (var actor in combat.Actors)
        {
            if (actor.Type != ActorType.Crew)
            {
                continue;
            }

            var crewId = actorToCrewMap.TryGetValue(actor.Id, out var id) ? id : -1;
            var crewOutcome = BuildCrewOutcome(actor, crewId, combat, outcome);
            output.CrewOutcomes.Add(crewOutcome);
        }

        // Build objective results
        BuildObjectiveResults(output, combat, outcome);

        return output;
    }

    private static CrewOutcome BuildCrewOutcome(
        Actor actor,
        int campaignCrewId,
        CombatState combat,
        MissionOutcome missionOutcome)
    {
        var outcome = new CrewOutcome
        {
            CampaignCrewId = campaignCrewId,
            Name = actor.Name ?? $"Crew #{actor.Id}",
            FinalHp = actor.Hp,
            MaxHp = actor.MaxHp,
            DamageTaken = actor.TotalDamageTaken,
            Kills = actor.Kills,
            ShotsFired = actor.ShotsFired,
            ShotsHit = actor.ShotsHit
        };

        // Determine status
        if (actor.State == ActorState.Dead)
        {
            outcome.Status = CrewFinalStatus.Dead;
        }
        else if (missionOutcome == MissionOutcome.Retreat &&
                 !combat.MapState.IsInEntryZone(actor.GridPosition))
        {
            outcome.Status = CrewFinalStatus.MIA;
        }
        else if (actor.Hp <= actor.MaxHp * 0.25f)
        {
            outcome.Status = CrewFinalStatus.Critical;
        }
        else if (actor.Hp <= actor.MaxHp * 0.5f)
        {
            outcome.Status = CrewFinalStatus.Wounded;
        }
        else
        {
            outcome.Status = CrewFinalStatus.Alive;
        }

        // Calculate ammo usage
        outcome.AmmoRemaining = actor.CurrentMagazine + actor.ReserveAmmo;
        outcome.AmmoUsed = actor.AmmoUsed;

        // Suggest XP based on performance
        outcome.SuggestedXp = CalculateSuggestedXp(outcome, missionOutcome);

        // Add injuries based on status
        if (outcome.Status == CrewFinalStatus.Wounded)
        {
            outcome.NewInjuries.Add("wounded");
        }
        else if (outcome.Status == CrewFinalStatus.Critical)
        {
            outcome.NewInjuries.Add("critical_wound");
        }

        return outcome;
    }

    private static int CalculateSuggestedXp(CrewOutcome crew, MissionOutcome outcome)
    {
        int xp = 0;

        // Base participation XP (only if survived and extracted)
        if (crew.Status != CrewFinalStatus.Dead && crew.Status != CrewFinalStatus.MIA)
        {
            xp += CampaignState.XP_PARTICIPATION;
        }

        // Kill XP
        xp += crew.Kills * CampaignState.XP_PER_KILL;

        // Victory bonus
        if (outcome == MissionOutcome.Victory)
        {
            xp += CampaignState.XP_VICTORY_BONUS;
        }
        else if (outcome == MissionOutcome.Retreat)
        {
            xp += CampaignState.XP_RETREAT_BONUS;
        }

        return xp;
    }

    /// <summary>
    /// PLACEHOLDER: Build objective results based on mission outcome.
    /// Currently uses simple outcome-based logic. Will be replaced when
    /// objective tracking system is implemented.
    /// TODO: Read from CombatState.Objectives when objective system exists.
    /// </summary>
    private static void BuildObjectiveResults(MissionOutput output, CombatState combat, MissionOutcome outcome)
    {
        
        if (outcome == MissionOutcome.Victory)
        {
            output.ObjectiveResults["primary"] = ObjectiveStatus.Complete;
        }
        else if (outcome == MissionOutcome.Defeat)
        {
            output.ObjectiveResults["primary"] = ObjectiveStatus.Failed;
        }
        else if (outcome == MissionOutcome.Retreat)
        {
            // Retreat means primary objective not completed
            output.ObjectiveResults["primary"] = ObjectiveStatus.Failed;
        }
        else
        {
            output.ObjectiveResults["primary"] = ObjectiveStatus.Pending;
        }
    }
}
