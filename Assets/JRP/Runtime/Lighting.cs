using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class Lighting
{
	private const string bufferName = "Lighting";

	private CommandBuffer commandBuffer = new CommandBuffer{ name = bufferName };

	private const int maxDirLightCount = 4;

	private static int dirLightCountId = Shader.PropertyToID("_DirectionalLightCount");
	private static int dirLightColorsId = Shader.PropertyToID("_DirectionalLightColors");
	private static int dirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections");
	private static int dirLightShadowDataId = Shader.PropertyToID("_DirectionalLightShadowData");

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

		for (int dirLightCount = 0; dirLightCount < visibleLights.Length; dirLightCount++)
		{
			VisibleLight visibleLight = visibleLights[dirLightCount];
			if (visibleLight.lightType != LightType.Directional) continue;
			SetupDirectionalLight(dirLightCount, ref visibleLight);
			if (dirLightCount >= maxDirLightCount) break;
		}

		commandBuffer.SetGlobalInt(dirLightCountId, visibleLights.Length);
		commandBuffer.SetGlobalVectorArray(dirLightColorsId, dirLightColors);
		commandBuffer.SetGlobalVectorArray(dirLightDirectionsId, dirLightDirections);
		commandBuffer.SetGlobalVectorArray(dirLightShadowDataId, dirLightShadowData);
	}

	void SetupDirectionalLight(int index, ref VisibleLight visibleLight)
	{
		dirLightColors[index] = visibleLight.finalColor;
		dirLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
		dirLightShadowData[index] = shadows.ReserveDirectionalShadows(visibleLight.light, index);
	}
}