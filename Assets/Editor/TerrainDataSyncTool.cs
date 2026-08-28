#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Safely makes the selected Terrain render the same TerrainData used by its
/// TerrainCollider. This avoids accidentally assigning Chapter 1 data to Chapter 2.
/// </summary>
public static class TerrainDataSyncTool
{
    [MenuItem("Varen/Terrain/Fix Selected Terrain From Collider")]
    private static void FixSelectedTerrainFromCollider()
    {
        Terrain terrain = Selection.activeGameObject != null
            ? Selection.activeGameObject.GetComponent<Terrain>()
            : null;

        if (terrain == null)
        {
            EditorUtility.DisplayDialog("Select a Terrain", "Select the Terrain object in the Hierarchy first.", "OK");
            return;
        }

        TerrainCollider terrainCollider = terrain.GetComponent<TerrainCollider>();
        if (terrainCollider == null || terrainCollider.terrainData == null)
        {
            EditorUtility.DisplayDialog("Terrain Collider Data Missing", "The selected Terrain needs a Terrain Collider with Terrain Data assigned.", "OK");
            return;
        }

        if (terrain.terrainData == terrainCollider.terrainData)
        {
            EditorUtility.DisplayDialog("Already Correct", "The Terrain and Terrain Collider already use the same Terrain Data.", "OK");
            return;
        }

        Undo.RecordObject(terrain, "Fix Terrain Data From Collider");
        terrain.terrainData = terrainCollider.terrainData;
        EditorUtility.SetDirty(terrain);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        EditorUtility.DisplayDialog(
            "Terrain Fixed",
            "The visible Terrain now uses: " + terrainCollider.terrainData.name + ". Save the scene with Ctrl+S.",
            "OK");
    }

    [MenuItem("Varen/Terrain/Fix Selected Terrain From Collider", true)]
    private static bool ValidateFixSelectedTerrainFromCollider()
    {
        return Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<Terrain>() != null;
    }
}
#endif