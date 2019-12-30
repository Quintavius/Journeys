using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace Voxeland5.Interface
{
	public static class Styles
	{
			public static int defaultFontSize = 11;

			public static GUIStyle label = null;
			public static GUIStyle smallLabel = null;
			public static GUIStyle boldLabel = null;
			public static GUIStyle centerLabel = null;
			public static GUIStyle url = null;
			
			public static GUIStyle foldout = null;
			public static GUIStyle field = null;
			public static GUIStyle button = null;
			
			public static GUIStyle enumMain = null;
			public static GUIStyle enumClose = null;
			public static GUIStyle enumFar = null;
			
			public static GUIStyle toolbar = null;
			public static GUIStyle toolbarButton = null;
			
			public static GUIStyle helpBoxStyle = null;


			public static void CheckInitStyles ()
			{
				#if UNITY_EDITOR
				
				if (label != null && field !=null && foldout != null) return; //let's say initialized

				label = new GUIStyle(UnityEditor.EditorStyles.label); 
				label.active.textColor = Color.black; 
				label.focused.textColor = label.active.textColor = label.normal.textColor; //no focus
				
				smallLabel = new GUIStyle(UnityEditor.EditorStyles.label); 
				smallLabel.active.textColor = Color.black; 
				smallLabel.focused.textColor = smallLabel.active.textColor = smallLabel.normal.textColor; //no focus
				smallLabel.fontSize = (int)(label.fontSize*0.8f);

				boldLabel = new GUIStyle(UnityEditor.EditorStyles.label); 
				boldLabel.fontStyle = FontStyle.Bold; 
				boldLabel.focused.textColor = boldLabel.active.textColor = boldLabel.normal.textColor;

				centerLabel = new GUIStyle(UnityEditor.EditorStyles.label); 
				centerLabel.alignment = TextAnchor.MiddleCenter;
				centerLabel.focused.textColor = centerLabel.active.textColor = centerLabel.normal.textColor;
					
				url = new GUIStyle(UnityEditor.EditorStyles.label); 
				url.normal.textColor = new Color(0.3f, 0.5f, 1f); 

				foldout = new GUIStyle(UnityEditor.EditorStyles.foldout);  
				foldout.fontStyle = FontStyle.Bold; 
				foldout.focused.textColor = Color.black; 
				foldout.active.textColor = Color.black; 
				foldout.onActive.textColor = Color.black;

				button = new GUIStyle("Button"); 
					
				toolbar = new GUIStyle(UnityEditor.EditorStyles.toolbar);
				toolbarButton = new GUIStyle(UnityEditor.EditorStyles.toolbarButton);  
				helpBoxStyle = new GUIStyle(UnityEditor.EditorStyles.helpBox);  

				enumClose = new GUIStyle(UnityEditor.EditorStyles.popup);
				enumMain = enumClose;
				enumFar = new GUIStyle(UnityEditor.EditorStyles.miniButton); 
				enumFar.alignment = TextAnchor.MiddleLeft;

				field = new GUIStyle(UnityEditor.EditorStyles.numberField);
				field.normal.background = Icons.GetIcon("DPLayout_Field"); //Resources.Load("DPLayout_Field") as Texture2D;
				field.border = new RectOffset(4,4,4,4);
				#endif
			}
		
			public static void ResizeStyles (float zoom)
			{
				#if UNITY_EDITOR
				
				int fontSize = Mathf.RoundToInt(defaultFontSize * zoom);
				int fieldFontSize = Mathf.RoundToInt(14 * zoom * 0.8f);
				
				if (label.fontSize != fontSize  ||  field.fontSize != fieldFontSize)
				{
					label.fontSize = fontSize;
					smallLabel.fontSize = Mathf.RoundToInt(fontSize*0.8f);
					boldLabel.fontSize = fontSize;
					centerLabel.fontSize = fontSize;
					url.fontSize = fontSize;
					
					foldout.fontSize = fontSize;
					field.fontSize = fieldFontSize;
					button.fontSize = fontSize;
					
					enumMain.fontSize = fontSize;
					enumClose.fontSize = fontSize;
					enumFar.fontSize = fontSize;
					
					toolbar.fontSize = fontSize;
					toolbarButton.fontSize = fontSize;
					
					helpBoxStyle.fontSize = fontSize;
				}

				#endif
			}
	}
}