namespace FringeTactics;

/// <summary>
/// Base class for objectives providing common functionality.
/// </summary>
public abstract class ObjectiveBase : IObjective
{
    public string Id { get; }
    public string Description { get; }
    public bool IsPrimary { get; }
    public virtual bool IsFailureCondition => false;
    public ObjectiveStatus Status { get; protected set; } = ObjectiveStatus.Pending;
    
    protected ObjectiveBase(string id, string description, bool isPrimary = true)
    {
        Id = id;
        Description = description;
        IsPrimary = isPrimary;
    }
    
    public abstract ObjectiveStatus Evaluate(CombatState state);
    public abstract string GetProgressText(CombatState state);
}
