using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace Voxeland5.Interface
{
	public static class ScriptOps 
	{
		public static T ScriptableAsset<T> (
			T asset, 
			string label, System.Func<T> construct=null, 
			Action<object> onChange=null, //if asset re-assigned, created or reset
			string tooltip = null )  
				where T : ScriptableObject, ISerializationCallbackReceiver
		{
			Cell back = UI.active.Add(Layout.Horizontal);
			UI.Background(back);

			Cell labelBack = back.Add(Layout.Vertical, size:0.5f);
			Cell labelCell = labelBack.Add(Layout.Full, size:UI.lineHeight);
			UI.Label(label, cell:labelCell);

			Cell fieldBack = back.Add(Layout.Vertical);
			Cell fieldCell = fieldBack.Add(Layout.Full, size:UI.lineHeight);
			T newAsset = (T)UI.Field(asset, cell:fieldCell);

			Cell createCell = fieldBack.Add(Layout.Full, size:UI.lineHeight);
			if (asset==null)
			{
				if (UI.Button(false, "Create", cell:createCell)) 
				{
					if (construct==null) newAsset = ScriptableObject.CreateInstance<T>();
					else newAsset = construct();
				}
			}
			else 
			{
				if (UI.Button(false, "Reset", cell:createCell)) 
				{
					#if UNITY_EDITOR
					if (UnityEditor.EditorUtility.DisplayDialog("Reset to Default", "This will remove all of the data and create a default one. Are you sure you wsih to continue?", "Reset to Default", "Cancel"))
					#endif
					{
						if (construct==null) newAsset = ScriptableObject.CreateInstance<T>();
						else newAsset = construct();
					}
				}
			}

			#if UNITY_EDITOR
			Cell storeCell = fieldBack.Add(Layout.Full, size:UI.lineHeight);
			if (asset==null || !UnityEditor.AssetDatabase.Contains(asset))
			{ 
				if (UI.Button(false, "Store to Assets", cell:storeCell, disabled:asset==null)) newAsset = SaveAsset(asset);
			}
			else 
			{ 
				if (UI.Button(false, "Release", cell:storeCell, disabled:asset==null)) newAsset = ReleaseAsset(asset);
			}
			#endif

			Cell saveCopyCell = fieldBack.Add(Layout.Full, size:UI.lineHeight);
			if (UI.Button(false, "Save as Copy", cell:saveCopyCell, disabled:asset==null))
			{
				T copyAsset = ScriptableObject.Instantiate<T>(asset);
				SaveAsset(copyAsset);
			}

			if (newAsset != asset) 
			{
				if (onChange!=null) onChange(newAsset);
				//RecordUndo();
			}

			return newAsset;
		}

		public static T SaveAsset<T> (T asset, string savePath=null, string filename="Data", string type="asset", string caption="Save Data as Unity Asset") where T : UnityEngine.Object
		{
			#if UNITY_EDITOR
			if (savePath==null) savePath = UnityEditor.EditorUtility.SaveFilePanel(
				caption,
				"Assets",
				filename, 
				type);
			if (savePath!=null && savePath.Length!=0)
			{
				savePath = savePath.Replace(Application.dataPath, "Assets");

				UnityEditor.AssetDatabase.CreateAsset(asset, savePath);
				if (asset is ISerializationCallbackReceiver) ((ISerializationCallbackReceiver)asset).OnBeforeSerialize();
				UnityEditor.AssetDatabase.SaveAssets();
			}
			#endif

			return asset;
		} 

		public static void SaveRawBytes (byte[] bytes, string savePath=null, string filename="Data", string type="asset")
		{
			#if UNITY_EDITOR
			if (savePath==null) savePath = UnityEditor.EditorUtility.SaveFilePanel(
				"Save Data as Unity Asset",
				"Assets",
				filename, 
				type);
			if (savePath!=null && savePath.Length!=0)
			{
				savePath = savePath.Replace(Application.dataPath, "Assets");
				System.IO.File.WriteAllBytes(savePath, bytes);
			}
			#endif
		}

		public static T ReleaseAsset<T> (T asset, string savePath=null) where T : ScriptableObject, ISerializationCallbackReceiver
		{
			#if UNITY_EDITOR
			asset = ScriptableObject.Instantiate<T>(asset); 
			#endif

			return asset;
		}

		public static T LoadAsset<T> (string label="Load Unity Asset", string[] filters=null) where T : UnityEngine.Object
		{
			#if UNITY_EDITOR
			if (filters==null && typeof(T).IsSubclassOf(typeof(Texture))) filters = new string[] { "Textures", "PSD,TIFF,TIF,JPG,TGA,PNG,GIF,BMP,IFF,PICT" };
			if (filters==null && typeof(T) == typeof(Transform)) filters = new string[] { "Meshes", "FBX,DAE,3DS,DXF,OBJ,SKP" };
			ArrayTools.Add(ref filters, "All files");
			ArrayTools.Add(ref filters, "*");

			string path= UnityEditor.EditorUtility.OpenFilePanelWithFilters(label, "Assets", filters);
			if (path!=null && path.Length!=0)
			{
				path = path.Replace(Application.dataPath, "Assets");
				T asset = (T)UnityEditor.AssetDatabase.LoadAssetAtPath(path, typeof(T));
				return asset;
			}
			#endif
			return null;
		}
	}
}
