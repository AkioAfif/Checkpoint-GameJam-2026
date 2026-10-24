using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Gives the imported village (the CozyFarm root in FixScene) real colliders (Tools > FixScene > Add Environment Colliders):
// every mesh at least MIN_SIZE across gets a MeshCollider, so the player, the truck and the chase camera meet the houses,
// trees and fences. Re-runnable: meshes that already have a collider are left alone, so new models only need another run.
public static class EnvironmentColliderBuilder
{
    private const string ENVIRONMENT_ROOT = "CozyFarm";
    // flowers and grass tufts stay walk-through
    private const float MIN_SIZE = .3f;

    [MenuItem("Tools/FixScene/Add Environment Colliders")]
    public static void AddEnvironmentColliders()
    {
        GameObject root = GameObject.Find(ENVIRONMENT_ROOT);
        if (root == null)
        {
            Debug.LogError($"No '{ENVIRONMENT_ROOT}' root in the open scene.");
            return;
        }

        int added = 0;
        foreach (MeshFilter meshFilter in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if (meshFilter.sharedMesh == null || meshFilter.GetComponent<Collider>() != null) continue;
            if (!meshFilter.TryGetComponent(out Renderer meshRenderer)) continue;
            Vector3 size = meshRenderer.bounds.size;
            if (Mathf.Max(size.x, size.y, size.z) < MIN_SIZE) continue;
            MeshCollider meshCollider = Undo.AddComponent<MeshCollider>(meshFilter.gameObject);
            meshCollider.sharedMesh = meshFilter.sharedMesh;
            added++;
        }
        EditorSceneManager.MarkSceneDirty(root.scene);
        Debug.Log($"Added {added} environment colliders under {ENVIRONMENT_ROOT}.");
    }
}
