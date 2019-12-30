using System;
using UnityEngine;
#if SCPE
using UnityEngine.Rendering.PostProcessing;
using TextureParameter = UnityEngine.Rendering.PostProcessing.TextureParameter;
using BoolParameter = UnityEngine.Rendering.PostProcessing.BoolParameter;
using FloatParameter = UnityEngine.Rendering.PostProcessing.FloatParameter;
using IntParameter = UnityEngine.Rendering.PostProcessing.IntParameter;
using ColorParameter = UnityEngine.Rendering.PostProcessing.ColorParameter;
using MinAttribute = UnityEngine.Rendering.PostProcessing.MinAttribute;
#endif
namespace SCPE
{
#if !SCPE
    public class Dithering : ScriptableObject {}
}
#else
    [Serializable]
    [PostProcess(typeof(DitheringRenderer), PostProcessEvent.AfterStack, "SC Post Effects/Retro/Dithering", true)]
    public sealed class Dithering : PostProcessEffectSettings
    {
        [DisplayName("Pattern"), Tooltip("Note that the texture's filter mode (Point or Bilinear) greatly affects the behavior of the pattern")]
        public TextureParameter lut = new TextureParameter { value = null };

        [Range(0f, 1f), Tooltip("The screen's luminance values control the density of the dithering matrix")]
        public FloatParameter luminanceThreshold = new FloatParameter { value = 0.5f };

        [Range(0f, 1f), Tooltip("Fades the effect in or out")]
        public FloatParameter intensity = new FloatParameter { value = 0.5f };

        [Range(0f,2f), DisplayName("Tiling")]
        public FloatParameter tiling = new FloatParameter { value = 1f };

        public override bool IsEnabledAndSupported(PostProcessRenderContext context)
        {
            if (enabled.value)
            {
                if (intensity == 0) return false;
                return true;
            }

            return false;
        }

    }

    internal sealed class DitheringRenderer : PostProcessEffectRenderer<Dithering>
    {
        Shader shader;

        public override void Init()
        {
            shader = Shader.Find("Hidden/SC Post Effects/Dithering");
        }

        public override void Release()
        {
            base.Release();
        }

        public override void Render(PostProcessRenderContext context)
        {
            var sheet = context.propertySheets.Get(shader);

            var lutTexture = settings.lut.value == null ? RuntimeUtilities.blackTexture : settings.lut.value;
            sheet.properties.SetTexture("_LUT", lutTexture);
            float luminanceThreshold = QualitySettings.activeColorSpace == ColorSpace.Gamma ? Mathf.LinearToGammaSpace(settings.luminanceThreshold.value) : settings.luminanceThreshold.value;

            Vector4 ditherParams = new Vector4(0f, settings.tiling, luminanceThreshold, settings.intensity);
            sheet.properties.SetVector("_Dithering_Coords", ditherParams);

            context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
        }
    }
}
#endif