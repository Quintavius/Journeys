using System;
using System.Collections;
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
using MinAttribute = UnityEngine.Rendering.PostProcessing.MinAttribute;
#endif

namespace SCPE
{
#if !SCPE
    public class TiltShift : ScriptableObject {}
}
#else
    [System.Serializable]
    [PostProcess(typeof(TiltShiftRenderer), PostProcessEvent.AfterStack, "SC Post Effects/Blurring/Tilt Shift")]
    public class TiltShift : PostProcessEffectSettings
    {
        public enum TiltShiftMethod
        {
            Horizontal,
            Radial,
        }

        [Serializable]
        public sealed class TiltShifMethodParameter : ParameterOverride<TiltShiftMethod> { }

        [DisplayName("Method")]
        public TiltShifMethodParameter mode = new TiltShifMethodParameter { value = TiltShiftMethod.Horizontal };

        public enum Quality
        {
            Performance,
            Appearance
        }

        [Serializable]
        public sealed class TiltShiftQualityParameter : ParameterOverride<Quality> { }

        [DisplayName("Quality"), Tooltip("Choose to use more texture samples, for a smoother blur when using a high blur amout")]
        public TiltShiftQualityParameter quality = new TiltShiftQualityParameter { value = Quality.Appearance };

        [Range(0f, 1f)]
        public FloatParameter areaSize = new FloatParameter { value = 1f };
        [Range(0f, 1f)]
        public FloatParameter areaFalloff = new FloatParameter { value = 1f };

        [Range(0f, 1f), Tooltip("The amount of blurring that must be performed")]
        public FloatParameter amount = new FloatParameter { value = 0.5f };

        public static bool debug;

        public override bool IsEnabledAndSupported(PostProcessRenderContext context)
        {
            if (enabled.value)
            {
                if ((areaSize == 0f && areaFalloff == 0f) || amount == 0f) { return false; }
                return true;
            }

            return false;
        }
    }

    internal sealed class TiltShiftRenderer : PostProcessEffectRenderer<TiltShift>
    {
        Shader shader;
        int screenCopyID;

        public override void Init()
        {
            shader = Shader.Find("Hidden/SC Post Effects/Tilt Shift");
            screenCopyID = Shader.PropertyToID("_ScreenCopyTexture");
        }

        enum Pass
        {
            FragHorizontal,
            FragHorizontalHQ,
            FragRadial,
            FragRadialHQ,
            FragBlend,
            FragDebug
        }

        public override void Render(PostProcessRenderContext context)
        {
            PropertySheet sheet = context.propertySheets.Get(shader);
            CommandBuffer cmd = context.command;

            sheet.properties.SetVector("_Params", new Vector4(settings.areaSize, settings.areaFalloff, settings.amount, (int)settings.mode.value));

            //Copy screen contents
            context.command.GetTemporaryRT(screenCopyID, context.width, context.height, 0, FilterMode.Bilinear, context.sourceFormat);
            int pass = (int)settings.mode.value + (int)settings.quality.value;

            switch ((int)settings.mode.value)
            {
                case 0: pass = 0 + (int)settings.quality.value;
                    break;
                case 1: pass = 2 + (int)settings.quality.value;
                    break;
            }
            cmd.BlitFullscreenTriangle(context.source, screenCopyID, sheet, pass);
            cmd.SetGlobalTexture("_BlurredTex", screenCopyID);

            // Render blurred texture in blend pass
            cmd.BlitFullscreenTriangle(context.source, context.destination, sheet, TiltShift.debug ? (int)Pass.FragDebug : (int)Pass.FragBlend);

            cmd.ReleaseTemporaryRT(screenCopyID);
        }

    }
}
#endif