namespace FringeTactics;

public partial class ShipState
{
    public int Hp { get; set; } = 100;
    public int MaxHp { get; set; } = 100;
    public int Fuel { get; set; } = 100;
    public int MaxFuel { get; set; } = 100;

    public ShipState()
    {
    }
}
