using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_5_5_OR_NEWER
using UnityEngine.Profiling;
#endif

using Voxeland5;

namespace Voxeland5
{
	[ExecuteInEditMode]
	public class Horizon : MonoBehaviour
	{
		public Voxeland voxeland;

		public int meshSize = 60; //in chunks. Mesh bounding box size
		public int scale = 1;
		public int textureResolutions = 512;

		public HashSet<Data.Area> usedAreas = new HashSet<Data.Area>();

		public Vector3 position;
		//private Vector3 appliedPosition; //the last applied position to calculate visibility

		public bool updateVisNeeded = false;

		[System.NonSerialized] public Texture2D heightmap; 
		[System.NonSerialized] public Texture2D typemap; 
		[System.NonSerialized] public Texture2D visibilityMap; 
		[System.NonSerialized] public byte[] heightmapBytes;
		[System.NonSerialized] public byte[] typeBytes;
		[System.NonSerialized] public byte[] visibilityBytes;
		//[System.NonSerialized] Matrix2<bool> displayedChunks;
		#if UNITY_EDITOR
		[System.NonSerialized] static bool mapsAppliedToMat = false;
		#endif

		public MeshFilter meshFilter;
		public MeshRenderer meshRenderer;

		[System.NonSerialized] public ThreadWorker repositionWorker;
		public float repositionThreshold = 3; //in chunks

		
		
		//public Coord newCoord;

		public bool updateVis;
		public void OnDrawGizmos () { if (updateVis) UpdateVisibility(true); updateVis=false; }



		public static Horizon Create (Voxeland voxeland=null)
		{
			GameObject go = new GameObject();
			go.name = "Horizon";
			if (voxeland!=null) go.transform.parent = voxeland.transform;
			Horizon horizon = go.AddComponent<Horizon>();
			horizon.voxeland = voxeland;
			horizon.meshRenderer = go.AddComponent<MeshRenderer>();
			horizon.meshFilter = go.AddComponent<MeshFilter>();
			voxeland.horizon = horizon;
		//	horizon.OnEnable();
			horizon.meshFilter.sharedMesh = Resources.Load<Transform>("VoxelandHorizonMesh").GetComponent<MeshFilter>().sharedMesh;
			horizon.meshFilter.sharedMesh.bounds = new Bounds(horizon.meshFilter.sharedMesh.bounds.center, new Vector3(horizon.meshFilter.sharedMesh.bounds.extents.x+10000, 20000, horizon.meshFilter.sharedMesh.bounds.extents.z+10000) );
			horizon.meshRenderer.sharedMaterial = voxeland.farMaterial;
			if (voxeland.guiHideWireframe) go.transform.ToggleDisplayWireframe(false);
			
			//events
			//voxeland.OnGeneratorChanged += delegate (Voxeland v) { horizon.Refresh(v, null, forceReposition: true); };

			return horizon;
		}


		//hack to avoid procedural textures reset on scene save
		#if UNITY_EDITOR
		public class SaveAssetsProcessor : UnityEditor.AssetModificationProcessor 
		{
			static string[] OnWillSaveAssets (string[] paths) 
			{
				mapsAppliedToMat = false;
    			return paths;
			}
		}
		public void Update ()
		{
			if (mapsAppliedToMat) return;
			Material mat = meshRenderer.sharedMaterial;
			//if (mat == null) { mat = voxeland.farMaterial; meshRenderer.sharedMaterial = mat; }
			if (mat == null) return;
			if (heightmap != null && mat.HasProperty("_HorizonHeightmap")) mat.SetTexture("_HorizonHeightmap", heightmap);
			if (typemap != null && mat.HasProperty("_HorizonTypemap")) mat.SetTexture("_HorizonTypemap", typemap);
			if (visibilityMap != null && mat.HasProperty("_HorizonVisibilityMap")) mat.SetTexture("_HorizonVisibilityMap", visibilityMap);
			mapsAppliedToMat = true;
		}
		#endif


		public void Repaint ()
		{
			//if (repositionNeeded) { Reposition(); repositionNeeded = false; }
			if (updateVisNeeded) { UpdateVisibility(); updateVisNeeded = false; }
		}

		public void RepositionTo (Vector3 newPosition) 
		{ 
			position = new Vector3(
				Mathf.FloorToInt(newPosition.x/voxeland.chunkSize) * voxeland.chunkSize, 
				0, 
				Mathf.FloorToInt(newPosition.z/voxeland.chunkSize) * voxeland.chunkSize ); 
			Rebuild(); 
		}

		public void Rebuild () //calculating height/typemap and placing object + updating visibility in apply
		{
			
			#if WDEBUG 
			Profiler.BeginSample("Rebuild");
			#endif

				//preparing worker
				if (repositionWorker == null)
				{
					repositionWorker = new ThreadWorker();
					repositionWorker.name = "Horizon Reposition";
					repositionWorker.Calculate += CalculateReposition;
					repositionWorker.Apply += ApplyReposition; 
					repositionWorker.tag = "VoxelandHorizon";
					repositionWorker.priority = 2; //prior to everything!
				}

				//placing object
				//transform.localPosition = camCoord.vector3; //moving in apply to prevent while-generate-mismatch
				//transform.localScale = new Vector3(voxeland.chunks.cellSize, 1, voxeland.chunks.cellSize);

				//saving ref variables
				usedAreas.Clear();
				//CoordRect areasRect = new CoordRect(currentCoord, (int)(meshSize*voxeland.chunks.cellSize/2), voxeland.data.areas.cellRes);
				CoordRect areasRect = CoordRect.PickIntersectingCellsByPos(transform.position, voxeland.data.areaSize/2, cellSize:voxeland.data.areaSize);
				foreach (Data.Area area in voxeland.data.areas.WithinRect(areasRect))
				{
					if (!area.generateWorker.ready) continue;
					usedAreas.Add(area);
				}

				//starting worker
				if (heightmap==null) { CalculateReposition(); ApplyReposition(); }
				else repositionWorker.Start();

			#if WDEBUG
			Profiler.EndSample();
			#endif
		}


		public void CalculateReposition ()
		{
			if (heightmapBytes==null || heightmapBytes.Length!=textureResolutions*textureResolutions*4) heightmapBytes = new byte[textureResolutions*textureResolutions*4];
			if (typeBytes==null || typeBytes.Length!=textureResolutions*textureResolutions) typeBytes = new byte[textureResolutions*textureResolutions];

			if (repositionWorker!=null && repositionWorker.stop) return; //could be called directly without worker

			//horizon rect (in worldunits)
			float size = meshSize*voxeland.chunkSize*scale;
			float step = size / textureResolutions;
			float minX = position.x - size/2; float maxX = position.x + size/2;
			float minZ = position.z - size/2; float maxZ = position.z + size/2;

			//getting heights and types
			int counter = 0;
			//Data.Area area = null;
			for (float z=maxZ; z>minZ+0.001f; z-=step)
				for (float x=maxX; x>minX+0.001f; x-=step)
			{
				int ix = (int)x; int iz = (int)z;

				int height=0; byte type=0;
				voxeland.data.GetTopTypePoint(ix,iz, out height, out type); //TODO ref area. Causes Unity breakdown

				byte hb = (byte)(height/250f);
				heightmapBytes[counter*4] = hb;
				heightmapBytes[counter*4+1] = (byte)(height-hb);

				typeBytes[counter] = (byte)type; 

				counter++;

				if (repositionWorker!=null && repositionWorker.stop) return;
			}

			if (repositionWorker!=null && repositionWorker.stop) return;

			//getting normals
			for (int i=0; i<heightmapBytes.Length; i+=4)
			{
				float curHeight = heightmapBytes[i]*250 + heightmapBytes[i+1];
						
				float prevXHeight = curHeight;
				if (i>=4) prevXHeight = heightmapBytes[i-4]*250+heightmapBytes[i-4+1];

				float nextXHeight = curHeight;
				if (i<heightmapBytes.Length-8) nextXHeight = heightmapBytes[i+4]*250 + heightmapBytes[i+4+1];

				float prevZHeight = curHeight;
				if (i-textureResolutions*4 >= 0) prevZHeight = heightmapBytes[i-textureResolutions*4]*250 + heightmapBytes[i-textureResolutions*4+1];

				float nextZHeight = curHeight;
				if (i+textureResolutions*4+1 < heightmapBytes.Length) nextZHeight = heightmapBytes[i+textureResolutions*4]*250 + heightmapBytes[i+textureResolutions*4+1];

				//normalizing
				Vector3 normal = new Vector3(prevXHeight-nextXHeight, step, prevZHeight-nextZHeight).normalized;  //actually step should be multiplied by 4

				heightmapBytes[i+2] = (byte)((normal.x/2f+0.5f)*256);
				heightmapBytes[i+3] = (byte)((normal.z/2f+0.5f)*256);

				if (repositionWorker!=null && repositionWorker.stop) return;
			}
		}

		public void ApplyReposition ()
		{
			if (heightmap==null || heightmap.width!=textureResolutions || heightmap.height!=textureResolutions) 
			{
				heightmap = new Texture2D(textureResolutions, textureResolutions, TextureFormat.RGBA32, false, true);
				heightmap.wrapMode = TextureWrapMode.Clamp;
			}
			heightmap.LoadRawTextureData(heightmapBytes);
			heightmap.Apply();

			if (repositionWorker!=null && repositionWorker.stop) return;
			
			if (typemap==null || typemap.width!=textureResolutions || typemap.height!=textureResolutions) 
			{
				typemap = new Texture2D(textureResolutions, textureResolutions, TextureFormat.Alpha8, false, true);
				typemap.wrapMode = TextureWrapMode.Clamp;
				typemap.filterMode = FilterMode.Point;
			}
			typemap.LoadRawTextureData(typeBytes);
			typemap.Apply();

			if (repositionWorker!=null && repositionWorker.stop) return;

			if (meshRenderer == null) meshRenderer = gameObject.GetComponent<MeshRenderer>();

			Material mat = meshRenderer.sharedMaterial;
			if (mat != null)
			{
				if (mat.HasProperty("_HorizonHeightmap")) mat.SetTexture("_HorizonHeightmap", heightmap);
				if (mat.HasProperty("_HorizonTypemap")) mat.SetTexture("_HorizonTypemap", typemap);
				#if UNITY_EEDITOR
				mapsAppliedToMat = true;
				#endif
			}

			if (repositionWorker!=null && repositionWorker.stop) return;

			transform.localPosition = position; //moving in apply to prevent while-generate-mismatch
			transform.localScale = new Vector3(voxeland.chunkSize*scale, 1, voxeland.chunkSize*scale);

			UpdateVisibility(checkAreasGenerated:true);
		}

		public void OnWillRenderObject ()
		{
//			UpdateVisibility();
//			Debug.Log("REnder " + Time.renderedFrameCount);
		}


		public void UpdateVisibility (bool checkAreasGenerated = false)
		{
			UnityEngine.Profiling.Profiler.BeginSample("Update Visibility");
			
			//horizon rect (in worldunits)
			int chunkSize = voxeland.chunkSize;
		//	float size = meshSize*chunkSize;
		//	float step = size / meshSize;
		//	float minX = position.x - size/2; float maxX = position.x + size/2;
		//	float minZ = position.z - size/2; float maxZ = position.z + size/2;

			//calculating
			Coord rectCenter = Coord.PickCellByPos(transform.localPosition, cellSize:chunkSize);
		//	CoordRect rect = CoordRect.PickIntersectingCells(rectCenter, meshSize/2); //rect = new CoordRect(p,r,cellSize) is not possible - we need fixed rect size
			Coord min = rectCenter - meshSize*scale/2; Coord max = rectCenter + meshSize*scale/2;

			if (visibilityBytes==null || visibilityBytes.Length!=meshSize*scale*meshSize*scale) 
			{
				visibilityBytes = new byte[meshSize*scale*meshSize*scale];
				checkAreasGenerated = true;
			}

			int counter = 0;
			//for (float z=maxZ; z>minZ+0.001f; z-=step)
			//	for (float x=maxX; x>minX+0.001f; x-=step)
			for (int z=max.z-1; z>=min.z; z--)
				for (int x=max.x-1; x>=min.x; x--)
			{
				Chunk chunk = voxeland.chunks[x,z];
				
				//if no chunk
				if (chunk == null) visibilityBytes[counter] = 255;
				/*{ 
					//finding if chunk's area is generated
					if (checkAreasGenerated) //leaving non-chunks bytes intact if chack areas generated is turned off (it takes 1.5ms and should be done on reposition)
					{
						visibilityBytes[counter] = 255;
					
						CoordRect areaRect = new CoordRect(x*chunkSize, z*chunkSize, chunkSize, chunkSize);
						areaRect.ToCellRect(voxeland.data.areas.cellRes);
						foreach (Data.Area area in voxeland.data.areas.WithinRect(areaRect, skipMissing:false))
							if (area == null || !area.generated) { visibilityBytes[counter] = 0; break; }
					}
				}*/

				//if chunk preset
				else
				{
					if (chunk.meshRenderer.enabled) visibilityBytes[counter] = 0;
					else visibilityBytes[counter] = 255;
				}

				counter++;
			}

			//apply
			if (visibilityMap==null || visibilityMap.width!=meshSize*scale || visibilityMap.height!=meshSize*scale) 
			{
				visibilityMap = new Texture2D(meshSize*scale, meshSize*scale, TextureFormat.Alpha8, false, true);
				visibilityMap.wrapMode = TextureWrapMode.Clamp;
				visibilityMap.filterMode = FilterMode.Point;
			}

			visibilityMap.LoadRawTextureData(visibilityBytes);
			visibilityMap.Apply();

			if (meshRenderer == null) meshRenderer = gameObject.GetComponent<MeshRenderer>();

			Material mat = meshRenderer.sharedMaterial;
			if (mat != null && mat.HasProperty("_HorizonVisibilityMap")) mat.SetTexture("_HorizonVisibilityMap", visibilityMap);
			#if UNITY_EDITOR
			mapsAppliedToMat = true;
			#endif

			UnityEngine.Profiling.Profiler.EndSample();
		}
	}
}
