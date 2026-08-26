#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds a separate forest terrain for Chapter 2 Hard Mode without changing Normal Mode terrain.
/// Full renderer bounds of the existing buildings are preserved as clear, unchanged ground.
/// </summary>
public static class HardModeForestTerrainGenerator
{
    private const string NormalTerrainPath = "Assets/New Terrain.asset";
    private const string HardTerrainFolder = "Assets/HardMode";
    private const string HardTerrainPath = HardTerrainFolder + "/Chapter2_ForestTerrain.asset";
    private const float BuildingPadding = 12f;
    private const float TreeClearance = 10f;

    private struct ProtectedArea
    {
        public Vector3 center;
        public Vector2 halfSize;
    }

    [MenuItem("Varen/Hard Mode/Build Chapter 2 Forest Terrain")]
    private static void BuildForestTerrain()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != "chapter 2")
        {
            EditorUtility.DisplayDialog("Open Chapter 2", "Open the chapter 2 Hard Mode scene, then run this command.", "OK");
            return;
        }

        Terrain terrain = Object.FindObjectOfType<Terrain>();
        if (terrain == null || terrain.terrainData == null)
        {
            EditorUtility.DisplayDialog("Terrain Not Found", "This scene needs an active Unity Terrain object.", "OK");
            return;
        }

        TerrainData normalTerrain = AssetDatabase.LoadAssetAtPath<TerrainData>(NormalTerrainPath);
        if (normalTerrain == null)
            normalTerrain = terrain.terrainData;

        TerrainData hardTerrain = GetOrCreateHardTerrain(normalTerrain);
        if (hardTerrain == null)
            return;

        Undo.RecordObject(terrain, "Assign Hard Mode Forest Terrain");
        terrain.terrainData = hardTerrain;
        TerrainCollider terrainCollider = terrain.GetComponent<TerrainCollider>();
        if (terrainCollider != null)
        {
            Undo.RecordObject(terrainCollider, "Assign Hard Mode Forest Terrain");
            terrainCollider.terrainData = hardTerrain;
        }

        List<ProtectedArea> protectedAreas = FindProtectedAreas(terrain.transform);
        if (protectedAreas.Count < 4)
        {
            EditorUtility.DisplayDialog("Building Clearings Not Found", "The generator could not find all building clearings, so it stopped before changing the terrain.", "OK");
            return;
        }
        GenerateHeights(hardTerrain, normalTerrain, terrain.transform, protectedAreas);
        PaintForestGround(hardTerrain);
        ConfigureForestAtmosphere();
        PlantForest(hardTerrain, terrain.transform, protectedAreas);

        EditorUtility.SetDirty(hardTerrain);
        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Forest Terrain Ready", "The terrain was regenerated with full building clearings. Your Normal terrain asset remains unchanged.", "OK");
    }

    private static TerrainData GetOrCreateHardTerrain(TerrainData normalTerrain)
    {
        TerrainData existing = AssetDatabase.LoadAssetAtPath<TerrainData>(HardTerrainPath);
        if (existing != null)
            return existing;

        if (!AssetDatabase.IsValidFolder(HardTerrainFolder))
            AssetDatabase.CreateFolder("Assets", "HardMode");

        string sourcePath = AssetDatabase.GetAssetPath(normalTerrain);
        if (string.IsNullOrEmpty(sourcePath) || !AssetDatabase.CopyAsset(sourcePath, HardTerrainPath))
        {
            EditorUtility.DisplayDialog("Could Not Copy Terrain", "Unity could not create the separate Hard Mode terrain asset.", "OK");
            return null;
        }

        return AssetDatabase.LoadAssetAtPath<TerrainData>(HardTerrainPath);
    }

    private static List<ProtectedArea> FindProtectedAreas(Transform terrainTransform)
    {
        var roots = new HashSet<Transform>();
        foreach (Transform item in Object.FindObjectsOfType<Transform>(true))
        {
            string name = item.name.ToLowerInvariant();
            bool isBuilding = name == "house 1" || name == "house 2" || name == "house 3" ||
                              name == "church" || name == "guardhouse" || name.StartsWith("house_prefab");
            if (isBuilding || item.CompareTag("Player"))
                roots.Add(item);
        }

        var areas = new List<ProtectedArea>();
        foreach (Transform root in roots)
        {
            Bounds bounds = new Bounds(root.position, Vector3.zero);
            bool hasRenderer = false;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!hasRenderer)
                {
                    bounds = renderer.bounds;
                    hasRenderer = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            bool isPlayer = root.CompareTag("Player");
            if (!hasRenderer)
                bounds = new Bounds(root.position, new Vector3(16f, 4f, 16f));

            Vector3 local = terrainTransform.InverseTransformPoint(bounds.center);
            if (local.x < 0f || local.z < 0f)
                continue;

            float padding = isPlayer ? 10f : BuildingPadding;
            areas.Add(new ProtectedArea
            {
                center = bounds.center,
                halfSize = new Vector2(Mathf.Max(10f, bounds.extents.x + padding), Mathf.Max(10f, bounds.extents.z + padding))
            });
        }

        return areas;
    }

private static void GenerateHeights(TerrainData data, TerrainData normalTerrain, Transform terrainTransform, List<ProtectedArea> protectedAreas)
    {
        int resolution = data.heightmapResolution;
        float[,] original = normalTerrain.GetHeights(0, 0, resolution, resolution);
        float[,] heights = new float[resolution, resolution];
        Vector3 size = data.size;
        Vector2[] mountainCenters =
        {
            new Vector2(0.12f, 0.18f), new Vector2(0.84f, 0.17f),
            new Vector2(0.18f, 0.84f), new Vector2(0.83f, 0.82f)
        };
        float mountainHeight = Mathf.Clamp(42f / Mathf.Max(1f, size.y), 0.02f, 0.09f);
        float mountainRadius = 0.16f;

        for (int z = 0; z < resolution; z++)
        {
            float nz = z / (float)(resolution - 1);
            for (int x = 0; x < resolution; x++)
            {
                float nx = x / (float)(resolution - 1);
                float mountain = 0f;
                foreach (Vector2 center in mountainCenters)
                {
                    float distance = Vector2.Distance(new Vector2(nx, nz), center);
                    mountain += Mathf.Exp(-(distance * distance) / (2f * mountainRadius * mountainRadius)) * mountainHeight;
                }
                float detail = (Mathf.PerlinNoise(nx * 7f + 19f, nz * 7f + 37f) - 0.5f) * 0.006f;
                heights[z, x] = Mathf.Clamp01(original[z, x] + mountain + detail);
            }
        }

        // Restore the original height under the complete footprint of every important building.
        foreach (ProtectedArea area in protectedAreas)
        {
            Vector3 local = terrainTransform.InverseTransformPoint(area.center);
            int centerX = Mathf.RoundToInt(Mathf.Clamp01(local.x / size.x) * (resolution - 1));
            int centerZ = Mathf.RoundToInt(Mathf.Clamp01(local.z / size.z) * (resolution - 1));
            int extentX = Mathf.CeilToInt(area.halfSize.x / size.x * (resolution - 1));
            int extentZ = Mathf.CeilToInt(area.halfSize.y / size.z * (resolution - 1));

            for (int z = Mathf.Max(0, centerZ - extentZ); z <= Mathf.Min(resolution - 1, centerZ + extentZ); z++)
            {
                for (int x = Mathf.Max(0, centerX - extentX); x <= Mathf.Min(resolution - 1, centerX + extentX); x++)
                {
                    float dx = ((x / (float)(resolution - 1) * size.x) - local.x) / area.halfSize.x;
                    float dz = ((z / (float)(resolution - 1) * size.z) - local.z) / area.halfSize.y;
                    float distance = Mathf.Sqrt(dx * dx + dz * dz);
                    float blend = 1f - Mathf.SmoothStep(0.68f, 1f, distance);
                    heights[z, x] = Mathf.Lerp(heights[z, x], original[z, x], blend);
                }
            }
        }

        data.SetHeights(0, 0, heights);
    }

    private static void ConfigureForestAtmosphere()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.13f, 0.18f, 0.15f, 1f);
        RenderSettings.fogDensity = 0.006f;
        RenderSettings.ambientIntensity = 0.8f;
    }

    private static void PaintForestGround(TerrainData data)
    {
        int layers = data.terrainLayers.Length;
        if (layers == 0)
            return;

        int width = data.alphamapWidth;
        int height = data.alphamapHeight;
        float[,,] maps = new float[height, width, layers];
        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                float slope = data.GetSteepness(x / (float)(width - 1), z / (float)(height - 1));
                float dirt = layers > 1 ? Mathf.Clamp01((slope - 10f) / 28f) * 0.45f : 0f;
                maps[z, x, 0] = 1f - dirt;
                if (layers > 1) maps[z, x, 1] = dirt;
            }
        }
        data.SetAlphamaps(0, 0, maps);
    }

    private static void PlantForest(TerrainData data, Transform terrainTransform, List<ProtectedArea> protectedAreas)
    {
        if (data.treePrototypes == null || data.treePrototypes.Length == 0)
            return;

        var trees = new List<TreeInstance>();
        var random = new System.Random(2402);
        for (int attempt = 0; attempt < 2400 && trees.Count < 650; attempt++)
        {
            float x = (float)random.NextDouble();
            float z = (float)random.NextDouble();
            Vector3 world = terrainTransform.TransformPoint(new Vector3(x * data.size.x, 0f, z * data.size.z));
            if (IsInsideProtectedArea(world, protectedAreas))
                continue;
            if (data.GetSteepness(x, z) > 26f)
                continue;

            trees.Add(new TreeInstance
            {
                position = new Vector3(x, data.GetInterpolatedHeight(x, z) / data.size.y, z),
                prototypeIndex = random.Next(data.treePrototypes.Length),
                widthScale = 0.75f + (float)random.NextDouble() * 0.55f,
                heightScale = 0.8f + (float)random.NextDouble() * 0.65f,
                color = Color.white,
                lightmapColor = Color.white
            });
        }
        data.treeInstances = trees.ToArray();
    }

    private static bool IsInsideProtectedArea(Vector3 worldPosition, List<ProtectedArea> protectedAreas)
    {
        foreach (ProtectedArea area in protectedAreas)
        {
            float x = Mathf.Abs(worldPosition.x - area.center.x);
            float z = Mathf.Abs(worldPosition.z - area.center.z);
            if (x < area.halfSize.x + TreeClearance && z < area.halfSize.y + TreeClearance)
                return true;
        }
        return false;
    }
}
#endif