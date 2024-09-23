using System.Diagnostics;
using UnityEditor;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

public class GizmosPass
{
#if UNITY_EDITOR
    JRenderer renderer;

    void Render(RenderGraphContext context)
    {
        if (renderer.useIntermediateBuffer)
        {
            renderer.Draw(
                JRenderer.depthAttachmentId, BuiltinRenderTextureType.CameraTarget,
                true);
            renderer.ExecuteBuffer();
        }
        context.renderContext.DrawGizmos(renderer.camera, GizmoSubset.PreImageEffects);
        context.renderContext.DrawGizmos(renderer.camera, GizmoSubset.PostImageEffects);
    }
#endif

    [Conditional("UNITY_EDITOR")]
    public static void Record(RenderGraph renderGraph, JRenderer renderer)
    {
#if UNITY_EDITOR
        if (Handles.ShouldRenderGizmos())
        {
            using RenderGraphBuilder builder = renderGraph.AddRenderPass("Gizmos", out GizmosPass pass);
            pass.renderer = renderer;
            builder.SetRenderFunc<GizmosPass>((pass, context) => pass.Render(context));
        }
#endif
    }
}