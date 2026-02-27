using BepInEx.Logging;
using GameConsole.pcon;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace StyleGoRound;

/*
 * ==================================================================================
 * 【处理前 (Original IL)】
 * ==================================================================================
 * IL_0270: ldarg.0                          // 栈: [this]
 * IL_0271: ldarg.0                          // 栈: [this, this]
 * IL_0272: ldfld  Enemy::health             // 栈: [this, health_val]
 * IL_0277: ldloc.s 7                        // 栈: [this, health_val, damage_num]
 * IL_0279: sub                              // 栈: [this, (health - damage)]
 * IL_027a: stfld  Enemy::health             // 栈: [] (赋值完成)
 * * ==================================================================================
 * 【处理后 (Patched IL)】
 * ==================================================================================
 * IL_0270: ldarg.0                          // 栈: [this]
 * IL_0271: ldarg.0                          // 栈: [this, this]
 * IL_0272: ldfld  Enemy::health             // 栈: [this, health_val]
 * IL_0277: ldloc.s 7                        // 栈: [this, health_val, damage_num]
 * * // >>>> 注入逻辑：判断是否拦截 >>>>
 * dup                                       // 栈: [..., damage_num, damage_num] (复制副本用于检测)
 * call   bool DamageCheck(float32)          // 栈: [..., damage_num, bool_result] (执行检测)
 * brtrue.s normalDamage                     // 栈: [..., damage_num] (如果条件达成/返回True, 跳过拦截逻辑)
 * * // ---- 拦截分支 (Result 为 False 时执行) ----
 * pop                                       // 栈: [this, health_val] (扔掉原始 damage_num)
 * ldc.r4   0.0                              // 栈: [this, health_val, 0.0] (塞入 0 替代它)
 * * // >>>> 注入逻辑结束 >>>>
 * * normalDamage:                             // [锚点位置]
 * IL_0279: sub                              // 栈: [this, (health - 实际值)] (实际值可能是原伤害或0)
 * IL_027a: stfld  Enemy::health             // 栈: [] (存入字段)
 * ==================================================================================
 */

public static class ILHelper
{
    public static IEnumerable<CodeInstruction> Insert(IEnumerable<CodeInstruction> instructions, ILGenerator il)
    {
        var matcher = new CodeMatcher(instructions, il);
        var fieldHealth = AccessTools.Field(typeof(Enemy), nameof(Enemy.health));
        var methodChecker = AccessTools.Method(typeof(GetHurtChecker), nameof(GetHurtChecker.DamageCheck));

        while (true)
        {
            matcher.MatchForward(false, 
                new CodeMatch(OpCodes.Sub), 
                new CodeMatch(OpCodes.Stfld, fieldHealth)
            );

            if (matcher.IsInvalid) break; // 

            Label normalDamage = il.DefineLabel();

            matcher.InsertAndAdvance(new[] {
                new CodeInstruction(OpCodes.Dup),                    // copy damage at top of stack
                new CodeInstruction(OpCodes.Call, methodChecker),    // call DamageCheck(float)
                new CodeInstruction(OpCodes.Brtrue_S, normalDamage), // if return true，goto normalDamage label
                new CodeInstruction(OpCodes.Pop),                    // else，pop out damage
                new CodeInstruction(OpCodes.Ldc_R4, 0f)              // push 0 for health - 0
            });

            // add label to sub
            matcher.AddLabels(new[] { normalDamage });

            // skip the sub and stfld, continue searching for next match
            matcher.Advance(2);
        }

        return matcher.InstructionEnumeration();
    }
}


[HarmonyPatch(typeof(Enemy))]
public static class EnemyPatch
{
    private static MethodInfo methodChecker = AccessTools.Method(typeof(GetHurtChecker), nameof(GetHurtChecker.DamageCheck));
    
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(Enemy.GetHurt))]
    public static IEnumerable<CodeInstruction> GetHurtTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
    {
        return ILHelper.Insert(instructions, il);
    }

    [HarmonyTranspiler]
    [HarmonyPatch(nameof(Enemy.HandleParrying))]
    public static IEnumerable<CodeInstruction> HandleParryingTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
    {
        return ILHelper.Insert(instructions, il);
    }

}

[HarmonyPatch(typeof(EnemyIdentifier))]
public static class EnemyIdentifierPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(EnemyIdentifier.Explode))]
    public static bool ExplosionPrefix()
    {
        var stackTrace = new System.Diagnostics.StackTrace();
        var callingMethod = stackTrace.GetFrame(2).GetMethod();
        string callerClassName = callingMethod.DeclaringType.Name;
        LogHelper.LogDebug($"Explosion triggered by: {callerClassName}");
        if (callerClassName == "DeathZone") return true;

        if (GetHurtChecker.IsEnoughRank() == false)
        {
            return false;
        }
        LogHelper.LogDebug("explosion prevented due to low rank.");
        return true;
    }
}