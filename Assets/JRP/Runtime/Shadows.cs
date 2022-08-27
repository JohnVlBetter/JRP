using UnityEngine;
using UnityEngine.Rendering;

public class Shadows
{

	private const string bufferName = "Shadows";

	private CommandBuffer commandBuffer = new CommandBuffer{ name = bufferName };

	private ScriptableRenderContext context;

	private CullingResults cullingResults;

	private ShadowSettings settings;

	private const int maxShadowedDirectionalLightCount = 4, maxShadowedOtherLightCount = 16, maxCascades = 4;
	
	private bool useShadowMask; 

	struct ShadowedDirectionalLight{
		public int visibleLightIndex;
		public float slopeScaleBias; 
		public float nearPlaneOffset;
	}

	private ShadowedDirectionalLight[] ShadowedDirectionalLights =
		new ShadowedDirectionalLight[maxShadowedDirectionalLightCount];
	
	struct ShadowedOtherLight {
		public int visibleLightIndex;
		public float slopeScaleBias;
		public float normalBias;
	}

	ShadowedOtherLight[] shadowedOtherLights =
		new ShadowedOtherLight[maxShadowedOtherLightCount];

	private int shadowedDirectionalLightCount, shadowedOtherLightCount;

	private static int dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas");
	private static int dirShadowMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices");
	private static int otherShadowAtlasId = Shader.PropertyToID("_OtherShadowAtlas");
	private static int otherShadowMatricesId = Shader.PropertyToID("_OtherShadowMatrices");
	private static int otherShadowTilesId = Shader.PropertyToID("_OtherShadowTiles");
	private static int cascadeCountId = Shader.PropertyToID("_CascadeCount");
	private static int cascadeCullingSpheresId = Shader.PropertyToID("_CascadeCullingSpheres");
	private static int shadowDistanceFadeId = Shader.PropertyToID("_ShadowDistanceFade");
	private static int shadowPancakingId = Shader.PropertyToID("_ShadowPancaking");
	private static int cascadeDataId = Shader.PropertyToID("_CascadeData");
	private static int shadowAtlasSizeId = Shader.PropertyToID("_ShadowAtlasSize");

	private static Vector4[] cascadeCullingSpheres = new Vector4[maxCascades];
	private static Vector4[] cascadeData = new Vector4[maxCascades];
	private static Vector4[] otherShadowTiles = new Vector4[maxShadowedOtherLightCount];

	private static Matrix4x4[] dirShadowMatrices = new Matrix4x4[maxShadowedDirectionalLightCount * maxCascades];
	private static Matrix4x4[] otherShadowMatrices = new Matrix4x4[maxShadowedOtherLightCount];

	private static string[] directionalFilterKeywords = {
		"_DIRECTIONAL_PCF3",
		"_DIRECTIONAL_PCF5",
		"_DIRECTIONAL_PCF7",
	};
	private static string[] otherFilterKeywords = {
		"_OTHER_PCF3",
		"_OTHER_PCF5",
		"_OTHER_PCF7",
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
		if (shadowedDirectionalLightCount > 0)
		{
			RenderDirectionalShadows();
		}
		else
		{
			commandBuffer.GetTemporaryRT(dirShadowAtlasId, 1, 1,
				32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
		}
		if (shadowedOtherLightCount > 0) {
			RenderOtherShadows();
		}
		else {
			commandBuffer.SetGlobalTexture(otherShadowAtlasId, dirShadowAtlasId);
		}
		
		commandBuffer.BeginSample(bufferName);
		SetKeywords(shadowMaskKeywords, useShadowMask ? 
			QualitySettings.shadowmaskMode == ShadowmaskMode.Shadowmask ? 0 : 1 : -1);
		
		commandBuffer.SetGlobalInt(cascadeCountId, 
			shadowedDirectionalLightCount > 0 ? settings.directional.cascadeCount : 0);
		float f = 1f - settings.directional.cascadeFade;
		commandBuffer.SetGlobalVector(shadowDistanceFadeId, new Vector4(
				1f / settings.maxDistance, 1f / settings.distanceFade,1f / (1f - f * f))
		);
		
		commandBuffer.SetGlobalVector(shadowAtlasSizeId, atlasSizes);
		commandBuffer.EndSample(bufferName);
		ExecuteBuffer();
	}
	
	Vector4 atlasSizes;

	void RenderDirectionalShadows() {
		int atlasSize = (int)settings.directional.atlasSize;
		atlasSizes.x = atlasSize;
		atlasSizes.y = 1f / atlasSize;
		commandBuffer.GetTemporaryRT(dirShadowAtlasId, atlasSize, atlasSize,
			32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
		commandBuffer.SetRenderTarget(dirShadowAtlasId,
			RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
		commandBuffer.ClearRenderTarget(true, false, Color.clear);
		commandBuffer.SetGlobalFloat(shadowPancakingId, 1f);
		commandBuffer.BeginSample(bufferName);
		ExecuteBuffer();

		int tiles = shadowedDirectionalLightCount * settings.directional.cascadeCount;
		int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
		int tileSize = atlasSize / split;

		for (int i = 0; i < shadowedDirectionalLightCount; i++)
		{
			RenderDirectionalShadows(i, split, tileSize);
		}

		commandBuffer.SetGlobalVectorArray(cascadeDataId, cascadeData);
		commandBuffer.SetGlobalVectorArray(cascadeCullingSpheresId, cascadeCullingSpheres);
		commandBuffer.SetGlobalMatrixArray(dirShadowMatricesId, dirShadowMatrices);
		SetKeywords(directionalFilterKeywords, (int)settings.directional.filter - 1);
		SetKeywords(cascadeBlendKeywords, (int)settings.directional.cascadeBlend - 1);
		commandBuffer.EndSample(bufferName);
		ExecuteBuffer();
	}
	
	void RenderSpotShadows (int index, int split, int tileSize) {
		ShadowedOtherLight light = shadowedOtherLights[index];
		var shadowSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex);
		cullingResults.ComputeSpotShadowMatricesAndCullingPrimitives(
			light.visibleLightIndex, out Matrix4x4 viewMatrix,
			out Matrix4x4 projectionMatrix, out ShadowSplitData splitData
		);
		shadowSettings.splitData = splitData;
		float texelSize = 2f / (tileSize * projectionMatrix.m00);
		float filterSize = texelSize * ((float)settings.other.filter + 1f);
		float bias = light.normalBias * filterSize * 1.4142136f;
		Vector2 offset = SetTileViewport(index, split, tileSize);
		float tileScale = 1f / split;
		SetOtherTileData(index, offset, tileScale, bias);
		otherShadowMatrices[index] = ConvertToAtlasMatrix(
			projectionMatrix * viewMatrix, offset, tileScale
		);
		commandBuffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
		commandBuffer.SetGlobalDepthBias(0f, light.slopeScaleBias);
		ExecuteBuffer();
		context.DrawShadows(ref shadowSettings);
		commandBuffer.SetGlobalDepthBias(0f, 0f);
	}
	
	void RenderOtherShadows () {
		int atlasSize = (int)settings.other.atlasSize;
		atlasSizes.z = atlasSize;
		atlasSizes.w = 1f / atlasSize;
		commandBuffer.GetTemporaryRT(
			otherShadowAtlasId, atlasSize, atlasSize,
			32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap
		);
		commandBuffer.SetRenderTarget(
			otherShadowAtlasId,
			RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
		);
		commandBuffer.ClearRenderTarget(true, false, Color.clear);
		commandBuffer.SetGlobalFloat(shadowPancakingId, 0f);
		commandBuffer.BeginSample(bufferName);
		ExecuteBuffer();

		int tiles = shadowedOtherLightCount;
		int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
		int tileSize = atlasSize / split;

		for (int i = 0; i < shadowedOtherLightCount; i++) {
			RenderSpotShadows(i, split, tileSize);
		}
		commandBuffer.SetGlobalMatrixArray(otherShadowMatricesId, otherShadowMatrices);
		commandBuffer.SetGlobalVectorArray(otherShadowTilesId, otherShadowTiles);
		SetKeywords(
			otherFilterKeywords, (int)settings.other.filter - 1
		);
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
	
	void SetOtherTileData (int index, Vector2 offset, float scale, float bias) {
		float border = atlasSizes.w * 0.5f;
		Vector4 data;
		data.x = offset.x * scale + border;
		data.y = offset.y * scale + border;
		data.z = scale - border - border;
		data.w = bias;
		otherShadowTiles[index] = data;
	}

	Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m, Vector2 offset, float scale)
	{
		if (SystemInfo.usesReversedZBuffer){
			m.m20 = -m.m20;
			m.m21 = -m.m21;
			m.m22 = -m.m22;
			m.m23 = -m.m23;
		}
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
		float tileScale = 1f / split;
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
				SetTileViewport(tileIndex, split, tileSize), tileScale
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
		if (shadowedOtherLightCount > 0) {
			commandBuffer.ReleaseTemporaryRT(otherShadowAtlasId);
		}
		ExecuteBuffer();
	}
	
	public Vector4 ReserveOtherShadows (Light light, int visibleLightIndex) {
		if (light.shadows == LightShadows.None || light.shadowStrength <= 0f) {
			return new Vector4(0f, 0f, 0f, -1f);
		}

		float maskChannel = -1f;
		LightBakingOutput lightBaking = light.bakingOutput;
		if (lightBaking.lightmapBakeType == LightmapBakeType.Mixed &&
			lightBaking.mixedLightingMode == MixedLightingMode.Shadowmask) {
			useShadowMask = true;
			maskChannel = lightBaking.occlusionMaskChannel;
		}
		if (shadowedOtherLightCount >= maxShadowedOtherLightCount ||
			!cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b)) {
			return new Vector4(-light.shadowStrength, 0f, 0f, maskChannel);
		}
		
		shadowedOtherLights[shadowedOtherLightCount] = new ShadowedOtherLight {
			visibleLightIndex = visibleLightIndex,
			slopeScaleBias = light.shadowBias,
			normalBias = light.shadowNormalBias
		};
		
		return new Vector4(light.shadowStrength, shadowedOtherLightCount++, 0f,maskChannel);
	}

	public Vector4 ReserveDirectionalShadows(Light light, int visibleLightIndex) {
		if (shadowedDirectionalLightCount < maxShadowedDirectionalLightCount &&
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
			
			ShadowedDirectionalLights[shadowedDirectionalLightCount] =
				new ShadowedDirectionalLight{ 
					visibleLightIndex = visibleLightIndex,
					slopeScaleBias = light.shadowBias,
					nearPlaneOffset = light.shadowNearPlane
				};
			return new Vector4(
				light.shadowStrength,
				settings.directional.cascadeCount * shadowedDirectionalLightCount++,
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
		shadowedDirectionalLightCount = shadowedOtherLightCount = 0;
	}

	void ExecuteBuffer(){
		context.ExecuteCommandBuffer(commandBuffer);
		commandBuffer.Clear();
	}
}