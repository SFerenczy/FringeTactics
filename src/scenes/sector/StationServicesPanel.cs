using Godot;

namespace FringeTactics;

/// <summary>
/// Modal panel for station services (shop, fuel, repairs, recruitment).
/// </summary>
public partial class StationServicesPanel : Panel
{
    private Label feedbackLabel;

    private static readonly string[] RecruitNames = {
        "Riley", "Quinn", "Avery", "Blake", "Cameron", "Dakota", "Ellis", "Finley",
        "Harper", "Jade", "Kai", "Logan", "Mason", "Nova", "Parker", "Reese",
        "Sage", "Taylor", "Val", "Winter"
    };

    public override void _Ready()
    {
        CreateUI();
    }

    private void CreateUI()
    {
        // Center the panel using anchors and offsets
        AnchorLeft = 0.5f;
        AnchorRight = 0.5f;
        AnchorTop = 0.5f;
        AnchorBottom = 0.5f;
        OffsetLeft = -175;
        OffsetRight = 175;
        OffsetTop = -200;
        OffsetBottom = 200;
        Visible = false;

        var panelStyle = new StyleBoxFlat();
        panelStyle.BgColor = new Color(0.1f, 0.12f, 0.1f, 0.95f);
        panelStyle.BorderWidthTop = 2;
        panelStyle.BorderWidthBottom = 2;
        panelStyle.BorderWidthLeft = 2;
        panelStyle.BorderWidthRight = 2;
        panelStyle.BorderColor = Colors.Green;
        panelStyle.CornerRadiusTopLeft = 8;
        panelStyle.CornerRadiusTopRight = 8;
        panelStyle.CornerRadiusBottomLeft = 8;
        panelStyle.CornerRadiusBottomRight = 8;
        AddThemeStyleboxOverride("panel", panelStyle);

        var vbox = new VBoxContainer();
        vbox.SetAnchorsPreset(LayoutPreset.FullRect);
        vbox.OffsetLeft = 15;
        vbox.OffsetRight = -15;
        vbox.OffsetTop = 15;
        vbox.OffsetBottom = -15;
        AddChild(vbox);

        // Title
        var title = new Label();
        title.Text = "STATION SERVICES";
        title.AddThemeFontSizeOverride("font_size", 20);
        title.AddThemeColorOverride("font_color", Colors.Green);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(title);

        AddSpacer(vbox, 15);

        // Shop section
        var shopLabel = new Label();
        shopLabel.Text = "SUPPLY SHOP";
        shopLabel.AddThemeFontSizeOverride("font_size", 14);
        shopLabel.AddThemeColorOverride("font_color", Colors.Yellow);
        vbox.AddChild(shopLabel);

        var shopHbox = new HBoxContainer();
        vbox.AddChild(shopHbox);

        var buyAmmoBtn = new Button();
        buyAmmoBtn.Text = "Buy Ammo ($20)";
        buyAmmoBtn.CustomMinimumSize = new Vector2(140, 35);
        buyAmmoBtn.Pressed += () => BuyResource("ammo", 20, 10);
        shopHbox.AddChild(buyAmmoBtn);

        var buyMedsBtn = new Button();
        buyMedsBtn.Text = "Buy Meds ($30)";
        buyMedsBtn.CustomMinimumSize = new Vector2(140, 35);
        buyMedsBtn.Pressed += () => BuyResource("meds", 30, 2);
        shopHbox.AddChild(buyMedsBtn);

        AddSpacer(vbox, 10);

        // Fuel Depot
        var fuelLabel = new Label();
        fuelLabel.Text = "FUEL DEPOT";
        fuelLabel.AddThemeFontSizeOverride("font_size", 14);
        fuelLabel.AddThemeColorOverride("font_color", Colors.Yellow);
        vbox.AddChild(fuelLabel);

        var buyFuelBtn = new Button();
        buyFuelBtn.Text = "Buy Fuel ($15 for 20)";
        buyFuelBtn.CustomMinimumSize = new Vector2(200, 35);
        buyFuelBtn.Pressed += () => BuyResource("fuel", 15, 20);
        vbox.AddChild(buyFuelBtn);

        AddSpacer(vbox, 10);

        // Repair Yard
        var repairLabel = new Label();
        repairLabel.Text = "REPAIR YARD";
        repairLabel.AddThemeFontSizeOverride("font_size", 14);
        repairLabel.AddThemeColorOverride("font_color", Colors.Yellow);
        vbox.AddChild(repairLabel);

        var repairBtn = new Button();
        repairBtn.Text = "Repair Hull (10 Parts)";
        repairBtn.CustomMinimumSize = new Vector2(200, 35);
        repairBtn.Pressed += OnRepairShip;
        vbox.AddChild(repairBtn);

        AddSpacer(vbox, 10);

        // Recruitment
        var recruitLabel = new Label();
        recruitLabel.Text = "RECRUITMENT";
        recruitLabel.AddThemeFontSizeOverride("font_size", 14);
        recruitLabel.AddThemeColorOverride("font_color", Colors.Yellow);
        vbox.AddChild(recruitLabel);

        var recruitHbox = new HBoxContainer();
        vbox.AddChild(recruitHbox);

        var hireSoldierBtn = new Button();
        hireSoldierBtn.Text = "Soldier ($50)";
        hireSoldierBtn.CustomMinimumSize = new Vector2(100, 35);
        hireSoldierBtn.Pressed += () => HireCrew(CrewRole.Soldier, 50);
        recruitHbox.AddChild(hireSoldierBtn);

        var hireMedicBtn = new Button();
        hireMedicBtn.Text = "Medic ($60)";
        hireMedicBtn.CustomMinimumSize = new Vector2(100, 35);
        hireMedicBtn.Pressed += () => HireCrew(CrewRole.Medic, 60);
        recruitHbox.AddChild(hireMedicBtn);

        var hireTechBtn = new Button();
        hireTechBtn.Text = "Tech ($60)";
        hireTechBtn.CustomMinimumSize = new Vector2(100, 35);
        hireTechBtn.Pressed += () => HireCrew(CrewRole.Tech, 60);
        recruitHbox.AddChild(hireTechBtn);

        AddSpacer(vbox, 10);

        // Feedback label
        feedbackLabel = new Label();
        feedbackLabel.AddThemeFontSizeOverride("font_size", 12);
        feedbackLabel.CustomMinimumSize = new Vector2(0, 40);
        vbox.AddChild(feedbackLabel);

        AddSpacer(vbox, 10);

        // Close button
        var closeBtn = new Button();
        closeBtn.Text = "Close";
        closeBtn.CustomMinimumSize = new Vector2(100, 35);
        closeBtn.Pressed += () => Visible = false;
        vbox.AddChild(closeBtn);
    }

    private void AddSpacer(VBoxContainer parent, int height)
    {
        var spacer = new Control();
        spacer.CustomMinimumSize = new Vector2(0, height);
        parent.AddChild(spacer);
    }

    public new void Show()
    {
        feedbackLabel.Text = "";
        Visible = true;
    }

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

    private void SetFeedback(string text, Color color)
    {
        feedbackLabel.Text = text;
        feedbackLabel.AddThemeColorOverride("font_color", color);
    }
}
