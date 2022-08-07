using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/JRP Asset")]
public class JRPAsset : RenderPipelineAsset
{
    [SerializeField]
    private bool useDynamicBatching = true, useGPUInstancing = true, useSRPBatcher = true;

    protected override RenderPipeline CreatePipeline()
    {
        return new JRP(useDynamicBatching, useGPUInstancing, useSRPBatcher);
    }

}