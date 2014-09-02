﻿/*
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
using Paraphernalia.Math;

namespace PrettyPoly {
[System.Serializable]
public class PrettyPolyMeshLayer : PrettyPolyLayer {

	public Sprite sprite;
	public int materialIndex = 0;

	public enum JoinType {
		None,
		Miter,
		Bevel,
		Rounded
	}

	public JoinType joinType = JoinType.None;

	public bool isTrigger = false;

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

	public Mesh GetMesh (PrettyPolyPoint[] points, bool closed, float winding) {
		Vector3[] positions = System.Array.ConvertAll(points, p => p.position);
		if (points.Length < 2) return null;
		Clear();

		float pathLength = positions.PathLength(closed);
		if (winding < 0) positions = positions.Reverse();
				
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
		if (sprite == null) return;
		int segments = points.Length + (closed?1:0);
		int index = 0;
		float distTraveled = 0;
		Vector3 prev = points[points.Length-1];
		Vector3 curr = points[0];
		Vector3 prevOut = Vector3.Cross((curr - prev).normalized, -Vector3.forward);
		bool prevExists = ExistsInDirection(prevOut);
		for (int i = 1; i < segments+1; i++) {
			prev = curr;
			curr = points[i%points.Length];
			Vector3 next = points[(i+1)%points.Length];
			Random.seed = index + seed;

			Vector3 outward = Vector3.Cross((next - curr).normalized, -Vector3.forward);
			bool existsOut = ExistsInDirection(outward);

			float cavity = Vector3.Cross(prevOut, outward).z;
			distTraveled += Vector3.Distance(curr, next);
			float t = distTraveled / pathLength;
			float size = GetSize(t);
			Color c = GetShiftedColor(color, t);
			if (prevExists && existsOut && Mathf.Abs(cavity) > 0.01f) {
				if (cavity < 0) {
					switch (joinType) {
						case JoinType.Miter:
							AddMiter(curr, outward, prevOut, size, c, ref index);
							break;
						case JoinType.Bevel:
							Line2D lineA = new Line2D(prevOut, prevOut + (curr - prev).normalized);
							Line2D lineB = new Line2D(outward, outward + (next - curr).normalized);
							Vector3 bevelOut = (Vector3)lineA.Intersect(lineB);
							AddBevel(curr, outward, prevOut, bevelOut, size, c, ref index);
							break;
						case JoinType.Rounded:
							float rot = Vector3.Angle(prevOut, outward);
							AddRound(curr, prevOut, rot, size, c, ref index);
							break;
					}
				}
			}

			prevOut = outward;
			prevExists = existsOut;
			if (existsOut) AddLineSegment(curr, next, outward, size, c, ref index);
		}
	}

	public void AddRound (Vector3 pos, Vector3 outward, float rotation, float size, Color c, ref int index) {
		Vector2[] quadUVs = GetSpriteUVs();
		Vector4 tan = (Vector4)outward;
		tan.w = 1;
		tans.AddRange(new Vector4[] {tan, tan});
		norms.AddRange(new Vector3[] {-Vector3.forward, -Vector3.forward});
		colors.AddRange(new Color[] {c, c});
		verts.AddRange(new Vector3[] {pos, pos + outward});
		uvs.AddRange(new Vector2[] {(quadUVs[2] + quadUVs[3]) * 0.5f, quadUVs[0]});

		int segments = (int)rotation;
		Quaternion rot = Quaternion.AngleAxis(rotation / (float)segments, -Vector3.forward);
		outward = rot * outward;
		for (int i = 1; i < segments; i++) {
			float frac = (float)i / (float)segments;
			outward = rot * outward;
			verts.Add(pos + outward);
			uvs.Add(Vector2.Lerp(quadUVs[0], quadUVs[1], frac));
			colors.Add(c);
			norms.Add(-Vector3.forward);
			tan = (Vector4)outward;
			tan.w = 1;
			tans.Add(tan);
			tris.AddRange(new int[] {index+i, index+i+1, index});
		}

		index += segments + 1;
	}

	public void AddMiter (Vector3 pos, Vector3 outward, Vector3 prevOut, float size, Color c, ref int index) {
		verts.AddRange(new Vector3[] {
			pos + prevOut * size,
			pos + outward * size,
			pos
		});

		Vector2[] quadUVs = GetSpriteUVs();
		uvs.AddRange(new Vector2[] {quadUVs[0], quadUVs[1], (quadUVs[2] + quadUVs[3]) * 0.5f});

		colors.AddRange(new Color[] {c, c, c});
		norms.AddRange(new Vector3[] {-Vector3.forward, -Vector3.forward, -Vector3.forward});

		Vector4 tan = (Vector4)outward;
		tan.w = 1;
		tans.AddRange(new Vector4[] {tan, tan, tan});

		tris.AddRange(new int[] {index, index+1, index+2});
		index += 3;
	}

	public void AddBevel (Vector3 pos, Vector3 outward, Vector3 prevOut, Vector3 bevelOut, float size, Color c, ref int index) {
		verts.AddRange(new Vector3[] {
			pos + prevOut * size,
			pos + bevelOut * size,
			pos + outward * size,
			pos
		});

		Vector2[] quadUVs = GetSpriteUVs();
		uvs.AddRange(new Vector2[] {quadUVs[0], quadUVs[1], quadUVs[0], quadUVs[3]});

		colors.AddRange(new Color[] {c, c, c, c});
		norms.AddRange(new Vector3[] {-Vector3.forward, -Vector3.forward, -Vector3.forward, -Vector3.forward});

		Vector4 tan = (Vector4)outward;
		tan.w = 1;
		tans.AddRange(new Vector4[] {tan, tan, tan, tan});

		tris.AddRange(new int[] {index, index+1, index+3, index+1, index+2, index+3});
		index += 4;
	}

	public void AddLineSegment (Vector3 a, Vector3 b, Vector3 outward, float size, Color c, ref int index) {
		outward *= size;
		verts.AddRange(new Vector3[] {
			a + outward,
			b + outward,
			b,
			a
		});
		
		uvs.AddRange(GetSpriteUVs());
		colors.AddRange(new Color[] {c, c, c, c});
		norms.AddRange(new Vector3[] {-Vector3.forward, -Vector3.forward, -Vector3.forward, -Vector3.forward});

		Vector4 tan = (Vector4)outward;
		tan.w = 1;
		tans.AddRange(new Vector4[] {tan, tan, tan, tan});

		tris.AddRange(new int[] {index, index+1, index+3, index+1, index+2, index+3});
		index += 4;
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
		Vector3 dir = (b - a).normalized;
		Vector3 outward = Vector3.Cross(dir, -Vector3.forward);
		if (!ExistsInDirection(outward)) return;
			
		float segLen = size * 2 * spacing;
		float dist = Vector3.Distance(a, b);
		if (dist < segLen) return;
		
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

		Random.seed = index + seed;

		dir = GetDirection(dir, index, t);
		Vector3 right = Vector3.Cross(dir, -Vector3.forward);
		Vector3 up = Vector3.Cross(-Vector3.forward, right);

		float s = GetSize(t);
		Vector3 p = GetPosition(position, right, up, s);

		up *= sprite.bounds.extents.y / sprite.bounds.extents.x;
		verts.AddRange(new Vector3[] {
			p + (up - right) * s,
			p + (up + right) * s,
			p + (-up + right) * s,
			p + (-up - right) * s,
		});
		
		uvs.AddRange(GetSpriteUVs());
		
		Color c = GetShiftedColor(color, t);
		colors.AddRange(new Color[] {c, c, c, c});

		norms.AddRange(new Vector3[] {-Vector3.forward, -Vector3.forward, -Vector3.forward, -Vector3.forward});

		Vector4 tan = (Vector4)right;
		tan.w = 1;
		tans.AddRange(new Vector4[] {tan, tan, tan, tan});

		tris.AddRange(new int[] {index+3, index+2, index, index+2, index+1, index});
		index += 4;
	}

	public void AddInnerFill (Vector3[] points, float pathLength) {

		int offset = 0;
		// Polygon[] polys = (new Polygon(points)).Subdivide(Vector2.one * 5); //		HOPE
		// Polygon[] polys = (new Polygon(points)).TestSplit(); //						TEST
		Polygon[] polys = new Polygon[]{new Polygon(points)}; // 						NROMAL
		// Debug.Log(polys.Length);

		foreach (Polygon poly in polys) {
			Vector3[] v = System.Array.ConvertAll(poly.path, pt => new Vector3(pt.x, pt.y, poly.z));
			verts.AddRange(v);

			float p = 0;
			// Color randC = ColorUtils.Random(1); //									TEST
			for (int i = 0; i < v.Length; i++) {
				norms.Add(-Vector3.forward);
				tans.Add(Vector3.right);

				// colors.Add(randC); //												TEST
				float frac = p / pathLength; //											NORMAL
				colors.Add(GetShiftedColor(color, frac)); //							NORMAL

				Vector3 p1 = v[i];
				Vector3 p2 = v[(i+1) % v.Length];
				p += Vector3.Distance(p1, p2);
			}

			Bounds b = v.GetBounds();
			float size = Mathf.Max(b.max.x - b.min.x, b.max.y - b.min.y);
			if (size < minTileSize) size = minTileSize;
			for (int i = 0; i < v.Length; i++) {
				Vector3 pt = (v[i] - b.min) / size;
				uvs.Add((Vector2)pt);
			}

			int[] t = System.Array.ConvertAll(Triangulator.Triangulate(v), r => r + offset);
			tris.AddRange(t);
			offset += v.Length;
		}
	}

	public void AddOuterFill (Vector3[] points, float pathLength) {

	}
}
}
