using UnityEngine;
using UnityEngine.Rendering;

public class Shadows
{

	private const string bufferName = "Shadows";

	private CommandBuffer commandBuffer = new CommandBuffer{ name = bufferName };

	private ScriptableRenderContext context;

	private CullingResults cullingResults;

	private ShadowSettings settings;

	private const int maxShadowedDirectionalLightCount = 4, maxCascades = 4;
	
	private bool useShadowMask; 

	struct ShadowedDirectionalLight{
		public int visibleLightIndex;
		public float slopeScaleBias; 
		public float nearPlaneOffset;
	}

	private ShadowedDirectionalLight[] ShadowedDirectionalLights =
		new ShadowedDirectionalLight[maxShadowedDirectionalLightCount];

	private int ShadowedDirectionalLightCount;

	private static int dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas");
	private static int dirShadowMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices");
	private static int cascadeCountId = Shader.PropertyToID("_CascadeCount");
	private static int cascadeCullingSpheresId = Shader.PropertyToID("_CascadeCullingSpheres");
	private static int shadowDistanceFadeId = Shader.PropertyToID("_ShadowDistanceFade");
	private static int cascadeDataId = Shader.PropertyToID("_CascadeData");
	private static int shadowAtlasSizeId = Shader.PropertyToID("_ShadowAtlasSize");

	private static Vector4[] cascadeCullingSpheres = new Vector4[maxCascades];
	private static Vector4[] cascadeData = new Vector4[maxCascades];

	private static Matrix4x4[] dirShadowMatrices = new Matrix4x4[maxShadowedDirectionalLightCount * maxCascades];

	private static string[] directionalFilterKeywords = {
		"_DIRECTIONAL_PCF3",
		"_DIRECTIONAL_PCF5",
		"_DIRECTIONAL_PCF7",
	};
	private static string[] cascadeBlendKeywords = {
		"_CASCADE_BLEND_SOFT",
		"_CASCADE_BLEND_DITHER"
	};
	
	static string[] shadowMaskKeywords = {
		"_SHADOW_MASK_ALWAYS",
		"_SHADOW_MASK_DISTANCE"
	};

	public void Render()
	{
		if (ShadowedDirectionalLightCount > 0)
		{
			RenderDirectionalShadows();
		}
		else
		{
			commandBuffer.GetTemporaryRT(dirShadowAtlasId, 1, 1,
				32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
		}
		
		commandBuffer.BeginSample(bufferName);
		SetKeywords(shadowMaskKeywords, useShadowMask ? 
			QualitySettings.shadowmaskMode == ShadowmaskMode.Shadowmask ? 0 : 1 : -1);
		commandBuffer.EndSample(bufferName);
		ExecuteBuffer();
	}

	void RenderDirectionalShadows() {
		int atlasSize = (int)settings.directional.atlasSize;
		commandBuffer.GetTemporaryRT(dirShadowAtlasId, atlasSize, atlasSize,
			32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
		commandBuffer.SetRenderTarget(dirShadowAtlasId,
			RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
		commandBuffer.ClearRenderTarget(true, false, Color.clear);

		commandBuffer.BeginSample(bufferName);
		ExecuteBuffer();

		int tiles = ShadowedDirectionalLightCount * settings.directional.cascadeCount;
		int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
		int tileSize = atlasSize / split;

		for (int i = 0; i < ShadowedDirectionalLightCount; i++)
		{
			RenderDirectionalShadows(i, split, tileSize);
		}

		float f = 1f - settings.directional.cascadeFade;
		commandBuffer.SetGlobalVectorArray(cascadeDataId, cascadeData);
		commandBuffer.SetGlobalVector(shadowDistanceFadeId,
			new Vector4(1f / settings.maxDistance, 1f / settings.distanceFade, 1f / (1f - f * f)));
		commandBuffer.SetGlobalInt(cascadeCountId, settings.directional.cascadeCount);
		commandBuffer.SetGlobalVectorArray(cascadeCullingSpheresId, cascadeCullingSpheres);
		commandBuffer.SetGlobalMatrixArray(dirShadowMatricesId, dirShadowMatrices);
		SetKeywords(directionalFilterKeywords, (int)settings.directional.filter - 1);
		SetKeywords(cascadeBlendKeywords, (int)settings.directional.cascadeBlend - 1);
		commandBuffer.SetGlobalVector(shadowAtlasSizeId, new Vector4(atlasSize, 1f / atlasSize));
		commandBuffer.EndSample(bufferName);
		ExecuteBuffer();
	}

	void SetKeywords(string[] keywords, int enabledIndex)
	{
		for (int i = 0; i <keywords.Length; i++)
		{
			if (i == enabledIndex)
			{
				commandBuffer.EnableShaderKeyword(keywords[i]);
			}
			else
			{
				commandBuffer.DisableShaderKeyword(keywords[i]);
			}
		}
	}

	Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m, Vector2 offset, int split)
	{
		if (SystemInfo.usesReversedZBuffer){
			m.m20 = -m.m20;
			m.m21 = -m.m21;
			m.m22 = -m.m22;
			m.m23 = -m.m23;
		}
		float scale = 1f / split;
		m.m00 = (0.5f * (m.m00 + m.m30) + offset.x * m.m30) * scale;
		m.m01 = (0.5f * (m.m01 + m.m31) + offset.x * m.m31) * scale;
		m.m02 = (0.5f * (m.m02 + m.m32) + offset.x * m.m32) * scale;
		m.m03 = (0.5f * (m.m03 + m.m33) + offset.x * m.m33) * scale;
		m.m10 = (0.5f * (m.m10 + m.m30) + offset.y * m.m30) * scale;
		m.m11 = (0.5f * (m.m11 + m.m31) + offset.y * m.m31) * scale;
		m.m12 = (0.5f * (m.m12 + m.m32) + offset.y * m.m32) * scale;
		m.m13 = (0.5f * (m.m13 + m.m33) + offset.y * m.m33) * scale;
		m.m20 = 0.5f * (m.m20 + m.m30);
		m.m21 = 0.5f * (m.m21 + m.m31);
		m.m22 = 0.5f * (m.m22 + m.m32);
		m.m23 = 0.5f * (m.m23 + m.m33);
		return m;
	}

	void RenderDirectionalShadows(int index, int split, int tileSize)
	{
		ShadowedDirectionalLight light = ShadowedDirectionalLights[index];
		var shadowSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex);

		int cascadeCount = settings.directional.cascadeCount;
		int tileOffset = index * cascadeCount;
		Vector3 ratios = settings.directional.CascadeRatios;

		float cullingFactor = Mathf.Max(0f, 0.8f - settings.directional.cascadeFade);
		for (int i = 0; i < cascadeCount; i++)
		{
			cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
				light.visibleLightIndex, i, cascadeCount, ratios, tileSize, light.nearPlaneOffset,
				out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix,
				out ShadowSplitData splitData
			);
			splitData.shadowCascadeBlendCullingFactor = cullingFactor;
			shadowSettings.splitData = splitData;
			if (index == 0){
				SetCascadeData(i, splitData.cullingSphere, tileSize);
			}
			int tileIndex = tileOffset + i;
			dirShadowMatrices[tileIndex] = ConvertToAtlasMatrix(
				projectionMatrix * viewMatrix,
				SetTileViewport(tileIndex, split, tileSize), split
			);
			commandBuffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);

			commandBuffer.SetGlobalDepthBias(0f, light.slopeScaleBias);
			ExecuteBuffer();
			context.DrawShadows(ref shadowSettings);
			commandBuffer.SetGlobalDepthBias(0f, 0f);
		}
	}
	void SetCascadeData(int index, Vector4 cullingSphere, float tileSize)
	{
		float texelSize = 2f * cullingSphere.w / tileSize;
		float filterSize = texelSize * ((float)settings.directional.filter + 1f);
		cullingSphere.w -= filterSize;
		cullingSphere.w *= cullingSphere.w;
		cascadeCullingSpheres[index] = cullingSphere;
		cascadeData[index] = new Vector4(1f / cullingSphere.w, filterSize * 1.4142136f);
	}

	Vector2 SetTileViewport(int index, int split, float tileSize)
	{
		Vector2 offset = new Vector2(index % split, index / split);
		commandBuffer.SetViewport(new Rect(
			offset.x * tileSize, offset.y * tileSize, tileSize, tileSize
		));
		return offset;
	}

	public void Cleanup()
	{
		commandBuffer.ReleaseTemporaryRT(dirShadowAtlasId);
		ExecuteBuffer();
	}
	
	public Vector4 ReserveOtherShadows (Light light, int visibleLightIndex) {
		if (light.shadows != LightShadows.None && light.shadowStrength > 0f) {
			LightBakingOutput lightBaking = light.bakingOutput;
			if (lightBaking.lightmapBakeType == LightmapBakeType.Mixed &&
				lightBaking.mixedLightingMode == MixedLightingMode.Shadowmask) {
				useShadowMask = true;
				return new Vector4(light.shadowStrength, 0f, 0f,
					lightBaking.occlusionMaskChannel);
			}
		}
		return new Vector4(0f, 0f, 0f, -1f);
	}

	public Vector4 ReserveDirectionalShadows(Light light, int visibleLightIndex) {
		if (ShadowedDirectionalLightCount < maxShadowedDirectionalLightCount &&
			light.shadows != LightShadows.None && light.shadowStrength > 0f){
			float maskChannel = -1;
			LightBakingOutput lightBaking = light.bakingOutput;
			if (lightBaking.lightmapBakeType == LightmapBakeType.Mixed &&
				lightBaking.mixedLightingMode == MixedLightingMode.Shadowmask) {
				useShadowMask = true;
				maskChannel = lightBaking.occlusionMaskChannel;
			}
			
			if (!cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b)) {
				return new Vector4(-light.shadowStrength, 0f, 0f, maskChannel);
			}
			
			ShadowedDirectionalLights[ShadowedDirectionalLightCount] =
				new ShadowedDirectionalLight{ 
					visibleLightIndex = visibleLightIndex,
					slopeScaleBias = light.shadowBias,
					nearPlaneOffset = light.shadowNearPlane
				};
			return new Vector4(
				light.shadowStrength,
				settings.directional.cascadeCount * ShadowedDirectionalLightCount++,
				light.shadowNormalBias, maskChannel
			);
		}
		return new Vector4(0f, 0f, 0f, -1f);
	}

	public void Setup(ScriptableRenderContext _context, CullingResults _cullingResults,
					  ShadowSettings _shadowSettings)
	{
		this.useShadowMask = false;
		this.context = _context;
		this.cullingResults = _cullingResults;
		this.settings = _shadowSettings;
		ShadowedDirectionalLightCount = 0;
	}

	void ExecuteBuffer(){
		context.ExecuteCommandBuffer(commandBuffer);
		commandBuffer.Clear();
	}
}