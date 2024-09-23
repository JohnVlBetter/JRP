using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

public class PostFXPass
{
    PostFXStack postFXStack;

    void Render(RenderGraphContext context) =>
        postFXStack.Render(JRenderer.colorAttachmentId);

    public static void Record(RenderGraph renderGraph, PostFXStack postFXStack)
    {
        using RenderGraphBuilder builder =
            renderGraph.AddRenderPass("Post FX", out PostFXPass pass);
        pass.postFXStack = postFXStack;
        builder.SetRenderFunc<PostFXPass>((pass, context) => pass.Render(context));
    }
}