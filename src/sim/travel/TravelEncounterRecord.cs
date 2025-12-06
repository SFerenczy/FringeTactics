namespace FringeTactics;

/// <summary>
/// Record of an encounter that occurred during travel.
/// </summary>
public class TravelEncounterRecord
{
    public int SegmentIndex { get; set; }
    public int DayInSegment { get; set; }
    public int SystemId { get; set; }
    public string EncounterType { get; set; }
    public string EncounterId { get; set; }
    public string Outcome { get; set; }
}
