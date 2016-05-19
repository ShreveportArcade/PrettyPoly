using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace PrettyPoly {
[CustomEditor(typeof(PrettyPolyMeshLayer))]
public class PrettyPolyMeshLayerEditor : Editor {

	PrettyPolyMeshLayer prettyPolyMeshLayer {
		get { return target as PrettyPolyMeshLayer; }
	}

	PrettyPolyMeshLayer[] prettyPolyMeshLayers {
		get { return targets as PrettyPolyMeshLayer[]; }
	}

	private static List<PrettyPoly> _prettyPolys;
	public static List<PrettyPoly> prettyPolys {
		get {
			if (_prettyPolys == null) {
		        _prettyPolys = new List<PrettyPoly>(FindObjectsOfType(typeof(PrettyPoly)) as PrettyPoly[]);
		    }
		    return _prettyPolys;
		}
	}

	public override void OnInspectorGUI () {
		DrawDefaultInspector();

		if (GUI.changed) {
			EditorUtility.SetDirty(target);
			foreach (PrettyPoly p in prettyPolys) {
				EditorUtility.SetDirty(p);
				p.UpdateMesh();
			}
		}
	}
}
}