using Godot;

namespace FringeTactics;

/// <summary>
/// Development tools for testing. Shift+Alt+Key for cheats.
/// </summary>
public partial class DevTools : Node
{
    public static DevTools Instance { get; private set; }

    // Cheat amounts
    private const int MONEY_CHEAT = 1000;
    private const int FUEL_CHEAT = 100;
    private const int AMMO_CHEAT = 100;
    private const int PARTS_CHEAT = 100;
    private const int MEDS_CHEAT = 10;

    public override void _Ready()
    {
        Instance = this;
        GD.Print("[DevTools] Initialized. Shift+Alt+M=Money, F=Fuel, A=Ammo, P=Parts, H=Meds, T=Teleport to job target");
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey keyEvent) return;
        if (!keyEvent.Pressed) return;
        if (!keyEvent.ShiftPressed || !keyEvent.AltPressed) return;

        var campaign = GameState.Instance?.Campaign;
        if (campaign == null) return;

        switch (keyEvent.Keycode)
        {
            case Key.M: // Money
                campaign.Money += MONEY_CHEAT;
                GD.Print($"[DevTools] +${MONEY_CHEAT} money. Total: ${campaign.Money}");
                break;

            case Key.F: // Fuel
                campaign.Fuel += FUEL_CHEAT;
                GD.Print($"[DevTools] +{FUEL_CHEAT} fuel. Total: {campaign.Fuel}");
                break;

            case Key.A: // Ammo
                campaign.Ammo += AMMO_CHEAT;
                GD.Print($"[DevTools] +{AMMO_CHEAT} ammo. Total: {campaign.Ammo}");
                break;

            case Key.P: // Parts
                campaign.Parts += PARTS_CHEAT;
                GD.Print($"[DevTools] +{PARTS_CHEAT} parts. Total: {campaign.Parts}");
                break;

            case Key.H: // Heal/Meds
                campaign.Meds += MEDS_CHEAT;
                GD.Print($"[DevTools] +{MEDS_CHEAT} meds. Total: {campaign.Meds}");
                break;

            case Key.X: // XP for all crew
                foreach (var crew in campaign.GetAliveCrew())
                {
                    crew.AddXp(50);
                }
                GD.Print("[DevTools] +50 XP to all alive crew");
                break;

            case Key.R: // Heal all injuries
                foreach (var crew in campaign.GetAliveCrew())
                {
                    crew.Injuries.Clear();
                }
                GD.Print("[DevTools] All injuries healed");
                break;

            case Key.T: // Teleport to job target (or selected node in sector view)
                TeleportToJobTarget(campaign);
                break;
        }
    }

    private void TeleportToJobTarget(CampaignState campaign)
    {
        int targetNodeId;

        if (campaign.CurrentJob != null)
        {
            targetNodeId = campaign.CurrentJob.TargetNodeId;
        }
        else
        {
            GD.Print("[DevTools] No active job to teleport to");
            return;
        }

        var targetNode = campaign.Sector.GetNode(targetNodeId);
        if (targetNode == null)
        {
            GD.Print("[DevTools] Invalid target node");
            return;
        }

        // Teleport without fuel cost
        campaign.CurrentNodeId = targetNodeId;
        GD.Print($"[DevTools] Teleported to {targetNode.Name}");

        // Refresh sector view if we're in it
        if (GameState.Instance.Mode == "sector")
        {
            GameState.Instance.GoToSectorView();
        }
    }
}
