using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class JRenderPipeline : RenderPipeline
{
    JRenderer renderer = new JRenderer();
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
    }

    protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
    {
        for (int i = 0; i < cameras.Count; i++)
        {
            renderer.Render(context, cameras[i]);
        }
    }
}