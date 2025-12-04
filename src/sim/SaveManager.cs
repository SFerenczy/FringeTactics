using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FringeTactics;

/// <summary>
/// Handles campaign state serialization/deserialization.
/// File I/O is handled by SaveFileAdapter (Godot layer).
/// </summary>
public static class SaveManager
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Create a SaveData envelope from campaign state.
    /// </summary>
    public static SaveData CreateSaveData(CampaignState campaign, string displayName = null)
    {
        return new SaveData
        {
            Version = SaveVersion.Current,
            SavedAt = DateTime.UtcNow,
            DisplayName = displayName ?? $"{campaign.Sector?.Name ?? "Campaign"} - Day {campaign.Time.CurrentDay}",
            Campaign = campaign.GetState()
        };
    }

    /// <summary>
    /// Serialize SaveData to JSON string.
    /// </summary>
    public static string Serialize(SaveData data)
    {
        return JsonSerializer.Serialize(data, JsonOptions);
    }

    /// <summary>
    /// Deserialize JSON string to SaveData.
    /// </summary>
    public static SaveData Deserialize(string json)
    {
        return JsonSerializer.Deserialize<SaveData>(json, JsonOptions);
    }

    /// <summary>
    /// Restore CampaignState from SaveData.
    /// </summary>
    public static CampaignState RestoreCampaign(SaveData data)
    {
        if (data == null || data.Campaign == null)
        {
            throw new ArgumentException("Invalid save data");
        }

        // Version check
        if (data.Version > SaveVersion.Current)
        {
            throw new InvalidOperationException(
                $"Save file version {data.Version} is newer than supported version {SaveVersion.Current}");
        }

        // Future: Apply migrations for older versions
        // if (data.Version < SaveVersion.Current) { MigrateFrom(data); }

        return CampaignState.FromState(data.Campaign);
    }

    /// <summary>
    /// Validate save data integrity.
    /// </summary>
    public static ValidationResult ValidateSaveData(SaveData data)
    {
        var result = new ValidationResult();

        if (data == null)
        {
            result.AddError("Save data is null");
            return result;
        }

        if (data.Version <= 0)
        {
            result.AddError("Invalid save version");
        }

        if (data.Campaign == null)
        {
            result.AddError("Campaign data is missing");
            return result;
        }

        if (data.Campaign.Time == null)
        {
            result.AddError("Campaign time data is missing");
        }

        if (data.Campaign.Sector == null)
        {
            result.AddError("Sector data is missing");
        }

        if (data.Campaign.Crew == null || data.Campaign.Crew.Count == 0)
        {
            result.AddWarning("No crew data found");
        }

        return result;
    }
}
