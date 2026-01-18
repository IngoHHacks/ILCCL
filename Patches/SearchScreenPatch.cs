using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ILCCL.Patches;

[HarmonyPatch]
internal class SearchScreenPatch
{

    /*
     * Patch:
     * - Enables search screen in the character select menu.
     */
    [HarmonyPatch(typeof(bu), nameof(bu.Update))]
    [HarmonyPrefix]
    public static bool bu_Update(bu __instance)
    {
        if (Plugin.EnableCharacterSearchScreen.Value)
        {
            if (Characters.filter == -1)
            {
                HandleKeybinds(__instance);
            }
        }
        return true;
    }
    
    private static void HandleKeybinds(bu __instance)
    {
        // Delete
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Delete))
        {
            if (Characters.foc > 0 && MappedMenus.foc > 0)
            {
                if (Characters.no_chars == 1)
                {
                    Sound.Play(MappedSound.block);
                    LogInfo("You can't delete the last character!");
                    return;
                }
                
                Sound.Play(MappedSound.death[3]);
                LogInfo("Deleting character " + Characters.c[Characters.foc].name);
                CharacterUtils.DeleteCharacter(Characters.foc);
                if (Characters.foc > Characters.no_chars)
                {
                    Characters.foc = Characters.no_chars;
                }
                MappedMenus.foc--;
                for (int m = 1; m <= MappedPlayers.no_plays; m++)
                {
                    if (Characters.profileChar[m] > 0)
                    {
                        Characters.profileChar[m] = 0;
                    }
                }
                __instance.oldFilter = -516391;
                __instance.oldPage = -516391;
                MappedMenus.oldFoc = -1;
                MappedMenus.PopulatePages();
                MappedMenus.Load();
                var newFoc = Characters.foc;
                Characters.foc = -1;
                ((MappedScene_Select_Char)__instance).UpdatePortrait(newFoc);
                MappedSaveSystem.request = 1;
            }
        }
        // New
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.N))
        {
            Sound.Play(MappedSound.tanoy);
            LogInfo("Creating new character");
            int CharID = CharacterUtils.CreateRandomCharacter();
            __instance.oldFilter = -516391;
            __instance.oldPage = -516391;
            Characters.foc = CharID;
            MappedMenus.oldFoc = -1;
            MappedMenus.PopulatePages();
            MappedMenus.Load();  //refresh the character selection pages
            Characters.foc = -1;
            ((MappedScene_Select_Char) __instance).UpdatePortrait(CharID);
            MappedSaveSystem.request = 1;
        }
    }
    
    private static List<GameObject> _tempObjects = new();

    /*
     * Patch:
     * - Loads menus for the search screen.
     */
    [HarmonyPatch(typeof(UnmappedMenus), nameof(UnmappedMenus.rz))]
    [HarmonyPostfix]
    public static void Menus_rz()
    {
        if (MappedMenus.screen == 11 && Characters.filter == -1)
        {
            GameObject obj = Object.Instantiate(MappedSprites.gMenu[1]);
            _tempObjects.Add(obj);
            obj.transform.position = new Vector3(-400f, -270f, 0f);
            obj.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            RectTransform rt = obj.transform.Find("Title").transform as RectTransform;
            rt.sizeDelta *= 5;
            obj.transform.SetParent(MappedMenus.gDisplay.transform, false);
            Object.Destroy(obj.transform.Find("Background").gameObject);
            Object.Destroy(obj.transform.Find("Border").gameObject);
            Object.Destroy(obj.transform.Find("Highlight").gameObject);
            obj.transform.Find("Title").gameObject.GetComponent<Text>().text =
                "Press [Ctrl+DEL] to delete the selected character.";

            obj = Object.Instantiate(MappedSprites.gMenu[1]);
            _tempObjects.Add(obj);
            obj.transform.position = new Vector3(-400f, -300f, 0f);
            obj.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            rt = obj.transform.Find("Title").transform as RectTransform;
            rt.sizeDelta *= 5;
            obj.transform.SetParent(MappedMenus.gDisplay.transform, false);
            Object.Destroy(obj.transform.Find("Background").gameObject);
            Object.Destroy(obj.transform.Find("Border").gameObject);
            Object.Destroy(obj.transform.Find("Highlight").gameObject);
            obj.transform.Find("Title").gameObject.GetComponent<Text>().text =
                "Press [Ctrl+N] to create a new character.";
        }
        else
        {
            if (_tempObjects.Count > 0)
            {
                foreach (GameObject obj in _tempObjects)
                {
                    if (obj != null)
                    {
                        Object.Destroy(obj);
                    }
                }
                _tempObjects.Clear();
            }
        }

        if (MappedMenus.screen == 2)
        {
            if (MappedMenus.tab == 6)
            {
                MappedMenus.Add();
                ((MappedMenu)MappedMenus.menu[MappedMenus.no_menus]).Load(2, "Mod", 0, 220, 1.5f, 1.5f);
            }
        }
    }
}