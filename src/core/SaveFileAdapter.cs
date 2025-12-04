using Godot;
using System;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Handles save file I/O using Godot's FileAccess.
/// </summary>
public static class SaveFileAdapter
{
    private const string SaveDirectory = "user://saves/";
    private const string SaveExtension = ".json";
    private const string AutosaveName = "autosave";
    private const int MaxSlots = 5;

    /// <summary>
    /// Get the full path for a save slot.
    /// </summary>
    public static string GetSavePath(int slot)
    {
        return $"{SaveDirectory}slot{slot}{SaveExtension}";
    }

    /// <summary>
    /// Get the autosave path.
    /// </summary>
    public static string GetAutosavePath()
    {
        return $"{SaveDirectory}{AutosaveName}{SaveExtension}";
    }

    /// <summary>
    /// Ensure save directory exists.
    /// </summary>
    public static void EnsureSaveDirectory()
    {
        var globalPath = ProjectSettings.GlobalizePath(SaveDirectory);
        if (!DirAccess.DirExistsAbsolute(globalPath))
        {
            var err = DirAccess.MakeDirRecursiveAbsolute(globalPath);
            if (err != Error.Ok)
            {
                GD.PrintErr($"[SaveFileAdapter] Failed to create save directory: {err}");
            }
        }
    }

    /// <summary>
    /// Save campaign to a slot.
    /// </summary>
    public static bool Save(CampaignState campaign, int slot)
    {
        if (slot < 1 || slot > MaxSlots)
        {
            GD.PrintErr($"[SaveFileAdapter] Invalid slot number: {slot}");
            return false;
        }
        return SaveToPath(campaign, GetSavePath(slot));
    }

    /// <summary>
    /// Save campaign to autosave.
    /// </summary>
    public static bool Autosave(CampaignState campaign)
    {
        return SaveToPath(campaign, GetAutosavePath(), "Autosave");
    }

    /// <summary>
    /// Save campaign to a specific path.
    /// </summary>
    private static bool SaveToPath(CampaignState campaign, string path, string displayName = null)
    {
        try
        {
            EnsureSaveDirectory();

            var saveData = SaveManager.CreateSaveData(campaign, displayName);
            var json = SaveManager.Serialize(saveData);

            using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
            if (file == null)
            {
                var error = FileAccess.GetOpenError();
                GD.PrintErr($"[SaveFileAdapter] Failed to open file for writing: {path} (Error: {error})");
                return false;
            }

            file.StoreString(json);
            GD.Print($"[SaveFileAdapter] Saved to {path}");
            return true;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[SaveFileAdapter] Save failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Load campaign from a slot.
    /// </summary>
    public static CampaignState Load(int slot)
    {
        if (slot < 1 || slot > MaxSlots)
        {
            GD.PrintErr($"[SaveFileAdapter] Invalid slot number: {slot}");
            return null;
        }
        return LoadFromPath(GetSavePath(slot));
    }

    /// <summary>
    /// Load campaign from autosave.
    /// </summary>
    public static CampaignState LoadAutosave()
    {
        return LoadFromPath(GetAutosavePath());
    }

    /// <summary>
    /// Load campaign from a specific path.
    /// </summary>
    private static CampaignState LoadFromPath(string path)
    {
        try
        {
            if (!FileAccess.FileExists(path))
            {
                GD.Print($"[SaveFileAdapter] Save file not found: {path}");
                return null;
            }

            using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
            if (file == null)
            {
                var error = FileAccess.GetOpenError();
                GD.PrintErr($"[SaveFileAdapter] Failed to open file for reading: {path} (Error: {error})");
                return null;
            }

            var json = file.GetAsText();
            var saveData = SaveManager.Deserialize(json);

            // Validate
            var validation = SaveManager.ValidateSaveData(saveData);
            if (!validation.IsValid)
            {
                foreach (var error in validation.Errors)
                {
                    GD.PrintErr($"[SaveFileAdapter] Validation error: {error}");
                }
                return null;
            }

            foreach (var warning in validation.Warnings)
            {
                GD.Print($"[SaveFileAdapter] Warning: {warning}");
            }

            var campaign = SaveManager.RestoreCampaign(saveData);
            GD.Print($"[SaveFileAdapter] Loaded from {path}");
            return campaign;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[SaveFileAdapter] Load failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Check if a save slot exists.
    /// </summary>
    public static bool SaveExists(int slot)
    {
        if (slot < 1 || slot > MaxSlots) return false;
        return FileAccess.FileExists(GetSavePath(slot));
    }

    /// <summary>
    /// Check if autosave exists.
    /// </summary>
    public static bool AutosaveExists()
    {
        return FileAccess.FileExists(GetAutosavePath());
    }

    /// <summary>
    /// Delete a save slot.
    /// </summary>
    public static bool DeleteSave(int slot)
    {
        if (slot < 1 || slot > MaxSlots) return false;

        var path = GetSavePath(slot);
        if (FileAccess.FileExists(path))
        {
            var globalPath = ProjectSettings.GlobalizePath(path);
            var err = DirAccess.RemoveAbsolute(globalPath);
            if (err == Error.Ok)
            {
                GD.Print($"[SaveFileAdapter] Deleted {path}");
                return true;
            }
            GD.PrintErr($"[SaveFileAdapter] Failed to delete {path}: {err}");
        }
        return false;
    }

    /// <summary>
    /// Get metadata for a save slot (without loading full campaign).
    /// </summary>
    public static SaveMetadata GetSaveMetadata(int slot)
    {
        if (slot < 1 || slot > MaxSlots) return null;
        return GetMetadataFromPath(GetSavePath(slot));
    }

    /// <summary>
    /// Get metadata for autosave.
    /// </summary>
    public static SaveMetadata GetAutosaveMetadata()
    {
        return GetMetadataFromPath(GetAutosavePath());
    }

    private static SaveMetadata GetMetadataFromPath(string path, bool fileExistsAlreadyChecked = false)
    {
        try
        {
            if (!fileExistsAlreadyChecked && !FileAccess.FileExists(path))
            {
                return null;
            }

            using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
            if (file == null) return null;

            var json = file.GetAsText();
            var saveData = SaveManager.Deserialize(json);

            return new SaveMetadata
            {
                DisplayName = saveData.DisplayName,
                SavedAt = saveData.SavedAt,
                Version = saveData.Version,
                Day = saveData.Campaign?.Time?.CurrentDay ?? 0,
                CrewCount = saveData.Campaign?.Crew?.Count ?? 0
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Get list of all available saves.
    /// </summary>
    public static List<SaveSlotInfo> GetAllSaves()
    {
        var saves = new List<SaveSlotInfo>();

        // Check autosave (single existence check)
        var autosavePath = GetAutosavePath();
        var autosaveExists = FileAccess.FileExists(autosavePath);
        saves.Add(new SaveSlotInfo
        {
            Slot = 0,
            IsAutosave = true,
            Exists = autosaveExists,
            Metadata = autosaveExists ? GetMetadataFromPath(autosavePath, fileExistsAlreadyChecked: true) : null
        });

        // Check numbered slots (single existence check per slot)
        for (int i = 1; i <= MaxSlots; i++)
        {
            var slotPath = GetSavePath(i);
            var slotExists = FileAccess.FileExists(slotPath);
            saves.Add(new SaveSlotInfo
            {
                Slot = i,
                IsAutosave = false,
                Exists = slotExists,
                Metadata = slotExists ? GetMetadataFromPath(slotPath, fileExistsAlreadyChecked: true) : null
            });
        }

        return saves;
    }
}
