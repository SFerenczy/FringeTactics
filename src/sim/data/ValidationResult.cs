using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Result of validating a data definition.
/// Accumulates errors and warnings during validation.
/// </summary>
public class ValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<string> Errors { get; } = new();
    public List<string> Warnings { get; } = new();

    public void AddError(string message) => Errors.Add(message);
    public void AddWarning(string message) => Warnings.Add(message);

    public void Merge(ValidationResult other)
    {
        Errors.AddRange(other.Errors);
        Warnings.AddRange(other.Warnings);
    }

    public static ValidationResult Success() => new();

    public static ValidationResult Error(string message)
    {
        var result = new ValidationResult();
        result.AddError(message);
        return result;
    }
}
