using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
#if SCPE
using UnityEditor.Rendering.PostProcessing;
using UnityEngine.Rendering.PostProcessing;
#endif

namespace SCPE
{
#if !SCPE
    public  class MosaicEditor : Editor {} }
#else
    [PostProcessEditor(typeof(Mosaic))]
    public sealed class MosaicEditor : PostProcessEffectEditor<Mosaic>
    {
        SerializedParameterOverride mode;
        SerializedParameterOverride size;

        public override void OnEnable()
        {
            mode = FindParameterOverride(x => x.mode);
            size = FindParameterOverride(x => x.size);
        }

        public override string GetDisplayTitle()
        {
            return base.GetDisplayTitle() + SCPE_GUI.ModeTitle(mode);
        }

        public override void OnInspectorGUI()
        {
            SCPE_GUI.DisplayDocumentationButton("mosaic");

            PropertyField(mode);
            PropertyField(size);
        }
    }
}
#endif