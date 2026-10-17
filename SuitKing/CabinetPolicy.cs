namespace SuitKing;

internal static class CabinetPolicy
{
    internal static OutfitAssessment Assess(OutfitAssessment outfit, uint? cabinetId, bool? stored, bool? registered)
    {
        if (cabinetId is null || registered != false) return outfit;
        if (stored == false) return new(OutfitStorageState.CanStoreCabinet, "收藏柜尚未收录，请优先存入收藏柜。");
        if (stored is null)
            return new(OutfitStorageState.Unknown, "收藏柜收录数据尚未加载，请先打开收藏柜确认。");
        return outfit;
    }

    internal static bool CanStore(uint? cabinetId, bool? stored, bool? registered, int copyCount, bool nativeEligible)
        => cabinetId.HasValue && stored == false && registered == false && copyCount == 1 && nativeEligible;
}