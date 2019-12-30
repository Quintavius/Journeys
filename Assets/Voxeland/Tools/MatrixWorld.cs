using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Voxeland5
{
	[System.Serializable]
	public class MatrixWorld : Matrix 
	{
		public CoordRect worldRect;

		public MatrixWorld (CoordRect rect, CoordRect worldRect, float[] array=null)
		{
			//standard matrix initialization
			this.rect = rect;
			count = rect.size.x*rect.size.z;

			if (array != null && array.Length<count) Debug.Log("Array length: " + array.Length + " is lower then matrix capacity: " + count);
			if (array != null && array.Length>=count) this.array = array;
			else this.array = new float[count];

			//world rect
			this.worldRect = worldRect;
		}

		public MatrixWorld (int resolution, CoordRect worldRect, float[] array=null)
		{
			//converting world rect to map rect
			CoordRect mapRect = new CoordRect( 
				Mathf.RoundToInt( worldRect.offset.x / (1f * worldRect.size.x / resolution)  ),
				Mathf.RoundToInt( worldRect.offset.z / (1f * worldRect.size.z / resolution) ),
				resolution,
				resolution );

			//standard matrix initialization
			this.rect = mapRect;
			count = rect.size.x*rect.size.z;

			if (array != null && array.Length<count) Debug.Log("Array length: " + array.Length + " is lower then matrix capacity: " + count);
			if (array != null && array.Length>=count) this.array = array;
			else this.array = new float[count];

			//world rect
			this.worldRect = worldRect;
		}

		public MatrixWorld (Matrix matrix, CoordRect worldRect)
		{
			//standard matrix initialization
			rect = matrix.rect;
			count = rect.size.x*rect.size.z;
			array = matrix.array;

			//world rect
			this.worldRect = worldRect;
		}


		public override object Clone () //IClonable
		{ 
			MatrixWorld result = new MatrixWorld(rect, worldRect);
			
			//copy params
			result.pos = pos;
			result.count = count;
			
			//copy array
			if (result.array.Length != array.Length) result.array = new float[array.Length];
			Array.Copy(array, result.array, array.Length);

			return result;
		}


		public float GetWorldValue (float x, float z)
		{
			//finding relative percent
			float percentX = (x - worldRect.offset.x) / worldRect.size.x;
			float percentZ = (z - worldRect.offset.z) / worldRect.size.z;

			//get map coordinates
			float mapX = percentX*rect.size.x + rect.offset.x;
			float mapZ = percentZ*rect.size.z + rect.offset.z;

			//flooring map values (values should be floored, not rounded since height pixel on terrain has it's own dimensions)
			int ix = (int)mapX; if (mapX<0) ix--; if (ix==rect.offset.x+rect.size.x) ix--;
			int iz = (int)mapZ; if (mapZ<0) iz--; if (iz==rect.offset.z+rect.size.z) iz--;

			UnityEngine.Assertions.Assert.IsTrue(ix>=rect.offset.x && iz>=rect.offset.z && ix<rect.offset.x+rect.size.x && iz<rect.offset.z+rect.size.z);
			
			return array[(iz-rect.offset.z)*rect.size.x + ix - rect.offset.x]; 
		}

		public void SetWorldValue (float x, float z, float val)
		{
			//finding relative percent
			float percentX = (x - worldRect.offset.x) / worldRect.size.x;
			float percentZ = (z - worldRect.offset.z) / worldRect.size.z;

			//get map coordinates
			float mapX = percentX*rect.size.x + rect.offset.x;
			float mapZ = percentZ*rect.size.z + rect.offset.z;

			//flooring map values (values should be floored, not rounded since height pixel on terrain has it's own dimensions)
			int ix = (int)mapX; if (mapX<0) ix--; if (ix==rect.offset.x+rect.size.x) ix--;
			int iz = (int)mapZ; if (mapZ<0) iz--; if (iz==rect.offset.z+rect.size.z) iz--;

			UnityEngine.Assertions.Assert.IsTrue(ix>=rect.offset.x && iz>=rect.offset.z && ix<rect.offset.x+rect.size.x && iz<rect.offset.z+rect.size.z);
			
			array[(iz-rect.offset.z)*rect.size.x + ix - rect.offset.x] = val; 
		}

		public float GetWorldInterpolatedValue (float x, float z)
		{
			//finding relative percent
			float percentX = (x - worldRect.offset.x) / worldRect.size.x;
			float percentZ = (z - worldRect.offset.z) / worldRect.size.z;

			//get map coordinates
			float mapX = percentX*rect.size.x + rect.offset.x;
			float mapZ = percentZ*rect.size.z + rect.offset.z;

			//return GetInterpolated(mapX, mapZ); //copy

			//neig coords
			int px = (int)mapX; if (mapX<0) px--; //because (int)-2.5 gives -2, should be -3 
			int nx = px+1;

			int pz = (int)mapZ; if (mapZ<0) pz--; 
			int nz = pz+1;

			//local coordinates (without offset)
			int lpx = px-rect.offset.x; int lnx = nx-rect.offset.x;
			int lpz = pz-rect.offset.z; int lnz = nz-rect.offset.z;

			//clamping coordinates
			if (lpx<0) lpx=0; if (lpx>=rect.size.x) lpx=rect.size.x-1;
			if (lnx<0) lnx=0; if (lnx>=rect.size.x) lnx=rect.size.x-1;
			if (lpz<0) lpz=0; if (lpz>=rect.size.z) lpz=rect.size.z-1;
			if (lnz<0) lnz=0; if (lnz>=rect.size.z) lnz=rect.size.z-1;

			//reading values
			float val_pxpz = array[lpz*rect.size.x + lpx];
			float val_nxpz = array[lpz*rect.size.x + lnx]; //array[pos_fxfz + 1]; //do not use fast calculations as they are not bounds safe
			float val_pxnz = array[lnz*rect.size.x + lpx]; //array[pos_fxfz + rect.size.z];
			float val_nxnz = array[lnz*rect.size.x + lnx]; //array[pos_fxfz + rect.size.z + 1];

			float subPercentX = mapX-px;
			float subPercentZ = mapZ-pz;

			float val_fz = val_pxpz*(1-subPercentX) + val_nxpz*subPercentX;
			float val_cz = val_pxnz*(1-subPercentX) + val_nxnz*subPercentX;
			float val = val_fz*(1-subPercentZ) + val_cz*subPercentZ;

			return val;
		}

		public int WorldToPos (float x, float z)
		{
			//finding relative percent
			float percentX = (x - worldRect.offset.x) / worldRect.size.x;
			float percentZ = (z - worldRect.offset.z) / worldRect.size.z;

			//get map coordinates
			float mapX = percentX*rect.size.x + rect.offset.x;
			float mapZ = percentZ*rect.size.z + rect.offset.z;

			//flooring map values (values should be floored, not rounded since height pixel on terrain has it's own dimensions)
			int ix = (int)mapX; if (mapX<0) ix--; if (ix==rect.offset.x+rect.size.x) ix--;
			int iz = (int)mapZ; if (mapZ<0) iz--; if (iz==rect.offset.z+rect.size.z) iz--;

			UnityEngine.Assertions.Assert.IsTrue(ix>=rect.offset.x && iz>=rect.offset.z && ix<rect.offset.x+rect.size.x && iz<rect.offset.z+rect.size.z);
			
			return (iz-rect.offset.z)*rect.size.x + ix - rect.offset.x; 
		}

		public int WorldToMap (float worldX)
		{
			float percentX = (worldX - worldRect.offset.x) / worldRect.size.x;
			float mapX = percentX*rect.size.x + rect.offset.x;
			int ix = (int)(mapX); if (mapX<0) ix--; if (ix==rect.offset.x+rect.size.x) ix--;
			return ix;
		}

			public float MapToWorld (int mapX)
		{
			float percentX = (mapX+0.5f - rect.offset.x) / rect.size.x;  //taking the center of the pixel
			float worldX = percentX*worldRect.size.x + worldRect.offset.x;
			return worldX;
		}

		/*public Coord WorldToCoord (float x, float z)
		{
			//finding relative percent
			float percentX = (x - worldRect.offset.x) / worldRect.size.x;
			float percentZ = (z - worldRect.offset.z) / worldRect.size.z;

			//get map coordinates
			float mapX = percentX*rect.size.x + rect.offset.x;
			float mapZ = percentZ*rect.size.z + rect.offset.z;

			//rounding map values
			int ix = (int)(mapX+0.5f); if (mapX<1) ix--; if (ix==rect.offset.x+rect.size.x) ix--;
			int iz = (int)(mapZ+0.5f); if (mapZ<1) iz--; if (iz==rect.offset.z+rect.size.z) iz--;

			//UnityEngine.Assertions.Assert.IsTrue(ix>=rect.offset.x && iz>=rect.offset.z && ix<rect.offset.x+rect.size.x && iz<rect.offset.z+rect.size.z);
			//assertions failed when using rect - it should have out-of-range points

			return new Coord(ix, iz);
		}


		public CoordRect WorldToRect (float offsetX, float offsetZ, float sizeX, float sizeZ)
		{
			Coord offset = WorldToCoord(offsetX, offsetZ);
			Coord size = WorldToCoord(offsetX+sizeX, offsetZ+sizeZ) - offset;

			return new CoordRect(offset, size);
		}*/
	}
}
