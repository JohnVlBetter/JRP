using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/JRP Asset")]
public class JRPAsset : RenderPipelineAsset
{
    [SerializeField]
    private bool useDynamicBatching = true, useGPUInstancing = true, useSRPBatcher = true,
        useLightsPerObject = true;

    [SerializeField]
    ShadowSettings shadows = default;

    protected override RenderPipeline CreatePipeline()
    {
        return new JRP(useDynamicBatching, useGPUInstancing,
            useLightsPerObject, useSRPBatcher, shadows);
    }
}