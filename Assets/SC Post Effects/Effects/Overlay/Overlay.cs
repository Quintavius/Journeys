using System;
using UnityEngine;
#if SCPE
using UnityEngine.Rendering.PostProcessing;
using TextureParameter = UnityEngine.Rendering.PostProcessing.TextureParameter;
using BoolParameter = UnityEngine.Rendering.PostProcessing.BoolParameter;
using FloatParameter = UnityEngine.Rendering.PostProcessing.FloatParameter;
#endif
namespace SCPE
{
#if !SCPE
    public class Overlay : ScriptableObject {}
}
#else
    [Serializable]
    [PostProcess(typeof(OverlayRenderer), PostProcessEvent.AfterStack, "SC Post Effects/Screen/Overlay", true)]
    public sealed class Overlay : PostProcessEffectSettings
    {
        [Tooltip("The texture's alpha channel controls its opacity")]
        public TextureParameter overlayTex = new TextureParameter { value = null };

        [Tooltip("Maintains the image aspect ratio, regardless of the screen width")]
        public BoolParameter autoAspect = new BoolParameter { value = false };

        public enum BlendMode
        {
            Linear,
            Additive,
            Multiply,
            Screen
        }

        [Serializable]
        public sealed class BlendModeParameter : ParameterOverride<BlendMode> { }

        [Tooltip("Blends the gradient through various Photoshop-like blending modes")]
        public BlendModeParameter blendMode = new BlendModeParameter { value = BlendMode.Linear };

        [Range(0f, 1f)]
        public FloatParameter intensity = new FloatParameter { value = 1f };

        [Range(0f, 1f)]
        public FloatParameter tiling = new FloatParameter { value = 0f };

        public override bool IsEnabledAndSupported(PostProcessRenderContext context)
        {
            if (enabled.value)
            {
                if (overlayTex.value == null || intensity == 0) { return false; }
                return true;
            }

            return false;
        }
    }

    internal sealed class OverlayRenderer : PostProcessEffectRenderer<Overlay>
    {
        Shader shader;

        public override void Init()
        {
            shader = Shader.Find("Hidden/SC Post Effects/Overlay");
        }

        public override void Release()
        {
            base.Release();
        }

        public override void Render(PostProcessRenderContext context)
        {
            var sheet = context.propertySheets.Get(shader);

            if (settings.overlayTex.value) sheet.properties.SetTexture("_OverlayTex", settings.overlayTex);
            sheet.properties.SetVector("_Params", new Vector4(settings.intensity, Mathf.Pow(settings.tiling + 1, 2), settings.autoAspect ? 1f : 0f, (int)settings.blendMode.value));

            context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
        }
    }
}
#endif