using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRenderer{
    private partial void DrawUnsupportedShaders();

    private partial void DrawGizmos();

    private partial void PrepareForSceneWindow();

    private partial void PrepareBuffer();

#if UNITY_EDITOR
    private static ShaderTagId unlitShaderTagID = new ShaderTagId("SRPDefaultUnlit");
    private static ShaderTagId[] unsupportedShaderTagIds = {
        new ShaderTagId("Always"),
        new ShaderTagId("ForwardBase"),
        new ShaderTagId("PrepassBase"),
        new ShaderTagId("Vertex"),
        new ShaderTagId("VertexLMRGBM"),
        new ShaderTagId("VertexLM")
    };
    private static Material errorMat;
    private string sampleName { set; get; }

    private partial void DrawUnsupportedShaders()
    {
        if (errorMat == null) {
            errorMat = new Material(Shader.Find("Hidden/InternalErrorShader"));
        }

        var drawingSettings = new DrawingSettings(
            unsupportedShaderTagIds[0],
            new SortingSettings(camera))
        { 
            overrideMaterial = errorMat,
        };
        for (int i = 1; i < unsupportedShaderTagIds.Length; i++) {
            drawingSettings.SetShaderPassName(i, unsupportedShaderTagIds[i]);
        }
        var filteringSettings = FilteringSettings.defaultValue;
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
    }

    private partial void DrawGizmos()
    {
        if (Handles.ShouldRenderGizmos()) {
            context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
            context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
        }
    }

    private partial void PrepareForSceneWindow()
    {
        if (camera.cameraType == CameraType.SceneView)
        {
            ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
        }
    }
    
    private partial void PrepareBuffer()
    {
        UnityEngine.Profiling.Profiler.BeginSample("Editor Only");
        sampleName = commandBuffer.name = camera.name;
        UnityEngine.Profiling.Profiler.EndSample();
    }

#else
    private string sampleName => bufferName;

#endif
}