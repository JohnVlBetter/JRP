using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class JRP : RenderPipeline{
    private CameraRenderer cameraRenderer = new CameraRenderer();

    private bool useDynamicBatching, useGPUInstancing, useLightsPerObject;
    private ShadowSettings shadowSettings;

    private PostFXSettings postFXSettings;
    
    public JRP(bool _useDynamicBatching, bool _useGPUInstancing, 
               bool _useSRPBatcher, bool useLightsPerObject,
               ShadowSettings _shadowSettings, PostFXSettings _postFXSettings)
    {
        this.useDynamicBatching = _useDynamicBatching;
        this.useGPUInstancing = _useGPUInstancing;
        this.useLightsPerObject = useLightsPerObject;
        this.shadowSettings = _shadowSettings;
        this.postFXSettings = _postFXSettings;
        GraphicsSettings.useScriptableRenderPipelineBatching = _useSRPBatcher;
        GraphicsSettings.lightsUseLinearIntensity = true;
        InitializeForEditor();
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras){
        foreach (var camera in cameras) {
            cameraRenderer.Render(context, camera, useDynamicBatching, useGPUInstancing,useGPUInstancing,
                shadowSettings, postFXSettings);
        }
    }
}