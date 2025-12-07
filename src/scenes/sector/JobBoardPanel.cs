using Godot;
using System;

namespace FringeTactics;

/// <summary>
/// Modal panel for viewing and accepting jobs.
/// </summary>
public partial class JobBoardPanel : Panel
{
    private VBoxContainer jobListContainer;

    public event Action OnJobAccepted;

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
        OffsetLeft = -200;
        OffsetRight = 200;
        OffsetTop = -175;
        OffsetBottom = 175;
        Visible = false;

        var panelStyle = new StyleBoxFlat();
        panelStyle.BgColor = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        panelStyle.BorderWidthTop = 2;
        panelStyle.BorderWidthBottom = 2;
        panelStyle.BorderWidthLeft = 2;
        panelStyle.BorderWidthRight = 2;
        panelStyle.BorderColor = Colors.Cyan;
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
        title.Text = "JOB BOARD";
        title.AddThemeFontSizeOverride("font_size", 20);
        title.AddThemeColorOverride("font_color", Colors.Cyan);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(title);

        AddSpacer(vbox, 10);

        // Scrollable job list
        var scroll = new ScrollContainer();
        scroll.CustomMinimumSize = new Vector2(0, 220);
        scroll.SizeFlagsVertical = SizeFlags.ExpandFill;
        vbox.AddChild(scroll);

        jobListContainer = new VBoxContainer();
        jobListContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        scroll.AddChild(jobListContainer);

        AddSpacer(vbox, 10);

        // Close button
        var closeButton = new Button();
        closeButton.Text = "Close";
        closeButton.CustomMinimumSize = new Vector2(100, 35);
        closeButton.Pressed += () => Visible = false;
        vbox.AddChild(closeButton);
    }

    private void AddSpacer(VBoxContainer parent, int height)
    {
        var spacer = new Control();
        spacer.CustomMinimumSize = new Vector2(0, height);
        parent.AddChild(spacer);
    }

    public new void Show()
    {
        Populate();
        Visible = true;
    }

    private void Populate()
    {
        foreach (var child in jobListContainer.GetChildren())
        {
            child.QueueFree();
        }

        var campaign = GameState.Instance?.Campaign;
        if (campaign == null) return;

        if (campaign.AvailableJobs.Count == 0)
        {
            var noJobsLabel = new Label();
            noJobsLabel.Text = "No jobs available at this location.";
            noJobsLabel.AddThemeFontSizeOverride("font_size", 14);
            noJobsLabel.AddThemeColorOverride("font_color", Colors.Gray);
            jobListContainer.AddChild(noJobsLabel);
            return;
        }

        foreach (var job in campaign.AvailableJobs)
        {
            CreateJobEntry(job, campaign);
        }
    }

    private void CreateJobEntry(Job job, CampaignState campaign)
    {
        var container = new PanelContainer();
        container.CustomMinimumSize = new Vector2(0, 70);

        var style = new StyleBoxFlat();
        style.BgColor = new Color(0.15f, 0.15f, 0.2f);
        style.CornerRadiusTopLeft = 4;
        style.CornerRadiusTopRight = 4;
        style.CornerRadiusBottomLeft = 4;
        style.CornerRadiusBottomRight = 4;
        container.AddThemeStyleboxOverride("panel", style);

        var hbox = new HBoxContainer();
        hbox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        container.AddChild(hbox);

        // Job info
        var infoVbox = new VBoxContainer();
        infoVbox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        hbox.AddChild(infoVbox);

        var titleLabel = new Label();
        titleLabel.Text = $"{job.Title} [{job.GetDifficultyDisplay()}]";
        titleLabel.AddThemeFontSizeOverride("font_size", 14);
        var diffColor = job.Difficulty switch
        {
            JobDifficulty.Easy => Colors.Green,
            JobDifficulty.Medium => Colors.Yellow,
            JobDifficulty.Hard => Colors.Red,
            _ => Colors.White
        };
        titleLabel.AddThemeColorOverride("font_color", diffColor);
        infoVbox.AddChild(titleLabel);

        var targetSystem = campaign.World?.GetSystem(job.TargetNodeId);
        var targetLabel = new Label();
        targetLabel.Text = $"Target: {targetSystem?.Name ?? "Unknown"}";
        targetLabel.AddThemeFontSizeOverride("font_size", 12);
        infoVbox.AddChild(targetLabel);

        var rewardLabel = new Label();
        rewardLabel.Text = $"Reward: {job.Reward}";
        rewardLabel.AddThemeFontSizeOverride("font_size", 12);
        rewardLabel.AddThemeColorOverride("font_color", Colors.Gold);
        infoVbox.AddChild(rewardLabel);

        // Accept button
        var acceptBtn = new Button();
        acceptBtn.Text = "Accept";
        acceptBtn.CustomMinimumSize = new Vector2(70, 50);
        acceptBtn.Pressed += () => AcceptJob(job);
        hbox.AddChild(acceptBtn);

        jobListContainer.AddChild(container);

        // Add small spacer between entries
        var spacer = new Control();
        spacer.CustomMinimumSize = new Vector2(0, 5);
        jobListContainer.AddChild(spacer);
    }

    private void AcceptJob(Job job)
    {
        var campaign = GameState.Instance?.Campaign;
        if (campaign == null) return;

        if (campaign.AcceptJob(job))
        {
            Visible = false;
            OnJobAccepted?.Invoke();
        }
    }
}
