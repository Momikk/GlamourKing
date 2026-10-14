namespace SuitKing;

internal static class NativeNameHighlightPolicy
{
    public static HashSet<string> Targets(IEnumerable<OutfitEquipment> equipment)
        => equipment.Where(item => !string.IsNullOrWhiteSpace(item.Name)).GroupBy(item => item.Name, StringComparer.Ordinal)
            .Where(group => group.All(item => item.Assessment?.State == OutfitStorageState.CanComplete))
            .Select(group => group.Key).ToHashSet(StringComparer.Ordinal);

    public static string? Match(string text, IReadOnlySet<string> targets)
        => targets.FirstOrDefault(name => text.Equals(name, StringComparison.Ordinal)
            || text.StartsWith(name, StringComparison.Ordinal) && text[name.Length..].All(c => !char.IsLetterOrDigit(c)));
}
