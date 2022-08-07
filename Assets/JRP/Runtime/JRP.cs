using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class JRP : RenderPipeline{
    private CameraRenderer cameraRenderer = new CameraRenderer();

    protected override void Render(ScriptableRenderContext context, Camera[] cameras){
        foreach (var camera in cameras) {
            cameraRenderer.Render(context, camera);
        }
    }
}