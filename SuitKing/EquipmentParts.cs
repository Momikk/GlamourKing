namespace SuitKing;

internal static class EquipmentParts
{
    public static readonly string[] Order = ["主手", "副手", "头部", "身体", "手部", "腿部", "脚部", "耳部", "颈部", "腕部", "戒指"];
    public static int Rank(string part) => Array.IndexOf(Order, part) is var rank && rank >= 0 ? rank : 99;
    public static string Group(string part) => part switch
    {
        "主手" or "副手" => "主手 / 副手",
        "头部" or "身体" or "手部" => "头部 / 身体 / 手部",
        "腿部" or "脚部" => "腿部 / 脚部",
        "耳部" or "颈部" => "耳部 / 颈部",
        "腕部" or "戒指" => "腕部 / 戒指",
        _ => "其他",
    };
}
