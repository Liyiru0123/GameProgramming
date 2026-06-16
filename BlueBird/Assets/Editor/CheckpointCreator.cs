using UnityEditor;
using UnityEngine;

public static class CheckpointCreator
{
    private const string CheckpointSpritePath = "Assets/Art/UI/Checkpoints/CustomCheckpoint.png";

    [MenuItem("GameObject/BlueBird/Create Checkpoint", false, 10)]
    private static void CreateCheckpoint(MenuCommand menuCommand)
    {
        GameObject checkpointObject = new GameObject("Checkpoint");
        GameObjectUtility.SetParentAndAlign(checkpointObject, menuCommand.context as GameObject);

        Undo.RegisterCreatedObjectUndo(checkpointObject, "Create Checkpoint");

        SpriteRenderer renderer = checkpointObject.AddComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CheckpointSpritePath);
        renderer.sortingOrder = -1;

        CircleCollider2D trigger = checkpointObject.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 0.65f;

        Checkpoint checkpoint = checkpointObject.AddComponent<Checkpoint>();

        GameObject respawnObject = new GameObject("RespawnPoint");
        Undo.RegisterCreatedObjectUndo(respawnObject, "Create Checkpoint Respawn Point");
        respawnObject.transform.SetParent(checkpointObject.transform, false);
        respawnObject.transform.localPosition = new Vector3(0f, -1f, 0f);

        SerializedObject serializedCheckpoint = new SerializedObject(checkpoint);
        serializedCheckpoint.FindProperty("checkpointId").stringValue = checkpointObject.name;
        serializedCheckpoint.FindProperty("respawnPoint").objectReferenceValue = respawnObject.transform;
        serializedCheckpoint.FindProperty("visualRoot").objectReferenceValue = checkpointObject.transform;
        serializedCheckpoint.FindProperty("targetRenderer").objectReferenceValue = renderer;
        serializedCheckpoint.ApplyModifiedPropertiesWithoutUndo();

        Selection.activeGameObject = checkpointObject;
        EditorGUIUtility.PingObject(checkpointObject);
    }
}
