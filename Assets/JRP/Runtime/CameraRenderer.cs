using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRenderer{
    private ScriptableRenderContext context;
    private Camera camera;

    private const string bufferName = "Render Camera";
    private CommandBuffer commandBuffer = new CommandBuffer() { 
        name = bufferName,
    };

    private CullingResults cullingResults;

    private static ShaderTagId unlitShaderTagID = new ShaderTagId("SRPDefaultUnlit"),
        litShaderTagId = new ShaderTagId("CustomLit");

    private Lighting lighting = new Lighting();
    
    private PostFXStack postFXStack = new PostFXStack();
    
    static int frameBufferId = Shader.PropertyToID("_CameraFrameBuffer");

    public void Render(ScriptableRenderContext _context, Camera _camera, bool useDynamicBatching, 
        bool useGPUInstancing, bool useLightsPerObject, ShadowSettings _shadowSettings,
        PostFXSettings _postFXSettings) { 
        this.camera = _camera;
        this.context = _context;

        PrepareBuffer();
        //在剔除前绘制世界UI
        PrepareForSceneWindow();

        if (!Cull(_shadowSettings.maxDistance)) return;

        commandBuffer.BeginSample(sampleName);
        ExecuteBuffer();
        lighting.Setup(context, cullingResults, _shadowSettings, useLightsPerObject);
        postFXStack.Setup(context, camera, _postFXSettings);
        commandBuffer.EndSample(sampleName);

        SetupCameraProperties();

        DrawVisibleGeometry(useDynamicBatching, useGPUInstancing, useLightsPerObject);

        DrawUnsupportedShaders();

        DrawGizmosBeforeFX();
        if (postFXStack.IsActive) {
            postFXStack.Render(frameBufferId);
        }
        DrawGizmosAfterFX();
        
        Cleanup();

        Submit();
    }

    private void SetupCameraProperties()
    {
        context.SetupCameraProperties(camera);
        CameraClearFlags flags = camera.clearFlags;
        
        if (postFXStack.IsActive) {
            if (flags > CameraClearFlags.Color) {
                flags = CameraClearFlags.Color;
            }
            
            commandBuffer.GetTemporaryRT(
                frameBufferId, camera.pixelWidth, camera.pixelHeight,
                32, FilterMode.Bilinear, RenderTextureFormat.Default
            );
            commandBuffer.SetRenderTarget(
                frameBufferId,
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
            );
        }
        
        commandBuffer.ClearRenderTarget(
            flags <= CameraClearFlags.Depth,
            flags == CameraClearFlags.Color,
            flags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear);
        commandBuffer.BeginSample(sampleName);
        ExecuteBuffer();
    }
    
    void Cleanup () {
        lighting.Cleanup();
        if (postFXStack.IsActive) {
            commandBuffer.ReleaseTemporaryRT(frameBufferId);
        }
    }

    private void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstancing, bool useLightsPerObject) {
        PerObjectData lightsPerObjectFlags = useLightsPerObject ?
            PerObjectData.LightData | PerObjectData.LightIndices : PerObjectData.None;
        //先绘制不透明物体
        var sortingSettings = new SortingSettings(camera) { 
            criteria = SortingCriteria.CommonOpaque
        };
        var drawingSettings = new DrawingSettings(unlitShaderTagID, sortingSettings) {
            enableDynamicBatching = useDynamicBatching,
            enableInstancing = useGPUInstancing,
            perObjectData = PerObjectData .ReflectionProbes |PerObjectData.Lightmaps | PerObjectData.ShadowMask 
                            | PerObjectData.LightProbe | PerObjectData.OcclusionProbe |PerObjectData.LightProbeProxyVolume
                            | PerObjectData.OcclusionProbeProxyVolume| lightsPerObjectFlags
        };
        drawingSettings.SetShaderPassName(1, litShaderTagId);
        var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);

        //再绘制天空盒
        context.DrawSkybox(camera);

        //最后绘制透明物体
        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortingSettings;
        filteringSettings.renderQueueRange = RenderQueueRange.transparent;
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
    }

    private void Submit()
    {
        commandBuffer.EndSample(sampleName);
        ExecuteBuffer();
        context.Submit();
    }

    private void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(commandBuffer);
        commandBuffer.Clear();
    }

    private bool Cull(float maxShadowDistance) {
        if (camera.TryGetCullingParameters(out ScriptableCullingParameters scp)) {
            scp.shadowDistance = Mathf.Min(maxShadowDistance, camera.farClipPlane);
            cullingResults = context.Cull(ref scp);
            return true;
        }
        return false;
    }
}