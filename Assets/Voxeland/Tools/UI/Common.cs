using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;

namespace Voxeland5.Interface
{
		public enum Layout { Full, Vertical, Horizontal }

		public struct Float2 
		{
			public float x;
			public float y;
		
			public Float2 (float x, float y) { this.x=x; this.y=y; }
		}

		public struct Quad
		{
			public Float2 pos;
			public Float2 size;
		}

		public struct Padding  // faster then rectoffset
		{
			public bool defined;

			public float left;
			public float top;
			public float right;
			public float bottom;

			public Padding (Padding src) { this.left=src.left; this.top=src.top; this.right=src.right; this.bottom=src.bottom; defined=true; }
			public Padding (float left, float top, float right, float bottom) { this.left=left; this.top=top; this.right=right; this.bottom=bottom; defined=true; }
			public Padding (float offset) { this.left=offset; this.top=offset; this.right=offset; this.bottom=offset; defined=true; }

			public static Padding operator + (Padding a, Padding b) { return new Padding(a.left+b.left, a.top+b.top, a.right+b.right, a.bottom+b.bottom); }
			public static Padding operator + (Padding a, float f) { return new Padding(a.left+f, a.top+f, a.right+f, a.bottom+f); }
			public static Padding operator - (Padding a, float f) { return new Padding(a.left-f, a.top-f, a.right-f, a.bottom-f); }
		}
}