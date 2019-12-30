using UnityEngine;
using UnityEditor;
using System.Collections;

using Voxeland5;

namespace Voxeland5 
{
	[CustomEditor(typeof(Chunk))]
	public class ChunkEditor : Editor
	{
		public bool showMeshLog;
		public bool showAmbientLog;

		Chunk chunk;
		Layout layout;
		public override void OnInspectorGUI ()
		{
			chunk = (Chunk)target;

			if (layout == null) layout = new Layout();
			layout.margin = 0; layout.rightMargin = 0;
			layout.field = Layout.GetInspectorRect();
			layout.cursor = new Rect();
			layout.undoObject = chunk;
			layout.undoName =  "Voxelump settings change";
			layout.dragChange = true;
			layout.change = false;

			if (chunk.meshWorker != null) chunk.meshWorker.OnGUI(layout);
			if (chunk.ambientWorker != null) chunk.ambientWorker.OnGUI(layout);
			if (chunk.colliderApplier != null) chunk.colliderApplier.OnGUI(layout);

			layout.Field( chunk.coord.vector2, "Coord");
			layout.Field( chunk.rect.offset.vector2, "Rect Offset");
			layout.Field( chunk.rect.size.vector2, "Rect Size");
			//layout.Field( chunk.pos.min, "Pos Offset");
			//layout.Field( chunk.pos.size, "Pos Size");

			layout.Field(chunk.hiMesh, "Hi Mesh");
			layout.Field(chunk.loMesh, "Lo Mesh");
			layout.Field(chunk.grassMesh, "Grass Mesh");

			layout.Par(5);
			//layout.Label("Stage");
			//layout.margin += 10;
			//layout.Field(ref chunk.stage.mesh, "Mesh");
			//layout.Field(ref chunk.stage.ambient, "Ambient");
			//layout.Field(ref chunk.stage.grass, "Grass");
			//layout.Field(ref chunk.stage.constructor, "Constructor");
			if (layout.Button("Clear")) chunk.Clear();
//			if (layout.Button("Refresh")) chunk.Refresh();
			layout.margin -= 10;
			
			layout.Par(5);
//			layout.Toggle(chunk.planarReady, "Planar Ready");
//			layout.Toggle(chunk.areasGenerated, "Areas Generated");

			layout.Par(5);
			//layout.Toggle(chunk.props!=null, "Has Props");
			layout.Toggle(chunk.loMesh!=null || chunk.hiMesh!=null, "Has Mesh");

			layout.Par(5);
//			if (layout.Button("Rebuild")) { chunk.Clear(); chunk.Refresh(); }

			Layout.SetInspectorRect(layout.field);

			Repaint();
		}


	}
}
