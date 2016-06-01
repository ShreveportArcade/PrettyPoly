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
using Paraphernalia.Utils;
using Paraphernalia.Extensions;

namespace PrettyPoly {
public class PrettyPolyPainter : EditorWindow {
	
	public static float overlapThreshold = 0.5f;
	public static float spacing = 4;
	public static float maxAng = 10;
	
	// enum Mode { 
	// 	None,
	// 	DrawShapes,
	// 	DeletePoints,
	// 	PushPoints
	// }
	// static Mode mode = Mode.DrawShapes;
	static bool isPainting = false;
	static List<Vector3> currentPoints;

	static Tool lastTool;
	static SceneView.OnSceneFunc onSceneGUIFunc;
	static int painterHash;	
	static PrettyPolyPainter _window;
	static PrettyPolyPainter window {
		get {
			if (_window == null) {
				_window = (PrettyPolyPainter)EditorWindow.GetWindow(typeof(PrettyPolyPainter));
				painterHash = _window.GetHashCode();
			}
			return _window;
		}
	}

	static int selectedPrefab = 0;
	static string[] prefabPaths {
		get {
			return System.Array.ConvertAll(
				AssetDatabase.FindAssets("t:Prefab l:PrettyPoly"),
				guid => AssetDatabase.GUIDToAssetPath(guid)
			);
		}
	}

	static string[] prefabNames {
		get {
			return System.Array.ConvertAll(
				prefabPaths,
				path => System.IO.Path.GetFileNameWithoutExtension(path)
			);
		}
	}
	
	static GameObject prefab {
		get {
			if (prefabPaths.Length > 0) {
				return AssetDatabase.LoadAssetAtPath(prefabPaths[selectedPrefab], typeof(GameObject)) as GameObject;
			}
			return null;
		}
	}


	[MenuItem ("Window/PrettyPoly Painter")]
	static void Create () {
		window.minSize = new Vector2(250, 360);
		window.titleContent = new GUIContent("PrettyPoly Painter");
	}

    void Update () {
    	if (isPainting) Tools.current = Tool.None;
    	else lastTool = Tools.current;
    }

	void OnEnable () {
   		if (onSceneGUIFunc == null) {
   			onSceneGUIFunc = new SceneView.OnSceneFunc(OnSceneGUI);
   		}
   		currentPoints = new List<Vector3>();
    }

    void OnDisable () {
    	SceneView.onSceneGUIDelegate -= onSceneGUIFunc;
    }

    void Paint() {
		Ray r = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
		Plane p = new Plane(Vector3.forward, Vector3.zero);
		float d;
        if (p.Raycast(r, out d)) {
        	currentPoints.Add(r.GetPoint(d));
        }
	}

	void CreatePoly () {
		if (currentPoints.Count < 2) return;

		GameObject go;
		PrettyPoly p;
		if (prefab == null) {
			go = new GameObject("PrettyPoly");
			p = go.AddComponent<PrettyPoly>();		
		}
		else {
			go = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
			p = go.GetComponent<PrettyPoly>();
		}
		
		Vector3[] points = currentPoints.ToArray();
		float winding = points.Winding();
		bool closed = (Mathf.Abs(winding) > 0.5f);
		p.closed = closed;
		float len = points.PathLength(closed);
		points = points.Resample((int)(len / spacing), closed);
		points = points.RemoveColinear(maxAng, closed);
		points = points.RemoveOverlapping(overlapThreshold);
		Vector3 center = points.Average();
		go.transform.position = center;
		points = points.MoveBy(-center);
		p.points = System.Array.ConvertAll(points, point => new PrettyPolyPoint(point));
		p.UpdateMesh();
		p.UpdateObjects();

		Undo.RegisterCreatedObjectUndo(go, "Created PrettyPoly");
		currentPoints.Clear();
	}

	static void OnSceneGUI(SceneView sceneview) {
		Event e = Event.current;
		
		if (isPainting) {
			switch(e.type){
				case EventType.MouseDown:
					window.Paint();
					e.Use();
					break;
				case EventType.MouseDrag:
					window.Paint();
					HandleUtility.Repaint();
					e.Use();
					break;
				case EventType.MouseUp:
					window.CreatePoly();
					break;
			}
		}

		HandleUtility.AddDefaultControl(GUIUtility.GetControlID (painterHash, FocusType.Passive));
		window.DrawHandles();
		HandleUtility.Repaint();
		sceneview.Repaint();
	}

	void DrawHandles() {
		Handles.DrawAAPolyLine(2, currentPoints.ToArray());
	}

	void OnGUI () {
		selectedPrefab = EditorGUILayout.Popup("PrettyPoly Prefab", selectedPrefab, prefabNames);
		if (prefab == null) {
			EditorGUILayout.HelpBox("Prefab required", MessageType.Info, true);
			if (isPainting) {
				StopPainting();
			}
			return;
		}

		PrettyPoly prettyPoly = prefab.GetComponent<PrettyPoly>();
		if (prettyPoly == null) {
			EditorGUILayout.HelpBox("PrettyPoly component required", MessageType.Warning, true);
			if (isPainting) {
				StopPainting();
			}
			return;
		}

		overlapThreshold = EditorGUILayout.FloatField("Overlap Threshold", overlapThreshold);
		if (!isPainting) {
			if (GUILayout.Button("Paint")) {
				StartPainting();
			}
		}
		else if (GUILayout.Button("Stop")) {
			StopPainting();
		}

	}

	void StartPainting () {
		SceneView.onSceneGUIDelegate += onSceneGUIFunc;
		lastTool = Tools.current;
		Tools.current = Tool.None;
		isPainting = true;
	}

	void StopPainting () {
		SceneView.onSceneGUIDelegate -= onSceneGUIFunc;
		isPainting = false;
		Tools.current = lastTool;
	}
}
}