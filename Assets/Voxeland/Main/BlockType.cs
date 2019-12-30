using UnityEngine;
using System.Collections;

using Voxeland5;

namespace Voxeland5
{
	public interface ITypeList
	{
		void OnAddBlock (int n, Voxeland voxeland);
		void OnRemoveBlock (int n, Voxeland voxeland);
		void OnSwitchBlock (int o, int n, Voxeland voxeland);
		void ApplyToMaterial (Voxeland voxeland, bool rebuildTextures);
	}

	[System.Serializable]
	public class TypeList<T> where T : new()
	{
		public T[] array = new T[0];
		public int selected = -1;
		public bool expanded = false;

		//switching internal data types on change
		public bool changeBlockData = false;

		public void OnAddBlock (int n, object voxelandObj) 
		{
			array[n] = new T();

			Voxeland voxeland = (Voxeland)voxelandObj;
			ApplyToMaterial(voxeland, true);

			if (changeBlockData && voxeland != null) 
			{
				if (typeof(T) != typeof(GrassType)) voxeland.data.InsertType((byte)n); //syncing blocks
				else voxeland.data.InsertGrassType((byte)n); //syncing grass
				voxeland.Rebuild();
			}
		}
		public void OnRemoveBlock (int n, object voxelandObj) 
		{
			Voxeland voxeland = (Voxeland)voxelandObj;

			if (changeBlockData && voxelandObj!=null) 
			{
				if (typeof(T) != typeof(GrassType)) voxeland.data.RemoveType((byte)n); //syncing blocks
				else voxeland.data.RemoveGrassType((byte)n); //syncing grass
				voxeland.Rebuild();
			}

			if (voxeland!=null) ApplyToMaterial(voxeland, true);
		}
		public void OnSwitchBlock (int o, int n, object voxelandObj)
		{
			Voxeland voxeland = (Voxeland)voxelandObj;

			if (changeBlockData && voxelandObj!=null) 
			{
				if (typeof(T) != typeof(GrassType)) voxeland.data.SwitchType((byte)o, (byte)n); //syncing blocks
				else voxeland.data.SwitchGrassType((byte)o, (byte)n); //syncing grass
				voxeland.Rebuild();
			}

			if (voxeland!=null) ApplyToMaterial(voxeland, true);
		}


		public virtual void ApplyToMaterial (Voxeland voxeland, bool rebuildTextures) { }
	}

	#region Land Blocks

		[System.Serializable]
		public class BlockType //aka LandBlock
		{
			public string name = "Block";

			//[System.NonSerialized] public Texture2D icon; //should be an editor-only, but it could be handy to give an access to display blocks in playmode
			
			public Color color = new Color(0.77f, 0.77f, 0.77f, 1f);
			public Texture2D mainTex;
			public Texture2D bumpMap;
			#if RTP
			public Texture2D heightMap;
			#endif

			public float height = 1;
			public float specularV = 0.05f;
			public float specularM = 0.25f;
			public float glossinessV = 0.1f;
			public float glossinessM = 0.7f;
			public bool  grass = true;
			//public Color grassTint = Color.white;
			//public float smooth = 1f; //not used 

			public bool filledAmbient = true;

			public bool filledPrefab = false;
			public Transform[] prefabs = new Transform[0]; //new ObjectPool[0];
		}


		[System.Serializable] public class LandTypeList : Layout.ILayered
		{ 
			public BlockType[] array = new BlockType[0];
			
			//storing texture arrays 
			public bool textureArrays = false;
			public Texture2DArray mainTexArray; 
			public Texture2DArray bumpMapArray; 
			[System.NonSerialized] public TextureArrayDecorator mainTexArrDec;
			[System.NonSerialized] public TextureArrayDecorator bumpTexArrDec;

			//megasplat
			public bool megaClusters = false;
			#if __MEGASPLAT__
			public MegaSplatTextureList megaTexList;
			#endif

			//shared material properties
			public bool anisotropicFiltration = true;
			public float tile = 0.1f;
			public float mips = -0.1f;
			public float ambientOcclusion = 1;
			public float blendMapFactor = 3;
			public float blendCrisp = 2;
			public enum PreviewType { disabled=0, albedo=1, ambient=2, normals=3, specular=4, smoothness=5, directions=6, vertexBlends=7, finalBlends=8, worldPos=9, define=10 };
			public PreviewType previewType = PreviewType.disabled;

			//far
			public bool farEnabled = false;
			public float farTile = 0.01f;
			public float farStart = 10f;
			public float farEnd = 100f;

			//horizon
			public float horizonTile = 0.01f;
			public float horizonBorderLower = 2f;

			//gui
			public bool showShared = true;
			public bool showFar = false;
			public bool showHorizon = false;

			//switching internal data types on change
			public bool changeBlockData = false;


			public void ApplyToMaterial (Material mat, bool horizon=false)
			{
				mat.CheckSetFloat("_Tile", tile);
				mat.CheckSetFloat("_Mips", mips);
				mat.CheckSetFloat("_AmbientOcclusion", ambientOcclusion);
				mat.CheckSetFloat("_BlendMapFactor", blendMapFactor);
				mat.CheckSetFloat("_BlendCrisp", blendCrisp);

				if (!horizon)
				{
					if (farEnabled && !mat.IsKeywordEnabled("_DOUBLELAYER")) mat.EnableKeyword("_DOUBLELAYER");
					if (!farEnabled && mat.IsKeywordEnabled("_DOUBLELAYER")) mat.DisableKeyword("_DOUBLELAYER");
					mat.CheckSetFloat("_FarTile", farTile);
					mat.CheckSetFloat("_FarStart", farStart);
					mat.CheckSetFloat("_FarEnd", farEnd);
				}

				else
				{
					if (mat.IsKeywordEnabled("_DOUBLELAYER")) mat.DisableKeyword("_DOUBLELAYER");
					if (mat.IsKeywordEnabled("_TRIPLANAR")) mat.DisableKeyword("_TRIPLANAR");
					if (!mat.IsKeywordEnabled("_HORIZON")) mat.EnableKeyword("_HORIZON");

					mat.CheckSetFloat("_Tile", horizonTile);
					mat.CheckSetFloat("_HorizonBorderLower", horizonBorderLower);

					Voxeland v = Voxeland.instances.Any();
					Horizon h = v.horizon;
					if (h != null) h.meshRenderer.sharedMaterial = v.farMaterial;
				}

				if (textureArrays)
				{
					if (!mat.IsKeywordEnabled("_TEX2DARRAY")) 
						mat.EnableKeyword("_TEX2DARRAY");

					//adding texture arrays
					mat.SetTexture("_MainTexArr", mainTexArray); //HasProperty returns false for texture arrays
					mat.SetTexture("_BumpMapArr", bumpMapArray);

					mat.SetTexture("_Diffuse", mainTexArray);  //for MegaSplat shaders
					mat.SetTexture("_Normal", bumpMapArray);

					if (mainTexArray != null && (mainTexArray.anisoLevel==1) != anisotropicFiltration)  mainTexArray.anisoLevel = anisotropicFiltration? 1 : 0;
					if (bumpMapArray != null && (bumpMapArray.anisoLevel==1) != anisotropicFiltration)  bumpMapArray.anisoLevel = anisotropicFiltration? 1 : 0;


					//removing non-array textures
					for (int i=0; i<array.Length; i++)
					{
						mat.CheckSetTexture("_MainTex"+i, null);
						mat.CheckSetTexture("_BumpMap"+i, null);
					}
				}
				else
				{
					if (mat.IsKeywordEnabled("_TEX2DARRAY")) mat.DisableKeyword("_TEX2DARRAY");

					//adding simple textures
					for (int i=0; i<array.Length; i++)
					{
						mat.CheckSetTexture("_MainTex"+i, array[i].mainTex);
						mat.CheckSetTexture("_BumpMap"+i, array[i].bumpMap);
					}

					//removing texture arrays
					mat.SetTexture("_MainTexArr", null); //HasProperty returns false for texture arrays
					mat.SetTexture("_BumpMapArr", null);
				}
				
				//per-layer params
				for (int i=0; i<array.Length; i++)
				{
					mat.CheckSetFloat("_Height"+i, array[i].height);
					mat.CheckSetVector("_SpecParams"+i, new Vector4(
						array[i].specularV, 
						array[i].specularM, 
						array[i].glossinessV, 
						array[i].glossinessM ) );
				}

				//preview
				if (mat.HasProperty("_PreviewType")) 
				{
					if (previewType == PreviewType.disabled && mat.IsKeywordEnabled("_PREVIEW")) mat.DisableKeyword("_PREVIEW");
					else if (previewType != PreviewType.disabled && !mat.IsKeywordEnabled("_PREVIEW")) mat.EnableKeyword("_PREVIEW");

					mat.SetInt("_PreviewType", (int)previewType);
				}
			}

			public void OnLayerHeader (Layout layout, int num)
			{
				BlockType block = array[num];

				layout.margin += 5; layout.rightMargin += 5;
				layout.Par(32, padding:0);

				//icon
				int iconWidth = (int)layout.cursor.height;
				Rect iconRect = layout.Inset(iconWidth);
				iconRect = iconRect.Extend(-3);

				//icon: texarr preview
				if (textureArrays)
				{
					if (mainTexArray != null  &&  bumpMapArray != null)  //always check diffuse and bump together
					{
						#if UNITY_EDITOR
						if (num < mainTexArray.depth) 
							layout.TextureIcon(mainTexArrDec.GetPreview(num), iconRect);
						#endif
					}
				}

				//icon: megasplat cluster preview
				#if __MEGASPLAT__
				else if (megaClusters)
				{
					if (megaTexList != null)
					{
						if (megaTexList.clusters[num].previewTex != null) layout.TextureIcon(megaTexList.clusters[num].previewTex, iconRect);
						block.name = megaTexList.clusters[num].name;
					}
				}
				#endif

				//icon: RTP
				#if RTP
				else if (Voxeland.instances.Any().material!=null && Voxeland.instances.Any().material.HasProperty("_SplatA"+num) && num<4) //if RTP material applied
				{
					layout.TextureIcon((Texture2D)Voxeland.instances.Any().material.GetTexture("_SplatA"+num), iconRect);
				}
				#endif

				//icon: standard texture
				else
				{
					if (block.mainTex != null) layout.TextureIcon(block.mainTex, iconRect);
				}

				//label
				Rect labelRect = layout.Inset(layout.field.width - iconWidth - 18 - layout.margin-layout.rightMargin);
				labelRect.y += (labelRect.height-18)/2f; labelRect.height = 18;
				block.name = layout.EditableLabel(block.name, labelRect);

				//drawing foldout where the cursor left

				layout.margin -= 5; layout.rightMargin -= 5;
			}

			public void OnLayerGUI (Layout layout, int num)
			{
				layout.margin += 5; layout.rightMargin += 5;
				BlockType block = array[num];

				//if (!selected) { layout.Label(block.name); return; }

				//name = layout.Field(name, style:layout.labelStyle); //layout.Field(name);

				if (textureArrays)
				{
					if (mainTexArray == null || bumpMapArray == null)
					{
						layout.Par(50);
						layout.Label("Diffuse AND Bump texture arrays not assigned. Assign proper texture arrays in Texture Array foldout or turn Texture Arrays feature off.", rect:layout.Inset(), helpbox:true);
					}

					else
					{
						#if UNITY_EDITOR
						Texture2D newMainSrc = layout.Field(mainTexArrDec.GetSource(num, isAlpha:false), "Diffuse Source", fieldSize:0.6f);
						if (layout.lastChange) mainTexArrDec.SetSource(newMainSrc, num, isAlpha:false);

						Texture2D newAlfSrc = layout.Field(mainTexArrDec.GetSource(num, isAlpha:true), "Height Source", fieldSize:0.6f, disabled:newMainSrc==null);
						if (layout.lastChange) mainTexArrDec.SetSource(newAlfSrc, num, isAlpha:true);

						Texture2D newBumpSrc = layout.Field(bumpTexArrDec.GetSource(num, isAlpha:false), "Normal Source", fieldSize:0.6f);
						if (layout.lastChange) 
							bumpTexArrDec.SetSource(newBumpSrc, num, isAlpha:false);
						#endif
					}
				}

				else
				{
					block.mainTex = layout.Field(block.mainTex, "Diffuse/Height");
					block.bumpMap = layout.Field(block.bumpMap, "Normal");
				}

				layout.Par(5);
				layout.fieldSize = 0.67f;
				layout.Field(ref block.height, "Height", fieldSize:0.67f);
				
				layout.Par(); 
				layout.Label("Specular", rect:layout.Inset(1-layout.fieldSize));
				layout.Field(ref block.specularV, "V", rect:layout.Inset(layout.fieldSize/2), fieldSize:0.81f);
				layout.Field(ref block.specularM, "M", rect:layout.Inset(layout.fieldSize/2), fieldSize:0.81f);

				layout.Par(); 
				layout.Label("Gloss", rect:layout.Inset(1-layout.fieldSize));
				layout.Field(ref block.glossinessV, "V", rect:layout.Inset(layout.fieldSize/2), fieldSize:0.81f);
				layout.Field(ref block.glossinessM, "M", rect:layout.Inset(layout.fieldSize/2), fieldSize:0.81f);

				layout.Par(5);
				layout.margin -= 5; layout.rightMargin -= 5;
			}

			public int selected {get;set;}
			public bool expanded {get;set;}

			public void AddLayer (int addPos)
			{
				ArrayTools.Insert(ref array, addPos, new BlockType() { name="Land Block" });
				if (changeBlockData) Voxeland.current.data.InsertType((byte)addPos);

				//saving texture array guids
				#if UNITY_EDITOR
				if (textureArrays)  //based on TextureArrayInspector
				{
					mainTexArrDec.Insert(null, null, addPos);
					bumpTexArrDec.Insert(null, null, addPos);
					UnityEditor.AssetDatabase.Refresh();
				}
				#endif
			}
			public void RemoveLayer (int num) 
			{ 
				ArrayTools.RemoveAt(ref array, num);
				if (changeBlockData) Voxeland.current.data.RemoveType((byte)num);

				#if UNITY_EDITOR
				if (textureArrays)
				{
					mainTexArrDec.RemoveAt(num);
					bumpTexArrDec.RemoveAt(num);
					UnityEditor.AssetDatabase.Refresh();
				}
				#endif
			}
			public void SwitchLayers (int num1, int num2) 
			{ 
				ArrayTools.Switch(array, num1, num2); 
				if (changeBlockData) Voxeland.current.data.SwitchType((byte)num1, (byte)num2);

				#if UNITY_EDITOR
				if (textureArrays)
				{
					mainTexArrDec.Switch(num1, num2);
					bumpTexArrDec.Switch(num1, num2);
					UnityEditor.AssetDatabase.Refresh();
				}
				#endif
			}


			public static void EqualizeResDialog  (ref Texture2DArray texArr, ref Texture2D tex)
			{
				#if UNITY_EDITOR
				int resizeChoise = UnityEditor.EditorUtility.DisplayDialogComplex(
					"Resize the texture array?",
					"Loaded texture size (" + tex.width + "," + tex.height + ") does not match the texture array size (" + texArr.width + "," + texArr.height + ")." +
					"Do you want to resize the texture array?",
					ok: "Yes, use " + tex.width + "," + tex.height + "",
					cancel: "No, use " + texArr.width + "," + texArr.height + "", //cancel and alt are switched
					alt: "Cancel" );

				switch (resizeChoise)
				{
					case 0: texArr = texArr.ResizedClone(tex.width, tex.height); break;
					case 1: tex = tex.ResizedClone(texArr.width, texArr.height); break;
					case 2: return;
					default:return;
				}
				#endif
			}
		}

	#endregion


	#region Constructor Blocks

		[System.Serializable]
		public class ConstructorType
		{
			public string name = "Constructor";
		}

		[System.Serializable] public class ConstructorTypeList
		{ 
			public ConstructorType[] array = new ConstructorType[0];

			public int selected = -1;
			public bool expanded = false;

			public bool changeBlockData = false; //switching internal data types on change
		}

	#endregion


	#region Object Blocks :

		[System.Serializable]
		public class ObjectType
		{
			public string name = "Object";

			public Transform[] prefabs = new Transform[1];
		}

		[System.Serializable] public class ObjectTypeList : Layout.ILayered
		{ 
			public ObjectType[] array = new ObjectType[0];

			public int selected {get;set;}
			public bool expanded {get;set;}
			public bool changeBlockData = false; //switching internal data types on change

			public void OnLayerHeader (Layout layout, int num)
			{
				ObjectType block = array[num];

				layout.margin += 5; layout.rightMargin += 5;
				layout.Par(24, padding:0);

				//label
				Rect labelRect = layout.Inset(layout.field.width - 18 - layout.margin-layout.rightMargin);
				labelRect.y += (labelRect.height-18)/2f; labelRect.height = 18;
				block.name = layout.EditableLabel(block.name, labelRect);

				//drawing foldout where the cursor left

				layout.margin -= 5; layout.rightMargin -= 5;
			}

			public void OnLayerGUI (Layout layout, int num)
			{
				ObjectType block = array[num];
				layout.margin += 5; layout.rightMargin += 5;

				int numToRemove = -1;
				for (int i=0; i<block.prefabs.Length; i++)
				{
					layout.Par();
					layout.Field(ref block.prefabs[i], rect:layout.Inset(layout.field.width-20-layout.margin-layout.rightMargin));
					if (layout.Button(icon:"DPLayout_Remove", rect:layout.Inset(20))) numToRemove = i;
					layout.Par(3);
				}

				if (numToRemove >= 0) ArrayTools.RemoveAt(ref block.prefabs, numToRemove);

				layout.Par();
				layout.Inset(layout.field.width-20-layout.margin-layout.rightMargin);
				if (layout.Button(icon:"DPLayout_Add", rect:layout.Inset(20))) ArrayTools.Add(ref block.prefabs, null);

				layout.Par(4);
				layout.margin -= 5; layout.rightMargin -= 5;
			}

			public void AddLayer (int addPos) 
			{ 
				ArrayTools.Insert(ref array, addPos, new ObjectType() { name="Objects Block" });
			}
			public void RemoveLayer (int num) 
			{ 
				ArrayTools.RemoveAt(ref array, num);
			}
			public void SwitchLayers (int num1, int num2) 
			{ 
				ArrayTools.Switch(array, num1, num2); 
			}
		}

	#endregion

	#region Grass Blocks

		[System.Serializable]
		public class GrassType //: IBlockType
		{
			public string name = "Grass";
			public Texture2D icon = null;

			public MeshWrapper[] meshes; 

			public Texture2D mainTex; 
			public Texture2D bumpMap;
			public Texture2D specSmoothMap; 
			public Texture2D sssVanishMap;

			
			public float height = 1;
			public float width = 1;
			public float elevation = 0;
			public float incline = 0.1f;
			public float random = 0.1f;
			public Vector4 uvRect = new Vector4(0,0,1,1);

			public bool onlyTopLevel = true;
			public bool normalsFromTerrain = false;
			public float heightRandom = 0.1f;
			public float uniformSizeRandom = 0.1f;
		
			public Color colorTint = Color.white;	
			public float specularV = 0f;
			public float specularM = 1f;
			public float glossinessV = 0f;
			public float glossinessM = 1f;	
		}

		[System.Serializable] 
		public class GrassTypeList : Layout.ILayered
		{ 
			public GrassType[] array = new GrassType[0];

			public bool textureArrays = false;
			public Texture2DArray mainTexArray; 
			public Texture2DArray bumpMapArray;
			public Texture2DArray specSmoothMapArray; 
			public Texture2DArray sssVanishMapArray;

			public bool atlas = false;
			public Texture2D mainTex; 
			public Texture2D bumpMap;
			public Texture2D specSmoothMap; 
			public Texture2D sssVanishMap;

			public bool anisotropicFiltration = true;
			public enum Culling { Off=0, Forward=1, Backface=2 };
			public Culling culling;
			public float cutoff = 0.33f;
			public float ambientPower = 0.25f;
			public float mipFactor = -0.1f;

			public float vanishAngle = 0.2f;
			public float appearAngle = 0.3f;

			public float sss = 1.5f;
			public float saturation = 4;
			public float sssDistance = 100;

			public float shakingAmplitude = 0.2f;
			public float shakingFrequency = 0.2f;

			public Texture2D windTex = null;
			public float windSize = 50;
			public float windSpeed = 5f;
			public float windStrength = 0.33f;

			//gui
			public bool showShared = true;
			public bool showSSS = false;
			public bool showWind = false;

			public int selected {get;set;}
			public bool expanded {get;set;}
			public bool changeBlockData = false; //switching internal data types on change

			public void ApplyToMaterial (Material mat)
			{
				mat.CheckSetInt("_Culling", (int)culling);
				mat.CheckSetFloat("_Cutoff", cutoff);
				mat.CheckSetFloat("_AmbientPower", ambientPower);
				mat.CheckSetFloat("_MipFactor", mipFactor);

				mat.CheckSetFloat("_VanishAngle", vanishAngle);
				mat.CheckSetFloat("_AppearAngle", appearAngle);

				mat.CheckSetFloat("_SSS", sss);
				mat.CheckSetFloat("_Saturation", saturation);
				mat.CheckSetFloat("_SSSDistance", sssDistance);

				mat.CheckSetFloat("_ShakingAmplitude", shakingAmplitude);
				mat.CheckSetFloat("_ShakingFrequency", shakingFrequency);

				mat.CheckSetTexture("_WindTex", windTex);
				mat.CheckSetFloat("_WindSize", windSize);
				mat.CheckSetFloat("_WindSpeed", windSpeed);
				mat.CheckSetFloat("_WindStrength", windStrength);

				//setting textures
				//not required! arrays and per-layer tex set to null when enabling atlas, etc.
				if (textureArrays)
				{
					if (!mat.IsKeywordEnabled("_TEX2DARRAY")) 
						mat.EnableKeyword("_TEX2DARRAY");
					
					//assigning arrays

					//removing per-layer textures
					for (int i=0; i<array.Length; i++)
					{
						mat.CheckSetTexture("_MainTex"+i, null);
						mat.CheckSetTexture("_BumpMap"+i, null);
						mat.CheckSetTexture("_SpecGlossMap"+i, null);
						mat.CheckSetTexture("_SSSVanishMap"+i, null);
					}

					//removing atlas
					mat.CheckSetTexture("_MainTex", null);
					mat.CheckSetTexture("_BumpMap", null);
					mat.CheckSetTexture("_SpecGlossMap", null);
					mat.CheckSetTexture("_SSSVanishMap", null);
				}
				else if (atlas)
				{
					if (!mat.IsKeywordEnabled("_ATLAS")) 
						mat.EnableKeyword("_ATLAS");

					//removing arrays
					mat.SetTexture("_MainTexArr", null); //HasProperty returns false for texture arrays
					mat.SetTexture("_BumpMapArr", null);
					mat.SetTexture("_SpecGlossMapArr", null);
					mat.SetTexture("_SSSVanishMapArr", null);

					//removing per-layer textures
					for (int i=0; i<array.Length; i++)
					{
						mat.CheckSetTexture("_MainTex"+i, null);
						mat.CheckSetTexture("_BumpMap"+i, null);
						mat.CheckSetTexture("_SpecGlossMap"+i, null);
						mat.CheckSetTexture("_SSSVanishMap"+i, null);
					}

					//atlas
					mat.CheckSetTexture("_MainTex", mainTex);
					mat.CheckSetTexture("_BumpMap", bumpMap);
					mat.CheckSetTexture("_SpecGlossMap", specSmoothMap);
					mat.CheckSetTexture("_SSSVanishMap", sssVanishMap);
				}
				else
				{
					if (mat.IsKeywordEnabled("_TEX2DARRAY")) mat.DisableKeyword("_TEX2DARRAY");
					if (mat.IsKeywordEnabled("_ATLAS")) mat.DisableKeyword("_ATLAS");
					
					//removing arrays
					mat.SetTexture("_MainTexArr", null); //HasProperty returns false for texture arrays
					mat.SetTexture("_BumpMapArr", null);
					mat.SetTexture("_SpecGlossMapArr", null);
					mat.SetTexture("_SSSVanishMapArr", null);

					//assigning per-layer textures
					for (int i=0; i<array.Length; i++)
					{
						mat.CheckSetTexture("_MainTex"+i, array[i].mainTex);
						mat.CheckSetTexture("_BumpMap"+i, array[i].bumpMap);
						mat.CheckSetTexture("_SpecGlossMap"+i, array[i].specSmoothMap);
						mat.CheckSetTexture("_SSSVanishMap"+i,  array[i].sssVanishMap);
					}

					//removing atlas
					mat.CheckSetTexture("_MainTex", null);
					mat.CheckSetTexture("_BumpMap", null);
					mat.CheckSetTexture("_SpecGlossMap", null);
					mat.CheckSetTexture("_SSSVanishMap", null);

				}

				//per-layer params
				for (int i=0; i<array.Length; i++)
				{
					mat.CheckSetColor("_Color"+i, array[i].colorTint);
					mat.CheckSetVector("_SpecParams"+i, new Vector4(
						array[i].specularV, 
						array[i].specularM, 
						array[i].glossinessV, 
						array[i].glossinessM ) );
					mat.CheckSetVector("_UVRect"+i, array[i].uvRect);	
				} 
			}


			public void OnLayerHeader (Layout layout, int num)
			{
				GrassType block = array[num];

				layout.margin += 5; layout.rightMargin += 5;
				layout.Par(32, padding:0);

				//checking icon
				if (block.icon == null)
				{
					if (textureArrays)
					{
						if(mainTexArray != null) 
						{
							block.icon = new Texture2D(64, 64);
							mainTexArray.FillTexture(block.icon, num); 
							#if UNITY_EDITOR
							if (UnityEditor.PlayerSettings.colorSpace == ColorSpace.Linear && block.icon!=null) block.icon.ApplyGamma();
							#endif
						}
					}
					else block.icon = block.mainTex;
				}

				//icon
				int iconWidth = (int)layout.cursor.height;
				Rect iconRect = layout.Inset(iconWidth);
				iconRect = iconRect.Extend(-3);
				layout.Icon(block.icon, iconRect);
				layout.Element(num == selected? "DPLayout_LayerIconActive" : "DPLayout_LayerIconInactive", iconRect, new RectOffset(6,6,6,6), null);

				//label
				Rect labelRect = layout.Inset(layout.field.width - iconWidth - 18 - layout.margin-layout.rightMargin);
				labelRect.y += (labelRect.height-18)/2f; labelRect.height = 18;
				block.name = layout.EditableLabel(block.name, labelRect);

				//drawing foldout where the cursor left

				layout.margin -= 5; layout.rightMargin -= 5;
			}

			public void OnLayerGUI (Layout layout, int num)
			{
				layout.margin += 5; layout.rightMargin += 5;
				
				GrassType block = array[num];

				layout.Par();
				layout.Label("Mesh", rect:layout.Inset(0.6f));
				if (layout.Button("Load", rect:layout.Inset(0.4f)))
				{
					Mesh mesh = null;
					Transform tfm = layout.LoadAsset<Transform>("Load Grass Mesh");
					if (tfm != null)
					{
						MeshFilter filter = tfm.GetComponent<MeshFilter>();
						if (filter != null)
						{
							mesh = filter.sharedMesh;
							if (mesh != null)
							{
								block.meshes = new MeshWrapper[4];
								for (int i=0; i<4; i++)
								{
									block.meshes[i] = new MeshWrapper();
									block.meshes[i].ReadMesh(mesh);
									block.meshes[i].RotateMirror(i*90, false);
								}
							}
						}
					}
					else if (mesh == null) block.meshes = new MeshWrapper[0];
				}
				layout.Par(5);

				if (!textureArrays && !atlas)
				{
					block.mainTex = layout.Field(block.mainTex, "Albedo/Alpha");
					block.bumpMap = layout.Field(block.bumpMap, "Normal");
					block.specSmoothMap = layout.Field(block.specSmoothMap, "Spec/Gloss");
					block.sssVanishMap = layout.Field(block.sssVanishMap, "SSS");
				}


				layout.fieldSize = 0.3f;
				layout.Par(5);
				layout.Toggle(ref block.onlyTopLevel, "Only Top Level");
				layout.Toggle(ref block.normalsFromTerrain, "Take Terrain Normals");
				layout.Field(ref block.uniformSizeRandom, "Size Random (Uniform)");
				layout.Field(ref block.heightRandom, "Height Random");

				layout.fieldSize = 0.6f;
				Vector2 uvOffset = new Vector2(block.uvRect.x, block.uvRect.y);
				Vector2 uvSize = new Vector2(block.uvRect.z, block.uvRect.w);
				layout.Label("UV:");
				layout.Field(ref uvOffset, "Offset");
				layout.Field(ref uvSize, "Size");
				block.uvRect = new Vector4(uvOffset.x, uvOffset.y, uvSize.x, uvSize.y);

				layout.Par(5);
				layout.Field(ref block.colorTint, "Color Tint");

				layout.Par(); 
				layout.Label("Specular", rect:layout.Inset(1-layout.fieldSize));
				layout.Field(ref block.specularV, "V", rect:layout.Inset(layout.fieldSize/2), fieldSize:0.81f);
				layout.Field(ref block.specularM, "M", rect:layout.Inset(layout.fieldSize/2), fieldSize:0.81f);

				layout.Par(); 
				layout.Label("Gloss", rect:layout.Inset(1-layout.fieldSize));
				layout.Field(ref block.glossinessV, "V", rect:layout.Inset(layout.fieldSize/2), fieldSize:0.81f);
				layout.Field(ref block.glossinessM, "M", rect:layout.Inset(layout.fieldSize/2), fieldSize:0.81f);

				layout.Par(5); 
				layout.margin -= 5; layout.rightMargin -= 5;
			}

			public void AddLayer (int addPos) 
			{ 
				ArrayTools.Insert(ref array, addPos, new GrassType() { name="Grass Block" });
				if (mainTexArray!=null) TextureArrayTools.Insert(ref mainTexArray, addPos, null);
				if (bumpMapArray!=null) TextureArrayTools.Insert(ref bumpMapArray, addPos, null);
				if (specSmoothMapArray!=null) TextureArrayTools.Insert(ref specSmoothMapArray, addPos, null);
				if (sssVanishMapArray!=null) TextureArrayTools.Insert(ref sssVanishMapArray, addPos, null);
				if (changeBlockData) Voxeland.current.data.InsertType((byte)addPos);
			}
			public void RemoveLayer (int num) 
			{ 
					ArrayTools.RemoveAt(ref array, num);
					TextureArrayTools.RemoveAt(ref mainTexArray, num);
					TextureArrayTools.RemoveAt(ref bumpMapArray, num);
					TextureArrayTools.RemoveAt(ref specSmoothMapArray, num);
					TextureArrayTools.RemoveAt(ref sssVanishMapArray, num);
					if (changeBlockData) Voxeland.current.data.RemoveType((byte)num);
			}
			public void SwitchLayers (int num1, int num2) 
			{ 
					ArrayTools.Switch(array, num1, num2); 
					TextureArrayTools.Switch(mainTexArray, num1,num2);
					TextureArrayTools.Switch(bumpMapArray, num1, num2);
					TextureArrayTools.Switch(specSmoothMapArray, num1, num2);
					TextureArrayTools.Switch(sssVanishMapArray, num1, num2);
					if (changeBlockData) Voxeland.current.data.SwitchType((byte)num1, (byte)num2);
			}
		}

	#endregion

}
