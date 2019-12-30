using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace Voxeland5.Interface
{
	public static class UI
	{
		public static Cell active;  
		//note that "active" is not used in Group. It's only for static usage!

		public const int lineHeight = 18;
		static readonly Padding defaultPadding = new Padding(1,1,1,1);
		const float fieldWidth = 0.5f;


		//starting group
		public static Cell Vertical (float size=1, string name=null)  
		{ 
			Cell cell = active.Add(Layout.Vertical, size, name);

			//if (background) Element(Icons.GetElementStyle("DPLayout_FoldoutBackground"), cell:cell);

			cell.prevActive = active;
			active = cell;
			return cell;
		}

		public static Cell Horizontal (float size=1, string name=null)  
		{ 
			Cell cell = active.Add(Layout.Horizontal, size, name);

			//if (background) Element(Icons.GetElementStyle("DPLayout_FoldoutBackground"), cell:cell);

			cell.prevActive = active;
			active = cell;
			return cell;
		}


		public static Cell Custom (Layout layout, Float2 pos, Float2 size, string name=null)  
		{ 
			Cell ui = active.Add(layout, 0, name);
			ui.OverrideRect(pos, size);

			ui.prevActive = active;
			active = ui;
			return ui;
		}

		public static void Empty (float size=1)
		{
			active.Add(Layout.Full, size);
		}


		public static Cell Padded (Layout layout, Padding padding, float size=1, string name=null, bool background=false)  
		{ 
			Cell h = active.Add(Layout.Horizontal, size);

			if (background) Element(Icons.GetElementStyle("DPLayout_FoldoutBackground"), cell:h);

			h.Add(Layout.Full, padding.left);
				Cell v = h.Add(Layout.Vertical, size);

				v.Add(Layout.Full, padding.top);
				Cell cell = v.Add(Layout.Vertical, size, name);
				v.Add(Layout.Full, padding.bottom);
			h.Add(Layout.Full, padding.right);

			cell.prevActive = active;
			active = cell;
			return cell;
		}

		public static Cell FoldoutCell (
			ref bool expanded, 
			string label,
			float size=1, 
			Cell cell=null, //vertical
			object backgroundPaddingBox = null,
			Action<object> onChange=null,
			string tooltip = null) 
		{
			if (size < 0) size = active.autoLayout==Layout.Vertical? lineHeight : 1;
			if (cell == null) cell = active.Add(Layout.Vertical, size);

			//background
			if (expanded)
			{
				Padding backgroundPadding = backgroundPaddingBox!=null ? (Padding)backgroundPaddingBox : new Padding(-3,-3,-3,-3);
				Element(Icons.GetElementStyle("DPLayout_FoldoutBackground"), cell:cell, padding:backgroundPadding);
			}

			//foldout
			Cell foldCell = cell.Add(Layout.Full, lineHeight);
			expanded = Foldout(expanded, label, cell:foldCell, onChange:onChange, tooltip:tooltip);

			//field
			Cell hCell = cell.Add(Layout.Horizontal);
			hCell.Add(Layout.Full, 10); //margin
			Cell fieldCell = hCell.Add(Layout.Vertical);

			fieldCell.prevActive = active;
			active = fieldCell;
			return fieldCell;
		}



		public static void Label (
			string label, 
			float size=-1, 
			Cell cell=null)
		{
			//if (Event.current.type == EventType.Layout  ||  Event.current.type == EventType.MouseDrag) return;

			if (size < 0) size = active.autoLayout==Layout.Vertical? lineHeight : 1;

			if (cell == null) cell = active.Add(Layout.Full, size);

			#if UNITY_EDITOR
			Rect displayRect = cell.ToDisplay(defaultPadding);
			UnityEditor.EditorGUI.LabelField(displayRect, label, style:Styles.label);
			#endif
		}


		public static bool Button (
			bool src, 
			string label = null, 
			Texture2D icon = null, 
			float size = -1, 
			Cell cell = null, //should be horizontal
			bool disabled = false,
			Action<bool> onChange = null, 
			string tooltip = null) 
		{
			//if (Event.current.type == EventType.Layout  ||  Event.current.type == EventType.MouseDrag) return src;

			if (size < 0) size = active.autoLayout==Layout.Vertical? lineHeight : 1;
			if (cell == null) cell = active.Add(Layout.Horizontal, size, "Button");  //using horizontal for icon+label

			#if UNITY_EDITOR
			if (disabled) UnityEditor.EditorGUI.BeginDisabledGroup(true);
			#endif

			//draw button
			Rect buttonRect = cell.ToDisplay(defaultPadding);
			GUIContent content = new GUIContent(icon==null? label : "", tooltip);  //drawing button text separately if icon is used
			bool dst = GUI.Toggle(buttonRect, src, content, Styles.button);

			//draw icon
			if (icon != null  &&  label == null) 
				Icon(icon, cell:cell);

			//draw icon + label
			if (icon != null  &&  label != null) 
			{
				cell.Add(Layout.Full, 5); // empty offset
				Cell iconCell = cell.Add(Layout.Full, size:icon.width, name:"Icon");

				Icon(icon, cell:iconCell);

				#if UNITY_EDITOR
				Cell labelCell = cell.Add(Layout.Full); 
				Rect labelRect = labelCell.ToDisplay();
				UnityEditor.EditorGUI.LabelField(labelRect, label, style:Styles.label);
				#endif
			}

			#if UNITY_EDITOR
			if (disabled) UnityEditor.EditorGUI.EndDisabledGroup();
			#endif

			if (src != dst) 
			{
				if (onChange!=null) onChange(dst);
				//RecordUndo();
			}

			return dst;
		}


		public static void Field<T> (
			ref T val, 
			string label = null, 
			float size=-1, 
			Cell cell = null, //should be horizontal
			float fieldWidth = -1,
			bool isLeft = false, //nof for toggles, later mabe for other fields
			bool disabled = false,
			Action<object> onChange=null,
			string tooltip = null) 
		{
			val = (T)Field(
				(object) val,
				label,
				size:size,
				cell:cell,
				fieldWidth:fieldWidth,
				isLeft:isLeft,
				disabled:disabled,
				onChange:onChange,
				tooltip:tooltip);
		}

		/*public static T Field<T> (
			T val, 
			string label = null, 
			float size=-1, 
			Cell cell = null, //should be horizontal
			float fieldWidth = -1,
			bool isLeft = false, //nof for toggles, later mabe for other fields
			bool disabled = false,
			Action<object> onChange=null,
			string tooltip = null) 
				where T : struct
		{
			return (T)Field(
				(object) val,
				label,
				size:size,
				cell:cell,
				fieldWidth:fieldWidth,
				isLeft:isLeft,
				disabled:disabled,
				onChange:onChange,
				tooltip:tooltip);
		}*/

		public static object Field (
			object val, 
			string label = null, 
			float size=-1, 
			Cell cell = null, //should be horizontal
			float fieldWidth = -1,
			bool isLeft = false, //nof for toggles, later mabe for other fields
			bool disabled = false,
			bool allowSceneObject = false,
			Func<object, Rect, object> drawField=null, 
			Func<object, object, bool> changeFunc=null,
			Action<object> onChange=null,
			string tooltip = null) 
		{
			//if (Event.current.type == EventType.Layout  ||  Event.current.type == EventType.MouseDrag) return val;

			if (val is bool) //special case for toggle
			{
				val = Toggle((bool)val, label, size:size, cell:cell, isLeft:isLeft, onChange:tmp=>{if (onChange!=null) onChange(tmp);}, tooltip:tooltip);
				return val;
			}

			if (size < 0) size = active.autoLayout==Layout.Vertical? lineHeight : 1;
			if (fieldWidth < 0) fieldWidth = UI.fieldWidth;
			if (cell == null) cell = active.Add(Layout.Horizontal, size, "FieldCell");

			#if UNITY_EDITOR
			if (disabled) UnityEditor.EditorGUI.BeginDisabledGroup(true);
			#endif

			Cell labelCell = cell;
			Cell fieldCell = cell;


			//label
			if (label != null) 
			{
				labelCell = cell.Add(Layout.Full, 1-fieldWidth, "Label");
				fieldCell = cell.Add(Layout.Full, 1-fieldWidth, "Field");
			}

			#if UNITY_EDITOR
			Rect labelRect = labelCell.ToDisplay(defaultPadding);  //for drag change
			#endif

			if (label != null) 
			{
				//Label(label, cell:labelCell, padding:padding);
				#if UNITY_EDITOR
				UnityEditor.EditorGUI.LabelField(labelRect, label, style:Styles.label);  //faster
				#endif
			}


			//field
			bool change = false;
			Rect fieldRect = fieldCell.ToDisplay(defaultPadding);

			if (drawField != null)
			{
				object newVal = drawField(val, fieldRect);

				if (changeFunc != null) change = changeFunc(val, newVal);
				else change = val != newVal;

				val = newVal;
			}

			else if (val is float)
			{
				#if UNITY_EDITOR
				float oldVal = (float)val;
				float newVal = UnityEditor.EditorGUI.DelayedFloatField(fieldRect, oldVal, style:Styles.field);
				if (label != null) newVal = DragChange.DragChangeField(oldVal, labelRect);
				change = Math.Abs(newVal - oldVal) > Mathf.Epsilon;
				val = newVal;
				#endif
			}

			else if (val is int)
			{
				#if UNITY_EDITOR
				int oldVal = (int)val;
				int newVal = UnityEditor.EditorGUI.DelayedIntField(fieldRect, oldVal, style:Styles.field);
				if (label != null) newVal = Mathf.RoundToInt(DragChange.DragChangeField(oldVal, labelRect, minStep:1));
				change = oldVal!=newVal;
				val = newVal;
				#endif
			}

			else if (val is UnityEngine.Object)
			//else if (type.IsSubclassOf(typeof(UnityEngine.Object))) 
			{
				#if UNITY_EDITOR
				Type type = val.GetType();
				UnityEngine.Object oldVal = (UnityEngine.Object)val;
				UnityEngine.Object newVal = UnityEditor.EditorGUI.ObjectField(fieldRect, oldVal, type, allowSceneObject);
				change = oldVal!=newVal;
				val = newVal;
				#endif
			}

			
			if (change) 
			{
				if (onChange!=null) onChange(val);
				//RecordUndo();
			}

			#if UNITY_EDITOR
			if (disabled) UnityEditor.EditorGUI.EndDisabledGroup();
			#endif
					
			return val;
		}





		public static bool Toggle (
			bool src,
			string label,
			float size=-1, 
			Cell cell = null, //should be horizontal
			bool isLeft=false,
			Action<object> onChange=null,
			string tooltip = null) 
		{
			if (size < 0) size = active.autoLayout==Layout.Vertical? lineHeight : 1;
			if (cell == null) cell = active.Add(Layout.Horizontal, size);

			Cell toggleCell;
			Cell labelCell;
			if (isLeft) 
			{
				toggleCell = cell.Add(Layout.Full, 20);
				labelCell = cell.Add(Layout.Full);
			}
			else
			{
				labelCell = cell.Add(Layout.Full);
				toggleCell = cell.Add(Layout.Full, 20);
			}

			Label(label, cell:labelCell);

			#if UNITY_EDITOR
			Rect rect = toggleCell.ToDisplay(defaultPadding);
			bool dst = UnityEditor.EditorGUI.Toggle(rect, src);
			#else
			bool dst = src;
			#endif

			if (src != dst) 
			{
				if (onChange!=null) onChange(dst);
				//RecordUndo();
			}

			return dst;
		}


		public static bool Foldout (
			bool src, 
			string label,
			float size=-1, 
			Cell cell = null, //should be vertical with padding 10
			Action<object> onChange=null,
			string tooltip = null) 
		{
			if (size < 0) size = active.autoLayout==Layout.Vertical? lineHeight : 1;
			if (cell == null) cell = active.Add(Layout.Full, size, "Foldout");

			GUIContent content = new GUIContent(label, tooltip);

			#if UNITY_EDITOR
			Rect rect = cell.ToDisplay(new Padding(11,1,1,1));
			//GUIUtility.GetControlID(FocusType.Passive);
			bool dst = UnityEditor.EditorGUI.Foldout(rect, src, content, true, Styles.foldout);
			if (src != dst) 
			{
				if (onChange!=null) onChange(dst);
				//RecordUndo();
				UnityEditor.EditorGUI.FocusTextInControl("");
			}
			return dst;
			#else
			return false;
			#endif
		}

		public static void Class (
			object obj, 
			string category=null,
			float size=-1, 
			Cell cell = null, //should be vertical
			Action<object> onChange=null) 
		{
			Type objType = obj.GetType();

			if (size < 0) size = active.autoLayout==Layout.Vertical? lineHeight : 1;

			//loading attributes
			if (!Val.attributesCaches.ContainsKey(objType)) Val.Cache(objType);
			Val[] attributes = Val.attributesCaches[objType];
			
			//drawing
			if (cell == null) cell = active.Add(Layout.Vertical, size, "Class " + objType.Name);

			for (int a=0; a<attributes.Length; a++)
			{
				if (category != null && attributes[a].cat != category) continue;

				Cell fieldCell = cell.Add(Layout.Horizontal, lineHeight);

				FieldInfo field = attributes[a].field;
				object val = field.GetValue(obj);
				Field(
					val, 
					attributes[a].name, 
					cell:fieldCell, 
					isLeft:attributes[a].isLeft, 
					onChange: newVal => {field.SetValue(obj, newVal); if (onChange!=null) onChange(newVal);} );
            }
		}


		public static void Element (
			GUIStyle elementStyle = null,
			string elementTexName = null,
			float size=-1, 
			Cell cell = null, //should be vertical
			Padding padding = new Padding(),
			Action<object> onChange=null,
			string tooltip = null) 
		{
			if (size < 0) size = active.autoLayout==Layout.Vertical? lineHeight : 1;
			if (cell == null) cell = active.Add(Layout.Full, size, "Element");

			if (elementStyle==null && elementTexName!=null) elementStyle = Icons.GetElementStyle(elementTexName);
			if (elementStyle==null) return;

			Rect rect = cell.ToDisplay(padding);
			if (Event.current.type==EventType.Repaint) elementStyle.Draw(rect, false, false, false, false);
		}

		public static void Background (Cell cell=null)
		{
			Element(Icons.GetElementStyle("DPLayout_FoldoutBackground"), cell:cell, padding:new Padding(-2,-2,-2,-2));
		}


		public static void Icon (
			Texture2D icon,
			float size=-1, 
			Cell cell = null)
		/// Draws an icon in the center of cell in native resolution (for zoom 1)
		{
			if (size < 0) size = active.autoLayout==Layout.Vertical? lineHeight : 1;
			if (cell == null) cell = active.Add(Layout.Full, size);

			Rect cellRect = cell.ToDisplay();

			Vector2 center = new Vector2(cellRect.x + cellRect.width/2f, cellRect.y + cellRect.height/2f);

			float zoom = cell.scrollZoom==null? 1 : cell.scrollZoom.zoom;

			Rect iconRect = new Rect(
				center.x - icon.width/2f * zoom,
				center.y - icon.height/2f * zoom,
				icon.width * zoom,
				icon.height * zoom);

			//UnityEditor.EditorGUI.DrawRect(iconRect, Color.green);
			GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleAndCrop);
		}


	}
}