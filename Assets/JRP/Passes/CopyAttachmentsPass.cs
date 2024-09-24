using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

public class CopyAttachmentsPass
{
    static readonly ProfilingSampler sampler = new("Copy Attachments");

    JRenderer renderer;

    void Render(RenderGraphContext context) => renderer.CopyAttachments();

    public static void Record(RenderGraph renderGraph, JRenderer renderer)
    {
        using RenderGraphBuilder builder = renderGraph.AddRenderPass(
            sampler.name, out CopyAttachmentsPass pass, sampler);
        pass.renderer = renderer;
        builder.SetRenderFunc<CopyAttachmentsPass>(
            (pass, context) => pass.Render(context));
    }
}