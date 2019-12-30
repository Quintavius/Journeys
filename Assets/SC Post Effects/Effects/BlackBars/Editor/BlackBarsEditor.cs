using System.Collections;
using System.Collections.Generic;
using UnityEditor;
#if SCPE
using UnityEngine.Rendering.PostProcessing;
using UnityEditor.Rendering.PostProcessing;
#endif

namespace SCPE
{
#if !SCPE
    public sealed class BlackBarsEditor : Editor {} }
#else
    [PostProcessEditor(typeof(BlackBars))]
    public class BlackBarsEditor : PostProcessEffectEditor<BlackBars>
    {
        SerializedParameterOverride mode;
        SerializedParameterOverride size;
        SerializedParameterOverride maxSize;

        public override void OnEnable()
        {
            mode = FindParameterOverride(x => x.mode);
            size = FindParameterOverride(x => x.size);
            maxSize = FindParameterOverride(x => x.maxSize);
        }

        public override string GetDisplayTitle()
        {
            return "Black Bars (" + (BlackBars.Direction)mode.value.enumValueIndex + ")";
        }

        public override void OnInspectorGUI()
        {
            SCPE_GUI.DisplayDocumentationButton("black-bars");

            PropertyField(mode);
            PropertyField(size);
            PropertyField(maxSize);
        }
    }
}
#endif