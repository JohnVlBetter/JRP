using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class JRP : RenderPipeline{
    private CameraRenderer cameraRenderer = new CameraRenderer();

    private bool useDynamicBatching, useGPUInstancing;

    public JRP(bool _useDynamicBatching, bool _useGPUInstancing, bool _useSRPBatcher)
    {
        this.useDynamicBatching = _useDynamicBatching;
        this.useGPUInstancing = _useGPUInstancing;
        GraphicsSettings.useScriptableRenderPipelineBatching = _useSRPBatcher;
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras){
        foreach (var camera in cameras) {
            cameraRenderer.Render(context, camera, useDynamicBatching, useGPUInstancing);
        }
    }
}