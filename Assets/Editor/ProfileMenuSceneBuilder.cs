using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ProfileMenuSceneBuilder
{
    private const string MenuScenePath = "Assets/Scenes/menu.unity";

    [MenuItem("Tools/VAREN/Update Profile UI in Menu Canvas")]
    public static void EnsureProfileUi()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[ProfileMenuSceneBuilder] Exit Play mode before editing the menu scene.");
            return;
        }

        Scene scene = SceneManager.GetSceneByPath(MenuScenePath);
        bool openedHere = !scene.IsValid() || !scene.isLoaded;
        if (openedHere)
            scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Additive);

        try
        {
            MenuManager manager = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                manager = root.GetComponentInChildren<MenuManager>(true);
                if (manager != null)
                    break;
            }

            if (manager == null)
                return;

            bool createdPanel = manager.EnsureProfileUiInCanvas();
            manager.EnsureProfileRowTemplates();
            manager.EnsureProfileCloseButton();
            manager.ConfigureProfileScroll();
            if (createdPanel)
                manager.ConfigureProfileAppearance(
                    AssetDatabase.LoadAssetAtPath<Font>("Assets/0ANDRIE/OTHERS/Blood Hunter TTF Demo.ttf"),
                    AssetDatabase.LoadAssetAtPath<Font>("Assets/0ANDRIE/OTHERS/InriaSerif-Bold.ttf"),
                    AssetDatabase.LoadAssetAtPath<Font>("Assets/0ANDRIE/OTHERS/Noto_Sans_KR/static/NotoSansKR-Medium.ttf"),
                    AssetDatabase.LoadAssetAtPath<RenderTexture>("Assets/0ANDRIE/OTHERS/MenuRenderTexture.renderTexture"));

            EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ProfileMenuSceneBuilder] Updated the profile UI in the menu Canvas.");
        }
        finally
        {
            if (openedHere)
                EditorSceneManager.CloseScene(scene, true);
        }
    }

}
