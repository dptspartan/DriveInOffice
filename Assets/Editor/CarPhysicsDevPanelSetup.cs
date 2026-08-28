using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CarPhysicsDevPanelSetup
{
    [MenuItem("DriveInOffice/Dev/Add Car Physics Dev Panel To Scene")]
    public static void AddToScene()
    {
        CarPhysicsDevPanel existing = Object.FindAnyObjectByType<CarPhysicsDevPanel>();
        if (existing != null)
        {
            Selection.activeGameObject = existing.gameObject;
            Debug.Log("CarPhysicsDevPanel already in scene: " + existing.name);
            return;
        }

        GameObject go = new GameObject("CarPhysicsDevPanel");
        go.AddComponent<CarPhysicsDevPanel>();
        Undo.RegisterCreatedObjectUndo(go, "Add Car Physics Dev Panel");
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = go;
        Debug.Log("Added CarPhysicsDevPanel. Press M in Play mode.");
    }
}
