using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class Lighting
{
	private const string bufferName = "Lighting";

	private CommandBuffer commandBuffer = new CommandBuffer{ name = bufferName };

	private const int maxDirLightCount = 4, maxOtherLightCount = 64;

	private static int dirLightCountId = Shader.PropertyToID("_DirectionalLightCount");
	private static int dirLightColorsId = Shader.PropertyToID("_DirectionalLightColors");
	private static int dirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections");
	private static int dirLightShadowDataId = Shader.PropertyToID("_DirectionalLightShadowData");

	private static int otherLightCountId = Shader.PropertyToID("_OtherLightCount");
	private static int otherLightColorsId = Shader.PropertyToID("_OtherLightColors");
	private static int otherLightPositionsId = Shader.PropertyToID("_OtherLightPositions");

	private static Vector4[] otherLightColors = new Vector4[maxOtherLightCount];
	private static Vector4[] otherLightPositions = new Vector4[maxOtherLightCount];

	private static int metallicId = Shader.PropertyToID("_Metallic");
	private static int smoothnessId = Shader.PropertyToID("_Smoothness");

	private static Vector4[] dirLightColors = new Vector4[maxDirLightCount];
	private static Vector4[] dirLightDirections = new Vector4[maxDirLightCount];
	private static Vector4[] dirLightShadowData = new Vector4[maxDirLightCount];

	private static float[] metallic = new float[1023];
	private static float[] smoothness = new float[1023];

	private CullingResults cullingResults;
	private Shadows shadows = new Shadows();

	public void Setup(ScriptableRenderContext _context, CullingResults _cullingResults, 
					  ShadowSettings _shadowSettings)
	{
		this.cullingResults = _cullingResults;
		commandBuffer.BeginSample(bufferName);
		shadows.Setup(_context, cullingResults, _shadowSettings);
		SetupLights();
		shadows.Render();
		commandBuffer.EndSample(bufferName);
		_context.ExecuteCommandBuffer(commandBuffer);
		commandBuffer.Clear();
	}
	public void Cleanup()
	{
		shadows.Cleanup();
	}

	void SetupLights() {
		NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
		int dirLightCount = 0, otherLightCount = 0;
		for (int i = 0; i < visibleLights.Length; i++)
		{
			VisibleLight visibleLight = visibleLights[i];
			switch (visibleLight.lightType) {
				case LightType.Directional:
					if (dirLightCount < maxDirLightCount) {
						SetupDirectionalLight(dirLightCount++, ref visibleLight);
					}
					break;
				case LightType.Point:
					if (otherLightCount < maxOtherLightCount) {
						SetupPointLight(otherLightCount++, ref visibleLight);
					}
					break;
			}
		}

		commandBuffer.SetGlobalInt(dirLightCountId, visibleLights.Length);
		if (dirLightCount > 0) {
			commandBuffer.SetGlobalVectorArray(dirLightColorsId, dirLightColors);
			commandBuffer.SetGlobalVectorArray(dirLightDirectionsId, dirLightDirections);
			commandBuffer.SetGlobalVectorArray(dirLightShadowDataId, dirLightShadowData);
		}

		commandBuffer.SetGlobalInt(otherLightCountId, otherLightCount);
		if (otherLightCount > 0) {
			commandBuffer.SetGlobalVectorArray(otherLightColorsId, otherLightColors);
			commandBuffer.SetGlobalVectorArray(otherLightPositionsId, otherLightPositions);
		}
	}

	void SetupPointLight (int index, ref VisibleLight visibleLight) {
		otherLightColors[index] = visibleLight.finalColor;
		Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);
		position.w = 1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f);
		otherLightPositions[index] = position;
	}
	
	void SetupDirectionalLight(int index, ref VisibleLight visibleLight)
	{
		dirLightColors[index] = visibleLight.finalColor;
		dirLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
		dirLightShadowData[index] = shadows.ReserveDirectionalShadows(visibleLight.light, index);
	}
}