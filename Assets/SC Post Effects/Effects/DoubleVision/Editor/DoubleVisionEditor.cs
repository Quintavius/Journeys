using UnityEditor;
using UnityEngine;
#if SCPE
using UnityEngine.Rendering.PostProcessing;
using UnityEditor.Rendering.PostProcessing;
#endif

namespace SCPE
{
#if !SCPE
    public sealed class DoubleVisionEditor : Editor {} }
#else
    [PostProcessEditor(typeof(DoubleVision))]
    public sealed class DoubleVisionEditor : PostProcessEffectEditor<DoubleVision>
    {
        SerializedParameterOverride mode;
        SerializedParameterOverride intensity;
        SerializedParameterOverride amount;

        public override void OnEnable()
        {
            mode = FindParameterOverride(x => x.mode);
            intensity = FindParameterOverride(x => x.intensity);
        }
        
        public override string GetDisplayTitle()
        {
            return base.GetDisplayTitle() + SCPE_GUI.ModeTitle(mode);
        }
        
        public override void OnInspectorGUI()
        {
            SCPE_GUI.DisplayDocumentationButton("double-vision");

            PropertyField(mode);
            PropertyField(intensity);
        }
    }
}
#endif