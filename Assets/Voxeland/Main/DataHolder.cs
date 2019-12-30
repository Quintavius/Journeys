using UnityEngine;
using System.Collections;

using Voxeland5;

public class DataHolder : MonoBehaviour 
{
	public Data data;
	public bool init;

	public void OnDrawGizmos ()
	{
		if (init) 
		{
			//data.FillNoise(-1000,-1000,2000);
			init = false;
		}
	}
}
