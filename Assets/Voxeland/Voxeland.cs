using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Assertions;
#if UNITY_5_5_OR_NEWER
using UnityEngine.Profiling;
#endif

using Voxeland5;

namespace Voxeland5
{
	[ExecuteInEditMode]
	[System.Serializable]
	///Voxel cubic-style terrain, subdivided and smoothed. Main component that is used both with Static and Infinite terrains.
	#if MAPMAGIC
	public class Voxeland : MonoBehaviour, ISerializationCallbackReceiver, MapMagic.IMapMagic
	#else
	public class Voxeland : MonoBehaviour, ISerializationCallbackReceiver
	#endif
	{
		public static readonly int version = 534;   ///<For serialization purpose', to load older versions as a special cases.

		public static HashSet<Voxeland> instances = new HashSet<Voxeland>();   ///<Sort of singleton instance but keeps track of multiple instances. Outdated.
		public static Voxeland current;		///<The last Voxeland object that was Updated or had OnEditorGUI drawn
	
		public ChunkGrid<Chunk> chunks = new ChunkGrid<Chunk>();   ///<Current Voxeland chunks. Serialized with OnBeforeSerialize callback.
		public Data data;   ///<Current Voxeland data that keeps all of the blocks information. Scriptable asset. Could be saved separately.
		public Highlight highlight;   ///<Voxeland cursor object.
		public Horizon horizon;  ///<Plane object displayed in the far.
		public ObjectPool objectsPool = new ObjectPool();

		//brush
		public Brush brush = new Brush();
		public enum EditMode {none, add, dig, replace, smooth}; //standard mode is similar to add one, except the preliminary switch to opposite in add
		public EditMode standardEditMode = EditMode.add;
		public EditMode controlEditMode = EditMode.dig;
		public EditMode shiftEditMode = EditMode.replace;
		public EditMode controlShiftEditMode = EditMode.smooth;

		//continuous painting
		public bool continuousPainting = false;
		public CoordDir prevAimingCoord = new CoordDir(0,0,0,7);
		public bool prevCoordReseted = false; //on collider apply

		//types
		public enum EditCategory { land, constructor, objects, grass };
		[SerializeField] public LandTypeList landTypes = new LandTypeList();  
		[SerializeField] public ConstructorTypeList constructorTypes = new ConstructorTypeList();
		[SerializeField] public ObjectTypeList objectsTypes = new ObjectTypeList();
		[SerializeField] public GrassTypeList grassTypes = new GrassTypeList();

		//stage distances
		public float createRange = 100;
		public float removeRange = 150;
		public float voxelHiRange = 20;
		public float voxelLoRange = 100;
		public float voxelRemoveRange = 120;
		public float collisionRange = 50;
		public float grassRange = 50;
		public float objectsRange = 100;
		public float areaGenerateRange = 200;
		public float areaRemoveRange = 300;

		public float terrainSize = 300;

		public enum SizeMode { Static, DynamicLimited, DynamicInfinite };
		public SizeMode playmodeSizeMode = SizeMode.DynamicInfinite;
		public SizeMode editorSizeMode = SizeMode.DynamicInfinite;

		//general settings
		public int chunkSize = 30;
		public bool saveMeshes = false;
		public bool saveNonpinnedAreas = false;
		public bool playmodeEdit = false;
		public float ambientFade = 0.7f;
		public int relaxIterations = 2;
		public float relaxStrength = 1.95f;
		public int normalsSmooth = 0;
		public enum ChannelEncoding { Voxeland, RTP, MegaSplat };
		public ChannelEncoding channelEncoding = ChannelEncoding.Voxeland;
		public bool hideColliderWire = true;
		public string chunkName = "Chunk";
		public bool copyLayersTags = true;
		public bool copyComponents = false;
		public bool useAmbient = true;

		public bool shift = false;
		public int shiftThreshold = 4000;

		//margins
		public int meshMargin = 2;
		public int ambientMargin = 7;

		//areas and generate
		public bool genAroundMainCam = true;
		public bool genAroundObjsTag = false;
		public string genAroundTag = null;

		//materials
		public Material material;
		public Material farMaterial;
		public Material grassMaterial;

		//gui
		public bool guiLockSelection = true;
		public bool guiHideWireframe = true;
		public bool guiBrush;
		public bool guiBlocks;
		public bool guiObjects;
		public bool guiConstructor;
		public bool guiGrass;
		public bool guiDistances;
		public bool guiDistancesFullControl;
		public float guiDistancesMax = 300;
		public bool guiSettings;
		public bool guiNewDataAsset = true;
		public bool guiMaterial;
		public bool guiMaterialMain;
		public bool guiMaterialFar;
		public bool guiMaterialGrass;
		public bool guiMaterialSamePlanarKeywords;
		public bool guiMaterialSamePlanarProps;
		public bool guiGenerate;
		public bool guiAbout;
		public bool guiLandTexarr = false;
		public bool guiLandMegaSplatClusters = false;
		public bool guiLandCommon = false;
		public bool guiLandFar = false;
		public bool guiLandHorizon = false;
		public bool guiGrassCommon = false;
		public bool guiGrassAtlas = false;
		public bool guiGrassSSS = false;
		public bool guiGrassShake = false;

		//reusing update arrays
		[System.NonSerialized] Vector3[] prevCamPoses; //to notify if cam poses changed
		[System.NonSerialized] Vector3[] camPoses;
		[System.NonSerialized] CoordRect[] areaDeployRects; 
		[System.NonSerialized] CoordRect[] chunkDeployRects; 
		[System.NonSerialized] CoordRect[] areaRemoveRects;  //actually not 'remove' but 'remove everything that is out of those rects
		[System.NonSerialized] CoordRect[] chunkRemoveRects;



		public void OnEnable ()
		{
			Assert.IsTrue(!instances.Contains(this));  //even if adding to instances on editor create - onenable called first
			instances.Add(this);
			current = this;

			#if UNITY_EDITOR
			//events
			UnityEditor.EditorApplication.update -= Update;
			if (!UnityEditor.EditorApplication.isPlaying) UnityEditor.EditorApplication.update += Update;	

			UnityEditor.Undo.undoRedoPerformed -= PerformUndo;
			UnityEditor.Undo.undoRedoPerformed += PerformUndo;

			//restoring prefab
			if (!UnityEditor.EditorApplication.isPlaying)
			{
				//ensuring that's really a prefab
				bool prefab = true;
				if (highlight != null && highlight.meshFilter.sharedMesh!=null) prefab = false;
				foreach (Chunk chunk in chunks.All())
					if (chunk.meshFilter.sharedMesh != null) prefab = false;

				if (prefab)
				{
					//removing all children
					chunks.grid.Clear();
					transform.RemoveChildren();

					//creating highlight
					if (highlight == null)
					{
						highlight = Voxeland5.Highlight.Create(this);
						highlight.material = GetDefaultHighlightMaterial();
					}

					//assigning default materials
					if (material == null || farMaterial == null) 
					{ 
						if (material == null) material = GetDefaultLandMaterial(); 
						landTypes.ApplyToMaterial(material);

						if (farMaterial == null) farMaterial = GetDefaultFarMaterial(); 
						landTypes.ApplyToMaterial(farMaterial, horizon:true);
					}
					if (grassMaterial == null) { grassMaterial = GetDefaultGrassMaterial(); grassTypes.ApplyToMaterial(grassMaterial); }
				}

				
				
			}

			//stoping shader animation
			if (!UnityEditor.EditorApplication.isPlaying) Shader.SetGlobalInt("_IsNonPlaymode", 1);
			else Shader.SetGlobalInt("_IsNonPlaymode", 0);

			#endif
		}

		public void OnDisable ()
		{
			#if UNITY_EDITOR	
			UnityEditor.EditorApplication.update -= Update;
			UnityEditor.Undo.undoRedoPerformed -= PerformUndo;
			#endif

			Assert.IsTrue(instances.Contains(this));
			instances.Remove(this);
			current = null;
		}


		public void Update ()
		{
			#if WDEBUG
			Profiler.BeginSample("Refresh");
			#endif

			current = this;

			//shifting world
			if (Extensions.isPlaying && shift) WorldShifter.Update(shiftThreshold);

			//un-subscribing from update on new scene
			#if UNITY_EDITOR
			if (this==null || transform==null) { UnityEditor.EditorApplication.update -= ThreadWorker.Refresh; return; }  
			#endif

			//data guard clause
			if (data == null) return;
			if (chunks==null) Debug.Log("Chunks are null");
			//if (chunks.grid.Count==0) Debug.Log("Chunks are empty"); 
			if (chunkSize < 1) return;	


			//finding current size mode
			SizeMode currentSizeMode = editorSizeMode;
			if (Extensions.isPlaying) currentSizeMode = playmodeSizeMode;


			//finding camera positions
			if (currentSizeMode != SizeMode.Static)
			{
				camPoses = Extensions.GetCamPoses(genAroundMainCam:genAroundMainCam, genAroundTag:genAroundObjsTag? genAroundTag : null, camPoses:camPoses);
				if (camPoses.Length == 0) return; //no cameras to deploy Voxeland
				transform.InverseTransformPoint(camPoses); //transforming cameras position to local
			}
			else camPoses = new Vector3[] { new Vector3(terrainSize/2, 0, terrainSize/2) }; //static cam pos needed only to set priority

			//finding deploy rects	
			/*if (chunkDeployRects == null || chunkDeployRects.Length!=camPoses.Length)
			{
				areaDeployRects = new CoordRect[camPoses.Length]; areaRemoveRects = new CoordRect[camPoses.Length];
				chunkDeployRects = new CoordRect[camPoses.Length]; chunkRemoveRects = new CoordRect[camPoses.Length];
			}*/

			if (currentSizeMode == SizeMode.DynamicInfinite)
			{
				areaDeployRects = new CoordRect[camPoses.Length]; areaRemoveRects = new CoordRect[camPoses.Length]; //TODO: wtf, why do I create them twice?
				chunkDeployRects = new CoordRect[camPoses.Length]; chunkRemoveRects = new CoordRect[camPoses.Length];
				for (int r=0; r<camPoses.Length; r++) //TODO: add cam pos change check
				{
					areaDeployRects[r] = CoordRect.PickIntersectingCellsByPos(camPoses[r], areaGenerateRange, cellSize:data.areaSize);
					areaRemoveRects[r] = CoordRect.PickIntersectingCellsByPos(camPoses[r], areaRemoveRange, cellSize:data.areaSize);
					chunkDeployRects[r] = CoordRect.PickIntersectingCellsByPos(camPoses[r], createRange, cellSize:chunkSize);
					chunkRemoveRects[r] = CoordRect.PickIntersectingCellsByPos(camPoses[r], removeRange, cellSize:chunkSize);
				}
			}
			else
			{
				Rect terrainSizeRect = new Rect(0,0,terrainSize,terrainSize);
				int margin = Mathf.Max(meshMargin, ambientMargin);
				areaDeployRects = new CoordRect[] { CoordRect.PickIntersectingCellsByPos(terrainSizeRect.Extend(margin+1), cellSize:data.areaSize) };
				chunkDeployRects = new CoordRect[] { CoordRect.PickIntersectingCellsByPos(terrainSizeRect, cellSize:chunkSize) };
				areaRemoveRects = new CoordRect[] { areaDeployRects[0] }; chunkRemoveRects = new CoordRect[] { chunkDeployRects[0] }; 
			}


			//checking and deploying
			bool areasChange = data.areas.CheckDeploy(areaDeployRects);
			if (areasChange) data.areas.Deploy(areaDeployRects, areaRemoveRects, parent:data, allowMove:false);

			bool chunksChange = chunks.CheckDeploy(chunkDeployRects);
			if (chunksChange) chunks.Deploy(chunkDeployRects, chunkRemoveRects, parent:this, allowMove:false);


			//on camera position changed
			if (!ArrayTools.EqualsVector3(prevCamPoses, camPoses, delta:1f) || chunksChange || areasChange) 
			{
				foreach (Chunk chunk in chunks.All()) 
				{
					//switching chunks
					if (currentSizeMode != SizeMode.Static)
					{
						float distanceAA = camPoses.DistToRectAxisAligned(chunk.rect.offset.x, chunk.rect.offset.z, chunk.rect.size.x);
						chunk.Switch(distanceAA);
					}
					else chunk.Switch(0);

					//setting chunk priorities
					float distance = camPoses.DistToRectCenter(chunk.rect.offset.x, chunk.rect.offset.z, chunk.rect.size.x);
					chunk.meshWorker.priority = 1f / distance;
					chunk.ambientWorker.priority = chunk.meshWorker.priority - 0.00001f;
					chunk.colliderApplier.priority = chunk.meshWorker.priority - 0.00002f;
					chunk.objectsWorker.priority = chunk.meshWorker.priority + 0.00001f;
				}

				//area priority
				foreach (Data.Area area in data.areas.All())
					area.generateWorker.priority = 1f / camPoses.DistToRectCenter(area.rect.offset.x, area.rect.offset.z, area.rect.size.x) - 0.00003f; //note that area priority is lower than the chunk one - chunk will wait for area anyways

				//moving horizon
				if (horizon != null)
				{
					Vector3 camPosesCenter = camPoses.Average();
					Vector3 horizonPosition = horizon.transform.localPosition;
					if (camPosesCenter.DistAxisAligned(horizonPosition) >= horizon.repositionThreshold*chunkSize || horizon.heightmap==null)
						horizon.RepositionTo(camPosesCenter);
				}

				if (prevCamPoses==null || prevCamPoses.Length!=camPoses.Length) prevCamPoses = new Vector3[camPoses.Length];
				for (int i=0; i<camPoses.Length; i++) prevCamPoses[i] = camPoses[i]; 
			}


			//editing in playmode
			if (Extensions.isPlaying && playmodeEdit) PlaymodeEdit();


			//processing threads
			ThreadWorker.Refresh();

			//refreshing horizon (after threads)
			if (horizon != null) horizon.Repaint();
			//horizon.RepositionTo(camPoses.Average());

			#if WDEBUG
			Profiler.EndSample();
			#endif
		}

		#region Events and actions

			public delegate void AreaGeneratedAction (Generator generator, Data.Area area);
			public static event AreaGeneratedAction OnAreaGeneratedEvent;
			public static void CallOnAreaGenerated (Data data, Generator generator, Data.Area area) //aka OnAreaWorkerApply
			{ 
				//finding instance
				Voxeland voxeland = null;
				foreach (Voxeland instance in instances)
					if (instance.data != null && instance.data.generator != null && instance.data.generator == generator) voxeland = instance;
				if (voxeland==null) return;

				//rebuilding chunks
				CoordRect chunksRect = CoordRect.PickIntersectingCells(area.rect, voxeland.chunkSize);
				foreach (Chunk chunk in voxeland.chunks.WithinRect(chunksRect)) 
				{ 
					chunk.RebuildAll();
				} 
			
				//marking horizon reposition
				if (voxeland.horizon != null) voxeland.horizon.Rebuild();
			
				//firing event
				if (OnAreaGeneratedEvent!=null) OnAreaGeneratedEvent(generator,area); 
			}


			public delegate void ChunkVisibilityChangedAction (Chunk chunk, bool visible);
			public event ChunkVisibilityChangedAction OnChunkVisibilityChangedEvent;
			public void CallOnChunkVisibilityChanged (Chunk chunk, bool visible)
			{
				//marking horizon visibility update
				if (horizon != null) horizon.updateVisNeeded = true;
			
				//firing event
				if (OnChunkVisibilityChangedEvent!=null) OnChunkVisibilityChangedEvent(chunk,visible);
			}


			public delegate void SimpleAction ();


			public event SimpleAction OnRebuildEvent;
			public void Rebuild () 
			{ 
				//refrteshing grass meshes
				//for (int i=0; i<grassTypes.array.Length; i++) grassTypes.array[i].LoadMeshes();
			
				chunks.Clear(); 
				chunks.deployedRects=null;
				prevCamPoses = null;
				
				#if MAPMAGIC
				MapMagic.Preview.Clear();
				#endif

				//apply materials
				landTypes.ApplyToMaterial(material);
				landTypes.ApplyToMaterial(farMaterial, horizon:true);
				grassTypes.ApplyToMaterial(grassMaterial);

				Update(); //TODO: do we really need update?

				//firing event
				if (OnRebuildEvent != null) OnRebuildEvent();
			}


			/*public event SimpleAction OnRefreshMaterialsEvent;
			public void RefreshMaterials ()
			{
				foreach (Chunk chunk in chunks.All())
				{
					chunk.meshRenderer.sharedMaterial = material;
					chunk.grassRenderer.sharedMaterial = grassMaterial;
				}
				if (horizon != null) horizon.meshRenderer.sharedMaterial = farMaterial;

				landTypes.ApplyToMaterial(this);
				landTypes.ApplyToMaterial(this);

				if (OnRefreshMaterialsEvent != null) OnRefreshMaterialsEvent();
			}*/

			public void Select (int num, System.Type type)
			{
				//un-selecting current selection
				landTypes.selected = -1; 
				constructorTypes.selected = -1; 
				objectsTypes.selected = -1; 
				grassTypes.selected = -1;

				//selecting new
				if (type == typeof(BlockType)) landTypes.selected = num;
				if (type == typeof(ObjectType)) objectsTypes.selected = num;
				if (type == typeof(ConstructorType)) constructorTypes.selected = num;
				if (type == typeof(GrassType)) grassTypes.selected = num;
			}

			public event SimpleAction OnGenerateEvent;
			public void Generate (bool force=false) //called on generator change
			{
				//removing all non-pinned areas - removing all areas at all unpinning them
				//if (force) data.areas.Clear(); //data.areas.RemoveNonPinned();

				foreach (Data.Area area in data.areas.All())
				{
					if (force) area.pinned = false;
					/*if (forceClear)
					{
						#if MAPMAGIC
						if (area.results != null) area.results.Clear();
						#endif
					}*/
					
					if (force || data.generator.instantGenerate) area.generateWorker.Start();


				}
				//starting chunks rebuild - in area oncomplete event

				//TODO: make switchable instant generate

				//firing event
				if (OnGenerateEvent != null) OnGenerateEvent();
			}

			public void ForceUpdate ()
			{
				prevCamPoses = null;
				Update();
			}

		#endregion

		#region MapMagic
		#if MAPMAGIC

			public void ClearResults (MapMagic.Generator gen)
			{
				foreach (Data.Area area in data.areas.All()) 
				{
					//area.generateWorker.Stop(); //just in case
					if (area.results!=null) 
					{
						if (data.generator.saveResults) area.results.ready.CheckRemove(gen);
						else area.results.Clear(); //if do not save intermediate - clearing all generators
					}
				}
			}

			public void ClearResults (MapMagic.Generator[] gens)
			{
				foreach (Data.Area area in data.areas.All())  
				{
					//area.generateWorker.Stop(); //just in case
					if (data.generator.saveResults) 
						for (int g=0; g<gens.Length; g++) 
							area.results.ready.CheckRemove(gens[g]);
					else area.results.Clear(); //if do not save intermediate - clearing all generators
				}
			}

			public void ClearResults ()
			{
				foreach (Data.Area area in data.areas.All()) 
				{
					//area.generateWorker.Stop(); //just in case
					if (area.results!=null) area.results.Clear();
				}
			}

			public MapMagic.Chunk.Results ClosestResults (Vector3 camPos)
			{
				Vector3 localCamPos = transform.InverseTransformPoint(camPos);
				float minDist = Mathf.Infinity;
				MapMagic.Chunk.Results closestResults = null;
				foreach (Data.Area area in data.areas.All())
				{
					float dist = (area.rect.Center.vector3 - localCamPos).sqrMagnitude;
					if (dist<minDist)
					{
						dist = minDist;
						closestResults = area.results;
					}
				}
				return closestResults;
			}

			public bool IsGeneratorReady (MapMagic.Generator gen)
			{
				foreach (Data.Area area in data.areas.All())
					if (area.results==null || !area.results.ready.Contains(gen)) return false;
				return true;
			}

			public bool IsWorking 
			{get{
				return ThreadWorker.IsWorking("MapMagic");
			}}

			public IEnumerable<MapMagic.Chunk.Results> Results ()
			{
				foreach (Data.Area area in data.areas.All())
					yield return area.results;
			}

			public IEnumerable<Transform> Transforms ()
			{
				foreach (Chunk chunk in chunks.All()) 
					yield return chunk.transform;
			}


		#endif
		#endregion

		#region Edit

			private CoordDir editPrevCoord = new CoordDir(false);

			public void PlaymodeEdit ()
			{
				//reading mouse button
				bool buttonDown; 
				if (!continuousPainting) buttonDown = Input.GetMouseButtonDown(0);
				else buttonDown = Input.GetMouseButton(0);

				//aiming
				Ray aimRay = Camera.main.ScreenPointToRay(Input.mousePosition);
				CoordDir aimCoord = PointOut(aimRay);

				//guard if aim coord not changed
				if (editPrevCoord == aimCoord && !buttonDown) return;
				editPrevCoord = aimCoord;

				//finding edit mode
				Voxeland.EditMode editMode = Voxeland.EditMode.none;
				if (buttonDown && !Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.RightAlt))
				{
					bool control = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
					bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
					
					if (control && shift) editMode = controlShiftEditMode;
					else if (shift) editMode = shiftEditMode;
					else if (control) editMode = controlEditMode;
					else editMode = standardEditMode;
				}

				//highlight
				if (highlight!=null)
				{
					if (aimCoord.exists) Highlight(aimCoord, brush, isEditing:editMode!=EditMode.none);
					else highlight.Clear(); //clearing highlight if nothing aimed or voxeland not selected
				}

				//altering
				if (editMode!=Voxeland.EditMode.none && aimCoord.exists) 
				{
					Alter(aimCoord, brush, editMode, landType:landTypes.selected, objectType:objectsTypes.selected, grassType:grassTypes.selected);
				}
			}

			public CoordDir PointOut (Ray ray)
			{
				//raycasting and miss-hit
				RaycastHit raycastHitData; 
				if ( !Physics.Raycast(ray, out raycastHitData, maxDistance:float.MaxValue)) //if was not hit or hit other object  //do not use maxValue here!
				{
					if (highlight!=null) highlight.Clear();
					return new CoordDir(false);
				}

				//non-voxeland collider
				if (!raycastHitData.collider.transform.IsChildOf(transform)) return new CoordDir(false);

				//aiming terrain
				if (raycastHitData.collider.transform.parent == transform)
				{
					//getting chunk
					Chunk chunk = raycastHitData.collider.GetComponent<Chunk>();

					//getting coordinate
					CoordDir aimCoord = new CoordDir(0,0,0,7);
					if (chunk.colliderIndexToCoord!=null && chunk.colliderIndexToCoord.Length!=0) aimCoord = chunk.colliderIndexToCoord[raycastHitData.triangleIndex/2]; //checking chunk.indexToCoord length because null is not serializable

					return aimCoord;
				}

				//aiming object
				else //if deep child (1st level child is checked above)
				{
					Vector3 pos = transform.InverseTransformPoint(raycastHitData.collider.transform.position);
					return new CoordDir( Mathf.FloorToInt(pos.x), (int)pos.y, Mathf.FloorToInt(pos.z), CoordDir.NormalToDir(raycastHitData.normal) );
					
				}
			}

			public void Highlight (CoordDir center, Brush brush, bool isEditing=false) //isEditing for displaying blob brush
			{
				#if WDEBUG
				Profiler.BeginSample("Highlight " + center);
				#endif

				if (!highlight.empty) highlight.Clear();

				//finding aiming transform and it's chunk component
				Transform tfm=null; Chunk chunk;
				Coord chunkCoord = Coord.PickCell(center.x, center.z, chunkSize);
				chunk = chunks[chunkCoord];
				if (chunk.colliderIndexToCoord==null || chunk.GetTriIndexByCoord(center) < 0)
				{
					tfm = chunk.GetObjectByCoord(center);
					chunk = null;
				}


				//if aiming nothing
				if (!center.exists) return;

				//aiming object
				else if (tfm != null)
				{
					Collider collider = tfm.GetComponent<Collider>();

					if (collider==null && tfm.childCount!=0)
						for (int c=0; c<tfm.childCount; c++)
						{
							collider = tfm.GetChild(c).GetComponent<Collider>();
							if (collider != null) break;
						}

					highlight.AddCube(
						transform.InverseTransformPoint(collider.bounds.center), 
						transform.InverseTransformVector(collider.bounds.size) );
				}

				//aiming terrain
				else if (chunk != null)
				{
					if (brush.form == Brush.Form.single) 
					{
						highlight.AddVoxelandFace(center,chunk);

						//adding block coords
						Matrix3<byte> matrix = new Matrix3<byte> (center.x-1, center.y-1, center.z-1, 3,3,3);
						data.FillMatrix(matrix);

						foreach (CoordDir blockCoord in ChunkMesh.SameBlockCoordinates(center,matrix)) 
						{
							highlight.AddVoxelandFace(blockCoord, chunk, opacity:0.35f);
						}
					}

					else if (brush.form == Brush.Form.blob )
					{
						if (isEditing) highlight.AddVoxelandFace(center,chunk); //drawing blob brush only when no edit is performed
						else 
						{
							CoordDir min = brush.Min(center);
							CoordDir max = brush.Max(center);
							Matrix3<byte> matrix = new Matrix3<byte> (min.x, min.y, min.z, max.x-min.x, max.y-min.y, max.z-min.z);

							data.FillMatrix(matrix);

							CoordDir[] neigCoords = ChunkMesh.NeighbourCoordinates(center, matrix, brush.extent, round:brush.round);

							//adding first face with 100% opacity
							{
								Coord neigChunkCoord = Coord.PickCell(neigCoords[0].x, neigCoords[0].z, chunkSize);
								highlight.AddVoxelandFace(neigCoords[0], chunks[neigChunkCoord]);
							}

							//adding other faces with lower opacity
							for (int n=1; n<neigCoords.Length; n++) 
							{
								Coord neigChunkCoord = Coord.PickCell(neigCoords[n].x, neigCoords[n].z, chunkSize);
								highlight.AddVoxelandFace(neigCoords[n], chunks[neigChunkCoord], opacity:0.7f);
							}
						}
					}
				
					else if (brush.form == Brush.Form.volume)
					{
						if (brush.round) highlight.AddSphere(center.vector3centered, brush.extent);
						else highlight.AddCube(center.vector3centered, new Vector3(brush.extent*2+1, brush.extent*2+1, brush.extent*2+1));
					}

					else if (brush.form == Brush.Form.stamp)
					{
						if (!brush.getStamp && brush.stamp!=null)
						{
							CoordDir min = brush.stamp.cube.Min; CoordDir max = brush.stamp.cube.Max;
							for (int x=min.x; x<max.x; x++)
								for (int y=min.y; y<max.y; y++)
									for (int z=min.z; z<max.z; z++)
										if (brush.stamp[x,y,z]) highlight.AddCube(center.vector3centered + new Vector3(x,y,z), Vector3.one); 
						}
						else 
						{
							Vector3 size = new Vector3 (brush.getStampMax.x-brush.getStampMin.x+1, brush.getStampMax.y-brush.getStampMin.y+1, brush.getStampMax.z-brush.getStampMin.z+1);
							highlight.AddCube(center.vector3 + new Vector3(brush.getStampMin.x, brush.getStampMin.y, brush.getStampMin.z) + size/2, size);
						}
					}
				}

				#if WDEBUG
				Profiler.EndSample();
				#endif
			}


			public void Alter (CoordDir center, Brush brush, EditMode mode,  int landType=-1, int objectType=-1, int grassType=-1)
			{

				//skiping continious edit
				if (prevCoordReseted) { prevAimingCoord = center; prevCoordReseted = false; }
				if (continuousPainting && center == prevAimingCoord) return;
				prevAimingCoord = center;

				//switching pos to opposite if single brush and add mode (not for grass)
				if (grassType<0 && brush.form==Brush.Form.single && mode==EditMode.add)
					center = center.opposite;

				//finding edit min and max
				CoordDir min = brush.Min(center,mode);
				CoordDir max = brush.Max(center,mode);
				CoordRect brushRect = new CoordRect(min.x, min.z, max.x-min.x, max.z-min.z);

				//altering land
				if (landType>=0)
				{
					Matrix3<byte> matrix = new Matrix3<byte> (min.x, min.y, min.z, max.x-min.x, max.y-min.y, max.z-min.z); //TODO reuse this matrix, copy it in undo
					data.FillMatrix(matrix);

					#if UNITY_EDITOR
					RecordUndo(matrix,0);
					#endif

					brush.Process(center, matrix, mode, (byte)landType);

					data.SetMatrix(matrix);
					data.RemoveObjectsByLandMatrix(matrix);

					foreach (Chunk chunk in chunks.WithinRect( CoordRect.PickIntersectingCells(brushRect.Expanded(meshMargin), chunkSize) )) chunk.Rebuild(mesh:true);
					foreach (Chunk chunk in chunks.WithinRect( CoordRect.PickIntersectingCells(brushRect.Expanded(ambientMargin), chunkSize) )) chunk.Rebuild(ambient:true);
					foreach (Chunk chunk in chunks.WithinRect( CoordRect.PickIntersectingCells(brushRect, chunkSize) )) chunk.Rebuild(objects:true);
				}

				else if (objectType>=0)
				{
					Matrix3<byte> matrix = new Matrix3<byte> (center.x, center.y, center.z, 1,1,1); //for undo only
					matrix.array[0] = (byte)data.GetObject(center.x, center.y, center.z);
					#if UNITY_EDITOR
					RecordUndo(matrix,1);
					#endif

					if (mode!=EditMode.dig) data.SetObject(center.x, center.y, center.z, objectType);
					else data.SetObject(center.x, center.y, center.z, -1);

					foreach (Chunk chunk in chunks.WithinRect( CoordRect.PickIntersectingCells(brushRect,chunkSize) )) chunk.Rebuild(objects:true);

					//TODO: set land type to 0 here
				}

				else if (grassType>=0)
				{
					center.y = -1;
					Matrix3<byte> matrix = new Matrix3<byte> (min.x, -1, min.z, max.x-min.x, 1, max.z-min.z); 
					data.FillGrass(matrix.Matrix2);

					#if UNITY_EDITOR
					RecordUndo(matrix,2);
					#endif
					
					//overriding brush form
					Brush.Form overridedBrushForm = brush.form;
					if (brush.form==Brush.Form.blob) overridedBrushForm = Brush.Form.volume;

					brush.Process(center, matrix, mode, overridedBrushForm, (byte)grassType);

					data.SetGrass(matrix.Matrix2);

					foreach (Chunk chunk in chunks.WithinRect( CoordRect.PickIntersectingCells(brushRect,chunkSize) )) { chunk.Rebuild(mesh:true); chunk.Rebuild(ambient:true); }
				}

				/*
				//get previous blocks
				Matrix3<byte> matrix;
				if (grassType<0)
				{
					matrix = new Matrix3<byte> (min.x, min.y, min.z, max.x-min.x, max.y-min.y, max.z-min.z); //TODO reuse this matrix, copy it in undo
					data.FillMatrix(matrix);
				}
				else
				{
					center.y = -1;
					matrix = new Matrix3<byte> (min.x, -1, min.z, max.x-min.x, 1, max.z-min.z); 
					data.FillGrass(matrix);
				}

				//record undo
				#if UNITY_EDITOR
				RecordUndo(matrix);
				#endif

				//overriding brush form
				Brush.Form overridedBrushForm = brush.form;
				if (objectType>=0 && (brush.form==Brush.Form.blob || brush.form==Brush.Form.volume)) overridedBrushForm = Brush.Form.single;
				if (grassType>=0 && brush.form==Brush.Form.blob) overridedBrushForm = Brush.Form.volume;

				//finding edit type
				int dataType = 0;
				if (landType>=0) dataType = landType + Data.minLandByte;
				else if (objectType>=0) dataType = objectType + Data.minObjectByte;
				else if (grassType>=0) dataType = grassType + Data.minGrassByte;
				if (dataType<0) Debug.LogError("Data type is less then zero: " + dataType); //this should not happen

				//setting blocks
				brush.Process(center, matrix, mode, overridedBrushForm, (byte)dataType);

				if (grassType<0) data.SetMatrix(matrix);
				else data.SetGrass(matrix);

				//rebuilding mesh, ambient and objects
				CoordRect brushRect = new CoordRect(min.x, min.z, max.x-min.x, max.z-min.z);
				if (landType>=0)
				{
					foreach (Chunk chunk in chunks.WithinRect(brushRect.Expanded(meshMargin).CellRect(chunkSize))) chunk.Rebuild(mesh:true);
					foreach (Chunk chunk in chunks.WithinRect(brushRect.Expanded(ambientMargin).CellRect(chunkSize))) chunk.Rebuild(ambient:true);
					foreach (Chunk chunk in chunks.WithinRect(brushRect.CellRect(chunkSize))) chunk.Rebuild(objects:true);
				}
				else if (objectType>=0)
					foreach (Chunk chunk in chunks.WithinRect(brushRect.CellRect(chunkSize))) chunk.Rebuild(objects:true);
				else if (grassType>=0)
					foreach (Chunk chunk in chunks.WithinRect(brushRect.CellRect(chunkSize))) { chunk.Rebuild(mesh:true); chunk.Rebuild(ambient:true); }
				*/
			}


		#endregion


		#region Defaults

			public static Material GetDefaultLandMaterial ()
			{
				Material mat = new Material( Shader.Find("Voxeland/Land") );
				mat.EnableKeyword("_TRIPLANAR");
				mat.name = "Land";
				return mat;
			}

			public static Material GetDefaultFarMaterial ()
			{
				Material mat = new Material( Shader.Find("Voxeland/Land") );
				mat.EnableKeyword("_HORIZON");
				if (mat.IsKeywordEnabled("_TRIPLANAR")) mat.DisableKeyword("_TRIPLANAR");
				mat.name = "Horizon";
				return mat;
			}

			public static Material GetDefaultGrassMaterial ()
			{
				Material mat = new Material( Shader.Find("Voxeland/Grass") );
				mat.name = "Grass";
				return mat;
			}

			public static Material GetDefaultHighlightMaterial ()
			{
				Material mat = new Material( Shader.Find("Voxeland/Highlight") );
				mat.name = "Highlight";
				return mat;
			}

		#endregion

//		#if WDEBUG
		public void OnDrawGizmos ()
		{
			//debugging
			//if (!debug) return; 

			//getting mouse pos
			/*UnityEditor.SceneView sceneview = UnityEditor.SceneView.lastActiveSceneView;
			if (sceneview==null || sceneview.camera==null) return;
			Vector2 mousePos = Event.current.mousePosition;
			mousePos = new Vector2(mousePos.x/sceneview.camera.pixelWidth, mousePos.y/sceneview.camera.pixelHeight);
			#if UNITY_5_4_OR_NEWER 	
			mousePos *= UnityEditor.EditorGUIUtility.pixelsPerPoint;
			#endif
			mousePos.y = 1 - mousePos.y;
			Ray aimRay = sceneview.camera.ViewportPointToRay(mousePos);

			CoordDir aimCoord = PointOut(aimRay, null);
			Gizmos.color = Color.red;
			//Gizmos.DrawWireCube(new Vector3(aimCoord.x+0.5f,aimCoord.y+0.5f,aimCoord.z+0.5f), Vector3.one*5);
			Extensions.GizmosDrawFrame(new Vector3(aimCoord.x+0.5f,aimCoord.y+0.5f,aimCoord.z+0.5f), Vector3.one*5, 5);

			//drawing top and bottom point
			int topPoint = 0; int bottomPoint = 0;
			if (aimCoord.exists)
			{
				data.GetTopBottomPoints(new CoordRect(aimCoord.x, aimCoord.z,1,1), out topPoint, out bottomPoint);
				Gizmos.DrawWireCube( new Vector3(aimCoord.x+0.5f, topPoint+0.5f, aimCoord.z+0.5f), new Vector3(0.5f,0.5f,0.5f));
				Gizmos.DrawWireSphere( new Vector3(aimCoord.x+0.5f, bottomPoint+0.5f, aimCoord.z+0.5f), 0.5f);
			}*/
		
			/*foreach (Chunk chunk in ChunksInRange(aimCoord,2))
			{
				Vector3 pos = chunk.rect.offset.vector3;
				Vector3 size = chunk.rect.size.vector3;
				size.y += 50;
				Gizmos.color = Color.green;
				//Gizmos.DrawWireCube(pos+size/2, size);
				//Extensions.GizmosDrawFrame(pos+size/2, size, chunkSize);
			}*/

			//drawing deployed chunks
			//chunks.DrawGizmoRects();

			Gizmos.color = Color.gray;
			if (data != null)
				data.areas.DrawObjectRects(data.areaSize);
		}
//		#endif

		#region Serialization
			[SerializeField] public Chunk[] serializedChunks = new Chunk[0];
			[SerializeField] public Coord[] serializedChunkCoords = new Coord[0];
			[SerializeField] public bool[] serializedChunkPin = new bool[0];

			[SerializeField] private int selectedLandBlock = 0;  //TODO: make block types serialization callback recievers?
			[SerializeField] private int selectedGrassBlock = 0;
			[SerializeField] private int selectedObjectBlock = 0;

			//public TempChunk temp;

			public virtual void OnBeforeSerialize () 
			{
				//if (saveMeshes)
					chunks.Serialize(out serializedChunks, out serializedChunkCoords, out serializedChunkPin);
				//else
				//	{ serializedChunks=new Chunk[0]; serializedChunkCoords = new Coord[0]; serializedChunkPin = new bool[0]; }

				selectedLandBlock = landTypes.selected;
				selectedGrassBlock = grassTypes.selected;
				selectedObjectBlock = objectsTypes.selected;
			}

			public virtual void OnAfterDeserialize ()   
			{  
				chunks.Deserialize(serializedChunks, serializedChunkCoords, serializedChunkPin);

				landTypes.selected = selectedLandBlock;
				grassTypes.selected = selectedGrassBlock;
				objectsTypes.selected = selectedObjectBlock;
				if (selectedLandBlock>=0 && selectedGrassBlock>=0) selectedGrassBlock = -1;  //should be selected only one!
				if (selectedLandBlock>=0 && selectedObjectBlock>=0) selectedObjectBlock = -1;
				if (selectedGrassBlock>=0 && selectedObjectBlock>=0) selectedObjectBlock = -1;

			}
		#endregion

		#region Undo

			[System.NonSerialized] public List<Matrix3<byte>> undo = new List<Matrix3<byte>>(); //list is better than stack since we will remove it's beginning
			[System.NonSerialized] public List<int> undoTypes = new List<int>();
			public int undoChange = 0;

			public void RecordUndo (Matrix3<byte> matrix, int type) //0 for land, 1 for objects, 2 for grass
			{
				undo.Add(matrix.Copy());
				undoTypes.Add(type);

				if (undo.Count > 20) { undo.RemoveRange(0, undo.Count-10); undoTypes.RemoveRange(0, undoTypes.Count-10); }

				#if UNITY_EDITOR
				UnityEditor.Undo.RecordObject(this, "Voxeland Edit");
				undoChange++;
				UnityEditor.EditorUtility.SetDirty(this);
				#endif
			}

			public void PerformUndo () //similar to Edit fn
			{
				Matrix3<byte> matrix = undo[undo.Count-1];
				undo.RemoveAt(undo.Count-1);

				int type = undoTypes[undoTypes.Count-1];
				undoTypes.RemoveAt(undoTypes.Count-1);
				
				if (type==0) data.SetMatrix(matrix);
				else if (type==1) data.SetObject(matrix.cube.offset.x, matrix.cube.offset.y, matrix.cube.offset.z, matrix.array[0]==255? -1 :  matrix.array[0]); //HACK: -1 is marked as 255 when converted to byte.
				else data.SetGrass(matrix.Matrix2);

				//clearing mesh and ambient
				CoordRect brushRect = matrix.cube.rect; 

				//refreshing
				foreach (Chunk chunk in chunks.WithinRect( CoordRect.PickIntersectingCells(brushRect.Expanded(meshMargin), chunkSize) )) chunk.Rebuild(mesh:true);
				foreach (Chunk chunk in chunks.WithinRect( CoordRect.PickIntersectingCells(brushRect.Expanded(ambientMargin), chunkSize) )) chunk.Rebuild(ambient:true);
				foreach (Chunk chunk in chunks.WithinRect( CoordRect.PickIntersectingCells(brushRect, chunkSize) )) chunk.Rebuild(objects:true);
				//TODO: Performing undo (ctrl-z or editor-undo) changes object pool variables (transforms list). So it goes.
			}

		#endregion
	}

}
