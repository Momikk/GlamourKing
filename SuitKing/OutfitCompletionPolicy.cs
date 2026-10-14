namespace SuitKing;

internal readonly record struct CompletionItemKey(uint ItemId, uint Container, ushort Slot, bool HighQuality);

internal static class OutfitCompletionPolicy
{
    internal static bool IsMergePrompt(string text, string expected = "发现了缺少此装备的现有套装，要将此装备并入其中吗？")
    {
        var normalized = string.Concat(text.Where(c => !char.IsWhiteSpace(c)));
        return !string.IsNullOrWhiteSpace(expected)
            && normalized == string.Concat(expected.Where(c => !char.IsWhiteSpace(c)));
    }

    internal static bool CanConfirmFinalStorage(bool owned, bool validatedSubmission, bool outfitChecked)
        => owned && validatedSubmission && outfitChecked;

    // Validate every filled native hand-in slot, including any automatically selected by the game.
    internal static string? ValidateHandIns(IReadOnlySet<CompletionItemKey> allowed,
        IReadOnlyList<CompletionItemKey> submitted, CompletionItemKey source, uint prismsHeld)
    {
        if (submitted.Count == 0) return "游戏尚未选择可提交的装备。";
        if (!submitted.Contains(source)) return "游戏没有选中本次目标装备。";
        if (submitted.Distinct().Count() != submitted.Count) return "游戏重复选择了同一格装备。";
        if (submitted.Any(item => !allowed.Contains(item))) return "游戏选择了批次之外或已被过滤的装备，已停止。";
        if (prismsHeld < submitted.Count) return "幻象棱晶不足，已停止。";
        return null;
    }
}