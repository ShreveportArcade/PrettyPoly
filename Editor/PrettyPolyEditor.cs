/*
Copyright (C) 2014 Nolan Baker

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, 
and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions 
of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
DEALINGS IN THE SOFTWARE.
*/

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace PrettyPoly {
[InitializeOnLoad]
[CanEditMultipleObjects]
[CustomEditor(typeof(PrettyPoly))]
public class PrettyPolyEditor : Editor {

	const float handleScale = 0.3f;

	PrettyPoly prettyPoly {
		get { return target as PrettyPoly; }
	}

	PrettyPoly[] prettyPolys {
		get { return targets as PrettyPoly[]; }
	}

	public static PrettyPoly selectedPoly;
	public static PrettyPolyPoint selectedPoint;
	public static int lastHotControl = 0;

	static PrettyPolyEditor () {
        EditorApplication.update += Update;
    }

    static void Update () {
        // Transform selection = Selection.activeTransform;
        // if (selection == null || selection.parent == null) {
        // 	selectedPoly = null;
        // 	return;
        // } 

        // PrettyPoly prettyPoly = selection.parent.gameObject.GetComponent<PrettyPoly>();
        // if (prettyPoly != null && selectedPoly != prettyPoly) {
        // 	Selection.activeGameObject = prettyPoly.gameObject;
        // 	selectedPoly = prettyPoly;
        // }
    }

	[MenuItem("GameObject/2D Object/PrettyPoly")]
	static void CreatePrettyPoly () {
		Camera sceneCam = SceneView.lastActiveSceneView.camera;
        Vector3 spawnPos = sceneCam.ViewportToWorldPoint(new Vector3(0.5f,0.5f,10f));
		GameObject go = new GameObject("PrettyPoly");
		go.transform.position = spawnPos;
		Selection.activeGameObject = go;

		PrettyPoly p = go.AddComponent<PrettyPoly>();
		UpdateLabels(p);
		p.points = new PrettyPolyPoint[] {
			new PrettyPolyPoint(new Vector3(-1,-1,0)), 
			new PrettyPolyPoint(new Vector3(1,-1,0)),
			new PrettyPolyPoint(new Vector3(0,1,0))
		};
		p.closed = true;
		Undo.RegisterCreatedObjectUndo(go, "Created PrettyPoly");
	}

	[MenuItem("Assets/Create/PrettyPoly/MeshLayer")]
	static void CreatePrettyPolyMeshLayer () {
		ScriptableObjectUtility.CreateAsset<PrettyPolyMeshLayer>();
	}
	
	[MenuItem("Assets/Create/PrettyPoly/ObjectLayer")]
	static void CreatePrettyPolyObjectLayer () {
		ScriptableObjectUtility.CreateAsset<PrettyPolyObjectLayer>();
	}
	

	public override void OnInspectorGUI() {
		EditorUtility.SetSelectedWireframeHidden(prettyPoly.GetComponent<Renderer>(), true);
		DrawDefaultInspector();
		UpdateLabels();

		if (GUILayout.Button("Update Materials")) {
			if (target != null) prettyPoly.UpdateObjects();
			if (prettyPolys != null) {
				foreach (PrettyPoly p in prettyPolys) {
					p.UpdateMaterials();
				}
			}
		}

		if (GUILayout.Button("Update Meshes")) {
			if (target != null) prettyPoly.UpdateMesh();
			if (prettyPolys != null) {
				foreach (PrettyPoly p in prettyPolys) {
					p.UpdateMesh();
				}
			}
		}

		if (GUILayout.Button("Update Objects")) {
			if (target != null) prettyPoly.UpdateObjects();
			if (prettyPolys != null) {
				foreach (PrettyPoly p in prettyPolys) {
					p.UpdateObjects();
				}
			}
		}

		if (GUI.changed) {
			prettyPoly.UpdateObjects();
			prettyPoly.UpdateMaterials();
			prettyPoly.UpdateMesh();
			EditorUtility.SetDirty(target);
		}
	}

	void UpdateLabels () {
		if (target != null) UpdateLabels(prettyPoly);
		else {
			foreach (PrettyPoly p in prettyPolys) {
				UpdateLabels(p);
			}
		}
	}

	static void UpdateLabels (PrettyPoly poly) {
		List<string> labels = new List<string>(AssetDatabase.GetLabels(poly.gameObject));
		if (!labels.Contains("PrettyPoly")) labels.Add("PrettyPoly");
		AssetDatabase.SetLabels(poly.gameObject, labels.ToArray());
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

		if (e.alt) {
			RemovePoint();
			points = prettyPoly.points;
		}
		else {
			if (e.shift) AddPoint();
			for (int i = 0; i < points.Length; i++) {
				PrettyPolyPoint point = new PrettyPolyPoint(prettyPoly.points[i]);
				
				Handles.color = Color.white;
				GUI.SetNextControlName("pretty poly point " + i);
				if (i == 0) Handles.color = Color.magenta;
				if (i == 1) Handles.color = Color.green;
				float size = GetHandleSize(point.position, 1);
				point.position = Handles.FreeMoveHandle(
					point.position, 
					Quaternion.identity, 
					size, 
					Vector3.zero, 
					Handles.CircleCap
				);
				point.position.z = 0;

				if (prettyPoly.curveType == PrettyPoly.CurveType.CubicBezier) {
					point.inTangent = TangentHandle(point.position, point.inTangent);
					point.outTangent = TangentHandle(point.position, point.outTangent);
				}

				points[i] = point;
			}
		}

		if (EditorGUI.EndChangeCheck()) {
			Undo.RecordObject(target, "moved prettyPoly point");
			prettyPoly.points = points;
			prettyPoly.UpdateMesh();
			prettyPoly.UpdateObjects();
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
			float size = GetHandleSize(mid, 0.5f);
			if (Handles.Button(mid, Quaternion.identity, size, size, Handles.CircleCap)) {
				Vector3 inT = (p1 - mid).normalized;
				Vector3 outT = (p2 - mid).normalized;
				points.Insert(n, new PrettyPolyPoint(mid, inT, outT));
				Undo.RecordObject(target, "added prettyPoly point");
				prettyPoly.points = points.ToArray();
				prettyPoly.UpdateMesh();
				prettyPoly.UpdateObjects();
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
		prettyPoly.UpdateObjects();
		EditorUtility.SetDirty(target);
    }

    void RemovePoint () {
    	for (int i = 0; i < prettyPoly.points.Length; i++) {
			Handles.color = Color.red;
   		 	float size = GetHandleSize(prettyPoly.points[i].position, 1);
   			GUI.SetNextControlName("remove pretty poly point " + i);
			if (Handles.Button(prettyPoly.points[i].position, Quaternion.identity, size, size, Handles.CircleCap)) {
				RemovePoint(i);
				break;
			}
		}
    }

    Vector3 TangentHandle (Vector3 point, Vector3 tangent) {
    	Handles.DrawLine(point, point + tangent);
    	Vector3 p = Handles.FreeMoveHandle(
			point + tangent, 
			Quaternion.identity, 
			GetHandleSize(point, 0.3f), 
			Vector3.zero, 
			Handles.CircleCap
		) - point;
		p.z = 0;
    	return p;
    }

    float GetHandleSize (Vector3 pos, float size) {
    	return HandleUtility.GetHandleSize(pos) * size * handleScale;
    }
}
}
