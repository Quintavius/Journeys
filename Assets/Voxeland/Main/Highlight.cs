using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Voxeland5;

namespace Voxeland5
{

	[ExecuteInEditMode]
	public class Highlight : MonoBehaviour 
	{
		

		public MeshRenderer meshRenderer;
		public MeshFilter meshFilter;
		public Material material;

		public List<Vector3> verts = new List<Vector3>();
		public List<int> tris = new List<int>();
		public List<Vector2> opacities = new List<Vector2>();

		public bool empty { get{ return verts.Count == 0; }}

		[System.NonSerialized] DictTuple<Mesh, string,Vector3[],int[]> recentMeshes = new DictTuple<Mesh, string,Vector3[],int[]>();

		public void Clear ()
		{
			verts.Clear();
			opacities.Clear();
			tris.Clear();
			meshFilter.sharedMesh.Clear();
			meshFilter.sharedMesh.bounds = new Bounds( Vector3.zero, new Vector3(float.MaxValue, float.MaxValue, float.MaxValue) ); //always visible
		}

		public void GetMeshArrays (Mesh mesh, out Vector3[] meshVerts, out int[] meshTris)
		{
			//finding proper recent mesh data
			meshVerts = null; meshTris = null;
			if (recentMeshes.ContainsKey(mesh) && recentMeshes.GetVal1(mesh)==mesh.name) //if contains mesh and it's name has not changed (i.e. mesh itself has not changed)
			{
				meshVerts = recentMeshes.GetVal2(mesh);
				meshTris = recentMeshes.GetVal3(mesh);
			}

			//retrieving and saving current mesh data
			else
			{
				meshVerts = mesh.vertices;
				meshTris = mesh.triangles;
				recentMeshes.CheckAdd(mesh, mesh.name, meshVerts, meshTris, overwrite:true);
			}

			//flushing recent meshes if the number of meshes is too big (ansd adding current one)
			if (recentMeshes.Count > 20)
			{
				recentMeshes.Clear();
				recentMeshes.Add(mesh, mesh.name, meshVerts, meshTris);
			}
		}

		public void AddVoxelandFace (CoordDir coord, Chunk chunk, float opacity=1)
		{
			//getting chunk
			if (chunk == null) return;
			Mesh chunkMesh = chunk.hiMesh;
			if (chunkMesh==null || chunkMesh.vertexCount==0) return; //empty mesh check

			//finding index
			int index = chunk.GetTriIndexByCoord(coord);

			//adding 4 hi-faces to highlight
			if (index >= 0)
				AddVoxelandFace(chunkMesh, index, chunk.transform, opacity);
		}

		public void AddVoxelandFace (Mesh mesh, int index, Transform assignedTfm=null, float opacity=1)
		{
			//get mesh arrays
			Vector3[] meshVerts; int[] meshTris;
			GetMeshArrays(mesh, out meshVerts, out meshTris);

			//adding face verts
			int initialVertCount = verts.Count;
			int faceNum = index*8*3;
			if (assignedTfm!=null)
			{
				Matrix4x4 chunkMatrix = Matrix4x4.TRS(assignedTfm.localPosition, assignedTfm.localRotation, assignedTfm.localScale); //note that matrix is local chunk transform
				for (int v=0; v<voxelandFaceVertNums.Length; v++)
				{
					verts.Add( chunkMatrix.MultiplyPoint3x4( meshVerts[meshTris[faceNum + voxelandFaceVertNums[v]]] )); 
					opacities.Add( new Vector2(opacity,0) );
				}
			}
			else
				for (int v=0; v<voxelandFaceVertNums.Length; v++)
				{
					verts.Add( meshVerts[meshTris[faceNum + voxelandFaceVertNums[v]]] );
					opacities.Add( new Vector2(opacity,0) );
				}

			
			//adding tris
			for (int f=0; f<voxelandFaceTris.Length; f++)
				tris.Add(initialVertCount + voxelandFaceTris[f]); 

			//if (index >= 0)
			//	for(int i=0; i<8; i++) AddFace(mesh, index*8+i, assignedTfm);
		}

		public void AddFace (Mesh mesh, int index, Transform assignedTfm=null) //reserved for constructor
		{
			if (mesh == null) return;

			//get mesh arrays
			Vector3[] meshVerts; int[] meshTris;
			GetMeshArrays(mesh, out meshVerts, out meshTris);

			//adding face
			for (int i=0; i<3; i++) 
			{
				Vector3 vertPos = meshVerts[meshTris[index*3+i]];
				Debug.Log(vertPos);
				if (assignedTfm != null) vertPos = assignedTfm.TransformPoint(vertPos);
				verts.Add(vertPos);
				opacities.Add( new Vector2(1,0) );
			}	

			for (int i=0; i<3; i++) tris.Add(tris.Count);
		}

		public void AddCube (Vector3 center, Vector3 size) 
		{ 
			int oldVertsCount = verts.Count;
			for (int v=0; v<cubeVerts.Length; v++)
			{
				verts.Add(	new Vector3(				//operator * could not be applied to Vector3 and Vector3
					cubeVerts[v].x*size.x/2 + center.x, 
					cubeVerts[v].y*size.y/2 + center.y, 
					cubeVerts[v].z*size.z/2 + center.z ) );
				opacities.Add( new Vector2(1,0) );
			}
			for (int t=0; t<cubeTris.Length; t++)
			{
				tris.Add(oldVertsCount+cubeTris[t]);
			}
		}

		public void AddSphere (Vector3 center, float radius) 
		{ 
			int oldVertsCount = verts.Count;
			for (int v=0; v<sphereVerts.Length; v++)
			{
				verts.Add(	new Vector3(				//operator * could not be applied to Vector3 and Vector3
					sphereVerts[v].x*radius + center.x, 
					sphereVerts[v].y*radius + center.y, 
					sphereVerts[v].z*radius + center.z ) );
				opacities.Add( new Vector2(1,0) );
			}
			for (int t=0; t<sphereTris.Length; t++)
			{
				tris.Add(oldVertsCount+sphereTris[t]);
			}
		}


		public void OnWillRenderObject ()
		{
			if (verts.Count == 0) return; //leaving mesh untouched if no new verts added (mesh is cleared via "Clear") 

			meshFilter.sharedMesh.Clear();

			meshRenderer.sharedMaterial = material;

			meshFilter.sharedMesh.SetVertices(verts);
			meshFilter.sharedMesh.SetUVs(1, opacities);
			meshFilter.sharedMesh.SetTriangles(tris,0);
			meshFilter.sharedMesh.bounds = new Bounds( Vector3.zero, new Vector3(float.MaxValue, float.MaxValue, float.MaxValue) ); //always visible

			//UnityEngine.Graphics.DrawMesh(meshFilter.sharedMesh, Matrix4x4.identity, material, 0, camera:null, submeshIndex:0, properties:null, castShadows:false, receiveShadows:false, useLightProbes:false);
			//meshFilter.sharedMesh.Clear();

			//clearing verts and tris after draw
			verts.Clear();
			opacities.Clear();
			tris.Clear();
		}


		public static Highlight Create (Voxeland voxeland=null)  
		{
			GameObject go = new GameObject();
			go.name = "Highlight";
			if (voxeland!=null) go.transform.parent = voxeland.transform;
			Highlight highlight = go.AddComponent<Highlight>();
			
			Shader highlightShader = Shader.Find("Voxeland/Highlight");
			if (highlightShader != null) highlight.material = new Material(highlightShader);

			highlight.meshFilter = go.AddComponent<MeshFilter>();
			highlight.meshRenderer = go.AddComponent<MeshRenderer>();

			highlight.meshFilter.sharedMesh = new Mesh(); 
			highlight.meshFilter.sharedMesh.MarkDynamic();
			highlight.meshFilter.sharedMesh.bounds = new Bounds( Vector3.zero, new Vector3(float.MaxValue, float.MaxValue, float.MaxValue) ); //always visible

			if (voxeland.guiHideWireframe) go.transform.ToggleDisplayWireframe(false);

			return highlight;
		}

		//private readonly Vector2[] planeUvs = {new Vector2(0,0), new Vector2(1,0), new Vector2(1,1), new Vector2(0,1)};
		//private readonly int[] planeTris = {0,1,2,2,3,0};
		
		//private readonly Vector2[] polyUvs = {new Vector2(0.25f,0), new Vector2(0.125f,0), new Vector2(0,0), new Vector2(0,0.125f), new Vector2(0,0.25f), new Vector2(0.125f,0.25f)};
		//private readonly int[] polyTris = {0,1,2,3,4,5};
	
		//private readonly Vector2[] faceUvs = {new Vector2(0.25f,0), new Vector2(0.125f,0), new Vector2(0,0), new Vector2(0,0.125f), new Vector2(0,0.25f),
		//					new Vector2(0.125f,0.25f), new Vector2(0.25f,0.25f), new Vector2(0.25f,0.125f), new Vector2(0.125f,0.125f)};
		//private readonly int[] faceTris = {7,0,1,1,8,7,8,1,2,2,3,8,5,8,3,3,4,5,6,7,8,8,5,6};
		
		
		private readonly Vector3[] cubeVerts = {new Vector3(-1.0f,-1.0f,-1.0f), new Vector3(1.0f,-1.0f,-1.0f), new Vector3(-1.0f,-1.0f,1.0f), new Vector3(1.0f,-1.0f,1.0f), new Vector3(-1.0f,1.0f,-1.0f), new Vector3(1.0f,1.0f,-1.0f), new Vector3(-1.0f,1.0f,1.0f), new Vector3(1.0f,1.0f,1.0f), new Vector3(-1.0f,-1.0f,-1.0f), new Vector3(-1.0f,-1.0f,-1.0f), new Vector3(1.0f,-1.0f,-1.0f), new Vector3(1.0f,-1.0f,-1.0f), new Vector3(-1.0f,-1.0f,1.0f), new Vector3(-1.0f,-1.0f,1.0f), new Vector3(1.0f,-1.0f,1.0f), new Vector3(1.0f,-1.0f,1.0f), new Vector3(-1.0f,1.0f,-1.0f), new Vector3(-1.0f,1.0f,-1.0f), new Vector3(1.0f,1.0f,-1.0f), 
		                       new Vector3(1.0f,1.0f,-1.0f), new Vector3(-1.0f,1.0f,1.0f), new Vector3(-1.0f,1.0f,1.0f), new Vector3(1.0f,1.0f,1.0f), new Vector3(1.0f,1.0f,1.0f)};
		private readonly int[] cubeTris = {0,1,3,3,2,0,4,6,7,7,5,4,8,16,18,18,10,8,11,19,22,22,14,11,15,23,20,20,12,15,13,21,17,17,9,13};
		
		
		private readonly Vector3[] sphereVerts = {new Vector3(0.0f,1.0f,0.0f), new Vector3(0.894427f,0.447214f,0.0f), new Vector3(0.276393f,0.447214f,0.850651f), new Vector3(-0.723607f,0.447214f,0.525731f), new Vector3(-0.723607f,0.447214f,-0.525731f), new Vector3(0.276393f,0.447214f,-0.850651f), new Vector3(0.723607f,-0.447214f,0.525731f), new Vector3(-0.276393f,-0.447214f,0.850651f), new Vector3(-0.894427f,-0.447214f,0.0f), new Vector3(-0.276393f,-0.447214f,-0.850651f), new Vector3(0.723607f,-0.447214f,-0.525731f), new Vector3(0.0f,-1.0f,0.0f), new Vector3(0.360729f,0.932671f,0.0f), 
		                         new Vector3(0.672883f,0.739749f,0.0f), new Vector3(0.111471f,0.932671f,0.343074f), new Vector3(0.207932f,0.739749f,0.63995f), new Vector3(-0.291836f,0.932671f,0.212031f), new Vector3(-0.544374f,0.739749f,0.395511f), new Vector3(-0.291836f,0.932671f,-0.212031f), new Vector3(-0.544374f,0.739749f,-0.395511f), new Vector3(0.111471f,0.932671f,-0.343074f), new Vector3(0.207932f,0.739749f,-0.63995f), new Vector3(0.784354f,0.516806f,0.343074f), new Vector3(0.568661f,0.516806f,0.63995f), new Vector3(-0.0839038f,0.516806f,0.851981f), new Vector3(-0.432902f,0.516806f,0.738584f), 
		                         new Vector3(-0.83621f,0.516806f,0.183479f), new Vector3(-0.83621f,0.516806f,-0.183479f), new Vector3(-0.432902f,0.516806f,-0.738584f), new Vector3(-0.0839036f,0.516806f,-0.851981f), new Vector3(0.568661f,0.516806f,-0.63995f), new Vector3(0.784354f,0.516806f,-0.343074f), new Vector3(0.964719f,0.156077f,0.212031f), new Vector3(0.905103f,-0.156077f,0.395511f), new Vector3(0.0964608f,0.156077f,0.983023f), new Vector3(-0.0964609f,-0.156077f,0.983024f), new Vector3(-0.905103f,0.156077f,0.395511f), new Vector3(-0.964719f,-0.156077f,0.212031f), new Vector3(-0.655845f,0.156077f,-0.738585f), 
		                         new Vector3(-0.499768f,-0.156077f,-0.851981f), new Vector3(0.499768f,0.156077f,-0.851981f), new Vector3(0.655845f,-0.156077f,-0.738584f), new Vector3(0.964719f,0.156077f,-0.212031f), new Vector3(0.905103f,-0.156077f,-0.395511f), new Vector3(0.499768f,0.156077f,0.851981f), new Vector3(0.655845f,-0.156077f,0.738584f), new Vector3(-0.655845f,0.156077f,0.738584f), new Vector3(-0.499768f,-0.156077f,0.851981f), new Vector3(-0.905103f,0.156077f,-0.395511f), new Vector3(-0.964719f,-0.156077f,-0.212031f), new Vector3(0.0964611f,0.156077f,-0.983024f), new Vector3(-0.0964605f,-0.156077f,-0.983023f), 
		                         new Vector3(0.432902f,-0.516806f,0.738584f), new Vector3(0.0839037f,-0.516806f,0.851981f), new Vector3(-0.568661f,-0.516806f,0.63995f), new Vector3(-0.784354f,-0.516806f,0.343074f), new Vector3(-0.784354f,-0.516806f,-0.343074f), new Vector3(-0.568661f,-0.516806f,-0.63995f), new Vector3(0.083904f,-0.516806f,-0.851981f), new Vector3(0.432902f,-0.516806f,-0.738584f), new Vector3(0.83621f,-0.516806f,-0.183479f), new Vector3(0.83621f,-0.516806f,0.183479f), new Vector3(0.291836f,-0.932671f,0.212031f), new Vector3(0.544374f,-0.739749f,0.395511f), new Vector3(-0.111471f,-0.932671f,0.343074f), 
		                         new Vector3(-0.207932f,-0.739749f,0.63995f), new Vector3(-0.360729f,-0.932671f,0.0f), new Vector3(-0.672883f,-0.739749f,0.0f), new Vector3(-0.111471f,-0.932671f,-0.343074f), new Vector3(-0.207932f,-0.739749f,-0.63995f), new Vector3(0.291836f,-0.932671f,-0.212031f), new Vector3(0.544374f,-0.739749f,-0.395511f), new Vector3(0.479506f,0.805422f,0.348381f), new Vector3(-0.183155f,0.805422f,0.563693f), new Vector3(-0.592702f,0.805422f,0.0f), new Vector3(-0.183155f,0.805422f,-0.563693f), new Vector3(0.479506f,0.805422f,-0.348381f), new Vector3(0.985456f,-0.169933f,0.0f), 
		                         new Vector3(0.304522f,-0.169933f,0.937224f), new Vector3(-0.79725f,-0.169933f,0.579236f), new Vector3(-0.79725f,-0.169933f,-0.579236f), new Vector3(0.304523f,-0.169933f,-0.937224f), new Vector3(0.79725f,0.169933f,0.579236f), new Vector3(-0.304523f,0.169933f,0.937224f), new Vector3(-0.985456f,0.169933f,0.0f), new Vector3(-0.304522f,0.169933f,-0.937224f), new Vector3(0.79725f,0.169933f,-0.579236f), new Vector3(0.183155f,-0.805422f,0.563693f), new Vector3(-0.479506f,-0.805422f,0.348381f), new Vector3(-0.479506f,-0.805422f,-0.348381f), new Vector3(0.183155f,-0.805422f,-0.563693f), new Vector3(0.592702f,-0.805422f,0.0f)};
		private readonly int[] sphereTris = {14,12,0,72,13,12,14,72,12,15,72,14,22,1,13,72,22,13,23,22,72,15,23,72,2,23,15,16,14,0,73,15,14,16,73,14,17,73,16,24,2,15,73,24,15,25,24,73,17,25,73,3,25,17,18,16,0,74,17,16,18,74,16,19,74,18,26,3,17,74,26,17,27,26,74,19,27,74,4,27,19,20,18,0,75,19,18,20,75,18,21,75,20,28,4,19,75,28,19,29,28,75,21,29,75,5,29,21,12,20,0,76,21,20,12,76,20,13,76,12,30,5,21,76,30,21,31,30,76,13,31,76,1,31,13,32,42,1,77,43,42,32,77,42,33,77,32,60,10,43,77,60,43,61,60,77,33,61,77,6,61,33,34,44,2,78,45,44,34,78,44,35,78,
		                    34,52,6,45,78,52,45,53,52,78,35,53,78,7,53,35,36,46,3,79,47,46,36,79,46,37,79,36,54,7,47,79,54,47,55,54,79,37,55,79,8,55,37,38,48,4,80,49,48,38,80,48,39,80,38,56,8,49,80,56,49,57,56,80,39,57,80,9,57,39,40,50,5,81,51,50,40,81,50,41,81,40,58,9,51,81,58,51,59,58,81,41,59,81,10,59,41,33,45,6,82,44,45,33,82,45,32,82,33,23,2,44,82,23,44,22,23,82,32,22,82,1,22,32,35,47,7,83,46,47,35,83,47,34,83,35,25,3,46,83,25,46,24,25,83,34,24,83,2,24,34,37,49,8,84,48,49,37,84,49,36,84,37,27,4,48,84,27,48,26,27,84,36,26,84,3,26,36,39,51,9,85,50,
		                    51,39,85,51,38,85,39,29,5,50,85,29,50,28,29,85,38,28,85,4,28,38,41,43,10,86,42,43,41,86,43,40,86,41,31,1,42,86,31,42,30,31,86,40,30,86,5,30,40,62,64,11,87,65,64,62,87,64,63,87,62,53,7,65,87,53,65,52,53,87,63,52,87,6,52,63,64,66,11,88,67,66,64,88,66,65,88,64,55,8,67,88,55,67,54,55,88,65,54,88,7,54,65,66,68,11,89,69,68,66,89,68,67,89,66,57,9,69,89,57,69,56,57,89,67,56,89,8,56,67,68,70,11,90,71,70,68,90,70,69,90,68,59,10,71,90,59,71,58,59,90,69,58,90,9,58,69,70,62,11,91,63,62,70,91,62,71,91,70,61,6,63,91,61,63,60,61,91,71,60,91,10,60,71};

		private readonly int[] voxelandFaceTris = { 0,1,2, 0,2,3, 0,3,4, 0,4,5, 0,5,6, 0,6,7, 0,7,8, 0,8,1 };
		private readonly int[] voxelandFaceVertNums = { 0,1,2,5,8,11,14,17,20 };		

	}
}
