using System;
using System.Collections.Generic;
using System.Linq;

namespace Bannerlord.LauncherManager.Models;

/// <summary>
/// Compatibility status between save and current setup.
/// </summary>
public enum SaveEditorCompatibility
{
    Compatible,
    MinorIssues,
    MajorIssues,
    Incompatible,
    Unknown
}

/// <summary>
/// Type of entity in the save file.
/// </summary>
public enum SaveEntityType
{
    Hero,
    Party,
    Settlement,
    Clan,
    Kingdom,
    Fleet,
    Ship,
    Item,
    Quest,
    Workshop,
    Caravan
}

/// <summary>
/// Validation severity for save edits.
/// </summary>
public enum EditValidationSeverity
{
    Info,
    Warning,
    Error,
    Critical
}

/// <summary>
/// Save file header information.
/// </summary>
public class SaveHeader
{
    /// <summary>
    /// Header format version.
    /// </summary>
    public int HeaderVersion { get; set; }

    /// <summary>
    /// Game version major.
    /// </summary>
    public int GameVersionMajor { get; set; }

    /// <summary>
    /// Game version minor.
    /// </summary>
    public int GameVersionMinor { get; set; }

    /// <summary>
    /// Game version build.
    /// </summary>
    public int GameVersionBuild { get; set; }

    /// <summary>
    /// Full game version string.
    /// </summary>
    public string GameVersion => $"{GameVersionMajor}.{GameVersionMinor}.{GameVersionBuild}";

    /// <summary>
    /// Modules used in save.
    /// </summary>
    public List<SaveModuleInfo> Modules { get; set; } = new();
}

/// <summary>
/// Module info from save header.
/// </summary>
public class SaveModuleInfo
{
    /// <summary>
    /// Module ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Module version when saved.
    /// </summary>
    public string Version { get; set; } = string.Empty;
}

/// <summary>
/// Save file metadata.
/// </summary>
public class SaveMetadataInfo
{
    /// <summary>
    /// Character name.
    /// </summary>
    public string CharacterName { get; set; } = string.Empty;

    /// <summary>
    /// Clan name.
    /// </summary>
    public string? ClanName { get; set; }

    /// <summary>
    /// Game day.
    /// </summary>
    public int GameDay { get; set; }

    /// <summary>
    /// Game year.
    /// </summary>
    public int GameYear { get; set; }

    /// <summary>
    /// Character level.
    /// </summary>
    public int CharacterLevel { get; set; }

    /// <summary>
    /// Total playtime in hours.
    /// </summary>
    public float PlaytimeHours { get; set; }

    /// <summary>
    /// When save was created.
    /// </summary>
    public DateTime SaveTimestamp { get; set; }
}

/// <summary>
/// Hero attributes.
/// </summary>
public class HeroAttributes
{
    public int Vigor { get; set; }
    public int Control { get; set; }
    public int Endurance { get; set; }
    public int Cunning { get; set; }
    public int Social { get; set; }
    public int Intelligence { get; set; }

    public int Total => Vigor + Control + Endurance + Cunning + Social + Intelligence;
}

/// <summary>
/// Hero skill data.
/// </summary>
public class HeroSkillData
{
    public string SkillId { get; set; } = string.Empty;
    public string SkillName { get; set; } = string.Empty;
    public int Level { get; set; }
    public int Focus { get; set; }
    public int MaxLevel { get; set; } = 300;
}

/// <summary>
/// Hero data from save.
/// </summary>
public class SaveHeroData
{
    /// <summary>
    /// Internal ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// String ID.
    /// </summary>
    public string StringId { get; set; } = string.Empty;

    /// <summary>
    /// Display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Character level.
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// Experience points.
    /// </summary>
    public int Experience { get; set; }

    /// <summary>
    /// Gold carried.
    /// </summary>
    public int Gold { get; set; }

    /// <summary>
    /// Current health.
    /// </summary>
    public int Health { get; set; }

    /// <summary>
    /// Maximum health.
    /// </summary>
    public int MaxHealth { get; set; }

    /// <summary>
    /// Character attributes.
    /// </summary>
    public HeroAttributes Attributes { get; set; } = new();

    /// <summary>
    /// Character skills.
    /// </summary>
    public List<HeroSkillData> Skills { get; set; } = new();

    /// <summary>
    /// Unlocked perks.
    /// </summary>
    public List<string> Perks { get; set; } = new();

    /// <summary>
    /// Is this the main character.
    /// </summary>
    public bool IsMainHero { get; set; }

    /// <summary>
    /// Is deceased.
    /// </summary>
    public bool IsDead { get; set; }

    /// <summary>
    /// Is prisoner.
    /// </summary>
    public bool IsPrisoner { get; set; }

    /// <summary>
    /// Clan ID reference.
    /// </summary>
    public string? ClanId { get; set; }

    /// <summary>
    /// Party ID reference.
    /// </summary>
    public string? PartyId { get; set; }
}

/// <summary>
/// Party troop stack.
/// </summary>
public class TroopStackData
{
    public string TroopId { get; set; } = string.Empty;
    public string TroopName { get; set; } = string.Empty;
    public int Count { get; set; }
    public int WoundedCount { get; set; }
    public int Tier { get; set; }
}

/// <summary>
/// Party data from save.
/// </summary>
public class SavePartyData
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? LeaderId { get; set; }
    public List<TroopStackData> Troops { get; set; } = new();
    public List<TroopStackData> Prisoners { get; set; } = new();
    public int Gold { get; set; }
    public int TotalTroops => Troops.Sum(t => t.Count);
    public int TotalPrisoners => Prisoners.Sum(p => p.Count);
}

/// <summary>
/// Fleet data (War Sails).
/// </summary>
public class SaveFleetData
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? CommanderId { get; set; }
    public List<string> ShipIds { get; set; } = new();
    public string? CurrentPortId { get; set; }
    public string State { get; set; } = "Docked";
    public int TotalShips => ShipIds.Count;
}

/// <summary>
/// Ship data (War Sails).
/// </summary>
public class SaveShipData
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ShipType { get; set; } = string.Empty;
    public int CurrentHull { get; set; }
    public int MaxHull { get; set; }
    public int CrewCount { get; set; }
    public int MaxCrew { get; set; }
    public int CargoCapacity { get; set; }
    public int CargoUsed { get; set; }
    public List<string> Upgrades { get; set; } = new();
    public float HullPercent => MaxHull > 0 ? (float)CurrentHull / MaxHull * 100 : 0;
}

/// <summary>
/// Edit validation result.
/// </summary>
public class EditValidation
{
    public EditValidationSeverity Severity { get; set; }
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Suggestion { get; set; }
}

/// <summary>
/// Result of an edit operation.
/// </summary>
public class EditResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public List<EditValidation> Validations { get; set; } = new();
    public bool HasErrors => Validations.Any(v => v.Severity == EditValidationSeverity.Error || 
                                                   v.Severity == EditValidationSeverity.Critical);
}

/// <summary>
/// Complete save file data for editing.
/// </summary>
public class SaveEditData
{
    /// <summary>
    /// File path.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// File name.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Header information.
    /// </summary>
    public SaveHeader Header { get; set; } = new();

    /// <summary>
    /// Metadata.
    /// </summary>
    public SaveMetadataInfo Metadata { get; set; } = new();

    /// <summary>
    /// Main hero.
    /// </summary>
    public SaveHeroData? MainHero { get; set; }

    /// <summary>
    /// All heroes.
    /// </summary>
    public List<SaveHeroData> Heroes { get; set; } = new();

    /// <summary>
    /// All parties.
    /// </summary>
    public List<SavePartyData> Parties { get; set; } = new();

    /// <summary>
    /// Has War Sails expansion data.
    /// </summary>
    public bool HasWarSails { get; set; }

    /// <summary>
    /// Fleets (War Sails).
    /// </summary>
    public List<SaveFleetData> Fleets { get; set; } = new();

    /// <summary>
    /// Ships (War Sails).
    /// </summary>
    public List<SaveShipData> Ships { get; set; } = new();

    /// <summary>
    /// Checksum for integrity.
    /// </summary>
    public string? Checksum { get; set; }

    /// <summary>
    /// Is data modified.
    /// </summary>
    public bool IsModified { get; set; }
}

/// <summary>
/// Options for loading a save.
/// </summary>
public class SaveLoadOptions
{
    /// <summary>
    /// Load in permissive mode (ignore minor errors).
    /// </summary>
    public bool Permissive { get; set; }

    /// <summary>
    /// Only load metadata (faster).
    /// </summary>
    public bool MetadataOnly { get; set; }

    /// <summary>
    /// Validate references after load.
    /// </summary>
    public bool ValidateReferences { get; set; } = true;
}

/// <summary>
/// Options for saving.
/// </summary>
public class SaveWriteOptions
{
    /// <summary>
    /// Create backup before saving.
    /// </summary>
    public bool CreateBackup { get; set; } = true;

    /// <summary>
    /// Verify integrity after save.
    /// </summary>
    public bool VerifyAfterSave { get; set; } = true;

    /// <summary>
    /// Compression level.
    /// </summary>
    public string CompressionLevel { get; set; } = "Optimal";
}

/// <summary>
/// Hero edit request.
/// </summary>
public class HeroEditRequest
{
    public string HeroId { get; set; } = string.Empty;
    public int? Level { get; set; }
    public int? Experience { get; set; }
    public int? Gold { get; set; }
    public int? Health { get; set; }
    public HeroAttributes? Attributes { get; set; }
    public Dictionary<string, int>? SkillLevels { get; set; }
    public List<string>? PerksToAdd { get; set; }
    public List<string>? PerksToRemove { get; set; }
}

/// <summary>
/// Party edit request.
/// </summary>
public class PartyEditRequest
{
    public string PartyId { get; set; } = string.Empty;
    public int? Gold { get; set; }
    public Dictionary<string, int>? TroopsToAdd { get; set; }
    public Dictionary<string, int>? TroopsToRemove { get; set; }
    public bool? HealAllWounded { get; set; }
}

/// <summary>
/// Ship edit request (War Sails).
/// </summary>
public class ShipEditRequest
{
    public string ShipId { get; set; } = string.Empty;
    public string? Name { get; set; }
    public int? CurrentHull { get; set; }
    public int? CrewCount { get; set; }
    public List<string>? UpgradesToAdd { get; set; }
    public List<string>? UpgradesToRemove { get; set; }
    public bool? RepairFully { get; set; }
}
