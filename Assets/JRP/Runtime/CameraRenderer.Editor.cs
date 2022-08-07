using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRenderer{
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
#endif
}