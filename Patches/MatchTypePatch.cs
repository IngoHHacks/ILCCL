using UnityEngine.SceneManagement;
using ILCCL.API;

namespace ILCCL.Patches;

[HarmonyPatch]
public class MatchTypePatch
{
    private static float OldPresetMin;
    private static float OldCageMax;
    private static float OldCageMin;
    private static float OldRewardMax;
    private static float OldRewardMin;

    /*
     * Patch:
     * - Adds custom match types from the selection menu.
     */
    [HarmonyPatch(typeof(UnmappedMenu), nameof(UnmappedMenu.rm))]
    [HarmonyPrefix]
    private static void Menu_rm_Pre1(UnmappedMenu __instance, float __result, ref float a, float b, float c, ref float d, ref float e, int f)
    {
        if (SceneManager.GetActiveScene().name != "Match_Setup") return;
        if (q.fkn == 2)
        {
            if (q.fik[1] == __instance)
            {
                OldPresetMin = d;
                d -= CustomMatch.CustomPresetsNeg.Count;
                if (a <= -10000)
                {
                    a = a + 10000 - OldPresetMin;
                }
            }
        }    
    }
    
    /*
     * Patch:
     * - Second patch for custom match types from the selection menu.
     */
    [HarmonyPatch(typeof(UnmappedMenu), nameof(UnmappedMenu.rm))]
    [HarmonyPostfix]
    private static void Menu_rm_Post1(UnmappedMenu __instance, ref float __result)
    {
        if (SceneManager.GetActiveScene().name != "Match_Setup") return;
        if (q.fkn == 2)
        {
            if (q.fik[1] == __instance)
            {
                if (__result < OldPresetMin) __result = -10000 - (OldPresetMin - __result);
            }
        }
    }
    
    /*
     * Patch:
     * - Adds custom cages to the selection menu.
     */
    [HarmonyPatch(typeof(UnmappedMenu), nameof(UnmappedMenu.rm))]
    [HarmonyPrefix]
    private static void Menu_rm_Pre2(UnmappedMenu __instance, float __result, ref float a, float b, float c, ref float d, ref float e, int f)
    {
        if (SceneManager.GetActiveScene().name != "Match_Setup") return;
        if (UnmappedMenus.fkn == 1 && UnmappedMenus.fid == 1)
        {
            if (UnmappedMenus.fik[9] == __instance)
            {
                ChangeHardcodedPrefix(ref OldCageMin, ref d, CustomMatch.CustomCagesNeg.Count, ref OldCageMax, ref e, CustomMatch.CustomCagesPos.Count, ref a);
            }
        }
    }
    
    /*
     * Patch:
     * - Second patch for custom cages to the selection menu.
     */
    [HarmonyPatch(typeof(UnmappedMenu), nameof(UnmappedMenu.rm))]
    [HarmonyPostfix]
    private static void Menu_rm_Post2(UnmappedMenu __instance, ref float __result, float a, float b, float c, ref float d, ref float e, int f)
    {
        if (SceneManager.GetActiveScene().name != "Match_Setup") return;
        if (UnmappedMenus.fkn == 1 && UnmappedMenus.fid == 1)
        {
            if (UnmappedMenus.fik[9] == __instance)
            {
                ChangeHardcodedPostfix(ref __result, OldCageMin, OldCageMax);
            }
        }
    }

    /*
     * Patch:
     * - Adds custom cages to the selection menu.
     */
    [HarmonyPatch(typeof(UnmappedMenu), nameof(UnmappedMenu.rm))]
    [HarmonyPrefix]
    private static void Menu_rm_Pre3(UnmappedMenu __instance, float __result, ref float a, float b, float c, ref float d, ref float e, int f)
    {
        if (SceneManager.GetActiveScene().name != "Match_Setup") return;
        if (q.fkn == 2)
        {
            if (q.fik[5] == __instance)
            {
                ChangeHardcodedPrefix(ref OldRewardMin, ref d, CustomMatch.CustomRewardsNeg.Count, ref OldRewardMax, ref e, CustomMatch.CustomRewardsPos.Count, ref a);
            }
        }
    }

    /*
     * Patch:
     * - Second patch for custom cages to the selection menu.
     */
    [HarmonyPatch(typeof(UnmappedMenu), nameof(UnmappedMenu.rm))]
    [HarmonyPostfix]
    private static void Menu_rm_Post3(UnmappedMenu __instance, ref float __result, float a, float b, float c, ref float d, ref float e, int f)
    {
        if (SceneManager.GetActiveScene().name != "Match_Setup") return;
        if (q.fkn == 2)
        {
            if (q.fik[5] == __instance)
            {
                ChangeHardcodedPostfix(ref __result, OldRewardMin, OldRewardMax);
            }
        }
    }

    private static void ChangeHardcodedPrefix(ref float OldMin, ref float GameMin, int NegativesCount, ref float OldMax, ref float GameMax, int PositivesCount, ref float CurrentValue)
    {
        OldMax = GameMax;
        OldMin = GameMin;
        GameMin -= NegativesCount;
        GameMax += PositivesCount;
        if (CurrentValue >= 10000)
        {
            CurrentValue = CurrentValue - 10000 + OldMax;
        }
        if (CurrentValue <= -10000)
        {
            CurrentValue = CurrentValue + 10000 - OldMin;
        }
    }
    private static void ChangeHardcodedPostfix(ref float __result, float OldMin, float OldMax)
    {
        if (__result > OldMax)
        {
            __result = 10000 + (__result - OldMax);
        }
        if (__result < OldMin) __result = -10000 - (OldMin - __result);
    }
}