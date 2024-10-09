using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

public class ClusterPass
{
    static readonly ProfilingSampler
        samplerOpaque = new("Opaque Geometry"),
        samplerTransparent = new("Transparent Geometry");

    static readonly ShaderTagId[] shaderTagIDs = {
        new("SRPDefaultUnlit"),
        new("JLit")
    };

    RendererListHandle list;

    /*void Render(RenderGraphContext context)
    {
        context.cmd.DrawMeshInstancedIndirect();
        context.cmd.DrawRendererList(list);
        context.renderContext.ExecuteCommandBuffer(context.cmd);
        context.cmd.Clear();
    }

    public static void Record(
        RenderGraph renderGraph,
        Camera camera,
        CullingResults cullingResults,
        bool useLightsPerObject,
        int renderingLayerMask,
        bool opaque,
        in CameraRendererTextures textures,
        in LightResources lightData)
    {
        ProfilingSampler sampler = opaque ? samplerOpaque : samplerTransparent;

        using RenderGraphBuilder builder = renderGraph.AddRenderPass(
            sampler.name, out ClusterPass pass, sampler);

        pass.list = builder.UseRendererList(renderGraph.CreateRendererList(
            new RendererListDesc(shaderTagIDs, cullingResults, camera)
            {
                sortingCriteria = opaque ?
                    SortingCriteria.CommonOpaque :
                    SortingCriteria.CommonTransparent,
                rendererConfiguration =
                    PerObjectData.ReflectionProbes |
                    PerObjectData.Lightmaps |
                    PerObjectData.ShadowMask |
                    PerObjectData.LightProbe |
                    PerObjectData.OcclusionProbe |
                    PerObjectData.LightProbeProxyVolume |
                    PerObjectData.OcclusionProbeProxyVolume |
                    (useLightsPerObject ?
                        PerObjectData.LightData | PerObjectData.LightIndices :
                        PerObjectData.None),
                renderQueueRange = opaque ?
                    RenderQueueRange.opaque : RenderQueueRange.transparent,
                renderingLayerMask = (uint)renderingLayerMask
            }));

        builder.ReadWriteTexture(textures.colorAttachment);
        builder.ReadWriteTexture(textures.depthAttachment);
        if (!opaque)
        {
            if (textures.colorCopy.IsValid())
            {
                builder.ReadTexture(textures.colorCopy);
            }
            if (textures.depthCopy.IsValid())
            {
                builder.ReadTexture(textures.depthCopy);
            }
        }

        builder.ReadComputeBuffer(lightData.directionalLightDataBuffer);
        builder.ReadComputeBuffer(lightData.otherLightDataBuffer);
        if (lightData.tilesBuffer.IsValid())
        {
            builder.ReadComputeBuffer(lightData.tilesBuffer);
        }
        builder.ReadTexture(lightData.shadowResources.directionalAtlas);
        builder.ReadTexture(lightData.shadowResources.otherAtlas);
        builder.ReadComputeBuffer(
            lightData.shadowResources.directionalShadowCascadesBuffer);
        builder.ReadComputeBuffer(
            lightData.shadowResources.directionalShadowMatricesBuffer);
        builder.ReadComputeBuffer(
            lightData.shadowResources.otherShadowDataBuffer);

        builder.SetRenderFunc<ClusterPass>(
            static (pass, context) => pass.Render(context));
    }*/

    public static Mesh CreateMesh64Triangles()
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[128];
        int[] triangles = new int[64 * 3];

        for (int i = 0; i < 128; i++)
        {
            vertices[i] = new Vector3(
            Mathf.Cos(2 * Mathf.PI * i / 128),
            Mathf.Sin(2 * Mathf.PI * i / 128),
            0);
        }

        for (int i = 0; i < 64; i++)
        {
            triangles[i * 3] = i * 2;
            triangles[i * 3 + 1] = (i * 2 + 1) % 128;
            triangles[i * 3 + 2] = (i * 2 + 2) % 128;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }
}
