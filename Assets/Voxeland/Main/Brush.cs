using UnityEngine;
using System.Collections;

using Voxeland5;

namespace Voxeland5
{
	[System.Serializable]
	public class Brush
	{
		public int extent = 3;
		public int maxExtent = 10; //for gui purpose
		public enum Form {single, blob, volume, stamp};
		public Form form;
		public bool round;
		
		public Matrix3<bool> stamp;
		public bool getStamp; //TODO: getStamp should be an edit mode
		public CoordDir getStampMin = new CoordDir(-1,-1,-1);
		public CoordDir getStampMax = new CoordDir(1,1,1);

		//public enum EditMode {none, standard, dig, add, replace, smooth}; //standard mode is similar to add one, except the preliminary switch to opposite in add


		public CoordDir Min (CoordDir pos, Voxeland.EditMode mode=Voxeland.EditMode.none)
		{
			switch (form)
			{
				//case Form.single: if (mode==Voxeland.EditMode.add) pos = pos.opposite; break; //shifting pos to opposite if single add mode //in Alter
				case Form.blob: pos.x -= extent+1; pos.y -= extent+1; pos.z -= extent+1; break; //getting 1-block boundary for opposites
				case Form.volume: pos.x -= extent; pos.y -= extent; pos.z -= extent; break;
				case Form.stamp:
					if (stamp==null) return pos; 
					if (getStamp) pos += getStampMin; 
					else pos+=stamp.cube.offset;
					break;
			}

			return pos;
		}

		public CoordDir Max (CoordDir pos, Voxeland.EditMode mode=Voxeland.EditMode.none) //note taht Max is never reached
		{
			switch (form)
			{
				//case Form.single: if (mode==Voxeland.EditMode.add) pos = pos.opposite; break; //shifting pos to opposite if single add mode
				case Form.blob: pos.x += extent+1; pos.y += extent+1; pos.z += extent+1; break; //getting 1-block boundary on opposite
				case Form.volume: pos.x += extent; pos.y += extent; pos.z += extent; break;
				case Form.stamp:
					if (stamp==null) return pos+1; 
					if (getStamp) pos += getStampMax;
					else pos+=stamp.cube.offset+stamp.cube.size;
					break;
			}

			pos.x++; pos.y++; pos.z++;
			return pos;
		}

		public void Process (CoordDir pos, Matrix3<byte> matrix, Voxeland.EditMode mode, byte type) { Process(pos, matrix, mode, form,type); }
		public void Process (CoordDir pos, Matrix3<byte> matrix, Voxeland.EditMode mode, Form formOverride, byte type) //overriding brush form to place objects and grass
		{
			//blurring matrix in case of blur mode
			Matrix3<float> blurredExist = null;
			if (mode == Voxeland.EditMode.smooth)
			{
				blurredExist = new Matrix3<float>(matrix.cube);
				for (int i=0; i<blurredExist.array.Length; i++) 
				{
					if (matrix.array[i]!=Data.emptyByte) blurredExist.array[i] = 1;
					else blurredExist.array[i] = 0;
				}
				BlurExistMatrix(blurredExist);
			}

			//single brush
			if (formOverride==Form.single)
			{
				switch (mode)
				{
					case Voxeland.EditMode.add: case Voxeland.EditMode.replace: matrix[pos.x, pos.y, pos.z] = type; break; //already switched add to opposite, so add==replace
					case Voxeland.EditMode.dig: matrix[pos.x, pos.y, pos.z] = Data.emptyByte; break;
				}
			}
			
			//blob brush
			if (formOverride == Form.blob)
			{
				CoordDir[] neigCoords = ChunkMesh.NeighbourCoordinates(pos, matrix, extent, round:round);

				for (int i=0; i<neigCoords.Length; i++)
				{
					int x = neigCoords[i].x; int y = neigCoords[i].y; int z = neigCoords[i].z; 
					CoordDir neigOpposite = neigCoords[i].opposite;
					int ox = neigOpposite.x; int oy = neigOpposite.y; int oz = neigOpposite.z; 
					
					switch (mode)
					{
						case Voxeland.EditMode.add: matrix[ox,oy,oz] = type; break;
						case Voxeland.EditMode.replace: matrix[x,y,z] = type; break;
						case Voxeland.EditMode.dig: matrix[x,y,z] = Data.emptyByte; break;
						case Voxeland.EditMode.smooth:
							if (blurredExist[ox,oy,oz]>0.5f && matrix[ox,oy,oz]==Data.emptyByte) matrix[ox,oy,oz] = ClosestExistingType(matrix, ox,oy,oz);  //TODO: exist check
							if (blurredExist[x,y,z]<0.5f && matrix[x,y,z]!=Data.emptyByte) matrix[x,y,z] = Data.emptyByte;
							break;
					}
				}
			}

			//volume brush
			else if (formOverride == Form.volume)
			{
				CoordDir min = matrix.cube.Min; CoordDir max = matrix.cube.Max;	

				for (int x=min.x; x<max.x; x++)
					for (int y=min.y; y<max.y; y++)
						for (int z=min.z; z<max.z; z++)
				{
					//ignoring out-of-sphere
					if (round)
					{
						int dx = x-pos.x; int dy = y-pos.y; int dz = z-pos.z;
						if (dx*dx + dy*dy + dz*dz > (extent+0.5f)*(extent+0.5f)) continue;
					}

					//setting block
					int i = matrix.cube.GetPos(x,y,z);
					switch (mode)
					{
						case Voxeland.EditMode.add: matrix.array[i] = type; break;
						case Voxeland.EditMode.dig: matrix.array[i] = Data.emptyByte; break;
						case Voxeland.EditMode.replace: if (matrix.array[i] != Data.emptyByte) matrix.array[i] = type; break; //TODO: exists check
						case Voxeland.EditMode.smooth: 
							if (blurredExist.array[i]>0.5f && (matrix.array[i]==Data.emptyByte || matrix.array[i]>=Data.constructorByte)) matrix.array[i] = ClosestExistingType(matrix, x,y,z); //if blured exists but matrix empty adding closest //TODO: exist check
							if (blurredExist.array[i]<0.5f && (matrix.array[i]!=Data.emptyByte && matrix.array[i]<Data.constructorByte) ) matrix.array[i] = Data.emptyByte; //if blured empty but matrix exists
							break;
					}
				}
			}

			else if (formOverride == Form.stamp)
			{
				if (getStamp)
				{
					stamp = new Matrix3<bool>(getStampMin, matrix.cube.size);
					
					for (int x=0; x<matrix.cube.size.x; x++)
						for (int y=0; y<matrix.cube.size.y; y++)
							for (int z=0; z<matrix.cube.size.z; z++)
					{
						if (matrix[x+matrix.cube.offset.x, y+matrix.cube.offset.y, z+matrix.cube.offset.z] != Data.emptyByte) stamp[x+stamp.cube.offset.x, y+stamp.cube.offset.y, z+stamp.cube.offset.z] = true; //TODO: exists check
					}
				}
				else if (stamp!=null)
				{
					for (int x=0; x<stamp.cube.size.x; x++)
						for (int y=0; y<stamp.cube.size.y; y++)
							for (int z=0; z<stamp.cube.size.z; z++)
					{
						CoordDir s = new CoordDir(x,y,z) + stamp.cube.offset;
						CoordDir m = new CoordDir(x,y,z) + matrix.cube.offset;

						switch (mode)
						{
							case Voxeland.EditMode.add: if (stamp[s]) matrix[m] = type; break;
							case Voxeland.EditMode.dig: if (stamp[s]) matrix[m] = Data.emptyByte; break;
							case Voxeland.EditMode.replace:  if (matrix[m]!=0 && stamp[s]) matrix[m] = type; break; //TODO: exists check
							case Voxeland.EditMode.smooth: 
								if (stamp[s])
								{
									if (blurredExist[m]>0.5f && (matrix[m]==Data.emptyByte || matrix[m]>=Data.constructorByte)) matrix[m] = ClosestExistingType(matrix, x,y,z); //if blured exists but matrix empty adding closest //TODO: exist check
									if (blurredExist[m]<0.5f && (matrix[m]!=Data.emptyByte && matrix[m]<Data.constructorByte) ) matrix[m] = Data.emptyByte; //if blured empty but matrix exists
								}
								break;
						}
					}
				}
			}
		}

		public void Process (CoordDir[] poses, Matrix3<byte> matrix, Voxeland.EditMode mode, byte type)
		{
			for (int i=0; i<poses.Length; i++)
				Process (poses[i], matrix, mode, type);
		}


		public void BlurExistMatrix (Matrix3<float> src, int iterations=10) //src is an exist (bool) array, 0 is empty, 1 is exists
		{
			Matrix3<float> dst = src.Copy(); //to fill matrix borders

			CoordDir start = src.cube.offset + 1; //new CoordDir(src.offsetX+1, src.offsetY+1, src.offsetZ+1);
			CoordDir end = src.cube.offset+src.cube.size - 1;

			for (int i=0; i<iterations; i++)
			{
				for (int x=start.x; x<end.x; x++)
					for (int y=start.y; y<end.y; y++)
						for (int z=start.z; z<end.z; z++)
				{
					float val = src[x,y,z] * 4;  
					val += src[x+1,y,z];
					val += src[x-1,y,z];
					val += src[x,y+1,z];
					val += src[x,y-1,z];
					val += src[x,y,z+1];
					val += src[x,y,z-1];

					dst[x,y,z] = val/10;
				}

				for (int a=0; a<src.array.Length; a++) src.array[a] = dst.array[a]; //copy dst to src for new iteration
			}
		}

		public byte ClosestExistingType (Matrix3<byte> matrix, int x, int y, int z)
		{
			//int x = coord.x; int y = coord.y; int z = coord.z;

			//TODO: exists check
			byte val = matrix[x,y,z]; if (val!=Data.emptyByte) return val;
			if (y > matrix.cube.offset.y)						 { val = matrix[x, y-1, z];		if (val!=Data.emptyByte) return val; }
			if (y < matrix.cube.offset.y+matrix.cube.size.y-1)	 { val = matrix[x, y+1, z];		if (val!=Data.emptyByte) return val; }
			if (x > matrix.cube.offset.x)						 { val = matrix[x-1, y, z];		if (val!=Data.emptyByte) return val; }
			if (x < matrix.cube.offset.x+matrix.cube.size.x-1)	 { val = matrix[x+1, y, z];		if (val!=Data.emptyByte) return val; }
			if (z > matrix.cube.offset.z)						 { val = matrix[x, y, z-1];		if (val!=Data.emptyByte) return val; }
			if (z < matrix.cube.offset.z+matrix.cube.size.z-1)	 { val = matrix[x, y, z+1];		if (val!=Data.emptyByte) return val; }

			if (y > matrix.cube.offset.y) return ClosestExistingType (matrix, x, y-1, z);
			else return 0;
		}
	}
}