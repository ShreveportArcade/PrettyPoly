using UnityEngine;
using System.Collections.Generic;
using Paraphernalia.Extensions;
using Paraphernalia.Utils;

namespace PrettyPoly {
[System.Serializable]
public class PrettyPolyLayer {

	public string name = "New Layer";
	public Sprite sprite;
	public int materialIndex = 0;
	public enum LayerType {
		Stroke,
		Line,
		Cap,
		InnerFill,
		OuterFill
	}
	public LayerType layerType = LayerType.Stroke;
	public int seed = 0;

	// position
	public Vector3 posOffset = Vector3.zero;
	public Vector2 dirOffset = Vector2.zero;
	public float spacing = 1;
	public float positionVariation = 0;
	[Range(0,1)] public float placementFrequency = 1;
	
	// rotation
	public bool followPath = true;
	public bool alternateAngles = false;
	[Range(0,360)] public float angle = 0;
	public AnimationCurve angleOffsets;
	[Range(0,180)] public float angleVariation = 0;
	[Range(-180,180)] public float naturalAngle = 0;
	[Range(0,1)] public float angularPlacementRange = 0;
	
	// scale
	public float size = 1;
	public AnimationCurve sizeOffsets;
	[Range(0,1)] public float sizeVariation;

	// color
	public Color color = Color.white;	
	public AnimationCurve hueOffsets;
	[Range(0,1)] public float hueVariation;
	public AnimationCurve saturationOffsets;
	[Range(0,1)] public float saturationVariation;
	public AnimationCurve valueOffsets;
	[Range(0,1)] public float valueVariation;
	public AnimationCurve alphaOffsets;
	[Range(0,1)] public float alphaVariation;
	
	public PrettyPolyLayer () {
		angleOffsets = new AnimationCurve(new Keyframe(0,0), new Keyframe(1,0));
		sizeOffsets = new AnimationCurve(new Keyframe(0,0), new Keyframe(1,0));
		hueOffsets = new AnimationCurve(new Keyframe(0,0), new Keyframe(1,0));
		saturationOffsets = new AnimationCurve(new Keyframe(0,0), new Keyframe(1,0));
		valueOffsets = new AnimationCurve(new Keyframe(0,0), new Keyframe(1,0));
		alphaOffsets = new AnimationCurve(new Keyframe(0,0), new Keyframe(1,0));
	}

	protected List<Vector3> verts = new List<Vector3>();
	protected List<Vector2> uvs = new List<Vector2>();
	protected List<int> tris = new List<int>();
	protected List<Color> colors = new List<Color>();
	protected List<Vector3> norms = new List<Vector3>();
	protected List<Vector4> tans = new List<Vector4>();

	private Mesh _mesh;
	protected Mesh mesh {
		get {
			if (_mesh == null) {
				_mesh = new Mesh();
			}
			return _mesh;
		}
	}

	public Vector3 naturalDirection {
		get { 
			float r = Mathf.Deg2Rad * naturalAngle;
			return new Vector3(Mathf.Cos(r), Mathf.Sin(r), 0); 
		}
	}

	public void Clear () {
		mesh.Clear();
		verts.Clear();
		uvs.Clear();
		tris.Clear();
		colors.Clear();
		norms.Clear();
		tans.Clear();
	}

	public Vector2[] GetSpriteUVs () {
		Rect rect = new Rect(
			sprite.textureRect.x / (float)sprite.texture.width,
			sprite.textureRect.y / (float)sprite.texture.height,
			sprite.textureRect.width / (float)sprite.texture.width, 
			sprite.textureRect.height / (float)sprite.texture.height
		);

		float left = rect.x;
		float right = left + rect.width;
		float bottom = rect.y;
		float top = bottom + rect.height;

		return new Vector2[] {
			new Vector2(left, top),
			new Vector2(right, top),
			new Vector2(right, bottom),
			new Vector2(left, bottom)
		};
	}

	public Bounds GetBounds (Vector3[] points) {
		Bounds b = new Bounds();
		Vector3 max = points[0];
		Vector3 min = points[0];
		for (int i = 1; i < points.Length; i++) {
			Vector3 p = points[i];
			if (p.x > max.x) max.x = p.x;
			else if (p.x < min.x) min.x = p.x;
			else if (p.y > max.y) max.y = p.y;
			else if (p.y < min.y) min.y = p.y;
		}
		b.SetMinMax(min, max);
		return b;
	}

	public Mesh GetMesh (PrettyPolyPoint[] points, bool closed, float winding) {
		Vector3[] positions = System.Array.ConvertAll(points, p => p.position);
		if (points.Length < 2) return null;
		Clear();

		float pathLength = positions.PathLength(closed);
		
		switch (layerType) {
			case (LayerType.Stroke):
				if (sprite == null) return null;
				AddStroke(positions, pathLength, closed);
				break;
			case (LayerType.Line):
				AddLine(positions, pathLength, closed);
				break;
			case (LayerType.Cap):
				// AddCap(positions);
				break;
			case (LayerType.InnerFill):
				if (winding > 0) positions = positions.Reverse();
				AddInnerFill(positions, pathLength);
				break;
			case (LayerType.OuterFill):
				AddOuterFill(positions, pathLength);
				break;
		}

		mesh.vertices = verts.ToArray();
		mesh.uv = uvs.ToArray();
		mesh.colors = colors.ToArray();
		mesh.normals = norms.ToArray();
		mesh.tangents = tans.ToArray();
		mesh.triangles = tris.ToArray();
		
		return mesh;
	}

	public void AddLine (Vector3[] points, float pathLength, bool closed) {
		int segments = points.Length + (closed?1:0);
		// int index = 0;
		float distTraveled = 0;
		for (int i = 1; i < segments+1; i++) {
			// Vector3 prev = points[i-1];
			Vector3 curr = points[i%points.Length];
			Vector3 next = points[(i+1)%points.Length];

			float segLen = size * 2 * spacing;
			float dist = Vector3.Distance(curr, next);
			if (dist < segLen) return;
			
			Vector3 dir = (next - curr).normalized;
			Vector3 normal = -Vector3.forward;
			Vector3 outward = Vector3.Cross(dir, normal);
			float dev = -Vector3.Dot(outward, naturalDirection);
			if (dev > 1 - angularPlacementRange * 2) return;

			
			distTraveled += dist;
		}
	}

	public void AddStroke (Vector3[] points, float pathLength, bool closed) {
		int segments = points.Length + (closed?1:0);
		int index = 0;
		float distTraveled = 0;
		for (int i = 1; i < segments; i++) {
			AddStrokeSegment(points[i-1], points[i%points.Length], pathLength, ref distTraveled, ref index);
		}
	}

	public void AddStrokeSegment (Vector3 a, Vector3 b, float pathLength, ref float distTraveled, ref int index) {	
		float segLen = size * 2 * spacing;
		float dist = Vector3.Distance(a, b);
		if (dist < segLen) return;
		
		Vector3 dir = (b - a).normalized;
		float segments = dist / segLen;

		for (int i = 0; i < segments; i++) {
			float frac = (float)i / segments;
			Vector3 p = Vector3.Lerp(a, b, frac);
			Random.seed = i + seed;
			AddStrokeQuad(p, dir, ref index, (distTraveled + dist * frac) / pathLength);
		}
		distTraveled += dist;
	}
		
	public void AddStrokeQuad (Vector3 position, Vector3 dir, ref int index, float t) {
		if (placementFrequency < Random.value) return;

		Vector3 normal = -Vector3.forward;
		Vector3 outward = Vector3.Cross(dir, normal);
		float dev = -Vector3.Dot(outward, naturalDirection);
		if (dev > 1 - angularPlacementRange * 2) return;

		Random.seed = index + seed;

		float a = angle;
		a += angleOffsets.Evaluate(t);
		if (alternateAngles && (index % 2) == 1) a = 180 - a;
		if (!followPath) {
			if (Vector3.Dot(Vector3.up, normal) > 0) dir = Vector3.Cross(-Vector3.up, normal).normalized;
			else dir = Vector3.Cross(Vector3.right, normal).normalized;
		}


		dir = Quaternion.AngleAxis(a + Random.Range(-angleVariation, angleVariation), normal) * dir;
		Vector3 right = Vector3.Cross(dir, normal);
		Vector3 up = Vector3.Cross(normal, right);

		float s = size * (1 - Random.Range(-sizeVariation, sizeVariation)) + sizeOffsets.Evaluate(t);
		if (s < 0.001f && s > -0.001f) s = 0.001f * Mathf.Sign(s);
		Vector3 p = position + posOffset +
			right * (Random.Range(-positionVariation, positionVariation) + dirOffset.x) * s + 
			up * (Random.Range(-positionVariation, positionVariation) + dirOffset.y) * s;

		up *= sprite.bounds.extents.y / sprite.bounds.extents.x;
		verts.AddRange(new Vector3[] {
			p + (up - right) * s,
			p + (up + right) * s,
			p + (-up + right) * s,
			p + (-up - right) * s,
		});
		
		uvs.AddRange(GetSpriteUVs());
		Vector4 hsv = ColorUtils.RGBtoHSV(color);
		hsv.x = Mathf.Clamp01((hsv.x + Random.Range(-hueVariation,hueVariation) + 1f + hueOffsets.Evaluate(t)) % 1f);
		hsv.y = Mathf.Clamp01(hsv.y + Random.Range(-saturationVariation,saturationVariation) + saturationOffsets.Evaluate(t));
		hsv.z = Mathf.Clamp01(hsv.z + Random.Range(-valueVariation,valueVariation) + valueOffsets.Evaluate(t));
		hsv.w = Mathf.Clamp01(hsv.w + Random.Range(-alphaVariation,alphaVariation) + alphaOffsets.Evaluate(t));
		Color c = ColorUtils.HSVtoRGB(hsv);
		colors.AddRange(new Color[] {c, c, c, c});

		norms.AddRange(new Vector3[] {-Vector3.forward, -Vector3.forward, -Vector3.forward, -Vector3.forward});

		Vector4 tan = (Vector4)right;
		tan.w = 1;
		tans.AddRange(new Vector4[] {tan, tan, tan, tan});

		tris.AddRange(new int[] {index+3, index+2, index, index+2, index+1, index});
		index += 4;
	}

	public void AddInnerFill (Vector3[] points, float pathLength) {
		verts.AddRange(points);

		float p = 0;
		for (int i = 0; i < points.Length; i++) {
			norms.Add(-Vector3.forward);
			tans.Add(Vector3.right);

			float t = p / pathLength;
			Vector4 hsv = ColorUtils.RGBtoHSV(color);
			hsv.x = Mathf.Clamp01((hsv.x + Random.Range(-hueVariation,hueVariation) + 1f + hueOffsets.Evaluate(t)) % 1f);
			hsv.y = Mathf.Clamp01(hsv.y + Random.Range(-saturationVariation,saturationVariation) + saturationOffsets.Evaluate(t));
			hsv.z = Mathf.Clamp01(hsv.z + Random.Range(-valueVariation,valueVariation) + valueOffsets.Evaluate(t));
			hsv.w = Mathf.Clamp01(hsv.w + Random.Range(-alphaVariation,alphaVariation) + alphaOffsets.Evaluate(t));
			colors.Add(ColorUtils.HSVtoRGB(hsv));

			Vector3 p1 = points[i];
			Vector3 p2 = points[(i+1) % points.Length];
			p += Vector3.Distance(p1, p2);
		}

		Bounds b = GetBounds(points);
		float size = Mathf.Max(b.max.x - b.min.x, b.max.y - b.min.y);
		for (int i = 0; i < points.Length; i++) {
			Vector3 pt = (points[i] - b.min) / size;
			uvs.Add((Vector2)pt);
		}

		tris.AddRange(Triangulator.Triangulate(points));
	}

	public void AddOuterFill (Vector3[] points, float pathLength) {

	}
}
}
