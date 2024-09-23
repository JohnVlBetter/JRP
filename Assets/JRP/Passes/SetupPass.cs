using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

public class SetupPass
{
    static readonly ProfilingSampler sampler = new("Setup");
    JRenderer renderer;

    void Render(RenderGraphContext context) => renderer.Setup();

    public static void Record(
        RenderGraph renderGraph, JRenderer renderer)
    {
        using RenderGraphBuilder builder = renderGraph.AddRenderPass(sampler.name, out SetupPass pass, sampler);
        pass.renderer = renderer;
        builder.SetRenderFunc<SetupPass>((pass, context) => pass.Render(context));
    }
}