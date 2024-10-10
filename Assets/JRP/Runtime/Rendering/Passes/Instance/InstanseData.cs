using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

[StructLayout(LayoutKind.Sequential)]
struct InstanseData
{
    public const int stride = 4 * 4 * 3;

    public Vector4 color, directionAndMask, shadowData;

    public InstanseData(
        ref VisibleLight visibleLight, Light light, Vector4 shadowData)
    {
        color = visibleLight.finalColor;
        directionAndMask = -visibleLight.localToWorldMatrix.GetColumn(2);
        directionAndMask.w = light.renderingLayerMask.ReinterpretAsFloat();
        this.shadowData = shadowData;
    }
}