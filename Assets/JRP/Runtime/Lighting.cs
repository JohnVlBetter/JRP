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
	private static int otherLightShadowDataId = Shader.PropertyToID("_OtherLightShadowData");

	private static int otherLightCountId = Shader.PropertyToID("_OtherLightCount");
	private static int otherLightColorsId = Shader.PropertyToID("_OtherLightColors");
	private static int otherLightPositionsId = Shader.PropertyToID("_OtherLightPositions");
	private static int otherLightDirectionsId = Shader.PropertyToID("_OtherLightDirections");
	private static int otherLightSpotAnglesId = Shader.PropertyToID("_OtherLightSpotAngles");

	private static Vector4[] otherLightColors = new Vector4[maxOtherLightCount];
	private static Vector4[] otherLightPositions = new Vector4[maxOtherLightCount];
	private static Vector4[] otherLightDirections = new Vector4[maxOtherLightCount];
	private static Vector4[] otherLightSpotAngles = new Vector4[maxOtherLightCount];
	private static Vector4[] otherLightShadowData = new Vector4[maxOtherLightCount];

	private static int metallicId = Shader.PropertyToID("_Metallic");
	private static int smoothnessId = Shader.PropertyToID("_Smoothness");

	private static Vector4[] dirLightColors = new Vector4[maxDirLightCount];
	private static Vector4[] dirLightDirections = new Vector4[maxDirLightCount];
	private static Vector4[] dirLightShadowData = new Vector4[maxDirLightCount];

	private static float[] metallic = new float[1023];
	private static float[] smoothness = new float[1023];

	private CullingResults cullingResults;
	private Shadows shadows = new Shadows();
	
	static string lightsPerObjectKeyword = "_LIGHTS_PER_OBJECT";

	public void Setup(ScriptableRenderContext _context, CullingResults _cullingResults, 
					  ShadowSettings _shadowSettings, bool useLightsPerObject)
	{
		this.cullingResults = _cullingResults;
		commandBuffer.BeginSample(bufferName);
		shadows.Setup(_context, cullingResults, _shadowSettings);
		SetupLights(useLightsPerObject);
		shadows.Render();
		commandBuffer.EndSample(bufferName);
		_context.ExecuteCommandBuffer(commandBuffer);
		commandBuffer.Clear();
	}
	public void Cleanup()
	{
		shadows.Cleanup();
	}

	void SetupLights(bool useLightsPerObject) {
		NativeArray<int> indexMap = useLightsPerObject ? cullingResults.GetLightIndexMap(Allocator.Temp) : default;
		NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
		int dirLightCount = 0, otherLightCount = 0;
		int i;
		for (i = 0; i < visibleLights.Length; i++)
		{
			int newIndex = -1;
			VisibleLight visibleLight = visibleLights[i];
			switch (visibleLight.lightType) {
				case LightType.Directional:
					if (dirLightCount < maxDirLightCount) {
						SetupDirectionalLight(dirLightCount++, i, ref visibleLight);
					}
					break;
				case LightType.Point:
					if (otherLightCount < maxOtherLightCount) {
						newIndex = otherLightCount;
						SetupPointLight(otherLightCount++, i, ref visibleLight);
					}
					break;
				case LightType.Spot:
					if (otherLightCount < maxOtherLightCount) {
						newIndex = otherLightCount;
						SetupSpotLight(otherLightCount++, i, ref visibleLight);
					}
					break;
			}
			if (useLightsPerObject) {
				indexMap[i] = newIndex;
			}
		}
		if (useLightsPerObject) {
			for (; i < indexMap.Length; i++) {
				indexMap[i] = -1;
			}
			cullingResults.SetLightIndexMap(indexMap);
			indexMap.Dispose();
			Shader.EnableKeyword(lightsPerObjectKeyword);
		}
		else {
			Shader.DisableKeyword(lightsPerObjectKeyword);
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
			commandBuffer.SetGlobalVectorArray(otherLightDirectionsId, otherLightDirections);
			commandBuffer.SetGlobalVectorArray(otherLightSpotAnglesId, otherLightSpotAngles);
			commandBuffer.SetGlobalVectorArray(otherLightShadowDataId, otherLightShadowData);
		}
	}
	
	void SetupSpotLight (int index, int visibleIndex, ref VisibleLight visibleLight) {
		otherLightColors[index] = visibleLight.finalColor;
		Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);
		position.w = 1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f);
		otherLightPositions[index] = position;
		otherLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
		Light light = visibleLight.light;
		float innerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * light.innerSpotAngle);
		float outerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * visibleLight.spotAngle);
		float angleRangeInv = 1f / Mathf.Max(innerCos - outerCos, 0.001f);
		otherLightSpotAngles[index] = new Vector4(angleRangeInv, -outerCos * angleRangeInv);
		otherLightShadowData[index] = shadows.ReserveOtherShadows(light, visibleIndex);
	}

	void SetupPointLight (int index, int visibleIndex, ref VisibleLight visibleLight) {
		otherLightColors[index] = visibleLight.finalColor;
		Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);
		position.w = 1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f);
		otherLightPositions[index] = position;
		otherLightSpotAngles[index] = new Vector4(0f, 1f);
		Light light = visibleLight.light;
		otherLightShadowData[index] = shadows.ReserveOtherShadows(light, visibleIndex);
	}
	
	void SetupDirectionalLight(int index, int visibleIndex, ref VisibleLight visibleLight)
	{
		dirLightColors[index] = visibleLight.finalColor;
		dirLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
		dirLightShadowData[index] = shadows.ReserveDirectionalShadows(visibleLight.light, visibleIndex);
	}
}