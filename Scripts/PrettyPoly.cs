using UnityEngine;
using System.Collections.Generic;
using Paraphernalia.Extensions;
using Paraphernalia.Utils;

namespace PrettyPoly {
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class PrettyPoly : MonoBehaviour {

	public bool closed = false;
	public bool addCollider = false;
	public PrettyPolyPoint[] points = new PrettyPolyPoint[0];
	[Range(0, 8)] public int subdivisions = 0;
	public Interpolate.EaseType pathType = Interpolate.EaseType.Linear;
	
	[SerializeField, Array] public PrettyPolyLayer[] layers;	
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
			if (Application.isPlaying) return renderer.materials;
			else return renderer.sharedMaterials;
		}
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

	public void AddCollider (PrettyPolyPoint[] pts) {
		if (closed) {
			gameObject.DestroyComponent<EdgeCollider2D>();
			PolygonCollider2D c = gameObject.GetOrAddComponent<PolygonCollider2D>();
			c.SetPath(0, System.Array.ConvertAll(pts, point => (Vector2)point.position));
		}
		else {
			gameObject.DestroyComponent<PolygonCollider2D>();
			EdgeCollider2D c = gameObject.GetOrAddComponent<EdgeCollider2D>();
			c.points = System.Array.ConvertAll(pts, point => (Vector2)point.position);
		}
	}

	public void UpdateRenderer () {
		// if (layers[0].sprite == null) return;
		// MaterialPropertyBlock props = new MaterialPropertyBlock();
		// props.AddTexture("_MainTex", layers[0].sprite.texture);
		// renderer.SetPropertyBlock(props);
	}

	[ContextMenu("Update Mesh")]
	public void UpdateMesh () {
		if (layers.Length == 0 || layers[0].sprite == null) return;

		List<PrettyPolyLayer> sortedLayers = new List<PrettyPolyLayer>(layers);
		sortedLayers.Sort((a,b) => b.posOffset.z.CompareTo(a.posOffset.z));
		
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
		
		PrettyPolyPoint[] pts = (subdivisions > 0)? GetCurve(): points;
		float winding = System.Array.ConvertAll(pts, point => point.position).Winding();
		for (int i = 0; i < layers.Length; i++) {
			Mesh m = sortedLayers[i].GetMesh(pts, closed, winding);
			if (m == null) continue;
			tris[sortedLayers[i].materialIndex].AddRange(
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

		if (Application.isPlaying) GetComponent<MeshFilter>().mesh = mesh;
		else GetComponent<MeshFilter>().sharedMesh = mesh;

		UpdateRenderer();

		if (addCollider) AddCollider(pts);
	}
}
}