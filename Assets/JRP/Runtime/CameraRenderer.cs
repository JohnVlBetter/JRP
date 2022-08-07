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

    public void Render(ScriptableRenderContext _context, Camera _camera,
        bool useDynamicBatching, bool useGPUInstancing) { 
        this.camera = _camera;
        this.context = _context;

        PrepareBuffer();
        //在剔除前绘制世界UI
        PrepareForSceneWindow();
        if (!Cull()) return;

        SetupCameraProperties();
        DrawVisibleGeometry(useDynamicBatching, useGPUInstancing);
        DrawUnsupportedShaders();
        DrawGizmos();
        Submit();
    }

    private void SetupCameraProperties()
    {
        context.SetupCameraProperties(camera);
        CameraClearFlags flags = camera.clearFlags;
        commandBuffer.ClearRenderTarget(
            flags <= CameraClearFlags.Depth,
            flags == CameraClearFlags.Color,
            flags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear);
        commandBuffer.BeginSample(sampleName);
        ExecuteBuffer();
    }

    private void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstancing) {
        //先绘制不透明物体
        var sortingSettings = new SortingSettings(camera) { 
            criteria = SortingCriteria.CommonOpaque
        };
        var drawingSettings = new DrawingSettings(unlitShaderTagID, sortingSettings) {
            enableDynamicBatching = useDynamicBatching,
            enableInstancing = useGPUInstancing
        };
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

    private bool Cull() {
        if (camera.TryGetCullingParameters(out ScriptableCullingParameters scp)) {
            cullingResults = context.Cull(ref scp);
            return true;
        }
        return false;
    }
}