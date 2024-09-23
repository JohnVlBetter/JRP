using System.Diagnostics;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

public class UnsupportedShadersPass
{
#if UNITY_EDITOR
	JRenderer renderer;

	void Render(RenderGraphContext context) => renderer.DrawUnsupportedShaders();
#endif

    [Conditional("UNITY_EDITOR")]
    public static void Record(RenderGraph renderGraph, JRenderer renderer)
    {
#if UNITY_EDITOR
		using RenderGraphBuilder builder = renderGraph.AddRenderPass(
			"Unsupported Shaders", out UnsupportedShadersPass pass);
		pass.renderer = renderer;
		builder.SetRenderFunc<UnsupportedShadersPass>(
			(pass, context) => pass.Render(context));
#endif
    }
}