using UnityEngine;
using System;
using System.Collections.Generic;

using Voxeland5;

namespace Voxeland5
{
	[System.Serializable]
	public class Data :  ScriptableObject, ISerializationCallbackReceiver, ICloneable
	{
		public class Area : IChunk
		{
			public class Line
			{
				public struct Column
				{
					public int start; 
					public byte count;

					//public byte grass;
					public ushort topLevel; //TODO: make short
					public byte topType;
				}


				public Column[] columns;

				public List<byte> types = new List<byte>();
				public List<byte> level = new List<byte>();
			}

			public Line[] lines;
			public Dictionary<ulong, short> objects;
			public byte[] grass; //works like matrix2

			public Coord coord { get; set; }		//cell size = 1
			public CoordRect rect;				 	//cell size = area size
			//public Rect pos { get; set; }			//in world units
			public int hash { get; set; }
			public bool pinned {get; set;}

			//public bool generated = false; //TODO consider replacing with worker.ready

			[System.NonSerialized] public ThreadWorker generateWorker;

			#if MAPMAGIC
			[System.NonSerialized] public MapMagic.Chunk.Results results;
			#endif


			public void Init (Coord coord, int areaSize, Data data)
			{
				//rect
				this.coord = coord;
				rect = new CoordRect(coord.x*areaSize, coord.z*areaSize, areaSize, areaSize);
				
				//lines
				lines = new Line[areaSize];
				for (int i=0; i<lines.Length; i++) 
				{
					Line line = new Line() { columns=new Line.Column[areaSize] };
					for(int c=0; c<line.columns.Length; c++)
						line.columns[c].topType = emptyByte;
					lines[i] = line;
				}

				//objects
				objects = new Dictionary<ulong, short>();

				//grass
				/*for (int l=0; l<lines.Length; l++)
				{
					Line line = lines[l];
					for (int c=0; c<line.columns.Length; c++)
						line.columns[c].grass = emptyByte;
				}*/
				grass = new byte[rect.size.x*rect.size.z];
				for (int i=0; i<grass.Length; i++) grass[i] = emptyByte;

				//worker
				if (data!=null && data.generator!=null)
				{
					generateWorker = new ThreadWorker();
					generateWorker.tag = "VoxelandGenerate";
					generateWorker.name = "Gen Area (" + coord.x + "," + coord.z + ")";
					generateWorker.Calculate += delegate() { data.generator.Generate(this, generateWorker.StopCallback); };
					#if MAPMAGIC
					generateWorker.Prepare += delegate() 
					{ 
						if (data.generator.mapMagicGens != null) 
							data.generator.mapMagicGens.Prepare(null); 
							//data.generator.mapMagicGens.Prepare(null, new CoordRect(coord,new Coord(areaSize,areaSize)), areaSize, 0, 12345); 
					}; //Preparing generators for mapmagic
					#endif
					generateWorker.Apply += delegate() 
					{
						#if MAPMAGIC
						//purging unused outputs
						/*if (data.generator.generatorType==Generator.GeneratorType.MapMagic && data.generator.mapMagicGens!=null)
						{
							HashSet<System.Type> outputGenTypes = new HashSet<System.Type>();
							foreach (MapMagic.OutputGenerator gen in data.generator.mapMagicGens.GeneratorsOfType<MapMagic.OutputGenerator>())
								if (!outputGenTypes.Contains(gen.GetType())) outputGenTypes.Add(gen.GetType());
							if (!outputGenTypes.Contains(typeof(MapMagic.VoxelandOutput))) MapMagic.VoxelandOutput.Purge(rect,null);
							if (!outputGenTypes.Contains(typeof(MapMagic.VoxelandObjectsOutput))) MapMagic.VoxelandObjectsOutput.Purge(rect,null);
							if (!outputGenTypes.Contains(typeof(MapMagic.VoxelandGrassOutput))) MapMagic.VoxelandGrassOutput.Purge(rect,null);
						}*/

						//clearing results in playmode
						bool playmode = true;
						#if UNITY_EDITOR
						if (!UnityEditor.EditorApplication.isPlaying) playmode = false;
						#endif
						if (playmode) results = null;
						#endif

						//preparing texture generators
						if (data.generator.generatorType == Generator.GeneratorType.Heightmap)
						{
							if (data.generator.standaloneHeightGen.loadEachGen || data.generator.standaloneHeightGen.textureMatrix==null) 
								data.generator.standaloneHeightGen.CheckLoadTexture(force:true);
						}

						//calling event
						Voxeland.CallOnAreaGenerated(data, data.generator, this);

						//generated = true; //marking as generated as a last step (to prevent waiting chunks start to generate)
					};
				}
			}
			public void Init (int x, int z, int areaSize, Data data) { Init(new Coord(x,z), areaSize, data); }

			public object Clone ()
			{
				Area newArea = new Area();
				newArea.coord = coord; newArea.rect = rect; 
				newArea.hash = hash; newArea.pinned = pinned;

				newArea.lines = new Line[lines.Length];
				for (int i=0; i<lines.Length; i++) 
				{
					newArea.lines[i] = new Line();
					newArea.lines[i].columns = new Line.Column[lines[i].columns.Length]; Array.Copy(lines[i].columns, newArea.lines[i].columns, newArea.lines[i].columns.Length); 
					newArea.lines[i].types = new List<byte>(); newArea.lines[i].types.AddRange(lines[i].types);
					newArea.lines[i].level = new List<byte>(); newArea.lines[i].level.AddRange(lines[i].level);
				}

				newArea.objects = new Dictionary<ulong, short>();
				foreach (KeyValuePair<ulong,short> kvp in objects) newArea.objects.Add(kvp.Key, kvp.Value);

				newArea.grass = new byte[grass.Length];
				Array.Copy(grass, newArea.grass, grass.Length);

				return newArea;
			}


			public void OnCreate (object dataBox=null)
			{
				//initializing
				Data data = (Data)dataBox;
				Init(coord, data.areaSize, data);

				//starting area generate
				generateWorker.Start();
			}
			public void OnMove (Coord oldCoord, Coord newCoord) { Debug.Log("MOVEEEEEEE!"); } //no move is used
			public void OnRemove () 
			{ 
				//stopping generate if any
				if (generateWorker != null) generateWorker.Stop();
			} 


			public int GetColumnNum (int x, int z)
			{
				return (z-rect.offset.z)*rect.size.x + x-rect.offset.x;
			}

			public void SetColumnsStarts ()	
			{
				for(int l=0; l<lines.Length; l++)
				{
					Line line = lines[l];

					int counter = 0;
					for(int c=0; c<line.columns.Length; c++)
					{
						line.columns[c].start = counter;
						counter += line.columns[c].count;
					}

					if(counter > line.types.Count)
						Debug.Log("Column start is more than types count");
				}
			}

			public void SetColumnstTopLevels ()
			{
				for(int l=0; l<lines.Length; l++)
				{
					Line line = lines[l];

					for (int c=0; c<line.columns.Length; c++)
					{
						int columnStart = line.columns[c].start;
						int columnCount = line.columns[c].count;

						int lev = 0;

						for (int i=columnStart; i<columnStart+columnCount; i++)
							lev += line.level[i];

						line.columns[c].topLevel = (ushort)lev;
					}
				}
			}

			public void ClearColumn (int l, int c)
			{
				Line line = lines[l];
				
				int columnStart = line.columns[c].start;
				byte columnCount = line.columns[c].count;

				line.types.RemoveRange(columnStart, columnCount);
				line.level.RemoveRange(columnStart, columnCount);

				line.columns[c].count = 0;
				line.columns[c].topType = 0;
				line.columns[c].topLevel = 0;
			}

			public void ClearLand ()
			{
				for (int l=0; l<lines.Length; l++)
				{
					Line line = lines[l];
					for (int c=0; c<line.columns.Length; c++)
					{
						line.columns[c].start = 0;
						line.columns[c].count = 0;
						line.columns[c].topType = 0;
						line.columns[c].topLevel = 0;
					}
					line.types.Clear();
					line.level.Clear();
				} 
			}

			public void ClearObjects ()
			{
				objects.Clear();
			}

			public void ClearGrass ()
			{
				for (int i=0; i<grass.Length; i++) grass[i] = emptyByte;
			}

			public byte GetBlock (int l, int c, int y) //for test purpose
			{
				Line line = lines[l];
				
				int layer = 0;
				for (int i=0; i<line.columns[c].count; i++)
				{
					layer += line.level[i+line.columns[c].start];
					if (layer > y) return line.types[i+line.columns[c].start];
				}
				return emptyByte;
			}

			public void InsertLayer (int l, int c, int start, int thickness, byte type)
			{
				//negative start
				if (start<0) { thickness+=start; start=0; }

				//guard in case of zero-thickness
				if (thickness <= 0) return;

				//splitting large thickness
				if (start/maxByte != (start+thickness-1)/maxByte) //if starts at maxByte*1 and ends at maxByte*2
				{
					int beginningThickness = maxByte - start%maxByte;
					int middleIterations = (thickness-beginningThickness)/maxByte;
					int endThickness = thickness - beginningThickness - middleIterations*maxByte;

					//beginning
					InsertLayer(l,c, start, beginningThickness, type);

					//middle 
					int middleStart = (start+beginningThickness) / maxByte;
					for (int i=0; i<middleIterations; i++)
						InsertLayer(l,c, (middleStart+i)*maxByte, maxByte, type);

					//end
					InsertLayer(l,c, (middleStart+middleIterations)*maxByte, endThickness, type);

					return;
				}

				//inserting over-244 values
				/*while (thickness > maxByte)
				{
					InsertLayer(l,c,start,maxByte,type);
					thickness -= maxByte;
					start += maxByte;
				}*/
				
				Line line = lines[l];
				
				//NOTE: per-column InsertLayer should be made in a reverse order!!!
				
				int end = start + thickness;

				int columnStart = line.columns[c].start;
				byte columnCount = line.columns[c].count;
				int columnEnd = columnStart + columnCount;

				ushort topLevel = line.columns[c].topLevel;

				//filling initial column if it is empty
				if (columnCount == 0)
				{
					if (type == emptyByte) return; //do nothing if inserting empty space to empty column
					
					//adding empty gap is layer is hovering
					if (start > 0) 
					{
							int gap = start-topLevel;

							//add maxbyte gaps if it is thicker then maxbyte
							while (gap > maxByte)
							{
								line.types.Insert(columnEnd,emptyByte);
								line.level.Insert(columnEnd,maxByte);
								columnCount++;
								columnEnd++;

								gap -= maxByte;
							}

							//add (remaining) gap
							line.types.Insert(columnEnd,emptyByte);
							line.level.Insert(columnEnd,(byte)gap);
							columnCount++;
							columnEnd++;
					}

					//inserting
					line.types.Insert(columnEnd,type);
					line.level.Insert(columnEnd,(byte)thickness);
					columnCount++;

					//area.columns[columnNum].count = columnCount;
					//area.columns[columnNum].topLevel = (ushort)end; //(ushort)(thickness + (start-topLevel));
					//area.columns[columnNum].topType = type;
				}


				//inserting if start is higher then  column top
				else if (start >= topLevel)
				{
					//extending previous layer if types match and no gap exists
					if (start == topLevel && line.types[columnEnd-1] == type && line.level[columnEnd-1]+thickness<maxByte)
					{
						line.level[columnEnd-1] += (byte)thickness;
					}

					else
					{
						//adding empty gap is layer is hovering
						if (start > topLevel)
						{
							int gap = start-topLevel;

							//add maxbyte gaps if it is thicker then maxbyte
							while (gap > maxByte)
							{
								line.types.Insert(columnEnd,emptyByte);
								line.level.Insert(columnEnd,maxByte);
								columnCount++;
								columnEnd++;

								gap -= maxByte;
							}

							//add (remaining) gap
							line.types.Insert(columnEnd,emptyByte);
							line.level.Insert(columnEnd,(byte)gap);
							columnCount++;
							columnEnd++;
						}

						//inserting layer in common case
						line.types.Insert(columnEnd,type);
						line.level.Insert(columnEnd,(byte)thickness);
						columnCount++;
					}

					//area.columns[columnNum].count = columnCount;
				}


				//inserting somewhere in a middle
				else
				{
				
					//finding index to insert
					int preIndex = columnStart;
					int preHeight = 0; //total height *before* prevIndex
					byte preType = 0; 
					int preThickness = 0; //current layer thickness (the one we are inserting in)
				
					for (int i=columnStart; i<columnEnd; i++)
					{
						preThickness = line.level[i];
						preIndex = i;
						preType = line.types[i];
						if (preHeight+preThickness > start) break;
						preHeight += preThickness;
					}

					//finding index of end
					int postIndex = columnStart;
					int postHeight = preHeight;
					byte postType = preType; 
					int postThickness = preThickness;

					if (start-preHeight + thickness > preThickness) //if pre layer != post layer
					{
					
						for (int i=preIndex; i<columnEnd; i++)
						{
							postThickness = line.level[i];
							postIndex = i;
							postType = line.types[i];
							if (postHeight+postThickness>end || i==columnEnd-1) break;
							postHeight += postThickness;
						}
					}

					//removing all layers in-between (except one - we will use it as new assigned)
					int layersToRemove = postIndex-preIndex;
					if (layersToRemove > 0)
					{
						line.types.RemoveRange(preIndex+1, layersToRemove);
						line.level.RemoveRange(preIndex+1, layersToRemove);
						columnCount -= (byte)layersToRemove;
					}

					//setting layer itself (at the place of pre-layer)
					line.types[preIndex] = type; //area.types.Insert(formerIndex, type);
					line.level[preIndex] = (byte)thickness; //area.level.Insert(formerIndex, (byte)thickness);

					//inserting the end layer (if any) goes before returning former layer (to avoid index changing)
					int remainPostThickness = postHeight + postThickness - end;
					if (remainPostThickness > 0)
					{
						if (postType == type  &&  preThickness<maxByte  &&  thickness<maxByte) //if type is the same - just extending current level
						{
							line.level[preIndex] += (byte)remainPostThickness;
						}
						else
						{
							line.types.Insert(preIndex+1, postType);
							line.level.Insert(preIndex+1, (byte)remainPostThickness);
							columnCount++;
						}
					}

					//if added layer is the last layer (this one is index-dependent too)
					/*if (preIndex == columnStart+columnCount-1) //do not use columnEnd since it's count changed
					{
						if (topLevel < end) area.columns[columnNum].topLevel = (ushort)end;
						area.columns[columnNum].topType = type;
					}*/

					//inserting back the beginning of former layer (if any)
					int remainPreThickness = start-preHeight;
					if (remainPreThickness > 0)
					{
						if (preType == type  &&  preThickness<maxByte  &&  thickness<maxByte)
						{
							line.level[preIndex] += (byte)remainPreThickness;
						}
						else
						{
							line.types.Insert(preIndex, preType);
							line.level.Insert(preIndex, (byte)remainPreThickness);
							columnCount++;
						}
					}
				} //instering in a middle
					
				line.columns[c].count = columnCount;

				RepairColumn(l,c);
			}


			public void RepairColumn (int l, int c) //for testing purpose
			{
				Line line = lines[l];
				
				int columnCount = line.columns[c].count;
				int columnStart = line.columns[c].start;

				//removing zero levels
				for (int i=columnStart+columnCount-1; i>=columnStart; i--) //use columnStart+columnCount-1 instead of 'columnLast' as column count is changing
				{
					byte curLevel = line.level[i];
					if (curLevel == 0)
					{
						line.types.RemoveAt(i);
						line.level.RemoveAt(i);
						columnCount--;
						continue;
					}
				}

				//removing zeroes at the top
				bool atTop = true;
				for (int i=columnStart+columnCount-1; i>=columnStart; i--)
				{
					byte curType = line.types[i];
					if (curType != emptyByte) atTop = false;
					if (atTop && curType==emptyByte)
					{
						line.types.RemoveAt(i);
						line.level.RemoveAt(i);
						columnCount--;
						continue;
					}
				}

				//merging duplicate layers
				for (int i=columnStart+columnCount-1; i>columnStart; i--)
				{
					byte curType = line.types[i];
					byte prevType = line.types[i-1];
					if (prevType == curType) 
					{
						int curLevel = line.level[i];
						int prevLevel = line.level[i-1];
						int sum = curLevel + prevLevel;

						if (sum >= maxByte) continue; //do not merge if this will exceed max byte

						line.level[i-1] = (byte)sum;
						line.types.RemoveAt(i);
						line.level.RemoveAt(i);
						columnCount--;
						continue;
					}
				}

				//finding top point
				int topPoint = 0;
				byte topType = emptyByte;
				int columnEnd = columnStart+columnCount;
				for (int i=columnStart; i<columnEnd; i++)
				{
					topPoint += line.level[i];
					topType = line.types[i];
				}
				if (columnCount==0) {topType=emptyByte; topPoint=0; }

				line.columns[c].count = (byte)columnCount;
				line.columns[c].topType = topType;
				line.columns[c].topLevel = (ushort)topPoint;
			}


			public void Polish (short[][] protrudes=null, bool ignoreObjects=true)	//TODO: skip out-of-rect columns //\param ignoreObjects will not change a column if it has object on top
			{
				if (protrudes==null) protrudes = new short[lines.Length][];

				//finding protruding height
				for (int l=1; l<lines.Length-1; l++)
				{
					Line line = lines[l];

					if (protrudes[l] == null) protrudes[l] = new short[line.columns.Length];
						
					for (int c=1; c<line.columns.Length-1; c++)
					{
						protrudes[l][c] = 0; //resetting protrude if it's not clear

						ushort level = line.columns[c].topLevel;
						ushort level_x = line.columns[c-1].topLevel;
						ushort level_X = line.columns[c+1].topLevel;
						ushort level_z = lines[l-1].columns[c].topLevel;
						ushort level_Z = lines[l+1].columns[c].topLevel;

						ushort maxLevel = level_x;
						if (level_X > maxLevel) maxLevel = level_X;
						if (level_z > maxLevel) maxLevel = level_z;
						if (level_Z > maxLevel) maxLevel = level_Z;

						ushort minLevel = level_x;
						if (level_X < minLevel) minLevel = level_X;
						if (level_z < minLevel) minLevel = level_z;
						if (level_Z < minLevel) minLevel = level_Z;

						short protrude = 0;
						if (level > maxLevel) protrude = (short)(level-maxLevel);
						else if (level < minLevel) protrude = (short)(level-minLevel);

						//skipping column if it has object on top
						if (protrude!=0 && ignoreObjects)
						{
							ulong hash =  (((ulong)level & 0xFFFFFFF) << 32)  |  (((ulong)l & 0xFFFF) << 16)  |  ((ulong)c & 0xFFFF);
							if (objects.ContainsKey(hash)) protrude = 0;
						}

						protrudes[l][c] = protrude;
					}
				}

				//clamping protruded
				for (int l=1; l<lines.Length-1; l++)
				{
					Line line = lines[l];
					for (int c=line.columns.Length-1; c>=0; c--)
					{
						if (line.columns[c].count == 0) continue; //skipping empty columns
						
						short lev = protrudes[l][c];

						if (lev>0) 
						{
							InsertLayer(l,c, line.columns[c].topLevel-lev, lev, emptyByte);
						}
						else if (lev<0) 
						{
							if (lev<-1) continue; //do no ADD mor than one block
							InsertLayer(l,c, line.columns[c].topLevel, -lev, line.columns[c].topType);
						}

						//old-style mode with no InsetLayer
						//int columnEnd = line.columns[c].start + line.columns[c].count - 1;
						//if (line.level[columnEnd] <= lev) continue; //skipping "stones", preventing negative levels
						//line.level[columnEnd] = (byte)(line.level[columnEnd] - lev);
						//line.columns[c].topLevel -= lev;
					}
				}

				SetColumnsStarts();
			}

			public void RemoveThinLayers (int minThickness)		//TODO: skip out-of-rect columns
			{
				for (int l=1; l<lines.Length-1; l++)
				{
					Line line = lines[l];
				
					for (int c=line.columns.Length-1; c>=0; c--)
					{
						int columnStart = line.columns[c].start;
						int columnCount = line.columns[c].count;
						int columnEnd = columnStart + columnCount;

						for (int i=columnStart; i<columnEnd; i++)
						{
							if (line.types[i] == emptyByte)
							{
								//looking for upper layer level sum
								int levelSum = 0;
								for (int j=i+1; j<columnEnd; j++)
								{
									if (line.types[j]==emptyByte) break;
									else levelSum += line.level[j];
									if (levelSum > minThickness) continue;
								}

								if (levelSum > minThickness) continue;

								//removing
								for (int j=i+1; j<columnEnd; j++)
								{
									if (line.types[j]==emptyByte) break;
									else line.level[j] = 0;
								}
								//InsertLayer(area, c, i, levelSum, 0);
							}
						}
						RepairColumn(l,c);
					}
				}
				SetColumnsStarts();
			}

			public void AddLayer (byte level, byte type, float heightFactor=1, Noise noise=null)
			{
				for (int l=0; l<rect.size.x; l++)
				{
					Line line = lines[l];
				
					for (int c=line.columns.Length-1; c>=0; c--)
					{
						short iHeight = (short)line.columns[c].topLevel;
						InsertLayer(l,c, iHeight, level, type);
					}
				}
				SetColumnsStarts();
			}
			
			public void AddLayer (Matrix thicknessMatrix, int type, float heightFactor=1, Noise noise=null)
			{
				#if WDEBUG
				if (thicknessMatrix.rect != rect) Debug.LogError("Rects differ: " + thicknessMatrix.rect + ", " + rect);
				#endif

				if (noise == null) noise = new Noise(1234, 64);

				//CoordRect mrect = thicknessMatrix.rect;
				byte iType = (byte)type; //TODO: why duplicate type?
				
				for (int l=0; l<rect.size.x; l++)
				{
					Line line = lines[l];
				
					for (int c=line.columns.Length-1; c>=0; c--)
					{
						float fThickness = thicknessMatrix.array[c*rect.size.x + l] * heightFactor; //note that array num is used here, not matrix coordinates
						short iThickness = (short)fThickness;	
						if (noise != null && noise.Random(l,c) < fThickness-iThickness) iThickness++;
						
						short iHeight = (short)line.columns[c].topLevel;

						InsertLayer(l,c, iHeight, iThickness, iType);
					}
				}
				SetColumnsStarts();
			}
			public void AddLayer (int offsetX, int offsetZ, int res, float[] thicknessArray, int type, float heightFactor=1) 
				{ AddLayer(new Matrix(new CoordRect(offsetX,offsetZ,res,res), thicknessArray), type, heightFactor); }


			public void SetLayer (Matrix thicknessMatrix, Matrix heightsMatrix, int type, float heightFactor=1, Noise noise=null)
			{
				#if WDEBUG
				if (thicknessMatrix.rect != rect) Debug.LogError("Rects differ: " + thicknessMatrix.rect + ", " + rect);
				if (heightsMatrix != null && heightsMatrix.rect != rect) Debug.LogError("Rects differ: " + thicknessMatrix.rect + ", " + rect);
				#endif

				if (noise == null) noise = new Noise(1234, 64);

				//CoordRect rect = thicknessMatrix.rect;
				byte iType = (byte)type; //TODO: why duplicate type?
				
				for (int l=0; l<rect.size.x; l++)
				{
					Line line = lines[l];
				
					for (int c=line.columns.Length-1; c>=0; c--)
					{
						int i = c*rect.size.x + l;
						
						float fHeight = heightsMatrix==null? 0 : heightsMatrix.array[i] * heightFactor;
						short iHeight = (short)fHeight;		
						if (noise != null && noise.Random(l,c) < fHeight-iHeight) iHeight++;

						float fSum = fHeight + thicknessMatrix.array[i]*heightFactor;
						short iSum = (short)fSum;
						if (noise != null && noise.Random(l,c) < fSum-iSum) iSum++;

						short iThickness = (byte)(iSum - iHeight);

						InsertLayer(l,c, iHeight, iThickness, iType);
					}
				}
				SetColumnsStarts();
			}
			public void SetLayer (int offsetX, int offsetZ, int res, float[] thicknessArray, float[] heightsArray, int type, float heightFactor=1) 
				{ SetLayer(new Matrix(new CoordRect(offsetX,offsetZ,res,res), thicknessArray), new Matrix(new CoordRect(offsetX,offsetZ,res,res), heightsArray), type, heightFactor); }


			public void ClampAppendLayer (Matrix thicknessMatrix, int type, float heightFactor=1, Noise noise=null)
			{
				#if WDEBUG
				if (thicknessMatrix.rect != rect) Debug.LogError("Rects differ: " + thicknessMatrix.rect + ", " + rect);
				#endif

				if (noise == null) noise = new Noise(1234, 64);

				//CoordRect rect = thicknessMatrix.rect;
				byte iType = (byte)type;
				
				for (int l=0; l<rect.size.x; l++)
				{
					Line line = lines[l];
				
					for (int c=line.columns.Length-1; c>=0; c--)
					{
						float fThickness = thicknessMatrix.array[c*rect.size.x + l] * heightFactor;
						int iThickness = (int)fThickness; 
						if (noise != null && noise.Random(l,c) < fThickness-iThickness) iThickness++;
								
						short iHeight = (short)line.columns[c].topLevel;
						//iThickness = (int)(iThickness-iHeight);
						
						if (iHeight > iThickness) InsertLayer(l,c, iThickness, (short)(iHeight-iThickness), emptyByte); //clamping
						else					  InsertLayer(l,c, iHeight, (short)(iThickness-iHeight), iType); //appending
					}
				}
				SetColumnsStarts();
			}
			public void ClampAppendLayer (int offsetX, int offsetZ, int res, float[] thicknessArray, int type, float heightFactor=1) 
				{ ClampAppendLayer(new Matrix(new CoordRect(offsetX,offsetZ,res,res), thicknessArray), type, heightFactor); }


			public void PaintLayer (Matrix matrix, int type, float paintThickness=1, Noise noise=null)
			{
				#if WDEBUG
				if (matrix.rect != rect) Debug.LogError("Rects differ: " + matrix.rect + ", " + rect);
				#endif

				if (noise == null) noise = new Noise(1234, 64);

				//CoordRect rect = matrix.rect;
				byte iType = (byte)type;
				
				for (int l=0; l<rect.size.x; l++)
				{
					Line line = lines[l];
				
					for (int c=line.columns.Length-1; c>=0; c--)
					{
						float fThickness = matrix.array[c*rect.size.x + l] * paintThickness;
						short iThickness = 0;

						if (fThickness <= 0.5f)
						{
							float randomPart = fThickness*2;
							if (randomPart<0) randomPart=0; if (randomPart>1) randomPart=1;
							if (noise != null && noise.Random(l,c) < randomPart ) iThickness = 1;
						}
						else
						{
							float thickPart = (fThickness-0.5f)*2;
							if (thickPart<0) thickPart=0; if (thickPart>1) thickPart=1;
							iThickness = (short)(thickPart*paintThickness + 0.5f); 
						}

						short topLevel = (short)line.columns[c].topLevel;
						if (iThickness > topLevel) iThickness = topLevel;
						short iHeight = (short)(topLevel - iThickness);

						InsertLayer(l,c, iHeight, iThickness, iType);
					}
				}
				SetColumnsStarts();
			}
			public void PaintLayer (int offsetX, int offsetZ, int res, float[] array, int type, float paintThickness=1) 
				{ PaintLayer(new Matrix(new CoordRect(offsetX,offsetZ,res,res), array), type, paintThickness); }


			public void AddObject (CoordDir coord, short type)
			{
				ulong hash =  (((ulong)coord.y & 0xFFFFFFF) << 32)  |  (((ulong)(coord.x-rect.offset.x) & 0xFFFF) << 16)  |  ((ulong)(coord.z-rect.offset.z) & 0xFFFF);

				if (!objects.ContainsKey(hash)) objects.Add(hash, type);
				else objects[hash] = type;
			}
			public void AddObject (int x, int y, int z, byte dir, short type) { AddObject(new CoordDir(x,y,z,dir), type); }


			public void SetGrassLayer (Matrix matrix, byte type, float density=1, Noise noise=null, int layerNum=0, Matrix mask=null)
			{
				#if WDEBUG
				if (matrix.rect != rect) Debug.LogError("Rects differ: " + matrix.rect + ", " + rect);
				#endif
				
				if (noise == null) noise = new Noise(1238, 64);
				//CoordRect rect = matrix.rect;

				for (int x=0; x<rect.size.x; x++)
					for (int z=0; z<rect.size.z; z++)
					{
						int pos = z*rect.size.x + x;

						float fVal = matrix.array[pos] * density;
						if (mask!=null) fVal *= mask.array[pos];
						bool bVal = false;

						float noiseVal = noise.Random(x,z,layerNum);
						if (noise!=null && noiseVal < fVal) bVal=true;  

						if (bVal) grass[pos] = type;
					}
			}
			public void SetGrassLayer (int offsetX, int offsetZ, int res, float[] array, byte type, float density=1, int layerNum=0, float[] maskArray=null)
				{ SetGrassLayer (new Matrix(new CoordRect(offsetX,offsetZ,res,res), array), type, density:density, layerNum:layerNum, mask:maskArray==null? null : new Matrix(new CoordRect(offsetX,offsetZ,res,res), maskArray)); }


			public void MixAreas (Area[] areas, Matrix[] opacities)
			{
				#if WDEBUG
				if (areas.Length != opacities.Length) Debug.LogError("Areas Length:" + areas.Length + " Opacities Length:" + opacities.Length);
				for (int i=0; i<areas.Length; i++)
					if (opacities[i]!=null && (areas[i].rect!=rect || opacities[i].rect!=rect))
						Debug.LogError("Rect mismatch: " + i + " Rect:" + rect + " Area:" + areas[i].rect + " Opacities:" + opacities[i].rect);
				#endif

				ClearLand();

				//copy area if some of opacities is null
				for (int i=0; i<areas.Length; i++)
					if (opacities[i] == null)
					{
						CopyLand(areas[i],this);
						return; //if an area sent without opacity matrix then it's opacity is 1. Cannot mix in anything else if the opacity is 1.
					}

				//calculating desired heights
				Matrix heights = new Matrix(rect);

				for (int l=0; l<rect.size.x; l++)
				{
					Line line = lines[l];
					for (int c=0; c<line.columns.Length; c++)
					{
						int matrixPos = c*rect.size.x + l;
						for (int i=0; i<areas.Length; i++)
						{
							if (opacities[i]==null) continue;
							float opacity = opacities[i].array[matrixPos];
							if (opacity < float.Epsilon) continue;

							float height = lines[l].columns[c].topLevel;
							heights.array[matrixPos] += height*opacity;
						}
					}
				}

				//mixing
				for (int l=0; l<rect.size.x; l++)
				{
					Line line = lines[l];

					line.types.Clear();
					line.level.Clear();

					for (int c=0; c<line.columns.Length; c++)
					{
						int matrixPos = c*rect.size.x + l;

						//calculating desired height and area that should be used for this column
						float height = 0;
						float maxOpacity = 0;
						int maxArea = 0;

						for (int i=0; i<areas.Length; i++)
						{
							if (opacities[i]==null) continue;
							float opacity = opacities[i].array[matrixPos];
							if (opacity < float.Epsilon) continue;

							int top = areas[i].lines[l].columns[c].topLevel;
							if (top==0) continue;
							height += top * opacity;

							if (opacity > maxOpacity) { maxOpacity = opacity; maxArea = i; }
						}

						if (maxOpacity<Mathf.Epsilon) continue; //no biomes are applied to this coordinate

						//max opacity column //TODO: consider selecting columns at random
						Line mLine = areas[maxArea].lines[l];
						int mLevel = mLine.columns[c].topLevel;
						if (mLevel==0) continue; //skipping empty column, otherwise division by zero

						//copy column to current area with scaling
						float heightFactor = height / areas[maxArea].lines[l].columns[c].topLevel;
						int mColumnStart = mLine.columns[c].start;
						byte mColumnCount = mLine.columns[c].count;

						line.columns[c].start = line.types.Count;
						line.columns[c].count = mColumnCount;

						for (int b=mColumnStart; b<mColumnStart+mColumnCount; b++)
						{
							line.types.Add(mLine.types[b]);
							line.level.Add((byte)(mLine.level[b]*heightFactor + 0.5f));
								
							//TODO: does not handle over-255
							#if WDEBUG
							if (mLine.level[b]*heightFactor+1 > maxByte) 
								Debug.LogError("Level is more than 255: " + mLine.level[b]*heightFactor+1);
							#endif
						}

						//fixing top type and point
						int topPoint = 0;
						byte topType = emptyByte;
						int columnStart = line.columns[c].start;
						int columnEnd = columnStart+mColumnCount;
						for (int i=columnStart; i<columnEnd; i++)
						{
							topPoint += line.level[i];
							topType = line.types[i];
						}
						if (mColumnCount==0) { topPoint=0; topType=emptyByte; }
						line.columns[c].topType = topType;
						line.columns[c].topLevel = (ushort)topPoint;
					}
				}
			}
			public void MixAreas (Area[] areas, int offsetX, int offsetZ, int res, float[][] opacitiesArrays)
			{ 
				Matrix[] opacities = new Matrix[opacitiesArrays.Length];
				for (int i=0; i<opacities.Length; i++) opacities[i] = new Matrix(new CoordRect(offsetX,offsetZ,res,res), opacitiesArrays[i]);
				MixAreas(areas, opacities);
			}


			public static void CopyLand (Area src, Area dst)
			{
				#if WDEBUG
				if (src.rect != dst.rect)
					Debug.Log("Rect Mismatch on area copy");
				#endif

				for (int l=0; l<src.lines.Length; l++) 
				{
					Line srcLine = src.lines[l];
					Line dstLine = dst.lines[l];
					for (int c=0; c<src.lines[l].columns.Length; c++)
					{
						dstLine.columns[c] = srcLine.columns[c];
					}
					dstLine.level.Clear(); dstLine.level.AddRange(srcLine.level);
					dstLine.types.Clear(); dstLine.types.AddRange(srcLine.types);
				}
			}

			public bool Check ()
			{
				if (lines.Length != rect.size.x) { Debug.LogError("Num Lines is not equal to rect size: " + lines.Length + ", " + rect.size.x); return false; }
				
				for (int l=0; l<rect.size.x; l++)
				{
					Line line = lines[l];
					if (line.columns.Length != rect.size.z) { Debug.LogError("Num Columns is not equal to rect size: " + line.columns.Length + ", " + rect.size.z); return false; }
					if (line.types.Count != line.level.Count) { Debug.LogError("Type and Level count differ: " + line.types.Count + ", " + line.level.Count); return false; }

					int counter = 0;
					for (int c=0; c<line.columns.Length; c++)
					{
						int columnStart = line.columns[c].start;
						byte columnCount = line.columns[c].count;

						if (columnStart != counter) { Debug.LogError("Columns start is not equal sum: " + columnStart + ", " + counter); return false; }
						counter += columnCount;
						if (counter > line.types.Count) { Debug.LogError("Columns start+count is larger than list: " + counter + ", " + line.types.Count); return false; }

						byte lastType = 255;
						int topPoint = 0;
						byte topType = emptyByte;
						for (int b=columnStart; b<columnStart+columnCount; b++)
						{
							if (line.level[b] == 0) { Debug.LogError("Levels contain zero"); return false; }
							if (line.types[b] == lastType && line.level[b]<Data.maxByte)  { Debug.LogError("Two same types:" + lastType); return false; }
							lastType = line.types[b];

							topPoint += line.level[b];
							topType = line.types[b];
						}

						if (line.columns[c].topType != topType)  { Debug.LogError("Top Type error:" + line.columns[c].topType + ", " + topType); return false; }
						if (line.columns[c].topLevel != (ushort)topPoint) { Debug.LogError("Top Level error:" + line.columns[c].topLevel + ", " + topPoint); return false; }
						if (topType==emptyByte && topPoint!=0)  { Debug.LogError("Top Type zero"); return false; }
					}
				}

				return true;
			}

			public Matrix GetSoilMatrix (float[] opacities) //opacities length = blockTypes length, if block is skipped opcaity 0
			{
				Matrix soil = new Matrix(rect);

				for (int l=0; l<rect.size.x; l++)
				{
					Line line = lines[l];
					for (int c=0; c<line.columns.Length; c++)
					{
						int pos = c*rect.size.x + l;
						byte type = line.columns[c].topType;
						
						if (type>=opacities.Length) soil.array[pos] = 0;
						else soil.array[pos] = opacities[type];
					}
				}

				return soil;
			}
			
			#region Test and Debug
			
				public static ulong GetObjectHash (CoordDir coord)
				{
					return (((ulong)coord.y & 0xFFFFFFF) << 32)  |  (((ulong)coord.x & 0xFFFF) << 16)  |  ((ulong)coord.z & 0xFFFF);
				}

				public static CoordDir GetObjectCoord (ulong hash)
				{
					return new CoordDir( (int)((hash >> 16) & 0xFFFF),  (int)((hash >> 32) & 0xFFFFFFF),  (int)(hash & 0xFFFF) );
				}

				public static byte[] SerializeHash (ulong hash)
				{
					return new byte[] {  (byte)((hash >> 56) & 0xFF),  (byte)((hash >> 48) & 0xFF),  (byte)((hash >> 40) & 0xFF),  (byte)((hash >> 32) & 0xFF),  
										 (byte)((hash >> 24) & 0xFF),  (byte)((hash >> 16) & 0xFF),  (byte)((hash >> 8) & 0xFF),  (byte)(hash & 0xFF)  };
				}

				public static ulong DeserializeHash (byte[] bytes)
				{
					return ((ulong)bytes[0] << 56) | ((ulong)bytes[1] << 48) | ((ulong)bytes[2] << 40) | ((ulong)bytes[3] << 32) |
						   ((ulong)bytes[4] << 24) | ((ulong)bytes[5] << 16) | ((ulong)bytes[6] << 8)  | (ulong)bytes[7];
				}


				public string LogColumn (int l, int c)
				{
					Area.Line line = lines[l];
					Area.Line.Column column = line.columns[c];

					string result = "Structure: "; 
					for (int i=column.start; i<column.start+column.count; i++) 
						result += line.types[i].ToString() + "(" + line.level[i].ToString() + "), ";

					result += "Blocks: "; 
					for (int y=0; y<column.topLevel; y++)
						result += GetBlock(l,c,y).ToString()+",";

					return result;
				}


			#endregion

		}//area

		public static readonly byte emptyByte = 240;
		public static readonly byte constructorByte = 230;
		//public static readonly byte minLandByte = 1; //TODO: byte 0 should not be in land types list
		//public static readonly byte maxLandByte = 32;
		//public static readonly byte minObjectByte = 33;
		//public static readonly byte maxObjectByte = 128;
		//public static readonly byte minConstructorByte = 129;
		//public static readonly byte maxConstructorByte = 243;
		//public static readonly byte minGrassByte = 1;

		public static readonly byte maxByte = 244; //larger value is service information
		[System.NonSerialized] public ChunkGrid<Area> areas = new ChunkGrid<Area>();
		public Generator generator = new Generator();

		public int areaSize = 512;

		//public void Init (int cellSize) { areas = new ChunkGrid<Area>(); } //TODO better use factory pattern instead

		/*public bool Generated 
		{get{
			foreach (Area area in areas.All())
			{
				if (!area.generateWorker.ready) 
					return false;
			}
			return true;
		}}*/


		#region Single block operations

			public byte GetBlock (int x, int y, int z)
			{
				//finding area
				Coord areaCoord = Coord.PickCell(x,z,areaSize);
				Area area = areas[areaCoord];
				if (area==null) return emptyByte;

				//finding column
				int l = x-area.rect.offset.x;
				int c = z-area.rect.offset.z;
				Area.Line line = area.lines[l];
				Area.Line.Column column = line.columns[c];
			
				//getting block
				int layer = 0;
				for (int i=0; i<column.count; i++)
				{
					layer += line.level[i+column.start];
					if (layer > y) return line.types[i+column.start];
				}

				return emptyByte;
			}

		
			public void SetBlock (int x, int y, int z, byte type, bool pin=false)
			{
				//finding area
				Coord areaCoord = Coord.PickCell(x,z,areaSize);
				Area area = areas[areaCoord];
				if (area==null) return;
				areas.Create(area.coord, parent:this, pin:true);

				//finding column  num
				int l = x-area.rect.offset.x;
				int c = z-area.rect.offset.z;

				area.InsertLayer(l,c,y, 1, type);
				area.SetColumnsStarts();
			}

			public int GetObject (int x, int y, int z)
			{
				//finding area
				Coord areaCoord = Coord.PickCell(x,z,areaSize);
				Area area = areas[areaCoord];
				if (area==null) return -1; //TODO: emptyByte?
				areas.Create(area.coord, parent:this, pin:true);

				//finding hash
				ulong hash =  (((ulong)y & 0xFFFFFFF) << 32)  |  (((ulong)(x-area.rect.offset.x) & 0xFFFF) << 16)  |  ((ulong)(z-area.rect.offset.z) & 0xFFFF);

				//object
				if (!area.objects.ContainsKey(hash)) return -1;
				else return area.objects[hash];
			}

			public void SetObject (int x, int y, int z, int type)
			{
				//finding area
				Coord areaCoord = Coord.PickCell(x,z,areaSize);
				Area area = areas[areaCoord];
				if (area==null) return;
				areas.Create(area.coord, parent:this, pin:true);

				//finding hash
				ulong hash =  (((ulong)y & 0xFFFFFFF) << 32)  |  (((ulong)(x-area.rect.offset.x) & 0xFFFF) << 16)  |  ((ulong)(z-area.rect.offset.z) & 0xFFFF);

				//removing
				if (type < 0)
				{
					if (area.objects.ContainsKey(hash)) area.objects.Remove(hash);
				}

				//adding
				else
				{
					if (!area.objects.ContainsKey(hash)) area.objects.Add(hash, (short)type);
					else area.objects[hash] = (short)type;
				}
			}

			public void SetGrass (int x, int z, byte type)
			{
				//finding area
				Coord areaCoord = Coord.PickCell(x,z,areaSize);
				Area area = areas[areaCoord];
				if (area==null) return;
				areas.Create(area.coord, parent:this, pin:true);

				area.grass[ (z-area.rect.offset.z)*area.rect.size.x + x - area.rect.offset.x ] = type;
			}

			public byte GetGrass (int x, int z)
			{
				//finding area
				Coord areaCoord = Coord.PickCell(x,z,areaSize);
				Area area = areas[areaCoord];
				if (area==null) return emptyByte;
				areas.Create(area.coord, parent:this, pin:true); //TODO: why create area???

				return area.grass[ (z-area.rect.offset.z)*area.rect.size.x + x - area.rect.offset.x ];
			}

		#endregion

		#region Matrix Operations

			public void FillMatrix (Matrix3<byte> matrix) //aka GetMatrix
			{
				//TODO this fn does not reset matrix
				CoordDir min = matrix.cube.Min; CoordDir max = matrix.cube.Max;

				matrix.Fill(emptyByte);

				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
				{
					//finding area
					Coord areaCoord = Coord.PickCell(x,z,areaSize);
					Area area = areas[areaCoord];
					if (area==null)
					{
						for (int ty=matrix.cube.offset.y; ty<matrix.cube.offset.y+matrix.cube.size.y; ty++)
							matrix[x,ty,z] = emptyByte;
						continue;
					}

					//finding column
					int l = x-area.rect.offset.x;
					int c = z-area.rect.offset.z;
					Area.Line line = area.lines[l];
					Area.Line.Column column = line.columns[c];

					//filling matrix
					int y = 0;
					for (int i=0; i<column.count; i++)
					{
						byte type = line.types[i+column.start];
						byte level = line.level[i+column.start];
						
						for (int tmp=0; tmp<level; tmp++)
						{
							if (y>=max.y) break;
							if (y>=min.y) matrix[x,y,z] = type;
							y++;
						}
					}
				}
			}

			public void SetMatrix (Matrix3<byte> matrix, bool pin=false)
			{
				CoordRect rect = matrix.cube.rect; //new CoordRect(matrix.offsetX, matrix.offsetZ, matrix.sizeX, matrix.sizeZ);

				Coord min = rect.Min; Coord max = rect.Max;

				for (int x=max.x-1; x>=min.x; x--)
					for (int z=max.z-1; z>=min.z; z--)
				{
					//finding area
					Coord areaCoord = Coord.PickCell(x,z,areaSize);
					Area area = areas[areaCoord];
					if (area==null) return;
					areas.Create(area.coord, parent:this, pin:true);

					//finding column
					int l = x-area.rect.offset.x;
					int c = z-area.rect.offset.z;

					int thickness = 0;
					byte type = matrix[x,matrix.cube.offset.y,z];
					for (int y=matrix.cube.offset.y; y<matrix.cube.offset.y+matrix.cube.size.y; y++)
					{
						byte curType =  matrix[x,y,z];

						if (curType!=type)
						{
							area.InsertLayer(l,c,y-thickness, thickness, type);
							thickness = 0;
							type = curType;
						}
						
						if (y==matrix.cube.offset.y+matrix.cube.size.y-1)
						{
							area.InsertLayer(l,c,y-thickness, thickness+1, type);
						}
						
						thickness++;
					}

					//for (int y=matrix.offsetY; y<matrix.offsetY+matrix.sizeY; y++)
					//	area.InsertLayer(l,c,y, 1, matrix[x,y,z]); //TODO not optimized here, call InsertLayer on type change only
				}

				CoordRect areasRect = CoordRect.PickIntersectingCells(rect, areaSize);
				foreach (Area area in areas.WithinRect(areasRect))
					area.SetColumnsStarts();

				#if WDEBUG
				foreach (Area area in areas.WithinRect(areasRect))
					area.Check();
				#endif
			}	

			public void FillHeightmap (Matrix heightmap) //aka GetHeightmap
			{
				Coord min = heightmap.rect.Min; Coord max = heightmap.rect.Max;
				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
				{
					//finding area
					Coord areaCoord = Coord.PickCell(x,z,areaSize);
					Area area = areas[areaCoord];
					if (area==null) { heightmap[x,z] = 0; continue; }

					//finding column  num
					int l = x-area.rect.offset.x;
					int c = z-area.rect.offset.z;

					//top level
					heightmap[x,z] = area.lines[l].columns[c].topLevel;
				}
			}

			public void FillHeightmap (Matrix2<ushort> heightmap)
			{
				Coord min = heightmap.rect.Min; Coord max = heightmap.rect.Max;
				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
				{
					//finding area
					Coord areaCoord = Coord.PickCell(x,z,areaSize);
					Area area = areas[areaCoord];
					if (area==null) { heightmap[x,z] = 0; continue; }

					//finding column  num
					int l = x-area.rect.offset.x;
					int c = z-area.rect.offset.z;

					//top level
					heightmap[x,z] = area.lines[l].columns[c].topLevel;
				}
			}

			public void FillExistMatrix (Matrix3<float> matrix) //if exists sets 1, if not - 0
			{
				for (int i=0; i<matrix.array.Length; i++) matrix.array[i] = 0; //TODO resets full array (while matrix count can be less)
				
				CoordDir min = matrix.cube.Min; CoordDir max = matrix.cube.Max;
				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
				{
					//finding area
					Coord areaCoord = Coord.PickCell(x,z,areaSize);
					Area area = areas[areaCoord];
					if (area==null)
					{
						for (int ty=matrix.cube.offset.y; ty<matrix.cube.offset.y+matrix.cube.size.y; ty++)
							matrix[x,ty,z] = 0;
						continue;
					}

					//finding column
					int l = x-area.rect.offset.x;
					int c = z-area.rect.offset.z;
					Area.Line line = area.lines[l];
					Area.Line.Column column = line.columns[c];

					int y = 0;
					for (int i=0; i<column.count; i++)
					{
						byte type = line.types[i+column.start];
						if (type != emptyByte) type = 1;
						else type = 0;
						
						byte level = line.level[i+column.start];
						
						for (int tmp=0; tmp<level; tmp++)
						{
							if (y>=max.y) break;
							if (y>=min.y) matrix[x,y,z] = type;
							y++;
						}
					}
				}
			}

			public void FillGrass (Matrix2<byte> matrix)
			{
				Coord min = matrix.rect.Min; Coord max = matrix.rect.Max;
				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
				{
					//finding area
					Coord areaCoord = Coord.PickCell(x,z,areaSize);
					Area area = areas[areaCoord];
					if (area==null) { matrix[x,z] = emptyByte; continue; }

					matrix[x,z] = area.grass[ (z-area.rect.offset.z)*area.rect.size.x + x - area.rect.offset.x ];
				}
			}

			public void SetGrass (Matrix2<byte> matrix)
			{
				Coord min = matrix.rect.Min; Coord max = matrix.rect.Max;
				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
				{
					//finding area
					Coord areaCoord = Coord.PickCell(x,z,areaSize);
					Area area = areas[areaCoord];
					if (area==null) continue;
					areas.Create(area.coord, parent:this, pin:true);

					area.grass[ (z-area.rect.offset.z)*area.rect.size.x + x - area.rect.offset.x ] = matrix[x,z];
				}
			}

			public IEnumerable<TupleSet<CoordDir,short>> ObjectsWithinRect (CoordRect rect)
			{
				CoordRect areaRect = CoordRect.PickIntersectingCells(rect, areaSize);
				foreach (Area area in areas.WithinRect(areaRect))
				{
					foreach (KeyValuePair<ulong,short> kvp in area.objects)
					{
						ulong hash = kvp.Key;
						
						int x = (int)((hash >> 16) & 0xFFFF) + area.rect.offset.x;
						int z = (int)(hash & 0xFFFF) + area.rect.offset.z;

						if (x<rect.offset.x || z<rect.offset.z || x>=rect.offset.x+rect.size.x || z>=rect.offset.z+rect.size.z) continue;

						int y = (int)((hash >> 32) & 0xFFFFFFF);
						
						yield return new TupleSet<CoordDir,short>( new CoordDir(x,y,z), kvp.Value );

					}
				}
			}

			public void RemoveObjectsByLandMatrix (Matrix3<byte> matrix)
			{
				CoordDir min = matrix.cube.Min; CoordDir max = matrix.cube.Max;

				for (int x=max.x-1; x>=min.x; x--) //don't have to go negative direction
					for (int z=max.z-1; z>=min.z; z--)
				{
					//finding area
					Coord areaCoord = Coord.PickCell(x,z,areaSize);
					Area area = areas[areaCoord];
					if (area==null) return;
					areas.Create(area.coord, parent:this, pin:true);

					//iterating cell
					for (int y=max.y-1; y>=min.y; y--)
					{
						byte type = matrix[x,y,z];
						if (type == emptyByte) continue;

						ulong hash =  (((ulong)y & 0xFFFFFFF) << 32)  |  (((ulong)(x-area.rect.offset.x) & 0xFFFF) << 16)  |  ((ulong)(z-area.rect.offset.z) & 0xFFFF);
						if (area.objects.ContainsKey(hash)) area.objects.Remove(hash);
					}
				}
			}


		#endregion


		#region Top and Bottom

			public void GetTopTypePoint (int x, int z, out int topPoint, out byte topType, Area area=null)
			{
				topPoint = 0;
				topType = emptyByte;

				//finding area
				if (area == null)
				{
					Coord areaCoord = Coord.PickCell(x,z,areaSize);
					area = areas[areaCoord];
				}
				if (area == null) return;

				//finding column  num
				int l = x-area.rect.offset.x;
				int c = z-area.rect.offset.z;

				//top level
				topPoint = area.lines[l].columns[c].topLevel;

				//top type
				topType = area.lines[l].columns[c].topType;
			}

			public void GetTopBottomPoints (CoordRect mRect, out int topPoint, out int bottomPoint, bool ignoreEmptyColumns=false)
			{
				topPoint = 0;
				bottomPoint = 20000000;

				Coord min = mRect.Min; Coord max = mRect.Max;
				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
				{
					//finding area
					Coord areaCoord = Coord.PickCell(x,z,areaSize);
					Area area = areas[areaCoord];
					if (area == null) continue;

					//finding column
					int l = x-area.rect.offset.x;
					int c = z-area.rect.offset.z;
					Area.Line line = area.lines[l];
					Area.Line.Column column = line.columns[c];

					if (column.count == 0 && ignoreEmptyColumns) continue;

					//top level
					if (column.topLevel > topPoint) topPoint = column.topLevel;

					//bottom level
					int columnBottom = line.level[column.start];
					byte bottomType = line.types[column.start];
					int i=1; while (i<column.count && line.types[column.start+i] == bottomType) { columnBottom += line.level[column.start+i]; i++; }
					if (columnBottom < bottomPoint) bottomPoint = columnBottom;
				}

				if (bottomPoint > topPoint) bottomPoint = topPoint;
				if (bottomPoint < 0) bottomPoint = 0;
			}

		#endregion

		#region Type Switch operations

			public void ReplaceType (byte o, byte n)
			{
				foreach (Area area in areas.All())
				{
					for (int l=0; l<area.lines.Length; l++)
					{
						Area.Line line = area.lines[l];

						int listCount = line.types.Count;
						for (int i=0; i<listCount; i++)
							if (line.types[i]==o) line.types[i]=n;
					}
				}
			}

			public void SwitchType (byte o, byte n)
			{
				Debug.Log("Data Switch: " + o + " " + n);
				foreach (Area area in areas.All())
				{
					for (int l=0; l<area.lines.Length; l++)
					{
						Area.Line line = area.lines[l];

						int listCount = line.types.Count;
						for (int i=0; i<listCount; i++)
						{
							byte val = 	line.types[i];
							if (val==o) line.types[i]=n;
							if (val==n) line.types[i]=o;
						}
					}
				}
			}

			public void InsertType (byte t)
			{
				foreach (Area area in  areas.All())
				{
					for (int l=0; l<area.lines.Length; l++)
					{
						Area.Line line = area.lines[l];

						int listCount = line.types.Count;
						for (int i=0; i<listCount; i++)
							if (line.types[i]>=t) line.types[i]++;
					}
				}
			}

			public void RemoveType (byte t)
			{
				foreach (Area area in areas.All())
				{
					for (int l=0; l<area.lines.Length; l++)
					{
						Area.Line line = area.lines[l];

						int listCount = line.types.Count;
						for (int i=0; i<listCount; i++)
							if (line.types[i]>=t) line.types[i]--;
					}
				}
			}

			public void ReplaceGrassType (byte o, byte n)
			{
				foreach (Area area in areas.All())
				{
					for (int i=0; i<area.grass.Length; i++)
						if (area.grass[i] == o) area.grass[i] = n;
				}
			}

			public void SwitchGrassType (byte o, byte n)
			{
				foreach (Area area in areas.All())
				{
					for (int i=0; i<area.grass.Length; i++)
					{
						byte val = 	area.grass[i];
						if (val==o) area.grass[i]=n;
						if (val==n) area.grass[i]=o;
					}
				}
			}

			public void InsertGrassType (byte t)
			{
				foreach (Area area in areas.All())
				{
					for (int i=0; i<area.grass.Length; i++)
						if (area.grass[i] >= t) area.grass[i]++; 
				}
			}

			public void RemoveGrassType (byte t) { ReplaceGrassType(t,emptyByte); }

		#endregion

		#region Debug

			
			public CoordRect GetFilledRect () //for Voxelump
			{
				return new CoordRect(); //empty data
			}


			public string LogColumn (int x, int z)
			{
				Coord areaCoord = Coord.PickCell(x,z,areaSize);
				Area area = areas[areaCoord];
				if (area == null) return "Area is empty";

				//finding column
				int l = x-area.rect.offset.x;
				int c = z-area.rect.offset.z;
				Area.Line line = area.lines[l];
				Area.Line.Column column = line.columns[c];

				string result = ""; 
				for (int i=column.start; i<column.start+column.count; i++) 
					result += line.types[i].ToString() + "(" + line.level[i].ToString() + "), ";
				return result;
			}


			public void FillPlanar (byte level, byte type)
			{
				foreach (Area area in areas.All())
				{
					for (int l=0; l<area.lines.Length; l++)
					{
						Area.Line line = area.lines[l];

						line.types.Clear();
						line.level.Clear();

						for (int c=0; c<line.columns.Length; c++)
						{
							line.columns[c].start = c;
							line.columns[c].count = 1;
							line.columns[c].topLevel = level;
							line.columns[c].topType = type;

							line.types.Add(type);
							line.level.Add(level);
						}
					}
				}
			}

			/*public void FillNoise (int offsetX, int offsetZ, int size)
			{
				InstanceRandom noise = new InstanceRandom(200, 512, 1234, 12345);

				for (int x=offsetX; x<offsetX+size; x++)
					for (int z=offsetZ; z<offsetZ+size; z++)
				{
					float result = noise.Fractal(x, z, 0.5f);
					
					if (result < 0) result = 0; 
					if (result > 1) result = 1;
									
					result = result*50;




					//finding/generating area
					Area area = GetArea(x,z);

					//finding column num (interim fns work with column num, Write Interim changes the column by setting it's count)
					int columnNum = (z-area.rect.offset.z)*area.rect.size.x + x-area.rect.offset.x;

					//writing
					ReadInterim(area, columnNum);
					for (int y=interimTypesCount; y<interimTypesCount+result; y++)
						interimTypes[y] = 1;
					interimTypesCount += (int)result;
					WriteInterim(area, columnNum);
				}
			}*/

		#endregion

		#region Serialization

			public static readonly int maxAreasCode = 245;
			public static readonly byte emptyAreaCode = 250;
			public static readonly byte filledAreaCode = 251;
			public static readonly byte endTypesLevelsCode = 252;

			public byte[] ToByteArray (bool pinnedOnly=false)
			{
				//calculating count
				int count = 3; //version
				foreach (Area area in areas.All(pinnedOnly))
				{
					count += 5; //coord.x, coord.z + 1 if area pinned

					for (int l=0; l<area.lines.Length; l++)
					{
						Area.Line line = area.lines[l];
						
						count += line.columns.Length;
						count += line.types.Count; count ++; //endTypesLevelsCode
						count += line.level.Count; count ++;
					}

					//objects
					count += area.objects.Count * 9;
					count ++;
					
					//grass
					count += area.rect.size.x*area.rect.size.z;
					count ++;
				}

				//creating array
				byte[] bytes = new byte[count+2];

				//version number
				bytes[0]=5; bytes[1]=0; bytes[2]=0;

				//area size
				bytes[3] = (byte)(areaSize/255);
				bytes[4] = (byte)(areaSize%255);

				//filling array
				int counter = 5;
				foreach (Area area in areas.All(pinnedOnly))
				{
					short ax = (short)area.coord.x;
					bytes[counter] = (byte)(ax >> 8);
					bytes[counter+1] = (byte)(ax);

					short az = (short)area.coord.z;
					bytes[counter+2] = (byte)(az >> 8);
					bytes[counter+3] = (byte)(az);

					bytes[counter+4] = area.pinned? (byte)1 : (byte)0;

					counter += 5;

					//adding land
					for (int l=0; l<area.lines.Length; l++)
					{
						Area.Line line = area.lines[l];

						for (int c=0; c<line.columns.Length; c++)
							{ bytes[counter] = line.columns[c].count; counter++; }

						int typesCount = line.types.Count;
						for (int i=0; i<typesCount; i++) { bytes[counter] = line.types[i]; counter++; }
						bytes[counter] = endTypesLevelsCode; counter++; 

						int levelCount = line.level.Count;
						for (int i=0; i<levelCount; i++) { bytes[counter] = line.level[i]; counter++; }
						bytes[counter] = endTypesLevelsCode; counter++; 
					}

					//adding objects
					foreach (KeyValuePair<ulong,short> kvp in area.objects)
					{
						ulong hash = kvp.Key;

						bytes[counter] = (byte)((hash >> 56) & 0xFF); 
						bytes[counter+1] = (byte)((hash >> 48) & 0xFF);  
						bytes[counter+2] = (byte)((hash >> 40) & 0xFF);
						bytes[counter+3] = (byte)((hash >> 32) & 0xFF);  
						bytes[counter+4] = (byte)((hash >> 24) & 0xFF);
						bytes[counter+5] = (byte)((hash >> 16) & 0xFF); 
						bytes[counter+6] = (byte)((hash >> 8) & 0xFF);
						bytes[counter+7] = (byte)(hash & 0xFF);

						bytes[counter+8] = (byte)kvp.Value;

						counter += 9;
					}
					bytes[counter] = endTypesLevelsCode; counter++;
					
					//adding grass
					for (int i=0; i<area.grass.Length; i++)
					{
						bytes[counter] = area.grass[i];
						counter++;
					}
					bytes[counter] = endTypesLevelsCode; counter++;
				}

				return bytes;
			}

			public void FromByteArray (byte[] bytes)
			{
				//version number
				//int vNum = bytes[0]*100 + bytes[1]*10 + bytes[2];
				//not used but just keep in mind it's there

				//size
				int bytesCount = bytes.Length;
				areaSize = bytes[3]*255 + bytes[4];

				areas.Clear();

				int counter = 5;
				while (counter < bytes.Length)
				{
					//creating area
					short ax = (short)((bytes[counter] << 8) | bytes[counter+1]);
					short az = (short)((bytes[counter+2] << 8) | bytes[counter+3]);
					bool pinned = bytes[counter+4]==1;

					Area area = new Area();
					areas[ax,az] = area;
					counter+=5;

					area.Init(new Coord (ax,az), areaSize, this);
					area.generateWorker.ready = true; //area.generated = true;
					//if (pinned) areas.Create(area.coord, parent:this, pin:true); //TODO: pin check non-clear
					if (pinned) area.pinned = true;

					for (int l=0; l<area.lines.Length; l++)
					{
						Area.Line line = area.lines[l];

						int startCounter = 0;
						for (int c=0; c<line.columns.Length; c++)
						{
							byte count = bytes[counter];
							line.columns[c] = new Area.Line.Column() { start=startCounter, count=count };
							counter++;
							startCounter+=count;
						}

						//filling line lists
						while (counter<bytesCount && bytes[counter]!=endTypesLevelsCode) { line.types.Add(bytes[counter]); counter++; }
						counter++;

						while (counter<bytesCount && bytes[counter]!=endTypesLevelsCode) { line.level.Add(bytes[counter]); counter++; }
						counter++;

						//setting columns toplevels 
						for (int c=0; c<line.columns.Length; c++)   
						{
							ushort topLevel = 0;
							byte topType = emptyByte;

							int start = line.columns[c].start;
							byte count = line.columns[c].count;
							for (int i=start; i<start+count; i++)
							{
								topLevel += line.level[i];
								topType = line.types[i];
							}

							line.columns[c].topLevel = topLevel;
							line.columns[c].topType = topType;
						}
					}

					//adding objects
					while (counter<bytesCount && bytes[counter]!=endTypesLevelsCode)
					{
						ulong hash = ((ulong)bytes[counter] << 56) | ((ulong)bytes[counter+1] << 48) | ((ulong)bytes[counter+2] << 40) | ((ulong)bytes[counter+3] << 32) |
									 ((ulong)bytes[counter+4] << 24) | ((ulong)bytes[counter+5] << 16) | ((ulong)bytes[counter+6] << 8)  | (ulong)bytes[counter+7];
						area.objects.Add(hash, bytes[counter+8]);
						counter+=9;
					}
					counter++;

					//adding grass
					for (int i=0; i<area.grass.Length; i++)
					{
						area.grass[i] = bytes[counter];
						counter++;
					}
					counter++;
				}
			}

			[SerializeField] private byte[] savedBytesArray;

			public void OnBeforeSerialize() 
			{ 
				//TODO: serialize only pinned areas
				savedBytesArray = ToByteArray();
				//savedPinnedHashes = areas.pinnedHashes;

				//savedDeployedRects = areas.deployedRects; //TODO maybe automatically assign when loading byte array?
			}

			public void OnAfterDeserialize() 
			{  
				FromByteArray(savedBytesArray); 
				//areas.pinnedHashes = savedPinnedHashes;
				//areas.deployedRects = new CoordRect[0]; 
			}

			public object Clone ()
			{
				Data copy = ScriptableObject.CreateInstance<Data>();

				byte[] byteArray = ToByteArray();
				copy.FromByteArray(byteArray);

				return copy;
			}
		#endregion
	}



}