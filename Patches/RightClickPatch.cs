namespace ILCCL.Patches;

[HarmonyPatch]
internal class RightClickPatch
{
    private static int _rcFoc = 0;
    
    public static int RightClickFoc {
        get => _rcFoc;
        set => _rcFoc = value;
    }
    
    /*
     * Patch:
     * - Shows list when 'Music' is right clicked in the editor.
     */
    [HarmonyPatch(typeof(bj), nameof(bj.Update))]
    [HarmonyPostfix]
    public static void Scene_Editor_Update(bj __instance)
    {
        if (_rcFoc > 0 && MappedMenus.page == 0 && MappedMenus.foc == 8 && MappedMenus.tab == 1)
        {
            Sound.Play(MappedSound.proceed, 1f);
            MappedMenus.tabOldFoc[MappedMenus.tab] = MappedMenus.foc;
            MappedMenus.CreateAlphabet();
        }
    }
}