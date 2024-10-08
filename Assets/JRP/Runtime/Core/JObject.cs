using UnityEngine;

public class JObject : MonoBehaviour
{
    public JMaterial material;
    public JMesh mesh;

    public int jMeshID;

#if UNITY_EDITOR
    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;
#endif

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        mesh = new JMesh(jMeshID);
    }
}