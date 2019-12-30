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
    public sealed class SharpenEditor : Editor {} }
#else
    [PostProcessEditor(typeof(Sharpen))]
    public sealed class SharpenEditor : PostProcessEffectEditor<Sharpen>
    {
        SerializedParameterOverride amount;

        public override void OnEnable()
        {
            amount = FindParameterOverride(x => x.amount);
        }

        public override void OnInspectorGUI()
        {
            SCPE_GUI.DisplayDocumentationButton("sharpen");

            PropertyField(amount);
        }
    }
}
#endif