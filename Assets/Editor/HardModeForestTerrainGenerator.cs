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

        // Keep the Terrain Data currently assigned to Chapter 2. Only create a
        // separate asset if this scene is still using the Chapter 1 terrain asset.
        TerrainData hardTerrain = GetOrCreateHardTerrain(terrain.terrainData);
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
        PaintForestGround(hardTerrain);
        ConfigureForestAtmosphere();
        PlantForest(hardTerrain, terrain.transform, protectedAreas);

        EditorUtility.SetDirty(hardTerrain);
        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Forest Terrain Ready", "The terrain was regenerated with full building clearings. Your Normal terrain asset remains unchanged.", "OK");
    }

    private static TerrainData GetOrCreateHardTerrain(TerrainData currentTerrain)
    {
        string currentPath = AssetDatabase.GetAssetPath(currentTerrain);

        // Chapter 2 already has independent Terrain Data. Never replace it with
        // the old generated asset or copy Chapter 1's heights over it.
        if (!string.IsNullOrEmpty(currentPath) && currentPath != NormalTerrainPath)
            return currentTerrain;

        if (!AssetDatabase.IsValidFolder(HardTerrainFolder))
            AssetDatabase.CreateFolder("Assets", "HardMode");

        TerrainData existing = AssetDatabase.LoadAssetAtPath<TerrainData>(HardTerrainPath);
        if (existing != null)
            return existing;

        if (string.IsNullOrEmpty(currentPath) || !AssetDatabase.CopyAsset(currentPath, HardTerrainPath))
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
                              name == "church" || name == "guardhouse" || name.StartsWith("house_prefab") || name.Contains("mansion");
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