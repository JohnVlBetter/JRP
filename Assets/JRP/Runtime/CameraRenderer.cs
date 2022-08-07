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

    public void Render(ScriptableRenderContext _context, Camera _camera) { 
        this.camera = _camera;
        this.context = _context;

        if (!Cull()) return;

        SetupCameraProperties();
        DrawVisibleGeometry();
        DrawUnsupportedShaders();
        Submit();
    }

    private void SetupCameraProperties()
    {
        context.SetupCameraProperties(camera);
        commandBuffer.ClearRenderTarget(true, true, Color.clear);
        commandBuffer.BeginSample(bufferName);
        ExecuteBuffer();
    }

    private partial void DrawUnsupportedShaders();

    private void DrawVisibleGeometry() {
        //先绘制不透明物体
        var sortingSettings = new SortingSettings(camera) { 
            criteria = SortingCriteria.CommonOpaque
        };
        var drawingSettings = new DrawingSettings(unlitShaderTagID, sortingSettings);
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
        commandBuffer.EndSample(bufferName);
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