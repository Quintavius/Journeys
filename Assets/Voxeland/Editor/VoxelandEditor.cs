using System;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

using Voxeland5;

namespace Voxeland5 
{
	[CustomEditor(typeof(Voxeland))]
	public class VoxelandEditor : Editor
	{
		[System.NonSerialized] private CoordDir prevAimCoord = new CoordDir(false);
		[System.NonSerialized] private int mouseButton = -1;
		[System.NonSerialized] private bool gaugeDisplayed = false; //for repainting the last final frame of progress gauge
		public enum FixedInfinite { Fixed, Infinite }
		
		Voxeland voxeland;
		Layout layout;

		[System.NonSerialized] private string[] blockNames;
		[System.NonSerialized] private string[] objectNames;
		[System.NonSerialized] private string[] grassNames;

		public void OnSceneGUI ()
		{	
			//updating Inspector GUI if thread is working (it lags less if done from OnSceneGUI... somehow)
			if (ThreadWorker.IsWorking("Voxeland") || gaugeDisplayed) Repaint();

			Voxeland voxeland = (Voxeland)target;
			if (voxeland.data == null) return;

			//disabling selection
			if (voxeland.guiLockSelection) 
			{
				HandleUtility.AddDefaultControl( GUIUtility.GetControlID(FocusType.Passive) ); 
				Tools.current = Tool.None;
			}

			//hiding wireframe
			//voxeland.transform.ToggleDisplayWireframe(!voxeland.guiHideWireframe);

			//switching highlight intensity mode
			if (voxeland.highlight != null && voxeland.highlight.material != null && voxeland.highlight.meshRenderer.sharedMaterial != null && voxeland.highlight.meshRenderer.sharedMaterial.HasProperty("_Linear"))
				voxeland.highlight.meshRenderer.sharedMaterial.SetInt("_Linear", PlayerSettings.colorSpace==ColorSpace.Linear? 1 : 0);

			//getting mouse button
			if (Event.current.type == EventType.MouseDown) mouseButton = Event.current.button;
			if (Event.current.rawType == EventType.MouseUp) mouseButton = -1;

			//getting mouse pos
			SceneView sceneview = UnityEditor.SceneView.lastActiveSceneView;
			if (sceneview==null || sceneview.camera==null) return;
			Vector2 mousePos = Event.current.mousePosition;
			mousePos = new Vector2(mousePos.x/sceneview.camera.pixelWidth, mousePos.y/sceneview.camera.pixelHeight);
			#if UNITY_5_4_OR_NEWER 	
			mousePos *= EditorGUIUtility.pixelsPerPoint;
			#endif
			mousePos.y = 1 - mousePos.y;

			//aiming
			Ray aimRay = sceneview.camera.ViewportPointToRay(mousePos);
			CoordDir aimCoord = voxeland.PointOut(aimRay);

			//focusing on brush
			if(Event.current.commandName == "FrameSelected")
			{ 
				Event.current.Use();
				if (aimCoord.exists) UnityEditor.SceneView.lastActiveSceneView.LookAt(
					voxeland.transform.TransformPoint( aimCoord.vector3 ), 
					UnityEditor.SceneView.lastActiveSceneView.rotation,
					Mathf.Max(voxeland.brush.extent*6, 20), 
					UnityEditor.SceneView.lastActiveSceneView.orthographic, 
					false);
				else 
				{
					Coord rectCenter = voxeland.chunks.deployedRects[0].Center * voxeland.chunkSize;
					int rectExtend = voxeland.chunks.deployedRects[0].size.x * voxeland.chunkSize;
					int height; byte temp;
					voxeland.data.GetTopTypePoint(rectCenter.x, rectCenter.z, out height, out temp);
					UnityEditor.SceneView.lastActiveSceneView.LookAt(
						voxeland.transform.TransformPoint( new Vector3(rectCenter.x, height, rectCenter.z) ), 
						UnityEditor.SceneView.lastActiveSceneView.rotation,
						rectExtend, 
						UnityEditor.SceneView.lastActiveSceneView.orthographic, 
						false);
				}
			}

			//if any change
			if (prevAimCoord != aimCoord || mouseButton==0)
			{
				//getting edit mode
				Voxeland.EditMode editMode = Voxeland.EditMode.none;
				bool buttonDown = false;
				if (!voxeland.continuousPainting) buttonDown = Event.current.type==EventType.MouseDown && Event.current.button==0;
				else buttonDown = mouseButton==0;
				if (buttonDown && !Event.current.alt)
				{
					if (Event.current.control && Event.current.shift) editMode = voxeland.controlShiftEditMode;
					else if (Event.current.shift) editMode = voxeland.shiftEditMode;
					else if (Event.current.control) editMode = voxeland.controlEditMode;
					else editMode = voxeland.standardEditMode;
				}

				//highlight
				if (voxeland.highlight!=null) // && Event.current.type!=EventType.KeyDown && Event.current.type!=EventType.mouseDrag) //do not redraw highlight on alt pressed
				{
					if (aimCoord.exists) voxeland.Highlight(aimCoord, voxeland.brush, isEditing:editMode!=Voxeland.EditMode.none);
					else voxeland.highlight.Clear(); //clearing highlight if nothing aimed or voxeland not selected
				}

				//altering
				if (editMode!=Voxeland.EditMode.none && aimCoord.exists) 
				{
					voxeland.Alter(aimCoord, voxeland.brush, editMode, landType:voxeland.landTypes.selected, objectType:voxeland.objectsTypes.selected, grassType:voxeland.grassTypes.selected);
				}
				
				prevAimCoord = aimCoord;
				
				SceneView.lastActiveSceneView.Repaint();
			} //if coord or button change

		}


		//repainting gui to make a animated indicator
		private void OnInspectorUpdate () 
		{ 	
			if (ThreadWorker.IsWorking("Voxeland")) Repaint();
		}

		public override void OnInspectorGUI ()
		{
			voxeland = (Voxeland)target;
			Voxeland.current = voxeland;

			//assigning voxeland to mapmagic window
			#if MAPMAGIC
			if (MapMagic.MapMagicWindow.instance != null && MapMagic.MapMagicWindow.instance.mapMagic != (MapMagic.IMapMagic)voxeland && voxeland.data != null && voxeland.data.generator != null && voxeland.data.generator.mapMagicGens != null)
				MapMagic.MapMagicWindow.Show(voxeland.data.generator.mapMagicGens, voxeland, forceOpen:false);
			#endif

			if (layout == null) layout = new Layout();
			layout.margin = 0; layout.rightMargin = 5;
			layout.field = Layout.GetInspectorRect();
			layout.cursor = new Rect();
			layout.undoObject = voxeland;
			layout.undoName =  "Voxeland settings change";
			layout.dragChange = true;
			layout.change = false;
			layout.delayed = true;

			if (voxeland.data == null) { layout.Par(30); layout.Label("Voxeland data is not assigned in Settings menu. No edit or rebuild allowed.", rect:layout.Inset(), helpbox:true); }

			#region Progress

				layout.Par();

				if (ThreadWorker.IsWorking("Voxeland"))
				{
					float calculatedSum; float completeSum; float totalSum;
					ThreadWorker.GetProgresByTag("VoxelandChunk", out totalSum, out calculatedSum, out completeSum);

					if (totalSum>10) 
					{
						Rect gaugeRect = layout.Inset(0.7f);
						layout.Gauge(0, "", gaugeRect);
						layout.Gauge(1, "", new Rect(gaugeRect.x, gaugeRect.y, gaugeRect.width * calculatedSum/totalSum, gaugeRect.height), disabled:true);
						layout.Gauge(1, "", new Rect(gaugeRect.x, gaugeRect.y, gaugeRect.width * completeSum/totalSum, gaugeRect.height));

					//	Rect cursor = layout.cursor;
				//		layout.Gauge(calculatedSum/totalSum, "", layout.Inset(0.7f), disabled:true);
					//	layout.cursor = cursor;
				//		layout.Gauge(calculatedSum/totalSum, "", layout.Inset(0.7f));
						//layout.Gauge(progress, "Progress: " + (int)completeSum + "(" + calculatedSum + ")" + "/" + (int)totalSum, layout.Inset(0.7f));
					//	layout.cursor = cursor;
						layout.Label("Progress: " + (int)completeSum + "(" + calculatedSum + ")" + "/" + (int)totalSum, gaugeRect);
					}
					else layout.Label("Progress: building", layout.Inset(0.7f));
					gaugeDisplayed = true;

					Repaint();
				}
				else 
				{
					layout.Label("Progress: complete", layout.Inset(0.7f));
					gaugeDisplayed = false;
				}

				if (layout.Button("Rebuild", layout.Inset(0.3f))) voxeland.Rebuild();

			#endregion

			layout.margin = 0; layout.rightMargin = 5;

			#region Brush
				layout.Par(8); 
				layout.Foldout(ref voxeland.guiBrush, "Brush");
				if (voxeland.guiBrush) 
				{
					Rect anchor = layout.lastRect;

					layout.Field(ref voxeland.brush.form, "Form");
					 voxeland.brush.extent = layout.Field(voxeland.brush.extent, "Extent", min:0, max: voxeland.brush.maxExtent, slider:true);
					layout.Toggle(ref voxeland.brush.round, "Round");
			
					layout.Par(5);
					if (voxeland.brush.form==Brush.Form.stamp)
					{
						 layout.Field(ref voxeland.brush.getStamp, "Get Stamp");
						if (voxeland.brush.getStamp)
						{
							layout.Par();
							layout.Label("Min:",rect:layout.Inset(0.25f));
							layout.Field(ref voxeland.brush.getStampMin.x, "X", rect:layout.Inset(0.25f));
							layout.Field(ref voxeland.brush.getStampMin.y, "Y", rect:layout.Inset(0.25f));
							layout.Field(ref voxeland.brush.getStampMin.z, "Z", rect:layout.Inset(0.25f));

							layout.Par();
							layout.Label("Max:",rect:layout.Inset(0.25f));
							layout.Field(ref voxeland.brush.getStampMax.x, "X", rect:layout.Inset(0.25f));
							layout.Field(ref voxeland.brush.getStampMax.y, "Y", rect:layout.Inset(0.25f));
							layout.Field(ref voxeland.brush.getStampMax.z, "Z", rect:layout.Inset(0.25f));
						}
					}

					layout.Par(5);
					layout.Field(ref voxeland.standardEditMode, "Standard Edit");
					layout.Field(ref voxeland.controlEditMode, "Control Mode");
					layout.Field(ref voxeland.shiftEditMode, "Shift Mode");
					layout.Field(ref voxeland.controlShiftEditMode, "Control+Shift Mode");

					layout.Par(5);
					layout.Toggle(ref voxeland.continuousPainting, "Continuous Painting");

					layout.Foreground(anchor);
					//if (voxeland.guiBrush) layout.Par(3);
				}

			#endregion

				
			#region Land Types
			if (voxeland.landTypes.selected >= 0 && voxeland.grassTypes.selected >= 0)  { voxeland.grassTypes.selected=-1; }
			if (voxeland.landTypes.selected >= 0 && voxeland.objectsTypes.selected >= 0)  { voxeland.objectsTypes.selected=-1; }

			layout.Par(8); 
			layout.Foldout(ref voxeland.guiBlocks, "Land Blocks");
			if (voxeland.guiBlocks) 
			{ 
				Rect anchor = layout.lastRect;
				layout.margin += 5; layout.rightMargin += 5;
				

				LandTypeList types = voxeland.landTypes;

				//blocks
				if (types.textureArrays  &&  types.mainTexArray != null  &&  types.bumpMapArray != null)
				{
					if (types.mainTexArrDec == null  ||  types.mainTexArrDec.texArr != types.mainTexArray) types.mainTexArrDec = new TextureArrayDecorator(types.mainTexArray);
					if (types.bumpTexArrDec == null  ||  types.bumpTexArrDec.texArr != types.bumpMapArray) types.bumpTexArrDec = new TextureArrayDecorator(types.bumpMapArray);

					int minCount = Mathf.Min(types.mainTexArray.depth, types.bumpMapArray.depth);
					if (types.array.Length != minCount) ArrayTools.Resize(ref types.array, minCount, createElement:num => new BlockType() { name="Land Block" });
				}

				layout.Par(4);
				for (int i=0; i<types.array.Length; i++)
				{
					/*#if RTP
					if (rtpMat) layout.DrawWithBackground(null, active:i==types.selected))
					else layout.DrawWithBackground(types.array[i].OnGUI, active:i==types.selected))

					if (layout.lastChange && !rtpMat) 
					{
						voxeland.landTypes.ApplyToMaterial(voxeland.material);
						voxeland.landTypes.ApplyToMaterial(voxeland.farMaterial);
					}
					#else*/
					
					int prevSelected = types.selected;
					layout.Layer(types, i);
					if (prevSelected != types.selected) { voxeland.grassTypes.selected=-1; voxeland.objectsTypes.selected=-1; }

					if (layout.lastChange) 
					{
						types.ApplyToMaterial(voxeland.material);
						types.ApplyToMaterial(voxeland.farMaterial, horizon:true);
					}

					//#endif
				}

				//drawing buttons
				layout.Par(3); layout.Par();
				layout.LayerButtons(types, types.array.Length, rect:layout.Inset(0.6f));
				layout.Inset(0.05f);
				layout.Field(ref types.changeBlockData, "Sync Data", rect:layout.Inset(0.35f), fieldSize:0.15f);
				layout.Par(5);

				//layout.AssetNewSaveField(ref voxeland.material, "Material", saveFilename:"LandMaterial", saveType:"mat", create:Voxeland.GetDefaultLandMaterial);

				//common (material)
				layout.Par(6); 
				layout.Par();
				layout.Foldout(ref voxeland.guiLandCommon, "General Material", rect:layout.Inset(), bold:false);
				if (voxeland.guiLandCommon) 
				{
					Rect internalAnchor = layout.lastRect;
					layout.change = false;

					layout.AssetNewSaveField(ref voxeland.material, "Material", saveFilename:"LandMaterial", saveType:"mat", create:Voxeland.GetDefaultLandMaterial);
					layout.Par(5);

					if (types.textureArrays)
						layout.Field(ref types.anisotropicFiltration, "Anisotropic Filtration", tooltip:"For TextureArray shader - enables/disables the anisotropic filtration in texture arrays created from blocks textures. Highly affects shader performance.");
					layout.Field(ref types.tile, "Tile");
					layout.Field(ref types.mips, "MipMap Factor");
					layout.Field(ref types.ambientOcclusion, "Ambient Occlusion");
					layout.Field(ref types.blendMapFactor, "Blend Map Factor");
					layout.Field(ref types.blendCrisp, "Blend Crispness");
					layout.Field(ref types.previewType, "Preview");

					if (layout.change)
					{
						types.ApplyToMaterial(voxeland.material);
						types.ApplyToMaterial(voxeland.farMaterial, horizon:true);
					}

					layout.Foreground(internalAnchor, layout.lastRect);
				}
				

				//texture arrays
				layout.Par(6); 
				layout.Foldout(ref voxeland.guiLandTexarr, "Texture Arrays", bold:false);
				if (voxeland.guiLandTexarr) 
				{
					Rect internalAnchor = layout.lastRect;

					layout.Toggle(ref types.textureArrays, "Use Texture Arrays");
					if (layout.lastChange)
					{
						//for (int i=0; i<types.array.Length; i++)
						//	types.array[i].icon = null;
					}

					if (types.textureArrays)
					{
						//if (voxeland.material==null || !voxeland.material.HasProperty("_MainTexArr"))
						//	{ layout.Par(30); layout.Label("Switch material to TextureArray shader in order to use texture arrays.", rect:layout.Inset(), helpbox:true); }

						layout.Par();
						layout.Field(ref types.mainTexArray, "Albedo", rect:layout.Inset(layout.field.width-25-layout.margin-layout.rightMargin));
						if (layout.lastChange) ChangeTextureArray(types.mainTexArray, ref types, isBump:false);
						layout.Inset(5);
						if (layout.Button("", rect:layout.Inset(20), icon:"DPLayout_New"))
						{
							Texture2DArray texArr = CreateTextureArrayAsset("MainTexArr", types, isBump:false);
							if (texArr != null) { types.mainTexArray = texArr; ChangeTextureArray(types.mainTexArray, ref types, isBump:false); }
						}

						layout.Par();
						layout.Field(ref types.bumpMapArray, "Normal", rect:layout.Inset(layout.field.width-25-layout.margin-layout.rightMargin));
						if (layout.lastChange) ChangeTextureArray(types.bumpMapArray, ref types, isBump:true);
						layout.Inset(5);
						if (layout.Button("", rect:layout.Inset(20), icon:"DPLayout_New"))
						{
							Texture2DArray texArr = CreateTextureArrayAsset("BumpMapArr", types, isBump:true);
							if (texArr != null) { types.bumpMapArray = texArr; ChangeTextureArray(types.bumpMapArray, ref types, isBump:true); }
						}

						if (types.bumpMapArray == null)
							{ layout.Par(30); layout.Label("A normal map is required for proper terrain lighing.", rect:layout.Inset(), helpbox:true); }

						layout.Par(0); layout.Inset();
					}
					//if (!types.textureArrays && voxeland.material!=null && voxeland.material.HasProperty("_MainTexArr"))
					//		{ layout.Par(30); layout.Label("Switch material to non-TextureArray (Voxeland/Land) shader in order to use the standard textures.", rect:layout.Inset(), helpbox:true); }
				
					layout.Foreground(internalAnchor, layout.lastRect);
				}

				//megasplat clusters (not yet implemented)
				/*#if __MEGASPLAT__
				layout.Par(6); 
				layout.Foldout(ref voxeland.guiLandMegaSplatClusters, "MegaSplat Clusters", bold:false);
				if (voxeland.guiLandMegaSplatClusters) 
				{
					Rect internalAnchor = layout.lastRect;

					layout.Toggle(ref types.megaClusters, "Use MegaSplat");
					if (layout.lastChange)
					{
						if (types.megaClusters) voxeland.channelEncoding = Voxeland.ChannelEncoding.MegaSplat;
						else voxeland.channelEncoding = Voxeland.ChannelEncoding.RTP;
					}

					if (types.megaClusters)
					{
						if (voxeland.material==null || !voxeland.material.HasProperty("_Diffuse") || !voxeland.material.HasProperty("_Normal"))
							{ layout.Par(30); layout.Label("Assign a proper MegaSplat mesh material.", rect:layout.Inset(), helpbox:true); }

						layout.Field(ref types.megaTexList);
						if (layout.lastChange) 
						{ 
							//if (types.megaTexList != null && types.array.Length != types.megaTexList.clusters.Length) 
							//	ArrayTools.Resize(ref types.array, types.megaTexList.clusters.Length, createElement:num => new BlockType() {name=types.megaTexList.clusters[num].name});
							types.array = new BlockType[0];
							if (types.megaTexList != null)
								ArrayTools.Resize(ref types.array, types.megaTexList.clusters.Length, createElement:num => new BlockType() {name=types.megaTexList.clusters[num].name});
						}
					}

					layout.Foreground(internalAnchor, layout.lastRect);
				}
				#endif*/
				

				//distant display
				layout.Par(6); 
				layout.Foldout(ref voxeland.guiLandFar, "Distant Tile", bold:false);
				if (voxeland.guiLandFar) 
				{
					Rect internalAnchor = layout.lastRect;
					layout.change = false;

					layout.Toggle(ref types.farEnabled, "Enabled");
					layout.Field(ref types.farTile, "Tile");
					layout.Field(ref types.farStart, "Start");
					layout.Field(ref types.farEnd, "End");

					if (layout.change)
					{
						types.ApplyToMaterial(voxeland.material);
						types.ApplyToMaterial(voxeland.farMaterial, horizon:true);
					}

					layout.Foreground(internalAnchor, layout.lastRect);
				}
				

				//horizon
				layout.Par(6); 
				layout.Foldout(ref voxeland.guiLandHorizon, "Horizon", bold:false);
				if (voxeland.guiLandHorizon) 
				{
					Rect internalAnchor = layout.lastRect;
					layout.change = false;

					layout.Field(ref types.horizonTile, "Tile");
					layout.Field(ref types.horizonBorderLower, "Border Lower");

					if (layout.change)
					{
						types.ApplyToMaterial(voxeland.material);
						types.ApplyToMaterial(voxeland.farMaterial, horizon:true);
					}

					layout.Foreground(internalAnchor, layout.lastRect);
				}
				
				layout.Par(5);

				
				layout.margin -= 5; layout.rightMargin -= 5;
				layout.Par(0,0,0); layout.Inset(1);
				layout.Foreground(anchor, layout.lastRect);
			}
			#endregion


			#region Object Types
			layout.Par(8); 
			layout.Foldout(ref voxeland.guiObjects, "Object Blocks");
			if (voxeland.guiObjects) 
			{ 
				Rect anchor = layout.lastRect;
				layout.margin += 5; layout.rightMargin += 5;

				ObjectTypeList types = voxeland.objectsTypes;

				layout.Par(0);
				int prevSelected = types.selected;
				for (int i=0; i<types.array.Length; i++)
					layout.Layer(types, i);
				if (types.selected != prevSelected)  { voxeland.grassTypes.selected=-1; voxeland.landTypes.selected=-1; }

				layout.Par(3); layout.Par();
				layout.LayerButtons(types, types.array.Length, rect:layout.Inset(0.6f));
				layout.Inset(0.05f);
				layout.Field(ref types.changeBlockData, "Sync Data", rect:layout.Inset(0.35f), fieldSize:0.15f);

				layout.Par(10);
				layout.Toggle(ref voxeland.objectsPool.regardPrefabRotation, "Regard Prefab Rotation");
				layout.Toggle(ref voxeland.objectsPool.regardPrefabScale, "Regard Prefab Scale");
				layout.Toggle(ref voxeland.objectsPool.instantiateClones, "Instantiate Clones");

				layout.margin -= 5; layout.rightMargin -= 5;
				layout.Foreground(anchor);
			}
			#endregion


			#region Grass Types
			layout.Par(8); 
			layout.Foldout(ref voxeland.guiGrass, "Grass");
			if (voxeland.guiGrass) 
			{
				Rect anchor = layout.lastRect;
				layout.margin += 5; layout.rightMargin += 5;

				GrassTypeList types = voxeland.grassTypes;


				//layers
				layout.Par(4);
				int prevSelected = types.selected;
				for (int i=0; i<types.array.Length; i++)
				{
					layout.Layer(types, i);
					if (layout.lastChange) types.ApplyToMaterial(voxeland.grassMaterial);
				}
				if (types.selected != prevSelected)  { voxeland.objectsTypes.selected=-1; voxeland.landTypes.selected=-1; }

				//drawing buttons
				layout.Par(3); layout.Par();
				layout.LayerButtons(types, types.array.Length, rect:layout.Inset(0.6f));
				layout.Inset(0.05f);
				layout.Field(ref types.changeBlockData, "Sync Data", rect:layout.Inset(0.35f), fieldSize:0.15f);
				layout.Par(5);

				//common (material)
				layout.Par(6); 
				layout.Par();
				layout.Foldout(ref voxeland.guiGrassCommon, "Grass Material", rect:layout.Inset(), bold:false);
				if (voxeland.guiGrassCommon) 
				{
					Rect internalAnchor = layout.lastRect;
					layout.change = false;

					layout.AssetNewSaveField(ref voxeland.grassMaterial, "Material", saveFilename:"GrassMaterial", saveType:"mat", create:Voxeland.GetDefaultGrassMaterial);
					layout.Par(5);

					layout.Field(ref types.culling, "Culling");
					layout.Field(ref types.cutoff, "Alpha Ref");
					layout.Field(ref types.ambientPower, "Ambient");
					layout.Field(ref types.mipFactor, "Mip Factor");
					layout.Field(ref types.vanishAngle, "Vanish Angle");
					layout.Field(ref types.appearAngle, "Appear Angle");
					layout.Par(5); layout.Inset();

					if (layout.change)
						types.ApplyToMaterial(voxeland.grassMaterial);

					layout.Foreground(internalAnchor, layout.lastRect);
				}

				//atlas
				layout.Par(6); 
				layout.Par();
				layout.Foldout(ref voxeland.guiGrassAtlas, "Atlas", rect:layout.Inset(), bold:false);
				if (voxeland.guiGrassAtlas) 
				{
					Rect internalAnchor = layout.lastRect;
					layout.change = false;

					layout.Field(ref types.atlas, "Atlas Enabled");
					if (layout.lastChange && types.atlas)  //removing layer textures
						for (int i=0; i<types.array.Length; i++)
						{
//							types.array[i].mainTex = null;
//							types.array[i].bumpMap = null;
//							types.array[i].specSmoothMap = null;
//							types.array[i].sssVanishMap = null;
						}
					if (layout.lastChange && types.atlas)  //removing assigned atlas
					{
//						types.mainTex = null;
//						types.bumpMap = null;
//						types.specSmoothMap = null;
//						types.sssVanishMap = null;
					}

					if (types.atlas)
					{
						layout.Field(ref types.mainTex, "Albedo/Alpha");
						layout.Field(ref types.bumpMap, "Normal");
						layout.Field(ref types.specSmoothMap, "Spec/Gloss");
						layout.Field(ref types.sssVanishMap, "SSS");
						
					}

					if (layout.change)
						types.ApplyToMaterial(voxeland.grassMaterial);

					layout.Foreground(internalAnchor, layout.lastRect);
				}

				//SSS
				layout.Par(6); 
				layout.Par();
				layout.Foldout(ref voxeland.guiGrassSSS, "Translucency", rect:layout.Inset(), bold:false);
				if (voxeland.guiGrassSSS) 
				{
					Rect internalAnchor = layout.lastRect;
					layout.change = false;

					layout.Field(ref types.sss, "Value");
					layout.Field(ref types.saturation, "Saturation");
					layout.Field(ref types.sssDistance, "Distance");
					layout.Par(5); layout.Inset();

					if (layout.change)
						types.ApplyToMaterial(voxeland.grassMaterial);

					layout.Foreground(internalAnchor, layout.lastRect);
				}

				//Shake
				layout.Par(6); 
				layout.Par();
				layout.Foldout(ref voxeland.guiGrassShake, "Shake/Wind", rect:layout.Inset(), bold:false);
				if (voxeland.guiGrassShake) 
				{
					Rect internalAnchor = layout.lastRect;
					layout.change = false;

					layout.Field(ref types.shakingAmplitude, "Shake Amplitude");
					layout.Field(ref types.shakingFrequency, "Shake Frequency");

					layout.Field(ref types.windTex, "Wind Texture");
					layout.Field(ref types.windSize, "Wind Size");
					layout.Field(ref types.windSpeed, "Wind Speed");
					layout.Field(ref types.windStrength, "Wind Strength");

					if (layout.change)
						types.ApplyToMaterial(voxeland.grassMaterial);

					layout.Foreground(internalAnchor, layout.lastRect);
				}

				layout.Par(5); layout.Inset();
				layout.margin -= 5; layout.rightMargin -= 5;
				layout.Foreground(anchor);
			}
			#endregion


			#region Distances

			layout.Par(8); 
			layout.Foldout(ref voxeland.guiDistances, "Mode and Ranges");
			if (voxeland.guiDistances)
			{
				Rect anchor = layout.lastRect;

				layout.fieldSize = 0.4f;
				layout.Field(ref voxeland.editorSizeMode, "Editor Mode");
				layout.Field(ref voxeland.playmodeSizeMode, "Playmode Mode");
				
				layout.Par(5);
				if (voxeland.editorSizeMode!=Voxeland.SizeMode.DynamicInfinite || voxeland.playmodeSizeMode!=Voxeland.SizeMode.DynamicInfinite)
				{
					layout.Field(ref voxeland.terrainSize, "Terrain Size");
					voxeland.terrainSize = ((int)(voxeland.terrainSize/voxeland.chunkSize)) * voxeland.chunkSize;
				}

				bool prevChange = layout.change;
				layout.change = false;

				if (voxeland.editorSizeMode!=Voxeland.SizeMode.Static || voxeland.playmodeSizeMode!=Voxeland.SizeMode.Static)
				{
					layout.Par(5);
					layout.Label("Terrain Ranges:");
					if (voxeland.guiDistancesFullControl)
					{
						layout.fieldSize = 0.2f;
						voxeland.guiDistancesMax = layout.Field(voxeland.guiDistancesMax, "Max Distance", delayed:true);
						layout.fieldSize = 0.8f;
					
						layout.Par(5);
						layout.Label("Chunks");
						DrawRange("Create", ref voxeland.createRange, 0.01f, voxeland.guiDistancesMax, voxeland.guiDistancesMax);
						DrawRange("Remove", ref voxeland.removeRange, voxeland.createRange, voxeland.guiDistancesMax, voxeland.guiDistancesMax);

						layout.Par(5);
						layout.Label("Voxel Meshes");
						DrawRange("Low Detail", ref voxeland.voxelLoRange,	voxeland.voxelHiRange,	voxeland.createRange,	voxeland.guiDistancesMax);
						DrawRange("High Detail", ref voxeland.voxelHiRange,	0.01f,						voxeland.voxelLoRange,	voxeland.guiDistancesMax);
						DrawRange("Collision", ref voxeland.collisionRange,	0.01f,						voxeland.voxelLoRange,	voxeland.guiDistancesMax);
						DrawRange("Objects", ref voxeland.objectsRange,		0.01f,						voxeland.createRange,	voxeland.guiDistancesMax);
						DrawRange("Grass", ref voxeland.grassRange,			0.01f,						voxeland.voxelLoRange,	voxeland.guiDistancesMax);
						DrawRange("Remove", ref voxeland.voxelRemoveRange,	voxeland.voxelLoRange,	voxeland.createRange,	voxeland.guiDistancesMax);

						layout.Par(5);
						layout.Label("Sculpt Areas");
						DrawRange("Generate", ref voxeland.areaGenerateRange, 0.01f, voxeland.areaRemoveRange, voxeland.areaRemoveRange);
						DrawRange("Remove", ref voxeland.areaRemoveRange, voxeland.areaGenerateRange, voxeland.areaRemoveRange, voxeland.areaRemoveRange);
					}

					else 
					{
						layout.fieldSize = 0.8f;
					
						DrawRange("Voxel Mesh", ref voxeland.voxelLoRange,	0.01f,	300, 300);
						DrawRange("Objects", ref voxeland.objectsRange,		0.01f,	300, 300);

						layout.Par(5);
						DrawRange("Collision", ref voxeland.collisionRange,	0.01f,	voxeland.voxelLoRange,	voxeland.guiDistancesMax);
						DrawRange("Grass", ref voxeland.grassRange,			0.01f,	voxeland.voxelLoRange,	voxeland.guiDistancesMax);
						DrawRange("High Detail", ref voxeland.voxelHiRange,	0.01f,	voxeland.voxelLoRange,	voxeland.guiDistancesMax);

						voxeland.createRange = Mathf.Max(voxeland.voxelLoRange, voxeland.objectsRange);
						voxeland.removeRange = voxeland.createRange + voxeland.chunkSize*2+1;
						voxeland.voxelRemoveRange = voxeland.voxelLoRange + voxeland.chunkSize*2+1;

						voxeland.areaGenerateRange = voxeland.createRange + voxeland.data.areaSize;
						voxeland.areaRemoveRange = voxeland.areaGenerateRange + voxeland.data.areaSize*2;
					}

					layout.Toggle(ref voxeland.guiDistancesFullControl, "Advanced");
					layout.fieldSize = 0.5f;

					layout.Par(5);
					layout.Field(ref voxeland.genAroundMainCam, "Generate Around Main Cam", fieldSize:0.1f);
					layout.Field(ref voxeland.genAroundObjsTag, "Generate Around Tagged Objs", fieldSize:0.1f);
					//layout.Field(ref voxeland.genAroundTag, "Tag");
					layout.Par(18); Rect rect = layout.ToDisplay(layout.Inset());
					voxeland.genAroundTag = EditorGUI.TagField(rect, voxeland.genAroundTag);
				}

				if (layout.change) voxeland.ForceUpdate();
				layout.change = prevChange;

				layout.Foreground(anchor);
			}
			#endregion

			#region Settings

			layout.Par(8); 
			layout.Foldout(ref voxeland.guiSettings, "Settings");
			if (voxeland.guiSettings)
			{
				Rect anchor = layout.lastRect;

				layout.margin += 5; layout.rightMargin += 5;
				layout.fieldSize = 0.4f;

				//data label
				layout.Par(5);
				voxeland.data = layout.ScriptableAssetField(voxeland.data, construct:null, savePath:null, fieldSize:0.8f);

				//margins
				layout.Par(5);
				layout.Field(ref voxeland.meshMargin, "Mesh Margin");
				layout.Field(ref voxeland.ambientMargin, "Ambient Margin");
				layout.Field(ref voxeland.ambientFade, "Ambient Fade", min:0, max:1);
				layout.Field(ref voxeland.normalsSmooth, "Smooth Normals", min:0, max:5);
				layout.Field(ref voxeland.relaxStrength, "Relax Strength", min:0, max:3);
				layout.Field(ref voxeland.relaxIterations, "Relax Iterations", min:0, max:10);
				if (voxeland.relaxIterations > voxeland.meshMargin)
				{
					layout.Par(30);
					layout.Label("Consider using Mesh Margin value higher than Relax Iterations to avoid visible seams between chunks.", rect:layout.Inset(), helpbox:true);
				}
				layout.Field(ref voxeland.channelEncoding, "Mesh Channel Encoding");
				layout.Field(ref voxeland.useAmbient, "Use Ambient");
				layout.Field(ref voxeland.playmodeEdit, "Edit in Playmode");
				

				//threads
				layout.Par(5);
				layout.Field(ref ThreadWorker.multithreading, "Multithreading");

				if (ThreadWorker.multithreading)
				{
					layout.Par();
					layout.Field(ref ThreadWorker.maxThreads, "Max Threads", rect:layout.Inset(0.75f), fieldSize:0.2f, disabled:ThreadWorker.autoMaxThreads);
					layout.Toggle(ref ThreadWorker.autoMaxThreads, "Auto",rect:layout.Inset(0.25f));
				}
				else layout.Field(ref ThreadWorker.maxThreads, "Max Coroutines");
				 
				layout.Field(ref ThreadWorker.maxApplyTime, "Max Apply Time");

				//other
				layout.Par(5);
				layout.Field(ref voxeland.guiLockSelection, "Lock Selection");
				layout.Field(ref voxeland.guiHideWireframe, "Hide Frame");
				if (layout.lastChange) voxeland.transform.ToggleDisplayWireframe(!voxeland.guiHideWireframe);
				layout.Field(ref voxeland.hideColliderWire, "Hide Collider Wire");
				layout.Field(ref voxeland.brush.maxExtent, "Max Brush Size");
				if (voxeland.brush.maxExtent>8) { layout.Par(30); layout.Label("Large brush extent can slow down Blob Brush display", layout.Inset(), helpbox:true); }
				layout.Field(ref voxeland.chunkSize, "Chunk Size (Experimental)");

				layout.Par(5);
				layout.Field(ref voxeland.saveMeshes, "Save Meshes with Scene");
				layout.Field(ref voxeland.saveNonpinnedAreas, "Save Non-pinned Areas");

				layout.Par(5);
				layout.Field(ref voxeland.chunkName, "Default Chunk Name");
				layout.Field(ref voxeland.copyLayersTags, "Copy Layers and Tags to Chunk");
				layout.Field(ref voxeland.copyComponents, "Copy Components to Chunk");

				//horizon
				layout.Par(10);
				layout.Par(0,padding:0); layout.Inset();
				Rect internalAnchor = layout.lastRect;
				bool useHorizon = layout.Toggle(voxeland.horizon!=null, "Horizon Mesh");
				if (useHorizon && voxeland.horizon==null) 
				{
					voxeland.horizon = Horizon.Create(voxeland); 
					voxeland.horizon.meshRenderer.sharedMaterial = voxeland.farMaterial;  
					
					voxeland.landTypes.ApplyToMaterial(voxeland.farMaterial, horizon:true);
				}

				if (!useHorizon && voxeland.horizon!=null) GameObject.DestroyImmediate(voxeland.horizon.gameObject);

				if (voxeland.horizon!=null)
				{
					voxeland.horizon.meshFilter.sharedMesh = layout.Field(voxeland.horizon.meshFilter.sharedMesh, "Mesh");
					layout.Field(ref voxeland.horizon.meshSize, "Mesh Bounding Box Size");
					layout.Field(ref voxeland.horizon.scale, "Scale");
					layout.Field(ref voxeland.horizon.textureResolutions, "Texture Resolution");
				}
				else
				{
					layout.Field<Mesh>(null, "Mesh", disabled:true);
					layout.Field(60, "Mesh Bounding Box Size", disabled:true);
					layout.Field(1, "Scale", disabled:true);
					layout.Field(512, "Texture Resolution", disabled:true);
				}
				layout.Foreground(internalAnchor);

				//floaty origin solution
				layout.Par(10);
				layout.Par(0,padding:0); layout.Inset();
				internalAnchor = layout.lastRect;
				layout.Toggle(ref voxeland.shift, "Shift World (Floating Point Solution)");
				layout.Field(ref voxeland.shiftThreshold, "Shift Threshold", disabled:!voxeland.shift);
				layout.Foreground(internalAnchor);

				//debug
				BuildTargetGroup buildGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
				string defineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildGroup);
				
				bool debug = false;
				if (defineSymbols.Contains("WDEBUG;") || defineSymbols.EndsWith("WDEBUG")) debug = true;
				
				layout.Par(10);
				layout.Toggle(ref debug, "Debug (Requires re-compile)");
				if (layout.lastChange) 
				{
					if (debug)
					{
						defineSymbols += (defineSymbols.Length!=0? ";" : "") + "WDEBUG";
					}
					else
					{
						defineSymbols = defineSymbols.Replace("WDEBUG","");  
						defineSymbols = defineSymbols.Replace(";;", ";"); 
					}
					PlayerSettings.SetScriptingDefineSymbolsForGroup(buildGroup, defineSymbols);
				}

				layout.margin -= 5; layout.rightMargin -= 5;

				layout.Foreground(anchor);
			}

			#endregion

			#region Material
			layout.Par(8); 
			layout.Foldout(ref voxeland.guiMaterial, "Materials");
			if (voxeland.guiMaterial)
			{
				Rect anchor = layout.lastRect;
				layout.margin += 10;
				layout.fieldSize = 0.5f;

				layout.AssetNewSaveField(ref voxeland.material, "Land", saveFilename:"Land", saveType:"mat", create:Voxeland.GetDefaultLandMaterial);
				layout.AssetNewSaveField(ref voxeland.farMaterial, "Horizon", saveFilename:"Horizon", saveType:"mat", create:Voxeland.GetDefaultFarMaterial);
				layout.AssetNewSaveField(ref voxeland.grassMaterial, "Grass", saveFilename:"Grass.mat", saveType:"mat", create:Voxeland.GetDefaultGrassMaterial);
				if (voxeland.highlight!=null) layout.AssetNewSaveField(ref voxeland.highlight.material, "Highlight", saveFilename:"Highlight.mat", saveType:"mat", create:Voxeland.GetDefaultHighlightMaterial);
				else layout.Field<Material>(null, "Highlight", disabled:true);

				layout.margin -= 10;
				layout.Foreground(anchor);
				//if (voxeland.guiMaterial) layout.Par(3);
			}

			#endregion

			#region Generate
			layout.Par(8); 
			layout.Foldout(ref voxeland.guiGenerate, "Generate");
			if (voxeland.guiGenerate)
			{
				Rect anchor = layout.lastRect;
				layout.margin += 10;

				if (voxeland.data == null) layout.Label("No proper Voxeland data to generate");
				else
				{	
					Generator generator = voxeland.data.generator;
					bool change = layout.change;
					layout.change = false;
				
					layout.Field(ref generator.generatorType, "Generator Type");

					//gathering block names
					if (blockNames == null || blockNames.Length != voxeland.landTypes.array.Length+2) blockNames = new string[voxeland.landTypes.array.Length+2];
					for (int i=0; i<voxeland.landTypes.array.Length; i++)
						blockNames[i] = voxeland.landTypes.array[i].name;
					blockNames[blockNames.Length-1] = "Empty";

					if (objectNames == null || objectNames.Length != voxeland.objectsTypes.array.Length+2) objectNames = new string[voxeland.objectsTypes.array.Length+2];
					for (int i=0; i<voxeland.objectsTypes.array.Length; i++)
						objectNames[i] = voxeland.objectsTypes.array[i].name;
					objectNames[objectNames.Length-1] = "Empty";

					if (grassNames == null || grassNames.Length != voxeland.grassTypes.array.Length+2) grassNames = new string[voxeland.grassTypes.array.Length+2];
					for (int i=0; i<voxeland.grassTypes.array.Length; i++)
						grassNames[i] = voxeland.grassTypes.array[i].name;
					grassNames[grassNames.Length-1] = "Empty";


					layout.Par(10);
			
					if (generator.generatorType == Generator.GeneratorType.Planar)
					{
						generator.planarGen.OnGUI(layout, blockNames);
					}

					if (generator.generatorType == Generator.GeneratorType.Noise)
					{
						generator.noiseGen.OnGUI(layout, blockNames, "Noise");
						generator.curveGen.OnGUI(layout, blockNames);
						generator.slopeGen.OnGUI(layout, blockNames);
						generator.cavityGen.OnGUI(layout, blockNames);
						generator.blurGen.OnGUI(layout, blockNames);
						generator.stainsGen.OnGUI(layout, blockNames);
						generator.noiseGenB.OnGUI(layout, blockNames, "Bedrock (Noise)");
						generator.scatterGen.OnGUI(layout, objectNames, blockNames);

						//grass
						generator.grassGens.grassNames = grassNames;
						generator.grassGens.landNames = blockNames;

						layout.LayerButtons(generator.grassGens, generator.grassGens.gens.Length, "Grass Generators:", addBeforeSelected:true);

						layout.margin += 3; layout.rightMargin += 1; //right margin is 3 by default
						layout.Par(3);
						for (int num=0; num<generator.grassGens.gens.Length; num++)
							layout.Layer(generator.grassGens, num);
						layout.Par(3);
						layout.margin -= 3; layout.rightMargin -= 1;
					}

					else if (generator.generatorType == Generator.GeneratorType.MapMagic)
					{
						#if MAPMAGIC
						MapMagic.VoxelandOutput.voxeland = voxeland;
						MapMagic.VoxelandGrassOutput.voxeland = voxeland;
						MapMagic.VoxelandObjectsOutput.voxeland = voxeland;

						//data field
						layout.Par(5);
						layout.fieldSize = 0.7f;
						generator.mapMagicGens = layout.ScriptableAssetField(generator.mapMagicGens, construct:MapMagic.GeneratorsAsset.DefaultVoxeland, savePath: null);
						//generator.mapMagicGens = layout.ScriptableAssetField(generator.mapMagicGens, construct:MapMagic.Graph.DefaultVoxeland, savePath: null);
						if (layout.lastChange) 
							MapMagic.MapMagicWindow.Show(generator.mapMagicGens, voxeland, forceOpen:false, asBiome:false);
				
						//show editor button
						layout.Par(5);
						layout.Par(22);
						if (layout.Button("Show Editor", rect:layout.Inset(1f), disabled:generator.mapMagicGens==null))
							MapMagic.MapMagicWindow.Show(generator.mapMagicGens, voxeland, forceOpen:true);
					
						#else
						layout.Par(40);
						layout.Label("MapMagic World Generator does not seems to be installed. If you sure that you have it try restarting Unity to add it's define symbol.", rect:layout.Inset(), helpbox:true);
						#endif
					}

					else if (generator.generatorType == Generator.GeneratorType.Heightmap)
					{
						generator.standaloneHeightGen.OnGUI(layout, blockNames);
					}

					if (generator.generatorType != Generator.GeneratorType.Planar)
					{
						layout.Par(10);
				
						layout.Field(ref generator.seed, "Seed"); if (layout.lastChange) generator.change = true;
						#if MAPMAGIC
						if (layout.lastChange) voxeland.ClearResults();
						#endif
						layout.Field(ref generator.heightFactor, "Height"); if (layout.lastChange) generator.change = true;
						layout.Toggle(ref generator.saveResults, "Save Interim Results");
						layout.Toggle(ref generator.instantGenerate,"Instant Generate");
						//layout.Toggle(ref generator.forceGenerateChangedAreas, "Force Generate Changed Areas");
						layout.Toggle(ref generator.polish, "Polish");
						//layout.Toggle(ref generator.leaveDemoUntouched, "Leave Demo Untouched");
				
						layout.Par();
						layout.Toggle(ref generator.removeThinLayers, "Remove Thin Layers", rect:layout.Inset(0.8f));
						layout.Field(ref generator.minLayerThickness, rect:layout.Inset(0.2f));

						if (layout.change) voxeland.Generate();
						layout.change = change;
					}

					layout.Par(5);

					string readyIcon = "Voxeland_Success"; int readyAnimFrames = 0;
					if (ThreadWorker.IsWorking("VoxelandGenerate")) { readyIcon = "Voxeland_Loading"; readyAnimFrames=12; Repaint(); }

					layout.Par(24);
					if (layout.Button("Generate", rect:layout.Inset(), icon:readyIcon, iconAnimFrames:readyAnimFrames)) 
						voxeland.Generate();
					if (layout.Button("Force Re-Generate") && EditorUtility.DisplayDialog("Re-Generate", "This will remove all of the custom changes made in your DATA file. Are you sure you wish to continue?", "Re-Generate", "Cancel")) 
						voxeland.Generate(force:true);
					
				}

				layout.margin -= 10;
				layout.Foreground(anchor);
			}
			#endregion

			#region About
			layout.Par(8); 
			layout.Foldout(ref voxeland.guiAbout, "About");
			if (voxeland.guiAbout)
			{
				Rect anchor = layout.lastRect;
				Rect savedCursor = layout.cursor;
				
				layout.margin = 20;
				layout.Par(100, padding:0);
				layout.Icon("VoxelandIcon", layout.Inset(50,padding:0));

				layout.cursor = savedCursor;
				layout.margin = 80;

				string versionName = Voxeland.version.ToString();
				versionName = versionName[0]+"."+versionName[1]+"."+versionName[2];
				layout.Label("Voxeland " + versionName);
				layout.Label("by Denis Pahunov");
				
				layout.Par(10);
				layout.Label(" - Wiki", url:"https://gitlab.com/denispahunov/voxeland/wikis/home");
				layout.Label(" - Forums", url:"https://forum.unity3d.com/threads/voxeland-voxel-terrain-tool.187741/");
				layout.Label(" - Issues / Ideas", url:"https://gitlab.com/denispahunov/voxeland/issues"); 

				/*layout.Par(10);
				layout.Label("Other Products:");

				layout.Par(5);
				layout.Label("MapMagic", url:"https://gitlab.com/denispahunov/voxeland/wikis/home");
				layout.Label("Node based infinite terrain generator");*/

				layout.Foreground(anchor);
			}
			#endregion

			Layout.SetInspectorRect(layout.field);
		}

		#region Editor Functions

			public void DrawRange (string label, ref float src, float min, float max, float gaugeMax)
		{
			layout.Par();
			layout.Label(label,rect:layout.Inset(0.25f));
			//layout.Gauge(Mathf.Sqrt(src/max),null,rect:layout.Inset(0.5f));
			//layout.Gauge(src/gaugeMax,null,rect:layout.Inset(0.5f));

			//rects
			float gaugeWidth = 0.55f;
			Rect gaugeRect = layout.Inset(gaugeWidth);
			Rect afterGauge = layout.cursor;

			//field
			layout.cursor = afterGauge;
			src = layout.Field(src, rect:layout.Inset(0.2f), min:min, max:max);
			if (layout.lastChange)
			{
				if (src < min) src = min;
				if (src > max) src = max;
				if (src > gaugeMax) src = gaugeMax;
			}

			//gauge background
			layout.Gauge(src/gaugeMax, null, rect:gaugeRect, disabled:true);

			//gauge itself
			gaugeRect.position = new Vector2(gaugeRect.x + (min/gaugeMax) * gaugeRect.width, gaugeRect.y);
			gaugeRect.width = ((max-min)/gaugeMax)*gaugeRect.width;
			if (gaugeRect.width < 4) gaugeRect.width = 4;
			
			float delta = max-min;
			if (delta < 1f) delta = 1f;
			layout.Gauge((src-min)/delta, null, rect:gaugeRect);
		}


			public Texture2DArray CreateTextureArrayAsset (string filename, LandTypeList types, bool isBump)   
			{
				string savePath = UnityEditor.EditorUtility.SaveFilePanel("Create New Texture Array", "Assets", filename, "asset");
				if (savePath == null  ||  savePath.Length==0) return null;  //savefilepanel returns "" on cancel
				savePath = savePath.Replace(Application.dataPath, "Assets");

				Texture2DArray texArr = new Texture2DArray(1024, 1024, types.array.Length, TextureFormat.RGBA32, true, linear:isBump);
				AssetDatabase.CreateAsset(texArr, savePath);
				EditorUtility.SetDirty(texArr);
				AssetDatabase.SaveAssets();

				TextureArrayDecorator texArrDec = new TextureArrayDecorator(texArr);
				for (int i=0; i<types.array.Length; i++)
				{
					if (isBump) texArrDec.SetSource(types.array[i].bumpMap, i, isAlpha:false, saveSources:false);
					else
					{
						texArrDec.SetSource(types.array[i].mainTex, i, isAlpha:false, saveSources:false);
						//texArrDec.SetSource(types.array[i].alphaSource, i, isAlpha:true, saveSources:false);
					}
				}
				texArrDec.SaveSources();

				AssetDatabase.Refresh();
				return texArr;
			}


			public void ChangeTextureArray (Texture2DArray texArr, ref LandTypeList types, bool isBump)
			{
				/*//array was removed - removing sources
				if (texArr == null)
					for (int i=0; i<types.array.Length; i++)
					{
						if (isBump) types.array[i].bumpSource = null;
						else { types.array[i].mainSource = null; types.array[i].alphaSource = null; }
					}

				else
				{
				
					//changing the blocks count on diffuse array assign
					if (!isBump  &&  types.array.Length != texArr.depth) 
						ArrayTools.Resize(ref types.array, texArr.depth, createElement:num => new BlockType() {name="Block " + num});

					//reload sources
					for (int i=0; i<types.array.Length; i++)
					{
						if (isBump) types.array[i].bumpSource = texArr.GetSource(i);
						else { types.array[i].mainSource = texArr.GetSource(i); types.array[i].alphaSource = texArr.GetSource(i, isAlpha:true); }
					}
				}*/

				//to mat
				types.ApplyToMaterial(voxeland.material);
				types.ApplyToMaterial(voxeland.farMaterial, horizon:true);
			}
			
		#endregion

		[MenuItem ("GameObject/3D Object/Voxeland Static")]
		static void CreateStaticVoxeland () { CreateVoxeland(false); }

		[MenuItem ("GameObject/3D Object/Voxeland Infinite")]
		static void CreateInfiniteVoxeland () { CreateVoxeland(true); }

		static void CreateVoxeland (bool infinite=true) 
		{
			GameObject go = new GameObject();
			go.name = "Voxeland";

			Voxeland voxeland = go.AddComponent<Voxeland>();
			voxeland.chunks = new ChunkGrid<Chunk>();
			voxeland.chunkSize = 30;
			Voxeland.instances.Add(voxeland);
			
			//adding empty layer 
			voxeland.landTypes = new LandTypeList();

			ArrayTools.Add(ref voxeland.landTypes.array, new BlockType()); 
			voxeland.landTypes.array[0].mainTex = TextureExtensions.ColorTexture(2,2,new Color(0.666f, 0.666f, 0.666f, 0.5f), linear:false);
			#if UNITY_2017_3_OR_NEWER
			voxeland.landTypes.array[0].bumpMap = TextureExtensions.ColorTexture(2,2,new Color(0.5f, 0.5f, 0.5f, 1f), linear:true);
			#else
			voxeland.landTypes.array[0].bumpMap = TextureExtensions.ColorTexture(2,2,new Color(0.5f, 0.5f, 0.5f, 0.5f), linear:true);
			#endif

			//voxeland.landTypes.mainTexArray = new Texture2DArray(1024, 1024, 1, TextureFormat.DXT5, true) { name = "Albedo Height Array" };
			//voxeland.landTypes.mainTexArray.SetTexture( TextureExtensions.ColorTexture(1024,1024,Color.gray), 0);
			//voxeland.landTypes.bumpMapArray = new Texture2DArray(1024, 1024, 1, TextureFormat.DXT5, true, linear:true) { name = "Normals Array" };
			//voxeland.landTypes.bumpMapArray.SetTexture( TextureExtensions.ColorTexture(1024,1024,new Color(0.5f,0.5f,0.5f,0.5f)), 0);

			voxeland.landTypes.selected = 0;

			//adding empty grass
			voxeland.grassTypes = new GrassTypeList();
			voxeland.grassTypes.selected = -1;

			//objects
			voxeland.objectsTypes = new ObjectTypeList();
			voxeland.objectsTypes.selected = -1;

			//data
			voxeland.data = ScriptableObject.CreateInstance<Data>(); 
			voxeland.data.areas = new ChunkGrid<Data.Area>();
			voxeland.data.areaSize = 512;

			//materials
			voxeland.material = Voxeland.GetDefaultLandMaterial();
			voxeland.farMaterial = Voxeland.GetDefaultFarMaterial();
			voxeland.grassMaterial = Voxeland.GetDefaultGrassMaterial();

			voxeland.landTypes.ApplyToMaterial(voxeland.material);
			voxeland.landTypes.ApplyToMaterial(voxeland.farMaterial, horizon:true);
			voxeland.grassTypes.ApplyToMaterial(voxeland.grassMaterial);

			if (voxeland.horizon != null) voxeland.horizon.meshRenderer.sharedMaterial = voxeland.farMaterial;
			
			//highlight
			if (voxeland.highlight == null) //could be created on de-prefab in onenable
			{
				voxeland.highlight = Highlight.Create(voxeland);
				voxeland.highlight.material = Voxeland.GetDefaultHighlightMaterial(); //new Material( Shader.Find("Voxeland/Highlight") );
			}

			//static and infinite
			if (infinite)
			{
				voxeland.playmodeSizeMode = Voxeland.SizeMode.DynamicInfinite;
				voxeland.editorSizeMode = Voxeland.SizeMode.DynamicInfinite;

				voxeland.saveMeshes = false;

				voxeland.horizon = Horizon.Create(voxeland);
			}
			else
			{
				voxeland.playmodeSizeMode = Voxeland.SizeMode.Static;
				voxeland.editorSizeMode = Voxeland.SizeMode.Static;

				voxeland.saveMeshes = true;  
			}

			//generators
			voxeland.data.generator.noiseGen.enabled = true;
			voxeland.data.generator.noiseGenB.enabled = false;
			voxeland.data.generator.noiseGenB.seed = 1234;
			voxeland.data.generator.noiseGenB.high = 0.1f;
			voxeland.data.generator.curveGen.enabled = true;
			voxeland.data.generator.blurGen.enabled = true;

			//registering undo
			Undo.RegisterCreatedObjectUndo (go, "Voxeland Create");
			EditorUtility.SetDirty(go);
		}
	}


	public class NewDataWindow : EditorWindow
	{
		Layout layout;
		public Voxeland voxeland;

		public int areaRes = 100;
		public int areaSize = 32;

		public void OnGUI ()
		{
			this.name = "New Data";
			if (layout == null) layout = new Layout();
			layout.margin = 0; layout.rightMargin = 0;
			layout.field = Layout.GetInspectorRect();
			layout.cursor = new Rect();
			layout.undoObject = voxeland;
			layout.undoName =  "Voxeland settings change";
			layout.dragChange = false;
			layout.change = false;

			layout.Field(ref areaSize, "Area Size");
			layout.Field(ref areaRes, "Areas Count");
			layout.Label("Total terrain size: " + areaRes*areaSize + "x" + areaRes*areaSize);

			layout.Par(5);
			if (layout.Button("Reset Terrain"))
			{
				voxeland.data = ScriptableObject.CreateInstance<Data>();
			}
		}
	}

}//namespace