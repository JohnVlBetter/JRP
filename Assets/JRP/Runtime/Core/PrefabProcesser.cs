using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

public class PrefabProcesser : MonoBehaviour
{
    [MenuItem("Tools/Process Prefabs")]
    public static void ProcessPrefabs()
    {
        string savePath = "Assets/Res/JMesh";
        if (!AssetDatabase.IsValidFolder(savePath))
        {
            AssetDatabase.CreateFolder("Assets/Res", "JMesh");
        }
        string path = "Assets/Res/Prefabs";
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new string[] { path });
        foreach (string guid in guids)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Debug.Log($"Processing prefab: {prefab.name}");
            ProcessPrefab(prefab);
            EditorUtility.SetDirty(prefab);
        }
        AssetDatabase.SaveAssets();
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
        //Debug.Log($"MeshData: {meshData.meshlets.Length}");
        var jo = prefab.GetComponent<JObject>();
        if (jo == null)
        {
            jo = prefab.AddComponent<JObject>();
        }
        jo.jMeshID = GetMeshHashCode(mesh);
        //Debug.Log($"MeshID: {jo.jMeshID}");
        var meshData = Meshopt.BuildMeshlets(mesh);
        //Debug.Log($"MeshData: {meshData.meshlets.Length}");
        //Debug.Log($"MeshData: {meshData.vertices.Length}");
        //Debug.Log($"MeshData: {meshData.normals.Length}");
        //Debug.Log($"MeshData: {meshData.tangents.Length}");
        //Debug.Log($"MeshData: {meshData.meshletTriangles.Length}");
        //Debug.Log($"MeshData: {meshData.meshletVertices.Length}");
        //Debug.Log($"MeshData: {meshData.normals[0]}");
        //Debug.Log($"MeshData: {meshData.vertices[1]}");
        //Debug.Log($"MeshData: {meshData.meshletTriangles[2]}");
        //Debug.Log($"--------------------------------------------");
        SaveMeshDataToFile(meshData, jo.jMeshID);
        //var data = LoadMeshDataFromFile(jo.jMeshID);
        //Debug.Log($"MeshData: {data.meshlets.Length}");
        //Debug.Log($"MeshData: {data.vertices.Length}");
        //Debug.Log($"MeshData: {data.normals.Length}");
        //Debug.Log($"MeshData: {data.tangents.Length}");
        //Debug.Log($"MeshData: {data.meshletTriangles.Length}");
        //Debug.Log($"MeshData: {data.meshletVertices.Length}");
        //Debug.Log($"MeshData: {data.normals[0]}");
        //Debug.Log($"MeshData: {data.vertices[1]}");
        //Debug.Log($"MeshData: {data.meshletTriangles[2]}");
    }

    private static int GetMeshHashCode(Mesh mesh)
    {
        int hash = mesh.name.GetHashCode();
        for (int idx = 0; idx < 5; ++idx)
        {
            hash ^= mesh.vertices[idx].GetHashCode();
        }
        return hash;
    }

    private static void SaveMeshDataToFile(Meshopt.MeshData meshData, int jMeshID)
    {
        string filePath = $"{Application.dataPath}/Res/JMesh/{jMeshID.ToString()}.jmesh";
        try
        {
            using FileStream fs = new FileStream(filePath, FileMode.Create);
            using BinaryWriter bw = new BinaryWriter(fs);

            var bytes = StructToBytes(meshData.meshlets);
            bw.Write(bytes.Length);
            bw.Write(bytes);

            bytes = StructToBytes(meshData.vertices);
            bw.Write(bytes.Length);
            bw.Write(bytes);

            bytes = StructToBytes(meshData.normals);
            bw.Write(bytes.Length);
            bw.Write(bytes);

            bytes = StructToBytes(meshData.tangents);
            bw.Write(bytes.Length);
            bw.Write(bytes);

            bytes = StructToBytes(meshData.meshletTriangles);
            bw.Write(bytes.Length);
            bw.Write(bytes);

            bytes = StructToBytes(meshData.meshletVertices);
            bw.Write(bytes.Length);
            bw.Write(bytes);

            bw.Close();
            fs.Close();
        }
        catch (IOException e)
        {
            Debug.LogError(e.Message);
        }
    }

    public static Meshopt.MeshData LoadMeshDataFromFile(int jMeshID)
    {
        string filePath = $"{Application.dataPath}/Res/JMesh/{jMeshID.ToString()}.jmesh";
        Meshopt.MeshData data = new Meshopt.MeshData();
        try
        {
            using FileStream fs = new FileStream(filePath, FileMode.Open);
            using BinaryReader br = new BinaryReader(fs);
            data.meshlets = BytesToStructArray<Meshopt.Meshlet>(br.ReadBytes(br.ReadInt32()));
            data.vertices = BytesToStructArray<Vector3>(br.ReadBytes(br.ReadInt32()));
            data.normals = BytesToStructArray<Vector3>(br.ReadBytes(br.ReadInt32()));
            data.tangents = BytesToStructArray<Vector4>(br.ReadBytes(br.ReadInt32()));
            data.meshletTriangles = BytesToStructArray<uint>(br.ReadBytes(br.ReadInt32()));
            data.meshletVertices = BytesToStructArray<uint>(br.ReadBytes(br.ReadInt32()));
            br.Close();
            fs.Close();
        }
        catch (IOException e)
        {
            Debug.LogError(e.Message);
        }
        return data;
    }

    private static byte[] StructToBytes<T>(T[] list) where T : struct
    {
        T t = list[0];
        Int32 size = Marshal.SizeOf(t);
        IntPtr buffer = Marshal.AllocHGlobal(size);
        byte[] bytes = new byte[size * list.Length];
        try
        {
            int ptr = 0;
            foreach (T t2 in list)
            {
                Marshal.StructureToPtr(t2, buffer, false);
                Marshal.Copy(buffer, bytes, ptr, size);
                ptr += size;
            }
            return bytes;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    public static T[] BytesToStructArray<T>(byte[] bytes) where T : struct
    {
        Int32 size = Marshal.SizeOf(typeof(T));
        IntPtr buffer = Marshal.AllocHGlobal(size);
        int count = bytes.Length / size;
        T[] array = new T[count];
        try
        {
            for (int i = 0; i < count; i++)
            {
                Marshal.Copy(bytes, i * size, buffer, size);
                array[i] = (T)Marshal.PtrToStructure(buffer, typeof(T));
            }
            return array;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }
}