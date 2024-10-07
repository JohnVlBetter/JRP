using UnityEditor;
using UnityEngine;

public class PrefabProcesser : MonoBehaviour
{
    [MenuItem("Tools/Process Prefabs")]
    public static void ProcessPrefabs()
    {
        string path = "Assets/Res/Prefabs";
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new string[] { path });
        foreach (string guid in guids)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Debug.Log($"Processing prefab: {prefab.name}");
            ProcessPrefab(prefab);
            AssetDatabase.SaveAssetIfDirty(prefab);
        }
    }

    private static void ProcessPrefab(GameObject prefab)
    {
        var mf = prefab.GetComponent<MeshFilter>();
        if (mf != null)
        {
            ProcessMesh(prefab.gameObject, mf);
        }
        var cCount = prefab.transform.childCount;
        for (int i = 0; i < cCount; i++)
        {
            var child = prefab.transform.GetChild(i);
            ProcessPrefab(child.gameObject);
        }
    }

    private static void ProcessMesh(GameObject prefab, MeshFilter mf)
    {
        var mesh = mf.sharedMesh;
        var meshData = Meshopt.BuildMeshlets(mesh);
        Debug.Log($"MeshData: {meshData.meshlets.Length}");
        var jo = prefab.GetComponent<JObject>();
        if (jo == null)
        {
            jo = prefab.AddComponent<JObject>();
        }
        jo.jMeshID = 10;
    }
}
