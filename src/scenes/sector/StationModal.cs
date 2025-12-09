using Godot;
using System.Collections.Generic;
using System.Linq;

namespace FringeTactics;

/// <summary>
/// Modal for interacting with station facilities.
/// Shows available facilities at the current station.
/// </summary>
public partial class StationModal : Panel
{
    private VBoxContainer mainContainer;
    private ScrollContainer facilityListView;
    private Control currentFacilityView;
    private Label titleLabel;
    private Label feedbackLabel;
    
    private Station currentStation;
    private Dictionary<FacilityType, Control> facilityViews = new();

    public override void _Ready()
    {
        CreateUI();
    }

    private void CreateUI()
    {
        AnchorLeft = 0.5f;
        AnchorRight = 0.5f;
        AnchorTop = 0.5f;
        AnchorBottom = 0.5f;
        OffsetLeft = -220;
        OffsetRight = 220;
        OffsetTop = -300;
        OffsetBottom = 300;
        Visible = false;

        var panelStyle = new StyleBoxFlat();
        panelStyle.BgColor = new Color(0.08f, 0.1f, 0.12f, 0.97f);
        panelStyle.BorderWidthTop = 2;
        panelStyle.BorderWidthBottom = 2;
        panelStyle.BorderWidthLeft = 2;
        panelStyle.BorderWidthRight = 2;
        panelStyle.BorderColor = new Color(0.3f, 0.6f, 0.8f);
        panelStyle.CornerRadiusTopLeft = 8;
        panelStyle.CornerRadiusTopRight = 8;
        panelStyle.CornerRadiusBottomLeft = 8;
        panelStyle.CornerRadiusBottomRight = 8;
        AddThemeStyleboxOverride("panel", panelStyle);

        mainContainer = new VBoxContainer();
        mainContainer.SetAnchorsPreset(LayoutPreset.FullRect);
        mainContainer.OffsetLeft = 15;
        mainContainer.OffsetRight = -15;
        mainContainer.OffsetTop = 15;
        mainContainer.OffsetBottom = -15;
        AddChild(mainContainer);

        CreateFacilityListView();
        CreateFacilityViews();
    }

    private void CreateFacilityListView()
    {
        facilityListView = new ScrollContainer();
        facilityListView.SizeFlagsVertical = SizeFlags.ExpandFill;
        facilityListView.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        facilityListView.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        mainContainer.AddChild(facilityListView);

        var vbox = new VBoxContainer();
        vbox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        facilityListView.AddChild(vbox);

        titleLabel = new Label();
        titleLabel.AddThemeFontSizeOverride("font_size", 22);
        titleLabel.AddThemeColorOverride("font_color", new Color(0.4f, 0.8f, 1.0f));
        titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(titleLabel);

        AddSpacer(vbox, 20);

        var facilitiesLabel = new Label();
        facilitiesLabel.Text = "AVAILABLE LOCATIONS";
        facilitiesLabel.AddThemeFontSizeOverride("font_size", 12);
        facilitiesLabel.AddThemeColorOverride("font_color", Colors.Gray);
        facilitiesLabel.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(facilitiesLabel);

        AddSpacer(vbox, 10);
    }

    private void CreateFacilityViews()
    {
        facilityViews[FacilityType.Shop] = CreateShopView();
        facilityViews[FacilityType.FuelDepot] = CreateFuelDepotView();
        facilityViews[FacilityType.RepairYard] = CreateRepairYardView();
        facilityViews[FacilityType.Recruitment] = CreateRecruitmentView();
        facilityViews[FacilityType.Bar] = CreateBarView();
        facilityViews[FacilityType.Medical] = CreateMedicalView();
        facilityViews[FacilityType.BlackMarket] = CreateBlackMarketView();
        facilityViews[FacilityType.MissionBoard] = CreateMissionBoardView();

        foreach (var view in facilityViews.Values)
        {
            view.Visible = false;
            mainContainer.AddChild(view);
        }
    }

    private Control CreateFacilityViewBase(string title, Color titleColor)
    {
        var container = new Control();
        container.SetAnchorsPreset(LayoutPreset.FullRect);

        var vbox = new VBoxContainer();
        vbox.SetAnchorsPreset(LayoutPreset.FullRect);
        container.AddChild(vbox);

        var header = new HBoxContainer();
        vbox.AddChild(header);

        var backBtn = new Button();
        backBtn.Text = "←";
        backBtn.CustomMinimumSize = new Vector2(40, 35);
        backBtn.Pressed += ShowFacilityList;
        header.AddChild(backBtn);

        var spacer = new Control();
        spacer.SizeFlagsHorizontal = Control.SizeFlags.Expand;
        header.AddChild(spacer);

        var titleLbl = new Label();
        titleLbl.Text = title;
        titleLbl.AddThemeFontSizeOverride("font_size", 20);
        titleLbl.AddThemeColorOverride("font_color", titleColor);
        header.AddChild(titleLbl);

        var spacer2 = new Control();
        spacer2.SizeFlagsHorizontal = Control.SizeFlags.Expand;
        spacer2.CustomMinimumSize = new Vector2(40, 0);
        header.AddChild(spacer2);

        AddSpacer(vbox, 15);

        return container;
    }

    private VBoxContainer GetFacilityVBox(Control facilityView)
    {
        return facilityView.GetChild<VBoxContainer>(0);
    }

    private Control CreateShopView()
    {
        var view = CreateFacilityViewBase("SUPPLY SHOP", Colors.Yellow);
        var vbox = GetFacilityVBox(view);

        var desc = new Label();
        desc.Text = "Purchase supplies for your crew.";
        desc.AddThemeFontSizeOverride("font_size", 12);
        desc.AddThemeColorOverride("font_color", Colors.LightGray);
        vbox.AddChild(desc);

        AddSpacer(vbox, 15);

        var buyAmmoBtn = new Button();
        buyAmmoBtn.Text = "Buy Ammo ($20 for 10)";
        buyAmmoBtn.CustomMinimumSize = new Vector2(0, 40);
        buyAmmoBtn.Pressed += () => BuyResource("ammo", 20, 10);
        vbox.AddChild(buyAmmoBtn);

        AddSpacer(vbox, 8);

        var buyMedsBtn = new Button();
        buyMedsBtn.Text = "Buy Meds ($30 for 2)";
        buyMedsBtn.CustomMinimumSize = new Vector2(0, 40);
        buyMedsBtn.Pressed += () => BuyResource("meds", 30, 2);
        vbox.AddChild(buyMedsBtn);

        AddSpacer(vbox, 8);

        var buyPartsBtn = new Button();
        buyPartsBtn.Text = "Buy Parts ($25 for 5)";
        buyPartsBtn.CustomMinimumSize = new Vector2(0, 40);
        buyPartsBtn.Pressed += () => BuyResource("parts", 25, 5);
        vbox.AddChild(buyPartsBtn);

        AddFeedbackLabel(vbox);

        return view;
    }

    private Control CreateFuelDepotView()
    {
        var view = CreateFacilityViewBase("FUEL DEPOT", Colors.Orange);
        var vbox = GetFacilityVBox(view);

        var desc = new Label();
        desc.Text = "Refuel your ship for travel.";
        desc.AddThemeFontSizeOverride("font_size", 12);
        desc.AddThemeColorOverride("font_color", Colors.LightGray);
        vbox.AddChild(desc);

        AddSpacer(vbox, 15);

        var buyFuelBtn = new Button();
        buyFuelBtn.Text = "Buy Fuel ($15 for 20)";
        buyFuelBtn.CustomMinimumSize = new Vector2(0, 40);
        buyFuelBtn.Pressed += () => BuyResource("fuel", 15, 20);
        vbox.AddChild(buyFuelBtn);

        AddSpacer(vbox, 8);

        var buyFuelLargeBtn = new Button();
        buyFuelLargeBtn.Text = "Buy Fuel ($60 for 100)";
        buyFuelLargeBtn.CustomMinimumSize = new Vector2(0, 40);
        buyFuelLargeBtn.Pressed += () => BuyResource("fuel", 60, 100);
        vbox.AddChild(buyFuelLargeBtn);

        AddFeedbackLabel(vbox);

        return view;
    }

    private Control CreateRepairYardView()
    {
        var view = CreateFacilityViewBase("REPAIR YARD", Colors.Cyan);
        var vbox = GetFacilityVBox(view);

        var desc = new Label();
        desc.Text = "Repair your ship's hull.";
        desc.AddThemeFontSizeOverride("font_size", 12);
        desc.AddThemeColorOverride("font_color", Colors.LightGray);
        vbox.AddChild(desc);

        AddSpacer(vbox, 15);

        var repairBtn = new Button();
        repairBtn.Text = "Repair Hull (10 Parts → +20 Hull)";
        repairBtn.CustomMinimumSize = new Vector2(0, 40);
        repairBtn.Pressed += OnRepairShip;
        vbox.AddChild(repairBtn);

        AddFeedbackLabel(vbox);

        return view;
    }

    private Control CreateRecruitmentView()
    {
        var view = CreateFacilityViewBase("RECRUITMENT", Colors.Green);
        var vbox = GetFacilityVBox(view);

        var desc = new Label();
        desc.Text = "Hire new crew members.";
        desc.AddThemeFontSizeOverride("font_size", 12);
        desc.AddThemeColorOverride("font_color", Colors.LightGray);
        vbox.AddChild(desc);

        AddSpacer(vbox, 15);

        var hireSoldierBtn = new Button();
        hireSoldierBtn.Text = "Hire Soldier ($50)";
        hireSoldierBtn.CustomMinimumSize = new Vector2(0, 40);
        hireSoldierBtn.Pressed += () => HireCrew(CrewRole.Soldier, 50);
        vbox.AddChild(hireSoldierBtn);

        AddSpacer(vbox, 8);

        var hireMedicBtn = new Button();
        hireMedicBtn.Text = "Hire Medic ($60)";
        hireMedicBtn.CustomMinimumSize = new Vector2(0, 40);
        hireMedicBtn.Pressed += () => HireCrew(CrewRole.Medic, 60);
        vbox.AddChild(hireMedicBtn);

        AddSpacer(vbox, 8);

        var hireTechBtn = new Button();
        hireTechBtn.Text = "Hire Tech ($60)";
        hireTechBtn.CustomMinimumSize = new Vector2(0, 40);
        hireTechBtn.Pressed += () => HireCrew(CrewRole.Tech, 60);
        vbox.AddChild(hireTechBtn);

        AddFeedbackLabel(vbox);

        return view;
    }

    private Control CreateBarView()
    {
        var view = CreateFacilityViewBase("BAR", new Color(0.8f, 0.6f, 0.4f));
        var vbox = GetFacilityVBox(view);

        var desc = new Label();
        desc.Text = "A place to gather information and find work.";
        desc.AddThemeFontSizeOverride("font_size", 12);
        desc.AddThemeColorOverride("font_color", Colors.LightGray);
        vbox.AddChild(desc);

        AddSpacer(vbox, 15);

        var placeholder = new Label();
        placeholder.Text = "Nothing available right now.";
        placeholder.AddThemeFontSizeOverride("font_size", 14);
        placeholder.AddThemeColorOverride("font_color", Colors.DarkGray);
        vbox.AddChild(placeholder);

        AddFeedbackLabel(vbox);

        return view;
    }

    private Control CreateMedicalView()
    {
        var view = CreateFacilityViewBase("MEDICAL BAY", new Color(1.0f, 0.4f, 0.4f));
        var vbox = GetFacilityVBox(view);

        var desc = new Label();
        desc.Text = "Heal injured crew members.";
        desc.AddThemeFontSizeOverride("font_size", 12);
        desc.AddThemeColorOverride("font_color", Colors.LightGray);
        vbox.AddChild(desc);

        AddSpacer(vbox, 15);

        var healBtn = new Button();
        healBtn.Text = "Heal All Crew (1 Med per injury)";
        healBtn.CustomMinimumSize = new Vector2(0, 40);
        healBtn.Pressed += OnHealCrew;
        vbox.AddChild(healBtn);

        AddFeedbackLabel(vbox);

        return view;
    }

    private Control CreateBlackMarketView()
    {
        var view = CreateFacilityViewBase("BLACK MARKET", new Color(0.6f, 0.3f, 0.6f));
        var vbox = GetFacilityVBox(view);

        var desc = new Label();
        desc.Text = "Illegal goods and services.";
        desc.AddThemeFontSizeOverride("font_size", 12);
        desc.AddThemeColorOverride("font_color", Colors.LightGray);
        vbox.AddChild(desc);

        AddSpacer(vbox, 15);

        var placeholder = new Label();
        placeholder.Text = "Nothing available right now.";
        placeholder.AddThemeFontSizeOverride("font_size", 14);
        placeholder.AddThemeColorOverride("font_color", Colors.DarkGray);
        vbox.AddChild(placeholder);

        AddFeedbackLabel(vbox);

        return view;
    }

    private Control CreateMissionBoardView()
    {
        var view = CreateFacilityViewBase("MISSION BOARD", new Color(0.4f, 0.7f, 1.0f));
        var vbox = GetFacilityVBox(view);

        var desc = new Label();
        desc.Text = "Find contracts and jobs.";
        desc.AddThemeFontSizeOverride("font_size", 12);
        desc.AddThemeColorOverride("font_color", Colors.LightGray);
        vbox.AddChild(desc);

        AddSpacer(vbox, 15);

        var placeholder = new Label();
        placeholder.Text = "Use the Job Board button in the main UI.";
        placeholder.AddThemeFontSizeOverride("font_size", 14);
        placeholder.AddThemeColorOverride("font_color", Colors.DarkGray);
        vbox.AddChild(placeholder);

        AddFeedbackLabel(vbox);

        return view;
    }

    private void AddFeedbackLabel(VBoxContainer vbox)
    {
        AddSpacer(vbox, 15);

        var feedback = new Label();
        feedback.Name = "FeedbackLabel";
        feedback.AddThemeFontSizeOverride("font_size", 12);
        feedback.CustomMinimumSize = new Vector2(0, 30);
        vbox.AddChild(feedback);
    }

    private void AddSpacer(VBoxContainer parent, int height)
    {
        var spacer = new Control();
        spacer.CustomMinimumSize = new Vector2(0, height);
        parent.AddChild(spacer);
    }

    public void ShowForStation(Station station)
    {
        currentStation = station;
        RefreshFacilityList();
        ShowFacilityList();
        Visible = true;
    }

    private void RefreshFacilityList()
    {
        if (currentStation == null) return;

        titleLabel.Text = currentStation.Name;

        var vbox = facilityListView.GetChild<VBoxContainer>(0);

        // Remove old facility buttons (keep title, spacer, and facilities label)
        var toRemove = new List<Node>();
        for (int i = 4; i < vbox.GetChildCount(); i++)
        {
            toRemove.Add(vbox.GetChild(i));
        }
        foreach (var node in toRemove)
        {
            node.QueueFree();
        }

        // Add buttons for available facilities
        var facilities = currentStation.GetAvailableFacilities().ToList();
        
        if (facilities.Count == 0)
        {
            var noFacilities = new Label();
            noFacilities.Text = "No facilities available.";
            noFacilities.AddThemeFontSizeOverride("font_size", 14);
            noFacilities.AddThemeColorOverride("font_color", Colors.DarkGray);
            noFacilities.HorizontalAlignment = HorizontalAlignment.Center;
            vbox.AddChild(noFacilities);
        }
        else
        {
            foreach (var facility in facilities)
            {
                var btn = new Button();
                btn.Text = GetFacilityDisplayName(facility.Type);
                btn.CustomMinimumSize = new Vector2(0, 45);
                var type = facility.Type;
                btn.Pressed += () => ShowFacility(type);
                vbox.AddChild(btn);

                AddSpacer(vbox, 5);
            }
        }

        AddSpacer(vbox, 20);

        var closeBtn = new Button();
        closeBtn.Text = "Leave Station";
        closeBtn.CustomMinimumSize = new Vector2(0, 40);
        closeBtn.Pressed += () => Visible = false;
        vbox.AddChild(closeBtn);
    }

    private string GetFacilityDisplayName(FacilityType type)
    {
        return type switch
        {
            FacilityType.Shop => "Supply Shop",
            FacilityType.Bar => "Bar",
            FacilityType.MissionBoard => "Mission Board",
            FacilityType.RepairYard => "Repair Yard",
            FacilityType.Recruitment => "Recruitment Office",
            FacilityType.Medical => "Medical Bay",
            FacilityType.BlackMarket => "Black Market",
            FacilityType.FuelDepot => "Fuel Depot",
            _ => type.ToString()
        };
    }

    private void ShowFacilityList()
    {
        facilityListView.Visible = true;
        currentFacilityView?.SetDeferred("visible", false);
        currentFacilityView = null;
        ClearFeedback();
    }

    private void ShowFacility(FacilityType type)
    {
        if (!facilityViews.TryGetValue(type, out var view)) return;

        facilityListView.Visible = false;
        currentFacilityView?.SetDeferred("visible", false);
        
        view.Visible = true;
        currentFacilityView = view;
        ClearFeedback();
    }

    private void ClearFeedback()
    {
        if (currentFacilityView != null)
        {
            var feedback = currentFacilityView.FindChild("FeedbackLabel", true, false) as Label;
            if (feedback != null) feedback.Text = "";
        }
    }

    private void SetFeedback(string text, Color color)
    {
        if (currentFacilityView == null) return;
        
        var feedback = currentFacilityView.FindChild("FeedbackLabel", true, false) as Label;
        if (feedback != null)
        {
            feedback.Text = text;
            feedback.AddThemeColorOverride("font_color", color);
        }
    }

    // ========== Actions ==========

    private static readonly string[] RecruitNames = {
        "Riley", "Quinn", "Avery", "Blake", "Cameron", "Dakota", "Ellis", "Finley",
        "Harper", "Jade", "Kai", "Logan", "Mason", "Nova", "Parker", "Reese",
        "Sage", "Taylor", "Val", "Winter"
    };

    private void BuyResource(string type, int cost, int amount)
    {
        var campaign = GameState.Instance?.Campaign;
        if (campaign == null) return;

        if (campaign.Money < cost)
        {
            SetFeedback("Not enough money!", Colors.Red);
            return;
        }

        campaign.Money -= cost;
        switch (type)
        {
            case "ammo":
                campaign.Ammo += amount;
                SetFeedback($"Bought {amount} ammo.", Colors.Green);
                break;
            case "meds":
                campaign.Meds += amount;
                SetFeedback($"Bought {amount} meds.", Colors.Green);
                break;
            case "fuel":
                campaign.Fuel += amount;
                SetFeedback($"Bought {amount} fuel.", Colors.Green);
                break;
            case "parts":
                campaign.Parts += amount;
                SetFeedback($"Bought {amount} parts.", Colors.Green);
                break;
        }
    }

    private void OnRepairShip()
    {
        var campaign = GameState.Instance?.Campaign;
        if (campaign == null) return;

        if (campaign.Ship == null)
        {
            SetFeedback("No ship to repair!", Colors.Red);
            return;
        }

        if (campaign.Parts < 10)
        {
            SetFeedback("Not enough parts! (Need 10)", Colors.Red);
            return;
        }

        if (campaign.Ship.Hull >= campaign.Ship.MaxHull)
        {
            SetFeedback("Hull already at maximum!", Colors.Yellow);
            return;
        }

        campaign.Parts -= 10;
        int repaired = System.Math.Min(20, campaign.Ship.MaxHull - campaign.Ship.Hull);
        campaign.Ship.Hull += repaired;
        SetFeedback($"Repaired {repaired} hull damage.", Colors.Green);
    }

    private void HireCrew(CrewRole role, int cost)
    {
        var campaign = GameState.Instance?.Campaign;
        if (campaign == null) return;

        if (campaign.Money < cost)
        {
            SetFeedback("Not enough money!", Colors.Red);
            return;
        }

        if (campaign.GetAliveCrew().Count >= 6)
        {
            SetFeedback("Crew roster full! (Max 6)", Colors.Red);
            return;
        }

        var name = RecruitNames[GD.RandRange(0, RecruitNames.Length - 1)];
        var hired = campaign.HireCrew(name, role, cost);

        if (hired != null)
        {
            SetFeedback($"Hired {name} ({role})!", Colors.Green);
        }
    }

    private void OnHealCrew()
    {
        var campaign = GameState.Instance?.Campaign;
        if (campaign == null) return;

        var injured = campaign.GetAliveCrew().Where(c => c.Injuries.Count > 0).ToList();
        
        if (injured.Count == 0)
        {
            SetFeedback("No injured crew members.", Colors.Yellow);
            return;
        }

        int totalInjuries = injured.Sum(c => c.Injuries.Count);

        if (campaign.Meds < totalInjuries)
        {
            SetFeedback($"Not enough meds! (Need {totalInjuries})", Colors.Red);
            return;
        }

        campaign.Meds -= totalInjuries;
        foreach (var crew in injured)
        {
            crew.Injuries.Clear();
        }
        SetFeedback($"Healed {injured.Count} crew members.", Colors.Green);
    }
}
