using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;

namespace Voxeland5.Interface
{	
	public class Val : Attribute
	{
		public string name;
		public string cat;
		public bool isLeft; //for toggles
		public FieldInfo field;


		public static readonly Dictionary<Type,Val[]> attributesCaches = new Dictionary<Type, Val[]>();

		public static void Cache (Type type)
		{
			List<Val> attList = new List<Val>();
			FieldInfo[] fields = type.GetFields();
				
			for (int f=0; f<fields.Length; f++)
			{
				Val valAtt = Attribute.GetCustomAttribute(fields[f], typeof(Val)) as Val;
				if (valAtt == null) continue;
					
				valAtt.field = fields[f];
					
				attList.Add(valAtt);
			}
				
			if (attributesCaches.ContainsKey(type)) attributesCaches[type] = attList.ToArray();
			else attributesCaches.Add(type, attList.ToArray());
		}
	}
}
