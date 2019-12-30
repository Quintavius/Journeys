using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_5_5_OR_NEWER
using UnityEngine.Profiling;
#endif

using Voxeland5;

namespace Voxeland5
{
	[System.Serializable]  
	public class Chunk : MonoBehaviour, IChunk, ISerializationCallbackReceiver
	{
		//always assigned before BuildMesh
		public Voxeland voxeland; 
		//[System.NonSerialized] public Data data; 
		//public int meshMargin = 2;
		//public int ambientMargin = 7;

		public Coord coord {get; set;} //in chunk-space
		public CoordRect rect;		 //in block-space, to read data
		//public Rect pos {get; set;}
		public int hash {get; set;}
		public bool pinned {get; set;}

		public Vector3 meshOffset;
		public float currentDistance = 200000000; //a distance to closest camera
		public float currentDistanceAA = 200000000; //axis-aligned distance to closest camera

		public MeshRenderer meshRenderer;
		public MeshFilter meshFilter;
		public MeshCollider meshCollider;
		public MeshRenderer grassRenderer;
		public MeshFilter grassFilter;

		public Mesh hiMesh; //should not be null, cannot be created in default constructor
		public Mesh loMesh; 
		public Mesh grassMesh; 

		[System.NonSerialized] public MeshWrapper hiWrapper; //sets to null after apply
		[System.NonSerialized] public MeshWrapper loWrapper;
		[System.NonSerialized] public MeshWrapper grassWrapper;
		[System.NonSerialized] public Matrix3<byte> ambient; //TODO use byte matrix instead of float

		[SerializeField] private CoordDir[] indexToCoord; //no need to make it public, should be used only by Chunk.  Length = lopolyMesh.numTris/2
		public CoordDir[] colliderIndexToCoord; //same as index to coord, but sets only when appling collider

		public ThreadWorker meshWorker = new ThreadWorker("Chunk"); //tag is assigned in OnMove
		public ThreadWorker ambientWorker = new ThreadWorker("Ambient");
		public ThreadWorker objectsWorker = new ThreadWorker("Objects");
		public ThreadWorker colliderApplier = new ThreadWorker("Collider");

		public bool meshRequired;
		public bool colliderRequired;
		public bool objectsRequired;


		public int GetTriIndexByCoord (CoordDir coord)
		{
			if (colliderIndexToCoord == null) throw new System.NullReferenceException("No colliderIndexToCoord defined");

			for (int i=0; i<colliderIndexToCoord.Length; i++) 
				if (colliderIndexToCoord[i].x==coord.x && colliderIndexToCoord[i].y==coord.y && colliderIndexToCoord[i].z==coord.z && colliderIndexToCoord[i].dir==coord.dir) //4 times faster than //if (chunk.colliderIndexToCoord[i] == center)
					return i;
			
			//throw new System.ArgumentOutOfRangeException("Could not find face index with coord: " + coord); //happens right after blob brush edit
			return -1;
		}

		public Transform GetObjectByCoord (CoordDir coord)
		{
			Coord chunkCoord = this.rect.offset;
			int childCount = transform.childCount;
			for (int i=0; i<childCount; i++)
			{
				Transform tfm = transform.GetChild(i);

				if (Mathf.FloorToInt(tfm.localPosition.x + chunkCoord.x) == coord.x && (int)tfm.localPosition.y == coord.y && Mathf.FloorToInt(tfm.localPosition.z + chunkCoord.z) == coord.z)
				{
					if (tfm.name == "Grass") continue; //ignoring grass objs
					return tfm;
				}
			}
			return null;
		}

		public void InitWorkers ()
		{
			//sequence:
			//- generate area - mesh thread	   -   mesh apply  \ (force amb.ap.)
			//				  \ ambient thread /			    - ambient apply
			//												     \ collider apply

			//checking workers
			if (!meshWorker.initialized) 
			{
				meshWorker.Calculate += CalculateMesh;
				meshWorker.Apply += ApplyMesh; 

				meshWorker.threadCondition = delegate() 
				{ 
					//checking if area is generated
					CoordRect areasRect = CoordRect.PickIntersectingCells( rect.Expanded(voxeland.meshMargin+1), voxeland.data.areaSize);
					foreach (Data.Area area in voxeland.data.areas.WithinRect(areasRect,skipMissing:false))
						if (area==null || !area.generateWorker.ready) return false;
					return true;
				};

				meshWorker.applyCondition = delegate()
				{
					//applies mesh only when ambient is calculated
					return ambientWorker.calculated;
				};
			}

			if (!ambientWorker.initialized)
			{
				ambientWorker.Calculate += CalculateAmbient;
				ambientWorker.Apply += ApplyAmbient;

				ambientWorker.threadCondition = delegate() 
				{ 
					//checking if area is generated
					CoordRect areasRect = CoordRect.PickIntersectingCells( rect.Expanded(voxeland.meshMargin+1), voxeland.data.areaSize);
					foreach (Data.Area area in voxeland.data.areas.WithinRect(areasRect,skipMissing:false))
						if (area==null || !area.generateWorker.ready) return false;
					return true;
				};
				ambientWorker.applyCondition = delegate()
				{
					//checking if mesh is built
					return meshWorker.ready  && !meshWorker.processing; //condition is ignored when ambient is forced to apply in mesh
				};
			}

			if (!colliderApplier.initialized)
			{
				colliderApplier.Apply += delegate() 
				{ 
					if (colliderApplier.stop || this==null) return;
					//meshCollider.sharedMesh = null; //needs to be reset only when previous mesh was empty
					meshCollider.sharedMesh = loMesh; 
					colliderIndexToCoord = indexToCoord;
					voxeland.prevCoordReseted = true;
				};

				colliderApplier.applyCondition = delegate() 
				{ 
					if (this==null || colliderApplier.stop) { return true; } 
					return meshWorker.ready; 
				};
			}

			if (!objectsWorker.initialized)
			{
				objectsWorker.Apply += ApplyObjects;
			}
		}

		public void OnBeforeSerialize () { }
		public void OnAfterDeserialize () { InitWorkers(); }
		//public void OnEnable () { InitWorkers(); }  //does not happen after each serialization

#if WDEBUG
		public void OnDrawGizmos ()
		{
			if (voxeland == null) return;
			if (meshWorker != null && colliderApplier != null)
			{
				Gizmos.color = Color.red;
				if (meshWorker.ready) Gizmos.color = Color.yellow; 
				if (meshWorker.ready && colliderApplier.ready) Gizmos.color = Color.green;

				Extensions.GizmosDrawFrame(new Vector3(rect.offset.x+rect.size.x/2, 0, rect.offset.z+rect.size.z/2), new Vector3(rect.size.x, 0, rect.size.z), 1);
				//Gizmos.DrawWireCube( new Vector3(pos.x+pos.width/2, 0, pos.y+pos.height/2), new Vector3(pos.width, 0, pos.height) );
			}
			//Gizmos.DrawWireCube( new Vector3(transform.position.x, 0, transform.position.z), new Vector3(voxeland.chunks.cellSize, 0, voxeland.chunks.cellSize));
		}
		#endif

		public void OnCreate (object voxelandBox) 
		{
			GameObject go = gameObject;
			voxeland = (Voxeland)voxelandBox;
			go.transform.parent = voxeland.transform;
			go.transform.localScale = Vector3.one; //no need to make it every move
			go.transform.localRotation = Quaternion.identity;
			
			//setting save flags
			if (!voxeland.saveMeshes) go.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;

			//creating chunk components
			meshRenderer = go.AddComponent<MeshRenderer>();
			meshCollider = go.AddComponent<MeshCollider>();
			#if UNITY_2017_3_OR_NEWER
			meshCollider.cookingOptions = MeshColliderCookingOptions.None;
			#endif
			if (voxeland.hideColliderWire) meshCollider.hideFlags = meshCollider.hideFlags | HideFlags.HideInHierarchy;
			meshFilter = go.AddComponent<MeshFilter>();
			meshRenderer.sharedMaterial = voxeland.material;

			//creating meshes
			hiMesh = new Mesh();
			loMesh = new Mesh(); 
			grassMesh = new Mesh(); 

			//adding grass
			GameObject grassGo = new GameObject();
			grassGo.name = "Grass";
			grassGo.transform.parent = go.transform;
			grassGo.transform.localScale = Vector3.one;
			grassRenderer = grassGo.AddComponent<MeshRenderer>();
			grassFilter = grassGo.AddComponent<MeshFilter>();
			grassRenderer.sharedMaterial = voxeland.grassMaterial;
			grassRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

			InitWorkers();
			OnMove(coord,coord); 
		}

		public void OnMove (Coord oldCoord, Coord newCoord) 
		{
			rect = new CoordRect(coord.x*voxeland.chunkSize, coord.z*voxeland.chunkSize, voxeland.chunkSize, voxeland.chunkSize);
			transform.localPosition = rect.offset.vector3;
			gameObject.name = voxeland.chunkName + " " + coord.x + "," + coord.z;

			string coordString = "(" + coord.x + "," + coord.z + ")";
			if (meshWorker != null) { meshWorker.Stop(); meshWorker.name = "Chunk " + coordString; meshWorker.tag = "VoxelandChunk "  + coordString; }
			if (ambientWorker != null) { ambientWorker.Stop(); ambientWorker.name = "Ambient "  + coordString; ambientWorker.tag = "VoxelandChunk "  + coordString; }
			if (objectsWorker != null) { objectsWorker.Stop(); objectsWorker.name = "Objects "  + coordString; objectsWorker.tag = "VoxelandChunk "  + coordString; }
			if (colliderApplier != null) { colliderApplier.Stop(); colliderApplier.name = "Collider " + coordString; colliderApplier.tag = "VoxelandChunk "  + coordString; }

			if (voxeland.guiHideWireframe) transform.ToggleDisplayWireframe(false);
			meshRenderer.enabled = false; voxeland.CallOnChunkVisibilityChanged(this,false); 
			
			meshRequired = false;
			colliderRequired = false;
			objectsRequired = false;

			//copy layer, tag, scripts from to chunks
			if (voxeland.copyLayersTags)
			{
				GameObject go = gameObject;
				go.layer = voxeland.gameObject.layer;
				go.isStatic = voxeland.gameObject.isStatic;
				try { go.tag = voxeland.gameObject.tag; } catch { Debug.LogError("Voxeland: could not copy object tag"); }
			}
			if (voxeland.copyComponents)
			{
				GameObject go = gameObject;
				MonoBehaviour[] components = voxeland.GetComponents<MonoBehaviour>();
				for (int i=0; i<components.Length; i++)
				{
					if (components[i] is Voxeland || components[i] == null) continue; //if Voxeland itself or script not assigned
					if (gameObject.GetComponent(components[i].GetType()) == null) ReflectionExtensions.CopyComponent(components[i], go);
				}
			}

			ClearAll(); 
		}

		public void OnRemove () 
		{ 
			if (this == null) return; //it could be destroyed by undo

			ClearAll();

			GameObject.DestroyImmediate(gameObject);
		}


		public void Switch (float distance)
		{
			//finding required stage
			meshRequired = distance<=voxeland.voxelLoRange;
			objectsRequired = distance<=voxeland.objectsRange;
			colliderRequired = distance<=voxeland.collisionRange;

			//switching lod
			bool hiLod = distance<=voxeland.voxelHiRange;
			if (hiLod && meshFilter.sharedMesh!=hiMesh) meshFilter.sharedMesh = hiMesh;
			else if (!hiLod && meshFilter.sharedMesh!=loMesh) meshFilter.sharedMesh = loMesh;

			//toggle renderer
			if (loMesh.vertexCount != 0) 
				{ if (!meshRenderer.enabled) { meshRenderer.enabled=true; voxeland.CallOnChunkVisibilityChanged(this,true); } }
			else 
				{ if (meshRenderer.enabled) { meshRenderer.enabled=false; voxeland.CallOnChunkVisibilityChanged(this,false); } }

			//switching grass
			bool grassRequired = distance<=voxeland.grassRange;
			if (grassRequired && !grassRenderer.enabled) grassRenderer.enabled = true;
			else if (!grassRequired && grassRenderer.enabled) grassRenderer.enabled = false;

			//removing out-of-range chunks
			if (!meshRequired  &&  distance>voxeland.voxelRemoveRange  &&  (meshWorker.ready||colliderApplier.ready)) ClearAll();

			Refresh();
		}

		public void Refresh ()  
		{
			//starting blanks
			if (meshRequired && (meshWorker.blank||ambientWorker.blank))
			{
				meshWorker.Start(); 
				ambientWorker.Start(); //starting ambient together with chunk
			}
			if (colliderRequired && colliderApplier.blank) colliderApplier.Start(); //will wait for mesh to generate
			if (objectsRequired && objectsWorker.blank) objectsWorker.Start();

			//stopping meshes if they are out of range
			if (!meshRequired)
			{
				if (!meshWorker.ready) meshWorker.Stop();
				if (!ambientWorker.ready) ambientWorker.Stop(); 
			}
			if (!objectsRequired && !objectsWorker.ready) objectsWorker.Stop(); 
			if (!colliderRequired && !colliderApplier.ready) { colliderApplier.Stop(); meshCollider.sharedMesh = null; colliderIndexToCoord = null; }
		}

		/*public void RebuildMesh ()
		{
			if (meshRequired) meshWorker.Start();
			else { ClearMesh(); ClearAmbient(); }

			if (colliderRequired) colliderApplier.Start();
			else ClearCollider();
		}

		public void RebuildAmbient ()
		{
			if (meshRequired) ambientWorker.Start();
			else { ClearMesh(); ClearAmbient(); }
		}

		public void RebuildObjects ()
		{
			if (objectsRequired) objectsWorker.Start();
			else ClearObjects();
		}*/

		public void RebuildAll () { Rebuild(true, true, true); }
		public void Rebuild (bool mesh=false, bool ambient=false, bool objects=false)
		{
			if (meshRequired) 
			{
				if (mesh) 
				{
					meshWorker.Start();
					if (colliderRequired) colliderApplier.Start();
				}
				if (ambient) ambientWorker.Start();
			}
			else Clear(mesh:true, ambient:true, collider:true);

			if (objects)
			{
				if (objectsRequired) 
					objectsWorker.Start();
				else Clear(objects:true);
			}
		}


		/*public void ClearMesh ()
		{
			meshWorker.Stop();

			hiMesh.Clear();
			loMesh.Clear();
			grassMesh.Clear();

			indexToCoord = null;

			if (meshRenderer.enabled)
			{
				meshRenderer.enabled=false; 
				voxeland.CallOnChunkVisibilityChanged(this,false);
			}

			ClearAmbient();
			ClearCollider();
		}

		public void ClearAmbient ()
		{
			ambientWorker.Stop();

			if (this.ambient!=null) 
				lock (this.ambient) //seems like that this should not work, but surprisingly it does
					this.ambient = null;
		}

		public void ClearObjects ()
		{
			objectsWorker.Stop();
			//TODO: clear chunk rect in pools
		}

		public void ClearCollider ()
		{
			colliderApplier.Stop();
			colliderIndexToCoord = null;
			meshCollider.sharedMesh = null;
		}*/

		//public void Clear () { Clear(true, true, true, true); } //Unity 5.2 does not allow that
		public void ClearAll () { Clear(true, true, true, true); }
		public void Clear (bool mesh=false, bool ambient=false, bool collider=false, bool objects=false)
		{
			if (mesh)
			{
				meshWorker.Stop();

				hiMesh.Clear();
				loMesh.Clear();
				grassMesh.Clear();

				indexToCoord = null;

				if (meshRenderer.enabled)
				{
					meshRenderer.enabled=false; 
					voxeland.CallOnChunkVisibilityChanged(this,false);
				}
			}

			if (mesh || ambient)
			{
				ambientWorker.Stop();

				if (this.ambient!=null) 
					lock (this.ambient) //seems like that this should not work, but surprisingly it does
						this.ambient = null;
			}

			if (mesh || collider)
			{
				colliderApplier.Stop();
				colliderIndexToCoord = null;
				meshCollider.sharedMesh = null;
			}

			if (objects)
			{
				objectsWorker.Stop();
				//TODO: clear chunk rect in pools
			}
		}


		#region Mesh
			

			public void CalculateMesh ()	
			{
				#if WDEBUG
				if (!ThreadWorker.multithreading) Profiler.BeginSample("Calculate Mesh");
				#endif

				//reading data
				if (meshWorker.stop) return;
				int margin = voxeland.meshMargin;

				//top and bottom points
				int topPoint; int bottomPoint;
				voxeland.data.GetTopBottomPoints(rect.Expanded(margin), out topPoint, out bottomPoint, ignoreEmptyColumns:true);

				//empty mesh check
				if (topPoint == 0) 
				{
					hiWrapper = null; loWrapper = null; grassWrapper = null;
					indexToCoord = null;
					return; //exit to apply without stopping
				}

				//creating and filling matrix
				Matrix3<byte> matrix = new Matrix3<byte>(rect.offset.x-margin, bottomPoint-1, rect.offset.z-margin, rect.size.x+margin*2, topPoint-bottomPoint+2, rect.size.z+margin*2);
				voxeland.data.FillMatrix(matrix); 

				//calculating verts
				if (meshWorker.stop) return;
				ChunkMesh.Face[] faces; Vector3[] verts;
				ChunkMesh.CalculateFaces(out faces, out verts, matrix, margin:margin);
			
				if (faces==null || verts==null || faces.Length==0) {  hiWrapper=null; loWrapper=null; return; } 
		
				ChunkMesh.RelaxMesh(faces, verts, iterations:voxeland.relaxIterations, strength:voxeland.relaxStrength);
		
				hiWrapper = ChunkMesh.CalculateMesh(faces, verts, hipoly:true);
				loWrapper = ChunkMesh.CalculateMesh(faces, verts, hipoly:false);
		
				//normals
				if (meshWorker.stop) return;
				hiWrapper.normals = ChunkMesh.CalculateNormals(faces,verts, smoothIterations:voxeland.normalsSmooth);
				loWrapper.normals = hiWrapper.normals.Truncated(loWrapper.verts.Length);

				//types
				if (meshWorker.stop) return;

				int maxChannelsCount;
				switch (voxeland.channelEncoding)
				{
					case Voxeland.ChannelEncoding.MegaSplat: maxChannelsCount = 256; break;
					case Voxeland.ChannelEncoding.RTP: maxChannelsCount = 4; break;
					default: maxChannelsCount = 24; break;
				}
				maxChannelsCount = Mathf.Min(maxChannelsCount, voxeland.landTypes.array.Length);


				if (voxeland.channelEncoding == Voxeland.ChannelEncoding.Voxeland)
				{
					float[] chWeights = new float[verts.Length];
					hiWrapper.tangents = new Vector4[verts.Length];
					for (int ch=0; ch<maxChannelsCount; ch++)
					{
						if (!ChunkMesh.IsChannelUsed(faces, ch)) continue;
						ChunkMesh.FillChWeights(faces, verts, ch, ref chWeights);
						ChunkMesh.EncodeChWeights(chWeights, ch, ref hiWrapper.tangents);
					}
					loWrapper.tangents = hiWrapper.tangents.Truncated(loWrapper.verts.Length);
				}

				else if (voxeland.channelEncoding == Voxeland.ChannelEncoding.MegaSplat)
				{
					float[] chWeights = new float[verts.Length];
					hiWrapper.colors = new Color[verts.Length];
					Vector4[] uvs2 = new Vector4[verts.Length];

					byte[] topTypes = new byte[verts.Length];
					byte[] secTypes = new byte[verts.Length];
					float[] topBlends = new float[verts.Length]; //blend value of the top type
					float[] secBlends = new float[verts.Length];

					ChunkMesh.EncodeMegaSplatFilters(faces, ref hiWrapper.colors);
					for (int ch=0; ch<maxChannelsCount; ch++)
					{
						if (!ChunkMesh.IsChannelUsed(faces, ch)) continue;
						ChunkMesh.FillChWeights(faces, verts, ch, ref chWeights);
						ChunkMesh.FillMegaSplatChTopTypes(chWeights, ch,  topTypes, secTypes, topBlends, secBlends);
					}

					ChunkMesh.EncodeMegaSplatChWeights(topTypes, secTypes, topBlends, secBlends, ref hiWrapper.colors, ref uvs2);

					hiWrapper.uvs4 = new List<Vector4>[4];
					hiWrapper.uvs4[3] = new List<Vector4>(); //3rd channel
					hiWrapper.uvs4[3].AddRange(uvs2);

					hiWrapper.uv = new Vector2[hiWrapper.verts.Length];
					for (int i=0; i<hiWrapper.verts.Length; i++) hiWrapper.uv[i] = new Vector2(-hiWrapper.verts[i].x, hiWrapper.verts[i].z);
					
					hiWrapper.tangents = new Vector4[hiWrapper.verts.Length];
					for (int i=0; i<hiWrapper.verts.Length; i++) hiWrapper.tangents[i] = new Vector4(0,1,0,1);
				}

				else if (voxeland.channelEncoding == Voxeland.ChannelEncoding.RTP)
				{
					float[] chWeights = new float[verts.Length];
					hiWrapper.colors = new Color[verts.Length];

					for (int ch=0; ch<maxChannelsCount; ch++)
					{
						if (!ChunkMesh.IsChannelUsed(faces, ch)) continue;
						ChunkMesh.FillChWeights(faces, verts, ch, ref chWeights);
						ChunkMesh.EncodeColorChWeights(chWeights, ch, ref hiWrapper.colors);
					}
					loWrapper.colors = hiWrapper.colors.Truncated(loWrapper.verts.Length);
				}

				//index to coordinate array
				if (meshWorker.stop) return;
				indexToCoord = ChunkMesh.CalculateIndexToCoord(faces);

				//calculating grass
				if (meshWorker.stop) return;
				Matrix2<byte> grassMatrix = new Matrix2<byte>(rect);
				voxeland.data.FillGrass(grassMatrix);
				
				Matrix2<ushort> topLevels = new Matrix2<ushort>(rect);
				voxeland.data.FillHeightmap(topLevels);

				//offset lo border verts to prevent seams
				ChunkMesh.OffsetBorderVerts(loWrapper.verts, loWrapper.normals, rect.size.x, -0.15f);

				if (meshWorker.stop) return;
				grassWrapper = ChunkMesh.CalculateGrassMesh(grassMatrix, topLevels, loWrapper.verts, loWrapper.normals, loWrapper.tris, indexToCoord, voxeland.grassTypes);
			
				#if WDEBUG
				if (!ThreadWorker.multithreading) Profiler.EndSample();
				#endif

			}

			public void ApplyMesh ()
			{
				#if WDEBUG
				Profiler.BeginSample("Apply Mesh");
				#endif

				if (meshWorker.stop) return;

				//resetting collider if it has empty mesh before applying loMesh - otherwise it will not be refreshed (seems to be Unity bug)
				if (meshCollider.sharedMesh!=null && meshCollider.sharedMesh.vertexCount==0 && loWrapper!=null && loWrapper.verts.Length!=0)
					meshCollider.sharedMesh = null;
			
				//apply meshes
				if (loWrapper==null || hiWrapper==null) 
				{
					hiMesh.Clear(); loMesh.Clear(); grassMesh.Clear();
				}
				else
				{
					//TODO: create new meshes instead re-using
					hiWrapper.ApplyTo(hiMesh);
					loWrapper.ApplyTo(loMesh);
				}

				//apply collider (in thread) 
				if (colliderRequired) colliderApplier.Start();
				else Clear(collider:true);

				//apply grass
				if (grassWrapper == null || grassWrapper.verts.Length == 0)
				{
					if (grassMesh!=null) grassMesh.Clear();
				}
				else
				{
					if (grassMesh == null) grassMesh = new Mesh();
					//grassWrapper.uv3 = new Vector2[grassWrapper.verts.Length];
					//for (int c=0; c<grassWrapper.uv3.Length; c++) grassWrapper.uv3[c] = new Vector2(1,1);
					grassWrapper.ApplyTo(grassMesh);
					grassFilter.sharedMesh = grassMesh; 
				}

				//renaming meshes to provide highlight change
				int meshVer = 0;
				if (hiMesh.name.Contains("VoxelandTerrainHi")) System.Int32.TryParse(hiMesh.name.Replace("VoxelandTerrainHi",""), out meshVer);
				if (meshVer > 100000000) meshVer = 0;

				hiMesh.name = "VoxelandTerrainHi" + (meshVer+1);
				loMesh.name = "VoxelandTerrainLo" + (meshVer+1);

				//purging wrappers
				loWrapper = null;
				hiWrapper = null;
				grassWrapper = null;

				//making ambient to apply this frame to prevent flickering
				if (!ambientWorker.calculated) Debug.LogError("Ambient is not calculated"); //this should not happen
				ambientWorker.FinalizeNow(); //forcing ambient to apply this frame (without condition!)

				/* Disabling seams flashes. Do not forget to use new meshes and disable mesh assigning in lod switch
					foreach (Chunk chunk in voxeland.chunks.All())
					{
						if (chunk.meshFilter.sharedMesh == null)
							chunk.meshFilter.sharedMesh = chunk.loMesh;

						else if (chunk.meshRenderer.enabled && chunk.meshFilter.sharedMesh != chunk.hiMesh && chunk.meshFilter.sharedMesh != chunk.loMesh) //if nmew mesh not assigned
						{
							bool allReady = true;
							foreach(Coord neigCoord in coord.Neightbours())
							{
								if (voxeland.chunks[neigCoord] == null || voxeland.chunks[neigCoord].meshWorker == null) continue;
								if (!voxeland.chunks[neigCoord].meshWorker.ready) allReady = false;
							}

							if (allReady)
							{
								if (chunk.meshFilter.sharedMesh.name.Contains("TerrainHi"))
									chunk.meshFilter.sharedMesh = chunk.hiMesh;
								else chunk.meshFilter.sharedMesh = chunk.loMesh;
							}
						}
					}
				 }*/

				//enabling renderer on apply
				if (meshRequired && !meshRenderer.enabled) { meshRenderer.enabled = true; voxeland.CallOnChunkVisibilityChanged(this,true); }

				#if WDEBUG
				Profiler.EndSample();
				#endif
			}
		#endregion

		#region Ambient
			public void CalculateAmbient () 
			{
				#if WDEBUG
				if (!ThreadWorker.multithreading) Profiler.BeginSample("Calculate Ambient");
				#endif
				
				//reading data
				if (ambientWorker.stop || !voxeland.useAmbient || voxeland.channelEncoding!=Voxeland.ChannelEncoding.Voxeland) {ambient=null; return;}
				int topPoint; int bottomPoint;
				int margin = voxeland.ambientMargin;
				voxeland.data.GetTopBottomPoints(rect.Expanded(margin), out topPoint, out bottomPoint, ignoreEmptyColumns:true);
				
				//empty mesh check
				if (topPoint==0) {ambient=null; return;}
				
				//create and fill matrix
				Matrix3<byte> matrix = new Matrix3<byte>(rect.offset.x-margin, bottomPoint-1, rect.offset.z-margin, rect.size.x+margin*2, topPoint-bottomPoint+2, rect.size.z+margin*2);
				voxeland.data.FillMatrix(matrix); 

				if (ambientWorker.stop) {ambient=null; return;}
				if (ambient == null || ambient.cube.size !=matrix.cube.size)
					ambient = new Matrix3<byte>(matrix.cube);

				lock (ambient)
				{
					ambient.cube.offset = matrix.cube.offset;
					voxeland.data.FillMatrix(ambient); 

					Matrix heightmap = new Matrix(ambient.cube.rect);
					voxeland.data.FillHeightmap(heightmap);

					if (ambientWorker.stop) return;
					ChunkMesh.HeightmapAmbient(ref ambient, heightmap);

					if (ambientWorker.stop) return;
					ChunkMesh.SpreadAmbient(ref ambient, voxeland.ambientFade);
					
					if (ambientWorker.stop) return;
					ChunkMesh.BlurAmbient(ref ambient);
					
					if (ambientWorker.stop) return;
					ChunkMesh.EqualizeBordersAmbient(ref ambient, margin:margin);
				}

				#if WDEBUG
				if (!ThreadWorker.multithreading) Profiler.EndSample();
				#endif
			}


			public void ApplyAmbient ()	
			{
				if (ambientWorker.stop) return;

				//empty mesh check
				if (ambient==null || hiMesh.vertexCount==0)  return; 
			
				#if WDEBUG
				Profiler.BeginSample("Apply Ambient");
				#endif

				//hi mesh
				int[] hiTris = hiMesh.triangles;
				Vector2[] hiAmbient = ChunkMesh.SetAmbient(ambient, hiTris, indexToCoord, hiMesh.vertexCount);
				hiMesh.uv4 = hiAmbient;

				//lo mesh
				Vector2[] loAmbient = new Vector2[loMesh.vertexCount];
				for (int i=0; i<loAmbient.Length; i++) loAmbient[i] = hiAmbient[i];
				loMesh.uv4 = loAmbient;

				//grass
				if (grassMesh.vertexCount != 0)
				{
					Vector2[] grassAmbient = ChunkMesh.SetGrassAmbient(ambient, grassMesh.vertices, grassMesh.triangles, grassMesh.uv4, transform.localPosition);
					grassMesh.uv4 = grassAmbient;
				}

				#if WDEBUG
				Profiler.EndSample();
				#endif
			}
		#endregion

		#region Objects

			public void ApplyObjects ()
			{
				#region Calculate 

					Dictionary<Transform, List<ObjectPool.Transition>> transitions = new Dictionary<Transform, List<ObjectPool.Transition>>();

					Noise noise = new Noise(12345, permutationCount:64); //random to floor floats 

					foreach (TupleSet<CoordDir,short> coordType in voxeland.data.ObjectsWithinRect(rect))
					{
						//position
						Vector3 objPos = new Vector3(coordType.item1.x+0.5f, coordType.item1.y+0.5f, coordType.item1.z+0.5f);
					//	objPos = voxeland.objectsPool.transformation.MultiplyPoint3x4(objPos);

						//rotation
						//TODO: check neig blocks for wall direction

						//finding proper prefab
						Transform prefab = null;
						if (coordType.item2 < voxeland.objectsTypes.array.Length)
						{
							ObjectType objType = voxeland.objectsTypes.array[coordType.item2];
							
							if (objType.prefabs.Length == 1) 
								prefab = objType.prefabs[0];

							else if (objType.prefabs.Length > 1) 
							{
								float rnd = noise.Random(coordType.item1.x, coordType.item1.y, coordType.item1.z);
								int num = (int)(rnd * objType.prefabs.Length);
								prefab = objType.prefabs[num];
							}
						}
						if (prefab == null) continue;

						//adding to dictionary
						if (!transitions.ContainsKey(prefab)) transitions.Add(prefab, new List<ObjectPool.Transition>());
						transitions[prefab].Add(new ObjectPool.Transition( new Vector3(
							objPos.x*voxeland.transform.localScale.x + voxeland.transform.position.x,
							objPos.y*voxeland.transform.localScale.y + voxeland.transform.position.y,
							objPos.z*voxeland.transform.localScale.z + voxeland.transform.position.z)));
					}

				#endregion

				#region Apply

					Rect translatedRect = new Rect (
						rect.offset.x*voxeland.transform.localScale.x + voxeland.transform.position.x,
						rect.offset.z*voxeland.transform.localScale.z + voxeland.transform.position.z,
						rect.size.x*voxeland.transform.localScale.x,
						rect.size.z*voxeland.transform.localScale.z );

					//adding
					foreach (KeyValuePair<Transform,List<ObjectPool.Transition>> kvp in transitions)
					{
						Transform prefab = kvp.Key;
						List<ObjectPool.Transition> draftList = kvp.Value;

						voxeland.objectsPool.RepositionNow(prefab, translatedRect, draftList, parent: transform);
					}

					//clear all of non-included pools from rect
					voxeland.objectsPool.ClearAllRectBut(translatedRect, transitions);
			
					//remove empty pools
					voxeland.objectsPool.RemoveEmptyPools();

				#endregion
			}

		#endregion
	}
}
