using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CarPhysicsDevPanelSetup
{
    [MenuItem("DriveInOffice/Dev/Add Car Physics Dev Panel To Scene")]
    public static void AddDevPanelToScene()
    {
        CarPhysicsDevPanel existing = Object.FindFirstObjectByType<CarPhysicsDevPanel>();
        if (existing != null)
        {
            Selection.activeGameObject = existing.gameObject;
            EditorGUIUtility.PingObject(existing.gameObject);
            Debug.Log("Car Physics Dev Panel already exists in this scene.");
            return;
        }

        GameObject go = new GameObject("CarPhysicsDevPanel");
        go.AddComponent<CarPhysicsDevPanel>();
        Undo.RegisterCreatedObjectUndo(go, "Add Car Physics Dev Panel");
        Selection.activeGameObject = go;
        EditorSceneManager.MarkSceneDirty(go.scene);
        Debug.Log("Added CarPhysicsDevPanel. Press Play and M to open the tuner.");
    }
}
