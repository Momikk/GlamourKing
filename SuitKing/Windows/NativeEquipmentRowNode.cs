using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Interfaces;
using KamiToolKit.Nodes;
using Lumina.Data.Parsing.Uld;

namespace SuitKing.Windows;

internal sealed record NativeEquipmentRow(string? Group, OutfitEquipment? Equipment);

internal sealed class NativeEquipmentRowNode : ListItemNode<NativeEquipmentRow>, IListItemNode
{
    public static float ItemHeight => 28;
    private readonly IconImageNode icon;
    private readonly TextNode group;
    private readonly TextNode[] cells;
    private readonly HorizontalLineNode line;

    public NativeEquipmentRowNode()
    {
        // The list is informational; hovering shows details without triggering inventory actions.
        EnableSelection = false;
        // Node size alone does not scale AtkImageNode's texture. FitTexture enables
        // AutoFit + Stretch, using the complete loaded icon inside the 24 px box.
        icon = new IconImageNode
        {
            Position = new Vector2(2, 2), Size = new Vector2(24), FitTexture = true,
        };
        icon.AttachNode(this);
        group = new TextNode { FontSize = 12, AlignmentType = AlignmentType.Left };
        group.AttachNode(this);
        cells = Enumerable.Range(0, 4).Select(_ => new TextNode
        {
            FontSize = 13, AlignmentType = AlignmentType.Left, TextFlags = TextFlags.Ellipsis,
        }).ToArray();
        foreach (var cell in cells) cell.AttachNode(this);
        line = new HorizontalLineNode { Height = 2, Y = 26 };
        line.AttachNode(this);
    }

    internal static (float X, float Width) Column(int index, float width)
    {
        var nameWidth = (width - 172) * .52f;
        var setWidth = width - 172 - nameWidth;
        return index switch
        {
            0 => (0, 32), 1 => (32, nameWidth), 2 => (32 + nameWidth, 48),
            3 => (80 + nameWidth, setWidth), _ => (width - 92, 92),
        };
    }

    protected override void OnSizeChanged()
    {
        base.OnSizeChanged();
        if (cells is null) return;
        group.Size = new Vector2(Width - 8, 28);
        group.Position = new Vector2(4, 0);
        line.Width = Width;
        for (var i = 0; i < cells.Length; i++)
        {
            var (x, width) = Column(i + 1, Width);
            cells[i].Position = new Vector2(x, 0);
            cells[i].Size = new Vector2(Math.Max(1, width - 4), 28);
        }
    }

    protected override void SetNodeData(NativeEquipmentRow row)
    {
        var item = row.Equipment;
        var isGroup = item is null;
        group.IsVisible = isGroup;
        icon.IsVisible = !isGroup && item!.IconId != 0;
        foreach (var cell in cells) cell.IsVisible = !isGroup;
        if (isGroup)
        {
            group.String = row.Group ?? "";
            TextTooltip = "";
            EnableHighlight = false;
            return;
        }
        EnableHighlight = true;
        icon.IconId = item!.IconId;
        cells[0].String = item.Name;
        cells[1].String = item.Part;
        cells[2].String = item.SetNames;
        var state = item.Assessment?.State ?? OutfitStorageState.Unknown;
        cells[3].String = state switch
        {
            OutfitStorageState.Saved => "已保存", OutfitStorageState.CanComplete => "可补齐",
            OutfitStorageState.CanStoreCabinet => "可存收藏柜", OutfitStorageState.NotCreated => "未建立", _ => "待确认",
        };
        // Use game theme colours so labels remain legible with the light and dark native skins.
        cells[3].TextColorID = state switch
        {
            OutfitStorageState.Saved => 43, OutfitStorageState.CanComplete => 31,
            OutfitStorageState.CanStoreCabinet => 31, OutfitStorageState.NotCreated => 37, _ => 8,
        };
        TextTooltip = $"{item.Name}\n{item.SetNames}\n{item.Location} · 第 {item.Slot} 格 · {(item.HighQuality ? "HQ" : "NQ")}\n{item.Assessment?.Details ?? "等待套装数据。"}";
    }
}