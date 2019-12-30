using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Voxeland5
{
	public static class Geometry 
	{
		public static Vector3 ClosestPointOnLine (Vector3 lineStart, Vector3 lineEnd, Vector3 point)
		{
			float lineLength = (lineStart-lineEnd).magnitude;
			float startDistance = (lineStart-point).magnitude;
			float endDistance = (lineEnd-point).magnitude;

			//finding height of triangle 
			float halfPerimeter = (startDistance + endDistance + lineLength) / 2;
			float square = Mathf.Sqrt( halfPerimeter*(halfPerimeter-endDistance)*(halfPerimeter-startDistance)*(halfPerimeter-lineLength) );
			float height = 2/lineLength * square;

			//finding a percent from startDist
			float distFromStart = Mathf.Sqrt(startDistance*startDistance - height*height);
			float percent = distFromStart / lineLength;

			return lineStart*(1-percent) + lineEnd*percent;
		}

		public static Vector3 ClosestPointOnLine2 (Vector3 a, Vector3 b, Vector3 p)
		{
			Vector3  ap = p-a;
			Vector3 ab = b-a;
			return a + Vector3.Dot(ap,ab)/Vector3.Dot(ab,ab) * ab;
		}

		public static Vector3 ClosestPointBetweenLines (Vector3 a0, Vector3 a1, Vector3 b0, Vector3 b1)
		{
		   //Calculate denomitator
		   Vector3 A = a1 - a0;
		   Vector3  B = b1 - b0;
		   float magA = A.magnitude;
		   float magB = B.magnitude;

		   Vector3 _A = A.normalized;
		   Vector3 _B = B.normalized;

		   Vector3 cross = Vector3.Cross(_A, _B);
		   float denom = cross.magnitude*cross.magnitude;

			//Lines criss-cross: Calculate the projected closest points
			Vector3 t = (b0 - a0);

			float a = t.x; float b = t.y; float c = t.z;
			float d = _B.x; float e = _B.y; float f = _B.z;
			float g = cross.x; float h = cross.y; float i = cross.z;

			float detA = a*e*i + b*f*g + c*d*h - c*e*g - b*d*i - a*f*h;

		   float t0 = detA/denom;

		   return  a0 + (_A * t0); //Projected closest point on segment A
		  //  pB = b0 + (_B * t1) //Projected closest point on segment B

			//https://stackoverflow.com/questions/2824478/shortest-distance-between-two-line-segments
		}

		//public static Vector3 ClosestPointOnLine (Vector3 line1Start, Vector3 line1End, Vector3 line2Start, Vector3 line2End)
		//{
				
		//}

	}
}
