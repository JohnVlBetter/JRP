using UnityEngine;
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditorForRenderPipeline(typeof(Light), typeof(JRPAsset))]
public class JLightEditor : LightEditor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (!settings.lightType.hasMultipleDifferentValues &&
            (LightType)settings.lightType.enumValueIndex == LightType.Spot)
        {
            settings.DrawInnerAndOuterSpotAngle();
            settings.ApplyModifiedProperties();
        }
    }
}