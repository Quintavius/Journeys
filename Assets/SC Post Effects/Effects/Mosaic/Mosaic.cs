using System;
using UnityEngine;
#if SCPE
using UnityEngine.Rendering.PostProcessing;
#endif
namespace SCPE
{
#if !SCPE
    public class Mosaic : ScriptableObject {}
}
#else
    [Serializable]
    [PostProcess(typeof(MosaicRenderer), PostProcessEvent.AfterStack, "SC Post Effects/Stylized/Mosaic", true)]
    public sealed class Mosaic : PostProcessEffectSettings
    {
        public enum MosaicMode
        {
            Triangles = 0,
            Hexagons = 1,
            Circles
        }

        [Serializable]
        public sealed class MosaicModeParam : ParameterOverride<MosaicMode> { }

        [DisplayName("Method"), Tooltip("")]
        public MosaicModeParam mode = new MosaicModeParam { value = MosaicMode.Hexagons };

        [Range(0f, 1f), Tooltip("Size")]
        public UnityEngine.Rendering.PostProcessing.FloatParameter size = new UnityEngine.Rendering.PostProcessing.FloatParameter { value = 0.075f };

        public override bool IsEnabledAndSupported(PostProcessRenderContext context)
        {
            if (enabled.value)
            {
                if (size == 0) { return false; }
                return true;
            }

            return false;
        }
    }

    internal sealed class MosaicRenderer : PostProcessEffectRenderer<Mosaic>
    {
        Shader shader;

        public override void Init()
        {
            shader = Shader.Find("Hidden/SC Post Effects/Mosaic");
        }

        public override void Release()
        {
            base.Release();
        }

        public override void Render(PostProcessRenderContext context)
        {
            var sheet = context.propertySheets.Get(shader);

            float size = settings.size;

            switch ((Mosaic.MosaicMode)settings.mode)
            {
                case Mosaic.MosaicMode.Triangles: size = 10f / settings.size;
                    break;
                case Mosaic.MosaicMode.Hexagons: size = settings.size / 10f;
                    break;
                case Mosaic.MosaicMode.Circles: size = (1-settings.size) * 300f;
                    break;
            }

            Vector4 parameters = new Vector4(size, ((context.screenWidth * 2 / context.screenHeight) * size / Mathf.Sqrt(3f)), 0f, 0f);

            sheet.properties.SetVector("_Params", parameters);

            context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, (int)settings.mode.value);
        }
    }
}
#endif
