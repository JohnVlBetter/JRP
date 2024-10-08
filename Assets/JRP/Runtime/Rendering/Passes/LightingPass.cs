using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

using static Unity.Mathematics.math;

public partial class LightingPass
{
    static readonly ProfilingSampler sampler = new("Lighting");

    const int
        maxDirectionalLightCount = 4,
        maxOtherLightCount = 128;

    static readonly GlobalKeyword lightsPerObjectKeyword =
        GlobalKeyword.Create("_LIGHTS_PER_OBJECT");

    static readonly int
        directionalLightCountId = Shader.PropertyToID("_DirectionalLightCount"),
        directionalLightDataId = Shader.PropertyToID("_DirectionalLightData"),
        otherLightCountId = Shader.PropertyToID("_OtherLightCount"),
        otherLightDataId = Shader.PropertyToID("_OtherLightData"),
        tilesId = Shader.PropertyToID("_ForwardPlusTiles"),
        tileSettingsId = Shader.PropertyToID("_ForwardPlusTileSettings");

    static readonly DirectionalLightData[] directionalLightData =
        new DirectionalLightData[maxDirectionalLightCount];

    static readonly OtherLightData[] otherLightData =
        new OtherLightData[maxOtherLightCount];

    ComputeBufferHandle
        directionalLightDataBuffer, otherLightDataBuffer, tilesBuffer;

    CullingResults cullingResults;

    readonly Shadows shadows = new();

    int directionalLightCount, otherLightCount;

    bool useLightsPerObject;

    NativeArray<float4> lightBounds;

    NativeArray<int> tileData;

    JobHandle forwardPlusJobHandle;

    Vector2 screenUVToTileCoordinates;

    Vector2Int tileCount;

    int maxLightsPerTile, tileDataSize, maxTileDataSize;

    int TileCount => tileCount.x * tileCount.y;

    void Setup(
        CullingResults cullingResults,
        Vector2Int attachmentSize,
        ForwardPlusSettings forwardPlusSettings,
        ShadowSettings shadowSettings,
        bool useLightsPerObject,
        int renderingLayerMask)
    {
        this.cullingResults = cullingResults;
        this.useLightsPerObject = useLightsPerObject;
        shadows.Setup(cullingResults, shadowSettings);

        if (!useLightsPerObject)
        {
            maxLightsPerTile = forwardPlusSettings.maxLightsPerTile <= 0 ?
                31 : forwardPlusSettings.maxLightsPerTile;
            maxTileDataSize = maxLightsPerTile + 1;
            lightBounds = new NativeArray<float4>(
                maxOtherLightCount, Allocator.TempJob,
                NativeArrayOptions.UninitializedMemory);
            float tileScreenPixelSize = forwardPlusSettings.tileSize <= 0 ?
                64f : (float)forwardPlusSettings.tileSize;
            screenUVToTileCoordinates.x =
                attachmentSize.x / tileScreenPixelSize;
            screenUVToTileCoordinates.y =
                attachmentSize.y / tileScreenPixelSize;
            tileCount.x = Mathf.CeilToInt(screenUVToTileCoordinates.x);
            tileCount.y = Mathf.CeilToInt(screenUVToTileCoordinates.y);
        }

        SetupLights(renderingLayerMask);
    }

    void SetupForwardPlus(int lightIndex, ref VisibleLight visibleLight)
    {
        if (!useLightsPerObject)
        {
            Rect r = visibleLight.screenRect;
            lightBounds[lightIndex] = float4(r.xMin, r.yMin, r.xMax, r.yMax);
        }
    }

    void SetupLights(int renderingLayerMask)
    {
        NativeArray<int> indexMap = useLightsPerObject ?
            cullingResults.GetLightIndexMap(Allocator.Temp) : default;
        NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
        int requiredMaxLightsPerTile = Mathf.Min(
            maxLightsPerTile, visibleLights.Length);
        tileDataSize = requiredMaxLightsPerTile + 1;
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
                            SetupForwardPlus(otherLightCount, ref visibleLight);
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
                            SetupForwardPlus(otherLightCount, ref visibleLight);
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
        else
        {
            tileData = new NativeArray<int>(
                TileCount * tileDataSize, Allocator.TempJob);
            /*forwardPlusJobHandle = new ForwardPlusTilesJob
            {
                lightBounds = lightBounds,
                tileData = tileData,
                otherLightCount = otherLightCount,
                tileScreenUVSize = float2(
                    1f / screenUVToTileCoordinates.x,
                    1f / screenUVToTileCoordinates.y),
                maxLightsPerTile = requiredMaxLightsPerTile,
                tilesPerRow = tileCount.x,
                tileDataSize = tileDataSize
            }.ScheduleParallel(TileCount, tileCount.x, default);*/
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

        if (useLightsPerObject)
        {
            context.renderContext.ExecuteCommandBuffer(buffer);
            buffer.Clear();
            return;
        }

        forwardPlusJobHandle.Complete();
        buffer.SetBufferData(
            tilesBuffer, tileData, 0, 0, tileData.Length);
        buffer.SetGlobalBuffer(tilesId, tilesBuffer);
        buffer.SetGlobalVector(tileSettingsId, new Vector4(
            screenUVToTileCoordinates.x, screenUVToTileCoordinates.y,
            tileCount.x.ReinterpretAsFloat(),
            tileDataSize.ReinterpretAsFloat()));
        context.renderContext.ExecuteCommandBuffer(buffer);
        buffer.Clear();
        lightBounds.Dispose();
        tileData.Dispose();
    }

    public static LightResources Record(
        RenderGraph renderGraph,
        CullingResults cullingResults,
        Vector2Int attachmentSize,
        ForwardPlusSettings forwardPlusSettings,
        ShadowSettings shadowSettings,
        bool useLightsPerObject,
        int renderingLayerMask)
    {
        using RenderGraphBuilder builder = renderGraph.AddRenderPass(
            sampler.name, out LightingPass pass, sampler);
        pass.Setup(cullingResults, attachmentSize,
            forwardPlusSettings, shadowSettings,
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
        if (!useLightsPerObject)
        {
            pass.tilesBuffer = builder.WriteComputeBuffer(
                renderGraph.CreateComputeBuffer(new ComputeBufferDesc
                {
                    name = "Forward+ Tiles",
                    count = pass.TileCount * pass.maxTileDataSize,
                    stride = 4
                }));
        }
        builder.SetRenderFunc<LightingPass>(
            static (pass, context) => pass.Render(context));
        builder.AllowPassCulling(false);
        return new LightResources(
            pass.directionalLightDataBuffer,
            pass.otherLightDataBuffer,
            pass.tilesBuffer,
            pass.shadows.GetResources(renderGraph, builder));
    }
}
