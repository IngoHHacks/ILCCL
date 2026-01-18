using System.Reflection;
using System.Text.RegularExpressions;
using ILCCL.Content;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace ILCCL.Patches;

[HarmonyPatch]
internal class ArenaPatch
{
    public static List<string> weaponList;
    public static float yOverride;
    public static bool freezeAnnouncers;
    public static int SignCount = 6;
    public static int CrowdCount = 12;
    //Below used for textures on custom arenas
    public static Texture[] signTextures = (Texture[])(object)new Texture[1];
    public static Texture[] crowdTextures = (Texture[])(object)new Texture[1];


    [HarmonyPatch(typeof(UnmappedBlocks), nameof(UnmappedBlocks.cbx))]
    [HarmonyPrefix]
    public static void Blocks_cbx()
    {
        if (World.location > VanillaCounts.Data.NoLocations)
        {
            World.arenaShape = 0;
        }
    }


    [HarmonyPostfix]
    [HarmonyPatch(typeof(World), nameof(World.cds))]
    public static void World_cds()
    {
        if (World.location > VanillaCounts.Data.NoLocations)
        {
            World.cfh();
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(World), nameof(World.cdi))]
    public static void World_cdi(ref string __result, ref int a, string b)
    {
        if (MappedMenus.paused != 0 && MappedMenus.page == 5 || a <= VanillaCounts.Data.NoLocations)
        {
            return;
        }
        
        string originalResult = __result;
        string text;

        GameObject arenaName = FindGameObjectWithNameStartingWith("Arena Name:");
        if (arenaName != null)
        {
            text = arenaName.name.Substring("Arena Name:".Length);
        }
        else
        {
            if (a > VanillaCounts.Data.NoLocations)
            {
                text = "Custom Arena";
            }
            else
            {
                text = CustomArenaPrefabs[a - VanillaCounts.Data.NoLocations - 1].name;
            }
        }

        __result = text;
    }
    
    [HarmonyPatch(typeof(World), nameof(World.cer))]
    [HarmonyPostfix]
    public static void World_cer(ref Vector3 __result, int a, int b)
    {
        if (World.location > VanillaCounts.Data.NoLocations)
        {
            if (World.gArena != null)
            {
                GameObject itemMarkerNorth = GameObject.Find("Itemborder (North)");
                GameObject itemMarkerEast = GameObject.Find("Itemborder (East)");
                GameObject itemMarkerSouth = GameObject.Find("Itemborder (South)");
                GameObject itemMarkerWest = GameObject.Find("Itemborder (West)");

                float furthestNorthDistance = float.MinValue;
                float furthestEastDistance = float.MinValue;
                float furthestSouthDistance = float.MaxValue;
                float furthestWestDistance = float.MaxValue;
                if (itemMarkerEast != null && itemMarkerNorth != null && itemMarkerSouth != null &&
                    itemMarkerWest != null)
                {
                    if (itemMarkerNorth != null)
                    {
                        float northDistance = Vector3.Distance(itemMarkerNorth.transform.position,
                            new Vector3(0.0f, -0.4f, 0.0f));
                        furthestNorthDistance = northDistance;
                    }

                    if (itemMarkerEast != null)
                    {
                        float eastDistance = Vector3.Distance(itemMarkerEast.transform.position,
                            new Vector3(0.0f, -0.4f, 0.0f));
                        furthestEastDistance = eastDistance;
                    }

                    if (itemMarkerSouth != null)
                    {
                        float southDistance = Vector3.Distance(itemMarkerSouth.transform.position,
                            new Vector3(0.0f, -0.4f, 0.0f));
                        furthestSouthDistance = southDistance;
                    }

                    if (itemMarkerWest != null)
                    {
                        float westDistance = Vector3.Distance(itemMarkerWest.transform.position,
                            new Vector3(0.0f, -0.4f, 0.0f));
                        furthestWestDistance = westDistance;
                    }

                    // The furthest distances from the center coordinates
                    float itemBorderNorth = furthestNorthDistance;
                    float itemBorderEast = furthestEastDistance;
                    float itemBorderSouth = furthestSouthDistance;
                    float itemBorderWest = furthestWestDistance;

                    float newX = Random.Range(-itemBorderWest, itemBorderEast);
                    float newZ = Random.Range(-itemBorderSouth, itemBorderNorth);
                    float newY = World.ground;

                    __result = new Vector3(newX, newY, newZ);
                }
            }
        }
    }
    
    [HarmonyPatch(typeof(World), nameof(World.ceo))]
    [HarmonyPostfix]
    public static void World_ceo(ref int a)
    {
        if (World.location > VanillaCounts.Data.NoLocations)
        {
            GameObject[] freezeObj = Object.FindObjectsOfType<GameObject>();
            GameObject[] announcerFreezeObj =
                freezeObj.Where(obj => obj.name.StartsWith("AnnouncerFreeze")).ToArray();
            if (announcerFreezeObj.Length > 0)
            {
                freezeAnnouncers = true;
            }
            else
            {
                freezeAnnouncers = false;
            }

            World.ground = 0f;

            GameObject[] objects = Object.FindObjectsOfType<GameObject>();

            float ceilingHeightFloat = 0;
            string ceilingHeight = "ceilingHeight";
            GameObject[] ceilingHeightObj = objects.Where(obj => obj.name.StartsWith(ceilingHeight)).ToArray();
            if (ceilingHeightObj.Length > 0)
            {
                string[] ceilingHeights =
                    ceilingHeightObj.Select(obj => obj.name.Substring(ceilingHeight.Length)).ToArray();

                foreach (string height in ceilingHeights)
                {
                    float parsedDistance;
                    if (float.TryParse(height, out parsedDistance))
                    {
                        ceilingHeightFloat = parsedDistance;
                    }
                    else
                    {
                        UnityEngine.Debug.LogError("Failed to parse ceilingHeight: " + height);
                    }
                }
            }

            if (ceilingHeightFloat > 0)
            {
                World.ceiling = ceilingHeightFloat;
            }
            else
            {
                World.ceiling = 100f;
            }

            
            float camDistanceFloat = new();

            string desiredName = "camDistance";
            GameObject[] camDistanceObj = objects.Where(obj => obj.name.StartsWith(desiredName)).ToArray();
            if (camDistanceObj.Length > 0)
            {
                string[] camDistance =
                    camDistanceObj.Select(obj => obj.name.Substring(desiredName.Length)).ToArray();

                foreach (string distance in camDistance)
                {
                    float parsedDistance;
                    if (float.TryParse(distance, out parsedDistance))
                    {
                        camDistanceFloat = parsedDistance;
                    }
                    else
                    {
                        UnityEngine.Debug.LogError("Failed to parse camDistance: " + distance);
                    }
                }
            }

            if (camDistanceFloat != 0)
            {
                World.camNorth = camDistanceFloat;
                World.camSouth = -camDistanceFloat;
                World.camEast = camDistanceFloat;
                World.camWest = -camDistanceFloat;
            }
            else
            {
                //Default to original arena values
                World.camNorth = 135f;
                World.camSouth = -135f;
                World.camEast = 135f;
                World.camWest = -135f;
            }

            if (World.gArena != null)
            {
                GameObject markerNorth = GameObject.Find("Marker (North)");
                GameObject markerEast = GameObject.Find("Marker (East)");
                GameObject markerSouth = GameObject.Find("Marker (South)");
                GameObject markerWest = GameObject.Find("Marker (West)");

                float furthestNorthDistance = float.MinValue;
                float furthestEastDistance = float.MinValue;
                float furthestSouthDistance = float.MaxValue;
                float furthestWestDistance = float.MaxValue;
                if (markerEast != null && markerNorth != null && markerSouth != null && markerWest != null)
                {
                    if (markerNorth != null)
                    {
                        float northDistance = Vector3.Distance(markerNorth.transform.position,
                            new Vector3(0.0f, -0.4f, 0.0f));
                        furthestNorthDistance = northDistance;
                    }

                    if (markerEast != null)
                    {
                        float eastDistance = Vector3.Distance(markerEast.transform.position,
                            new Vector3(0.0f, -0.4f, 0.0f));
                        furthestEastDistance = eastDistance;
                    }

                    if (markerSouth != null)
                    {
                        float southDistance = Vector3.Distance(markerSouth.transform.position,
                            new Vector3(0.0f, -0.4f, 0.0f));
                        furthestSouthDistance = southDistance;
                    }

                    if (markerWest != null)
                    {
                        float westDistance = Vector3.Distance(markerWest.transform.position,
                            new Vector3(0.0f, -0.4f, 0.0f));
                        furthestWestDistance = westDistance;
                    }

                    // The furthest distances from the center coordinates
                    float furthestNorth = furthestNorthDistance;
                    float furthestEast = furthestEastDistance;
                    float furthestSouth = furthestSouthDistance;
                    float furthestWest = furthestWestDistance;

                    World.farNorth = furthestNorth;
                    World.farSouth = -furthestEast;
                    World.farEast = furthestSouth;
                    World.farWest = -furthestWest;
                }
            }
        }
    }
    

    private static GameObject FindGameObjectWithNameStartingWith(string name)
    {
        GameObject[] gameObjects = Object.FindObjectsOfType<GameObject>();

        foreach (GameObject gameObject in gameObjects)
        {
            if (gameObject.name.StartsWith(name))
            {
                return gameObject;
            }
        }

        return null;
    }
    
    private static Texture2D LoadTextureFromPath(string path)
    {
        try
        {
            byte[] fileData = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(fileData); // Assuming PNG format
            return texture;
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"Error loading texture from path {path}: {ex.Message}");
            return null;
        }
    }

    private static string GetParentOfParentDirectory()
    {
        // Get the directory where the current executing assembly (your DLL) is located
        string assemblyLocation = Assembly.GetExecutingAssembly().Location;

        // Get the parent directory of the assembly location
        string parentDirectory = Path.GetDirectoryName(assemblyLocation);
        parentDirectory = Path.GetDirectoryName(parentDirectory);

        return parentDirectory;
    }
    
    public static int storedValue;
    
    [HarmonyPatch(typeof(UnmappedPlayer), nameof(UnmappedPlayer.bcr))]
    [HarmonyPrefix]
    public static void Player_bcr_Pre(UnmappedPlayer __instance)
    {
        if (freezeAnnouncers)
        {
            if (__instance.fwv == 0)
            {
                storedValue = __instance.fuw;
                __instance.fuw = 0;
            }
        }
    }


    [HarmonyPatch(typeof(UnmappedPlayer), nameof(UnmappedPlayer.bcr))]
    [HarmonyPostfix]
    public static void Player_bcr_Post(UnmappedPlayer __instance)
    {
        if (freezeAnnouncers)
        {
            if (__instance.fwv == 0 && storedValue != __instance.fuw)
            {
                __instance.fuw = storedValue;
            }
        }
    }
    

    public static bool furnitureAdded;
    public static List<string> furnitureList;
    
    [HarmonyPatch(typeof(UnmappedItems), nameof(UnmappedItems.btw))]
    [HarmonyPostfix]
    public static void Items_btw(ref int __result, int a, int b, int c)
    {
        int num = __result;
        furnitureAdded = false;
        if (b == 1)
        {
            //Code is making new list of arena items so set our list back to empty here
            furnitureList = new List<string>();
        }

        if (a > VanillaCounts.Data.NoLocations)
        {
            if (UnmappedItems.glt == null)
            {
                UnmappedItems.glt = new Stock[1];
            }

            UnmappedItems.glt[0] = new Stock();
            {
                //Maybe consider making this dynamic with map objects too for spawning stairs at any of the 4 (Or 6) corners
                if (World.ringShape == 1)
                {
                    if (b == 1)
                    {
                        furnitureAdded = true;
                        furnitureList.Add("Steps1");
                        yOverride = 0f;
                        UnmappedItems.glt[0].bun(4, a, -35f * World.ringSize,
                            35f * World.ringSize, 315f);
                    }

                    if (b == 2)
                    {
                        furnitureList.Add("Steps2");
                        furnitureAdded = true;
                        yOverride = 0f;
                        UnmappedItems.glt[0].bun(4, a, 35f * World.ringSize,
                            -35f * World.ringSize, 135f);
                    }
                }

                if (World.ringShape == 2)
                {
                    if (b == 1)
                    {
                        furnitureList.Add("Steps1");
                        furnitureAdded = true;
                        yOverride = 0f;
                        UnmappedItems.glt[0].bun(4, a, -21f * World.ringSize,
                            35f * World.ringSize, 330f);
                    }

                    if (b == 2)
                    {
                        furnitureList.Add("Steps2");
                        furnitureAdded = true;
                        yOverride = 0f;
                        UnmappedItems.glt[0].bun(4, a, 21f * World.ringSize,
                            -35f * World.ringSize, 150f);
                    }
                }
            }

            GameObject[] announcerObjects = Object.FindObjectsOfType<GameObject>()
                .Where(obj => obj.name.StartsWith("AnnouncerDeskBundle")).ToArray();

            foreach (GameObject announcerObject in announcerObjects)
            {
                ProcessAnnouncerDesk(announcerObject);
            }

            GameObject[] customGameObjects = Object.FindObjectsOfType<GameObject>()
                .Where(obj => obj.name.StartsWith("GameObject:")).ToArray();

            foreach (GameObject customGameObject in customGameObjects)
            {
                CustomGameObjectSpawner(customGameObject);
            }


            if (UnmappedItems.btq(UnmappedItems.glt[0].type) == 0 ||
                UnmappedItems.glt[0].type > UnmappedItems.glp)
            {
                UnmappedItems.glt[0].type = 0;
            }

            if (c != 0 && UnmappedItems.glt[0].type != 0)
            {
                if (c > 0)
                {
                    num = UnmappedItems.bty();
                    UnmappedItems.glt[num].bun(UnmappedItems.glt[0].type,
                        UnmappedItems.glt[0].location, UnmappedItems.glt[0].x,
                        UnmappedItems.glt[0].z, UnmappedItems.glt[0].angle);
                }
                else
                {
                    num = UnmappedItems.buc(UnmappedItems.glt[0].type);
                    if (UnmappedItems.glt[0].scale != 1f)
                    {
                        UnmappedItems.glu[num].gib = UnmappedItems.glt[0].scale;
                        UnmappedItems.glu[num].btm(UnmappedItems.glt[0].type);
                        UnmappedItems.glu[num].ghz.transform.localScale = new Vector3(
                            UnmappedItems.glu[num].gib, UnmappedItems.glu[num].gib,
                            UnmappedItems.glu[num].gib);
                    }

                    UnmappedItems.glu[num].brz(UnmappedItems.glt[0].x, World.ground,
                        UnmappedItems.glt[0].z, UnmappedItems.glt[0].angle);
                }
            }

            if (c == 0 && UnmappedItems.glt[0].type != 0)
            {
                num = b;
            }
        }

        __result = num;

        void CustomGameObjectSpawner(GameObject customObject)
        {
            string customObjectName = customObject.name.Substring("GameObject:".Length);
            //Remove numbers from end of the name
            customObjectName = Regex.Replace(customObjectName, @"\d+$", string.Empty);
            Vector3 newObjectPosition = customObject.transform.position;
            Quaternion newObjectRotation = customObject.transform.rotation;

            if (!furnitureList.Contains(customObject.name) && !furnitureAdded)
            {
                //Always add to list even if a valid customObjectId isn't returned to save going in everytime
                furnitureList.Add(customObject.name);
                int customObjectId = GetMapping(customObjectName);
                if (customObjectId > 0)
                {
                    furnitureAdded = true;
                    yOverride = newObjectPosition.y;
                    UnmappedItems.glt[0].bun(customObjectId, a, newObjectPosition.x,
                        newObjectPosition.z, newObjectRotation.eulerAngles.y);
                }
            }
        }

        void ProcessAnnouncerDesk(GameObject deskObject)
        {
            // Use Original table and chair positions to make custom location of them stay together
            Vector3 originalTablePosition = new(44f, 0f, 43f);
            Quaternion originalTableRotation = Quaternion.Euler(0f, 180f, 0f);

            Vector3 originalChair1Position = new(39.5f, 0f, 50.5f);
            Vector3 originalChair2Position = new(48.5f, 0f, 50.5f);

            Vector3 originalChair1RelativePosition = Quaternion.Euler(0f, originalTableRotation.eulerAngles.y, 0f) *
                                                     (originalChair1Position - originalTablePosition);
            Vector3 originalChair2RelativePosition = Quaternion.Euler(0f, originalTableRotation.eulerAngles.y, 0f) *
                                                     (originalChair2Position - originalTablePosition);

            // Retrieve the position (x, y, z) and rotation of the object
            Vector3 newDeskPosition = deskObject.transform.position;
            Quaternion newDeskRotation = deskObject.transform.rotation;

            Quaternion relativeRotation = Quaternion.Inverse(originalTableRotation) * newDeskRotation;

            // Adjust the chair positions based on the relative rotation of the desk object
            Vector3 updatedChair1Position =
                newDeskPosition + (relativeRotation * (originalChair1Position - originalTablePosition));
            Vector3 updatedChair2Position =
                newDeskPosition + (relativeRotation * (originalChair2Position - originalTablePosition));

            // Add the furniture to the list and perform other actions
            if (!furnitureList.Contains(deskObject.name) && !furnitureAdded)
            {
                furnitureList.Add(deskObject.name);
                furnitureAdded = true;
                yOverride = newDeskPosition.y;
                UnmappedItems.glt[0].bun(3, a, newDeskPosition.x, newDeskPosition.z,
                    newDeskRotation.eulerAngles.y);
            }

            if (!furnitureList.Contains(deskObject.name + "ChairA") && !furnitureAdded)
            {
                furnitureList.Add(deskObject.name + "ChairA");
                furnitureAdded = true;
                yOverride = newDeskPosition.y;
                UnmappedItems.glt[0].bun(2, a, updatedChair1Position.x,
                    updatedChair1Position.z, newDeskRotation.eulerAngles.y);
            }

            if (!furnitureList.Contains(deskObject.name + "ChairB") && !furnitureAdded)
            {
                furnitureList.Add(deskObject.name + "ChairB");
                furnitureAdded = true;
                yOverride = newDeskPosition.y;
                UnmappedItems.glt[0].bun(2, a, updatedChair2Position.x,
                    updatedChair2Position.z, newDeskRotation.eulerAngles.y);
            }
        }

        int GetMapping(string input)
        {
            for (int i = 1; i <= UnmappedItems.glp; i++)
            {
                if (UnmappedItems.btq(i) > 0)
                {
                    if (UnmappedItems.bts(i) == input)
                    {
                        return i;
                    }
                }
            }

            return 0;
        }
    }

    [HarmonyPatch(typeof(UnmappedItem), nameof(UnmappedItem.brz))]
    [HarmonyPostfix]
    public static void Item_brz(UnmappedItem __instance)
    {
        if (yOverride != 0f)
        {
            //This overrides the height for placement of furniture so it can be above ground level.
            UnmappedItems.glt[__instance.ghy].y = yOverride;
            __instance.gii = yOverride;
            __instance.gig = yOverride;
            __instance.gie = yOverride;

            //Set yOverride back to 0 afterwards so going to another map doesn't spawn all furniture in the air...
            yOverride = 0f;
        }
    }

    private static bool _z3Reduced = false;


    [HarmonyPatch(typeof(UnmappedDoor), nameof(UnmappedDoor.ccu))]
    [HarmonyPrefix]
    public static void Door_ccu(Door __instance)
    {
        if (World.location > VanillaCounts.Data.NoLocations)
        {
            //Force set these to 20 / -20 to match original arena so it can trigger (Think its related to size of the door in original as custom arenas seem to have these a values of around 1.5 instead).
            __instance.hfh[1] = 20;
            __instance.hfh[2] = 20;
            __instance.hfh[3] = -20;
            __instance.hfh[4] = -20;
            if (!_z3Reduced)
            {
                //Also once only per map load, set this value to 5 less as otherwise the wrestlers needed to stand on a near exact spot to exit which the AI would almost never do.
                __instance.hfi[3] -= 5;
                _z3Reduced = true;
            }
        }
    }

    internal static Vector3? newWeaponPosition;
    internal static Vector3? newWeaponRotation;
    internal static int customWeaponId;
    internal static string customWeaponName;

    public static string CustomWeaponName
    {
        get => customWeaponName;
        set => customWeaponName = value;
    }
    
    [HarmonyPatch(typeof(UnmappedWeapons), nameof(UnmappedWeapons.bwo))]
    [HarmonyPrefix]
    public static void Weapons_bwo_Pre()
    {
        _z3Reduced = false;
        //Reset these to null so loading custom map second time onwards doesn't force all outside ring weapons to a weapon spawn point
        newWeaponPosition = null;
        newWeaponRotation = null;
    }

    [HarmonyPatch(typeof(UnmappedWeapons), nameof(UnmappedWeapons.bwo))]
    [HarmonyPostfix]
    public static void Weapons_bwo_Post()
    {
        newWeaponPosition = null;
        newWeaponRotation = null;
        weaponList = new List<string>();
        System.Random random = new();

        //Loops through here to add weapons, CHMHJJNEMKB = weapon ID
        GameObject[] customWeaponObjects = Object.FindObjectsOfType<GameObject>()
            .Where(obj => obj.name.StartsWith("WeaponObject:")).ToArray();
        foreach (GameObject customWeaponObject in customWeaponObjects)
        {
            string customWeaponName = customWeaponObject.name.Substring("WeaponObject:".Length);
            //Remove numbers from end of the name
            customWeaponName = Regex.Replace(customWeaponName, @"\d+$", string.Empty);
            CustomWeaponName = customWeaponName;
            newWeaponPosition = customWeaponObject.transform.position;
            newWeaponRotation = customWeaponObject.transform.eulerAngles;

            if (!weaponList.Contains(customWeaponObject.name))
            {
                if (customWeaponName == "Random")
                {
                    customWeaponId = random.Next(1, 68 + 1);
                }
                else
                {
                    customWeaponId = GetWeaponMapping(customWeaponName);
                }

                weaponList.Add(customWeaponObject.name);
                if (customWeaponId != 0)
                {
                    Weapons.bwp(customWeaponId);
                }
            }
        }
    }

    private static int GetWeaponMapping(string input)
    {
        for (int i = 1; i <= Weapons.gpv; i++)
        {
            if (Weapons.bwc(i) > 0)
            {
                if (Weapons.bwe(i) == input)
                {
                    return i;
                }
            }
        }

        return 0;
    }
    
    [HarmonyPatch(typeof(UnmappedWeapon), nameof(UnmappedWeapon.buw))]
    [HarmonyPostfix]
    public static void Weapon_buw(Weapon __instance, int a, int b, int c)
    {
        if (newWeaponPosition != null && newWeaponRotation != null)
        {
            __instance.gmh = newWeaponPosition.Value.x;
            __instance.gmk = newWeaponPosition.Value.y;
            __instance.gmj = newWeaponPosition.Value.z;
            __instance.gmo = newWeaponRotation.Value.y;
            float rotationX = newWeaponRotation.Value.x;
            float rotationZ = newWeaponRotation.Value.z;
            string weaponName = CustomWeaponName;
            if (weaponName == "Random")
            {
                rotationX = 0f;
                __instance.gmo = UnmappedGlobals.od(0, 359);
                rotationZ = 0f;
            }

            //Need to update these for weapons to allow pickup
            __instance.gmi = __instance.gmk;
            __instance.gml = __instance.gmh;
            __instance.gmm = __instance.gmi;
            __instance.gmn = __instance.gmk;
            __instance.gmp = __instance.gmo;

            __instance.gmc.transform.position = new Vector3(__instance.gmh, __instance.gmi,
                __instance.gmk);
            __instance.gmc.transform.eulerAngles =
                new Vector3(rotationX, __instance.gmo, rotationZ);
        }
    }
    
    [HarmonyPatch(typeof(UnmappedCam), nameof(UnmappedCam.ccf))]
    [HarmonyPostfix]
    public static void cds_Patch()
    {
        GameObject titanCamera = GameObject.Find("TitantronCamera");
        if (titanCamera)
        {
            titanCamera.AddComponent<CameraTracking>();
        }
    }

    public class CameraTracking : MonoBehaviour
    {
        public GameObject CameraFocalPoint;

        private void Start()
        {
            this.CameraFocalPoint = GameObject.Find("Camera Focal Point");
        }

        private void Update()
        {
            if (this.CameraFocalPoint)
            {
                this.transform.LookAt(this.CameraFocalPoint.transform);
            }
        }
    }

    //Below is for applying Shaders from custom Arena to things that need it in WE
    
    //Fix cage in Custom Arena shaders
    [HarmonyPatch(typeof(World), nameof(World.cfy))]
    [HarmonyPostfix]
    public static void World_cfy(int a = 0)
    {
        if (World.location > VanillaCounts.Data.NoLocations && UnmappedMatch.fcr != 0 && World.gCage != null && World.arenaCage == 3)
        {
            Transform ReferenceSolidObject = World.gArena.transform.Find("SolidShader");
            Transform ReferenceTransparentObject = World.gArena.transform.Find("TransparentShader");

            if (ReferenceSolidObject != null && ReferenceTransparentObject != null)
            {
                Material customSolidMaterial = ReferenceSolidObject.gameObject.GetComponent<Renderer>().sharedMaterial;
                Material customTransparentMaterial = ReferenceTransparentObject.gameObject.GetComponent<Renderer>().sharedMaterial;

                GameObject[] targetGameObjects = World.gCage;

                foreach (GameObject targetGameObject in targetGameObjects)
                {
                    foreach (Renderer renderer in targetGameObject.GetComponentsInChildren<Renderer>())
                    {
                        Texture[] existingTextures = new Texture[renderer.materials.Length];
                        string[] existingShaderName = new string[renderer.materials.Length];
                        Vector2[] existingTiling = new Vector2[renderer.materials.Length];
                        Vector2[] existingOffset = new Vector2[renderer.materials.Length];

                        for (int i = 0; i < renderer.materials.Length; i++)
                        {
                            existingTextures[i] = renderer.materials[i].mainTexture;
                            existingShaderName[i] = renderer.materials[i].shader.name;
                            existingTiling[i] = renderer.materials[i].mainTextureScale;
                            existingOffset[i] = renderer.materials[i].mainTextureOffset;
                        }

                        // Change the material for each Renderer component found
                        Material[] customMaterials = new Material[renderer.materials.Length];
                        for (int i = 0; i < customMaterials.Length; i++)
                        {
                            if (existingShaderName[i] == "Custom/My Simple Solid")
                            {
                                customMaterials[i] = customSolidMaterial;
                            }
                            //If it ain't solid, assume its cutout
                            if (existingShaderName[i] != "Custom/My Simple Solid")
                            {
                                customMaterials[i] = customTransparentMaterial;
                            }

                            customMaterials[i].mainTextureScale = existingTiling[i];
                            customMaterials[i].mainTextureOffset = existingOffset[i];
                        }
                        renderer.materials = customMaterials;

                        // Restore the existing textures for each material
                        for (int i = 0; i < renderer.materials.Length; i++)
                        {
                            renderer.materials[i].mainTexture = existingTextures[i];
                        }
                    }
                }
            }
        }
    }

    //Fix turnbuckles etc. in Custom Arena shaders
    [HarmonyPatch(typeof(World), nameof(World.cfa))]
    [HarmonyPostfix]
    public static void cfa_ShaderPatch(int a = 0)
    {
        if (World.location > VanillaCounts.Data.NoLocations && UnmappedMatch.fcr != 0 && World.no_ropes > 0)
        {
            Transform ReferenceSolidObject = World.gArena.transform.Find("SolidShader");
            Transform ReferenceTransparentObject = World.gArena.transform.Find("TransparentShader");

            if (ReferenceSolidObject != null && ReferenceTransparentObject != null)
            {
                Material customSolidMaterial = ReferenceSolidObject.gameObject.GetComponent<Renderer>().sharedMaterial;
                Material customTransparentMaterial = ReferenceTransparentObject.gameObject.GetComponent<Renderer>().sharedMaterial;

                GameObject targetGameObject1 = World.gPosts;
                GameObject targetGameObject2 = World.gSupports;
                GameObject targetGameObject3 = World.gPads;

                for (int n = 1; n <= 3; n++)
                {
                    GameObject targetGameObject = null;
                    switch (n)
                    {
                        case 1:
                            targetGameObject = targetGameObject1;
                            break;
                        case 2:
                            targetGameObject = targetGameObject2;
                            break;
                        case 3:
                            targetGameObject = targetGameObject3;
                            break;
                    }

                    foreach (Renderer renderer in targetGameObject.GetComponentsInChildren<Renderer>())
                    {
                        Texture[] existingTextures = new Texture[renderer.materials.Length];
                        Color[] existingColor = new Color[renderer.materials.Length];
                        string[] existingShaderName = new string[renderer.materials.Length];
                        Material[] existingRenderMaterials = renderer.materials;

                        for (int i = 0; i < renderer.materials.Length; i++)
                        {
                            existingTextures[i] = renderer.materials[i].mainTexture;
                            if (renderer.materials[i].HasProperty("_Color"))
                            {
                                existingColor[i] = renderer.materials[i].color;
                            }
                            existingShaderName[i] = renderer.materials[i].shader.name;
                        }

                        // Change the material for each Renderer component found
                        Material[] customMaterials = new Material[renderer.materials.Length];
                        for (int i = 0; i < customMaterials.Length; i++)
                        {
                            if (existingShaderName[i] == "Custom/My Simple Solid")
                            {
                                customMaterials[i] = customSolidMaterial;
                            }
                            //If it ain't solid, assume its cutout
                            if (existingShaderName[i] != "Custom/My Simple Solid")
                            {
                                customMaterials[i] = customTransparentMaterial;
                            }
                        }
                        renderer.materials = customMaterials;

                        // Restore the existing textures for each material
                        for (int i = 0; i < renderer.materials.Length; i++)
                        {
                            renderer.materials[i].mainTexture = existingTextures[i];
                            if (existingRenderMaterials[i].HasProperty("_Color"))
                            {
                                renderer.materials[i].color = existingColor[i];
                            }
                        }
                    }
                }
            }
        }
    }

    //Fix ropes in Custom Arena shaders
    [HarmonyPatch(typeof(World), nameof(World.cfn))]
    [HarmonyPostfix]
    public static void cfn_ShaderPatch(int a, string b, Material c)
    {
        if (World.location > VanillaCounts.Data.NoLocations && UnmappedMatch.fcr != 0)
        {
            Transform ReferenceSolidObject = World.gArena.transform.Find("SolidShader");
            Transform ReferenceTransparentObject = World.gArena.transform.Find("TransparentShader");

            if (ReferenceSolidObject != null && ReferenceTransparentObject != null)
            {
                Material customSolidMaterial = ReferenceSolidObject.gameObject.GetComponent<Renderer>().sharedMaterial;
                Material customTransparentMaterial = ReferenceTransparentObject.gameObject.GetComponent<Renderer>().sharedMaterial;

                GameObject targetGameObject1 = World.gRope[a].transform.Find(b + "01").gameObject;
                GameObject targetGameObject2 = World.gRope[a].transform.Find(b + "01/" + b + "02").gameObject;
                GameObject targetGameObject3 = World.gRope[a].transform.Find(b + "01/" + b + "02/" + b + "03").gameObject;
                GameObject targetGameObject4 = World.gRope[a].transform.Find(b + "01/" + b + "02/" + b + "03/" + b + "04").gameObject;

                for (int n = 1; n <= 4; n++)
                {
                    GameObject targetGameObject = null;
                    switch (n)
                    {
                        case 1:
                            targetGameObject = targetGameObject1;
                            break;
                        case 2:
                            targetGameObject = targetGameObject2;
                            break;
                        case 3:
                            targetGameObject = targetGameObject3;
                            break;
                        case 4:
                            targetGameObject = targetGameObject4;
                            break;
                    }

                    foreach (Renderer renderer in targetGameObject.GetComponentsInChildren<Renderer>())
                    {
                        Texture[] existingTextures = new Texture[renderer.materials.Length];
                        Color[] existingColor = new Color[renderer.materials.Length];
                        string[] existingShaderName = new string[renderer.materials.Length];
                        Material[] existingRenderMaterials = renderer.materials;

                        for (int i = 0; i < renderer.materials.Length; i++)
                        {
                            existingTextures[i] = renderer.materials[i].mainTexture;
                            if (renderer.materials[i].HasProperty("_Color"))
                            {
                                existingColor[i] = renderer.materials[i].color;
                            }
                            existingShaderName[i] = renderer.materials[i].shader.name;
                        }

                        // Change the material for each Renderer component found
                        Material[] customMaterials = new Material[renderer.materials.Length];
                        for (int i = 0; i < customMaterials.Length; i++)
                        {
                            if (existingShaderName[i] == "Custom/My Simple Solid")
                            {
                                customMaterials[i] = customSolidMaterial;
                            }
                            //If it ain't solid, assume its cutout
                            if (existingShaderName[i] != "Custom/My Simple Solid")
                            {
                                customMaterials[i] = customTransparentMaterial;
                            }
                        }
                        renderer.materials = customMaterials;

                        // Restore the existing textures for each material
                        for (int i = 0; i < renderer.materials.Length; i++)
                        {
                            renderer.materials[i].mainTexture = existingTextures[i];
                            if (existingRenderMaterials[i].HasProperty("_Color"))
                            {
                                renderer.materials[i].color = existingColor[i];
                            }
                        }
                    }
                }
            }
        }
    }
    
    //Fix wrestler headgear with custom arena shaders
    [HarmonyPatch(typeof(UnmappedPlayer), nameof(UnmappedPlayer.zn))]
    [HarmonyPostfix]
    public static void Player_zn(Player __instance, int a, int b)
    {
        if (World.location > VanillaCounts.Data.NoLocations && UnmappedMatch.fcr != 0)
        {
            if ((a == 28 || a == 31) && __instance.fth.shape[a] > 0)
            {
                Transform ReferenceObject = null;
                if (World.gArena != null)
                {
                    ReferenceObject = World.gArena.transform.Find("SolidShader");
                }


                if (ReferenceObject != null)
                {
                    Material customArenaMaterial = ReferenceObject.gameObject.GetComponent<Renderer>().sharedMaterial;
                    GameObject targetGameObject = __instance.fti[a];
                    //This doesn't work, needs more investigating
                    foreach (Renderer renderer in targetGameObject.GetComponentsInChildren<Renderer>())
                    {
                        Texture[] existingTextures = new Texture[renderer.materials.Length];
                        Color[] existingColor = new Color[renderer.materials.Length];
                        for (int i = 0; i < renderer.materials.Length; i++)
                        {
                            existingTextures[i] = renderer.materials[i].mainTexture;
                            existingColor[i] = renderer.materials[i].color;
                        }

                        // Change the material for each Renderer component found
                        Material[] customMaterials = new Material[renderer.materials.Length];
                        for (int i = 0; i < customMaterials.Length; i++)
                        {
                            customMaterials[i] = customArenaMaterial;
                        }
                        renderer.materials = customMaterials;

                        // Restore the existing textures for each material
                        for (int i = 0; i < renderer.materials.Length; i++)
                        {
                            renderer.materials[i].mainTexture = existingTextures[i];
                            renderer.materials[i].color = existingColor[i];
                        }
                    }
                }
            }
        }
    }
    
    //Fix Furniture with custom arena shaders
    [HarmonyPatch(typeof(UnmappedItem), nameof(UnmappedItem.bry))]
    [HarmonyPostfix]
    public static void Item_bry(UnmappedItem __instance)
    {
        if (World.location > VanillaCounts.Data.NoLocations && UnmappedMatch.fcr != 0)
        {
            Transform ReferenceObject = World.gArena.transform.Find("SolidShader");

            if (ReferenceObject != null)
            {
                Material customArenaMaterial = ReferenceObject.gameObject.GetComponent<Renderer>().sharedMaterial;

                GameObject targetGameObject = __instance.ghz;

                foreach (Renderer renderer in targetGameObject.GetComponentsInChildren<Renderer>())
                {
                    //This works for getting all parts of model like ladder and fixing it, need to do same to hats
                    Texture[] existingTextures = new Texture[renderer.materials.Length];
                    for (int i = 0; i < renderer.materials.Length; i++)
                    {
                        existingTextures[i] = renderer.materials[i].mainTexture;
                    }

                    // Change the material for each Renderer component found
                    Material[] customMaterials = new Material[renderer.materials.Length];
                    for (int i = 0; i < customMaterials.Length; i++)
                    {
                        customMaterials[i] = customArenaMaterial;
                    }
                    renderer.materials = customMaterials;

                    // Restore the existing textures for each material
                    for (int i = 0; i < renderer.materials.Length; i++)
                    {
                        renderer.materials[i].mainTexture = existingTextures[i];
                    }
                }
            }
        }
    }
    
    //Fix weapons with custom arena shaders
    [HarmonyPatch(typeof(UnmappedWeapon), nameof(UnmappedWeapon.buw))]
    [HarmonyPostfix]
    public static void Weapon_buw(Weapon __instance)
    {
        if (World.location > VanillaCounts.Data.NoLocations && UnmappedMatch.fcr != 0)
        {
            //Exclude Belts and Glass weapons
            if (__instance.gom > 0 && __instance.gom != 23 && __instance.gom != 24 && __instance.gom != 25 && __instance.gom != 26 && __instance.gom != 37)
            {
                Transform ReferenceSolidObject = World.gArena.transform.Find("SolidShader");
                Transform ReferenceTransparentObject = World.gArena.transform.Find("TransparentShader");

                if (ReferenceSolidObject != null && ReferenceTransparentObject != null)
                {
                    GameObject targetGameObject = __instance.gmd;

                    foreach (var rendererComponent in targetGameObject.GetComponentsInChildren<Renderer>())
                    {
                        Material customSolidMaterial = ReferenceSolidObject.gameObject.GetComponent<Renderer>().sharedMaterial;
                        Material customTransparentMaterial = ReferenceTransparentObject.gameObject.GetComponent<Renderer>().sharedMaterial;

                        Texture[] existingTextures = new Texture[rendererComponent.materials.Length];
                        int[] existingRenderQueue = new int[rendererComponent.materials.Length];
                        string[] existingShaderName = new string[rendererComponent.materials.Length];
                        Material[] existingMaterials = new Material[rendererComponent.materials.Length];
                        Material[] existingRenderMaterials = rendererComponent.materials;
                        Vector2[] existingTiling = new Vector2[rendererComponent.materials.Length];
                        Vector2[] existingOffset = new Vector2[rendererComponent.materials.Length];
                        Color[] existingColor = new Color[rendererComponent.materials.Length];

                        for (int i = 0; i < rendererComponent.materials.Length; i++)
                        {
                            existingTextures[i] = rendererComponent.materials[i].mainTexture;
                            existingRenderQueue[i] = rendererComponent.materials[i].renderQueue;
                            existingShaderName[i] = rendererComponent.materials[i].shader.name;
                            existingMaterials[i] = rendererComponent.materials[i];
                            if (rendererComponent.materials[i].HasProperty("_Color"))
                            {
                                existingColor[i] = rendererComponent.materials[i].color;
                            }
                            existingTiling[i] = rendererComponent.materials[i].mainTextureScale;
                            existingOffset[i] = rendererComponent.materials[i].mainTextureOffset;
                        }

                        // Change the material for each Renderer component found
                        Material[] customMaterials = new Material[rendererComponent.materials.Length];
                        for (int i = 0; i < customMaterials.Length; i++)
                        {
                            if (existingShaderName[i] == "Custom/My Simple Solid")
                            {
                                customMaterials[i] = customSolidMaterial;
                            }
                            //If it ain't solid, assume its cutout as we ignore glass weapons
                            if (existingShaderName[i] != "Custom/My Simple Solid")
                            {
                                customMaterials[i] = customTransparentMaterial;
                            }

                            customMaterials[i].mainTextureScale = existingTiling[i];
                            customMaterials[i].mainTextureOffset = existingOffset[i];
                        }
                        rendererComponent.materials = customMaterials;

                        // Restore the existing textures and RenderQueue for each material
                        for (int i = 0; i < rendererComponent.materials.Length; i++)
                        {
                            rendererComponent.materials[i].mainTexture = existingTextures[i];
                            rendererComponent.materials[i].renderQueue = existingRenderQueue[i];
                            if (existingRenderMaterials[i].HasProperty("_Color"))
                            {
                                rendererComponent.materials[i].color = existingColor[i];
                            }
                        }
                    }
                }
            }
        }
    }
}