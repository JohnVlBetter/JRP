using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

public class ClusterPass
{
    static readonly ProfilingSampler samplerCluster = new("Cluster");

    static readonly ShaderTagId[] shaderTagIDs = {
        new("SRPDefaultUnlit"),
        new("JLit")
    };

    RendererListHandle list;

    void Render(RenderGraphContext context)
    {
        ComputeBuffer args = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        context.cmd.DrawProceduralIndirect(Matrix4x4.identity, null, 0, MeshTopology.Triangles, args, 0, null);
        context.renderContext.ExecuteCommandBuffer(context.cmd);
        context.cmd.Clear();
    }

    public static void Record(
        RenderGraph renderGraph,
        Camera camera,
        in CameraRendererTextures textures,
        in LightResources lightData)
    {
        using RenderGraphBuilder builder = renderGraph.AddRenderPass(
            samplerCluster.name, out ClusterPass pass, samplerCluster);

        builder.ReadWriteTexture(textures.colorAttachment);
        builder.ReadWriteTexture(textures.depthAttachment);

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
    }
}
