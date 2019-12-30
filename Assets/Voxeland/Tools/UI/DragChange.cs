using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace Voxeland5.Interface
{
	public static class DragChange
	{
		private static float StepRound (float src)
		{
			if (src < 1) src= ((int)(src*1000))/1000f;
			else if (src < 10) src= ((int)(src*100))/100f;
			else if (src < 100) src= ((int)(src*10))/10f;
			else src= (int)(src);
			return src;
		}

		private static Vector2 sliderClickPos;
		private static ulong sliderDraggingId = 4294967295;  //way more then screen res. Do not leafe 0 here, new unprepared cells would be considered as dragging
		private static float sliderOriginalValue;

		public static float DragChangeField (float val, Rect rect, float min = 0, float max = 0, float minStep=0.01f)
		{
			ulong controlId = 
				(ulong)(rect.x % 65535) << 48 |
				(ulong)(rect.y % 65535) << 32 |
				(ulong)(rect.width % 65535) << 16 |
				(ulong)(rect.height % 65535);

			#if UNITY_EDITOR
			UnityEditor.EditorGUIUtility.AddCursorRect (rect, UnityEditor.MouseCursor.SlideArrow);
			#endif
			if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition) ) 
			{ 
				sliderClickPos = Event.current.mousePosition; 
				sliderOriginalValue = val;
				sliderDraggingId = controlId; 
			}

			if (sliderDraggingId == controlId) // && Event.current.type == EventType.MouseDrag)
			{
				int steps = (int)((Event.current.mousePosition.x - sliderClickPos.x) / 5);
					
				val = sliderOriginalValue;

				for (int i=0; i<Mathf.Abs(steps); i++)
				{
					float absVal = val>=0? val : -val;

					float step = 0.01f;
					//if (absVal > 0.99f) step=0.02f; if (absVal > 1.99f) step=0.1f;   if (absVal > 4.999f) step = 0.2f; if (absVal > 9.999f) step=0.5f;
					//if (absVal > 39.999f) step=1f;  if (absVal > 99.999f) step = 2f; if (absVal > 199.999f) step = 5f; if (absVal > 499.999f) step = 10f; 
					if (absVal > 0.5f) step=0.05f;  if (absVal > 0.999f) step=0.1f;   if (absVal > 9.999f) step=0.5f;
					if (absVal > 39.999f) step=1f;   if (absVal > 199.999f) step = 5f; if (absVal > 499.999f) step = 10f; 
					if (step < minStep) step = minStep;

					val = steps>0? val+step : val-step;
					val = Mathf.Round(val*10000)/10000f;

					if (Mathf.Abs(min)>0.001f && val<min) val=min;
					if (Mathf.Abs(max)>0.001f && val>max) val=max;
				}

				#if UNITY_EDITOR
				if (UnityEditor.EditorWindow.focusedWindow!=null) UnityEditor.EditorWindow.focusedWindow.Repaint(); 
				UnityEditor.EditorGUI.FocusTextInControl("");
				#endif
			}
			if (Event.current.rawType == EventType.MouseUp) 
			{
				sliderDraggingId = 4294967295;

				#if UNITY_EDITOR
				if (UnityEditor.EditorWindow.focusedWindow!=null) UnityEditor.EditorWindow.focusedWindow.Repaint(); 
					//UnityEditor.EditorGUI.FocusTextInControl("");
					#endif
			}
			if (Event.current.isMouse && sliderDraggingId == controlId) Event.current.Use();

			return val;
		}
	}
}