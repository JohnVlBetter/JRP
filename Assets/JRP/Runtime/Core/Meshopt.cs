using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

public class Meshopt
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Meshlet
    {
        public System.UInt32 vertexOffset;
        public System.UInt32 triangleOffset;
        public System.UInt32 vertexCount;
        public System.UInt32 triangleCount;

        /* bounding box, useful for frustum and occlusion culling */
        public Vector3 min;
        public Vector3 max;

        /* normal cone, useful for backface culling */
        public Vector3 coneApex;
        public Vector3 coneAxis;
        public float coneCutoff; /* = cos(angle/2) */
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MeshoptBounds
    {
        /* bounding box, useful for frustum and occlusion culling */
        public Vector3 min;
        public Vector3 max;

        /* normal cone, useful for backface culling */
        public Vector3 cone_apex;
        public Vector3 cone_axis;
        public float cone_cutoff; /* = cos(angle/2) */

        /* normal cone axis and cutoff, stored in 8-bit SNORM format; decode using x/127.0 */
        public int cone_axis_cutoff_s8;
    }

    [DllImport("ClusterizerUtil")]
    private static extern Int64 meshopt_buildMeshletsBound(Int64 index_count, Int64 max_vertices,
        Int64 max_triangles);
    [DllImport("ClusterizerUtil")]
    private static extern Int64 meshopt_buildMeshlets(Meshlet[] meshlets, uint[] meshlet_vertices,
        byte[] meshlet_triangles, int[] indices, Int64 index_count, Vector3[] vertex_positions,
        Int64 vertexCount, Int64 vertex_positions_stride, Int64 max_vertices, Int64 max_triangles,
        float cone_weight);
    [DllImport("ClusterizerUtil")]
    private static extern MeshoptBounds meshopt_computeMeshletBounds(uint[] meshlet_vertices,
        byte[] meshlet_triangles, Int64 triangle_count, float[] vertex_positions, Int64 vertex_count,
        Int64 vertex_positions_stride);

    public struct MeshData
    {
        public Meshlet[] meshlets;
        public Vector3[] vertices;
        public Vector3[] normals;
        public Vector4[] tangents;
        public uint[] meshletTriangles;
        public uint[] meshletVertices;
    }

    public static MeshData BuildMeshlets(Mesh mesh)
    {
        const Int64 maxVertices = 255;
        const Int64 maxTriangles = 64;
        const float coneWeight = 0.0f;

        MeshData meshData = new MeshData();
        List<Vector3> vertices = new List<Vector3>();
        mesh.GetVertices(vertices);

        List<uint> meshletTrianglesUintList = new List<uint>();
        List<uint> meshletVerticesList = new List<uint>();
        List<Meshlet> meshletList = new List<Meshlet>();
        for (int meshIdx = 0; meshIdx < mesh.subMeshCount; ++meshIdx)
        {
            int[] triangles = mesh.GetTriangles(meshIdx);
            Int64 maxMeshlets = meshopt_buildMeshletsBound(triangles.Length, maxVertices, maxTriangles);
            Meshlet[] meshlets = new Meshlet[maxMeshlets];
            uint[] meshletVertices = new uint[maxMeshlets * maxVertices];
            byte[] meshletTriangles = new byte[maxMeshlets * maxTriangles * 3];

            Int64 meshlet_count = meshopt_buildMeshlets(meshlets, meshletVertices,
                meshletTriangles, triangles, triangles.Length, vertices.ToArray(),
                vertices.Count, 3 * 4, maxVertices, maxTriangles, coneWeight);

            if (meshlet_count <= 0)
            {
                Debug.LogError("Meshlet划分失败!");
                return meshData;
            }
            Array.Resize(ref meshlets, (int)meshlet_count);
            Meshlet last = meshlets.Last();
            Array.Resize(ref meshletVertices, (int)(last.vertexOffset + last.vertexCount));
            int meshletTrianglesCount = (int)(last.triangleOffset + ((last.triangleCount * 3 + 3) & ~3));
            uint[] meshletTrianglesUint = new uint[meshletTrianglesCount];
            for (int idx = 0; idx < meshletTrianglesCount; ++idx)
            {
                meshletTrianglesUint[idx] = meshletTriangles[idx];
            }
            for (int idx = 0; idx < meshlet_count; ++idx)
            {
                meshlets[idx].vertexOffset += (uint)meshletVerticesList.Count;
                meshlets[idx].triangleOffset += (uint)meshletTrianglesUintList.Count;
            }
            meshletTrianglesUintList.AddRange(meshletTrianglesUint);
            meshletVerticesList.AddRange(meshletVertices);
            meshletList.AddRange(meshlets);
        }
        List<Vector3> normals = new List<Vector3>();
        mesh.GetNormals(normals);
        List<Vector4> tangents = new List<Vector4>();
        mesh.GetTangents(tangents);

        meshData.vertices = vertices.ToArray();
        meshData.normals = normals.ToArray();
        meshData.tangents = tangents.ToArray();
        meshData.meshlets = meshletList.ToArray();
        meshData.meshletTriangles = meshletTrianglesUintList.ToArray();
        meshData.meshletVertices = meshletVerticesList.ToArray();
        return meshData;
    }
}