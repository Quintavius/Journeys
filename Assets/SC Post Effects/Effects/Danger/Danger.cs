using System;
using UnityEngine;
#if SCPE
using UnityEngine.Rendering.PostProcessing;
using TextureParameter = UnityEngine.Rendering.PostProcessing.TextureParameter;
using ColorParameter = UnityEngine.Rendering.PostProcessing.ColorParameter;
using FloatParameter = UnityEngine.Rendering.PostProcessing.FloatParameter;
#endif
namespace SCPE
{
#if !SCPE
    public class Danger : ScriptableObject {}
}
#else
    [Serializable]
    [PostProcess(typeof(DangerRenderer), PostProcessEvent.AfterStack, "SC Post Effects/Screen/Danger", true)]
    public sealed class Danger : PostProcessEffectSettings
    {
        public TextureParameter overlayTex = new TextureParameter { value = null };
        public ColorParameter color = new ColorParameter { value = new Color(0.66f, 0f, 0f) };

        [Range(0f, 1f), DisplayName("Opacity")]
        public FloatParameter intensity = new FloatParameter { value = 1f };

        [Range(0f, 1f), Tooltip("Size")]
        public FloatParameter size = new FloatParameter { value = 1f };

        public override bool IsEnabledAndSupported(PostProcessRenderContext context)
        {
            if (enabled.value)
            {
                if (size == 0 || intensity == 0) { return false; }
                return true;
            }

            return false;
        }
    }

    internal sealed class DangerRenderer : PostProcessEffectRenderer<Danger>
    {
        Shader shader;

        public override void Init()
        {
            shader = Shader.Find("Hidden/SC Post Effects/Danger");
        }

        public override void Release()
        {
            base.Release();
        }

        public override void Render(PostProcessRenderContext context)
        {

            var sheet = context.propertySheets.Get(shader);

            sheet.properties.SetVector("_Params", new Vector4(settings.intensity, settings.size, 0, 0));
            sheet.properties.SetColor("_Color", settings.color);
            var overlayTexture = settings.overlayTex.value == null ? RuntimeUtilities.blackTexture : settings.overlayTex.value;
            sheet.properties.SetTexture("_Overlay", overlayTexture);

            context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
        }

    }
}
#endif