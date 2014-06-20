using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace PrettyPoly {
[CanEditMultipleObjects]
[CustomEditor(typeof(PrettyPoly))]
public class PrettyPolyEditor : Editor {

	PrettyPoly prettyPoly {
		get { return target as PrettyPoly; }
	}

	PrettyPoly[] prettyPolys {
		get { return targets as PrettyPoly[]; }
	}

	[MenuItem("GameObjects/Create Other/PrettyPoly #&n")]
	static void CreatePrettyPoly () {
		GameObject go = new GameObject("PrettyPoly");
		PrettyPoly p = go.AddComponent<PrettyPoly>();
		p.points = new PrettyPolyPoint[2] {
			new PrettyPolyPoint(-Vector3.right), 
			new PrettyPolyPoint(Vector3.right)
		};
		p.layers = new PrettyPolyLayer[] {new PrettyPolyLayer()};
		Undo.RegisterCreatedObjectUndo(go, "Created PrettyPoly");
	}

	public override void OnInspectorGUI() {
		EditorUtility.SetSelectedWireframeHidden(prettyPoly.renderer, true);
		DrawDefaultInspector();

		if (GUILayout.Button("Update Mesh")) {
			if (target != null) prettyPoly.UpdateMesh();
			else {
				foreach (PrettyPoly p in prettyPolys) {
					p.UpdateMesh();
				}
			}
		}

		if (GUI.changed) {
			prettyPoly.UpdateMesh();
			EditorUtility.SetDirty(target);
		}
	}

	void OnSceneGUI() {
		if (target == null) return;

		Event e = Event.current;

		if (e.type == EventType.ValidateCommand && e.commandName == "UndoRedoPerformed") {
			prettyPoly.UpdateMesh();
			Repaint();
		}
		if (prettyPoly.points.Length == 0) return;

		Handles.matrix = prettyPoly.transform.localToWorldMatrix;
		Handles.color = Color.blue;
		int len = prettyPoly.points.Length;
		for (int i = 0; i < len - (prettyPoly.closed?0:1); i++) {
			Vector3 p1 = prettyPoly.points[i].position;
			Vector3 p2 = prettyPoly.points[(i + 1) % len].position;
			Handles.DrawLine(p1, p2);
		}

		EditorGUI.BeginChangeCheck();
		PrettyPolyPoint[] points = new PrettyPolyPoint[prettyPoly.points.Length];

		if (e.command || e.control) {
			RemovePoint();
			points = prettyPoly.points;
		}
		else {
			AddPoint();
			for (int i = 0; i < points.Length; i++) {
				PrettyPolyPoint point = new PrettyPolyPoint(prettyPoly.points[i]);
				
				Handles.color = Color.white;
				GUI.SetNextControlName("pretty poly point " + i);
				if (i == 0) Handles.color = Color.magenta;
				if (i == 1) Handles.color = Color.green;
				point.position = Handles.FreeMoveHandle(
					point.position, 
					Quaternion.identity, 
					1, 
					Vector3.zero, 
					Handles.CircleCap
				);
				point.position.z = 0;
				points[i] = point;
			}
		}

		if (EditorGUI.EndChangeCheck()) {
			Undo.RecordObject(target, "moved prettyPoly point");
			prettyPoly.points = points;
			prettyPoly.UpdateMesh();
			EditorUtility.SetDirty(target);
		}
	}

	void AddPoint () {
		List<PrettyPolyPoint> points = new List<PrettyPolyPoint>(prettyPoly.points);
		int len = prettyPoly.points.Length;
		for (int i = 0; i < len; i++) {
			int n = (i+1)%len;
			Vector3 p1 = prettyPoly.points[i].position;
			Vector3 p2 = prettyPoly.points[n].position;
			Handles.color = Color.green;
			GUI.SetNextControlName("remove pretty poly point " + i);
			Vector3 mid = (p1 + p2) * 0.5f;
			if (Handles.Button(mid, Quaternion.identity, 0.5f, 0.5f, Handles.CircleCap)) {
				points.Insert(n, new PrettyPolyPoint(mid));
				Undo.RecordObject(target, "added prettyPoly point");
				prettyPoly.points = points.ToArray();
				prettyPoly.UpdateMesh();
				EditorUtility.SetDirty(target);
				break;
			}
		}
	}	

	void RemovePoint (int index) {
        if (index < 0 || index >= prettyPoly.points.Length) return;

        Undo.RecordObject(target, "removed prettyPoly point");
		List<PrettyPolyPoint> points = new List<PrettyPolyPoint>(prettyPoly.points);
        points.RemoveAt(index);
        prettyPoly.points = points.ToArray();
		prettyPoly.UpdateMesh();
		EditorUtility.SetDirty(target);
    }

    void RemovePoint () {
    	for (int i = 0; i < prettyPoly.points.Length; i++) {
			Handles.color = Color.red;
			GUI.SetNextControlName("remove pretty poly point " + i);
			if (Handles.Button(prettyPoly.points[i].position, Quaternion.identity, 1, 1, Handles.CircleCap)) {
				RemovePoint(i);
				break;
			}
		}
    }
}
}
