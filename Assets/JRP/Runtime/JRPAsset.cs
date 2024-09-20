using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/JRP")]
public class JRPAsset : RenderPipelineAsset
{
    [SerializeField]
    bool allowHDR = true;

    [SerializeField]
    bool
        useDynamicBatching = true,
        useGPUInstancing = true,
        useSRPBatcher = true,
        useLightsPerObject = true;

    [SerializeField]
    ShadowSettings shadows = default;

    [SerializeField]
    PostFXSettings postFXSettings = default;

    protected override RenderPipeline CreatePipeline()
    {
        return new JRenderPipeline(
            allowHDR, useDynamicBatching, useGPUInstancing, useSRPBatcher,
            useLightsPerObject, shadows, postFXSettings
        );
    }
}