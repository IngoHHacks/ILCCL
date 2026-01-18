using Newtonsoft.Json;
using System.Reflection.Emit;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine.SceneManagement;
using ILCCL.API.Events;
using ILCCL.Content;
using ILCCL.Saves;
using System.Reflection;

namespace ILCCL.Patches;

[HarmonyPatch]
internal class SaveFilePatch
{
    /*
     * Patch
     * - Resets the character and federation counts when default data is loaded.
     * - Resets the star to 1 if they are greater than the new character count when default data is loaded.
     */
    [HarmonyPatch(typeof(UnmappedSaveSystem), nameof(UnmappedSaveSystem.bxk))]
    [HarmonyPrefix]
    public static void SaveSystem_bxk_Pre()
    {
        if (SceneManager.GetActiveScene().name == "Loading")
        {
            return;
        }
        try
        {
            Characters.no_chars = 200;

            if (Characters.star > 200)
            {
                Characters.star = 1;
            }
            
            Array.Resize(ref Characters.c, Characters.no_chars + 1);
            Array.Resize(ref Progress.charUnlock, Characters.no_chars + 1);
            Array.Resize(ref bf.gqu.charUnlock, Characters.no_chars + 1);
            Array.Resize(ref bf.gqu.savedChars, Characters.no_chars + 1);
            Array.Resize(ref Characters.history, Characters.no_chars + 1);
            Array.Resize(ref bf.gqu.history, Characters.no_chars + 1);
            for (int i = 1; i <= Characters.no_chars; i++)
            {
                if (bf.gqu.savedChars[i] == null)
                {
                    Characters.c[i] = MappedCharacters.CopyClass(Characters.c[1]);
                    bf.gqu.savedChars[i] = MappedCharacters.CopyClass(bf.gqu.savedChars[1]);
                }
            }
            for (int i = 0; i <= Characters.no_chars; i++)
            {
                if (Characters.c[i] == null)
                {
                    continue;
                }
                Array.Resize(ref Characters.c[i].relation, Characters.no_chars + 1);
                Array.Resize(ref Characters.c[i].knowledge, Characters.no_chars + 1);
            }
        }
        catch (Exception e)
        {
            LogError(e);
        }
    }

    /*
     * Patch:
     * - Clears the previously imported characters list after default data is loaded.
     * - Fixes corrupted save data and relations after default data is loaded.
     */
    [HarmonyPatch(typeof(UnmappedSaveSystem), nameof(UnmappedSaveSystem.bxk))]
    [HarmonyPostfix]
    public static void SaveSystem_bxk_Post()
    {
        if (SceneManager.GetActiveScene().name == "Loading")
        {
            return;
        }
        for (int i = 0; i <= Characters.no_chars; i++)
        {
            if (Characters.c[i] == null)
            {
                continue;
            }
            Array.Resize(ref Characters.c[i].relation, Characters.no_chars + 1);
            Array.Resize(ref Characters.c[i].knowledge, Characters.no_chars + 1);
        }
        try
        {
            SaveRemapper.FixBrokenSaveData();
            
            CharacterMappings.CharacterMap.PreviouslyImportedCharacters.Clear();
            CharacterMappings.CharacterMap.PreviouslyImportedCharacterIds.Clear();
        }
        catch (Exception e)
        {
            LogError(e);
        }
        
    }
    
    /*
     * Patch:
     * - Fixes corrupted save data before rosters are loaded.
     */
    [HarmonyPatch(typeof(SaveData), nameof(SaveData.bxa))]
    [HarmonyPrefix]
    public static void SaveData_bxa_Pre(SaveData __instance, int a)
    {
        if (a > 0) {
            try
            {
                Characters.no_chars = __instance.backupChars.Length;

                if (Characters.star > Characters.no_chars)
                {
                    Characters.star = 1;
                }
                Array.Resize(ref Characters.c, Characters.no_chars + 1);
                Array.Resize(ref Progress.charUnlock, Characters.no_chars + 1);
                Array.Resize(ref bf.gqu.charUnlock, Characters.no_chars + 1);
                Array.Resize(ref bf.gqu.savedChars, Characters.no_chars + 1);
                Array.Resize(ref Characters.history, Characters.no_chars + 1);
                Array.Resize(ref bf.gqu.history, Characters.no_chars + 1);
                for (int i = 1; i <= Characters.no_chars; i++)
                {
                    if (bf.gqu.savedChars[i] == null)
                    {
                        Characters.c[i] = MappedCharacters.CopyClass(Characters.c[1]);
                        bf.gqu.savedChars[i] = MappedCharacters.CopyClass(bf.gqu.savedChars[1]);
                    }
                }
                for (int i = 0; i <= Characters.no_chars; i++)
                {
                    if (Characters.c[i] == null)
                    {
                        continue;
                    }
                    Array.Resize(ref Characters.c[i].relation, Characters.no_chars + 1);
                    Array.Resize(ref Characters.c[i].knowledge, Characters.no_chars + 1);
                }
                CharacterMappings.CharacterMap.PreviouslyImportedCharacters.Clear();
                CharacterMappings.CharacterMap.PreviouslyImportedCharacterIds.Clear();
            }
            catch (Exception e)
            {
                LogError(e);
            }
        }
        else {
            if (bf.gqu.savedChars != null)
            {
                SaveRemapper.FixBrokenSaveData();
            }
        }
    }

    /*
     * Patch:
     * - Fixes relations after loading backup data
     */
    [HarmonyPatch(typeof(SaveData), nameof(SaveData.bxa))]
    [HarmonyPostfix]
    public static void SaveData_bxa_Post(SaveData __instance, int a)
    {
        for (int i = 0; i <= Characters.no_chars; i++)
        {
            if (Characters.c[i] == null)
            {
                continue;
            }
            Array.Resize(ref Characters.c[i].relation, Characters.no_chars + 1);
            Array.Resize(ref Characters.c[i].knowledge, Characters.no_chars + 1);
        }
    }
    
    
    /*
     * Patch:
     * - Changes the save file name to 'ModdedSave.bytes' (or whatever the user has set) during user load.
     */
    [HarmonyPatch(typeof(UnmappedSaveSystem), nameof(UnmappedSaveSystem.bxm))]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> SaveSystem_bxm(IEnumerable<CodeInstruction> instructions)
    {
        foreach (CodeInstruction instruction in instructions)
        {
            if (instruction.opcode == OpCodes.Ldstr && instruction.operand is string str && str == "Save")
            {
                instruction.operand = Plugin.SaveFileName.Value;
            }
            if (instruction.opcode == OpCodes.Call && instruction.operand is MethodInfo methodInfo && IsStringGetter(methodInfo) && StringGetterValue(methodInfo) == "Save")
            {
                instruction.opcode = OpCodes.Ldstr;
                instruction.operand = Plugin.SaveFileName.Value;
            }
            yield return instruction;
        }
    }
    
    /*
     * Patch:
     * - Changes the save file name to 'ModdedSave.bytes' (or whatever the user has set) during user save.
     */
    [HarmonyPatch(typeof(UnmappedSaveSystem), nameof(UnmappedSaveSystem.bxl))]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> SaveSystem_bxl_Trans(IEnumerable<CodeInstruction> instructions)
    {
        foreach (CodeInstruction instruction in instructions)
        {
            if (instruction.opcode == OpCodes.Ldstr && instruction.operand is string str && str == "Save")
            {
                instruction.operand = Plugin.SaveFileName.Value;
            }
            if (instruction.opcode == OpCodes.Call && instruction.operand is MethodInfo methodInfo && IsStringGetter(methodInfo) && StringGetterValue(methodInfo) == "Save")
            {
                instruction.opcode = OpCodes.Ldstr;
                instruction.operand = Plugin.SaveFileName.Value;
            }
            yield return instruction;
        }
    }
    
    private static bool IsStringGetter(MethodInfo methodInfo)
    {
        return methodInfo.ReturnType == typeof(string) && methodInfo.GetParameters().Length == 0 && methodInfo.IsStatic;
    }
    
    private static string StringGetterValue(MethodInfo methodInfo)
    {
        return methodInfo.Invoke(null, null) as string;
    }

    /*
     * SaveData.bxm is called when the game loads the save file.
     * This prefix patch is used to update character counts and arrays to accommodate the custom content.
     */
    [HarmonyPatch(typeof(bf), nameof(bf.bxm))]
    [HarmonyPrefix]
    public static void SaveData_bxm_PRE(int a)
    {
        try
        {
            string save = Locations.SaveFile.FullName;
            if (!File.Exists(save))
            {
                string  vanillaSave = Locations.SaveFileVanilla.FullName;
                if (File.Exists(vanillaSave))
                {
                    File.Copy(vanillaSave, save);
                }
                else
                {
                    return;
                }
            }

            FileStream fileStream = new(save, FileMode.Open);
            SaveData data = new BinaryFormatter().Deserialize(fileStream) as SaveData;
            Characters.no_chars = data!.savedChars.Length - 1;

            Array.Resize(ref Characters.c, Characters.no_chars + 1);
            Array.Resize(ref Progress.charUnlock, Characters.no_chars + 1);
            Array.Resize(ref bf.gqu.charUnlock, Characters.no_chars + 1);
            Array.Resize(ref Characters.history, Characters.no_chars + 1);
            Array.Resize(ref bf.gqu.history, Characters.no_chars + 1);
            for (int i = 1; i <= Characters.no_chars; i++)
            {
                if (Characters.c[i] == null)
                {
                    continue;
                }
                Array.Resize(ref Characters.c[i].relation, Characters.no_chars + 1);
                Array.Resize(ref Characters.c[i].knowledge, Characters.no_chars + 1);
            }
            fileStream.Close();
        }
        catch (Exception e)
        {
            LogError(e);
        }
    }

    /*
     * This postfix patch is used to remap any custom content that has moved, and also add the imported characters.
     */
    [HarmonyPatch(typeof(bf), nameof(bf.bxm))]
    [HarmonyPostfix]
    public static void SaveData_bxm_POST(int a)
    {
        string save = Locations.SaveFile.FullName;
        if (!File.Exists(save))
        {
            string vanillaSave = Locations.SaveFileVanilla.FullName;
            if (File.Exists(vanillaSave))
            {
                File.Copy(vanillaSave, save);
            }
            else
            {
                return;
            }
        }

        try
        {
            SaveRemapper.FixBrokenSaveData();
            SaveRemapper.PatchCustomContent(ref bf.gqu);
            foreach (BetterCharacterDataFile file in ImportedCharacters)
            {
                string nameWithGuid = file._guid;
                string overrideMode = file.OverrideMode + "-" + file.FindMode;
                overrideMode = overrideMode.ToLower();
                if (overrideMode.EndsWith("-"))
                {
                    overrideMode = overrideMode.Substring(0, overrideMode.Length - 1);
                }

                try
                {
                    bool previouslyImported = CheckIfPreviouslyImported(nameWithGuid);
                    if (previouslyImported)
                    {
                        LogInfo(
                            $"Character with name {file.CharacterData.name ?? "null"} was previously imported. Skipping.");
                        continue;
                    }
                    if (!overrideMode.Contains("append"))
                    {
                        LogInfo(
                            $"Importing character {file.CharacterData.name ?? "null"} with id {file.CharacterData.id.ToString() ?? "null"} using mode {overrideMode}");
                    }
                    else
                    {
                        LogInfo(
                            $"Appending character {file.CharacterData.name ?? "null"} to next available id using mode {overrideMode}");
                    }
                    

                    Character importedCharacter = null;
                    if (!overrideMode.Contains("merge"))
                    {
                        importedCharacter = file.CharacterData.ToRegularCharacter(bf.gqu.savedChars);
                    }
                    switch (overrideMode)
                    {
                        case "override-id":
                        case "override-name":
                        case "override-name_then_id":
                            int id = overrideMode.Contains("id") ? importedCharacter.id : -1;
                            if (overrideMode.Contains("name"))
                            {
                                string find = file.FindName ?? importedCharacter.name;
                                try
                                {
                                    id = bf.gqu.savedChars
                                        .Single(c => c != null && c.name != null && c.name == find).id;
                                }
                                catch (Exception)
                                {
                                    // ignored
                                }
                            }

                            if (id == -1)
                            {
                                LogWarning(
                                    $"Could not find character with id {importedCharacter.id} and name {importedCharacter.name} using override mode {overrideMode}. Skipping.");
                                break;
                            }

                            for (var i = 1; i <= Characters.no_chars; i++) {
                                if (Characters.c[i] == null)
                                {
                                    continue;
                                }
                                Characters.c[i].relation[id] = importedCharacter.relation[i];
                                if (bf.gqu.savedChars[i] == null)
                                {
                                    continue;
                                }
                                bf.gqu.savedChars[i].relation[id] = importedCharacter.relation[i];
                            }

                            Character oldCharacter = bf.gqu.savedChars[id];
                            string name = importedCharacter.name;
                            string oldCharacterName = oldCharacter.name;
                            bf.gqu.savedChars[id] = importedCharacter;

                            LogInfo(
                                $"Imported character with id {id} and name {name}, overwriting character with name {oldCharacterName}.");
                            break;
                        case "append":
                            LogInfo($"Appending character {importedCharacter.name ?? "null"} to next available id.");
                            int id2 = Characters.no_chars + 1;
                            importedCharacter.id = id2;
                            CharacterEvents.InvokeBeforeCharacterAdded(id2, importedCharacter, CharacterAddedEvent.Source.Import);
                            Characters.no_chars++;
                            try
                            {
                                if (bf.gqu.savedChars.Length <= id2)
                                {
                                    Array.Resize(ref bf.gqu.savedChars, Characters.no_chars + 1);
                                    Array.Resize(ref bf.gqu.charUnlock, Characters.no_chars + 1);
                                    Array.Resize(ref Characters.c, Characters.no_chars + 1);
                                    Array.Resize(ref Progress.charUnlock, Characters.no_chars + 1);
                                    Array.Resize(ref Characters.history, Characters.no_chars + 1);
                                    Array.Resize(ref bf.gqu.history, Characters.no_chars + 1);
                                    for (int i = 1; i <= Characters.no_chars; i++)
                                    {
                                        if (Characters.c[i] == null)
                                        {
                                            continue;
                                        }
                                        Array.Resize(ref Characters.c[i].relation, Characters.no_chars + 1);
                                        if (bf.gqu.savedChars[i] == null)
                                        {
                                            continue;
                                        }
                                        Array.Resize(ref bf.gqu.savedChars[i].relation, Characters.no_chars + 1);
                                    }
                                    bf.gqu.charUnlock[id2] = 1;
                                    Progress.charUnlock[id2] = 1;
                                    bf.gqu.history[id2] = 0;
                                    Characters.history[id2] = 0;
                                }
                                else
                                {
                                    LogWarning(
                                        $"The array of characters is larger than the number of characters. This should not happen. The character {bf.gqu.savedChars[id2].name} will be overwritten.");
                                }

                                for (var i = 1; i <= Characters.no_chars; i++) {
                                    if (Characters.c[i] == null)
                                    {
                                        continue;
                                    }
                                    Characters.c[i].relation[id2] = importedCharacter.relation[i];
                                    if (bf.gqu.savedChars[i] == null)
                                    {
                                        continue;
                                    }
                                    bf.gqu.savedChars[i].relation[id2] = importedCharacter.relation[i];
                                }

                                bf.gqu.savedChars[id2] = importedCharacter;
                                LogInfo(
                                    $"Imported character with id {id2} and name {importedCharacter.name}. Incremented number of characters to {Characters.no_chars}.");
                                CharacterEvents.InvokeAfterCharacterAdded(id2, importedCharacter,
                                    CharacterAddedEvent.Source.Import);
                            }
                            catch (Exception e)
                            {
                                CharacterEvents.InvokeAfterCharacterAddedFailure(id2, importedCharacter,
                                    CharacterAddedEvent.Source.Import);
                                throw new Exception($"Error while appending character {importedCharacter.name ?? "null"} to next available id.", e);
                            }

                            break;
                        case "merge-id":
                        case "merge-name":
                        case "merge-name_then_id":
                            int id3 = overrideMode.Contains("id") ? file.CharacterData.id ?? -1 : -1;
                            if (overrideMode.Contains("name"))
                            {
                                string find = file.FindName ?? file.CharacterData.name ??
                                    throw new Exception($"No name found for file {nameWithGuid}");
                                try
                                {
                                    id3 = bf.gqu.savedChars
                                        .Single(c => c != null && c.name != null && c.name == find).id;
                                }
                                catch (Exception)
                                {
                                    // ignored
                                }
                            }

                            if (id3 == -1)
                            {
                                LogWarning(
                                    $"Could not find character with id {file.CharacterData.id?.ToString() ?? "null"} and name {file.FindName ?? file.CharacterData.name ?? "null"} using override mode {overrideMode}. Skipping.");
                                break;
                            }

                            for (var i = 1; i <= Characters.no_chars; i++) {
                                if (Characters.c[i] == null)
                                {
                                    continue;
                                }
                                Characters.c[i].relation[id3] = importedCharacter.relation[i];
                                if (bf.gqu.savedChars[i] == null)
                                {
                                    continue;
                                }
                                bf.gqu.savedChars[i].relation[id3] = importedCharacter.relation[i];
                            }

                            Character oldCharacter2 = bf.gqu.savedChars[id3];
                            file.CharacterData.MergeIntoCharacter(oldCharacter2);

                            bf.gqu.savedChars[id3] = oldCharacter2;

                            LogInfo(
                                $"Imported character with id {id3} and name {file.CharacterData.name ?? "null"}, merging with existing character: {oldCharacter2.name}.");
                            break;
                        default:
                            throw new Exception($"Unknown override mode {overrideMode}");
                    }

                    CharacterMappings.CharacterMap.AddPreviouslyImportedCharacter(nameWithGuid,
                        importedCharacter?.id ?? file.CharacterData.id ?? -1);
                }
                catch (Exception e)
                {
                    LogError($"Error while importing character {nameWithGuid}.");
                    LogError(e);
                }
            }

            bf.gqu.bxa(a);
        }
        catch (Exception e)
        {
            LogError("Error while importing characters.");
            LogError(e);
        }
    }

#pragma warning disable Harmony003
    private static bool CheckIfPreviouslyImported(string nameWithGuid)
    {
        if (nameWithGuid.EndsWith(".json"))
        {
            nameWithGuid = nameWithGuid.Substring(0, nameWithGuid.Length - 5);
        }
        else if (nameWithGuid.EndsWith(".character"))
        {
            nameWithGuid = nameWithGuid.Substring(0, nameWithGuid.Length - 10);
        }
        
        return CharacterMappings.CharacterMap.PreviouslyImportedCharacters.Contains(nameWithGuid);
    }
#pragma warning restore Harmony003

    /*
     * - Saves the current custom content map and exports all characters during user save.
     */
    [HarmonyPatch(typeof(UnmappedSaveSystem), nameof(UnmappedSaveSystem.bxl))]
    [HarmonyPostfix]
    public static void SaveSystem_bxl_Post(int a)
    {
        Plugin.CreateBackups();
        SaveCurrentMap();
        CharacterMappings.CharacterMap.Save();
        MetaFile.Data.Save();

        if (NonSavedData.DeletedCharacters.Count > 0)
        {
            LogInfo($"Saving {NonSavedData.DeletedCharacters.Count} characters to purgatory.");
            foreach (Character character in NonSavedData.DeletedCharacters)
            {
                BetterCharacterData moddedCharacter = BetterCharacterData.FromRegularCharacter(character, Characters.c, true);
                BetterCharacterDataFile file = new() { characterData = moddedCharacter, overrideMode = "append" };
                string json = JsonConvert.SerializeObject(file, Formatting.Indented);
                string path = Path.Combine(Locations.DeletedCharacters.FullName, $"{character.id}_{Escape(character.name)}.character");
                if (!Directory.Exists(Locations.DeletedCharacters.FullName))
                {
                    Directory.CreateDirectory(Locations.DeletedCharacters.FullName);
                }
                File.WriteAllText(path, json);
            }
        }
        
        if (Plugin.AutoExportCharacters.Value)
        {
            ModdedCharacterManager.SaveAllCharacters();
        }

        if (Plugin.DeleteImportedCharacters.Value)
        {
            foreach (string file in FilesToDeleteOnSave)
            {
                File.Delete(file);
            }
        }
    }


    /*
    Special cases:
    BodyFemale is negative Flesh[2]
    FaceFemale is negative Material[3]
    SpecialFootwear is negative Material[14] and [15]
    TransparentHairMaterial is negative Material[17]
    TransparentHairHairstyle is negative Shape[17]
    Kneepad is negative Material[24] and [25]
     */

    internal static void SaveCurrentMap()
    {
        ContentMappings.ContentMap.Save();
    }

    internal static ContentMappings LoadPreviousMap()
    {
        return ContentMappings.Load();
    }
    
    /*
     * Patch:
     * - Increases the character limit if the user has set it to be higher than the default during user progress save.
     */
    [HarmonyPatch(typeof(SaveData), nameof(SaveData.bxb))]
    [HarmonyPrefix]
    public static void SaveData_bxb(SaveData __instance)
    {
        if (__instance.charUnlock.Length < Characters.no_chars + 1)
        {
            Array.Resize(ref __instance.charUnlock, Characters.no_chars + 1);
        }
        if (__instance.history.Length < Characters.no_chars + 1)
        {
            Array.Resize(ref __instance.history, Characters.no_chars + 1);
        }
    }
    
    /*
     * Patch:
     * - Increases the character limit if the user has set it to be higher than the default during user progress load.
     */
    [HarmonyPatch(typeof(SaveData), nameof(SaveData.bxc))]
    [HarmonyPrefix]
    public static void SaveData_bxc(SaveData __instance)
    {
        if (Progress.charUnlock.Length < __instance.savedChars.Length)
        {
            Array.Resize(ref Progress.charUnlock, __instance.savedChars.Length);
        }
        if (Progress.mapUnlock.Length < __instance.mapUnlock.Length)
        {
            Array.Resize(ref Progress.mapUnlock, __instance.mapUnlock.Length);
        }
        if (Characters.history.Length < __instance.savedChars.Length)
        {
            Array.Resize(ref Characters.history, __instance.savedChars.Length);
        }
    }
    
    /*
     * Patch:
     * - Fixes stock for resized character arrays.
     */
    [HarmonyPatch(typeof(Weapons), nameof(Weapons.bwm))]
    [HarmonyPrefix]
    public static void Weapons_bwm()
    {
        FixStock();
    }

    /*
     * Patch:
     * - Fixes stock for resized character arrays.
     */
    [HarmonyPatch(typeof(Character), nameof(Character.jd))]
    [HarmonyPrefix]
    public static void Character_jd()
    {
        FixStock();
    }

    private static void FixStock() {
        for (Weapons.gqb = 1; Weapons.gqb < Weapons.gpz.Length; Weapons.gqb++)
        {
            if (Weapons.gpz[Weapons.gqb].holder > Characters.no_chars)
            {
                Weapons.gpz[Weapons.gqb].holder = 0;
            }
        }
    }
    
    private static int _origCharCount;
    
    /*
     * Patch:
     * - Adds dummy characters to the "default data" to prevent index out of range exceptions.
     */
    [HarmonyPatch(typeof(SaveData), nameof(SaveData.bxg))]
    [HarmonyPrefix]
    public static void SaveData_bxg_Pre()
    {
        _origCharCount = bf.gqt.savedChars.Length - 1;
        Array.Resize(ref bf.gqt.savedChars, Characters.no_chars + 1);
        for (int i = _origCharCount + 1; i <= Characters.no_chars; i++)
        {
            bf.gqt.savedChars[i] = MappedCharacters.CopyClass(bf.gqt.savedChars[1]);
        }
    }
    
    /*
     * Patch:
     * - Removes the dummy characters from the "default data" after the default data is loaded.
     */
    [HarmonyPatch(typeof(SaveData), nameof(SaveData.bxg))]
    [HarmonyPostfix]
    public static void SaveData_bxg_Post()
    {
        Array.Resize(ref bf.gqt.savedChars, _origCharCount + 1);
    }
}

