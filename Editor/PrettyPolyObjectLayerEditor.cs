using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace PrettyPoly {
[CanEditMultipleObjects]
[CustomEditor(typeof(PrettyPolyObjectLayer))]
public class PrettyPolyObjectLayerEditor : Editor {

	PrettyPolyObjectLayer prettyPolyObjectLayer {
		get { return target as PrettyPolyObjectLayer; }
	}

	PrettyPolyObjectLayer[] prettyPolyObjectLayers {
		get { return targets as PrettyPolyObjectLayer[]; }
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
				p.UpdateObjects();
			}
		}
	}
}
}