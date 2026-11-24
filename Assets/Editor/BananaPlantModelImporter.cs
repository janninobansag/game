using System;
using UnityEditor;
using UnityEngine;

// This model includes an Unreal-style UCX collision mesh. Unity otherwise
// imports it as visible geometry with a gray default material.
public class BananaPlantModelImporter : AssetPostprocessor
{
    private const string ModelPath = "Assets/0ANDRIE/TREES N GRASS/Textures/Banana_Plant_01.fbx";

    private void OnPostprocessModel(GameObject model)
    {
        if (!string.Equals(assetPath, ModelPath, StringComparison.OrdinalIgnoreCase))
            return;

        foreach (MeshFilter filter in model.GetComponentsInChildren<MeshFilter>(true))
        {
            if (!filter.name.StartsWith("UCX_", StringComparison.OrdinalIgnoreCase))
                continue;

            Renderer renderer = filter.GetComponent<Renderer>();
            if (renderer != null)
                renderer.enabled = false;

            if (filter.sharedMesh != null && filter.GetComponent<Collider>() == null)
            {
                MeshCollider collider = filter.gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = filter.sharedMesh;
                collider.convex = true;
            }
        }
    }

    [InitializeOnLoadMethod]
    private static void RepairExistingImport()
    {
        // Apply the fix to the model already in the project after script reload.
        // A repaired import does not request another reimport.
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null)
                return;

            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.enabled && renderer.name.StartsWith("UCX_", StringComparison.OrdinalIgnoreCase))
                {
                    AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceUpdate);
                    break;
                }
            }
        };
    }
}
