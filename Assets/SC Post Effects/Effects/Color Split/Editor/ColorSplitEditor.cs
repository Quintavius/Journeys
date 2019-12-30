using UnityEditor;
using UnityEngine;
#if SCPE
using UnityEngine.Rendering.PostProcessing;
using UnityEditor.Rendering.PostProcessing;
#endif

namespace SCPE
{
#if !SCPE
    public sealed class ColorSplitEditor : Editor {} }
#else
    [PostProcessEditor(typeof(ColorSplit))]
    public sealed class ColorSplitEditor : PostProcessEffectEditor<ColorSplit>
    {
        SerializedParameterOverride mode;
        SerializedParameterOverride offset;

        public override void OnEnable()
        {
            mode = FindParameterOverride(x => x.mode);
            offset = FindParameterOverride(x => x.offset);
        }


        public override string GetDisplayTitle()
        {
            return base.GetDisplayTitle() + SCPE_GUI.ModeTitle(mode);
        }

        public override void OnInspectorGUI()
        {
            SCPE_GUI.DisplayDocumentationButton("color-split");

            PropertyField(mode);
            PropertyField(offset);
        }
    }
}
#endif