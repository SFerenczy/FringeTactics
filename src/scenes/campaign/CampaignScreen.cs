using Godot;
using System.Collections.Generic;
using System.Linq;

namespace FringeTactics;

/// <summary>
/// Campaign HQ screen showing resources, crew roster, and navigation.
/// UI layout is defined in CampaignScreen.tscn.
/// </summary>
public partial class CampaignScreen : Control
{
    // Scene node references (using %UniqueNames)
    private Label resourcesLabel;
    private Label statsLabel;
    private Label missionCostLabel;
    private VBoxContainer crewContainer;
    private Button backToSectorButton;
    private Button abandonButton;

    // Detail panel references
    private PanelContainer crewDetailPanel;
    private Label detailNameLabel;
    private Label detailRoleLabel;
    private Label detailLevelLabel;
    private GridContainer statsContainer;
    private VBoxContainer traitsContainer;
    private VBoxContainer injuriesContainer;
    private Button closeDetailButton;
    private Button fireButton;
    private ConfirmationDialog confirmFireDialog;

    // Equipment UI references
    private Label weaponItemLabel;
    private Label armorItemLabel;
    private Label gadgetItemLabel;
    private Label statBonusLabel;
    private PopupPanel equipmentPopup;
    private Label equipmentPopupTitle;
    private ItemList equipmentItemList;
    private Label statPreviewLabel;
    private Button equipButton;
    private Button unequipButton;

    // State
    private int? selectedCrewId;
    private EquipSlot selectedEquipSlot;
    private string selectedItemId;

    private CampaignState Campaign => GameState.Instance?.Campaign;

    public override void _Ready()
    {
        GetNodeReferences();
        ConnectSignals();
        UpdateDisplay();
    }

    private void GetNodeReferences()
    {
        resourcesLabel = GetNode<Label>("%ResourcesLabel");
        statsLabel = GetNode<Label>("%StatsLabel");
        missionCostLabel = GetNode<Label>("%MissionCostLabel");
        crewContainer = GetNode<VBoxContainer>("%CrewContainer");
        backToSectorButton = GetNode<Button>("%BackToSectorButton");
        abandonButton = GetNode<Button>("%AbandonButton");

        // Detail panel
        crewDetailPanel = GetNode<PanelContainer>("%CrewDetailPanel");
        detailNameLabel = GetNode<Label>("%DetailNameLabel");
        detailRoleLabel = GetNode<Label>("%DetailRoleLabel");
        detailLevelLabel = GetNode<Label>("%DetailLevelLabel");
        statsContainer = GetNode<GridContainer>("%StatsContainer");
        traitsContainer = GetNode<VBoxContainer>("%TraitsContainer");
        injuriesContainer = GetNode<VBoxContainer>("%InjuriesContainer");
        closeDetailButton = GetNode<Button>("%CloseDetailButton");
        fireButton = GetNode<Button>("%FireButton");
        confirmFireDialog = GetNode<ConfirmationDialog>("%ConfirmFireDialog");

        // Equipment section
        weaponItemLabel = GetNode<Label>("%WeaponItemLabel");
        armorItemLabel = GetNode<Label>("%ArmorItemLabel");
        gadgetItemLabel = GetNode<Label>("%GadgetItemLabel");
        statBonusLabel = GetNode<Label>("%StatBonusLabel");

        // Equipment popup
        equipmentPopup = GetNode<PopupPanel>("%EquipmentPopup");
        equipmentPopupTitle = GetNode<Label>("%EquipmentPopupTitle");
        equipmentItemList = GetNode<ItemList>("%EquipmentItemList");
        statPreviewLabel = GetNode<Label>("%StatPreviewLabel");
        equipButton = GetNode<Button>("%EquipButton");
        unequipButton = GetNode<Button>("%UnequipButton");
    }

    private void ConnectSignals()
    {
        backToSectorButton.Pressed += OnBackToSectorPressed;
        abandonButton.Pressed += OnMainMenuPressed;
        closeDetailButton.Pressed += OnCloseDetailPressed;
        fireButton.Pressed += OnFireButtonPressed;
        confirmFireDialog.Confirmed += OnFireConfirmed;

        // Equipment signals
        GetNode<Button>("%WeaponChangeButton").Pressed += () => OpenEquipmentPopup(EquipSlot.Weapon);
        GetNode<Button>("%ArmorChangeButton").Pressed += () => OpenEquipmentPopup(EquipSlot.Armor);
        GetNode<Button>("%GadgetChangeButton").Pressed += () => OpenEquipmentPopup(EquipSlot.Gadget);
        equipmentItemList.ItemSelected += OnEquipmentItemSelected;
        equipButton.Pressed += OnEquipPressed;
        unequipButton.Pressed += OnUnequipPressed;
        GetNode<Button>("%EquipCancelButton").Pressed += () => equipmentPopup.Hide();
    }

    public override void _Process(double delta)
    {
        UpdateResourceDisplay();
    }

    private void UpdateResourceDisplay()
    {
        if (Campaign == null) return;

        resourcesLabel.Text = $"Money: ${Campaign.Money}\n" +
                              $"Fuel: {Campaign.Fuel}\n" +
                              $"Ammo: {Campaign.Ammo}\n" +
                              $"Parts: {Campaign.Parts}\n" +
                              $"Meds: {Campaign.Meds}";
    }

    private void UpdateDisplay()
    {
        if (Campaign == null)
        {
            resourcesLabel.Text = "No active campaign!";
            return;
        }

        UpdateResourceDisplay();

        // Mission cost
        var missionConfig = CampaignConfig.Instance.Mission;
        missionCostLabel.Text = $"Mission cost: {missionConfig.FuelCost} fuel";

        // Stats
        statsLabel.Text = $"Missions: {Campaign.MissionsCompleted} won, {Campaign.MissionsFailed} lost";

        // Update crew roster
        UpdateCrewRoster(Campaign);
    }

    private void UpdateCrewRoster(CampaignState campaign)
    {
        ClearChildren(crewContainer);

        foreach (var crew in campaign.Crew)
        {
            var crewRow = CreateCrewListEntry(crew, campaign);
            crewContainer.AddChild(crewRow);
        }
    }

    private HBoxContainer CreateCrewListEntry(CrewMember crew, CampaignState campaign)
    {
        var crewRow = new HBoxContainer();
        crewRow.AddThemeConstantOverride("separation", 10);

        // Status indicator
        var statusColor = crew.IsDead ? Colors.Red :
                          crew.Injuries.Count > 0 ? Colors.Orange :
                          Colors.Green;

        var statusDot = new ColorRect();
        statusDot.CustomMinimumSize = new Vector2(12, 12);
        statusDot.Color = statusColor;
        crewRow.AddChild(statusDot);

        // Clickable crew button
        var crewButton = new Button();
        crewButton.Flat = true;
        crewButton.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        // Show name, role, and key stats (Aim, Grit, Tech)
        var aimVal = crew.GetEffectiveStat(CrewStatType.Aim);
        var gritVal = crew.GetEffectiveStat(CrewStatType.Grit);
        var techVal = crew.GetEffectiveStat(CrewStatType.Tech);
        crewButton.Text = $"{crew.Name} - {crew.Role} | Aim:{aimVal} Grit:{gritVal} Tech:{techVal}";

        if (crew.IsDead)
        {
            crewButton.AddThemeColorOverride("font_color", Colors.Gray);
        }

        var crewId = crew.Id;
        crewButton.Pressed += () => SelectCrew(crewId);
        crewRow.AddChild(crewButton);

        // Heal button if injured and have meds
        if (!crew.IsDead && crew.Injuries.Count > 0 && campaign.Meds > 0)
        {
            var healBtn = new Button();
            healBtn.Text = "Heal";
            healBtn.CustomMinimumSize = new Vector2(60, 30);
            healBtn.Pressed += () => OnHealPressed(crewId);
            crewRow.AddChild(healBtn);
        }

        return crewRow;
    }

    private void SelectCrew(int crewId)
    {
        selectedCrewId = crewId;
        UpdateDetailPanel();
    }

    private CrewMember GetSelectedCrew()
    {
        if (selectedCrewId == null) return null;
        return Campaign?.Crew.Find(c => c.Id == selectedCrewId.Value);
    }

    private void UpdateDetailPanel()
    {
        var crew = GetSelectedCrew();
        if (crew == null)
        {
            selectedCrewId = null;
            crewDetailPanel.Visible = false;
            return;
        }

        crewDetailPanel.Visible = true;

        // Header
        detailNameLabel.Text = crew.Name;
        detailRoleLabel.Text = crew.Role.ToString();

        // Level/XP
        var xpPerLevel = CampaignConfig.Instance.Crew.XpPerLevel;
        detailLevelLabel.Text = $"Level {crew.Level} ({crew.Xp}/{xpPerLevel} XP)";

        // Stats
        UpdateStatsDisplay(crew);

        // Traits
        UpdateTraitsDisplay(crew);

        // Injuries
        UpdateInjuriesDisplay(crew);

        // Equipment (MG-UI3)
        UpdateEquipmentDisplay();

        // Fire button state
        UpdateFireButtonState(crew);
    }

    private void UpdateFireButtonState(CrewMember crew)
    {
        if (Campaign == null) return;

        if (crew.IsDead)
        {
            fireButton.Text = "Bury";
            fireButton.TooltipText = "Remove dead crew member from roster";
            fireButton.Disabled = false;
        }
        else if (Campaign.GetAliveCrew().Count <= 1)
        {
            fireButton.Text = "Dismiss";
            fireButton.TooltipText = "Cannot dismiss last crew member";
            fireButton.Disabled = true;
        }
        else
        {
            fireButton.Text = "Dismiss";
            fireButton.TooltipText = "Remove crew member from roster";
            fireButton.Disabled = false;
        }
    }

    private void UpdateStatsDisplay(CrewMember crew)
    {
        ClearChildren(statsContainer);

        var stats = new[] {
            ("Grit", CrewStatType.Grit),
            ("Reflexes", CrewStatType.Reflexes),
            ("Aim", CrewStatType.Aim),
            ("Tech", CrewStatType.Tech),
            ("Savvy", CrewStatType.Savvy),
            ("Resolve", CrewStatType.Resolve)
        };

        foreach (var (name, statType) in stats)
        {
            var baseStat = crew.GetBaseStat(statType);
            var effectiveStat = crew.GetEffectiveStat(statType);
            var modifier = effectiveStat - baseStat;

            var nameLabel = new Label();
            nameLabel.Text = name;
            nameLabel.CustomMinimumSize = new Vector2(80, 0);
            statsContainer.AddChild(nameLabel);

            var valueLabel = new Label();
            if (modifier != 0)
            {
                var modSign = modifier > 0 ? "+" : "";
                valueLabel.Text = $"{effectiveStat} ({baseStat}{modSign}{modifier})";
                valueLabel.AddThemeColorOverride("font_color", modifier > 0 ? Colors.Green : Colors.Red);
            }
            else
            {
                valueLabel.Text = effectiveStat.ToString();
            }
            statsContainer.AddChild(valueLabel);
        }
    }

    private void UpdateTraitsDisplay(CrewMember crew)
    {
        ClearChildren(traitsContainer);

        var traits = crew.GetTraits().ToList();
        if (traits.Count == 0)
        {
            var noTraitsLabel = new Label();
            noTraitsLabel.Text = "No traits";
            noTraitsLabel.AddThemeColorOverride("font_color", Colors.Gray);
            traitsContainer.AddChild(noTraitsLabel);
            return;
        }

        foreach (var trait in traits)
        {
            var traitLabel = new Label();
            traitLabel.Text = trait.Name;

            // Color-code by category
            var color = trait.Category switch
            {
                TraitCategory.Injury => Colors.Red,
                TraitCategory.Personality => Colors.Cyan,
                TraitCategory.Background => Colors.Yellow,
                TraitCategory.Acquired => Colors.Green,
                _ => Colors.White
            };
            traitLabel.AddThemeColorOverride("font_color", color);
            traitLabel.TooltipText = trait.Description;
            traitsContainer.AddChild(traitLabel);
        }
    }

    private void UpdateInjuriesDisplay(CrewMember crew)
    {
        ClearChildren(injuriesContainer);

        if (crew.Injuries.Count == 0)
        {
            var noInjuriesLabel = new Label();
            noInjuriesLabel.Text = "No injuries";
            noInjuriesLabel.AddThemeColorOverride("font_color", Colors.Gray);
            injuriesContainer.AddChild(noInjuriesLabel);
            return;
        }

        foreach (var injury in crew.Injuries)
        {
            var injuryLabel = new Label();
            injuryLabel.Text = $"• {FormatInjuryName(injury)}";
            injuryLabel.AddThemeColorOverride("font_color", Colors.Orange);
            injuriesContainer.AddChild(injuryLabel);
        }
    }

    private static string FormatInjuryName(string injury)
    {
        return injury switch
        {
            InjuryTypes.Wounded => "Wounded (reduced combat effectiveness)",
            InjuryTypes.Critical => "Critical (cannot deploy)",
            InjuryTypes.Concussed => "Concussed (reduced accuracy)",
            InjuryTypes.Bleeding => "Bleeding (needs immediate care)",
            _ => injury
        };
    }

    private static void ClearChildren(Node container)
    {
        foreach (var child in container.GetChildren())
        {
            child.QueueFree();
        }
    }

    private void OnCloseDetailPressed()
    {
        selectedCrewId = null;
        crewDetailPanel.Visible = false;
    }

    private void OnHealPressed(int crewId)
    {
        if (Campaign != null && Campaign.HealCrewMember(crewId))
        {
            UpdateDisplay();
            if (selectedCrewId == crewId)
            {
                UpdateDetailPanel();
            }
        }
    }

    private void OnBackToSectorPressed()
    {
        GD.Print("[CampaignScreen] Returning to sector view...");
        GameState.Instance.GoToSectorView();
    }

    private void OnMainMenuPressed()
    {
        GD.Print("[CampaignScreen] Returning to main menu...");
        GameState.Instance.GoToMainMenu();
    }

    private void OnFireButtonPressed()
    {
        var crew = GetSelectedCrew();
        if (crew == null || Campaign == null) return;

        // Handle dead crew - bury immediately without confirmation
        if (crew.IsDead)
        {
            Campaign.BuryDeadCrew(crew.Id);
            selectedCrewId = null;
            UpdateDetailPanel();
            UpdateCrewRoster(Campaign);
            return;
        }

        // Check if can fire
        if (Campaign.GetAliveCrew().Count <= 1)
        {
            GD.Print("[CampaignScreen] Cannot dismiss last crew member");
            return;
        }

        // Show confirmation dialog
        confirmFireDialog.DialogText = $"Dismiss {crew.Name}?\n\n" +
            $"Role: {crew.Role}\n" +
            $"Level: {crew.Level}\n\n" +
            "This action cannot be undone.";
        confirmFireDialog.PopupCentered();
    }

    private void OnFireConfirmed()
    {
        var crew = GetSelectedCrew();
        if (crew == null || Campaign == null) return;

        if (Campaign.FireCrew(crew.Id))
        {
            GD.Print($"[CampaignScreen] Dismissed {crew.Name}");
            selectedCrewId = null;
            UpdateDetailPanel();
            UpdateCrewRoster(Campaign);
        }
    }

    // === Equipment UI Methods ===

    private void UpdateEquipmentDisplay()
    {
        var crew = GetSelectedCrew();
        if (crew == null || Campaign == null) return;

        var inventory = Campaign.Inventory;

        // Update slot labels
        weaponItemLabel.Text = GetEquippedItemName(crew, EquipSlot.Weapon, inventory);
        armorItemLabel.Text = GetEquippedItemName(crew, EquipSlot.Armor, inventory);
        gadgetItemLabel.Text = GetEquippedItemName(crew, EquipSlot.Gadget, inventory);

        // Update stat bonus summary
        var bonuses = crew.GetEquipmentStatSummary(inventory);
        statBonusLabel.Text = bonuses.Count > 0 
            ? FormatStatBonuses(bonuses) 
            : "No equipment bonuses";
    }

    private string GetEquippedItemName(CrewMember crew, EquipSlot slot, Inventory inventory)
    {
        var itemId = crew.GetEquipped(slot);
        if (string.IsNullOrEmpty(itemId)) return "[Empty]";

        var item = inventory?.FindById(itemId);
        return item?.GetName() ?? "[Missing]";
    }

    private static string FormatStatName(string key)
    {
        return key switch
        {
            EquipmentStats.Armor => "Armor",
            EquipmentStats.Aim => "Aim",
            EquipmentStats.Grit => "Grit",
            EquipmentStats.Reflexes => "Reflexes",
            EquipmentStats.Tech => "Tech",
            EquipmentStats.Savvy => "Savvy",
            EquipmentStats.Resolve => "Resolve",
            EquipmentStats.MaxHp => "HP",
            _ => key
        };
    }

    private static string FormatStatBonuses(Dictionary<string, int> stats)
    {
        if (stats == null || stats.Count == 0)
            return "No stat bonuses";

        var parts = new List<string>();
        foreach (var kvp in stats)
        {
            string sign = kvp.Value >= 0 ? "+" : "";
            parts.Add($"{sign}{kvp.Value} {FormatStatName(kvp.Key)}");
        }
        return string.Join(", ", parts);
    }

    private void OpenEquipmentPopup(EquipSlot slot)
    {
        var crew = GetSelectedCrew();
        if (crew == null || Campaign == null) return;

        selectedEquipSlot = slot;
        selectedItemId = null;

        var inventory = Campaign.Inventory;

        // Set popup title
        equipmentPopupTitle.Text = $"Select {slot}";

        // Populate item list with available items for this slot
        equipmentItemList.Clear();

        var equipment = inventory.GetByCategory(ItemCategory.Equipment);
        foreach (var item in equipment)
        {
            var def = item.GetDef();
            if (def == null || def.EquipSlot != slot) continue;

            // Skip items equipped by other crew
            if (IsEquippedByOther(item.Id, crew.Id)) continue;

            equipmentItemList.AddItem(def.Name);
            equipmentItemList.SetItemMetadata(equipmentItemList.ItemCount - 1, item.Id);
        }

        // Update button states
        var currentEquipped = crew.GetEquipped(slot);
        unequipButton.Disabled = string.IsNullOrEmpty(currentEquipped);
        equipButton.Disabled = true;

        // Clear preview
        statPreviewLabel.Text = "Select an item to see stats";

        equipmentPopup.PopupCentered();
    }

    private bool IsEquippedByOther(string itemId, int currentCrewId)
    {
        if (Campaign == null) return false;
        foreach (var crew in Campaign.Crew)
        {
            if (crew.Id == currentCrewId) continue;
            if (crew.GetAllEquippedIds().Contains(itemId)) return true;
        }
        return false;
    }

    private void OnEquipmentItemSelected(long index)
    {
        selectedItemId = equipmentItemList.GetItemMetadata((int)index).AsString();
        equipButton.Disabled = string.IsNullOrEmpty(selectedItemId);

        // Show stat preview
        if (!string.IsNullOrEmpty(selectedItemId) && Campaign != null)
        {
            var item = Campaign.Inventory.FindById(selectedItemId);
            var def = item?.GetDef();
            statPreviewLabel.Text = FormatStatBonuses(def?.Stats);
        }
    }

    private void OnEquipPressed()
    {
        var crew = GetSelectedCrew();
        if (crew == null || string.IsNullOrEmpty(selectedItemId) || Campaign == null) return;

        if (Campaign.EquipItem(crew.Id, selectedItemId))
        {
            equipmentPopup.Hide();
            UpdateEquipmentDisplay();
            UpdateStatsDisplay(crew);
        }
    }

    private void OnUnequipPressed()
    {
        var crew = GetSelectedCrew();
        if (crew == null || Campaign == null) return;

        if (Campaign.UnequipItem(crew.Id, selectedEquipSlot))
        {
            equipmentPopup.Hide();
            UpdateEquipmentDisplay();
            UpdateStatsDisplay(crew);
        }
    }
}
