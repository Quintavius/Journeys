using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Voxeland5.Interface
{
	[System.Serializable]
	public class ScrollZoom
	{
		public float zoom = 1;
		public float zoomStep = 0.0625f;
		public float minZoom = 0.25f;
		public float maxZoom = 2f;
		
		public int scrollWheelStep = 18;

		public Vector2 scroll = new Vector2(0, 0);
		private bool isScrolling = false;
		private	Vector2 clickPos = new Vector2(0,0);
		private Vector2 clickScroll = new Vector2(0,0);
		public int scrollButton = 2;

		public void Zoom()
		{
			if (Event.current == null) return;

			//reading control
			#if UNITY_EDITOR_OSX
			bool control = Event.current.command;
			#else
			bool control = Event.current.control;
			#endif
			
			float delta = 0;
			if (Event.current.type == EventType.ScrollWheel) delta = Event.current.delta.y / 3f;
			//else if (Event.current.type == EventType.MouseDrag && Event.current.button == 0 && control) delta = Event.current.delta.y / 15f;
			//else if (control && Event.current.alt && Event.current.type==EventType.KeyDown && Event.current.keyCode==KeyCode.Equals) delta --;
			//else if (control && Event.current.alt && Event.current.type==EventType.KeyDown && Event.current.keyCode==KeyCode.Minus) delta ++;
			if (Mathf.Abs(delta) < 0.001f) return;
			float zoomChange = -zoom * zoomStep * delta; //progressive step

			//returning if zoom will be out-of-range
			//if (zoom+zoomChange > maxZoom || zoom+zoomChange < minZoom) return;

			//clamping zoom change so it will never be out of range
			if (zoom + zoomChange > maxZoom) zoomChange = maxZoom - zoom;
			if (zoom + zoomChange < minZoom) zoomChange = minZoom - zoom;

			//record mouse position in worldspace
			Vector2 worldMousePos = (Event.current.mousePosition - scroll) / zoom;

			//changing zoom
			zoom += zoomChange;
			if (zoom >= minZoom && zoom <= maxZoom) scroll -= worldMousePos * zoomChange;
			//zoom = Mathf.Clamp(zoom, minZoom, maxZoom); //returning on out-of-range instead
			#if UNITY_EDITOR
			if (UnityEditor.EditorWindow.focusedWindow != null) UnityEditor.EditorWindow.focusedWindow.Repaint();
			#endif
		}


		public void ScrollWheel(int step = 3)
		{
			float delta = 0;
			if (Event.current.type == EventType.ScrollWheel) delta = Event.current.delta.y / 3f;
			scroll.y -= delta * scrollWheelStep * step;
		}


		public void Scroll()
		{
			if (Event.current.type == EventType.MouseDown  &&  Event.current.button == scrollButton)
			{
				clickPos = Event.current.mousePosition;
				clickScroll = scroll;
				isScrolling = true;
			}

			if (Event.current.type == EventType.MouseDown  &&  Event.current.button == 0  &&  Event.current.alt)  //alternative way to scroll
			{
				clickPos = Event.current.mousePosition;
				clickScroll = scroll;
				isScrolling = true;
			}

			if (Event.current.rawType == EventType.MouseUp) 
			{
				isScrolling = false;
			}
			
			if (isScrolling)
			{
				scroll = clickScroll + Event.current.mousePosition - clickPos; 

				#if UNITY_EDITOR
				UnityEditor.EditorWindow.focusedWindow.Repaint();
				#endif
			}
		}



		/*public Rect ToDisplay(Rect rect)
		{
			return new Rect(rect.x * zoom + scroll.x, rect.y * zoom + scroll.y, rect.width * zoom, rect.height * zoom);
		}
		
		public Rect ToDisplay(float offsetX, float offsetY, float sizeX, float sizeY)
		{
			return new Rect(offsetX * zoom + scroll.x, offsetY * zoom + scroll.y, sizeX * zoom, sizeY * zoom);
		}

		public Rect ToDisplay(Float2 pos, Float2 size)
		{
			if (paddingBox==null) return new Rect(
				(int)( pos.x * zoom + scroll.x  + 0.5f), 
				(int)( pos.y * zoom + scroll.y  + 0.5f), 
				(int)( size.x * zoom  + 0.5f), 
				(int)( size.y * zoom  + 0.5f) );

			else 
			{
				Padding padding = (Padding)paddingBox;
				return new Rect(
				(int)( (pos.x + padding.left) * zoom + scroll.x  + 0.5f), 
				(int)( (pos.y + padding.top) * zoom + scroll.y   + 0.5f), 
				(int)( (size.x - (padding.left+padding.right)) * zoom  + 0.5f), 
				(int)( (size.y - (padding.top+padding.bottom)) * zoom  + 0.5f));
			}
		}

		public Rect ToInternal(Rect rect)
		{
			return new Rect((rect.x - scroll.x) / zoom, (rect.y - scroll.y) / zoom, rect.width / zoom, rect.height / zoom);
		}

		public Vector2 ToInternal(Vector2 pos)
		{
			return (pos - scroll) / zoom;
		} //return new Vector2( (pos.x-scroll.x)/zoom, (pos.y-scroll.y)/zoom); }

		public Vector2 ToDisplay(Vector2 pos)
		{
			return pos * zoom + scroll;
		}

		public void Focus(Cell cell, Vector2 pos)
		{
			throw new System.NotImplementedException();
		}*/
	}
}
