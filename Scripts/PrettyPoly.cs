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
using System.Collections.Generic;
using Paraphernalia.Extensions;
using Paraphernalia.Utils;

namespace PrettyPoly {
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class PrettyPoly : MonoBehaviour {

	public delegate void OnCollision2DCallback(Collision2D collision, PrettyPolyLayer layer);
	public static event OnCollision2DCallback onCollision2D = delegate{};

	public bool closed = false;
	public bool solid = true;
	public bool addCollider = false;
	public PrettyPolyPoint[] points = new PrettyPolyPoint[0];
	[Range(0, 10)] public int subdivisions = 0;

	[SortingLayer] public string sortingLayerName = "Default";
	public int sortingOrder = 0;

	public enum CurveType {
		Linear,
		CatmullRom,
		CubicBezier,
	}
	public CurveType curveType = CurveType.CatmullRom;
	
	public PrettyPolyMeshLayer[] meshLayers;

	public List<PrettyPolyMeshLayer> sortedLayers {
		get {
			if (meshLayers == null) return null;
			List<PrettyPolyMeshLayer> _sortedLayers = new List<PrettyPolyMeshLayer>(meshLayers);
			_sortedLayers.Sort((a,b) => a.sortOrder.CompareTo(b.sortOrder));
			return _sortedLayers;
		}
	}

	public PrettyPolyObjectLayer[] objectLayers;	

	private Mesh _mesh;
	public Mesh mesh {
		get {
			if (_mesh == null) {
				_mesh = new Mesh();
			}
			return _mesh;
		}
	}

	public Material[] materials {
		get {
			if (Application.isPlaying) return GetComponent<Renderer>().materials;
			else return GetComponent<Renderer>().sharedMaterials;
		}
		set {
			if (Application.isPlaying) GetComponent<Renderer>().materials = value;
			else GetComponent<Renderer>().sharedMaterials = value;
		}
	}

	public PrettyPolyPoint GetClosestPoint (Vector3 point) {
		float sqDist = Mathf.Infinity;
		PrettyPolyPoint closest = null;
		for (int i = 0; i < points.Length; i++) {
			float d = (points[i].position - point).sqrMagnitude;
			if (d < sqDist) {
				sqDist = d;
				closest = points[i];
			}
		}
		return closest;
	}

	public int GetClosestSegment (Vector3 point) {
		int index = -1;
        float dist = Mathf.Infinity;
        int segments = points.Length + (closed?1:0);
        for (int i = 1; i < segments; i++) {
			Vector3 p1 = points[i-1].position;
			Vector3 p2 = points[i%points.Length].position;
			float len = Vector3.Distance(p1, p2);
			if (len == 0) return i;
		    
		    float d;
		    int off = 0;
		    float t = Vector3.Dot(point - p1, p2 - p1) / len;
		    if (t < 0) {
			    d = Vector3.Distance(point, p1);
			}
		    else if (t > 1) {
		    	d = Vector3.Distance(point, p2);
		    }
		    else {
		    	Vector3 proj = p1 + t * (p2 - p1);
		    	d = Vector3.Distance(point, proj);
		    }

		    if (d < dist) {
		    	dist = d;
		    	index = (i + off)%points.Length;
		    }
        }
        return index;
	}

	public PrettyPolyPoint[] GetCurve () {
		switch (curveType) {
			case CurveType.CatmullRom:
				return GetCatmullRom();
			case CurveType.CubicBezier:
				return GetCubicBezier();
			default:
				return points;
		}
	}

	public PrettyPolyPoint[] GetCatmullRom () {
		int len = points.Length;
		if (len <= 2) return points;
		List<PrettyPolyPoint> newPoints = new List<PrettyPolyPoint>();
		for (int i = 0; i < len; i++) {
			PrettyPolyPoint prev = points[(i - 1 + len) % len];
			PrettyPolyPoint start = points[i];
			PrettyPolyPoint end = points[(i + 1) % len];
			PrettyPolyPoint next = points[(i + 2) % len];

			if (!closed) {
				if (i == 0) prev = start;
				else if (i == len - 2) next = end;
				else if (i == len - 1) return newPoints.ToArray();
			}

			for (float j = 0; j < subdivisions; j++) {
				float t = j / (float)subdivisions;
				Vector3 pos = Interpolate.CatmullRom(prev.position, start.position, end.position, next.position, t);
				PrettyPolyPoint prettyPolyPoint = new PrettyPolyPoint(pos);
				prettyPolyPoint.color = Color.Lerp(start.color, end.color, t);
				prettyPolyPoint.size = Mathf.Lerp(start.size, end.size, t);
				newPoints.Add(prettyPolyPoint);
			}
		}
		return newPoints.ToArray();
	}

	public PrettyPolyPoint[] GetCubicBezier () {
		int len = points.Length;
		if (len <= 2) return points;
		List<PrettyPolyPoint> newPoints = new List<PrettyPolyPoint>();
		for (int i = 0; i < len; i++) {
			if (!closed && i == len - 1) continue;
			PrettyPolyPoint start = points[i];
			PrettyPolyPoint end = points[(i + 1) % len];
			Vector3 cp1 = start.position + start.outTangent;
			Vector3 cp2 = end.position + end.inTangent;
			
			for (float j = 0; j < subdivisions; j++) {
				float t = j / (float)subdivisions;
				Vector3 pos = Interpolate.CubicBezier(start.position, cp1, cp2, end.position, t);
				PrettyPolyPoint prettyPolyPoint = new PrettyPolyPoint(pos);
				prettyPolyPoint.color = Color.Lerp(start.color, end.color, t);
				prettyPolyPoint.size = Mathf.Lerp(start.size, end.size, t);
				newPoints.Add(prettyPolyPoint);
			}
		}
		return newPoints.ToArray();
	}

	public void AddCollider (PrettyPolyPoint[] pts) {
		if (closed && solid) {
			gameObject.DestroyComponent<EdgeCollider2D>();
			PolygonCollider2D c = gameObject.GetOrAddComponent<PolygonCollider2D>();
			c.SetPath(0, System.Array.ConvertAll(pts, point => (Vector2)point.position));
		}
		else {
			gameObject.DestroyComponent<PolygonCollider2D>();
			EdgeCollider2D c = gameObject.GetOrAddComponent<EdgeCollider2D>();
			c.points = System.Array.ConvertAll(pts, point => (Vector2)point.position);
			if (closed) {
				List<Vector2> closedPts = new List<Vector2>(c.points);
				closedPts.Add(closedPts[0]);
				c.points = closedPts.ToArray();
			}
		}
	}

	public void UpdateMaterials () {
		if (sortedLayers == null) return;
		List<Material> mats = new List<Material>();
		Material prevMat = null;
		foreach (PrettyPolyMeshLayer layer in sortedLayers) {
			Material mat = layer.material;
			if (mat != prevMat) mats.Add(mat);
			prevMat = mat;
		}
		materials = mats.ToArray();
	}

	[ContextMenu("Update Mesh")]
	public void UpdateMesh () {
		PrettyPolyPoint[] pts = (subdivisions > 0)? GetCurve(): points;
		if (addCollider) AddCollider(pts);
		else gameObject.DestroyComponent<PolygonCollider2D>();
		if (meshLayers == null || meshLayers.Length == 0) {
			mesh.Clear();
			return;
		}
		
		mesh.Clear();
		List<Vector3> verts = new List<Vector3>();
		List<Vector2> uvs = new List<Vector2>();
		List<Color> colors = new List<Color>();
		List<Vector3> norms = new List<Vector3>();
		List<Vector4> tans = new List<Vector4>();
		List<List<int>> tris = new List<List<int>>();
		
		for (int i = 0; i < materials.Length; i++) {
			tris.Add(new List<int>());
		}
		
		Vector3[] positions = System.Array.ConvertAll(pts, point => point.position);
		float winding = closed ? positions.ClosedWinding() : positions.Winding();
		pts = (winding < 0) ? pts.Reverse() : pts.Shift(2);

		int matIndex = -1;
		Material prevMat = null;
		for (int i = 0; i < meshLayers.Length; i++) {
			PrettyPolyMeshLayer layer = sortedLayers[i];
			if (layer == null) continue;
			Mesh m = layer.GetMesh(pts, closed);
			if (m == null) continue;
			Material mat = layer.material;
			if (mat != prevMat) matIndex++;
			prevMat = mat;
			tris[matIndex].AddRange(
				System.Array.ConvertAll(m.triangles, t => t + verts.Count)
			);			
			verts.AddRange(m.vertices);
			uvs.AddRange(m.uv);
			colors.AddRange(m.colors);
			norms.AddRange(m.normals);
			tans.AddRange(m.tangents);
		}

		mesh.vertices = verts.ToArray();
		mesh.uv = uvs.ToArray();
		mesh.colors = colors.ToArray();
		mesh.normals = norms.ToArray();
		mesh.tangents = tans.ToArray();
		mesh.subMeshCount = materials.Length;
		
		for (int i = materials.Length - 1; i >= 0; i--) {
			mesh.SetTriangles(tris[i].ToArray(), i);
		}		

		mesh.RecalculateBounds();
		if (Application.isPlaying) GetComponent<MeshFilter>().mesh = mesh;
		else GetComponent<MeshFilter>().sharedMesh = mesh;

		UpdateRenderer();
	}

	public void UpdateRenderer () {
		GetComponent<Renderer>().sortingLayerName = sortingLayerName;
		GetComponent<Renderer>().sortingOrder = sortingOrder;
	}

	[ContextMenu("Update Objects")]
	public void UpdateObjects () {
		if (objectLayers == null || objectLayers.Length == 0) return;

		PrettyPolyPoint[] pts = (subdivisions > 0)? GetCurve(): points;
		pts = pts.Shift(1);

		for (int i = 0; i < objectLayers.Length; i++) {
			if (objectLayers[i] == null) continue;
			objectLayers[i].UpdateObjects(transform, pts, closed);
		}

		if (addCollider) AddCollider(pts);
		else gameObject.DestroyComponent<PolygonCollider2D>();
	}

	void OnCollisionEnter2D (Collision2D collision) {
		Vector2 norm = -collision.contacts[0].normal;

		for (int i = 0; i < meshLayers.Length; i++) { 
			PrettyPolyMeshLayer layer = meshLayers[i];
			if (layer != null && layer.isTrigger && layer.ExistsInDirection(norm)) {
				onCollision2D(collision, layer);
			}
		}
	}
}
}