using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_5_5_OR_NEWER
using UnityEngine.Profiling;
#endif

using Voxeland5;

namespace Voxeland5
{

	public static class ChunkMesh  
	{
		#region Readonly arrays

			static readonly int[] dirToPosX = {0, 0, 1,-1, 0, 0};
			static readonly int[] dirToPosY = {1,-1, 0, 0, 0, 0};
			static readonly int[] dirToPosZ = {0, 0, 0, 0, 1,-1};

			//dir0				  dir1				   dir2					dir3
			static readonly float[] sidePosesX = 	{0.5f,1.0f,0.5f,0.0f, 0.5f,0.0f,0.5f,1.0f, 1.0f,1.0f,1.0f,1.0f, 0.0f,0.0f,0.0f,0.0f, 1.0f,0.5f,0.0f,0.5f, 1.0f,0.5f,0.0f,0.5f};
			static readonly float[] sidePosesY = 	{1.0f,1.0f,1.0f,1.0f, 0.0f,0.0f,0.0f,0.0f, 1.0f,0.5f,0.0f,0.5f, 1.0f,0.5f,0.0f,0.5f, 0.5f,1.0f,0.5f,0.0f, 0.5f,0.0f,0.5f,1.0f};
			static readonly float[] sidePosesZ = 	{1.0f,0.5f,0.0f,0.5f, 1.0f,0.5f,0.0f,0.5f, 0.5f,1.0f,0.5f,0.0f, 0.5f,0.0f,0.5f,1.0f, 1.0f,1.0f,1.0f,1.0f, 0.0f,0.0f,0.0f,0.0f};
		
			static readonly float[] cornerPosesX =	{0.0f,1.0f,1.0f,0.0f, 1.0f,0.0f,0.0f,1.0f, 1.0f,1.0f,1.0f,1.0f, 0.0f,0.0f,0.0f,0.0f, 1.0f,1.0f,0.0f,0.0f, 1.0f,1.0f,0.0f,0.0f};
			static readonly float[] cornerPosesY =	{1.0f,1.0f,1.0f,1.0f, 0.0f,0.0f,0.0f,0.0f, 1.0f,1.0f,0.0f,0.0f, 1.0f,1.0f,0.0f,0.0f, 0.0f,1.0f,1.0f,0.0f, 1.0f,0.0f,0.0f,1.0f};
			static readonly float[] cornerPosesZ =	{1.0f,1.0f,0.0f,0.0f, 1.0f,1.0f,0.0f,0.0f, 0.0f,1.0f,1.0f,0.0f, 1.0f,0.0f,0.0f,1.0f, 1.0f,1.0f,1.0f,1.0f, 0.0f,0.0f,0.0f,0.0f};
		
			static readonly float[] centerPosesX =	{0.5f,0.5f,1.0f,0.0f,0.5f,0.5f};
			static readonly float[] centerPosesY =	{1.0f,0.0f,0.5f,0.5f,0.5f,0.5f};
			static readonly float[] centerPosesZ =	{0.5f,0.5f,0.5f,0.5f,1.0f,0.0f};
		
			//dir (side (neig) )
			//								dir0							dir1							dir2							dir3							dir4
			//								side1	side2  side3 side4		side1
			//static readonly byte[] neigDir = {4,0,5, 2,0,3, 5,0,4, 3,0,2, 	4,1,5, 3,1,2, 5,1,4, 2,1,3, 	0,2,1, 4,2,5, 1,2,0, 5,2,4, 	0,3,1, 5,3,4,  1,3,0,  4,3,5, 	2,4,3, 0,4,1, 3,4,2,  1,4,0, 	2,5,3, 1,5,0,   3,5,2,  0,5,1};
			static readonly byte[] neigSide= {1,2,1, 0,3,2, 3,0,3, 0,1,2, 	3,2,3, 2,3,0, 1,0,1, 2,1,0, 	1,2,1, 0,3,2, 3,0,3, 0,1,2, 	3,2,3, 2,3,0,  1,0,1,  2,1,0, 	1,2,1, 0,3,2, 3,0,3,  0,1,2, 	3,2,3, 2,3,0,   1,0,1,  2,1,0};
			//static readonly int[] neigX =	{0,0,0, 0,1,1, 0,0,0, 0,-1,-1,	0,0,0,0,-1,-1,0,0,0, 0,1,1, 	0,0,1, 0,0,1, 0,0,1, 0,0,1, 	0,0,-1,0,0,-1, 0,0,-1, 0,0,-1, 	0,1,1, 0,0,0, 0,-1,-1,0,0,0, 	0,1,1, 0,0,0,   0,-1,-1,0,0,0};
			//static readonly int[] neigY =   {0,0,1, 0,0,1, 0,0,1, 0,0,1,	0,0,-1,0,0,-1,0,0,-1,0,0,-1,	0,1,1, 0,0,0,0,-1,-1,0,0,0, 	0,1,1, 0,0,0,  0,-1,-1,0,0,0, 	0,0,0, 0,1,1, 0,0,0,  0,-1,-1, 	0,0,0, 0,-1,-1, 0,0,0,  0,1,1};
			//static readonly int[] neigZ =   {0,1,1, 0,0,0,0,-1,-1,0,0,0,	0,1,1, 0,0,0,0,-1,-1,0,0,0, 	0,0,0, 0,1,1, 0,0,0, 0,-1,-1,	0,0,0, 0,-1,-1,0,0,0,  0,1,1,	0,0,1, 0,0,1, 0,0,1,  0,0,1, 	0,0,-1,0,0,-1,  0,0,-1, 0,0,-1};
		
			static readonly CoordDir[] neigs = new CoordDir[] {
					//this block		//planar block			//concave corner
				//dir0
				new CoordDir(0,0,0,4), new CoordDir(0, 0, 1, 0), new CoordDir(0, 1, 1, 5),	
				new CoordDir(0,0,0,2), new CoordDir(1, 0, 0, 0), new CoordDir(1, 1, 0, 3), 
				new CoordDir(0,0,0,5), new CoordDir(0, 0,-1, 0), new CoordDir(0, 1,-1, 4), 
				new CoordDir(0,0,0,3), new CoordDir(-1,0, 0, 0), new CoordDir(-1,1, 0, 2), 
				//dir1
				new CoordDir(0,0,0,4), new CoordDir(0, 0, 1, 1), new CoordDir(0,-1, 1, 5), 
				new CoordDir(0,0,0,3), new CoordDir(-1,0, 0, 1), new CoordDir(-1,-1,0, 2), 
				new CoordDir(0,0,0,5), new CoordDir(0, 0,-1, 1), new CoordDir(0,-1,-1, 4), 
				new CoordDir(0,0,0,2), new CoordDir(1, 0, 0, 1), new CoordDir(1,-1, 0, 3), 
				//dir2
				new CoordDir(0,0,0,0), new CoordDir(0, 1, 0, 2), new CoordDir(1, 1, 0, 1), 
				new CoordDir(0,0,0,4), new CoordDir(0, 0, 1, 2), new CoordDir(1, 0, 1, 5), 
				new CoordDir(0,0,0,1), new CoordDir(0,-1, 0, 2), new CoordDir(1,-1, 0, 0), 
				new CoordDir(0,0,0,5), new CoordDir(0, 0,-1, 2), new CoordDir(1, 0,-1, 4), 
				//dir3
				new CoordDir(0,0,0,0), new CoordDir(0, 1, 0, 3), new CoordDir(-1, 1, 0, 1), 
				new CoordDir(0,0,0,5), new CoordDir(0, 0,-1, 3), new CoordDir(-1, 0,-1, 4), 
				new CoordDir(0,0,0,1), new CoordDir(0,-1, 0, 3), new CoordDir(-1,-1, 0, 0), 
				new CoordDir(0,0,0,4), new CoordDir(0, 0, 1, 3), new CoordDir(-1, 0, 1, 5), 
				//dir4
				new CoordDir(0,0,0,2), new CoordDir(1, 0, 0, 4), new CoordDir(1, 0, 1, 3), 
				new CoordDir(0,0,0,0), new CoordDir(0, 1, 0, 4), new CoordDir(0, 1, 1, 1), 
				new CoordDir(0,0,0,3), new CoordDir(-1,0, 0, 4), new CoordDir(-1,0, 1, 2), 
				new CoordDir(0,0,0,1), new CoordDir(0,-1, 0, 4), new CoordDir(0,-1, 1, 0), 
				//dir5
				new CoordDir(0,0,0,2), new CoordDir(1, 0, 0, 5), new CoordDir(1, 0,-1, 3), 
				new CoordDir(0,0,0,1), new CoordDir(0,-1, 0, 5), new CoordDir(0,-1,-1, 0), 
				new CoordDir(0,0,0,3), new CoordDir(-1,0, 0, 5), new CoordDir(-1,0,-1, 2), 
				new CoordDir(0,0,0,0), new CoordDir(0, 1, 0, 5), new CoordDir(0, 1,-1, 1) };


		#endregion

		#region Structs
		
			/*public struct Coord
			{
				public int x;
				public int y;
				public int z;
			}
		
			public struct CoordDir
			{
				public int x;
				public int y;
				public int z;
				public byte dir;
			}

			public struct CoordDirSide
			{
				public int x;
				public int y;
				public int z;
				public byte dir;
				public byte side;
			}*/

			public struct Nodes<T> //it's better use fixed buffer, but, unfortunately, it is unsafe
			{
				public T a;
				public T b;
				public T c;
				public T d;

				public T this[int num]
				{
					get 
					{ 
						switch (num)
						{
							case -1:return d;
							case 0: return a;
							case 1: return b;
							case 2: return c;
							case 3: return d;
							case 4: return a;
							default: return a;
						}
					}
					set 
					{ 
						switch (num)
						{
							case -1:d = value; break;
							case 0: a = value; break;
							case 1: b = value; break;
							case 2: c = value; break;
							case 3: d = value; break;
							case 4: a = value; break;
						}
					}
				}
			
				public Nodes (T n0, T n1, T n2, T n3)
				{
					a=n0; b=n1; c=n2; d=n3;
				}

				public IEnumerable<T> All ()
				{
					yield return a;
					yield return b;
					yield return c;
					yield return d;
				}
			}
		
			public struct Face
			{
				public CoordDir coord;
			
				public bool visible;

				public Nodes<int> cornerNums;
				public Nodes<int> sideNums;
				public int centerNum;
			
				public Nodes<int> neigFaceNums;
				public Nodes<int> neigSides;
			
				//public Vector3 normal;
				//public Nodes<Vector3> normals;
				public float ambient;

				public byte type;
				public byte channel; //same as type, but contains texture number information (non-terrain types are skipped)
				//public IntArray blendedMaterial; //how much each of mat-spaced types affect face texture
				//public IntArray blurredMaterial;

				public Vector3 testCenter
				{get{
					Vector3 p = new Vector3(coord.x, coord.y, coord.z);
					Vector3 d = new Vector3(dirToPosX[coord.dir], dirToPosY[coord.dir], dirToPosZ[coord.dir]);
					return p+d/2;
				}}
			}

			public struct ArrayStruct
			{
				public uint val;

				static public readonly uint[] masks = new uint[] { 0xF, 0xF0, 0xF00, 0xF000, 0xF0000, 0xF00000, 0xF000000, 0xF0000000 };
				static public readonly uint[] invMasks = new uint[] { 0xFFFFFFF0, 0xFFFFFF0F, 0xFFFFF0FF, 0xFFFF0FFF, 0xFFF0FFFF, 0xFF0FFFFF, 0xF0FFFFFF, 0x0FFFFFFF };

				public uint this[int i] 
				{ 
					get { return (val & masks[i]) >> i*4; }
					set { val = (val & invMasks[i]) | (value << i*4); }
				}

				public void Add (ArrayStruct a) { for (int i=0; i<8; i++) this[i] += a[i]; }
				public void Divide (uint d) { for (int i=0; i<8; i++) this[i] /= d; }

				public void Reset () { val = 0; }
			}
		#endregion

		#region Functions

			/*public static IEnumerable<CoordDir> PossibleFaceCoords (Matrix3<byte> matrix)
			{
				int maxX = matrix.offsetX+matrix.sizeX; int maxY = matrix.offsetY+matrix.sizeY; int maxZ = matrix.offsetZ+matrix.sizeZ;
				int minX = matrix.offsetX; int minY = matrix.offsetY; int minZ = matrix.offsetZ;

				for (int y=minY; y<maxY; y++)
					for (int x=minX; x<maxX; x++)	
				{
					bool wasFilled = matrix[x,y,minZ] != 0;
					for (int z=minZ+1; z<maxZ; z++)
					{
						byte type = matrix.array[(z-matrix.offsetZ)*matrix.sizeX*matrix.sizeY + (y-matrix.offsetY)*matrix.sizeX + x - matrix.offsetX];
						bool filled = type!=0;
						if (wasFilled != filled)
						{
							if (filled) yield return new CoordDir() { x=x, y=y, z=z, dir=5 };
							else yield return new CoordDir() { x=x, y=y, z=z-1, dir=4 };
							wasFilled = filled;
						}
					}
				}

				for (int x=minX; x<maxX; x++)
					for (int z=minZ; z<maxZ; z++)	
				{
					bool wasFilled = matrix[x,minY,z] != 0;
					for (int y=minY+1; y<maxY; y++)
					{
						byte type = matrix.array[(z-matrix.offsetZ)*matrix.sizeX*matrix.sizeY + (y-matrix.offsetY)*matrix.sizeX + x - matrix.offsetX];
						bool filled = type!=0;
						if (wasFilled != filled)
						{
							if (filled) yield return new CoordDir() { x=x, y=y, z=z, dir=1 };
							else yield return new CoordDir() { x=x, y=y-1, z=z, dir=0 };
							wasFilled = filled;
						}
					}
				}

				for (int y=minY; y<maxY; y++)
					for (int z=minZ; z<maxZ; z++)	
				{
					bool wasFilled = matrix[minX,y,z] != 0;
					for (int x=minX+1; x<maxX; x++)
					{
						byte type = matrix.array[(z-matrix.offsetZ)*matrix.sizeX*matrix.sizeY + (y-matrix.offsetY)*matrix.sizeX + x - matrix.offsetX];
						bool filled = type!=0;
						if (wasFilled != filled)
						{
							if (filled) yield return new CoordDir() { x=x, y=y, z=z, dir=3 };
							else yield return new CoordDir() { x=x-1, y=y, z=z, dir=2 };
							wasFilled = filled;
						}
					}
				}
			}*/

			/*public static Vector3 SmoothCorner (Face[] faces, Vector3[] verts, int f, int c)
			{
				int div = 1; Vector3 sum = verts[ faces[f].sideNums[c] ]; //this face side on the beginning
				int nextFace = f; int nextCorner = c;
			
				while (div < 8) //could not be more than 7 welded faces
				{
					//getting next face and next corner
					int temp = faces[nextFace].neigFaceNums[nextCorner]; //storing next face num to temporary value, we'll need original to get corner
					nextCorner = faces[nextFace].neigSides[nextCorner] + 1; //it's a next corner after side, so adding 1
					nextFace = temp;
				
					if (nextFace == -1 || nextFace == f) break; //if reached boundary
				
					sum += verts[ faces[nextFace].sideNums[nextCorner] ]; //adding side vert (with same number as corner vert)
					div++;
				}
			
				return sum/div;
			}*/
		
			public static CoordDir[] NeighbourCoordinates (CoordDir coord, Matrix3<byte> matrix, int iterations, bool round=false) 
			{
				//preparing array and adding current coordinate 
				List<CoordDir> coords = new List<CoordDir>();
				List<bool> used = new List<bool>();

				coords.Add(coord);
				used.Add(false);

				//adding neigs
				for (int iteration=0; iteration<iterations; iteration++)
				{
					int coordsCount = coords.Count; //count changes during iteration, new faces added
					for (int c=0; c<coordsCount; c++)
					{
						if (used[c]) continue;

						for (int side=0; side<4; side++)
						{
							for (int neig=0; neig<3; neig++)
							{							
								int i = coords[c].dir*12 + side*3 + neig; //num in neig coords lut

								CoordDir neigCoord = CoordDir.neigsLut[i]; //finding from lut of neig coords
								neigCoord.x += coords[c].x; neigCoord.y += coords[c].y; neigCoord.z += coords[c].z;
	
								if (!matrix.cube.Contains(neigCoord)) continue;
								if (round) 
								{
									int dx = coord.x-neigCoord.x; int dy = coord.y-neigCoord.y; int dz = coord.z-neigCoord.z;
									if (dx*dx + dy*dy + dz*dz > (iterations/1.5f)*(iterations/1.5f)) continue;
								}

								CoordDir oppositeNeigCoord = neigCoord.opposite;
								//if (oppositeNeigCoord.x<matrix.offsetX || oppositeNeigCoord.x>=matrix.offsetX+matrix.sizeX || oppositeNeigCoord.y<matrix.offsetY || oppositeNeigCoord.y>=matrix.offsetY+matrix.sizeY || oppositeNeigCoord.z<matrix.offsetZ || oppositeNeigCoord.z>=matrix.offsetZ+matrix.sizeZ) continue;
								if (!matrix.cube.Contains(oppositeNeigCoord)) continue;

								byte neigType = matrix[neigCoord.x, neigCoord.y, neigCoord.z];
								byte oppositeType = matrix[oppositeNeigCoord.x, oppositeNeigCoord.y, oppositeNeigCoord.z];

								if ( (neigType!=Data.emptyByte && neigType<Data.constructorByte) && (oppositeType==Data.emptyByte || oppositeType>=Data.constructorByte) )
								//if ((neigType>=Data.minLandByte && neigType<=Data.maxLandByte) && (oppositeType<Data.minLandByte || oppositeType>Data.maxLandByte)) //if neig exists, and opposite not exists
								{
									bool foundInCoords = false;
									int coodsCount = coords.Count;
									for (int j=0; j<coodsCount; j++)
										if (coords[j].x==neigCoord.x && coords[j].y==neigCoord.y && coords[j].z==neigCoord.z && coords[j].dir==neigCoord.dir) foundInCoords = true;
									if (!foundInCoords) { coords.Add(neigCoord); used.Add(false); }
									break;
								}	
							}
						}

						used[c] = true;
					}
				}

				return coords.ToArray();
			}

			public static IEnumerable<CoordDir> SameBlockCoordinates (CoordDir coord, Matrix3<byte> matrix) //NeighbourCoordinates on the same block only
			{
				if (matrix[coord] == Data.emptyByte) yield break; 
				for (byte s=0; s<6; s++)
				{
					CoordDir current = new CoordDir(coord,s);
					if (current == coord) continue;
					CoordDir opposite = current.opposite;
					if (matrix[opposite] == Data.emptyByte) yield return current;
				}
			}

		#endregion


		#region Mesh Operations


		public static void CalculateFaces (out Face[] faces, out Vector3[] verts,  Matrix3<byte> matrix, int margin=0, Vector3 offset=new Vector3())
		{
			#if WDEBUG
			if (!ThreadWorker.multithreading) Profiler.BeginSample("Calculate Faces");
			#endif

			#region Finding face coordinates

				#if WDEBUG
				if (!ThreadWorker.multithreading) Profiler.BeginSample ("Face Coords");
				#endif

				List<CoordDir> faceCoords = new List<CoordDir>();

				CoordDir min = matrix.cube.Min;  CoordDir max = matrix.cube.Max; 

				for (int y=min.y; y<max.y; y++)
					for (int x=min.x; x<max.x; x++)	
				{
					byte wasType = matrix[x,y,min.z];
					bool wasFilled = wasType!=Data.emptyByte && wasType<Data.constructorByte; //wasType>=Data.minLandByte && wasType<=Data.maxLandByte;
					for (int z=min.z+1; z<max.z; z++)
					{
						byte type = matrix.array[(z-matrix.cube.offset.z)*matrix.cube.size.x*matrix.cube.size.y + (y-matrix.cube.offset.y)*matrix.cube.size.x + x - matrix.cube.offset.x];
						bool filled = type!=Data.emptyByte && type<Data.constructorByte; //type>=Data.minLandByte && type<=Data.maxLandByte;
						if (wasFilled != filled)
						{
							if (filled) faceCoords.Add( new CoordDir() { x=x, y=y, z=z, dir=5 } );
							else faceCoords.Add( new CoordDir() { x=x, y=y, z=z-1, dir=4 } );
							wasFilled = filled;
						}
					}
				}

				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)	
				{
					byte wasType = matrix[x,min.y,z];
					bool wasFilled = wasType!=Data.emptyByte && wasType<Data.constructorByte; //wasType>=Data.minLandByte && wasType<=Data.maxLandByte;
					for (int y=min.y+1; y<max.y; y++)
					{
						byte type = matrix.array[(z-matrix.cube.offset.z)*matrix.cube.size.x*matrix.cube.size.y + (y-matrix.cube.offset.y)*matrix.cube.size.x + x - matrix.cube.offset.x];
						bool filled = type!=Data.emptyByte && type<Data.constructorByte; //type>=Data.minLandByte && type<=Data.maxLandByte;
						if (wasFilled != filled)
						{
							if (filled) faceCoords.Add( new CoordDir() { x=x, y=y, z=z, dir=1 } );
							else faceCoords.Add( new CoordDir() { x=x, y=y-1, z=z, dir=0 } );
							wasFilled = filled;
						}
					}
				}

				for (int y=min.y; y<max.y; y++)
					for (int z=min.z; z<max.z; z++)	
				{
					byte wasType = matrix[min.x,y,z];
					bool wasFilled = wasType!=Data.emptyByte && wasType<Data.constructorByte; //wasType>=Data.minLandByte && wasType<=Data.maxLandByte;
					for (int x=min.x+1; x<max.x; x++)
					{
						byte type = matrix.array[(z-matrix.cube.offset.z)*matrix.cube.size.x*matrix.cube.size.y + (y-matrix.cube.offset.y)*matrix.cube.size.x + x - matrix.cube.offset.x];
						bool filled = type!=Data.emptyByte && type<Data.constructorByte; //type>=Data.minLandByte && type<=Data.maxLandByte;
						if (wasFilled != filled)
						{
							if (filled) faceCoords.Add( new CoordDir() { x=x, y=y, z=z, dir=3 } );
							else faceCoords.Add( new CoordDir() { x=x-1, y=y, z=z, dir=2 } );
							wasFilled = filled;
						}
					}
				}

				#if WDEBUG
				if (!ThreadWorker.multithreading) Profiler.EndSample ();
				#endif

			#endregion


			#region Creating Faces

				#if WDEBUG
				if (!ThreadWorker.multithreading) Profiler.BeginSample ("Creating faces");
				#endif

				int facesCount = faceCoords.Count;
				if (facesCount == 0) { faces=null; verts=null; return; } //guard if chunk is empty

				faces = new Face[facesCount];

				Dictionary<int, int> faceHashes = new Dictionary<int, int>();

				for (int f=0; f<facesCount; f++)
				{
					CoordDir coord = faceCoords[f];

					//surprizingly this is twice faster then creating new Face()
					faces[f].coord = coord;
					faces[f].type = matrix[coord.x,coord.y,coord.z]; //TODO: call matrix array directly
					faces[f].visible = coord.x>=matrix.cube.offset.x+margin && coord.z>=matrix.cube.offset.z+margin && coord.x<matrix.cube.offset.x+matrix.cube.size.x-margin && coord.z<matrix.cube.offset.z+matrix.cube.size.z-margin; 
					faces[f].cornerNums.a=f*4; faces[f].cornerNums.b=f*4+1; faces[f].cornerNums.c=f*4+2; faces[f].cornerNums.d=f*4+3;
					faces[f].sideNums.a=facesCount*4+f*4; faces[f].sideNums.b=facesCount*4+f*4+1; faces[f].sideNums.c=facesCount*4+f*4+2; faces[f].sideNums.d=facesCount*4+f*4+3;
					faces[f].centerNum = facesCount*8+f;
							
					faces[f].neigFaceNums.a=faces[f].neigFaceNums.b=faces[f].neigFaceNums.c=faces[f].neigFaceNums.d= -1;
					faces[f].neigSides.a=faces[f].neigSides.b=faces[f].neigSides.c=faces[f].neigSides.d= -1;

					//int hash = (coord.x+100)%500*200*2000*10 + (coord.y+100)%500*2000*10 + (coord.z+100)%500*10 + coord.dir; //2 000 000 000, max is 2 147 483 647 //same formula is used in Neig Faces	
					int hash =  (((coord.y-matrix.cube.offset.y+1024) & 0xFFF) << 20)  |  
								(((coord.x-matrix.cube.offset.x+128) & 0xFF) << 12)  |  
								(((coord.z-matrix.cube.offset.z+128) & 0xFF) << 4) | 
								coord.dir;
					//y: min -1024, range 4096, max 4096-1024
					//x,z: min -128 max 127
											 
					faceHashes.Add(hash, f); 
				}

				#if WDEBUG
				if (!ThreadWorker.multithreading) Profiler.EndSample ();
				#endif

			#endregion


			#region Finding Neig faces

				#if WDEBUG
				if (!ThreadWorker.multithreading) Profiler.BeginSample ("Finding neigs");
				#endif

				for (int f=0; f<facesCount; f++)
					for (int side=0; side<4; side++)
					{
						if (faces[f].neigFaceNums[side] != -1) continue; //already has neig

						for (int neig=0; neig<3; neig++)
						{
							int i = faces[f].coord.dir*12 + side*3 + neig; //num in neig coords lut
								
							CoordDir neigCoord = neigs[i]; //finding from lut of neig coords
							neigCoord.x += faces[f].coord.x; neigCoord.y += faces[f].coord.y; neigCoord.z += faces[f].coord.z;

							if (neigCoord.x<matrix.cube.offset.x || neigCoord.x>=matrix.cube.offset.x+matrix.cube.size.x || neigCoord.y<matrix.cube.offset.y || neigCoord.y>=matrix.cube.offset.y+matrix.cube.size.y || neigCoord.z<matrix.cube.offset.z || neigCoord.z>=matrix.cube.offset.z+matrix.cube.size.z) continue;

							//int neigHash = (neigCoord.x+100)%500*200*2000*10 + (neigCoord.y+100)%500*2000*10 + (neigCoord.z+100)%500*10 + neigCoord.dir;
							int neigHash =  (((neigCoord.y-matrix.cube.offset.y+1024) & 0xFFF) << 20)  |  
								(((neigCoord.x-matrix.cube.offset.x+128) & 0xFF) << 12)  |  
								(((neigCoord.z-matrix.cube.offset.z+128) & 0xFF) << 4) | 
								neigCoord.dir;
							if (faceHashes.ContainsKey(neigHash))
							{
								faces[f].neigFaceNums[side] = faceHashes[neigHash];
								faces[f].neigSides[side] = neigSide[i];
								break;
							}
						}
					}
			
				#if WDEBUG
				if (!ThreadWorker.multithreading) Profiler.EndSample ();
				#endif

			#endregion
				
		
			#region Welding Verts

				#if WDEBUG
				if (!ThreadWorker.multithreading) Profiler.BeginSample("Welding");
				#endif

				bool[] processed = new bool[faces.Length*9];

				for (int f=0; f<faces.Length; f++)
				{
					//side verts
					for (int s=0; s<4; s++) 
					{
						//no need to add processing test there - it will only take a time
						if (faces[f].neigFaceNums[s] == -1) continue;
						faces[f].sideNums[s] = faces[faces[f].neigFaceNums[s]].sideNums[faces[f].neigSides[s]];
					}

					//corners
					for (int c=0; c<4; c++)
					{
						int cornerNum = faces[f].cornerNums[c];
						if (processed[cornerNum]) continue;

						int nextFace = f; int nextCorner = c;

						for (int i=0; i<1000; i++) //while(true) //iterating infinitely
						{
							//getting next face and next corner
							int temp = faces[nextFace].neigFaceNums[nextCorner]; //storing next face num to temporary value, we'll need original to get corner
							nextCorner = faces[nextFace].neigSides[nextCorner]+1;
							nextFace = temp;

							if (nextFace == -1) break; //if reached boundary
							faces[nextFace].cornerNums[nextCorner] = cornerNum;
							if (nextFace == f) { processed[cornerNum]=true; break; } //if returned to same face

							#if WDEBUG
							if (i==500)
								Debug.LogError("Could not finish welding corners");
							#endif
						}	
					}
				}

				#if WDEBUG
				if (!ThreadWorker.multithreading) Profiler.EndSample();
				#endif

			#endregion


			#region Getting rid of unused vertices

				#if WDEBUG
				if (!ThreadWorker.multithreading) Profiler.BeginSample("Remove Unused Verts");
				#endif

				bool[] used = processed;
				for (int i=0; i<used.Length; i++) used[i] = false;
				int numUsed = 0;

				for (int f=0; f<faces.Length; f++)
				{
					//if (!faces[f].visible) continue; //invisible verts are needed in relax
					for (int c=0; c<4; c++) used[ faces[f].cornerNums[c] ] = true;
					for (int s=0; s<4; s++) used[ faces[f].sideNums[s] ] = true;
					used[ faces[f].centerNum ] = true;
				}

				int[] vertReplaceLut = new int[faces.Length*9];
			
				for (int i=0; i<vertReplaceLut.Length; i++) 
					if (used[i])
					{
						vertReplaceLut[i] = numUsed;
						numUsed++;
					}

				for (int f=0; f<faces.Length; f++)
				{
					//if (!faces[f].visible) continue;
					for (int c=0; c<4; c++) faces[f].cornerNums[c] = vertReplaceLut[ faces[f].cornerNums[c] ];
					for (int s=0; s<4; s++) faces[f].sideNums[s] = vertReplaceLut[ faces[f].sideNums[s] ];
					faces[f].centerNum = vertReplaceLut[ faces[f].centerNum ];
				}

				#if WDEBUG
				if (!ThreadWorker.multithreading) Profiler.EndSample();
				#endif

			#endregion


			#region Creating Verts

				#if WDEBUG
				if(!ThreadWorker.multithreading) Profiler.BeginSample ("Creating verts");
				#endif

				verts = new Vector3[numUsed];

				for (int f=0; f<faces.Length; f++) //TODO: each vert is created twice, add processed test
					for (int n=0; n<4; n++)
					{
						int i = faces[f].coord.dir*4 + n;
						verts[ faces[f].cornerNums[n] ] = new Vector3(
							faces[f].coord.x - matrix.cube.offset.x - margin + cornerPosesX[i] + offset.x,
							faces[f].coord.y + cornerPosesY[i] + offset.y, 
							faces[f].coord.z - matrix.cube.offset.z - margin + cornerPosesZ[i] + offset.z);

						verts[ faces[f].sideNums[n] ]   = new Vector3(
							faces[f].coord.x - matrix.cube.offset.x - margin + sidePosesX[i] + offset.x,   
							faces[f].coord.y + sidePosesY[i] + offset.y,   
							faces[f].coord.z - matrix.cube.offset.z - margin + sidePosesZ[i] + offset.z);

						verts[ faces[f].centerNum ] = new Vector3( //TODO: take it outta n loop
							faces[f].coord.x - matrix.cube.offset.x - margin + centerPosesX[faces[f].coord.dir] + offset.x,   
							faces[f].coord.y + centerPosesY[faces[f].coord.dir] + offset.y,   
							faces[f].coord.z - matrix.cube.offset.z - margin + centerPosesZ[faces[f].coord.dir] + offset.z);
					}

				#if WDEBUG
				if (!ThreadWorker.multithreading) Profiler.EndSample ();
				#endif

			#endregion

			#if WDEBUG
			if (!ThreadWorker.multithreading) Profiler.EndSample ();
			#endif
		}


		public static void RelaxMesh (Face[] faces, Vector3[] verts, int iterations, float strength=1, bool oldStyleRelax=false)
		{
				#if WDEBUG
				if(!ThreadWorker.multithreading) Profiler.BeginSample ("Relax Mesh");
				#endif

				//finding number of corners and sides
				int maxCornerNum = 0;
				for (int f=0; f<faces.Length; f++)
					for (int c=0; c<4; c++)
					{
						int num = faces[f].cornerNums[c];
						if (num > maxCornerNum) maxCornerNum = num;
					}

				int numCorners = maxCornerNum+1;
				int numCenters = faces.Length; //TODO: take only visible faces

				//pinning boundary verts
				bool[] boundary = new bool[verts.Length];
				for (int f=0; f<faces.Length; f++)
					for (int s=0; s<4; s++)
						if (faces[f].neigFaceNums[s] == -1) //if no neig face - marking all 3 verts as boundary
						{
							boundary[ faces[f].cornerNums[s] ] = true;
							boundary[ faces[f].cornerNums[s+1] ] = true;
							boundary[ faces[f].sideNums[s] ] = true;
						}

				//creating new relaxed array to keep original verts unchanged - and adding boundary points
				Vector3[] relaxed = new Vector3[verts.Length];
				for (int v=0; v<verts.Length; v++) if (boundary[v]) relaxed[v] = verts[v];
			
				for (int i=0; i<iterations; i++)
				{
					if (i!=0 && strength>1) strength = 1;
					//strength affects only the first iteration, otherwise artefacts will appear

					//gathering side positions
					Vector3[] sidePosesSum = new Vector3[verts.Length];
					float[] sidePosesNum = new float[verts.Length];

					for (int f=0; f<faces.Length; f++)
						for (int c=0; c<4; c++)
						{
							int cornerNum = faces[f].cornerNums[c];
							int sideNum = faces[f].sideNums[c];
							sidePosesSum[cornerNum] += verts[sideNum];
							sidePosesNum[cornerNum] ++;
						}

					//apply relax
					for (int v=0; v<numCorners; v++) 
							if (!boundary[v]) relaxed[v] = verts[v] + (sidePosesSum[v]/sidePosesNum[v] - verts[v]) * strength;

					//placing side points mid-way between corners
					for (int f=0; f<faces.Length; f++)
						for (int s=0; s<4; s++)
						{
							int sideNum = faces[f].sideNums[s];
							if (boundary[sideNum]) continue;

							if (i==0 || strength<0.7f) //placing side right between corners for the first iteration
								relaxed[sideNum] = ( relaxed[ faces[f].cornerNums[s] ] + relaxed[ faces[f].cornerNums[s+1] ] ) / 2f;

							else //normal relax for all the others
								relaxed[sideNum] = ( verts[ faces[f].cornerNums[s] ] + verts[ faces[f].cornerNums[s+1] ] +
												   verts[ faces[f].centerNum ] + verts[ faces[faces[f].neigFaceNums[s] ].centerNum] ) / 4f;
						}
			
					//placing centers
					for (int f=0; f<faces.Length; f++)
					{
						if (i==0 || strength<0.7f)
							relaxed[ faces[f].centerNum ] = (relaxed[faces[f].sideNums.a] + relaxed[faces[f].sideNums.b] + relaxed[faces[f].sideNums.c] + relaxed[faces[f].sideNums.d] ) / 4f;
						else
							relaxed[ faces[f].centerNum ] = (verts[faces[f].sideNums.a] + verts[faces[f].sideNums.b] + verts[faces[f].sideNums.c] + verts[faces[f].sideNums.d] ) / 4f;
					}

					//saving relaxed array
					for (int v=0; v<verts.Length; v++) verts[v] = relaxed[v];

					//TODO: return chunk border verts back to x or z zero/max
				}

				#if WDEBUG
				if (!ThreadWorker.multithreading) Profiler.EndSample ();
				#endif
		}


		public static Vector3[] RemoveUnusedVerts (Face[] faces, Vector3[] allVerts)
		//note that it could not be merged with CalculateFaces, invisible vers are used in Relax
		{
			#if WDEBUG
			if (!ThreadWorker.multithreading) Profiler.BeginSample("Remove Invisible Verts");
			#endif

			bool[] used = new bool[allVerts.Length];
			int numUsed = 0;

			for (int f=0; f<faces.Length; f++)
			{
				if (!faces[f].visible) continue;
				for (int c=0; c<4; c++) used[ faces[f].cornerNums[c] ] = true;
				for (int s=0; s<4; s++) used[ faces[f].sideNums[s] ] = true;
				used[ faces[f].centerNum ] = true;
			}

			int[] vertReplaceLut = new int[allVerts.Length];
		
			for (int i=0; i<vertReplaceLut.Length; i++) 
				if (used[i])
				{
					vertReplaceLut[i] = numUsed;
					numUsed++;
				}

			for (int f=0; f<faces.Length; f++)
			{
				if (!faces[f].visible) continue;
				for (int c=0; c<4; c++) faces[f].cornerNums[c] = vertReplaceLut[ faces[f].cornerNums[c] ];
				for (int s=0; s<4; s++) faces[f].sideNums[s] = vertReplaceLut[ faces[f].sideNums[s] ];
				faces[f].centerNum = vertReplaceLut[ faces[f].centerNum ];
			}

			Vector3[] verts = new Vector3[numUsed];
			for (int v=0; v<allVerts.Length; v++)
			{
				verts[ vertReplaceLut[v] ] = allVerts[v];
			}

			#if WDEBUG
			if (!ThreadWorker.multithreading) Profiler.EndSample();
			#endif

			return verts;
		}


		public static MeshWrapper CalculateMesh (Face[] faces, Vector3[] verts, bool hipoly=false)
		{
			//hipoly triangles scheme:

				//	2	 4-5		7-8		10	
				//	|\	  \|		|/		/|
				//	1-0	   3		6	   9-11
				//
				//	23-21  18		15	  12-13
				//	| /	  / |		| \	   \ |
				//	22   20-19	    17-16	14


			#if WDEBUG
			if(!ThreadWorker.multithreading) Profiler.BeginSample ("Calculate Mesh");
			#endif

			MeshWrapper mesh = new MeshWrapper();

			#region Calculating number of lowpoly points

				#if WDEBUG
				if(!ThreadWorker.multithreading) Profiler.BeginSample ("Number of corners");
				#endif

				int maxCornerNum = 0;

				for (int f=0; f<faces.Length; f++)
				{
					if (!faces[f].visible) continue;
					for (int c=0; c<4; c++)
					{
						int num = faces[f].cornerNums[c];
						if (num > maxCornerNum) maxCornerNum = num;
					}
				}

				int numCorners = maxCornerNum+1;

				#if WDEBUG
				if (!ThreadWorker.multithreading) Profiler.EndSample ();
				#endif

			#endregion

			#region Verts

				#if WDEBUG
				if(!ThreadWorker.multithreading) Profiler.BeginSample ("Verts");
				#endif

				if (!hipoly)
				{
					mesh.verts = new Vector3[numCorners];
					for (int i=0; i<mesh.verts.Length; i++) mesh.verts[i] = verts[i];
				}
				else mesh.verts = verts;

				#if WDEBUG
				if (!ThreadWorker.multithreading) Profiler.EndSample ();
				#endif

			#endregion

			#region Creating Tris

				#if WDEBUG
				if (!ThreadWorker.multithreading) Profiler.BeginSample ("Tris");
				#endif
			
				//calculating vis face num
				int facesNum = 0;
				for (int f=0; f<faces.Length; f++) 
				if (faces[f].visible) facesNum++;
				
				//tris array
				if (hipoly) mesh.tris = new int[facesNum*8*3];
				else mesh.tris = new int[facesNum*2*3];
			
				int counter = 0;
				for (int f=0; f<faces.Length; f++)
				{
					if (!faces[f].visible) continue;
				
					if (hipoly)
					{
						for (int s=0; s<4; s++)
						{
							//for each quad (1/4 of face) the order of vertices
							//tri0: 00-S3-C0, 
							//tri1: 00-C0-S0

							int j = counter*24 + s*6;
							mesh.tris[j] = mesh.tris[j+3] = faces[f].centerNum;
							mesh.tris[j+1] = faces[f].sideNums[s-1];
							mesh.tris[j+2] = mesh.tris[j+4] = faces[f].cornerNums[s];
							mesh.tris[j+5] = faces[f].sideNums[s];
						}
					}

					else
					{
						//for (int n=0; n<4; n++) mesh.verts[ faces[f].cornerNums[n] ] = new Vector3(faces[f].coord.x, faces[f].coord.y, faces[f].coord.z);

						mesh.tris[counter*6] = faces[f].cornerNums.a;   mesh.tris[counter*6+1] = faces[f].cornerNums.b; mesh.tris[counter*6+2] = faces[f].cornerNums.d;
						mesh.tris[counter*6+3] = faces[f].cornerNums.c; mesh.tris[counter*6+4] = faces[f].cornerNums.d; mesh.tris[counter*6+5] = faces[f].cornerNums.b;
					}
				
					counter++;
				}
			
				#if WDEBUG
				if (!ThreadWorker.multithreading) Profiler.EndSample ();
				#endif

			#endregion

			#if WDEBUG
			if (!ThreadWorker.multithreading) Profiler.EndSample ();
			#endif

			return mesh;
		}

		public static Vector3[] CalculateNormals (Face[] faces, Vector3[] verts, int smoothIterations=0)
		{
			#if WDEBUG
			if (!ThreadWorker.multithreading) Profiler.BeginSample ("Calculate Normals");
			#endif
			
			//gathering face normals
			#if WDEBUG
			if (!ThreadWorker.multithreading) Profiler.BeginSample ("Gathering");
			#endif

			Vector3[] faceNorms = new Vector3[faces.Length];
			for (int f=0; f<faces.Length; f++)
			{
				//calculating central face normal
				Vector3 v0 = verts[faces[f].sideNums.c] - verts[faces[f].sideNums.a];
				Vector3 v1 = verts[faces[f].sideNums.d] - verts[faces[f].sideNums.b];
				
				//faces[f].normal = new Vector3(v0.y*v1.z - v0.z*v1.y, v0.z*v1.x - v0.x*v1.z, v0.x*v1.y - v0.y*v1.x).normalized;
				//normals[faces[f].centerNum] = new Vector3(v0.y*v1.z - v0.z*v1.y, v0.z*v1.x - v0.x*v1.z, v0.x*v1.y - v0.y*v1.x).normalized;
				faceNorms[f] = new Vector3(v0.y*v1.z - v0.z*v1.y, v0.z*v1.x - v0.x*v1.z, v0.x*v1.y - v0.y*v1.x).normalized;
			}

			#if WDEBUG
			if (!ThreadWorker.multithreading) Profiler.EndSample ();
			#endif

			//relaxing normals
			#if WDEBUG
			if (!ThreadWorker.multithreading) Profiler.BeginSample ("Relaxing");
			#endif

			for (int i=0; i<smoothIterations; i++)
			{
				Vector3[] srcNorms = faceNorms;
				Vector3[] dstNorms = new Vector3[faces.Length];
				for (int f=0; f<faces.Length; f++)
				{
					int sum = 2;
					Vector3 norm = srcNorms[f]*2;

					Nodes<int> neigFaceNums = faces[f].neigFaceNums;
					int nf = neigFaceNums.a; if (nf>=0) { norm += srcNorms[nf]; sum++; }
					nf = neigFaceNums.b; if (nf>=0) { norm += srcNorms[nf]; sum++; }
					nf = neigFaceNums.c; if (nf>=0) { norm += srcNorms[nf]; sum++; }
					nf = neigFaceNums.d; if (nf>=0) { norm += srcNorms[nf]; sum++; }

					dstNorms[f] = norm/sum;
				}

				faceNorms = dstNorms; //switching arrays (in case of iterational smooth) and writing back faceNorms
				dstNorms = srcNorms;
				srcNorms = faceNorms;
			}

			#if WDEBUG
			if (!ThreadWorker.multithreading) Profiler.EndSample ();
			#endif


			//setting verts normals
			#if WDEBUG
			if (!ThreadWorker.multithreading) Profiler.BeginSample ("Setting");
			#endif

			Vector3[] vertNorms = new Vector3[verts.Length];
			byte[] vertSum = new byte[verts.Length];
			for (int f=0; f<faces.Length; f++)
			{
				Vector3 faceNormal = faceNorms[f];

				for (int c=0; c<4; c++)
				{
					int num = faces[f].cornerNums[c];
					vertNorms[num] += faceNormal;
					vertSum[num]++;
				}
				for (int s=0; s<4; s++)
				{
					int num = faces[f].sideNums[s];
					vertNorms[num] += faceNormal;
					vertSum[num]++;
				}
				
				{
					int num = faces[f].centerNum;
					vertNorms[num] += faceNormal;
					vertSum[num]++;
				}
			}

			for (int v=0; v<verts.Length; v++)
			{
				byte sum = vertSum[v];
				if (sum != 0) vertNorms[v] /= vertSum[v];
				else vertNorms[v] = new Vector3(0,0,0);
			}


			/*bool[] processed = new bool[verts.Length];
			for (int f=0; f<faces.Length; f++)
			{
				if (!faces[f].visible) continue;
				int num = 0;
				
				//corners
				for (int c=0; c<4; c++)
				{
					num = faces[f].cornerNums[c];
					if (processed[num]) continue;
					
					int div = 1; 
					//Vector3 sum = faces[f].normal; 
					Vector3 sum = faceNorms[f]; //vertNorms[faces[f].centerNum]; //this face normal

					int nextFace = f; int nextCorner = c;
					
					while (div < 8) //could not be more than 7 welded faces
					{
						//getting next face and next corner
						int temp = faces[nextFace].neigFaceNums[nextCorner]; //storing next face num to temporary value, we'll need original to get corner
						nextCorner = faces[nextFace].neigSides[nextCorner] + 1; //it's a next corner after side, so adding 1
						nextFace = temp;
						
						if (nextFace == -1 || nextFace == f) break; //if reached boundary
						
						//sum += faces[nextFace].normal;
						sum += vertNorms[faces[nextFace].centerNum]; //next face normal
						div++;
					}
					
					vertNorms[num] = (sum/div).normalized;
					processed[num] = true;
				}
				
				//sides
				for (int s=0; s<4; s++)
				{
					num = faces[f].sideNums[s];
					if (processed[num]) continue;
					if (faces[f].neigFaceNums[s] < 0) 
					{ 
						//normals[num] = faces[f].normal; 
						vertNorms[num] = vertNorms[faces[f].centerNum]; //this face normal
						continue; 
					}
					
					Vector3 thisNormal = vertNorms[faces[f].centerNum];
					Vector3 neigNormal = vertNorms[faces[ faces[f].neigFaceNums[s] ].centerNum];
					vertNorms[num] = ((thisNormal + neigNormal) / 2f).normalized;
					
					processed[num] = true;
				}
				
				//center
				//normals[faces[f].centerNum] = faces[f].normal;
			}*/

			#if WDEBUG
			if (!ThreadWorker.multithreading) Profiler.EndSample ();
			#endif

			#if WDEBUG
			if (!ThreadWorker.multithreading) Profiler.EndSample ();
			#endif

			return vertNorms;
		}

		public static void GridBorderVerts (Vector3[] verts, int chunkSize, float gridSize=0.1f)
		{
			for (int v=0; v<verts.Length; v++)
			{
				float x = verts[v].x; float z = verts[v].z;

				//if border vert
				if (x<0.1f || x>chunkSize-0.1f)
				{
					verts[v] = new Vector3( x,
											((int)(verts[v].y/gridSize+0.5f)) * gridSize,
											((int)(z/gridSize+0.5f)) * gridSize);
				}

				if (z<0.1f || z>chunkSize-0.1f)
				{
					verts[v] = new Vector3( ((int)(x/gridSize+0.5f)) * gridSize,
											((int)(verts[v].y/gridSize+0.5f)) * gridSize,
											z);
				}
			}
		}


		public static void OffsetBorderVerts (Vector3[] verts, Vector3[] normals, int chunkSize, float dist)
		{
			for (int v=0; v<verts.Length; v++)
			{
				float x = verts[v].x; float z = verts[v].z;

				//if border x vert
				if (x<0.1f)
				{
					verts[v] = new Vector3(x+dist/2,
									verts[v].y + normals[v].y*dist,
									z + normals[v].z*dist);
				}
				else if (x>chunkSize-0.1f)
				{
					verts[v] = new Vector3(x-dist/2,
									verts[v].y + normals[v].y*dist,
									z + normals[v].z*dist);
				}

				//border z vert
				if (z<0.1f)
				{
					verts[v] = new Vector3(x + normals[v].x*dist,
									verts[v].y + normals[v].y*dist,
									z+dist/2);
				}
				else if (z>chunkSize-0.1f)
				{
					verts[v] = new Vector3(x + normals[v].x*dist,
									verts[v].y + normals[v].y*dist,
									z-dist/2);
				}
			}
		}
		
		
		public static CoordDir[] CalculateIndexToCoord (Face[] faces)
		{
			int numVisFaces = 0;
			for (int i=0; i<faces.Length; i++) 
				if (faces[i].visible) numVisFaces++;

			CoordDir[] coords = new CoordDir[numVisFaces];

			int counter = 0;
			for (int i=0; i<faces.Length; i++) 
			{
				if (!faces[i].visible) continue; 
				coords[counter] = faces[i].coord;
				counter++;
			}
			return coords;
		}

		#endregion


		#region Texturing (Channels) Operations

		public static bool IsChannelUsed (Face[] faces, int chNum)
		{
			for (int f=0; f<faces.Length; f++)
				if (faces[f].type == chNum) return true;
			return false;
		}

		public static void FillChWeights (Face[] faces, Vector3[] verts, int chNum, ref float[] vertChWeights)
		{
			int facesLength = faces.Length; 
			int vertsLength = verts.Length;

			//finding if this channel is ever used in chunk


			byte[] vertChSums = new byte[verts.Length];

			float[] bluredFaceWeights = new float[faces.Length];
			float[] bluredTmp = new float[faces.Length];
			for (int f=0; f<facesLength; f++)
				bluredFaceWeights[f] = faces[f].type==chNum? 1 : 0;  //emulating blur

			//blurring 
			for (int i=0; i<2; i++)
			{
				for (int f=0; f<facesLength; f++)
				{
					float val = bluredFaceWeights[f] * 2;
					int div = 2;
						
					Nodes<int> neigFaceNums = faces[f].neigFaceNums;
					int nf = neigFaceNums.a; if (nf>=0) { val += bluredFaceWeights[nf]; div++; }
					nf = neigFaceNums.b; if (nf>=0) { val += bluredFaceWeights[nf]; div++; }
					nf = neigFaceNums.c; if (nf>=0) { val += bluredFaceWeights[nf]; div++; }
					nf = neigFaceNums.d; if (nf>=0) { val += bluredFaceWeights[nf]; div++; }

					bluredTmp[f] = val / div;
				}

				//switching blur and orig arrays
				float[] temp = bluredFaceWeights;
				bluredFaceWeights = bluredTmp;
				bluredTmp = temp;
			}

			//populating verts array
			for (int v=0; v<vertsLength; v++) { vertChWeights[v] = 0; vertChSums[v] = 0; }
            for (int f=0; f<facesLength; f++)
			{
				float faceOrig = faces[f].type==chNum? 1 : 0; //mixing original non-blurred values with blured ones

				for (int c=0; c<4; c++)
				{
					int num = faces[f].cornerNums[c];
					vertChWeights[num] += bluredFaceWeights[f]/2 + faceOrig/2;
					vertChSums[num]++;
				}
				for (int s=0; s<4; s++)
				{
					int num = faces[f].sideNums[s];
					vertChWeights[num] += bluredFaceWeights[f]/2 + faceOrig/2;
					vertChSums[num]++;
				}

				{
					int num = faces[f].centerNum;
					vertChWeights[num] += bluredFaceWeights[f]/2 + faceOrig/2;
					vertChSums[num]++;
				}
			}

			for (int v=0; v<vertsLength; v++)
			{
				float fval = vertChWeights[v];
				
				if (vertChSums[v] == 0) Debug.Log("Sum zero");
				fval /= vertChSums[v];

				//applying fallof
				fval = (Mathf.Sqrt(fval) * (1 - fval)) + fval*fval*fval;

				vertChWeights[v] = fval;
			}
	
		}


		public static void EncodeChWeights (float[] vertChWeights, int chNum, ref Vector4[] tangents)
		{
			for (int v=0; v<vertChWeights.Length; v++)
			{
				float fval = vertChWeights[v];

				int ival = (int)(fval * 15);
				if (ival==0) continue;

				//note adding Data.minLandType
				if (chNum<6)
				{
					int tan = (int)tangents[v].x;
					tan = tan | (ival << (chNum*4));
					tangents[v].x = tan;
				}
				else if (chNum<12) //types from 7 to 12 inclusive
				{
					int tan = (int)tangents[v].y;
					tan = tan | (ival << ((chNum-6)*4));
					tangents[v].y = tan;
				}
				else if (chNum<18)
				{
					int tan = (int)tangents[v].z;
					tan = tan | (ival << ((chNum-12)*4));
					tangents[v].z = tan;
				}
				else if (chNum<24)
				{
					int tan = (int)tangents[v].w;
					tan = tan | (ival << ((chNum-18)*4));
					tangents[v].w = tan;
				}
			}
		}

		public static void EncodeColorChWeights (float[] vertChWeights, int chNum, ref Color[] colors)
		{
			for (int v=0; v<vertChWeights.Length; v++)
			{
				float fval = vertChWeights[v];
				
				switch (chNum)
				{
					//note adding Data.minLandType
					case 0: colors[v].r = fval; break;
					case 1: colors[v].g = fval; break;
					case 2: colors[v].b = fval; break;
					case 3: colors[v].a = fval; break;
				}
			}
		}

		public static void EncodeMegaSplatFilters (Face[] faces, ref Color[] colors)
		{
			// filters in vertex color, main splat in color.a, secondary in uv2.a

			//filters
			for (int f=0; f<faces.Length; f++)
			{
				for (int c=0; c<4; c++)
				{
					int num = faces[f].cornerNums[c];
					colors[num] = new Color(1,0,0,0);
				}
				for (int s=0; s<4; s++)
				{
					int num = faces[f].sideNums[s];
					colors[num] = new Color(0,1,0,0);
				}

				{
					int num = faces[f].centerNum;
					colors[num] = new Color(0,0,1,0);
				}
			}
		}

		public static void FillMegaSplatChTopTypes (float[] vertChWeights, int chNum, byte[] topTypes, byte[] secTypes, float[] topBlends, float[] secBlends)
		/// For each channel adjusts the top (and sec) layer index (along with the blend weight)
		{
			//splats
			for (int v=0; v<vertChWeights.Length; v++)
			{
				float val = vertChWeights[v];

				if (val > topBlends[v])  //if this channel is higher than top splat
				{
					secTypes[v] = topTypes[v]; //moving top to second splat
					secBlends[v] = topBlends[v]; 

					topTypes[v] = (byte)chNum;
					topBlends[v] = val;
				}
				else if (val > secBlends[v]) //if higher than second splat
				{
					secTypes[v] = (byte)chNum;
					secBlends[v] = val;
				}
			}
		}

		public static void EncodeMegaSplatChWeights (byte[] topTypes, byte[] secTypes, float[] topBlends, float[] secBlends, ref Color[] colors, ref Vector4[] uv2)
		/// Encodes prepared per-cahnnel top/secondary types to color and uv3 to use with MegaSplat
		{
			for (int v=0; v<topTypes.Length; v++)
			{
				//secondary splat is in uv3.a, blend is in uv3.x
				//Jason
				colors[v].a = topTypes[v] / 255f; //note 255, not 256
				uv2[v].w = secTypes[v] / 255f;
				uv2[v].x = 0; //blend;
			}
		}


		public static Vector4[] CalculateChannels (Face[] faces, Vector3[] verts, int numCh=24, float blurStrength=1f, bool centerAccent=true, bool simpleEncode=false)
		{
			#if WDEBUG
			if (!ThreadWorker.multithreading) Profiler.BeginSample ("Calculate Channels");
			#endif
			
			int facesLength = faces.Length; int vertsLength = verts.Length;
			//float[] orig = new float[faces.Length];
			float[] blurSrc = new float[faces.Length];
			float[] blurDst = new float[faces.Length];

			float[] vertVals = new float[verts.Length];
			byte[] vertSums = new byte[verts.Length];

			Vector4[] tangents = new Vector4[verts.Length];

			//calculating same type neigs first (this could be made non per-channel)
			byte[] sameTypeNeigs = new byte[faces.Length];
			for (int f=0; f<facesLength; f++)
			{
				byte stn = 0;
				byte type = faces[f].type;
					
				Nodes<int> neigFaceNums = faces[f].neigFaceNums;
				int nf = neigFaceNums.a; if (nf>=0 && faces[nf].type == type) stn++;
				nf = neigFaceNums.b; if (nf>=0 && faces[nf].type == type) stn++;
				nf = neigFaceNums.c; if (nf>=0 && faces[nf].type == type) stn++;
				nf = neigFaceNums.d; if (nf>=0 && faces[nf].type == type)  stn++;

				sameTypeNeigs[f] = stn;
			}

			//and based on sameTypeNeigs per-vertex blur factor
			byte[] blurFactors = new byte[verts.Length]; //from 0 to 255
			for (int v=0; v<blurFactors.Length; v++) blurFactors[v] = 255;
			//byte[] blurCases = new byte[] { 0, (byte)(255*0.3f), (byte)(255*0.4f), 255, 255, 255 };
			byte blurCase1 = (byte)(255*0.3f); byte blurCase2 = (byte)(255*0.4f);
			for (int f=0; f<facesLength; f++)
			{
				byte stn = sameTypeNeigs[f];

				//byte blurVal = blurCases[stn]; //switch is faster and more clear
				byte blurVal;
				switch (stn)
				{
					case 0: blurVal = 0; break;
					case 1: blurVal = blurCase1; break;
					case 2: blurVal = blurCase2; break;
					default: continue;
				}
				
				for (int c=0; c<4; c++)
				{
					int num = faces[f].cornerNums[c];
					if (blurVal < blurFactors[num]) blurFactors[num] = stn;
				}
				for (int s=0; s<4; s++)
				{
					int num = faces[f].sideNums[s];
					if (blurVal < blurFactors[num]) blurFactors[num] = stn;
				}

				{
					int num = faces[f].centerNum;
					if (blurVal < blurFactors[num]) blurFactors[num] = stn;
				}
			}
			
			//per-cahnnel types calculation
			for (int ch=0; ch<numCh; ch++) //TODO: replace 1 with 0
			{
				//finding if this channel is ever used in chunk
				bool used = false;
				for (int f=0; f<facesLength; f++)
					if (faces[f].type == ch) { used=true; break; }
				if (!used) continue;

				//resetting orig and blur arrays, initial orig scatter
				for (int f=0; f<facesLength; f++)
				{
					float val = faces[f].type==ch? 1 : 0; 
					//orig[f] = val;
					blurSrc[f] = val;
					blurDst[f] = 0;
				}

				//iterating 
				for (int i=0; i<3; i++)
				{
					for (int f=0; f<facesLength; f++)
					{
						float val = blurSrc[f] * 2;
						int div = 2;
						
						Nodes<int> neigFaceNums = faces[f].neigFaceNums;
						int nf = neigFaceNums.a; if (nf>=0) { val += blurSrc[nf]; div++; }
						nf = neigFaceNums.b; if (nf>=0) { val += blurSrc[nf]; div++; }
						nf = neigFaceNums.c; if (nf>=0) { val += blurSrc[nf]; div++; }
						nf = neigFaceNums.d; if (nf>=0) { val += blurSrc[nf]; div++; }

						blurDst[f] = val / div;
					}

					//switching blur and orig arrays
					float[] temp = blurSrc;
					blurSrc = blurDst;
					blurDst = temp;
				}

				//populating verts array
				for (int v=0; v<vertsLength; v++) { vertVals[v] = 0; vertSums[v] = 0; }
                for (int f=0; f<facesLength; f++)
				{
					float faceOrig = faces[f].type==ch? 1 : 0; //mixing original non-blurred values with blured ones
					//float blurStrength = 0.8f;

					for (int c=0; c<4; c++)
					{
						int num = faces[f].cornerNums[c];
						float curBlurStrength = blurFactors[num] / 255f * blurStrength;
						vertVals[num] += blurSrc[f]*curBlurStrength + faceOrig*(1-curBlurStrength);
						vertSums[num]++;
					}
					for (int s=0; s<4; s++)
					{
						int num = faces[f].sideNums[s];
						float curBlurStrength = blurFactors[num] / 255f * blurStrength;
						vertVals[num] += blurSrc[f]*curBlurStrength + faceOrig*(1-curBlurStrength);
						vertSums[num]++;
					}

					{
						int num = faces[f].centerNum;
						float curBlurStrength = blurFactors[num] / 255f * blurStrength;
						vertVals[num] += blurSrc[f]*curBlurStrength + faceOrig*(1-curBlurStrength);
						vertSums[num]++;
					}
				}

				//setting tangents/colors
				if (simpleEncode)
				{
					for (int v=0; v<verts.Length; v++)
					{
						float fval = vertVals[v]; int sum = vertSums[v];
						if (sum!=0) fval = fval / sum;

						switch (ch)
						{
							//note adding Data.minLandType
							case 0: tangents[v].x = fval; break;
							case 1: tangents[v].y = fval; break;
							case 2: tangents[v].z = fval; break;
							case 3: tangents[v].w = fval; break;
						}
					}
				}
				else
				{
					for (int v=0; v<verts.Length; v++)
					{
						float fval = vertVals[v]; int sum = vertSums[v];
						if (sum!=0) fval = fval / sum;

						//applying fallof
						fval = (Mathf.Sqrt(fval) * (1 - fval)) + fval*fval*fval;

						int ival = (int)(fval * 15);
						if (ival==0) continue;

						//note adding Data.minLandType
						if (ch<6)
						{
							int tan = (int)tangents[v].x;
							tan = tan | (ival << (ch*4));
							tangents[v].x = tan;
						}
						else if (ch<12) //types from 7 to 12 inclusive
						{
							int tan = (int)tangents[v].y;
							tan = tan | (ival << ((ch-6)*4));
							tangents[v].y = tan;
						}
						else if (ch<18)
						{
							int tan = (int)tangents[v].z;
							tan = tan | (ival << ((ch-12)*4));
							tangents[v].z = tan;
						}
						else if (ch<24)
						{
							int tan = (int)tangents[v].w;
							tan = tan | (ival << ((ch-18)*4));
							tangents[v].w = tan;
						}
					}
				}



			}//for channels
			
			#if WDEBUG
			if (!ThreadWorker.multithreading) Profiler.EndSample ();
			#endif

			return tangents;
		}

		#endregion



		#region Ambient

			public static ComputeShader ambientShader;
			public static int ambientKernelNum = -1;

			public static readonly byte ambientFilled = Data.emptyByte;

			public static void HeightmapAmbient (ref Matrix3<byte> ambient, Matrix heightmap)
			{
				Coord min = heightmap.rect.Min+1; Coord max = heightmap.rect.Max-1;

				#region Smoothing Heightmap

					#if WDEBUG
					if(!ThreadWorker.multithreading) Profiler.BeginSample("Smoothing Heightmap");
					#endif

					for (int i=0; i<2; i++)
					{
						for (int x=min.x; x<max.x; x++)
							for (int z=min.z; z<max.z; z++)
							{
								//TODO: optimize
								heightmap[x,z] = (
									Mathf.Max(heightmap[x,z], heightmap[x+1,z]) + 
									Mathf.Max(heightmap[x,z], heightmap[x-1,z]) + 
									Mathf.Max(heightmap[x,z], heightmap[x,z+1]) + 
									Mathf.Max(heightmap[x,z], heightmap[x,z-1]) ) / 4f;
							}
					}

					//lowering heightmap 1 block to prevent stairs effect
					for (int i=0; i<heightmap.array.Length; i++) 
						heightmap.array[i]--;

					#if WDEBUG
					if(!ThreadWorker.multithreading) Profiler.EndSample();
					#endif

				#endregion

				#region Filling Heightmap
				
					#if WDEBUG
					if(!ThreadWorker.multithreading) Profiler.BeginSample("Filling Heightmap");
					#endif
					
					min = heightmap.rect.Min; max = heightmap.rect.Max;
					int minY = ambient.cube.offset.y;
					int maxY = ambient.cube.offset.y + ambient.cube.size.y;
					
					for (int x=min.x; x<max.x; x++)
						for (int z=min.z; z<max.z; z++)
					{
						for (int y=minY; y<maxY; y++)
						{
							if (ambient[x,y,z] != Data.emptyByte) { ambient[x,y,z] = ambientFilled; continue; }  //inverting matrix - filled blocks are now 240, empty blocks have ambient value

							float val = y - heightmap[x,z]+1; //smoothed heightmap has gradients
							if (val > 1) val = 1; if (val < 0) val = 0;
							ambient[x,y,z] = (byte)(val * (ambientFilled-1));
						}
					}

					#if WDEBUG
					if(!ThreadWorker.multithreading) Profiler.EndSample();
					#endif

				#endregion
			}

			public static void SpreadAmbient (ref Matrix3<byte> byteambient, float fade)
			{
				#region Spreading

					#if WDEBUG
					if (!ThreadWorker.multithreading) Profiler.BeginSample ("Spreading");
					#endif

					CoordDir min3D = byteambient.cube.Min; CoordDir max3D = byteambient.cube.Max;

					int prev;
					for (int itaration=0; itaration<2; itaration++)
					{	
						for (int x=min3D.x; x<max3D.x; x++)	
							for (int z=min3D.z; z<max3D.z; z++)	
						{
							prev = ambientFilled-1;
							for (int y=max3D.y-1; y>=min3D.y; y--)	
							{
								int num = (z-byteambient.cube.offset.z)*byteambient.cube.size.x*byteambient.cube.size.y + (y-byteambient.cube.offset.y)*byteambient.cube.size.x + x - byteambient.cube.offset.x;

								byte curVal = byteambient.array[num];
								if (curVal == ambientFilled) { prev = 0; continue; }

								byte newVal = (byte)(prev * fade + 0.5f);

								if (newVal > curVal) { byteambient.array[num] = newVal; prev = newVal; }
								else prev = curVal;
							}
						}


						for (int y=min3D.y; y<max3D.y; y++)	
							for (int z=min3D.z; z<max3D.z; z++)	
						{
							prev = ambientFilled-1;
							for (int x=max3D.x-1; x>=min3D.x; x--)	
							{
								int num = (z-byteambient.cube.offset.z)*byteambient.cube.size.x*byteambient.cube.size.y + (y-byteambient.cube.offset.y)*byteambient.cube.size.x + x - byteambient.cube.offset.x;
								byte curVal = byteambient.array[num];
								if (curVal == ambientFilled) { prev = 0; continue; }

								byte newVal = (byte)(prev * fade + 0.5f);

								if (newVal > curVal) { byteambient.array[num] = newVal; prev = newVal; }
								else prev = curVal;
							}

							prev = ambientFilled-1;
							for (int x=min3D.x; x<max3D.x; x++)	
							{
								int num = (z-byteambient.cube.offset.z)*byteambient.cube.size.x*byteambient.cube.size.y + (y-byteambient.cube.offset.y)*byteambient.cube.size.x + x - byteambient.cube.offset.x;
								byte curVal = byteambient.array[num];
								if (curVal == ambientFilled) { prev = 0; continue; }

								byte newVal = (byte)(prev * fade + 0.5f);

								if (newVal > curVal) { byteambient.array[num] = newVal; prev = newVal; }
								else prev = curVal;
							}
						}

						for (int y=min3D.y; y<max3D.y; y++)	
							for (int x=min3D.x; x<max3D.x; x++)	
						{
							prev = ambientFilled-1;
							for (int z=max3D.z-1; z>=min3D.z; z--)	
							{
								int num = (z-byteambient.cube.offset.z)*byteambient.cube.size.x*byteambient.cube.size.y + (y-byteambient.cube.offset.y)*byteambient.cube.size.x + x - byteambient.cube.offset.x;
								byte curVal = byteambient.array[num];
								if (curVal == ambientFilled) { prev = 0; continue; }

								byte newVal = (byte)(prev * fade + 0.5f);

								if (newVal > curVal) { byteambient.array[num] = newVal; prev = newVal; }
								else prev = curVal;
							}

							prev = ambientFilled-1;
							for (int z=min3D.z; z<max3D.z; z++)	
							{
								int num = (z-byteambient.cube.offset.z)*byteambient.cube.size.x*byteambient.cube.size.y + (y-byteambient.cube.offset.y)*byteambient.cube.size.x + x - byteambient.cube.offset.x;
								byte curVal = byteambient.array[num];
								if (curVal == ambientFilled) { prev = 0; continue; }

								byte newVal = (byte)(prev * fade + 0.5f);

								if (newVal > curVal) { byteambient.array[num] = newVal; prev = newVal; }
								else prev = curVal;
							}
						}
					}

					#if WDEBUG
					if (!ThreadWorker.multithreading) Profiler.EndSample ();
					#endif

				#endregion
			}

			public static void BlurAmbient (ref Matrix3<byte> ambient)
			{
				#if WDEBUG
				if(!ThreadWorker.multithreading) Profiler.BeginSample("Blurring");
				#endif

					CoordDir min3D = ambient.cube.Min; CoordDir max3D = ambient.cube.Max;
					
					//min = heightmap.rect.Min; max = heightmap.rect.Max;
					
					for (int x=min3D.x; x<max3D.x; x++)	
						for (int z=min3D.z; z<max3D.z; z++)	
							for (int y=min3D.y+1; y<max3D.y-1; y++)	
							{
								int num = (z-ambient.cube.offset.z)*ambient.cube.size.x*ambient.cube.size.y + (y-ambient.cube.offset.y)*ambient.cube.size.x + x - ambient.cube.offset.x;
								int currVal = ambient.array[num];
								if (currVal == ambientFilled) continue;

								byte prevVal = ambient.array[num + ambient.cube.size.x]; //ambient[x,y-1,z];
								byte nextVal = ambient.array[num - ambient.cube.size.x]; //ambient[x,y+1,z];

								int div = 2; currVal *=2;
								if (prevVal != ambientFilled) { currVal+=prevVal; div++; }
								if (nextVal != ambientFilled) { currVal+=nextVal; div++; }

								#if WDEBUG
								if (currVal/div > ambientFilled) { Debug.LogError("CurrVal/div exeeds 240:" + currVal + "/" + div + ", " + prevVal + "," + nextVal); return; }
								#endif

								ambient.array[num] = (byte)(currVal / div);
							}
					for (int y=min3D.y; y<max3D.y; y++)	
						for (int x=min3D.x; x<max3D.x; x++)	
							for (int z=min3D.z+1; z<max3D.z-1; z++)	
							{
								int num = (z-ambient.cube.offset.z)*ambient.cube.size.x*ambient.cube.size.y + (y-ambient.cube.offset.y)*ambient.cube.size.x + x - ambient.cube.offset.x;
								int currVal = ambient.array[num];
								if (currVal == ambientFilled) continue;

								byte prevVal = ambient.array[num + ambient.cube.size.x*ambient.cube.size.y]; //ambient[x,y,z-1];
								byte nextVal = ambient.array[num - ambient.cube.size.x*ambient.cube.size.y]; //ambient[x,y,z+1];

								int div = 2; currVal *=2;
								if (prevVal != ambientFilled) { currVal+=prevVal; div++; }
								if (nextVal != ambientFilled) { currVal+=nextVal; div++; }
								ambient.array[num] = (byte)(currVal / div);
							}

					for (int y=min3D.y; y<max3D.y; y++)	
						for (int z=min3D.z; z<max3D.z; z++)	
							for (int x=min3D.x+1; x<max3D.x-1; x++)	
							{
								int num = (z-ambient.cube.offset.z)*ambient.cube.size.x*ambient.cube.size.y + (y-ambient.cube.offset.y)*ambient.cube.size.x + x - ambient.cube.offset.x;
								float currVal = ambient.array[num];
								if (currVal == ambientFilled) continue;

								byte prevVal = ambient.array[num-1];
								byte nextVal = ambient.array[num+1];

								int div = 2; currVal *=2;
								if (prevVal != ambientFilled) { currVal+=prevVal; div++; }
								if (nextVal != ambientFilled) { currVal+=nextVal; div++; }
								ambient.array[num] = (byte)(currVal / div);
							}
							
				#if WDEBUG
				if(!ThreadWorker.multithreading) Profiler.EndSample();
				#endif
			}

			public static void EqualizeBordersAmbient (ref Matrix3<byte> ambient, int margin=0)
			{

				//TODO this will not be needed if welding will be used
				#region Borders

				#if WDEBUG
				if(!ThreadWorker.multithreading) Profiler.BeginSample("Borders");
				#endif
					
				if (margin > 0)
				{
					Coord min = ambient.cube.Min.coord + margin; //new Coord(ambient.offsetX+margin, ambient.offsetZ+margin);
					Coord max = ambient.cube.Max.coord - margin - 1; // new Coord(ambient.offsetX+ambient.sizeX-margin-1, ambient.offsetZ+ambient.sizeZ-margin-1);
					int minY = ambient.cube.offset.y+1; int maxY = ambient.cube.offset.y+ambient.cube.size.y-1;

					for (int y=minY; y<maxY; y++)
					{
						for (int x=min.x; x<max.x; x++)
						{
							if (ambient[x,y,min.z-1] != ambientFilled)
								ambient[x,y,min.z] = (byte)(((int)ambient[x,y,min.z] + (int)ambient[x,y,min.z-1]) / 2);
							if (ambient[x,y,max.z+1] != ambientFilled)
								ambient[x,y,max.z] = (byte)(((int)ambient[x,y,max.z] + (int)ambient[x,y,max.z+1]) / 2);
						}
						for (int z=min.z; z<max.z; z++)
						{
							if (ambient[min.x-1,y,z] != ambientFilled)
								ambient[min.x,y,z] = (byte)(((int)ambient[min.x,y,z] + (int)ambient[min.x-1,y,z]) / 2);
							if (ambient[max.x+1,y,z] != ambientFilled)
								ambient[max.x,y,z] = (byte)(((int)ambient[max.x,y,z] + (int)ambient[max.x+1,y,z]) / 2);
						}
					}
				}

				#if WDEBUG
				if(!ThreadWorker.multithreading) Profiler.EndSample();
				#endif

				#endregion
			}



			public static Vector2[] SetAmbient (Matrix3<byte> ambientMatrix, int[] tris, CoordDir[] indexToCoord, int numVerts)
			{
				#if WDEBUG
				Profiler.BeginSample ("Setting Ambient");
				#endif

UnityEngine.Profiling.Profiler.BeginSample ("New Calc");

				float[] ambients = new float[numVerts];
				int[] divs = new int[numVerts];
	
				for (int i=0; i<indexToCoord.Length; i++)
				{
					CoordDir coord = indexToCoord[i];
					CoordDir nCoord = coord.opposite;
					float val = 1f*ambientMatrix[nCoord.x, nCoord.y, nCoord.z] / Data.maxByte; 

					/*for (int c=0; c<24; c++)
					{
						int vNum = tris[i*24+c]; 
						ambients[vNum] += val;
						divs[vNum] ++;
					}*/

					//setting only border verts
					int v;
					v=tris[i*24];		ambients[v]=val;  divs[v]=1; //center
					v=tris[i*24 + 1];	ambients[v]+=val; divs[v]++; //side
					v=tris[i*24 + 2];	ambients[v]+=val; divs[v]++; //corner
					v=tris[i*24 + 5];	ambients[v]+=val; divs[v]++; //side
					v=tris[i*24 + 8];	ambients[v]+=val; divs[v]++; //corner
					v=tris[i*24 + 11];	ambients[v]+=val; divs[v]++; //side
					v=tris[i*24 + 14];	ambients[v]+=val; divs[v]++; //corner
					v=tris[i*24 + 17];	ambients[v]+=val; divs[v]++; //side
					v=tris[i*24 + 20];	ambients[v]+=val; divs[v]++; //corner
				}

UnityEngine.Profiling.Profiler.EndSample ();
				

UnityEngine.Profiling.Profiler.BeginSample ("To UV");

				Vector2[] uvs = new Vector2[numVerts];
				for (int i=0; i<uvs.Length; i++)
				{ 
					float val = ambients[i]/divs[i];
					uvs[i].x = val; // = new Vector2(val, val);
					uvs[i].y = val;
				}
UnityEngine.Profiling.Profiler.EndSample ();
				#if WDEBUG
				 Profiler.EndSample();
				#endif

				return uvs;
			}

			public static Vector2[] SetGrassAmbient (Matrix3<byte> ambientMatrix, Vector3[] verts, int[] tris, Vector2[] uvs, Vector3 meshOffset)
			{
				#if WDEBUG
				if(!ThreadWorker.multithreading) Profiler.BeginSample ("Setting Grass Ambient");
				#endif

				//Vector2[] uvs = new Vector2[verts.Length];

				int trisCount = tris.Length / 3;
				for (int t=0; t<trisCount; t++)
				{
					int vert1 = tris[t*3];
					int vert2 = tris[t*3 + 1];
					int vert3 = tris[t*3 + 2];

					//by center
					//byte amb = ambientMatrix[
					//	(int)( (verts[vert1].x+verts[vert2].x+verts[vert3].x)/3f + meshOffset.x + 0.5f), 
					//	(int)( (verts[vert1].y+verts[vert2].y+verts[vert3].y)/3f + meshOffset.y + 1f), 
					//	(int)( (verts[vert1].z+verts[vert2].z+verts[vert3].z)/3f + meshOffset.z + 0.5f)	]; 

					//by verts
					byte amb1 = 0;
					if ((int)(verts[vert1].y + meshOffset.y + 1f) == ambientMatrix.cube.offset.y + ambientMatrix.cube.size.y) amb1 = 1;
					else amb1 = ambientMatrix[
						(int)(verts[vert1].x + meshOffset.x + 0.5f), 
						(int)(verts[vert1].y + meshOffset.y + 1f), 
						(int)(verts[vert1].z + meshOffset.z + 0.5f)	]; 
					if (amb1 == ambientFilled) amb1 = 0;

					byte amb2 = 0;
					if ((int)(verts[vert2].y + meshOffset.y + 1f) == ambientMatrix.cube.offset.y + ambientMatrix.cube.size.y) amb2 = 1;
					else amb2 = ambientMatrix[
						(int)(verts[vert2].x + meshOffset.x + 0.5f), 
						(int)(verts[vert2].y + meshOffset.y + 1f), 
						(int)(verts[vert2].z + meshOffset.z + 0.5f)	]; 
					if (amb2 == ambientFilled) amb2 = 0;

					byte amb3 = 0;
					if ((int)(verts[vert3].y + meshOffset.y + 1f) == ambientMatrix.cube.offset.y + ambientMatrix.cube.size.y) amb3 = 1;
					else amb3 = ambientMatrix[
						(int)(verts[vert3].x + meshOffset.x + 0.5f), 
						(int)(verts[vert3].y + meshOffset.y + 1f), 
						(int)(verts[vert3].z + meshOffset.z + 0.5f)	]; 
					if (amb3 == ambientFilled) amb3 = 0;

					if (amb1>amb2) amb2=amb1;
					if (amb2>amb3) amb3=amb2;

					uvs[vert1] = new Vector2(amb3 / 256f, uvs[vert1].y);
					uvs[vert2] = new Vector2(amb3 / 256f, uvs[vert2].y);
					uvs[vert3] = new Vector2(amb3 / 256f, uvs[vert3].y);
				}

				#if WDEBUG
				if(!ThreadWorker.multithreading) Profiler.EndSample();
				#endif

				return uvs;

			}
		#endregion


		#region Grass 

			public static MeshWrapper CalculateGrassMesh (Matrix2<byte> grassMatrix, Matrix2<ushort> topLevels,  Vector3[] loVerts, Vector3[] loNormals, int[] loTris, CoordDir[] indexToCoord,  GrassTypeList grassTypes)
			{
				#if WDEBUG
				if (!ThreadWorker.multithreading) Profiler.BeginSample("Calculate Grass Mesh");
				#endif
				
				//loading grass meshes
				//for (int t=0; t<grassTypes.array.Length; t++)
				//	grassTypes.array[t].LoadMeshes();

				//finding per-index grass mesh and total number of verts and tris
				MeshWrapper[] perindexBushes = new MeshWrapper[indexToCoord.Length];
				byte[] perindexTypes = new byte[indexToCoord.Length];
				float[] perindexRandom = new float[indexToCoord.Length];

				int numVerts = 0; int numTris = 0;
				for (int i=0; i<indexToCoord.Length; i++)
				{
					CoordDir coord = indexToCoord[i];

					//skip blocks where grass does not grow
					//TODO: got to get block type somehow
					//TODO: use top type

					//getting grass type
					int typeNum = grassMatrix[coord.x, coord.z];
					if (typeNum == Data.emptyByte || typeNum >= grassTypes.array.Length) continue;
					perindexTypes[i] = (byte)typeNum;
					GrassType type = grassTypes.array[typeNum];
					if (type.meshes==null || type.meshes.Length==0) continue;

					//skip blocks with non-top level
					if (type.onlyTopLevel && coord.y < topLevels[coord.x,coord.z]-1) continue; //could above getting grass type - it will be faster
					
					//growing grass only on faces whose normal is facing up
					int normalAnum = loTris[i*2*3]; int normalBnum = loTris[i*2*3+1]; int normalCnum = loTris[(i*2+1)*3]; int normalDnum = loTris[i*2*3+2];
					if (loNormals[normalAnum].y < type.incline || loNormals[normalBnum].y < type.incline || loNormals[normalCnum].y < type.incline || loNormals[normalDnum].y < type.incline) continue;

					//selecting grass at "random"
					MeshWrapper[] bushes = type.meshes;
					float random = 1f*(coord.x%bushes.Length + coord.z%bushes.Length) / (bushes.Length + bushes.Length);
					if (random < 0) random = 1+random; //dealing negative coordinates
					MeshWrapper bush = bushes[(int)(random*bushes.Length)]; //TODO test how even is probability 

					perindexBushes[i] = bush;
					perindexRandom[i] = random;
					numVerts += bush.verts.Length;
					numTris += bush.tris.Length;
				}

				//creating grass wrapper
				MeshWrapper grass = new MeshWrapper();
				grass.verts = new Vector3[numVerts];
				grass.normals = new Vector3[numVerts];
				grass.tangents = new Vector4[numVerts];
				grass.uv = new Vector2[numVerts];
				grass.uv2 = new Vector2[numVerts]; //wind
				grass.uv4 = new Vector2[numVerts]; //x is ambient, y is type
				grass.tris = new int[numTris];

				//filling grass wraper
				for (int i=0; i<perindexBushes.Length; i++)
				{
					MeshWrapper bush = perindexBushes[i];
					if (bush == null) continue;

					byte typeNum = perindexTypes[i]; //0-based, as in grassTypes

					//CoordDir coord = indexToCoord[i];

					//Vector3 offset = new Vector3(coord.x-grassMatrix.rect.offset.x+0.5f, coord.y+0.5f, coord.z-grassMatrix.rect.offset.z+0.5f);
					
					//finding face corners
					Vector3 cornerA = loVerts[ loTris[i*2*3] ];
					Vector3 cornerB = loVerts[ loTris[i*2*3+1] ];
					Vector3 cornerC = loVerts[ loTris[(i*2+1)*3] ];
					Vector3 cornerD = loVerts[ loTris[i*2*3+2] ];

					//swapping corners to make them axis aligned
					float centerX = (cornerA.x + cornerB.x + cornerC.x + cornerD.x) / 4;
					float centerZ = (cornerA.z + cornerB.z + cornerC.z + cornerD.z) / 4;

					if (cornerA.x < centerX && cornerA.z < centerZ)
						{ Vector3 temp = cornerC; cornerC = cornerD; cornerD = cornerA; cornerA = cornerB; cornerB = temp; }
					if (cornerB.x < centerX && cornerB.z < centerZ) //according to the tests should not happen
						{ Vector3 temp = cornerB; cornerB = cornerD; cornerD = temp; temp = cornerA; cornerA = cornerC; cornerC = temp; }
					if (cornerC.x < centerX && cornerC.z < centerZ)
						{ Vector3 temp = cornerA; cornerA = cornerD; cornerD = cornerC; cornerC = cornerB; cornerB = temp; }
					//min point should be cornerD

					//face normal
					Vector3 normalA = loNormals[ loTris[i*2*3] ];
					Vector3 normalB = loNormals[ loTris[i*2*3+1] ];
					Vector3 normalC = loNormals[ loTris[(i*2+1)*3] ];
					Vector3 normalD = loNormals[ loTris[i*2*3+2] ];

					//randoms
					float randomSize = 1 + (perindexRandom[i]*2-1)*grassTypes.array[typeNum].uniformSizeRandom;
					float randomHeight = 1 + (perindexRandom[i]*2-1)*grassTypes.array[typeNum].heightRandom;

					//appending grass to wrapper
					int prevVertCounter = grass.vertCounter;
					grass.Append(bush, size:randomSize, height:randomHeight);

					//adjusting verts and normals
					for (int v=0; v<bush.verts.Length; v++)
					{
						//inclining grass mesh according corner positions
						float xPercent = grass.verts[prevVertCounter+v].x + 0.5f; //bush.verts[v].x + 0.5f;
						float zPercent = grass.verts[prevVertCounter+v].z + 0.5f; //bush.verts[v].z + 0.5f;
						float yHeight = grass.verts[prevVertCounter+v].y;

//Vector3 center = (cornerA+cornerB+cornerC+cornerD)/4;
//grass.verts[prevVertCounter+v] = grass.verts[prevVertCounter+v] + (cornerA+cornerB+cornerC+cornerD)/4;

						//creating new Vector3 (including creating with operators) is very slow

						//Vector3 vertX1 = cornerB*xPercent + cornerA*(1-xPercent);
						//Vector3 vertX2 = cornerC*xPercent + cornerD*(1-xPercent);
						//Vector3 vert =  vertX1*zPercent + vertX2*(1-zPercent);
						//vert.y += bush.verts[v].y;

						float vAx = cornerB.x*xPercent + cornerA.x*(1-xPercent); float vAy = cornerB.y*xPercent + cornerA.y*(1-xPercent); float vAz = cornerB.z*xPercent + cornerA.z*(1-xPercent);
						float vBx = cornerC.x*xPercent + cornerD.x*(1-xPercent); float vBy = cornerC.y*xPercent + cornerD.y*(1-xPercent); float vBz = cornerC.z*xPercent + cornerD.z*(1-xPercent);
						float vx = vAx*zPercent + vBx*(1-zPercent); float vy = vAy*zPercent + vBy*(1-zPercent); float vz = vAz*zPercent + vBz*(1-zPercent);

						//grass.verts[prevVertCounter+v] = new Vector3(vx, vy+bush.verts[v].y, vz);
						Vector3 var1 = new Vector3(vx, vy+yHeight, vz);
						Vector3 var2 = grass.verts[prevVertCounter+v] + (cornerA+cornerB+cornerC+cornerD)/4;
						float percent = 1f;
						grass.verts[prevVertCounter+v] = var1*percent + var2*(1-percent);
						

						//normals
						if (grassTypes.array[typeNum].normalsFromTerrain)// && yHeight < 0.0001f)
						{
							//Vector3 normX1 = normalB*xPercent + normalA*(1-xPercent);
							//Vector3 normX2 = normalC*xPercent + normalD*(1-xPercent);
							//grass.normals[prevVertCounter+v] =  normX1*zPercent + normX2*(1-zPercent);


							float nAx = normalB.x*xPercent + normalA.x*(1-xPercent); float nAy = normalB.y*xPercent + normalA.y*(1-xPercent); float nAz = normalB.z*xPercent + normalA.z*(1-xPercent);
							float nBx = normalC.x*xPercent + normalD.x*(1-xPercent); float nBy = normalC.y*xPercent + normalD.y*(1-xPercent); float nBz = normalC.z*xPercent + normalD.z*(1-xPercent);
							float nx = nAx*zPercent + nBx*(1-zPercent); float ny = nAy*zPercent + nBy*(1-zPercent); float nz = nAz*zPercent + nBz*(1-zPercent);
							grass.normals[prevVertCounter+v] = new Vector3(nx, ny, nz);
						}
//						grass.normals[prevVertCounter+v] = new Vector3(0,1,0);
//						grass.tangents[prevVertCounter+v] = new Vector4(0,0,-1,-1);


						//type
						grass.uv4[prevVertCounter+v].y = typeNum;
					}
				}

				#if WDEBUG
				if (!ThreadWorker.multithreading) Profiler.EndSample();
				#endif

				return grass;
			}

		#endregion
	}
}
