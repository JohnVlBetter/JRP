using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/JRP")]
public class JRPAsset : RenderPipelineAsset
{
    [SerializeField]
    bool useDynamicBatching = false, useGPUInstancing = true,
        useSRPBatcher = true, useLightsPerObject = true;

    [SerializeField]
    ShadowSettings shadows = default;

    protected override RenderPipeline CreatePipeline()
    {
        return new JRenderPipeline(useDynamicBatching, useGPUInstancing,
            useSRPBatcher, useLightsPerObject, shadows);
    }
}