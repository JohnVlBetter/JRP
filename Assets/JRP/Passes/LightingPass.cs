using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

public partial class LightingPass
{
    static readonly ProfilingSampler sampler = new("Lighting");

    const int maxDirectionalLightCount = 4, maxOtherLightCount = 64;

    static readonly GlobalKeyword lightsPerObjectKeyword =
        GlobalKeyword.Create("_LIGHTS_PER_OBJECT");

    static readonly int
        directionalLightCountId = Shader.PropertyToID("_DirectionalLightCount"),
        directionalLightDataId = Shader.PropertyToID("_DirectionalLightData"),
        otherLightCountId = Shader.PropertyToID("_OtherLightCount"),
        otherLightDataId = Shader.PropertyToID("_OtherLightData");

    static readonly DirectionalLightData[] directionalLightData =
        new DirectionalLightData[maxDirectionalLightCount];

    static readonly OtherLightData[] otherLightData =
        new OtherLightData[maxOtherLightCount];

    ComputeBufferHandle directionalLightDataBuffer, otherLightDataBuffer;

    CullingResults cullingResults;

    readonly Shadows shadows = new();

    int directionalLightCount, otherLightCount;

    bool useLightsPerObject;

    void Setup(
        CullingResults cullingResults, ShadowSettings shadowSettings,
        bool useLightsPerObject, int renderingLayerMask)
    {
        this.cullingResults = cullingResults;
        this.useLightsPerObject = useLightsPerObject;
        shadows.Setup(cullingResults, shadowSettings);
        SetupLights(renderingLayerMask);
    }

    void SetupLights(int renderingLayerMask)
    {
        NativeArray<int> indexMap = useLightsPerObject ?
            cullingResults.GetLightIndexMap(Allocator.Temp) : default;
        NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
        int i;
        directionalLightCount = otherLightCount = 0;
        for (i = 0; i < visibleLights.Length; i++)
        {
            int newIndex = -1;
            VisibleLight visibleLight = visibleLights[i];
            Light light = visibleLight.light;
            if ((light.renderingLayerMask & renderingLayerMask) != 0)
            {
                switch (visibleLight.lightType)
                {
                    case LightType.Directional:
                        if (directionalLightCount < maxDirectionalLightCount)
                        {
                            directionalLightData[directionalLightCount++] =
                                new DirectionalLightData(
                                    ref visibleLight, light,
                                    shadows.ReserveDirectionalShadows(
                                        light, i));
                        }
                        break;
                    case LightType.Point:
                        if (otherLightCount < maxOtherLightCount)
                        {
                            newIndex = otherLightCount;
                            otherLightData[otherLightCount++] =
                                OtherLightData.CreatePointLight(
                                    ref visibleLight, light,
                                    shadows.ReserveOtherShadows(light, i));
                        }
                        break;
                    case LightType.Spot:
                        if (otherLightCount < maxOtherLightCount)
                        {
                            newIndex = otherLightCount;
                            otherLightData[otherLightCount++] =
                                OtherLightData.CreateSpotLight(
                                    ref visibleLight, light,
                                    shadows.ReserveOtherShadows(light, i));
                        }
                        break;
                }
            }
            if (useLightsPerObject)
            {
                indexMap[i] = newIndex;
            }
        }

        if (useLightsPerObject)
        {
            for (; i < indexMap.Length; i++)
            {
                indexMap[i] = -1;
            }
            cullingResults.SetLightIndexMap(indexMap);
            indexMap.Dispose();
        }
    }

    void Render(RenderGraphContext context)
    {
        CommandBuffer buffer = context.cmd;
        buffer.SetKeyword(lightsPerObjectKeyword, useLightsPerObject);
        buffer.SetGlobalInt(directionalLightCountId, directionalLightCount);
        buffer.SetBufferData(
            directionalLightDataBuffer, directionalLightData,
            0, 0, directionalLightCount);
        buffer.SetGlobalBuffer(
            directionalLightDataId, directionalLightDataBuffer);

        buffer.SetGlobalInt(otherLightCountId, otherLightCount);
        buffer.SetBufferData(
            otherLightDataBuffer, otherLightData, 0, 0, otherLightCount);
        buffer.SetGlobalBuffer(otherLightDataId, otherLightDataBuffer);

        shadows.Render(context);
        context.renderContext.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    public static LightResources Record(
        RenderGraph renderGraph,
        CullingResults cullingResults, ShadowSettings shadowSettings,
        bool useLightsPerObject, int renderingLayerMask)
    {
        using RenderGraphBuilder builder = renderGraph.AddRenderPass(
            sampler.name, out LightingPass pass, sampler);
        pass.Setup(cullingResults, shadowSettings,
            useLightsPerObject, renderingLayerMask);
        pass.directionalLightDataBuffer = builder.WriteComputeBuffer(
            renderGraph.CreateComputeBuffer(new ComputeBufferDesc
            {
                name = "Directional Light Data",
                count = maxDirectionalLightCount,
                stride = DirectionalLightData.stride
            }));
        pass.otherLightDataBuffer = builder.WriteComputeBuffer(
            renderGraph.CreateComputeBuffer(new ComputeBufferDesc
            {
                name = "Other Light Data",
                count = maxOtherLightCount,
                stride = OtherLightData.stride
            }));
        builder.SetRenderFunc<LightingPass>(
            static (pass, context) => pass.Render(context));
        builder.AllowPassCulling(false);
        return new LightResources(
            pass.directionalLightDataBuffer,
            pass.otherLightDataBuffer,
            pass.shadows.GetResources(renderGraph, builder));
    }
}
