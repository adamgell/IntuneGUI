namespace Intune.Commander.Desktop.ViewModels;

/// <summary>
/// Represents a navigation category in the left nav panel.
/// </summary>
public class NavCategory
{
    public required string Name { get; init; }
    public required string Icon { get; init; }

    public override string ToString() => Name;
}
