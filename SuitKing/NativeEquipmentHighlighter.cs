using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Memory;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace SuitKing;

// Store ownership information only. Cached addresses are never dereferenced for restoration.
internal sealed unsafe class NativeEquipmentHighlighter : IDisposable
{
    private const string AddonName = "MiragePrismPrismBoxCrystallize";
    private sealed record Paint(string Name, byte[] Original, byte[] Applied, ByteColor OriginalColor, ByteColor AppliedColor);
    private readonly Dictionary<nint, Paint> painted = new();
    private readonly Plugin plugin;
    private IReadOnlyList<OutfitEquipment>? snapshot;
    private HashSet<string> completable = new(StringComparer.Ordinal);

    internal NativeEquipmentHighlighter(Plugin plugin)
    {
        this.plugin = plugin;
        Plugin.Addons.RegisterListener(AddonEvent.PreDraw, AddonName, Draw);
        Plugin.Addons.RegisterListener(AddonEvent.PreFinalize, AddonName, FinalizeAddon);
    }

    private void Draw(AddonEvent _, AddonArgs args)
    {
        try
        {
            if (!ReferenceEquals(snapshot, plugin.Inventory.Equipment))
            {
                snapshot = plugin.Inventory.Equipment;
                completable = NativeNameHighlightPolicy.Targets(snapshot);
            }
            PaintLive((AddonMiragePrismPrismBoxCrystallize*)args.Addon.Address,
                plugin.IsDresserOpen && plugin.Inventory.AreOutfitsLoaded);
        }
        catch (Exception error) { Plugin.Log.Error(error, "SuitKing native name highlighting failed."); }
    }

    private void PaintLive(AddonMiragePrismPrismBoxCrystallize* addon, bool enabled)
    {
        if (addon == null || addon->ItemTreeList == null) return;
        var tree = addon->ItemTreeList;
        if (tree->Items.LongCount < 0 || tree->Items.LongCount > 200) return;
        var visited = new HashSet<nint>();
        for (var i = 0; i < tree->Items.LongCount; i++)
        {
            var item = tree->Items[i].Value;
            if (item == null || item->Renderer == null) continue;
            var renderer = item->Renderer;
            var uld = &renderer->UldManager;
            if (uld->NodeList == null || uld->NodeListCount > 100) continue;
            for (var n = 0; n < uld->NodeListCount; n++)
            {
                var node = uld->NodeList[n];
                if (node == null || node->Type != NodeType.Text || !visited.Add((nint)node)) continue;
                var text = (AtkTextNode*)node;
                if ((byte*)text->NodeText.StringPtr == null) continue;
                var bytes = MemoryHelper.ReadRawNullTerminated((nint)(byte*)text->NodeText.StringPtr).Append((byte)0).ToArray();
                var plain = text->NodeText.StringPtr.AsDalamudSeString().TextValue;
                if (painted.TryGetValue((nint)text, out var old))
                {
                    var ownsText = bytes.SequenceEqual(old.Applied);
                    var ownsColor = Same(text->TextColor, old.AppliedColor);
                    if (!enabled || !ownsText || !ownsColor)
                    {
                        if (ownsText) text->SetText(old.Original.AsSpan());
                        if (ownsColor) text->TextColor = old.OriginalColor;
                        painted.Remove((nint)text);
                        bytes = MemoryHelper.ReadRawNullTerminated((nint)(byte*)text->NodeText.StringPtr).Append((byte)0).ToArray();
                        plain = text->NodeText.StringPtr.AsDalamudSeString().TextValue;
                    }
                }
                var name = enabled ? NativeNameHighlightPolicy.Match(plain, completable) : null;
                if (name == null)
                {
                    RestoreNode(text);
                    continue;
                }
                if (!painted.ContainsKey((nint)text))
                {
                    var seString = text->NodeText.StringPtr.AsDalamudSeString();
                    // Preserve icon/HQ and other payloads; remove only colors that could override red.
                    var redText = new SeString(seString.Payloads.Where(payload => payload is not UIForegroundPayload and not UIGlowPayload).ToList())
                        .Encode().Append((byte)0).ToArray();
                    var originalColor = text->TextColor;
                    var red = new ByteColor { R = 230, G = 45, B = 45, A = originalColor.A };
                    text->SetText(redText.AsSpan());
                    text->TextColor = red;
                    var applied = MemoryHelper.ReadRawNullTerminated((nint)(byte*)text->NodeText.StringPtr).Append((byte)0).ToArray();
                    painted[(nint)text] = new(name, bytes, applied, originalColor, red);
                }
            }
        }
    }

    private static bool Same(ByteColor a, ByteColor b) => a.R == b.R && a.G == b.G && a.B == b.B && a.A == b.A;
    private void RestoreNode(AtkTextNode* node)
    {
        if (!painted.Remove((nint)node, out var old)) return;
        var bytes = MemoryHelper.ReadRawNullTerminated((nint)(byte*)node->NodeText.StringPtr).Append((byte)0).ToArray();
        if (bytes.SequenceEqual(old.Applied)) node->SetText(old.Original.AsSpan());
        if (Same(node->TextColor, old.AppliedColor)) node->TextColor = old.OriginalColor;
    }

    private void FinalizeAddon(AddonEvent _, AddonArgs args)
    {
        PaintLive((AddonMiragePrismPrismBoxCrystallize*)args.Addon.Address, false);
        painted.Clear();
    }

    internal void Restore()
    {
        var addon = Plugin.GameGui.GetAddonByName(AddonName);
        if (!addon.IsNull && addon.IsReady) PaintLive((AddonMiragePrismPrismBoxCrystallize*)addon.Address, false);
        painted.Clear();
    }

    public void Dispose()
    {
        Restore();
        Plugin.Addons.UnregisterListener(AddonEvent.PreDraw, AddonName, Draw);
        Plugin.Addons.UnregisterListener(AddonEvent.PreFinalize, AddonName, FinalizeAddon);
    }
}
