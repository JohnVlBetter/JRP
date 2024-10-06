using System;
using System.Collections.Generic;
using UnityEngine;

public class JObject : MonoBehaviour
{
    private JMaterial material;
    private JMesh mesh;

#if UNITY_EDITOR
    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;
#endif

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        var mesh = meshFilter.sharedMesh;
        var meshData = Meshopt.BuildMeshlets(mesh);
        Debug.Log($"MeshData: {meshData.meshlets.Length}");
    }
}