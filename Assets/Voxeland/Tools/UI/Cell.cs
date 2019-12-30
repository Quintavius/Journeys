using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace Voxeland5.Interface
{
	public class Cell : IDisposable
	{
		public string name = null; //debug purpose
		
		public List<Cell> children;
		public int childCounter; //number of children currently added. Resets on each start. (can't use children.Count since children are cached)
		public Layout autoLayout; //the way children are aligned
		

		private Float2 relSize;
		private Float2 absSize;
			
		private bool fixedRect;  //layout is for children, fixed rect is for this
		public Float2 finalSize;
		public Float2 finalOffset;

		//public Padding padding;

		public Cell prevActive;

		public ScrollZoom scrollZoom = null;


		/// Optimizing
		public bool layoutUsed = false;
		public bool Optimize ()
		{
			//Optimizing unnecessary layouts and mouse drags
			if (Event.current.type == EventType.MouseDrag) return true;  //do not ever use mouse drag event for anything
			if (Event.current.type == EventType.Layout)
			{
				if (layoutUsed) return true; 
				layoutUsed = true;
			}
			return false;
		}

		/// Read inspector width and assign inspector height
		public Rect GetInspectorRect (object editor)
		{
			Rect rect = new Rect();

			#if UNITY_EDITOR
			UnityEditor.EditorGUI.indentLevel = 0;
			rect = UnityEditor.EditorGUILayout.GetControlRect(GUILayout.Height(0));

			rect.x -= 10; rect.width += 10;

			if (Event.current.type == EventType.Layout)  //using saved rect
			{
				rect = new Rect(finalOffset.x, finalOffset.y, finalSize.x, finalSize.y);
			}
			#endif

			

			return rect;
		}

		/// Starting root
		public void Start (Rect rect)
		{
			//making this root
			name = "Root";
			UI.active = this;
			autoLayout = Layout.Vertical; //vertical is the default root layout

			//if rect is changed - recalculating sizes
			if (Mathf.Abs(rect.x-finalOffset.x) > 0.0001f  ||  Mathf.Abs(rect.y-finalOffset.y) > 0.0001f  ||  
				Mathf.Abs(rect.width-finalSize.x) > 0.0001f) //  ||  Mathf.Abs(rect.height-finalSize.y) > 0.0001f)  //do not take height into account
			{
				Debug.Log("Overriding: rect:" + rect + " old:" + finalOffset.x + "," + finalOffset.y + "," + finalSize.x + "," + finalSize.y);
				OverrideRect( new Float2(rect.x, rect.y), new Float2(rect.width, rect.height) );

				CalcMinSizes();
				CalcFinalSizes();
			}
			

			//scroll/zoom
			if (scrollZoom == null) scrollZoom = new ScrollZoom(); //will not convert to display without it 

			//styles
			Styles.CheckInitStyles();
			Styles.ResizeStyles(scrollZoom!=null ? scrollZoom.zoom : 1f);  //not only in case of zoom - it has to return zoomed window when drawing other control

			//resetting children counter to 0. Do not remove children, but pretend they were cleared
			ResetChildrenCounter(); 

			//debugging
			//PaintBackgroundRecursively(caption:true);
		}


		/// Finalizing root
		public void End ()
		{
			//removing all the children above the child counter
			RemoveExtraChildren();

			//calculating rects
			CalcMinSizes();
			CalcFinalSizes();

			GUILayoutUtility.GetRect(1, finalSize.y, "TextField");
		}


		/// Adding (selecting from a pool) of a new ui instance. Use this for non-static group adding
		public Cell Add (Layout layout, float size=1, string name=null) 
		{
			//get next pooled child
			if (children == null) children = new List<Cell>();
			if (childCounter >= children.Count) children.Add(null);
			if (childCounter >= children.Count) throw new Exception("Could not add child");

			Cell ui = children[childCounter] ?? new Cell();
			children[childCounter] = ui;
			childCounter++;


			//setting size
			switch (autoLayout)  //note parent layout used here, not the new one
			{
				case Layout.Horizontal:
					if (size > 1.0001f) { ui.absSize.x = size; ui.relSize.x = 0; }
					else { ui.relSize.x = size; ui.absSize.x = 0; }
					ui.relSize.y = 1; ui.absSize.y = 0;
					break;
						
				case Layout.Vertical:
					ui.relSize.x = 1; ui.absSize.x = 0;
					if (size > 1.0001f) { ui.absSize.y = size; ui.relSize.y = 0; }
					else { ui.relSize.y = size; ui.absSize.y = 0; }
					break;
						
				case Layout.Full: default:
					ui.relSize = new Float2(1,1);
					ui.absSize = new Float2(0,0);
					break;
			}


			ui.autoLayout = layout;
			ui.name = name;
			ui.scrollZoom = scrollZoom;
			//ui.padding = padding;
			
			return ui;
		}


		/// Sets the fixed rect that will not be changed with auto-layout
		public void OverrideRect (Float2 pos, Float2 size)
		{ 
			fixedRect = true;
			finalOffset = pos;
			absSize = size;  //using abs size, not final one since it could be increased. It will be converted to final on CalcMin
		}


		/// Converting internal 1-based zoom and 0-offset to display with real zoom and offset
		public Rect ToDisplay(Padding padding)
		{
			return new Rect(
				(int)( (finalOffset.x + padding.left) * scrollZoom.zoom + scrollZoom.scroll.x  + 0.5f), 
				(int)( (finalOffset.y + padding.top) * scrollZoom.zoom + scrollZoom.scroll.y   + 0.5f), 
				(int)( (finalSize.x - (padding.left+padding.right)) * scrollZoom.zoom  + 0.5f), 
				(int)( (finalSize.y - (padding.top+padding.bottom)) * scrollZoom.zoom  + 0.5f));
		}

		public Rect ToDisplay()
		{
			return new Rect(
				(int)( finalOffset.x * scrollZoom.zoom + scrollZoom.scroll.x  + 0.5f), 
				(int)( finalOffset.y * scrollZoom.zoom + scrollZoom.scroll.y   + 0.5f), 
				(int)( finalSize.x * scrollZoom.zoom  + 0.5f), 
				(int)( finalSize.y * scrollZoom.zoom  + 0.5f));
		}

		
		/// Recursively sets the final size so that it's never less than abs size of childrenn
		public void CalcMinSizes ()
		{
			//from child to parent

			if (children == null) return; //that should no happen except empty root

			//now work with final size only
			finalSize = absSize;
			for (int i=0; i<children.Count; i++)
				children[i].finalSize = children[i].absSize;
				
				
			for (int i=0; i<children.Count; i++)
			{
				if (children[i].children != null) children[i].CalcMinSizes();
			}

			Float2 finalSum = new Float2(0,0);

			if (autoLayout == Layout.Horizontal)
			{
				for (int i=0; i<children.Count; i++)
				{
					finalSum.x += children[i].finalSize.x;
					finalSum.y = Mathf.Max(finalSum.y, children[i].finalSize.y);
				}
			}

			if (autoLayout == Layout.Vertical)
			{
				for (int i=0; i<children.Count; i++)
				{
					finalSum.x = Mathf.Max(finalSum.x, children[i].finalSize.x);
					finalSum.y += children[i].finalSize.y;
				}
			}
				
			if (autoLayout == Layout.Full)
			{
				for (int i=0; i<children.Count; i++)
				{
					finalSum.x = Mathf.Max(finalSum.x, children[i].finalSize.x);
					finalSum.y = Mathf.Max(finalSum.y, children[i].finalSize.y);
				}
			}

			finalSize = new Float2( Mathf.Max(finalSize.x, finalSum.x), Mathf.Max(finalSize.y, finalSum.y) );
		}

			
		/// Recursively transforms relative size to  absolute in all children. Calculates offsets for auto-layout too. Use onlt final size instead of abs (it's == after CalcMinSizes)
		public void CalcFinalSizes ()
		{
			//from parent to children

			if (children == null) return; //this should not happen except empty root




			if (autoLayout == Layout.Horizontal)
			{
				//normalizing relative sizes
				float relSum = 0;
				for (int i=0; i<children.Count; i++)
					relSum += children[i].relSize.x;

				if (relSum > 0.0001f)
				for (int i=0; i<children.Count; i++)
					children[i].relSize.x /= relSum;

				//calculating relative factor (relSize*factor = absSize)
				float finalSum = 0;
				for (int i=0; i<children.Count; i++)
					finalSum += children[i].finalSize.x;

				float relSpaceLeft = finalSize.x - finalSum;  //the space left for relative cells

				//layout
				float offset = 0;
				for (int i=0; i<children.Count; i++)
				{
					if (children[i].fixedRect) continue;
						
					children[i].finalSize.x = children[i].finalSize.x  +  children[i].relSize.x*relSpaceLeft;  // rel size is applied additively
					children[i].finalSize.y = finalSize.y;

					children[i].finalOffset.x = finalOffset.x + offset;
					children[i].finalOffset.y = finalOffset.y;

					offset += children[i].finalSize.x;
				}
			}
				
			if (autoLayout == Layout.Vertical)
			{
				//normalizing relative sizes
				float relSum = 0;
				for (int i=0; i<children.Count; i++)
					relSum += children[i].relSize.y;

				if (relSum > 0.0001f)
				for (int i=0; i<children.Count; i++)
					children[i].relSize.y /= relSum;

				//calculating relative factor (relSize*factor = absSize)
				float finalSum = 0;
				for (int i=0; i<children.Count; i++)
					finalSum += children[i].finalSize.y;

				float relSpaceLeft = finalSize.y - finalSum;  //the space left for relative cells

				//layout
				float offset = 0;
				for (int i=0; i<children.Count; i++)
				{
					if (children[i].fixedRect) continue;
						
					children[i].finalSize.x = finalSize.x;
					children[i].finalSize.y = children[i].finalSize.y  +  children[i].relSize.y*relSpaceLeft;  // rel size is applied additively

					children[i].finalOffset.x = finalOffset.x;
					children[i].finalOffset.y = finalOffset.y + offset;

					offset += children[i].finalSize.y;
				}
			}

			if (autoLayout == Layout.Full)
			{
				for (int i=0; i<children.Count; i++)
				{
					if (children[i].fixedRect) continue;
						
					children[i].finalSize.x = finalSize.x;
					children[i].finalSize.y = finalSize.y;

					children[i].finalOffset.x = finalOffset.x;
					children[i].finalOffset.y = finalOffset.y;
				}
			}

			for (int i=0; i<children.Count; i++)
			{
				//apply padding
				//children[i].finalOffset.x += children[i].padding.left;
				//children[i].finalOffset.y += children[i].padding.top;
				//children[i].finalSize.x -= children[i].padding.left+children[i].padding.right;
				//children[i].finalSize.y -= children[i].padding.top+children[i].padding.bottom;

				if (children[i].children != null) children[i].CalcFinalSizes();	
			}
		}


		/// Removes all the children that are above childCounter value
		public void RemoveExtraChildren ()
		{
			if (children!=null)
			{
				if (children.Count > childCounter) children.RemoveRange(childCounter, children.Count-childCounter);
				if (children.Count == 0) children = null;
			}

			if (children == null) return; //that should no happen except empty root
			for (int i=0; i<children.Count; i++)
			{
				if (children[i] == null || children[i].children == null) continue;
				
				if (children[i].children.Count > children[i].childCounter) children[i].children.RemoveRange(children[i].childCounter, children[i].children.Count-children[i].childCounter);
				if (children[i].children.Count == 0) children[i].children = null;

				children[i].RemoveExtraChildren();
			}
		}


		/// Recursively set children counter to 0 on start to add children one-by-one (without removing children)
		public void ResetChildrenCounter ()
		{
			childCounter = 0;
			if (children == null) return; //that should no happen except empty root
			for (int i=0; i<children.Count; i++)
			{
				if (children[i] == null) continue;
				children[i].childCounter = 0;
				if (children[i].children != null) children[i].ResetChildrenCounter();
			}
		}


		/// Recurisvely paints background rect for debug purpose
		public void PaintBackgroundRecursively (int level=0, bool caption=true)
		{
			if (children == null) //if has children it will be overpainted anyways
			{
				#if UNITY_EDITOR
				Rect rect;
				rect = ToDisplay();
				//rect = new Rect(finalOffset.x, finalOffset.y, finalSize.x, finalSize.y);

				Color color = new Color(level/7f, 1-level/7f, 0, 0.5f);

				
				GUI.DrawTexture(rect, Icons.GetIcon("DPMarkup_Gradient"), ScaleMode.StretchToFill);
				UnityEditor.EditorGUI.DrawRect(rect, color);
				if (caption && name != null) 
					UnityEditor.EditorGUI.LabelField( new Rect(rect.x, rect.y+rect.height/2-9, rect.width, 18), name, style:Styles.centerLabel);
				#endif
			}

			if (children != null)
				for (int i=0; i<children.Count; i++)
					children[i].PaintBackgroundRecursively(level+1, caption);
		}


		/// Returns the internal hierarchy of the cell
		public string Hierarchy () { string h = ""; Hierarchy(ref h); return h; }
		public void Hierarchy (ref string h, int level=0)
		{
			string n = name ?? "Unnamed";
			string t = "";
			for (int i=0; i<level; i++) t += "\t";
			h += t + n + " " + finalSize + "," + autoLayout + " " + "realCh:" + (children!=null? children.Count.ToString() : "na") + "cntCh:" + childCounter + "\n";

			if (children != null)
				for (int i=0; i<children.Count; i++)
					children[i].Hierarchy(ref h, level+1);
		}


		public void Dispose ()
		{
			UI.active = prevActive;
		}
	}
}
