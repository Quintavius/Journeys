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
    public sealed class ScanlinesEditor : Editor {} }
#else
    [PostProcessEditor(typeof(Scanlines))]
    public sealed class ScanlinesEditor : PostProcessEffectEditor<Scanlines>
    {
        SerializedParameterOverride intensity;
        SerializedParameterOverride amount;
        SerializedParameterOverride speed;

        public override void OnEnable()
        {
            intensity = FindParameterOverride(x => x.intensity);
            amount = FindParameterOverride(x => x.amount);
            speed = FindParameterOverride(x => x.speed);
        }

        public override void OnInspectorGUI()
        {
            SCPE_GUI.DisplayDocumentationButton("scanlines");

            PropertyField(intensity);
            PropertyField(amount);
            PropertyField(speed);
        }
    }
}
#endif