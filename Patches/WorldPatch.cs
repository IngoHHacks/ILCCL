using ILCCL.Content;
using Object = UnityEngine.Object;

namespace ILCCL.Patches;

[HarmonyPatch]
public class WorldPatch
{
    private static int _tempLocation = -999;
    private static readonly LimitedDictionary<string, float> RaycastCache = new(1000);
    
    /*
     * Patch:
     * - Loads custom arenas if the player is in a custom location.
     */
    [HarmonyPatch(typeof(World), nameof(World.cds))]
    [HarmonyPrefix]
    public static bool World_cds(int a)
    {
        try
        {
            if (World.location > VanillaCounts.Data.NoLocations)
            {
                Debug.Log("Loading location " + World.location);
                World.waterDefault = 0f;
                World.no_baskets = 0;
                if (a != 0 && World.gArena != null)
                {
                    Object.Destroy(World.gArena);
                }

                MappedWorld.GetArenaShape();
                World.gArena = Object.Instantiate(CustomArenaPrefabs[World.location - VanillaCounts.Data.NoLocations - 1]);

                if (MappedMenus.screen == 60)
                {
                    World.gArena.transform.eulerAngles = new Vector3(0f, 170f, 0f);
                }

                if (Mathf.Abs(World.waterOffset) <= 1f)
                {
                    World.waterOffset = 0f;
                }

                if (UnmappedGlobals.eyx == 1)
                {
                    World.waterOffset = World.floodLevel;
                }
                else
                {
                    World.floodLevel = World.waterOffset;
                }

                World.waterLevel = World.waterDefault + World.waterOffset;
                MappedWorld.LoadWater();
                if (UnmappedMenus.fhy == 60)
                {
                    return false;
                }

                World.ceo(World.location);
                if (UnmappedMenus.fhy != 14)
                {
                    UnmappedSound.byt();
                }

                if (a == 0)
                {
                    if (World.ringShape > 0)
                    {
                        World.cfa();
                    }

                    World.ceh();
                }

                UnmappedBlocks.cbx();
                return false;
            }
        }
        catch (Exception e)
        {
            LogError("Error loading location " + World.location + ": " + e);
        }

        return true;
    }

    /*
     * Patch:
     * - Clears the 'blocks' (collision boxes) when loading a custom arena.
     */
    [HarmonyPatch(typeof(UnmappedBlocks), nameof(UnmappedBlocks.cbx))]
    [HarmonyPrefix]
    public static void Blocks_cbx_Pre()
    {
        if (World.location > VanillaCounts.Data.NoLocations)
        {
            UnmappedBlocks.hcf = 0;
            UnmappedBlocks.hcg = 0;
            UnmappedBlocks.hcn = 0;
            UnmappedBlocks.hch = new Block[UnmappedBlocks.hcg + 1];
            UnmappedBlocks.hch[0] = new Block();
            UnmappedBlocks.hci = 0;
            UnmappedBlocks.hco = 0;
            UnmappedBlocks.hcp = new Door[UnmappedBlocks.hco + 1];
            UnmappedBlocks.hcp[0] = new Door();
            UnmappedBlocks.hcz = 0;
            UnmappedBlocks.hda = new cd[UnmappedBlocks.hcz + 1];
            UnmappedBlocks.hda[0] = new cd();
            _tempLocation = World.location;
            World.location = 999;
            //Sets arenaShape to 0 here to stop spawning of default collisions while setting it to other shapes based on object in custom arena
            World.arenaShape = 0;
        }
    }

    /*
     * Patch:
     * - Creates the 'blocks' (collision boxes) when loading a custom arena.
     * - Renders the debug collision boxes if the user has enabled them in the config.
     */
    [HarmonyPatch(typeof(UnmappedBlocks), nameof(UnmappedBlocks.cbx))]
    [HarmonyPostfix]
    public static void Blocks_cbx_Post()
    {
        if (_tempLocation != -999)
        {
            World.location = _tempLocation;
            _tempLocation = -999;

            int last = UnmappedBlocks.hcg;
            UnmappedBlocks.hcf = last;

            MeshCollider[] colliders = World.gArena.GetComponentsInChildren<MeshCollider>();
            foreach (MeshCollider collider in colliders)
            {
                if (Math.Abs(collider.transform.rotation.eulerAngles.x - 90) > 1)
                {
                    continue;
                }

                Matrix4x4 matrix = collider.transform.localToWorldMatrix;
                Vector3[] corners = new Vector3[4];
                Vector3 up = collider.transform.up * 5;
                corners[0] = matrix.MultiplyPoint3x4(new Vector3(5, 0, 5));
                corners[1] = matrix.MultiplyPoint3x4(new Vector3(5, 0, -5));
                corners[2] = matrix.MultiplyPoint3x4(new Vector3(-5, 0, 5));
                corners[3] = matrix.MultiplyPoint3x4(new Vector3(-5, 0, -5));

                Array.Sort(corners, (a, b) =>
                {
                    return a.y.CompareTo(b.y);
                });
                float yBottom = corners[0].y;
                float yTop = corners[3].y;

                corners[0] -= up;
                corners[1] -= up;
                corners[2] = corners[1] + (up * 2);
                corners[3] = corners[0] + (up * 2);

                Vector3[] xSorted = new Vector3[4];
                Vector3[] zSorted = new Vector3[4];
                Array.Copy(corners, xSorted, 4);
                Array.Copy(corners, zSorted, 4);
                Array.Sort(xSorted, (a, b) =>
                {
                    return a.x.CompareTo(b.x);
                });
                Array.Sort(zSorted, (a, b) =>
                {
                    return a.z.CompareTo(b.z);
                });

                Vector3 topRight = corners[0];
                Vector3 bottomRight = corners[0];
                Vector3 bottomLeft = corners[0];
                Vector3 topLeft = corners[0];

                if (zSorted[3].x > zSorted[2].x)
                {
                    topRight = zSorted[3];
                    bottomRight = zSorted[2];
                }
                else
                {
                    topRight = zSorted[2];
                    bottomRight = zSorted[3];
                }

                if (zSorted[1].x < zSorted[0].x)
                {
                    bottomLeft = zSorted[1];
                    topLeft = zSorted[0];
                }
                else
                {
                    bottomLeft = zSorted[0];
                    topLeft = zSorted[1];
                }

                // Create block
                UnmappedBlocks.hcf++;
                int hcf = UnmappedBlocks.hcf;
                UnmappedBlocks.cbh();
                UnmappedBlocks.hch[hcf].hbl = 0f;
                UnmappedBlocks.hch[hcf].hbi = yBottom;
                UnmappedBlocks.hch[hcf].hbj = yTop - yBottom;
                UnmappedBlocks.hch[hcf].hbp = 0;
                UnmappedBlocks.hch[hcf].hbg[1] = topRight.x;
                UnmappedBlocks.hch[hcf].hbh[1] = topRight.z;
                UnmappedBlocks.hch[hcf].hbg[4] = bottomRight.x;
                UnmappedBlocks.hch[hcf].hbh[4] = bottomRight.z;
                UnmappedBlocks.hch[hcf].hbg[3] = bottomLeft.x;
                UnmappedBlocks.hch[hcf].hbh[3] = bottomLeft.z;
                UnmappedBlocks.hch[hcf].hbg[2] = topLeft.x;
                UnmappedBlocks.hch[hcf].hbh[2] = topLeft.z;
            }

            foreach (GameObject gameObject in (from t in World.gArena.GetComponentsInChildren<Transform>()
                         where t.gameObject != null && t.gameObject.name.StartsWith("Barrier_Climbables")
                         select t.gameObject).ToArray())
            {
                MeshCollider meshCollider = gameObject.GetComponent<MeshCollider>();

                if (meshCollider != null)
                {
                    Bounds bounds = meshCollider.sharedMesh.bounds;

                    Vector3 center = bounds.center;
                    Vector3 extents = bounds.extents;

                    // Calculate the 8 corners of the bounding box
                    Vector3[] corners = new Vector3[8];
                    corners[0] = center + new Vector3(-extents.x, -extents.y, -extents.z);
                    corners[1] = center + new Vector3(-extents.x, -extents.y, extents.z);
                    corners[2] = center + new Vector3(extents.x, -extents.y, extents.z);
                    corners[3] = center + new Vector3(extents.x, -extents.y, -extents.z);
                    corners[4] = center + new Vector3(-extents.x, extents.y, -extents.z);
                    corners[5] = center + new Vector3(-extents.x, extents.y, extents.z);
                    corners[6] = center + new Vector3(extents.x, extents.y, extents.z);
                    corners[7] = center + new Vector3(extents.x, extents.y, -extents.z);

                    // Get the 8 corners of the bounding box as world position
                    Vector3[] worldCorners = new Vector3[corners.Length];
                    for (int i = 0; i < corners.Length; i++)
                    {
                        worldCorners[i] = meshCollider.transform.TransformPoint(corners[i]);
                    }

                    UnmappedBlocks.hcf++;
                    int peifijckaoc = UnmappedBlocks.hcf;
                    UnmappedBlocks.cbh();
                    UnmappedBlocks.hch[peifijckaoc].hbl = 0f;
                    UnmappedBlocks.hch[peifijckaoc].hbi = worldCorners[0].y;
                    UnmappedBlocks.hch[peifijckaoc].hbj = worldCorners[1].y - worldCorners[0].y;
                    UnmappedBlocks.hch[peifijckaoc].hbp = 1;
                    UnmappedBlocks.hch[peifijckaoc].hbf = "Barrier";
                    GameObject arenaObject = GetTopLevelParent(gameObject);
                    if (arenaObject.transform.rotation == Quaternion.Euler(0f, 180f, 0f))
                    {
                        UnmappedBlocks.hch[peifijckaoc].hbg[1] = worldCorners[4].x + 2.5f;
                        UnmappedBlocks.hch[peifijckaoc].hbh[1] = worldCorners[4].z + 2.5f;
                        UnmappedBlocks.hch[peifijckaoc].hbg[4] = worldCorners[7].x - 2.5f;
                        UnmappedBlocks.hch[peifijckaoc].hbh[4] = worldCorners[7].z + 2.5f;
                        UnmappedBlocks.hch[peifijckaoc].hbg[3] = worldCorners[3].x - 2.5f;
                        UnmappedBlocks.hch[peifijckaoc].hbh[3] = worldCorners[3].z - 2.5f;
                        UnmappedBlocks.hch[peifijckaoc].hbg[2] = worldCorners[0].x + 2.5f;
                        UnmappedBlocks.hch[peifijckaoc].hbh[2] = worldCorners[0].z - 2.5f;
                    }
                    else
                    {
                        UnmappedBlocks.hch[peifijckaoc].hbg[1] = worldCorners[3].x + 2.5f;
                        UnmappedBlocks.hch[peifijckaoc].hbh[1] = worldCorners[3].z + 2.5f;
                        UnmappedBlocks.hch[peifijckaoc].hbg[4] = worldCorners[0].x - 2.5f;
                        UnmappedBlocks.hch[peifijckaoc].hbh[4] = worldCorners[0].z + 2.5f;
                        UnmappedBlocks.hch[peifijckaoc].hbg[3] = worldCorners[4].x - 2.5f;
                        UnmappedBlocks.hch[peifijckaoc].hbh[3] = worldCorners[4].z - 2.5f;
                        UnmappedBlocks.hch[peifijckaoc].hbg[2] = worldCorners[7].x + 2.5f;
                        UnmappedBlocks.hch[peifijckaoc].hbh[2] = worldCorners[7].z - 2.5f;
                    }
                }
                else
                {
                    string warning = "Barrier_Climbables with name '" + gameObject.name + "' is missing a meshCollider and won't work as expected.";
                    LogWarning(warning);
                }
            }

            foreach (GameObject gameObject in (from t in World.gArena.GetComponentsInChildren<Transform>()
                         where t.gameObject != null && t.gameObject.name.StartsWith("Fence_Climbables")
                         select t.gameObject).ToArray())
            {
                MeshCollider meshCollider = gameObject.GetComponent<MeshCollider>();
                if (meshCollider != null)
                {
                    Bounds bounds = meshCollider.sharedMesh.bounds;

                    Vector3 center = bounds.center;
                    Vector3 extents = bounds.extents;

                    // Calculate the 8 corners of the bounding box
                    Vector3[] corners = new Vector3[8];
                    corners[0] = center + new Vector3(-extents.x, -extents.y, -extents.z);
                    corners[1] = center + new Vector3(-extents.x, -extents.y, extents.z);
                    corners[2] = center + new Vector3(extents.x, -extents.y, extents.z);
                    corners[3] = center + new Vector3(extents.x, -extents.y, -extents.z);
                    corners[4] = center + new Vector3(-extents.x, extents.y, -extents.z);
                    corners[5] = center + new Vector3(-extents.x, extents.y, extents.z);
                    corners[6] = center + new Vector3(extents.x, extents.y, extents.z);
                    corners[7] = center + new Vector3(extents.x, extents.y, -extents.z);

                    // Get the 8 corners of the bounding box as world position
                    Vector3[] worldCorners = new Vector3[corners.Length];
                    for (int i = 0; i < corners.Length; i++)
                    {
                        worldCorners[i] = meshCollider.transform.TransformPoint(corners[i]);
                    }

                    UnmappedBlocks.hcf++;
                    int peifijckaoc = UnmappedBlocks.hcf;
                    UnmappedBlocks.cbh();
                    UnmappedBlocks.hch[peifijckaoc].hbl = 0f;
                    UnmappedBlocks.hch[peifijckaoc].hbi = worldCorners[0].y;
                    UnmappedBlocks.hch[peifijckaoc].hbj = worldCorners[1].y - worldCorners[0].y;
                    UnmappedBlocks.hch[peifijckaoc].hbp = 12;
                    UnmappedBlocks.hch[peifijckaoc].hbf = "Cage";
                    GameObject arenaObject = GetTopLevelParent(gameObject);
                    if (arenaObject.transform.rotation == Quaternion.Euler(0f, 180f, 0f))
                    {
                        UnmappedBlocks.hch[peifijckaoc].hbg[1] = worldCorners[4].x + 2.5f;
                        UnmappedBlocks.hch[peifijckaoc].hbh[1] = worldCorners[4].z + 2.5f;
                        UnmappedBlocks.hch[peifijckaoc].hbg[4] = worldCorners[7].x - 2.5f;
                        UnmappedBlocks.hch[peifijckaoc].hbh[4] = worldCorners[7].z + 2.5f;
                        UnmappedBlocks.hch[peifijckaoc].hbg[3] = worldCorners[3].x - 2.5f;
                        UnmappedBlocks.hch[peifijckaoc].hbh[3] = worldCorners[3].z - 2.5f;
                        UnmappedBlocks.hch[peifijckaoc].hbg[2] = worldCorners[0].x + 2.5f;
                        UnmappedBlocks.hch[peifijckaoc].hbh[2] = worldCorners[0].z - 2.5f;
                    }
                    else
                    {
                        UnmappedBlocks.hch[peifijckaoc].hbg[1] = worldCorners[3].x + 2.5f;
                        UnmappedBlocks.hch[peifijckaoc].hbh[1] = worldCorners[3].z + 2.5f;
                        UnmappedBlocks.hch[peifijckaoc].hbg[4] = worldCorners[0].x - 2.5f;
                        UnmappedBlocks.hch[peifijckaoc].hbh[4] = worldCorners[0].z + 2.5f;
                        UnmappedBlocks.hch[peifijckaoc].hbg[3] = worldCorners[4].x - 2.5f;
                        UnmappedBlocks.hch[peifijckaoc].hbh[3] = worldCorners[4].z - 2.5f;
                        UnmappedBlocks.hch[peifijckaoc].hbg[2] = worldCorners[7].x + 2.5f;
                        UnmappedBlocks.hch[peifijckaoc].hbh[2] = worldCorners[7].z - 2.5f;
                    }
                }
                else
                {
                    string warning = "Fence_Climbables with name '" + gameObject.name + "' is missing a meshCollider and won't work as expected.";
                    LogWarning(warning);
                }
            }

            for (UnmappedBlocks.hcf = last + 1;
                 UnmappedBlocks.hcf <= UnmappedBlocks.hcg;
                 UnmappedBlocks.hcf++)
            {
                if (UnmappedBlocks.hch[UnmappedBlocks.hcf].hbs != null)
                {
                    UnmappedBlocks.hch[UnmappedBlocks.hcf].hbv = UnmappedBlocks
                        .hch[UnmappedBlocks.hcf].hbs.transform.localEulerAngles;
                    UnmappedBlocks.hci = 1;
                }
            }

            GameObject[] doors = World.gArena.GetComponentsInChildren<Transform>()
                .Where(t => t.gameObject != null && t.gameObject.name.StartsWith("Exit")).Select(t => t.gameObject)
                .ToArray();
            foreach (GameObject door in doors)
            {
                if (door.GetComponent<Renderer>() != null)
                {
                    // Access the door's Renderer component to get the bounds
                    Renderer doorRenderer = door.GetComponent<Renderer>();
                    Bounds bounds = doorRenderer.bounds;

                    Vector3[] corners = new Vector3[8];

                    Vector3 min = bounds.min; // Minimum corner
                    Vector3 max = bounds.max; // Maximum corner

                    corners[0] = new Vector3(min.x, min.y, min.z); // Bottom-left-front
                    corners[1] = new Vector3(max.x, min.y, min.z); // Bottom-right-front
                    corners[2] = new Vector3(min.x, min.y, max.z); // Bottom-left-back
                    corners[3] = new Vector3(max.x, min.y, max.z); // Bottom-right-back
                    corners[4] = new Vector3(min.x, max.y, min.z); // Top-left-front
                    corners[5] = new Vector3(max.x, max.y, min.z); // Top-right-front
                    corners[6] = new Vector3(min.x, max.y, max.z); // Top-left-back
                    corners[7] = new Vector3(max.x, max.y, max.z); // Top-right-back

                    Vector3 topRight = corners[1]; // Top-right-front
                    Vector3 bottomLeft = corners[2]; // Bottom-left-front
                    Vector3 bottomRight = corners[3];
                    Vector3 topLeft = corners[0];

                    // Create door and set necessary parameters
                    UnmappedBlocks.cbi();
                    UnmappedBlocks.hcp[0] = UnmappedBlocks.hcp[UnmappedBlocks.hco];
                    //Ignore wording on these, corners of X and Z won't line up right way with way Mat did this but this way builds the collision correctly.
                    UnmappedBlocks.hcp[0].hfh[1] = topRight.x;
                    UnmappedBlocks.hcp[0].hfi[1] = bottomLeft.z;
                    UnmappedBlocks.hcp[0].hfh[4] = bottomRight.x;
                    UnmappedBlocks.hcp[0].hfi[4] = topLeft.z;
                    UnmappedBlocks.hcp[0].hfh[3] = bottomLeft.x;
                    UnmappedBlocks.hcp[0].hfi[3] = topRight.z;
                    UnmappedBlocks.hcp[0].hfh[2] = topLeft.x;
                    UnmappedBlocks.hcp[0].hfi[2] = bottomRight.z;
                    UnmappedBlocks.hcp[0].hfk = max.y - min.y;
                    UnmappedBlocks.hcp[0].hfl = door.transform.rotation.eulerAngles.y;
                    UnmappedBlocks.hcp[0].hfm = 1;
                    UnmappedBlocks.hcp[0].hfo = 1f;
                    UnmappedBlocks.hcp[0].hft = UnmappedSound.gwm[1];
                    UnmappedBlocks.hcp[0].hfp = int.Parse(door.name.Substring(4));
                    UnmappedBlocks.hcp[0].hfs = door.transform.rotation.eulerAngles.y + 180f;
                }
                else
                {
                    //If door is not an invisible cube object, do old method so not to break existing custom maps.
                    Vector3[] corners = new Vector3[4];
                    Vector3 center = door.transform.position;
                    Vector3 localScale = door.transform.localScale;
                    float up = localScale.y;
                    float right = localScale.x;
                    float forward = localScale.z;
                    corners[0] = center + new Vector3(right, 0, forward);
                    corners[1] = center + new Vector3(right, 0, -forward);
                    corners[2] = center + new Vector3(-right, 0, forward);
                    corners[3] = center + new Vector3(-right, 0, -forward);

                    float yTop = center.y + up;
                    float yBottom = center.y - up;

                    Vector3[] xSorted = new Vector3[4];
                    Vector3[] zSorted = new Vector3[4];

                    Array.Copy(corners, xSorted, 4);
                    Array.Copy(corners, zSorted, 4);

                    Array.Sort(xSorted, (a, b) =>
                    {
                        return a.x.CompareTo(b.x);
                    });
                    Array.Sort(zSorted, (a, b) =>
                    {
                        return a.z.CompareTo(b.z);
                    });

                    Vector3 topRight = corners[0];
                    Vector3 bottomRight = corners[0];
                    Vector3 bottomLeft = corners[0];
                    Vector3 topLeft = corners[0];

                    if (zSorted[3].x > zSorted[2].x)
                    {
                        topRight = zSorted[3];
                        bottomRight = zSorted[2];
                    }
                    else
                    {
                        topRight = zSorted[2];
                        bottomRight = zSorted[3];
                    }

                    if (zSorted[1].x < zSorted[0].x)
                    {
                        bottomLeft = zSorted[1];
                        topLeft = zSorted[0];
                    }
                    else
                    {
                        bottomLeft = zSorted[0];
                        topLeft = zSorted[1];
                    }

                    // Create door
                    UnmappedBlocks.cbi();
                    UnmappedBlocks.hcp[0] = UnmappedBlocks.hcp[UnmappedBlocks.hco];
                    UnmappedBlocks.hcp[0].hfh[1] = topRight.x;
                    UnmappedBlocks.hcp[0].hfi[1] = topRight.z;
                    UnmappedBlocks.hcp[0].hfh[4] = bottomRight.x;
                    UnmappedBlocks.hcp[0].hfi[4] = bottomRight.z;
                    UnmappedBlocks.hcp[0].hfh[3] = bottomLeft.x;
                    UnmappedBlocks.hcp[0].hfi[3] = bottomLeft.z;
                    UnmappedBlocks.hcp[0].hfh[2] = topLeft.x;
                    UnmappedBlocks.hcp[0].hfi[2] = topLeft.z;
                    UnmappedBlocks.hcp[0].hfk = yTop - yBottom;
                    UnmappedBlocks.hcp[0].hfl = door.transform.rotation.eulerAngles.y;
                    UnmappedBlocks.hcp[0].hfo = 1f;
                    UnmappedBlocks.hcp[0].hft = UnmappedSound.gwm[1];
                    UnmappedBlocks.hcp[0].hfp = int.Parse(door.name.Substring(4));
                    UnmappedBlocks.hcp[0].hfs = door.transform.rotation.eulerAngles.y + 180f;
                }
            }
        }

        if (Plugin.DebugRender.Value)
        {
            Block[] arr = UnmappedBlocks.hch;
            for (int i = 1; i < arr.Length; i++)
            {
                try
                {
                    GameObject scene = World.gArena;
                    float[] x4 = arr[i].hbg; // float[5], x4[0] is always 0
                    float[] z4 = arr[i].hbh; // float[5], z4[0] is always 0
                    float yLow = arr[i].hbi; // float
                    float yHigh = arr[i].hbj; // float
                    int type = arr[i].hbp; // int
                    Color color = Color.red;
                    switch (type)
                    {
                        case -6:
                            color = new Color(0f, 1f, 0f);
                            break;
                        case -5:
                            color = new Color(0f, 1f, 0.5f);
                            break;
                        case -4:
                            color = new Color(0f, 1f, 1f);
                            break;
                        case -3:
                            color = new Color(0f, 0.5f, 1f);
                            break;
                        case -2:
                            color = new Color(0f, 0f, 1f);
                            break;
                        case -1:
                            color = new Color(0.5f, 0f, 1f);
                            break;
                        case 1:
                            color = new Color(1f, 0f, 1f);
                            break;
                        case 11:
                            color = new Color(1f, 0f, 0f);
                            break;
                        case 12:
                            color = new Color(1f, 1f, 0f);
                            break;
                    }

                    DrawCube(scene.transform, x4, yLow, yHigh, z4, color);

                    if (arr[i].hcb != null)
                    {
                        IEnumerable<float> xl = arr[i].hcb.Skip(1);
                        IEnumerable<float> zl = arr[i].hcc.Skip(1);
                        Color color2 = new Color(1f, 1f, 1f);
                        DrawLines(scene.transform, xl.ToArray(), yLow, yHigh, zl.ToArray(), color2, "Seating");
                    }
                }
                catch (Exception e)
                {
                    LogWarning(e);
                }
            }

            cd[] arr2 = UnmappedBlocks.hda;
            for (int i = 1; i < arr2.Length; i++)
            {
                try
                {
                    GameObject scene = World.gArena;
                    float[] x4 = arr2[i].hgh; // float[5], x4[0] is always 0
                    float[] z4 = arr2[i].hgi; // float[5], z4[0] is always 0
                    int yLow = 0;
                    int yHigh = 0;
                    Color color = new Color(1f, 0.5f, 0f);
                    DrawCube(scene.transform, x4, yLow, yHigh, z4, color);
                }
                catch (Exception e)
                {
                    LogWarning(e);
                }
            }

            Door[] arr3 = UnmappedBlocks.hcp;
            for (int i = 1; i < arr3.Length; i++)
            {
                try
                {
                    GameObject scene = World.gArena;
                    float[] x4 = arr3[i].hfh; // float[5], x4[0] is always 0
                    float[] z4 = arr3[i].hfi; // float[5], z4[0] is always 0
                    float yLow = World.ground - 5f;
                    float yHigh = arr3[i].hfk; // float

                    Color color = new Color(0.5f, 1f, 0f);
                    DrawCube(scene.transform, x4, yLow, yHigh, z4, color);
                }
                catch (Exception e)
                {
                    LogWarning(e);
                }
            }
        }
    }

    public static GameObject GetTopLevelParent(GameObject childObject)
    {
        Transform currentTransform = childObject.transform;

        // Traverse up the hierarchy until there's no parent
        while (currentTransform.parent != null)
        {
            currentTransform = currentTransform.parent;
        }

        // Return the top-level parent GameObject
        return currentTransform.gameObject;
    }

    private static void DrawCube(Transform parent, float[] x4, float yLow, float yHigh, float[] z4, Color color)
    {
        // Bottom
        GameObject child = new GameObject("Bottom");
        child.transform.parent = parent;
        LineRenderer lineRenderer = child.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.positionCount = 4;
        lineRenderer.SetPosition(0, new Vector3(x4[1], yLow, z4[1]));
        lineRenderer.SetPosition(1, new Vector3(x4[2], yLow, z4[2]));
        lineRenderer.SetPosition(2, new Vector3(x4[3], yLow, z4[3]));
        lineRenderer.SetPosition(3, new Vector3(x4[4], yLow, z4[4]));
        lineRenderer.sortingOrder = 999;
        lineRenderer.loop = true;

        // Top
        child = new GameObject("Top");
        child.transform.parent = parent;
        lineRenderer = child.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.positionCount = 4;
        lineRenderer.SetPosition(0, new Vector3(x4[1], yHigh, z4[1]));
        lineRenderer.SetPosition(1, new Vector3(x4[2], yHigh, z4[2]));
        lineRenderer.SetPosition(2, new Vector3(x4[3], yHigh, z4[3]));
        lineRenderer.SetPosition(3, new Vector3(x4[4], yHigh, z4[4]));
        lineRenderer.sortingOrder = 999;
        lineRenderer.loop = true;

        // Sides
        child = new GameObject("Side1");
        child.transform.parent = parent;
        lineRenderer = child.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, new Vector3(x4[1], yLow, z4[1]));
        lineRenderer.SetPosition(1, new Vector3(x4[1], yHigh, z4[1]));
        lineRenderer.sortingOrder = 999;

        child = new GameObject("Side2");
        child.transform.parent = parent;
        lineRenderer = child.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, new Vector3(x4[2], yLow, z4[2]));
        lineRenderer.SetPosition(1, new Vector3(x4[2], yHigh, z4[2]));
        lineRenderer.sortingOrder = 999;

        child = new GameObject("Side3");
        child.transform.parent = parent;
        lineRenderer = child.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, new Vector3(x4[3], yLow, z4[3]));
        lineRenderer.SetPosition(1, new Vector3(x4[3], yHigh, z4[3]));
        lineRenderer.sortingOrder = 999;

        child = new GameObject("Side4");
        child.transform.parent = parent;
        lineRenderer = child.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, new Vector3(x4[4], yLow, z4[4]));
        lineRenderer.SetPosition(1, new Vector3(x4[4], yHigh, z4[4]));
        lineRenderer.sortingOrder = 999;
    }

    private static void DrawLines(Transform parent, float[] xl, float yLow, float yHigh, float[] zl, Color color,
        string name = "Lines")
    {
        int count = Math.Min(xl.Length, zl.Length);
        GameObject child = new GameObject(name);
        child.transform.parent = parent;
        LineRenderer lineRenderer = child.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.positionCount = count;
        for (int i = 0; i < count; i++)
        {
            lineRenderer.SetPosition(i, new Vector3(xl[i], yLow, zl[i]));
        }

        lineRenderer.sortingOrder = 999;
        lineRenderer.loop = true;
    }

    /*
     * Patch:
     * - Disables default boundaries for custom arenas
     */
    [HarmonyPatch(typeof(World), nameof(World.ceo))]
    [HarmonyPrefix]
    public static bool World_ceo(int a)
    {
        if (World.location > VanillaCounts.Data.NoLocations)
        {
            World.ground = 0f;
            World.ceiling = 100f;
            World.farNorth = 9999f;
            World.farSouth = -9999f;
            World.farEast = 9999f;
            World.farWest = -9999f;
            World.camNorth = 60f;
            World.camSouth = -60f;
            World.camEast = 60;
            World.camWest = -60f;
            return false;
        }

        return true;
    }

    /*
     * Patch:
     * - Sets custom arenas as 'available' for the game to load.
     */
    [HarmonyPatch(typeof(World), nameof(World.cde))]
    [HarmonyPrefix]
    public static bool World_cde(ref int __result, int a)
    {
        if (a <= VanillaCounts.Data.NoLocations)
        {
            return true;
        }
        __result = 1;
        if (a - VanillaCounts.Data.NoLocations - 1 >= CustomArenaPrefabs.Count)
        {
            __result = 0;
        }
        return false;
    }

    /*
     * Patch:
     * - Determines the floor height for custom arenas.
     */
    [HarmonyPatch(typeof(World), nameof(World.cep))]
    [HarmonyPostfix]
    public static void World_cep(ref float __result, float a, float b, float c)
    {
        if (World.location > VanillaCounts.Data.NoLocations)
        {
            if (World.ringShape != 0 && a > -40f && a < 40f && c > -40f &&
                c < 40f)
            {
                return;
            }

            Vector3 coords = new Vector3(a, b, c).Round(2);
            string cstr = coords.ToString();
            if (RaycastCache.TryGetValue(cstr, out float cached))
            {
                __result = cached;
                return;
            }

            // Raycast down to find the ground
            Ray ray = new Ray(coords + new Vector3(0, 5f, 0f), Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit hit, 105f, 1 << 0))
            {
                __result = hit.point.y;
            }
            else
            {
                __result = World.ground;
            }

            if (!RaycastCache.ContainsKey(cstr))
            {
                RaycastCache.Add(cstr, __result);
            }
        }
    }
}