using System.Runtime.InteropServices;
using UnityEngine;

partial class Shadows
{
    [StructLayout(LayoutKind.Sequential)]
    struct DirectionalShadowCascade
    {
        public const int stride = 4 * 4 * 2;

        public Vector4 cullingSphere, data;

        public DirectionalShadowCascade(
            Vector4 cullingSphere,
            float tileSize,
            ShadowSettings.FilterMode filterMode)
        {
            float texelSize = 2f * cullingSphere.w / tileSize;
            float filterSize = texelSize * ((float)filterMode + 1f);
            cullingSphere.w -= filterSize;
            cullingSphere.w *= cullingSphere.w;
            this.cullingSphere = cullingSphere;
            data = new Vector4(1f / cullingSphere.w, filterSize * 1.4142136f);
        }
    }
}
