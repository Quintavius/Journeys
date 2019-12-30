using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace Voxeland5.Interface
{
	public static class Icons 
	{
		[System.NonSerialized] private static readonly Dictionary<string, Texture2D> iconsCache = new Dictionary<string, Texture2D>();
		[System.NonSerialized] private static readonly Dictionary<string, GUIStyle> elementStyles = new Dictionary<string, GUIStyle>();

		public static Texture2D GetIcon (string textureName)
		/// Gets an icon from resourses, chaches it as texture
		{
			string nonProName = textureName;
			#if UNITY_EDITOR
			if (UnityEditor.EditorGUIUtility.isProSkin) textureName += "_pro";
			#endif
				
			Texture2D texture=null;
			if (!iconsCache.ContainsKey(textureName))
			{
				texture = Resources.Load(textureName) as Texture2D;
				if (texture==null) texture = Resources.Load(nonProName) as Texture2D; //trying to load a texture without _pro

				iconsCache.Add(textureName, texture);
			}
			else texture = iconsCache[textureName]; 
			return texture;
		}
	

		public static GUIStyle GetElementStyle (string textureName, int left=-1, int right=-1, int top=-1, int bottom=-1)
		{
			string origTexName = textureName;

			#if UNITY_EDITOR
			if (UnityEditor.EditorGUIUtility.isProSkin) textureName += "_pro";
			#endif

			GUIStyle elementStyle = null;
			if (!elementStyles.ContainsKey(textureName))
			{
				elementStyle = new GUIStyle();
				Texture2D tex = GetIcon(origTexName); 
				elementStyle.normal.background = tex;

				RectOffset borders = new RectOffset(
					left<0? tex.width/2 : left,
					right<0? tex.width/2 : right,
					top<0? tex.height/2 : top,
					bottom<0? tex.height/2 : bottom);

				elementStyle.border = borders;

				elementStyles.Add(textureName, elementStyle);
			}
			else elementStyle = elementStyles[textureName];

			return elementStyle;
		}
	}
}
