using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Paraphernalia.Utils;
using Paraphernalia.Extensions;

namespace PrettyPoly {
public class PrettyPolyPainter : EditorWindow {
	
	public static GameObject prefab;
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

	[MenuItem ("Window/PrettyPoly Painter")]
	static void Create () {
		window.minSize = new Vector2(250, 360);
		window.name = "PrettyPoly";
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
		GameObject go;
		PrettyPoly p;
		if (prefab == null) {
			go = new GameObject("PrettyPoly");
			p = go.AddComponent<PrettyPoly>();		
			p.layers = new PrettyPolyLayer[] {new PrettyPolyLayer()};
		}
		else {
			go = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
			p = go.GetComponent<PrettyPoly>();
		}
		
		Vector3[] points = currentPoints.ToArray();
		float winding = points.Winding();
		if (winding < 0) points = points.Reverse();
		bool closed = (Mathf.Abs(winding) > 0.5f);
		p.closed = closed;
		float len = points.PathLength(closed);
		points = points.Resample((int)(len / spacing), closed);
		points = points.RemoveColinear(maxAng, closed);
		Vector3 center = points.Center();
		go.transform.position = center;
		points = points.MoveBy(-center);
		p.points = System.Array.ConvertAll(points, point => new PrettyPolyPoint(point));
		p.UpdateMesh();

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
		prefab = EditorGUILayout.ObjectField("PrettyPoly Prefab", prefab, typeof(GameObject), false) as GameObject;
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