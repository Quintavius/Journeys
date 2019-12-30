using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace Voxeland5 
{
	public static class ErosionCpp
	{
			public struct Cross
			{
				//public float xz; public float z; public float Xz;
				//public float x; public float c; public float X;
				//public float xZ; public float Z; public float XZ;

				public float[] vals;

				public static MooreCross Zero () { return new MooreCross() { vals = new float[5] }; }

				public Cross (float[] m, int i, int sizeX)
				{
					//xz = m[i-1-sizeX];	z = m[i-sizeX];		Xz = m[i+1-sizeX];
					//x = m[i-1];			c = m[i];			X = m[i+1];
					//xZ = m[i-1+sizeX];	Z = m[i+sizeX];		XZ = m[i+1+sizeX]; 

					vals = new float[]
					{
										m[i-sizeX],		
						m[i-1],			m[i],			m[i+1],
										m[i+sizeX],	
					};
				}

				public Cross (float val) { vals = new float[5]; for (int i=0; i<5; i++) vals[i] = val; }

				public void SetToMatrix (float[] m, int i, int sizeX)
				{
												m[i-sizeX] = vals[0];
						m[i-1] = vals[1];		m[i] = vals[2];			m[i+1] = vals[3];
												m[i+sizeX] = vals[4];	
				}

				public void AddToMatrix (float[] m, int i, int sizeX)
				{
												m[i-sizeX] += vals[0];
						m[i-1] += vals[1];		m[i] += vals[2];			m[i+1] += vals[3];
												m[i+sizeX] += vals[4];	
				}

				public static Cross operator + (Cross c1, Cross c2) { for (int i=0; i<5; i++) c1.vals[i] += c2.vals[i]; return c1; }
				public static Cross operator + (Cross c1, float f) { for (int i=0; i<5; i++) c1.vals[i] += f; return c1; }
				public static Cross operator - (float f, Cross c) { for (int i=0; i<5; i++) c.vals[i] = f-c.vals[i]; return c; }
				public static Cross operator - (Cross c1, float f) { for (int i=0; i<5; i++) c1.vals[i] -=f; return c1; }
				public static Cross operator - (Cross c1, Cross c2) { for (int i=0; i<5; i++) c1.vals[i] -= c2.vals[i]; return c1; }
				public static Cross operator / (Cross c, float f) { for (int i=0; i<5; i++) c.vals[i] = c.vals[i]/f; return c; }
				public static Cross operator * (Cross c, float f) { for (int i=0; i<5; i++) c.vals[i] = c.vals[i]*f; return c; }
				public static Cross operator * (float f, Cross c) { for (int i=0; i<5; i++) c.vals[i] = c.vals[i]*f; return c; }

				public float Min () { float min=2000000000; for (int i=0; i<5; i++) if (vals[i]<min) min = vals[i]; return min; }
				public float MinSides () { float min=2000000000; for (int i=0; i<5; i++) { if (i==2) continue; if (vals[i]<min) min = vals[i];} return min; }
				public float MaxSides () { float max=-2000000000; for (int i=0; i<5; i++) { if (i==2) continue; if (vals[i]>max) max = vals[i];} return max; }
				public float Sum () { float sum=0; for (int i=0; i<5; i++) sum += vals[i]; return sum; }
				public float SumSides () { float sum=0; for (int i=0; i<5; i++) { if (i==2) continue; sum += vals[i];} return sum; }
				public float Avg () { return Sum() / 5f; }
				public float AvgSides () { return  SumSides() / 4f; }

				public static Cross ClampMax (Cross c, float f) { for (int i=0; i<5; i++) c.vals[i] = Mathf.Max(f,c.vals[i]); return c; }
				public static Cross ClampMin (Cross c, float f) { for (int i=0; i<5; i++) c.vals[i] = Mathf.Min(f,c.vals[i]); return c; }

				public static Cross Pour (Cross height, float liquid)
				{
					//if (liquid < 0.0000001f) return new Cross(0);

					//initial avg scatter
					float sum = height.Sum() + liquid;
					float avg = sum / 5;

					Cross pour = new Cross(avg);
					pour -= height;
					pour = ClampMax(pour, 0);
					//now liquids sum is larger than original

					//lowering all of the liquid cells
					int liquidCellsCount = 0;
					float currentLiquidSum = 0;
					for (int i=0; i<5; i++)
					{
						float val = pour.vals[i];
						if (val > 0.0001f) liquidCellsCount++;
						currentLiquidSum += val;
					}
					if (liquidCellsCount == 0) return pour; //should not happen
					float lowerAmount = (pour.Sum() - liquid) / liquidCellsCount;
					pour = pour - lowerAmount;
					pour = ClampMax(pour, 0);

					//in most cases now the delta is 0, but sometimes it's still needs to be adjusted
					if (Mathf.Abs(pour.Sum() - liquid) > 0.00001f)
					{
						if (Mathf.Abs(pour.Sum()) < 0.000001f) return new Cross(0); //this is 100% needed
						float factor = liquid / pour.Sum();
						pour *= factor;
					}

					return pour;
				}
			}
			public struct MooreCross
			{
				//public float xz; public float z; public float Xz;
				//public float x; public float c; public float X;
				//public float xZ; public float Z; public float XZ;

				public float[] vals;

				public static MooreCross Zero () { return new MooreCross() { vals = new float[9] }; }

				public MooreCross (float[] m, int i, int sizeX)
				{
					//xz = m[i-1-sizeX];	z = m[i-sizeX];		Xz = m[i+1-sizeX];
					//x = m[i-1];			c = m[i];			X = m[i+1];
					//xZ = m[i-1+sizeX];	Z = m[i+sizeX];		XZ = m[i+1+sizeX]; 

					vals = new float[]
					{
						m[i-1-sizeX],	m[i-sizeX],		m[i+1-sizeX],
						m[i-1],			m[i],			m[i+1],
						m[i-1+sizeX],	m[i+sizeX],		m[i+1+sizeX]
					};
				}

				public void AddToMatrix (float[] m, int i, int sizeX)
				{
						m[i-1-sizeX] += vals[0];	m[i-sizeX] += vals[1];		m[i+1-sizeX] += vals[2];
						m[i-1] += vals[3];			m[i] += vals[4];			m[i+1] += vals[5];
						m[i-1+sizeX] += vals[6];	m[i+sizeX] += vals[7];		m[i+1+sizeX] += vals[8];
				}

				public void SetToMatrix (float[] m, int i, int sizeX)
				{
						m[i-1-sizeX] = vals[0];	m[i-sizeX] = vals[1];		m[i+1-sizeX] = vals[2];
						m[i-1] = vals[3];			m[i] = vals[4];			m[i+1] = vals[5];
						m[i-1+sizeX] = vals[6];	m[i+sizeX] = vals[7];		m[i+1+sizeX] = vals[8];
				}

				public static MooreCross operator + (MooreCross c1, MooreCross c2) { for (int i=0; i<9; i++) c1.vals[i] += c2.vals[i]; return c1; }
				public static MooreCross operator - (float f, MooreCross c) { for (int i=0; i<9; i++) c.vals[i] = f-c.vals[i]; return c; }
				public static MooreCross operator / (MooreCross c, float f) { for (int i=0; i<9; i++) c.vals[i] = c.vals[i]/f; return c; }
				public static MooreCross operator * (MooreCross c, float f) { for (int i=0; i<9; i++) c.vals[i] = c.vals[i]*f; return c; }
				public static MooreCross operator * (float f, MooreCross c) { for (int i=0; i<9; i++) c.vals[i] = c.vals[i]*f; return c; }

				public static MooreCross ClampMax (MooreCross c, float f) { for (int i=0; i<9; i++) c.vals[i] = Mathf.Max(f,c.vals[i]); return c; }
				public static MooreCross ClampMin (MooreCross c, float f) { for (int i=0; i<9; i++) c.vals[i] = Mathf.Min(f,c.vals[i]); return c; }
				
				public float Sum () { float sum=0; for (int i=0; i<9; i++) sum += vals[i]; return sum; }
			}


			public static void SetOrderArray (float[] refArray, int[] orderArray, int length)
			{
				for (int i=0; i<orderArray.Length; i++) orderArray[i] = i;
				float[] refHeights = new float[refArray.Length];
				Array.Copy(refArray, refHeights, length);
				Array.Sort(refHeights, orderArray);

			//	int[] orderCopy = new int[refArray.Length];
			//	Array.Copy(orderArray, orderCopy, length);
			//	for (int i=0; i<orderCopy.Length; i++) orderArray[i] = orderCopy[orderCopy.Length-1-i];

			}

			public static void MaskBorders (int[] order, int sizeX, int sizeZ)
			{
				for (int j=0; j<order.Length; j++)
				{
					int pos = order[j];

					int x = pos / sizeX;
					int z = pos % sizeX;

					if (x==0 || z==0 || x==sizeX-1 || z==sizeZ-1) order[j] = -1;
				}
			}

			public static void CreateTorrentsRef (float[] heights, int[] order, float[] torrents, CoordRect rect=new CoordRect())
			{
					for (int i=0; i<heights.Length; i++) torrents[i] = 1; //casting initial rain
					
					for (int j=heights.Length-1; j>=0; j--)
					{
						//finding column ordered by height
						int pos = order[j];
						if (pos<0) continue;


						MooreCross height = new MooreCross(heights, pos, rect.size.x);
						MooreCross torrent = new MooreCross(torrents, pos, rect.size.x); //moore
						if (torrent.vals[4] > 200000000) torrent.vals[4] = 200000000;

						MooreCross delta = height.vals[4] - height;
						delta = MooreCross.ClampMax(delta, 0);

						MooreCross percents = MooreCross.Zero();
						float sum = delta.Sum();
						if (sum>0.00001f) percents = delta / sum;

						MooreCross newTorrent = percents*torrent.vals[4];
						newTorrent.AddToMatrix(torrents, pos, rect.size.x);
					}
			}

			public static void ErosionRef (float[] heights, float[] torrents, float[] mudflow, int[] order, CoordRect rect=new CoordRect(),
				float erosionDurability=0.9f, float erosionAmount=1f, float sedimentAmount=0.5f)
			{
					for (int i=0; i<mudflow.Length; i++) mudflow[i] = 0;

					for (int j=heights.Length-1; j>=0; j--)
					{
						//finding column ordered by height
						int pos = order[j];
						if (pos<0) continue;


						Cross height = new Cross(heights, pos, rect.size.x);
						float h_min = height.Min();

						//getting height values
//						float[] m = heights; int i=pos; int sizeX = rect.size.x;
//						float h = m[i]; float hx = m[i-1]; float hX = m[i+1]; float hz = m[i-sizeX]; float hZ = m[i+sizeX];

						//height minimum
//						float h_min = h;
//						if (hx<h_min) h_min=hx; if (hX<h_min) h_min=hX; if (hz<h_min) h_min=hz; if (hZ<h_min) h_min=hZ;


						//erosion line
						float erodeLine = (heights[pos] + h_min)/2f; //halfway between current and maximum height
						if (heights[pos] < erodeLine) continue;

						//raising soil
						float raised = heights[pos] - erodeLine;
						float maxRaised = raised*(torrents[pos]-1) * (1-erosionDurability);
						if (raised > maxRaised) raised = maxRaised;
						raised *= erosionAmount;

						//saving arrays
						heights[pos] -= raised;
						mudflow[pos] += raised * sedimentAmount;
						//if (erosion != null) erosion.array[pos] += raised; //and writing to ref
					}
					
					//for (int i=0; i<heights.Length; i++) 
					//	if (float.IsNaN(heights[i])) Debug.Log("NaN"); 
			}

			public static void TransferMudflow (float[] heights, float[] mudflow, float[] sediments, int[] order, CoordRect rect=new CoordRect(), int erosionFluidityIterations=3)
			{
				for (int i=0; i<sediments.Length; i++) sediments[i] = 0;

				#region Settling sediment

					for (int l=0; l<erosionFluidityIterations; l++)
					for (int j=heights.Length-1; j>=0; j--)
					{				
						//finding column ordered by height
						int pos = order[j];
						if (pos<0) continue;

						Cross height = new Cross(heights, pos, rect.size.x);
						Cross sediment = new Cross(mudflow, pos, rect.size.x);

						float sedimentSum = sediment.Sum();
						if (sedimentSum < 0.00001f) continue;

						Cross pour = Cross.Pour(height, sedimentSum);

						pour.SetToMatrix(mudflow, pos, rect.size.x);
						if (sediments != null) pour.AddToMatrix(sediments, pos, rect.size.x);
				}

				//for (int i=0; i<heights.Length; i++) 
				//	if (float.IsNaN(heights[i])) Debug.Log("NaN");
				
				#endregion
			}

			public static void SettleMudflow (float[] heights, float[] mudflow, int[] order, CoordRect rect=new CoordRect(), float ruffle=0.1f)
			{
				
				
				//int seed = 12345;
				for(int j=heights.Length-1; j>=0; j--) 
				{
					//writing heights
					heights[j] += mudflow[j];
					
					/*seed = 214013*seed + 2531011; 
					float random = ((seed>>16)&0x7FFF) / 32768f;

					int pos = order[j];
					if (pos<0) continue;

					//float[] m = heights; int sizeX = rect.size.x;
					//float h = m[pos]; float hx = m[pos-1]; float hX = m[pos+1]; float hz = m[pos-sizeX]; float hZ = m[pos+sizeX];
					Cross height = new Cross(heights, pos, rect.size.x);

					//smoothing sediments a bit
					float s = mudflow[pos];
					if (s > 0.0001f)
					{
						float smooth = s/2f; if (smooth > 0.75f) smooth = 0.75f; 
						heights[pos] = heights[pos]*(1-smooth) + height.AvgSides()*smooth;
					}

					else
					{
						float maxHeight = height.MaxSides();
						float minHeight = height.MinSides();
						float randomHeight = random*(maxHeight-minHeight) + minHeight;
					//	heights[pos] = heights[pos]*(1-ruffle) + randomHeight*ruffle;
					}*/
				}
				
			}



	}//erosion class

}//namespace
