using System;
using System.Collections.Generic;
using UnityEngine;

public class JMesh
{
    public int jMeshID;

    public Meshopt.MeshData meshData;

    public JMesh(int jMeshID)
    {
        Debug.LogError(jMeshID);
        this.jMeshID = jMeshID;
        meshData = PrefabProcesser.LoadMeshDataFromFile(jMeshID);
        if (meshData.meshlets == null)
        {
            Debug.LogError("Failed to load JMesh data");
        }
        Debug.Log($"MeshData: {meshData.meshlets.Length}");
        Debug.Log($"MeshData: {meshData.vertices.Length}");
        Debug.Log($"MeshData: {meshData.normals.Length}");
        Debug.Log($"MeshData: {meshData.tangents.Length}");
        Debug.Log($"MeshData: {meshData.meshletTriangles.Length}");
        Debug.Log($"MeshData: {meshData.meshletVertices.Length}");
        Debug.Log($"MeshData: {meshData.normals[0]}");
        Debug.Log($"MeshData: {meshData.vertices[1]}");
        Debug.Log($"MeshData: {meshData.meshletTriangles[2]}");
    }
}