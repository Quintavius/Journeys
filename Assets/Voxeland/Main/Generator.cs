using System;
using UnityEngine;
using System.Collections.Generic;
#if UNITY_5_5
using UnityEngine.Profiling;
#endif

using Voxeland5;

namespace Voxeland5
{
	[System.Serializable]
	public class Generator
	{
		public enum LayerOverlayType { absolute, add, clampAppend, paint }
		
		[System.Serializable]
		public class PlanarGenerator
		{
			public int blockType = 0;
			public int level = 20;
			public bool borders = false;

			public void Generate (Matrix matrix, Func<float,bool> stop= null)
			{
				matrix.Fill(level);

				Coord min = matrix.rect.Min; Coord max = matrix.rect.Max;

				if (stop!=null && stop(1)) return;

				if (borders)
				{
					for (int x=min.x; x<max.x; x++)
					{
						matrix[x, min.z] = 1;
						matrix[x, max.z-1] = 1;
					}

					for (int z=min.z; z<max.z; z++)
					{
						matrix[min.x, z] = 1;
						matrix[max.x-1, z] = 1;
					}
				}
			}

			public virtual void OnGUI (Layout layout, string[] blockNames)
			{
				layout.Field(ref level, "Level");

				layout.Par(); 
				layout.Label("Type:", rect:layout.Inset(0.4f));
				blockType = (byte)layout.Popup(blockType, blockNames, rect:layout.Inset(0.4f));
				if (layout.lastChange && blockType==blockNames.Length-1) blockType = Data.emptyByte;
				blockType = (byte)layout.Field((int)blockType, rect:layout.Inset(0.2f), dragChange:false);

				layout.Field(ref borders, "Borders");

				layout.margin -= 10;
				layout.Par(10);
			}
		}

		[System.Serializable]
		public class NoiseGenerator
		{
			public bool enabled = false;
			public bool unfolded = false;
			public int seed = 12345;
			public float high = 1f;
			public float low = 0f;
			public float size = 200f;
			public float detail = 0.525f;
			public float turbulence = 0f;
			public Vector2 offset = new Vector2(0,0);
		
			public enum Type { Legacy=-1, Unity=0, Linear=1, Perlin=2, Simplex=3 };
			public Type noiseType = Type.Unity;

			public int blockType = 0;

			public void Generate (Matrix matrix, int seed, Func<float,bool> stop= null)
			{
				Noise noise = new Noise(seed^this.seed, permutationCount:16384);

				//range
				float range = high - low;

				//number of iterations
				int iterations = (int)Mathf.Log(size,2) + 1; //+1 max size iteration

				Coord min = matrix.rect.Min; Coord max = matrix.rect.Max;
				for (int x=min.x; x<max.x; x++)
				{
					if (stop!=null && stop(0)) return;
					for (int z=min.z; z<max.z; z++)
					{
						float val = noise.Fractal(x+(int)offset.x,z+(int)offset.y,size,iterations,detail,turbulence,(int)noiseType);
					
						val = val*range + low;

						matrix[x,z] = val;
					}
				}
			}

			public virtual void OnGUI (Layout layout, string[] blockNames, string name)
			{
				layout.ToggleFoldout(ref unfolded, ref enabled, name);
				if (!unfolded) return;

				layout.margin += 10;

				//params
				layout.fieldSize = 0.6f;
				//output.sharedResolution.guiResolution = layout.ComplexField(output.sharedResolution.guiResolution, "Output Resolution");
				layout.Field(ref noiseType, "Algorithm");
				layout.Field(ref seed, "Seed");
				layout.Field(ref high, "High (Intensity)");
				layout.Field(ref low, "Low");
				layout.Field(ref size, "Size", min:1);
				layout.Field(ref detail, "Detail", min:0,max:1);
				layout.Field(ref turbulence, "Turbulence");
				layout.Field(ref offset, "Offset");

				//type selector
				layout.Par(); 
				layout.Label("Type:", rect:layout.Inset(0.4f));
				blockType = (byte)layout.Popup(blockType, blockNames, rect:layout.Inset(0.4f));
				if (layout.lastChange && blockType==blockNames.Length-1) 
					blockType = Data.emptyByte;
				blockType = (byte)layout.Field((int)blockType, rect:layout.Inset(0.2f), dragChange:false);


				layout.margin -= 10;
				layout.Par(10);
			}
		}

		[System.Serializable]
		public class StainGenerator
		{
			public bool enabled = false;
			public bool unfolded = false;

			public int seed = 1234;
			public float high = 1f;
			public float low = -1f;
			public float size = 20f;
			public int thickness = 5;

			public float[] soilOpacity = new float[] {1};
			public bool showSoil = false;

			public int blockType = 0;

			public void Generate (Matrix matrix, int seed, Func<float,bool> stop= null) //input matrix is a mask
			{
				Noise noise = new Noise(seed^this.seed, permutationCount:16384);

				//range
				float range = high - low;

				//number of iterations
				int iterations = (int)Mathf.Log(size,2) + 1; //+1 max size iteration

				Coord min = matrix.rect.Min; Coord max = matrix.rect.Max;
				for (int x=min.x; x<max.x; x++)
				{
					if (stop!=null && stop(0)) return;
					for (int z=min.z; z<max.z; z++)
					{
						float val = noise.Fractal(x,z,size,iterations, detail:0.5f, turbulence:0f, type:0);
					
						val = val*range + low;

						matrix[x,z] = val * matrix[x,z];
					}
				}
			}

			public void OnGUI (Layout layout, string[] blockNames)
			{
				layout.ToggleFoldout(ref unfolded, ref enabled, "Stains");
				if (!unfolded) return;

				layout.margin += 10;

				//params
				layout.fieldSize = 0.6f;

				layout.Field(ref seed, "Seed");
				layout.Field(ref high, "High (Intensity)");
				layout.Field(ref low, "Low");
				layout.Field(ref size, "Size", min:1);
				layout.Field(ref thickness, "Layer Thickness", min:0);

				//soil types
				layout.margin += 13;
				layout.Par(5);
				layout.Foldout(ref showSoil, "Soil Types Opacity",bold:false);
				if (soilOpacity.Length != blockNames.Length-2) ArrayTools.Resize(ref soilOpacity, blockNames.Length-2);
				if (showSoil)
				{
					for (int i=0; i<soilOpacity.Length; i++)
					{
						layout.Par(); layout.Inset(0.1f);
						layout.Field(ref soilOpacity[i], label:blockNames[i], rect:layout.Inset(0.9f), fieldSize:0.3f);
					}
				}
				layout.margin -= 13;

				//type selector
				layout.Par(5);
				layout.Par(); 
				layout.Label("Type:", rect:layout.Inset(0.4f));
				blockType = (byte)layout.Popup(blockType, blockNames, rect:layout.Inset(0.4f));
				if (layout.lastChange && blockType==blockNames.Length-1) 
					blockType = Data.emptyByte;
				blockType = (byte)layout.Field((int)blockType, rect:layout.Inset(0.2f), dragChange:false);

				layout.margin -= 10;
				layout.Par(10);
			}
		}

		[System.Serializable]
		public class CurveGenerator
		{
			public bool enabled = true;
			public bool unfolded = false;
			public AnimationCurve curve = new AnimationCurve( new Keyframe[] { new Keyframe(0,0,1,1), new Keyframe(1,1,1,1) } );
			public bool extended = true;
			public Vector2 min = new Vector2(0,0);
			public Vector2 max = new Vector2(1,1);

			public void Generate (Matrix matrix, Func<float,bool> stop = null)
			{
				Curve c = new Curve(curve);
				for (int i=0; i<matrix.array.Length; i++) 
				{
					if (i%100==0 && stop!=null && stop(0)) return;
					matrix.array[i] = c.Evaluate(matrix.array[i]); 
				}
			}

			public void OnGUI (Layout layout, string[] blockNames)
			{
				layout.ToggleFoldout(ref unfolded, ref enabled, "Curve");
				if (!unfolded) return;

				layout.margin += 10;
				
				layout.Par(1);
				Rect savedCursor = layout.cursor;
				layout.Par(50, padding:0);
				layout.Inset(3);
				layout.Curve(curve, rect:layout.Inset(80, padding:0), ranges:new Rect(min.x, min.y, max.x-min.x, max.y-min.y));
				layout.Par(3);

				
				layout.cursor = savedCursor;
				layout.Par(); layout.Inset(100);
				layout.Label("Range:", rect:layout.Inset(layout.field.width-100));
				layout.Par(); layout.Inset(100);
				layout.Field(ref min, rect:layout.Inset(layout.field.width-120));
				layout.Par(); layout.Inset(100);
				layout.Field(ref max, rect:layout.Inset(layout.field.width-120));

				layout.cursor = savedCursor;
				layout.Par(50);

				layout.margin -= 10;
			}

			public Keyframe[] serializedKeys;
			public void OnBeforeSerialize ()
			{
				//serializedKeys = curve.keys;
			}
			public void OnAfterDeserialize ()
			{
				//curve = new AnimationCurve(serializedKeys); 
			}
		}

		[System.Serializable]
		public class SlopeGenerator
		{
			public bool enabled = false;
			public bool unfolded = false;
			public Vector2 steepness = new Vector2(45,90);
			public float range = 5f;
			public int thickness = 5;
			public int blockType = 0;

			public Matrix Generate (Matrix src, Func<float,bool> stop = null)
			{
				//preparing output
				Matrix dst = new Matrix(src.rect);

				//using the terain-height relative values
				float pixelSize = 1; //1f * MapMagic.instance.terrainSize / MapMagic.instance.resolution;
			
				float min0 = Mathf.Tan((steepness.x-range/2)*Mathf.Deg2Rad) * pixelSize / 128; //TODO: replace 128 with heightFactor
				float min1 = Mathf.Tan((steepness.x+range/2)*Mathf.Deg2Rad) * pixelSize / 128;
				float max0 = Mathf.Tan((steepness.y-range/2)*Mathf.Deg2Rad) * pixelSize / 128;
				float max1 = Mathf.Tan((steepness.y+range/2)*Mathf.Deg2Rad) * pixelSize / 128;

				//dealing with 90-degree
				if (steepness.y-range/2 > 89.9f) max0 = 20000000; if (steepness.y+range/2 > 89.9f) max1 = 20000000;

				//ignoring min if it is zero
				if (steepness.x<0.0001f) { min0=0; min1=0; }

				//delta map
				System.Func<float,float,float,float> inclineFn = delegate(float prev, float curr, float next) 
				{
					float prevDelta = prev-curr; if (prevDelta < 0) prevDelta = -prevDelta;
					float nextDelta = next-curr; if (nextDelta < 0) nextDelta = -nextDelta;
					return prevDelta>nextDelta? prevDelta : nextDelta; 
				};
				if (stop!=null && stop(0)) return dst;
				dst.Blur(inclineFn, intensity:1, takemax:true, reference:src); //intensity is set in func

				//slope map
				for (int i=0; i<dst.array.Length; i++)
				{
					if (i%100==0 && stop!=null && stop(0)) return dst;
					
					float delta = dst.array[i];
				
					if (steepness.x<0.0001f) dst.array[i] = 1-(delta-max0)/(max1-max0);
					else
					{
						float minVal = (delta-min0)/(min1-min0);
						float maxVal = 1-(delta-max0)/(max1-max0);
						float val = minVal>maxVal? maxVal : minVal;
						if (val<0) val=0; if (val>1) val=1;

						dst.array[i] = val;
					}
				}

				return dst;
			}

			public void OnGUI (Layout layout, string[] blockNames)
			{
				layout.ToggleFoldout(ref unfolded, ref enabled, "Slope");
				if (!unfolded) return;

				layout.margin += 10;
				
				layout.fieldSize = 0.6f;
				layout.Field(ref steepness, "Steepness", min:0, max:90);
				//layout.Field(ref range, "Range", min:0.1f);
				layout.Field(ref thickness, "Layer Thickness", min:0);

				//type selector
				layout.Par(); 
				layout.Label("Type:", rect:layout.Inset(0.4f));
				blockType = (byte)layout.Popup(blockType, blockNames, rect:layout.Inset(0.4f));
				if (layout.lastChange && blockType==blockNames.Length-1) 
					blockType = Data.emptyByte;
				blockType = (byte)layout.Field((int)blockType, rect:layout.Inset(0.2f), dragChange:false);

				layout.margin -= 10;
				layout.Par(10);
			}
		}

		[System.Serializable]
		public class CavityGenerator
		{
			public bool enabled = false;
			public bool unfolded = false;
			public enum CavityType { Convex, Concave }
			public CavityType type = CavityType.Convex;
			public float intensity = 5;
			public float spread = 5;
			public bool normalize = true;
			public int safeBorders = 5;
			public int thickness = 5;
			public int blockType = 0;

			public Matrix Generate (Matrix src, Func<float,bool> stop = null)
			{
				//preparing outputs
				Matrix dst = new Matrix(src.rect);

				//cavity
				System.Func<float,float,float,float> cavityFn = delegate(float prev, float curr, float next) 
				{
					float c = curr - (next+prev)/2;
					return (c*c*(c>0?1:-1))*intensity*100000;
				};
				if (stop!=null && stop(0)) return dst;
				dst.Blur(cavityFn, intensity:1, additive:true, reference:src); //intensity is set in func

				//borders
				dst.RemoveBorders(); 

				//inverting
				if (type == CavityType.Concave) dst.Invert();

				//normalizing
				if (!normalize) dst.Clamp01();

				//spread
				dst.Spread(strength:spread); 

				dst.Clamp01();
			
				//mask and safe borders
			//	if (intensity < 0.9999f) Matrix.Blend(src, dst, intensity);
			//	if (safeBorders != 0) Matrix.SafeBorders(null, dst, safeBorders);

				return dst;
			}

			public void OnGUI (Layout layout, string[] blockNames)
			{
				layout.ToggleFoldout(ref unfolded, ref enabled, "Cavity");
				if (!unfolded) return;

				layout.margin += 10;

				layout.Field(ref type, "Type");
				layout.Field(ref intensity, "Intensity");
				layout.Field(ref spread, "Spread");
				//layout.Par(3);
				//layout.Toggle(ref normalize, "Normalize");
				//layout.Par(15); layout.Inset(20); layout.Label(label:"Convex + Concave", rect:layout.Inset(), textAnchor:TextAnchor.LowerLeft);
				//layout.Field(ref safeBorders, "Safe Borders");
				layout.Field(ref thickness, "Layer Thickness", min:0);

				//type selector
				layout.Par(); 
				layout.Label("Type:", rect:layout.Inset(0.4f));
				blockType = (byte)layout.Popup(blockType, blockNames, rect:layout.Inset(0.4f));
				if (layout.lastChange && blockType==blockNames.Length-1) 
					blockType = Data.emptyByte;
				blockType = (byte)layout.Field((int)blockType, rect:layout.Inset(0.2f), dragChange:false);

				layout.margin -= 10;
				layout.Par(10);
			}
		}

		[System.Serializable]
		public class BlurGenerator
		{
			public bool enabled = true;
			public bool unfolded = false;
			public int iterations = 5;
			public float strength = 1;
			public int loss = 30;
			public float level = -3;
			public int safeBorders = 40;
			public int blockType = 0;

			public Matrix Generate (Matrix src, Func<float,bool> stop = null)
			{
				//preparing outputs
				Matrix dst = src.Copy();
				
				//blur with loss
				int curLoss = loss;
				while (curLoss>1)
				{
					dst.LossBlur(curLoss);
					curLoss /= 2;
				}
			
				//main blur (after loss)
				dst.SimpleBlur(iterations, strength);

				dst.Add(level/128);
				dst.Clamp01();
			
				//mask and safe borders
				if (safeBorders != 0) Matrix.SafeBorders(src, dst, safeBorders);

				return dst;
			}

			public void OnGUI (Layout layout, string[] blockNames)
			{
				layout.ToggleFoldout(ref unfolded, ref enabled, "Sediment");
				if (!unfolded) return;

				layout.margin += 10;

				layout.Field(ref iterations, "Iterations");
				layout.Field(ref strength, "Blur");
				layout.Field(ref loss, "Loss");
				layout.Field(ref safeBorders, "Safe Borders");
				layout.Field(ref level, "Level");

				//type selector
				layout.Par(); 
				layout.Label("Type:", rect:layout.Inset(0.4f));
				blockType = (byte)layout.Popup(blockType, blockNames, rect:layout.Inset(0.4f));
				if (layout.lastChange && blockType==blockNames.Length-1) 
					blockType = Data.emptyByte;
				blockType = (byte)layout.Field((int)blockType, rect:layout.Inset(0.2f), dragChange:false);

				layout.margin -= 10;
				layout.Par(10);
			}
		}

		[System.Serializable]
		public class ScatterGenerator
		{
			public bool enabled = false;
			public bool unfolded = false;
			public int seed = 12345;
			public int count = 10;
			public float uniformity = 0.1f;
			public float relax = 0.1f;
			public int safeBorders = 2;

			public float[] soilOpacity = new float[] {1};
			public bool showSoil = false;

			public int blockType = 0;

			public void Generate (SpatialHash spatialHash, int seed, Matrix probability)
			{
				InstanceRandom rnd = new InstanceRandom(seed^this.seed);
				RandomScatter(count, spatialHash, rnd, probability);
			}

			public void RandomScatter (int count, SpatialHash spatialHash, InstanceRandom rnd, Matrix probMatrix)
			{
				int candidatesNum = (int)(uniformity*100);
				if (candidatesNum < 1) candidatesNum = 1;
			
				for (int i=0; i<count; i++)
				{
					Vector2 bestCandidate = Vector3.zero;
					float bestDist = 0;
				
					for (int c=0; c<candidatesNum; c++)
					{
						Vector2 candidate = new Vector2((spatialHash.offset.x+1) + (rnd.Random()*(spatialHash.size-2.01f)), (spatialHash.offset.y+1) + (rnd.Random()*(spatialHash.size-2.01f)));
				
						//checking if candidate available here according to probability map
						//if (probMatrix!=null && probMatrix[candidate] < rnd.Random()+0.0001f) continue;

						//checking if candidate is the furthest one
						float dist = spatialHash.MinDist(candidate);

						//distance to the edge
						float bd = (candidate.x-spatialHash.offset.x)*2; if (bd < dist) dist = bd;
						bd = (candidate.y-spatialHash.offset.y)*2; if (bd < dist) dist = bd;
						bd = (spatialHash.offset.x+spatialHash.size-candidate.x)*2; if (bd < dist) dist = bd;
						bd = (spatialHash.offset.y+spatialHash.size-candidate.y)*2; if (bd < dist) dist = bd;

						if (dist>bestDist) { bestDist=dist; bestCandidate = candidate; }
					}

					if (bestDist>0.001f) 
					{
						spatialHash.Add(bestCandidate, 0, 0, 1); //adding only if some suitable candidate found
					}
				}

				//masking
				for (int c=0; c<spatialHash.cells.Length; c++)
				{
					SpatialHash.Cell cell = spatialHash.cells[c];
					for (int i=cell.objs.Count-1; i>=0; i--)
					{
						Vector2 pos = cell.objs[i].pos;
				
						if (pos.x < spatialHash.offset.x+safeBorders || 
							pos.y < spatialHash.offset.y+safeBorders ||
							pos.x > spatialHash.offset.x+spatialHash.size-safeBorders ||
							pos.y > spatialHash.offset.y+spatialHash.size-safeBorders ) { cell.objs.RemoveAt(i); continue; }

						if (probMatrix!=null && probMatrix[pos] < rnd.Random()+0.0001f) { cell.objs.RemoveAt(i); continue; }
					}
				}
			}

			public void OnGUI (Layout layout, string[] objectNames, string[] landNames)
			{
				layout.ToggleFoldout(ref unfolded, ref enabled, "Scatter");
				if (!unfolded) return;

				layout.margin += 10;

				layout.Field(ref seed, "Seed");
				layout.Field(ref count, "Density");
				layout.Field(ref uniformity, "Uniformity", min:0, max:1);
				layout.Field(ref relax, "Relax", min:0, max:1);
				layout.Field(ref safeBorders, "Safe Borders", min:0);

				//soil types
				layout.margin += 13;
				layout.Par(5);
				layout.Foldout(ref showSoil, "Soil Types Opacity",bold:false);
				if (soilOpacity.Length != landNames.Length-2) ArrayTools.Resize(ref soilOpacity, landNames.Length-2);
				if (showSoil)
				{
					for (int i=0; i<soilOpacity.Length; i++)
					{
						layout.Par(); layout.Inset(0.1f);
						layout.Field(ref soilOpacity[i], label:landNames[i], rect:layout.Inset(0.9f), fieldSize:0.3f);
					}
				}
				layout.margin -= 13;

				//type selector
				layout.Par(5);
				layout.Par(); 
				layout.Label("Type:", rect:layout.Inset(0.4f));
				blockType = (byte)layout.Popup(blockType, objectNames, rect:layout.Inset(0.4f));
				if (layout.lastChange && blockType==objectNames.Length-1) 
					blockType = Data.emptyByte;
				blockType = (byte)layout.Field((int)blockType, rect:layout.Inset(0.2f), dragChange:false);

				layout.margin -= 10;
				layout.Par(10);
			}
		}

		[System.Serializable]
		public class GrassGenerator
		{
			public bool enabled = false;
			public bool unfolded = false;
			public int seed = 12345;
			public float size = 200f;
			public float contrast = 1f;
			public float bias = 0.5f;
		
			public enum Type { Unity=0, Linear=1, Perlin=2, Simplex=3 };
			public Type noiseType = Type.Unity;

			public float[] soilOpacity = new float[] {1};
			public bool showSoil = false;

			public int grassType = 0;
			public string name = "Grass";

			public void Generate (Matrix matrix, int seed) // aka probabilityMatrix
			{
				Noise noise = new Noise(seed^this.seed, permutationCount:16384);

				//number of iterations
				int iterations = (int)Mathf.Log(size,2) + 1; //+1 max size iteration

				Coord min = matrix.rect.Min; Coord max = matrix.rect.Max;
				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
					{
						float val = noise.Fractal(x,z,size,iterations,0.5f,0,(int)noiseType);
					
						//val = val*range + low;
						
						//apply contrast and bias
						val += bias*2 - 1;
						val = (val-0.5f)*contrast + 0.5f;
						//val -= 0*(1-bias) + (intensity-1)*bias; //0.5f - intensity*bias;

						matrix[x,z] *= val;
					}
			}

			public void OnGUI (Layout layout, string[] grassNames, string[] landNames)
			{
				//layout.ToggleFoldout(ref unfolded, ref enabled, "Grass");
				//if (!unfolded) return;

				//layout.margin += 10;

				//params
				layout.fieldSize = 0.6f;

				layout.Field(ref seed, "Seed");
				layout.Field(ref size, "Noise Size", min:1);
				layout.Field(ref contrast, "Grouping");
				layout.Field(ref bias, "Density");

				//soil types
				layout.margin += 13;
				layout.Par(5);
				layout.Foldout(ref showSoil, "Soil Types Opacity",bold:false);
				if (soilOpacity.Length != landNames.Length-2) ArrayTools.Resize(ref soilOpacity, landNames.Length-2);
				if (showSoil)
				{
					for (int i=0; i<soilOpacity.Length; i++)
					{
						layout.Par(); layout.Inset(0.1f);
						layout.Field(ref soilOpacity[i], label:landNames[i], rect:layout.Inset(0.9f), fieldSize:0.3f);
					}
				}
				layout.margin -= 13;

				//type selector
				layout.Par(5);
				layout.Par(); 
				layout.Label("Type:", rect:layout.Inset(0.4f));
				grassType = (byte)layout.Popup(grassType, grassNames, rect:layout.Inset(0.4f));
				if (layout.lastChange && grassType==grassNames.Length-1) 
					grassType = Data.emptyByte;
				grassType = (byte)layout.Field((int)grassType, rect:layout.Inset(0.2f), dragChange:false);
				layout.Par(5);

				//layout.margin -= 10;
				//layout.Par(10);
			}
		}

		[System.Serializable]
		public class HeightGenerator
		{
			public bool enabled = true;
			public bool unfolded = false;
			
			public Texture2D texture;
			public bool loadEachGen = false;

			public bool autoScale = true;
			public float scale = 1;
			public Vector2 offset;
			public Matrix.WrapMode wrapMode = Matrix.WrapMode.Once;

			[System.NonSerialized] public Matrix textureMatrix = new Matrix();

			public void CheckLoadTexture (bool force=false)
			{
				if (texture==null) return;
				lock (textureMatrix)
				{
					if (textureMatrix.rect.size.x!=texture.width || textureMatrix.rect.size.z!=texture.height || force)
					{
						textureMatrix.ChangeRect( new CoordRect(0,0, texture.width, texture.height), forceNewArray:true );
						try { textureMatrix.FromTextureAlpha(texture); }
						catch (UnityException e) { Debug.LogError(e); }
					}
				}
			}
		
			public void Generate (Matrix matrix, int seed, Func<float,bool> stop= null)
			{
				if (autoScale && Voxeland.instances != null && Voxeland.instances.Count != 0) 
					scale = Voxeland.instances.Any().terrainSize / matrix.rect.size.x;
				
				Coord min = matrix.rect.Min; Coord max = matrix.rect.Max;

				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
				{
					float sx = (x-offset.x)/scale * textureMatrix.rect.size.x/matrix.rect.size.x;
					float sz = (z-offset.y)/scale * textureMatrix.rect.size.z/matrix.rect.size.z;

					matrix[x,z] = textureMatrix.GetInterpolated(sx, sz, wrapMode);
				}
				if (stop!=null && stop(0)) return;

				if (scale >= 2f)
				{
					//Matrix cpy = matrix.Copy();
					for (int i=1; i<scale-1; i+=2) matrix.Blur();
					//Matrix.SafeBorders(cpy, matrix, Mathf.Max(matrix.rect.size.x/128, 4));
				}
			}

			public void OnGUI (Layout layout, string[] landNames)
			{
				layout.ToggleFoldout(ref unfolded, ref enabled, "Heightmap");
				if (!unfolded) return;

				layout.margin += 10;

				layout.fieldSize = 0.62f;
				layout.Field(ref texture, "Texture");
				if (layout.Button("Reload")) CheckLoadTexture(force:true); //ReloadTexture();
				layout.Toggle(ref loadEachGen, "Reload Each Generate");
				layout.Toggle(ref autoScale, "Auto Scale to terrain size");
				layout.Field(ref scale, "Scale", disabled:autoScale);
				layout.Field(ref offset, "Offset");
				layout.Field(ref wrapMode, "Wrap Mode");

				layout.margin -= 10;
				layout.Par(10);
			}
		}

		public enum GeneratorType { Planar, Noise, MapMagic, Heightmap }
		public GeneratorType generatorType = GeneratorType.Noise;

		public PlanarGenerator planarGen = new PlanarGenerator();
		public NoiseGenerator noiseGen = new NoiseGenerator();  
		public NoiseGenerator noiseGenB = new NoiseGenerator();  
		public CurveGenerator curveGen = new CurveGenerator(); 
		public SlopeGenerator slopeGen = new SlopeGenerator(); 
		public CavityGenerator cavityGen = new CavityGenerator(); 
		public BlurGenerator blurGen = new BlurGenerator(); 
		public StainGenerator stainsGen = new StainGenerator();
		public ScatterGenerator scatterGen = new ScatterGenerator(); 

		[System.Serializable]
		public class GrassGens : Layout.ILayered
		{
			public GrassGenerator[] gens;

			public string[] landNames;
			public string[] grassNames;

			public void OnLayerHeader (Layout layout, int num)
			{
				layout.margin += 10; layout.rightMargin += 10;
				layout.Par(32, padding:0);

				//icon
				//int iconWidth = (int)layout.cursor.height;
				//Rect iconRect = layout.Inset(iconWidth);
				//iconRect = iconRect.Extend(-3);
				//if (num<) layout.Icon(layer.splat.texture, iconRect, alphaBlend:false);
				//layout.Element(num == selected? "DPLayout_LayerIconActive" : "DPLayout_LayerIconInactive", iconRect, new RectOffset(6,6,6,6), null);

				//label
				Rect labelRect = layout.Inset(layout.field.width - 0 - 18 - layout.margin-layout.rightMargin);
				labelRect.y += (labelRect.height-18)/2f; labelRect.height = 18;
				if (gens[num].grassType<grassNames.Length) layout.Label(grassNames[gens[num].grassType], labelRect);
				else layout.Label("Empty", labelRect);

				layout.margin -= 10; layout.rightMargin -= 10;
			}

			public void OnLayerGUI (Layout layout, int num) 
			{
				layout.margin += 5; layout.rightMargin += 5;

				gens[num].OnGUI(layout, grassNames, landNames);

				layout.Par(3);
				layout.margin -= 5; layout.rightMargin -= 5;
			}

			public int selected {get;set;}
			public bool expanded {get;set;}

			public void AddLayer (int num) { ArrayTools.Insert(ref gens, num, new GrassGenerator()); }
			public void RemoveLayer (int num) { ArrayTools.RemoveAt(ref gens, num);  }
			public void SwitchLayers (int num1, int num2) { ArrayTools.Switch(gens, num1, num2); }
		}

		//public GrassGenerator grassGen = new GrassGenerator(); 
		public GrassGens grassGens = new GrassGens { gens= new GrassGenerator[0] };
		public int grassGensSelected = 0;
		public bool grassGensExpanded = false;


		public HeightGenerator standaloneHeightGen = new HeightGenerator();
		
		public int seed = 12345;
		public bool polish = true;
		public bool removeThinLayers = false;
		public int minLayerThickness = 3;
		public int heightFactor = 128;
		public bool saveResults = true;
		public bool instantGenerate = true;
		public bool forceGenerateChangedAreas = true;
		static public bool leaveDemoUntouched = false;

		public bool change = false; //generator is changed and non-generated

		#if MAPMAGIC
		//public MapMagic.Graph mapMagicGens;
		public MapMagic.GeneratorsAsset mapMagicGens;
		#endif

		public void Generate (Data.Area area, Func<float,bool> stop = null)
		{
			if (area.pinned) return;
			change = false;
			if (stop!=null && stop(0)) return;

			//special case for preserving a demo scene on generator change
//			if (area.coord.x==0 && area.coord.z==0) return;
			Data.Area savedArea = null;
			if (leaveDemoUntouched && area.coord.x==0 && area.coord.z==0)
				savedArea = (Data.Area)area.Clone();

			if (generatorType == GeneratorType.Planar)
			{
				area.ClearLand();
				area.ClearObjects();
				area.ClearGrass();

				Matrix matrix = new Matrix(area.rect);
				planarGen.Generate(matrix, stop);

				area.AddLayer(matrix, planarGen.blockType, heightFactor:1, noise:null);
			}

			if (generatorType == GeneratorType.Noise)
			{
				area.ClearLand();
				area.ClearObjects();
				area.ClearGrass();

				Noise noise = new Noise(123, permutationCount:512); //random to floor floats 

				Matrix noiseMatrix = new Matrix(area.rect);

				if (stop!=null && stop(0)) return;
				if (noiseGen.enabled) noiseGen.Generate(noiseMatrix, seed, stop); 

				if (stop!=null && stop(0)) return;
				if (curveGen.enabled) curveGen.Generate(noiseMatrix, stop);

				if (stop!=null && stop(0)) return;
				area.AddLayer(noiseMatrix, noiseGen.blockType, heightFactor:heightFactor,noise:noise); //TODO: set block types instead of magical numbers
					
				if (slopeGen.enabled)
				{
					if (stop!=null && stop(0)) return;
					Matrix slopeMatrix = slopeGen.Generate(noiseMatrix, stop);
					area.PaintLayer(slopeMatrix, slopeGen.blockType, noise:noise, paintThickness:slopeGen.thickness);
				}

				if (cavityGen.enabled)
				{
					if (stop!=null && stop(0)) return;
					Matrix cavityMatrix = cavityGen.Generate(noiseMatrix, stop);
					area.PaintLayer(cavityMatrix, cavityGen.blockType, noise:noise, paintThickness:cavityGen.thickness);
				}

				Matrix blurMatrix;
				if (blurGen.enabled)
				{
					if (stop!=null && stop(0)) return;
					blurMatrix = blurGen.Generate(noiseMatrix, stop);
					blurMatrix.Max(noiseMatrix);
					area.ClampAppendLayer(blurMatrix, blurGen.blockType, noise:noise, heightFactor:heightFactor);
				}
				else blurMatrix = noiseMatrix;

				if (stainsGen.enabled)
				{
					Matrix matrix = area.GetSoilMatrix(stainsGen.soilOpacity);
					//Matrix stains = new Matrix(area.rect);

					stainsGen.Generate(matrix, seed, stop);
					area.PaintLayer(matrix, stainsGen.blockType, noise:noise, paintThickness:stainsGen.thickness);
				}

				if (noiseGenB.enabled)
				{
					Matrix matrix = new Matrix(area.rect);
					noiseGenB.Generate(matrix, seed, stop);
					area.SetLayer(matrix, null, noiseGenB.blockType, heightFactor:heightFactor, noise:noise);
				}

				if (scatterGen.enabled)
				{
					if (stop!=null && stop(0)) return;

					SpatialHash spatialHash = new SpatialHash( new Vector2(area.rect.offset.x, area.rect.offset.z), area.rect.size.x, 16);

					Matrix soil = area.GetSoilMatrix(scatterGen.soilOpacity);

					scatterGen.Generate(spatialHash,seed,soil);

					foreach (SpatialObject obj in spatialHash.AllObjs())
					{
						int x = (int)(obj.pos.x + 0.5f);
						int z = (int)(obj.pos.y + 0.5f);
						int y = (int)((obj.height + blurMatrix[x,z]) * heightFactor);
						area.AddObject( new CoordDir(x,y,z), (short)scatterGen.blockType );
					}
				}

				for (int g=0; g<grassGens.gens.Length; g++)
				{
					GrassGenerator grassGen = grassGens.gens[g];
					//if (grassGen.enabled)
					{
						if (stop!=null && stop(0)) return;

						Matrix grassMatrix = area.GetSoilMatrix(grassGen.soilOpacity);

						grassGen.Generate(grassMatrix,seed);

						area.SetGrassLayer(grassMatrix, (byte)grassGen.grassType, 1, noise);
					}
				}
			}

			else if (generatorType == GeneratorType.Heightmap)
			{
				area.ClearLand();
				area.ClearObjects();
				area.ClearGrass();

				Noise noise = new Noise(123, permutationCount:512); //random to floor floats 

				Matrix matrix = new Matrix(area.rect);

				if (stop!=null && stop(0)) return;
				if (standaloneHeightGen.enabled) standaloneHeightGen.Generate(matrix, seed, stop); 
				area.AddLayer(matrix, noiseGen.blockType, heightFactor:heightFactor, noise:noise); //TODO: set block types instead of magical numbers
			}

			else if (generatorType == GeneratorType.MapMagic)
			{
				#if MAPMAGIC
				if (stop!=null && stop(0)) return;
				if (area.results==null) area.results = new MapMagic.Chunk.Results();
				//MapMagic.Chunk.Size size = new MapMagic.Chunk.Size(area.rect.size.x,area.rect.size.x,heightFactor);

				if (stop!=null && stop(0)) return;
				if (mapMagicGens != null)
					mapMagicGens.Calculate(area.rect.offset.x, area.rect.offset.z, area.rect.size.x, area.results,  new MapMagic.Chunk.Size(area.rect.size.x,area.rect.size.x,heightFactor), seed, stop);
					//mapMagicGens.Generate(area.results, area.rect.offset.x, area.rect.offset.z, area.rect.size.x, area.rect.size.x, heightFactor, seed, stop);
				else { area.ClearLand(); area.ClearObjects(); area.ClearGrass(); }
				#else
				area.ClearLand(); area.ClearObjects(); area.ClearGrass();
				#endif
			}

			if (stop!=null && stop(0)) return;
			if (removeThinLayers) area.RemoveThinLayers(minLayerThickness);

			if (stop!=null && stop(0)) return;
			if (polish) area.Polish();

			//special case for preserving a demo scene on generator change
			if (leaveDemoUntouched && area.coord.x==0 && area.coord.z==0)
			{
				Matrix mask = new Matrix( new CoordRect(0,0,area.lines.Length, area.lines.Length) );
				for (int x=0; x<mask.rect.size.x; x++)
					for (int z=0; z<mask.rect.size.z; z++)
					{
						int distFromEdge = Mathf.Min(x, mask.rect.size.x-x, z, mask.rect.size.z-z);
						mask[x,z] = Mathf.Clamp01(distFromEdge/50f);
					}

				Matrix maskInverted = (Matrix)mask.Clone(); maskInverted.InvertOne();

				area.MixAreas( new Data.Area[] {(Data.Area)area.Clone(), savedArea}, new Matrix[] {maskInverted, mask} );
				area.objects = savedArea.objects;
			}

			//area.generated = true;
			//area.serializable = true; 
			
		}

		
		/*public void GenerateThreaded (Data.Area area)
		{
			gens.Prepare();
			ThreadWorker generateWorker = new ThreadWorker();
			generateWorker.tag = "Voxeland";
			generateWorker.name = "Area (" + area.coord.x + "," + area.coord.z + ")";
			generateWorker.Calculate += delegate() { Generate(area); };
			generateWorker.Start();
		}*/

	/*	public void OnGUI (Layout layout)
		{
			layout.Field(ref generatorType, "Generator Type");
			
			if (generatorType == GeneratorType.Noise)
			{
				noiseGen.OnGUI(layout);
				curveGen.OnGUI(layout);
				slopeGen.OnGUI(layout);
				cavityGen.OnGUI(layout);
				blurGen.OnGUI(layout);
			}
			else if (generatorType == GeneratorType.MapMagic)
			{
				layout.Field(ref gens, "Generators");
				
				layout.Par(24); if (layout.Button("Show Editor", rect:layout.Inset(), icon:"MapMagic_EditorIcon"))
				{
					MapMagic.MapMagic. window = (MapMagicWindow)EditorWindow.GetWindow (typeof (MapMagicWindow));
					window.gens = script.gens;
					window.mapMagic = script;
					//SceneMagicWindow window = EditorWindow.GetWindow<VoxelandCreate>();
					//window.script = script;
					window.Show();
	//				window.FocusOnOutput();
				}
			}

			layout.Field(ref heightFactor, "Height");
		}
		*/

	}
}
