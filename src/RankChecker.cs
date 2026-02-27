using System;

namespace StyleGoRound;

public static class GetHurtChecker
{
    public static bool IsEnoughRank()
    {
        try
        {
            return global::StyleHUD.Instance.rankIndex > 3;
        }
        catch(Exception e)
        {
            LogHelper.LogError("Error checking rank: " + e.Message);
            return true;
        }
    }

    public static bool IsDamageOverLimit(float damage)
        => damage > 500f;

    public static bool DamageCheck(float damage)
    {
        LogHelper.LogInfo($"Enough rank: {IsEnoughRank()}, Damage over limit: {IsDamageOverLimit(damage)}");
        return IsEnoughRank() || IsDamageOverLimit(damage);   
    }
}