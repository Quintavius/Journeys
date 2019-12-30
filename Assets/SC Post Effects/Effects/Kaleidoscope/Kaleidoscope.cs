using System;
using UnityEngine;
#if SCPE
using UnityEngine.Rendering.PostProcessing;
#endif
namespace SCPE
{
#if !SCPE
    public class Kaleidoscope : ScriptableObject { }
}
#else
    [Serializable]
    [PostProcess(typeof(KaleidoscopeRenderer), PostProcessEvent.AfterStack, "SC Post Effects/Misc/Kaleidoscope", true)]
    public sealed class Kaleidoscope : PostProcessEffectSettings
    {
        [Range(0f, 10f), Tooltip("The number of times the screen is split up")]
        public UnityEngine.Rendering.PostProcessing.IntParameter splits = new UnityEngine.Rendering.PostProcessing.IntParameter { value = 5 };

        public override bool IsEnabledAndSupported(PostProcessRenderContext context)
        {
            if (enabled.value)
            {
                if (splits == 0) { return false; }
                return true;
            }

            return false;
        }
    }

    internal sealed class KaleidoscopeRenderer : PostProcessEffectRenderer<Kaleidoscope>
    {
        Shader shader;

        public override void Init()
        {
            shader = Shader.Find("Hidden/SC Post Effects/Kaleidoscope");
        }

        public override void Release()
        {
            base.Release();
        }

        public override void Render(PostProcessRenderContext context)
        {
            var sheet = context.propertySheets.Get(shader);
            sheet.properties.SetFloat("_Splits", Mathf.PI * 2 / Mathf.Max(1, settings.splits));
            context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
        }
    }
}
#endif