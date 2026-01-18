namespace ILCCL.Patches;

[HarmonyPatch]
internal class SelectMenuScrollPatch
{
    
    /*
     * Patch:
     * - Enables scrolling in selection lists.
     */
    [HarmonyPatch(typeof(UnmappedMenus), nameof(UnmappedMenus.sp))]
    [HarmonyPostfix]
    public static void Menus_sp_Pre()
    {
        var d = Input.mouseScrollDelta;
        if (d.y != 0)
        {
            MappedMenus.scrollSpeedY -= 20f * d.y / MappedGlobals.resY;
            MappedMenus.scrollDelay = 200f;
        }
    }
}