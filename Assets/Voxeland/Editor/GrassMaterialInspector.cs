using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using Voxeland5;

public class GrassMaterialInspector : MaterialEditor
{
	Layout layout;
	bool[] openedChannels = new bool[20];

	public override void OnInspectorGUI ()
	{
		if (!isVisible) { base.OnInspectorGUI (); return; }
		
		Material mat = target as Material;
		
		if (layout == null) layout = new Layout();
		layout.margin = 0; layout.rightMargin = 0;
		layout.field = Layout.GetInspectorRect();
		layout.cursor = new Rect();
		layout.undoName =  "Material parameters change";
		layout.dragChange = true;
		layout.change = false;

		DrawMain(mat, layout);

		layout.Par(10);
		for (int i=0; i<openedChannels.Length; i++)
		{
			string layerName = "Layer " + i;
			if (mat.HasProperty("_MainTex"+i))
			{
				Texture mainTex = mat.GetTexture("_MainTex"+i);
				if (mainTex!=null) layerName = mainTex.name;
			}
			
			layout.Foldout(ref openedChannels[i], layerName);
			layout.margin += 5;
			if (openedChannels[i]) DrawLayer(mat, layout, i);
			layout.margin -= 5;
		}

		Layout.SetInspectorRect(layout.field);
	}

	public enum PreviewType { disabled=0, ambient=1, metallic=2, smoothness=3, fade=4, blends=5 };


	public static void DrawMain (Material mat, Layout layout)
	{
		layout.Par(54);
		layout.Label("Texture Arrays:\n(Albedo/Normal)", layout.Inset(1-layout.fieldSize));
		layout.MatField<Texture>(mat, "_MainTexArr", rect:layout.Inset(54f));
		layout.MatField<Texture>(mat, "_BumpMapArr", rect:layout.Inset(54f));
		layout.Par(20);
		layout.Inset(1-layout.fieldSize);
		if (layout.Button("Save",  rect:layout.Inset(54f))) 
		{ 
			if (mat.HasProperty("_MainTexArr") && mat.GetTexture("_MainTexArr")!=null) 
				layout.SaveAsset((Texture2DArray)mat.GetTexture("_MainTexArr"));
		}
		if (layout.Button("Save",  rect:layout.Inset(54f)))
		{
			if (mat.HasProperty("_BumpMapArr") && mat.GetTexture("_BumpMapArr")!=null) 
				layout.SaveAsset((Texture2DArray)mat.GetTexture("_BumpMapArr"));
		}

		layout.MatField<int>(mat, "_Culling", "Culling");
		layout.MatField<float>(mat, "_Cutoff", "Alpha Ref");
		layout.MatField<float>(mat, "_Mips", "MipMap Factor");
		layout.MatField<float>(mat, "_DistanceFadeZero", "Distance Fade Zero");
		layout.MatField<float>(mat, "_DistanceFadeOne", "Distance Fade One");
		layout.MatField<Color>(mat, "_ColorTint0", "Color Tint");

		layout.Par(5);
		layout.MatKeyword(mat, "_WIND", "Wind");
		if (mat.IsKeywordEnabled("_WIND"))
		{
			layout.MatField<Texture>(mat, "_WindTex", "Wind(XY)");
			layout.MatField<float>(mat, "_WindSize", "Wind Size");
			layout.MatField<float>(mat, "_WindSpeedX", "Wind Speed X");
			layout.MatField<float>(mat, "_WindSpeedZ", "Wind Speed Z");
			layout.MatField<float>(mat, "_WindStrength", "Wind Strength");
		}

		layout.Par(5);
		layout.MatKeyword(mat, "_SPEC", "Metallic/Gloss");

		//preview
		layout.Par(5);
		if (!mat.HasProperty("_PreviewType")) { layout.Field(PreviewType.disabled, "Preview", disabled:true); }
		else
		{
			PreviewType previewType = (PreviewType)mat.GetInt("_PreviewType");
			if (!mat.IsKeywordEnabled("_PREVIEW")) previewType = PreviewType.disabled;
			layout.Field(ref previewType, "Preview");
			if (layout.lastChange)
			{
				if (previewType == PreviewType.disabled) { mat.DisableKeyword("_PREVIEW"); mat.SetInt("_PreviewType", 0); }
				else { mat.EnableKeyword("_PREVIEW"); mat.SetInt("_PreviewType", (int)previewType); }
			}
		}
	}

	public static void DrawLayer (Material mat, Layout layout, int num)
	{
		layout.Par(54);
		layout.MatField<Texture>(mat, "_MainTex"+num, rect:layout.Inset(54f));
		layout.MatField<Texture>(mat, "_BumpMap"+num, rect:layout.Inset(54f));

		layout.cursor.y -= 54;
		layout.margin += 108;
		layout.MatField<Vector2>(mat, "_SpecParams"+num, "Metallic", fieldSize:0.67f, zwOfVector4:false);
		layout.MatField<Vector2>(mat, "_SpecParams"+num, "Gloss", fieldSize:0.67f, zwOfVector4:true);
		layout.margin -= 108;
	}



}
