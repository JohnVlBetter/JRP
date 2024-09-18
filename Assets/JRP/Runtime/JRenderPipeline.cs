using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class JRenderPipeline : RenderPipeline
{
    JRenderer renderer = new JRenderer();

    bool useDynamicBatching, useGPUInstancing, useLightsPerObject;

    ShadowSettings shadowSettings;

    public JRenderPipeline(
        bool useDynamicBatching, bool useGPUInstancing, bool useSRPBatcher,
        bool useLightsPerObject, ShadowSettings shadowSettings
    )
    {
        this.shadowSettings = shadowSettings;
        this.useDynamicBatching = useDynamicBatching;
        this.useGPUInstancing = useGPUInstancing;
        this.useLightsPerObject = useLightsPerObject;
        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
        GraphicsSettings.lightsUseLinearIntensity = true;
        InitializeForEditor();
    }

    protected override void Render(
        ScriptableRenderContext context, Camera[] cameras
    )
    { }

    protected override void Render(
        ScriptableRenderContext context, List<Camera> cameras
    )
    {
        for (int i = 0; i < cameras.Count; i++)
        {
            renderer.Render(
                context, cameras[i],
                useDynamicBatching, useGPUInstancing, useLightsPerObject,
                shadowSettings
            );
        }
    }
}