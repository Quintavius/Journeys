using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Voxeland5
{
	[System.Serializable]
	public class PosTab //: ICloneable 
	{
//make child of Matrix2
//id is number in list

//2rect matrix
// - child of matrix
// - standard operators are map sized
// - worldRect rect, and GetWorldPoint fn

		[System.Serializable]
		public struct Pos
		{
			public float x;
			public float z;
			public float height;
			public float rotation;
			public float inclineX;
			public float inclineZ;
			public float size;
			public int type;
			public int id; //num to apply per-object random that does not depend of object coords. 0 if pos is null. Note: not unique because of Combine, Forest, etc
		}

		[System.Serializable]
		public struct Cell
		{
			public CoordRect rect;
			public Pos[] poses;
			public int count;

			public int GetPosNum (float x, float z)
			{
				float epsilon = Mathf.Epsilon;
				for (int i=0; i<count; i++)
					if (poses[i].x <= x+epsilon && poses[i].x >= x-epsilon &&
						poses[i].z <= z+epsilon && poses[i].z >= z-epsilon) return i;
				return -1;
			}
		}

		public readonly CoordRect rect;
		public Matrix2<Cell> cells;
		public readonly int resolution; //number of cells
		public readonly Coord cellSize;

		public int totalCount = 0;
		public int idCounter = 1; //always increases, not changes if pos removed


		public PosTab (CoordRect rect, int resolution)
		{
			//if rect%resolution!=0 the last cell size will be lower than usual

			this.resolution = resolution;
			this.rect = rect;

			cells = new Matrix2<Cell>(resolution, resolution);

			cellSize = new Coord( Mathf.CeilToInt(1f*rect.size.x/resolution), Mathf.CeilToInt(1f*rect.size.z/resolution) );

			for (int x=0; x<resolution; x++)
				for (int z=0; z<resolution; z++)
			{
				Cell cell = new Cell();
				cell.rect = new CoordRect(
					x*cellSize.x + rect.offset.x,
					z*cellSize.z + rect.offset.z,
					Mathf.Min(cellSize.x, Mathf.Max(0, rect.size.x - x*cellSize.x)),
					Mathf.Min(cellSize.z, Mathf.Max(0, rect.size.z - z*cellSize.z)) );
				cell.rect.offset.x = Mathf.Min(cell.rect.offset.x, rect.offset.x+rect.size.x);
				cell.rect.offset.z = Mathf.Min(cell.rect.offset.z, rect.offset.z+rect.size.z);
				cells[x,z] = cell;
			}
		}

		public PosTab Copy ()
		{
			PosTab copy = new PosTab(rect, resolution);
			for (int c=0; c<cells.array.Length; c++)
			{
				copy.cells.array[c].count = cells.array[c].count;
				copy.cells.array[c].rect = cells.array[c].rect;

				if (cells.array[c].poses == null) continue;
				copy.cells.array[c].poses = new Pos[cells.array[c].poses.Length];
				Array.Copy(cells.array[c].poses, copy.cells.array[c].poses, cells.array[c].poses.Length);
			}
			return copy;
		}

		private Coord GetCellCoord (float x, float z, bool throwExceptions=true)
		{
			int ix = (int)((x-rect.offset.x)/cellSize.x);
			int iz = (int)((z-rect.offset.z)/cellSize.z); //no need to process negative values

			if (throwExceptions && (ix > cells.rect.size.x || iz > cells.rect.size.z)) throw new Exception("Out of cells range " + ix + "," + iz);

			return new Coord(ix, iz);
		}

		private int GetCellNum (float x, float z, bool throwExceptions=true)
		{
			int ix = (int)((x-rect.offset.x)/cellSize.x);
			int iz = (int)((z-rect.offset.z)/cellSize.z); //no need to process negative values

			if (throwExceptions && (ix > cells.rect.size.x || iz > cells.rect.size.z)) throw new Exception("Out of cells range " + ix + "," + iz);

			int n = iz*cells.rect.size.x + ix;

			if (throwExceptions && (n < 0)) throw new Exception("Could not find object at coord " + x + "," + z);

			return n;

		}


		public void Add (PosTab tab)
		{
			foreach (Pos pos in tab.AllObjs())
				Add(pos);
		}


		public void Add (float x, float z)
		{
			Pos pos = new Pos() {x=x, z=z, id=0};
			Add(pos);
		}


		public void Add (Pos pos)
		{
			//checking in range
			bool inRange =  pos.x >= rect.offset.x &&
				pos.z >= rect.offset.z &&
				pos.x < rect.offset.x+rect.size.x &&
				pos.z < rect.offset.z+rect.size.z;
			//if (!inRange) throw new Exception("Pos out of range: " + pos.x + "," + pos.z + " rect:" + rect.ToString());
			if (!inRange) return;

			if (pos.id == 0) //if id not defined
			{
				pos.id = idCounter;
				idCounter++;
				if (idCounter > 2147000000) idCounter = 1;
			}
			else if (idCounter <= pos.id) idCounter = pos.id+1; //to avoid duplicate ids
			
			int n = GetCellNum(pos.x, pos.z);

			//creating poses array
			if (cells.array[n].poses == null) cells.array[n].poses = new Pos[1];
			
			//resizing poses array
			if (cells.array[n].poses.Length == cells.array[n].count)
			{
				Pos[] newPoses = new Pos[cells.array[n].count*4];
				Array.Copy(cells.array[n].poses, newPoses, cells.array[n].count);
				cells.array[n].poses = newPoses;
			}

			//adding to array
			cells.array[n].poses[ cells.array[n].count ] = pos;

			cells.array[n].count++;
			totalCount++;

			//checking that cell length is power of two
//			UnityEngine.Assertions.Assert.IsTrue( cells.array[n].poses.Length == Mathf.ClosestPowerOfTwo(cells.array[n].poses.Length),
//				"Non pot " + cells.array[n].poses.Length.ToString() );
		}

		

		public void Remove (int cellNum, int posNum)
		{
			//swapping given pos num with the last one
			cells.array[cellNum].poses[posNum] = cells.array[cellNum].poses[ cells.array[cellNum].count-1 ];
			cells.array[cellNum].poses[ cells.array[cellNum].count-1 ].id = 0;

			cells.array[cellNum].count--;
			totalCount--;

			//shrinking array length
			if (cells.array[cellNum].count == 0) cells.array[cellNum].poses = null;
			else if (cells.array[cellNum].count < cells.array[cellNum].poses.Length / 2)
			{
				Pos[] newPoses = new Pos[cells.array[cellNum].count];
				Array.Copy(cells.array[cellNum].poses, newPoses, cells.array[cellNum].count);
				cells.array[cellNum].poses = newPoses;
			}

			//checking that cell length is power of two
//			UnityEngine.Assertions.Assert.IsTrue( cells.array[cellNum].poses.Length == Mathf.ClosestPowerOfTwo(cells.array[cellNum].poses.Length),
//				"Non pot " + cells.array[cellNum].poses.Length.ToString() );
		}

		public void RemoveAt (float x, float z)
		{
			int c = GetCellNum(x,z);
			int n = cells.array[c].GetPosNum(x,z);
			
			if (n!=-1) Remove(c,n);
		}

		public void Move (int cellNum, int posNum, float newX, float newZ)
		///Moves pos to the new coordinates, preserving all other data
		{
			int newCellNum = GetCellNum(newX,newZ);

			//if moving withing same cell
			if (newCellNum == cellNum)
			{
				cells.array[cellNum].poses[posNum].x = newX;
				cells.array[cellNum].poses[posNum].z = newZ;

				bool inRange =  newX >= rect.offset.x &&
					newZ >= rect.offset.z &&
					newX < rect.offset.x+rect.size.x &&
					newZ < rect.offset.z+rect.size.z;
				if (!inRange) Remove(cellNum, posNum);
				//if (!inRange) throw new Exception("Pos out of range: " + newX + "," + newZ + " rect:" + rect.ToString());
			}

			//removing from other cell and adding to new
			else
			{
				Pos pos = cells.array[cellNum].poses[posNum];
				Remove(cellNum, posNum);

				pos.x = newX;
				pos.z = newZ;

				Add(pos);
			}
		}

		public void GetAndMove (float oldX, float oldZ, float newX, float newZ)
		{
			int c = GetCellNum(oldX,oldZ);
			int n = cells.array[c].GetPosNum(oldX,oldZ);
			
			if (n < 0) throw new Exception("Could not find object at coord " + oldX + "," + oldZ + " cell num:" + c);

			Move(c,n, newX, newZ);
		}

		public bool Exists (float x, float z)
		///Checks if there an object at specified coord. Mainly for test purpose
		{
			int c = GetCellNum(x,z, throwExceptions:false);
			if (c > cells.array.Length) return false;

			int n = cells.array[c].GetPosNum(x,z);

			return n >= 0;
		}

		public void Flush ()
		///Removes unused array tails, reducing PosTab size (up to 4 times)
		{
			for (int c=0; c<cells.array.Length; c++)
			{
				if (cells.array[c].poses == null) continue;
				
				if (cells.array[c].count == 0) cells.array[c].poses = null;

				if (cells.array[c].poses.Length > cells.array[c].count)
				{
					Pos[] newPoses = new Pos[cells.array[c].count];
					Array.Copy(cells.array[c].poses, newPoses, cells.array[c].count);
					cells.array[c].poses = newPoses;
				}
			}
		}


		public Pos Closest (float x, float z, float minDist=0, float maxDist=2147000000)
		///Finds the closest Pos to given x and z in all cells. Use minDist=epsilon to exclude self.
		{
			//alternative way: keep a bool array of process cells, and increase rect each iteration (skipping processed)

			float minDistSq = maxDist;
			Pos closestPos = new Pos() { id=0 };

			Coord center = GetCellCoord(x,z);

			//finding cell search limit - a _maximum_ distance from center to cells rect bounds
			int maxP = center.x>center.z? center.x : center.z;
			if (cells.rect.size.x-center.x > maxP) maxP = cells.rect.size.x-center.x; 
			if (cells.rect.size.z-center.z > maxP) maxP = cells.rect.size.z-center.z; 
			maxP++; //TODO: remove +1;

			//looking in perimeters
			for (int p=0; p<maxP; p++)
			{
				ClosestInPerimeter(ref minDistSq, ref closestPos, center, p, x,z,minDist,maxDist);

				//if closest found at least - checking 2 perimeters more
				if (closestPos.id != 0) 
				{
					ClosestInPerimeter(ref minDistSq, ref closestPos, center, p+1, x,z,minDist,maxDist);
					ClosestInPerimeter(ref minDistSq, ref closestPos, center, p+2, x,z,minDist,maxDist); //TODO: search in 2nd perimeter only when x and z are near cell bounds
					break;
				}
			}

			return closestPos;
		}

		private void ClosestInPerimeter (ref float minDistSq, ref Pos closestPos, Coord center, int perimSize, float x, float z, float minDist, float maxDist)
		///finds the closest in a rectangular (square) perimeter. Just a helper for Closest. PerDist is the perimeter size (distance from center).
		{
			if (perimSize == 0) //in current cell
			{
				Cell curCell = cells[center];
				for (int i=0; i<curCell.count; i++)
				{
					float curDistSq = (curCell.poses[i].x-x)*(curCell.poses[i].x-x) + (curCell.poses[i].z-z)*(curCell.poses[i].z-z);
					if (curDistSq<minDistSq && curDistSq>=minDist*minDist) { minDistSq=curDistSq; closestPos=curCell.poses[i]; }
				}
			}

			else //in perimeter
			{
				for (int s=0; s<perimSize; s++)
					foreach (Coord c in center.DistanceStep(s,perimSize))
				{
					//checking cell in range
					if (!(c.x >= cells.rect.offset.x && c.x < cells.rect.offset.x + cells.rect.size.x &&
			        c.z >= cells.rect.offset.z && c.z < cells.rect.offset.z + cells.rect.size.z)) continue;

					Cell curCell = cells[c];
					for (int i=0; i<curCell.count; i++)
					{
						float curDistSq = (curCell.poses[i].x-x)*(curCell.poses[i].x-x) + (curCell.poses[i].z-z)*(curCell.poses[i].z-z);
						if (curDistSq<minDistSq && curDistSq>=minDist*minDist) { minDistSq=curDistSq; closestPos=curCell.poses[i]; }
					}
				}
			}
		}

		public Pos ClosestDebug (float x, float z, float minDist=0, float maxDist=20000000000)
		///Finds closest iterating in all cells and objects. Hust fn to test Closest.
		{
			float minDistSq = maxDist;
			Pos closestPos = new Pos() { id=0 };

			for (int c=0; c<cells.array.Length; c++)
			{
				Cell curCell = cells.array[c];
				for (int i=0; i<curCell.count; i++)
				{
					float curDistSq = (curCell.poses[i].x-x)*(curCell.poses[i].x-x) + (curCell.poses[i].z-z)*(curCell.poses[i].z-z);
					if (curDistSq<minDistSq && curDistSq>=minDist*minDist) { minDistSq=curDistSq; closestPos=curCell.poses[i]; }
				}
			}

			return closestPos;
		}

		#region MapMagic functions

			public static PosTab Combine (params PosTab[] posTabs)
			//TODO: combine ids are not unique
			{
				if (posTabs.Length==0) return null;

				PosTab any = ArrayTools.Any(posTabs);
				if (any == null) return null;
				PosTab result = new PosTab(any.rect, any.resolution);

				for (int i=0; i<posTabs.Length; i++)
				{
					PosTab posTab = posTabs[i];
					if (posTab == null) continue;

					for (int c=0; c<posTab.cells.array.Length; c++)
					{
						Cell cell = posTab.cells.array[c];
						for (int p=0; p<cell.count; p++)
							result.Add(cell.poses[p]);
					}
				}

				return result;
			}


		#endregion


		public IEnumerable<Pos> AllObjs()
		{
			for (int c=0; c<cells.array.Length; c++)
			{
				Cell cell = cells.array[c];
				for (int i=0; i<cell.count; i++)
					yield return cell.poses[i];
			}
		}

		public IEnumerable<int> CellNumsInRect(Vector2 min, Vector2 max, bool inCenter=true)
		{
			int minX = (int)((min.x-rect.offset.x) / cellSize.x);
			int minY = (int)((min.y-rect.offset.z) / cellSize.z);
			int maxX = (int)((max.x-rect.offset.x) / cellSize.x);
			int maxY = (int)((max.y-rect.offset.z) / cellSize.z); 

			minX = Mathf.Max(0, minX); minY = Mathf.Max(0, minY);
			maxX = Mathf.Min(resolution-1, maxX); maxY = Mathf.Min(resolution-1, maxY);  

			//processing all the rect
			if (inCenter)
				for (int x=minX; x<=maxX; x++)
					for (int y=minY; y<=maxY; y++)
						yield return y*resolution + x;

			//borders only
			else 
			{
				for (int x=minX; x<=maxX; x++) { yield return minY*resolution + x; yield return maxY*resolution + x; }
				for (int y=minY; y<=maxY; y++) { yield return y*resolution + minX; yield return y*resolution + maxX; }
			}
		}

		public void RemoveObjsInRange(float posX, float posZ, float range) //TODO: test, then replace CellNumsInRect. Or better replace with callback
		{
			Rect rect = new Rect(posX-range, posZ-range, range*2, range*2);
			rect = CoordinatesExtensions.Intersect(rect, this.rect);

			foreach (int c in CellNumsInRect(rect.min, rect.max))
			{
				for (int p=cells.array[c].count-1; p>=0; p--)
				{
					float distSq = (cells.array[c].poses[p].x-posX)*(cells.array[c].poses[p].x-posX) + (cells.array[c].poses[p].z-posZ)*(cells.array[c].poses[p].z-posZ);
					if (distSq < range*range) Remove(c,p);
				}
			}
		}

		public bool IsAnyObjInRange(float posX, float posZ, float range) 
		{
			Vector2 min = new Vector2(posX-range, posZ-range);
			Vector2 max = new Vector2(posX+range, posZ+range);

			foreach (int c in CellNumsInRect(min,max))
			{
				for (int p=cells.array[c].count-1; p>=0; p--)
				{
					float distSq = (cells.array[c].poses[p].x-posX)*(cells.array[c].poses[p].x-posX) + (cells.array[c].poses[p].z-posZ)*(cells.array[c].poses[p].z-posZ);
					if (distSq < range*range) return true;
				}
			}
			return false;
		}

		public void DrawGizmo ()
		{
			Coord center = new Coord(3,3);

			for (int dist=0; dist<5; dist++)
			{
				Gizmos.color = new Color(1f*dist/5, 1-1f*dist/5, 0, 1); 
				for (int i=0; i<dist; i++)
					foreach (Coord c in center.DistanceStep(i,dist))
						if (cells.rect.CheckInRange(c)) cells[c].rect.DrawGizmo();
			}


			foreach (Coord c in center.DistanceArea(cells.rect))
			{
			//	Gizmos.color = new Color(1f*counter/cells.count, 1-1f*counter/cells.count, 0, 1); 
			//	cells[c].rect.DrawGizmo();
			//	counter++;
			}

			for (int x=0; x<resolution; x++)
				for (int z=0; z<resolution; z++)
			{
				//Gizmos.color = new Color(1f*c/cells.Length, 1-1f*c/cells.Length, 0, 1); 
				//cells[x,z].rect.DrawGizmo();
			}
		}
	}
}
