using SCPE;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if SCPE
using UnityEngine.Rendering.PostProcessing;
using TextureParameter = UnityEngine.Rendering.PostProcessing.TextureParameter;
using BoolParameter = UnityEngine.Rendering.PostProcessing.BoolParameter;
using FloatParameter = UnityEngine.Rendering.PostProcessing.FloatParameter;
using IntParameter = UnityEngine.Rendering.PostProcessing.IntParameter;
using ColorParameter = UnityEngine.Rendering.PostProcessing.ColorParameter;
using Vector3Parameter = UnityEngine.Rendering.PostProcessing.Vector3Parameter;
using MinAttribute = UnityEngine.Rendering.PostProcessing.MinAttribute;
using MaxAttribute = UnityEngine.Rendering.PostProcessing.MaxAttribute;
#endif
namespace SCPE
{
#if !SCPE
    public class Fog : ScriptableObject { }
}
#else
    [Serializable]
    [PostProcess(typeof(FogRenderer), PostProcessEvent.BeforeStack, "SC Post Effects/Environment/Screen-Space Fog")]
    public sealed class Fog : PostProcessEffectSettings
    {
        public TextureParameter skyboxTex = new TextureParameter { value = null };

        [DisplayName("Use scene's settings"), Tooltip("Use the settings of the current active scene found under the Lighting tab\n\nThis is also advisable for third-party scripts that modify fog settings\n\nThis will force the effect to use the scene's fog color")]
        public BoolParameter useSceneSettings = new BoolParameter { value = false };
        [Serializable]
        public sealed class FogModeParameter : ParameterOverride<FogMode> { }
        [DisplayName("Mode"), Tooltip("Sets how the fog distance is calculated")]
        public FogModeParameter fogMode = new FogModeParameter { value = FogMode.Exponential };

        [Range(0f, 1f)]
        public FloatParameter globalDensity = new FloatParameter { value = 0.2f };
        [DisplayName("Start")]
        public FloatParameter fogStartDistance = new FloatParameter { value = 170f };
        [DisplayName("End")]
        public FloatParameter fogEndDistance = new FloatParameter { value = 600f };

        public enum FogColorSource
        {
            UniformColor,
            GradientTexture,
            SkyboxColor
        }

        [Serializable]

        public sealed class FogColorSourceParameter : ParameterOverride<FogColorSource> { }
        [Space]

        [Tooltip("Color: use a uniform color for the fog\n\nGradient: sample a gradient texture to control the fog color over distance, the alpha channel controls the density\n\nSkybox: Sample the skybox's color for the fog, only works well with low detail skies")]
        public FogColorSourceParameter colorSource = new FogColorSourceParameter { value = FogColorSource.UniformColor };

        [DisplayName("Mipmap"), Tooltip("Set the mipmap level for the skybox texture"), Range(0f, 8f)]
        public FloatParameter skyboxMipLevel = new FloatParameter { value = 0f };

#if UNITY_2018_1_OR_NEWER
        [DisplayName("Color"), ColorUsage(true, true)]
#else
        [DisplayName("Color"), ColorUsage(true, true, 1f, 10f, 0.125f, 3f)]
#endif
        public ColorParameter fogColor = new ColorParameter { value = new Color(0.76f, 0.94f, 1f, 1f) };

        [DisplayName("Texture")]
        public TextureParameter fogColorGradient = new TextureParameter { value = null };
        [Tooltip("Automatic mode uses the current camera's far clipping plane to set the max distance\n\nOtherwise, a fixed value may be used instead")]
        public FloatParameter gradientDistance = new FloatParameter { value = 1000f };
        public BoolParameter gradientUseFarClipPlane = new BoolParameter { value = true };

        [Header("Distance")]
        [DisplayName("Enable")]
        public BoolParameter distanceFog = new BoolParameter { value = true };
        [Range(0.001f, 1.0f)]
        [DisplayName("Density")]
        public FloatParameter distanceDensity = new FloatParameter { value = 1f };
        [Tooltip("Distance based on radial distance from viewer, rather than parrallel")]
        public BoolParameter useRadialDistance = new BoolParameter { value = true };

        [Header("Skybox")]
        [Range(0f, 1f), Tooltip("Determines how much the fog influences the skybox")]
        public FloatParameter skyboxInfluence = new FloatParameter { value = 1f };

        [Header("Directional Light")]
        [Tooltip("Translates the a Directional Light's direction and color into the fog. Creates a faux-atmospheric scattering effect.")]
        public BoolParameter enableDirectionalLight = new BoolParameter { value = false };
        [Tooltip("Use the intensity of the Directional Light that's set as the caster")]
        public BoolParameter useLightDirection = new BoolParameter { value = true };
        [Tooltip("Use the color of the Directional Light that's set as the caster")]
        public BoolParameter useLightColor = new BoolParameter { value = true };
        [Tooltip("Use the intensity of the Directional Light that's set as the caster")]
        public BoolParameter useLightIntensity = new BoolParameter { value = true };

        public static Vector3 LightDirection = Vector3.zero;
#if UNITY_2018_1_OR_NEWER
        [DisplayName("Color"), ColorUsage(true, true)]
#else
        [DisplayName("Color"), ColorUsage(true, true, 1f, 10f, 0.125f, 3f)]
#endif
        public ColorParameter lightColor = new ColorParameter { value = new Color(1f, 0.89f, 0.55f, 1f) };
        [Max(1f)]
        public Vector3Parameter lightDirection = new Vector3Parameter { value = new Vector3(0, 0.5f, -1f) };
        public FloatParameter lightIntensity = new FloatParameter { value = 1f };

        [Header("Height")]
        [DisplayName("Enable"), Tooltip("Enable vertical height fog")]
        public BoolParameter heightFog = new BoolParameter { value = true };

        [Tooltip("Height relative to 0 world height position")]
        public FloatParameter height = new FloatParameter { value = 10f };

        [Range(0.001f, 1.0f)]
        [DisplayName("Density")]
        public FloatParameter heightDensity = new FloatParameter { value = 0.75f };

        [Header("Height noise (2D)")]
        [DisplayName("Enable"), Tooltip("Enables height fog density variation through the use of a texture")]
        public BoolParameter heightFogNoise = new BoolParameter { value = false };
        [DisplayName("Texture (R)"), Tooltip("The density is read from this texture's red color channel")]
        public TextureParameter heightNoiseTex = new TextureParameter { value = null };
        [Range(0f, 1f)]
        [DisplayName("Size")]
        public FloatParameter heightNoiseSize = new FloatParameter { value = 0.25f };
        [Range(0f, 1f)]
        [DisplayName("Strength")]
        public FloatParameter heightNoiseStrength = new FloatParameter { value = 1f };
        [Range(0f, 10f)]
        [DisplayName("Speed")]
        public FloatParameter heightNoiseSpeed = new FloatParameter { value = 2f };

        [Header("Light scattering")]
        [DisplayName("Enable"), Tooltip("Execute a bloom pass to diffuse light in dense fog")]
        public BoolParameter lightScattering = new BoolParameter { value = false };

        [Space]

        [Min(0f), DisplayName("Intensity")]
        public FloatParameter scatterIntensity = new FloatParameter { value = 10f };

        [Min(0f), DisplayName("Threshold"), Tooltip("Filters out pixels under this level of brightness. Value is in gamma-space.")]
        public FloatParameter scatterThreshold = new FloatParameter { value = 1f };

        [Range(1f, 10f), DisplayName("Diffusion")]
        public FloatParameter scatterDiffusion = new FloatParameter { value = 10f };

        [Range(0f, 1f), DisplayName("Smoothness"), Tooltip("Makes transitions between under/over-threshold gradual. 0 for a hard threshold, 1 for a soft threshold).")]
        public FloatParameter scatterSoftKnee = new FloatParameter { value = 0.5f };

        public override bool IsEnabledAndSupported(PostProcessRenderContext context)
        {
            return enabled.value;
        }

    }

    internal sealed class FogRenderer : PostProcessEffectRenderer<Fog>
    {

        Shader shader;

        struct MipLevel
        {
            internal int down;
            internal int up;
        }

        MipLevel[] m_Pyramid;
        const int k_MaxPyramidSize = 16;

        enum Pass
        {
            Prefilter,
            Downsample,
            Upsample,
            Blend,
            BlendScattering
        }

        public override void Init()
        {
            shader = Shader.Find("Hidden/SC Post Effects/Fog");

            m_Pyramid = new MipLevel[k_MaxPyramidSize];

            for (int i = 0; i < k_MaxPyramidSize; i++)
            {
                m_Pyramid[i] = new MipLevel
                {
                    down = Shader.PropertyToID("_BloomMipDown" + i),
                    up = Shader.PropertyToID("_BloomMipUp" + i)
                };
            }
        }

        public override void Release()
        {
            base.Release();
        }

        public static Dictionary<Camera, RenderScreenSpaceSkybox> skyboxCams = new Dictionary<Camera, RenderScreenSpaceSkybox>();

        public override void Render(PostProcessRenderContext context)
        {
            PropertySheet sheet = context.propertySheets.Get(shader);
            CommandBuffer cmd = context.command;

            Camera cam = context.camera;

            #region Skybox sampling
            //Add the skybox rendering component to any camera rendering Fog
            if (settings.colorSource.value == Fog.FogColorSource.SkyboxColor)
            {
                //Ignore hidden camera's, except scene-view cam
                if (cam.hideFlags != HideFlags.None && cam.name != "SceneCamera") return;

                if (!skyboxCams.ContainsKey(cam))
                {
                    skyboxCams[cam] = cam.gameObject.GetComponent<RenderScreenSpaceSkybox>();

                    if (!skyboxCams[cam])
                    {
                        skyboxCams[cam] = cam.gameObject.AddComponent<RenderScreenSpaceSkybox>();
                    }
                    skyboxCams[cam].manuallyAdded = false; //Don't show warning on component
                }
            }
            #endregion

            //OpenVR.System.GetProjectionMatrix(vrEye, mainCamera.nearClipPlane, mainCamera.farClipPlane, EGraphicsAPIConvention.API_DirectX)

            //Clip-space to world-space camera matrix conversion
            #region Property value composition
            var p = GL.GetGPUProjectionMatrix(cam.projectionMatrix, false);
            p[2, 3] = p[3, 2] = 0.0f;
            p[3, 3] = 1.0f;
            var clipToWorld = Matrix4x4.Inverse(p * cam.worldToCameraMatrix) * Matrix4x4.TRS(new Vector3(0, 0, -p[2, 2]), Quaternion.identity, Vector3.one);
            sheet.properties.SetMatrix("clipToWorld", clipToWorld);

            float FdotC = cam.transform.position.y - settings.height;
            float paramK = (FdotC <= 0.0f ? 1.0f : 0.0f);
            //Always exclude skybox for skybox color mode
            //Always include when using light scattering to avoid depth discontinuity
            float skyboxInfluence = (settings.lightScattering) ? 1.0f : settings.skyboxInfluence;
            float distanceFog = (settings.distanceFog) ? 1.0f : 0.0f;
            float heightFog = (settings.heightFog) ? 1.0f : 0.0f;

            int colorSource = (settings.useSceneSettings) ? 0 : (int)settings.colorSource.value;
            var sceneMode = (settings.useSceneSettings) ? RenderSettings.fogMode : settings.fogMode;
            var sceneDensity = (settings.useSceneSettings) ? RenderSettings.fogDensity : settings.globalDensity / 100;
            var sceneStart = (settings.useSceneSettings) ? RenderSettings.fogStartDistance : settings.fogStartDistance;
            var sceneEnd = (settings.useSceneSettings) ? RenderSettings.fogEndDistance : settings.fogEndDistance;

            bool linear = (sceneMode == FogMode.Linear);
            float diff = linear ? sceneEnd - sceneStart : 0.0f;
            float invDiff = Mathf.Abs(diff) > 0.0001f ? 1.0f / diff : 0.0f;

            Vector4 sceneParams;
            sceneParams.x = sceneDensity * 1.2011224087f; // density / sqrt(ln(2)), used by Exp2 fog mode
            sceneParams.y = sceneDensity * 1.4426950408f; // density / ln(2), used by Exp fog mode
            sceneParams.z = linear ? -invDiff : 0.0f;
            sceneParams.w = linear ? sceneEnd * invDiff : 0.0f;

            float gradientDistance = (settings.gradientUseFarClipPlane.value) ? settings.gradientDistance : context.camera.farClipPlane;
            #endregion

            #region Property assignment
            if (settings.heightNoiseTex.value) sheet.properties.SetTexture("_NoiseTex", settings.heightNoiseTex);
            if (settings.fogColorGradient.value) sheet.properties.SetTexture("_ColorGradient", settings.fogColorGradient);
            sheet.properties.SetFloat("_FarClippingPlane", gradientDistance);
            sheet.properties.SetVector("_SceneFogParams", sceneParams);
            sheet.properties.SetVector("_SceneFogMode", new Vector4((int)sceneMode, settings.useRadialDistance ? 1 : 0, colorSource, settings.heightFogNoise ? 1 : 0));
            sheet.properties.SetVector("_NoiseParams", new Vector4(settings.heightNoiseSize * 0.01f, settings.heightNoiseSpeed * 0.01f, settings.heightNoiseStrength, 0));
            sheet.properties.SetVector("_DensityParams", new Vector4(settings.distanceDensity, settings.heightNoiseStrength, settings.skyboxMipLevel, 0));
            sheet.properties.SetVector("_HeightParams", new Vector4(settings.height, FdotC, paramK, settings.heightDensity * 0.5f));
            sheet.properties.SetVector("_DistanceParams", new Vector4(-sceneStart, 0f, distanceFog, heightFog));
            sheet.properties.SetColor("_FogColor", (settings.useSceneSettings) ? RenderSettings.fogColor : settings.fogColor);
            sheet.properties.SetVector("_SkyboxParams", new Vector4(skyboxInfluence, settings.skyboxMipLevel, 0, 0));

            Vector3 sunDir = (settings.useLightDirection) ? Fog.LightDirection : settings.lightDirection.value.normalized;
            float sunIntensity = (settings.useLightIntensity) ? FogLightSource.intensity : settings.lightIntensity.value;
            sunIntensity = (settings.enableDirectionalLight) ? sunIntensity : 0f;
            sheet.properties.SetVector("_DirLightParams", new Vector4(sunDir.x, sunDir.y, sunDir.z, sunIntensity));

            Color sunColor = (settings.useLightColor) ? FogLightSource.color : settings.lightColor.value;
            sheet.properties.SetVector("_DirLightColor", new Vector4(sunColor.r, sunColor.g, sunColor.b, 0));

            #endregion

            #region Light scattering
            //Repurpose parts of the bloom effect
            bool enableScattering = (settings.lightScattering) ? true : false;

            if (enableScattering)
            {
                int tw = Mathf.FloorToInt(context.screenWidth / (2f));
                int th = Mathf.FloorToInt(context.screenHeight / (2f));
                bool singlePassDoubleWide = (context.stereoActive && (context.stereoRenderingMode == PostProcessRenderContext.StereoRenderingMode.SinglePass) && (context.camera.stereoTargetEye == StereoTargetEyeMask.Both));
                int tw_stereo = singlePassDoubleWide ? tw * 2 : tw;

                // Determine the iteration count
                int s = Mathf.Max(tw, th);
                float logs = Mathf.Log(s, 2f) + Mathf.Min(settings.scatterDiffusion.value, 10f) - 10f;
                int logs_i = Mathf.FloorToInt(logs);
                int iterations = Mathf.Clamp(logs_i, 1, k_MaxPyramidSize);
                float sampleScale = 0.5f + logs - logs_i;
                sheet.properties.SetFloat("_SampleScale", sampleScale);

                // Prefiltering parameters
                float lthresh = Mathf.GammaToLinearSpace(settings.scatterThreshold.value);
                float knee = lthresh * settings.scatterSoftKnee.value + 1e-5f;
                var threshold = new Vector4(lthresh, lthresh - knee, knee * 2f, 0.25f / knee);
                sheet.properties.SetVector("_Threshold", threshold);

                // Downsample
                var lastDown = context.source;
                for (int i = 0; i < iterations; i++)
                {
                    int mipDown = m_Pyramid[i].down;
                    int mipUp = m_Pyramid[i].up;
                    int pass = i == 0 ? (int)Pass.Prefilter : (int)Pass.Downsample;

                    context.GetScreenSpaceTemporaryRT(cmd, mipDown, 0, context.sourceFormat, RenderTextureReadWrite.Default, FilterMode.Bilinear, tw_stereo, th);
                    context.GetScreenSpaceTemporaryRT(cmd, mipUp, 0, context.sourceFormat, RenderTextureReadWrite.Default, FilterMode.Bilinear, tw_stereo, th);
                    cmd.BlitFullscreenTriangle(lastDown, mipDown, sheet, pass);

                    lastDown = mipDown;
                    tw_stereo = (singlePassDoubleWide && ((tw_stereo / 2) % 2 > 0)) ? 1 + tw_stereo / 2 : tw_stereo / 2;
                    tw_stereo = Mathf.Max(tw_stereo, 1);
                    th = Mathf.Max(th / 2, 1);
                }

                // Upsample
                int lastUp = m_Pyramid[iterations - 1].down;
                for (int i = iterations - 2; i >= 0; i--)
                {
                    int mipDown = m_Pyramid[i].down;
                    int mipUp = m_Pyramid[i].up;
                    cmd.SetGlobalTexture("_BloomTex", mipDown);
                    cmd.BlitFullscreenTriangle(lastUp, mipUp, sheet, (int)Pass.Upsample);
                    lastUp = mipUp;
                }

                float intensity = RuntimeUtilities.Exp2(settings.scatterIntensity.value / 10f) - 1f;
                var shaderSettings = new Vector4(sampleScale, intensity, 0, iterations);
                sheet.properties.SetVector("_ScatteringParams", shaderSettings);

                cmd.SetGlobalTexture("_BloomTex", lastUp);

                // Cleanup
                for (int i = 0; i < iterations; i++)
                {
                    if (m_Pyramid[i].down != lastUp)
                        cmd.ReleaseTemporaryRT(m_Pyramid[i].down);
                    if (m_Pyramid[i].up != lastUp)
                        cmd.ReleaseTemporaryRT(m_Pyramid[i].up);
                }
            }

            #endregion

            #region shader passes
            context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, enableScattering ? (int)Pass.BlendScattering : (int)Pass.Blend);
            #endregion

        }

        public override DepthTextureMode GetCameraFlags()
        {
            return DepthTextureMode.Depth;
        }
    }
}
#endif