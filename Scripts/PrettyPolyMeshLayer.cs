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
using Paraphernalia.Math;

namespace PrettyPoly {
[System.Serializable]
public class PrettyPolyMeshLayer : PrettyPolyLayer {

	[Space(10)]
	[Header("Mesh Settings")]
	
	[Tooltip("If multiple sprites are used, they must come from a texture atlas.")]
    public Sprite[] sprites = new Sprite[]{};
    public AnimationCurve spriteDistributionCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));
    private int spriteIndex = 0;
    private Sprite sprite {
    	get {
    		if (spriteIndex >= sprites.Length || spriteIndex < 0) return null;
    		return sprites[spriteIndex];
    	}
    }

    [Tooltip("Sort Order")]
    public int sortOrder = 0;
	
	[Tooltip("Material used to render the sprite.")]
    public int materialIndex = 0; // TODO: Remove in favor of using materials directly
    public Material material;

    [Tooltip("Used for tiling solid fills.")]
    public float minTileSize = 100;

	public enum JoinType {
		None,
		Miter,
		Bevel,
		Rounded
	}

	[Tooltip("Solid edge join type for convex angles.")]
    public JoinType outerJoinType = JoinType.None;
	
	[Tooltip("Solid edge join type for concave angles.")]
    public JoinType innerJoinType = JoinType.None;

	[Tooltip("If false, the sprite is tiled.\n Otherwise, a single quad is used for edges.")]
    public bool allowStretching = false;
	
	[Tooltip("If true, this layer gives collision callbacks.")]
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

	public PrettyPolyMeshLayer () : base () {
		minTileSize = 100;
		outerJoinType = JoinType.Rounded;
		innerJoinType = JoinType.Rounded;
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

	public void RandomizeSprite () {
		float rand = spriteDistributionCurve.Evaluate(Random.value);
		spriteIndex = (int)Mathf.Round(Mathf.Lerp(0, sprites.Length - 1, rand));
	}

	public Vector2[] GetSpriteUVs () {
		return GetSpriteUVs(0, 1);
	}

	public Vector2[] GetSpriteUVs (float start, float end) {
		if (sprite == null) {
			Vector2 a = Vector2.right * start;
			Vector2 b = -Vector2.right * (1 - end);
			return new Vector2[] {
				Vector2.up + a,
				Vector2.one + b,
				Vector2.right + b,
				Vector2.zero + a
			};
		}

		Rect rect = new Rect(
			sprite.textureRect.x / (float)sprite.texture.width,
			sprite.textureRect.y / (float)sprite.texture.height,
			sprite.textureRect.width / (float)sprite.texture.width, 
			sprite.textureRect.height / (float)sprite.texture.height
		);

		float left = rect.x + rect.width * start;
		float right = left + rect.width * (end - start);
		float bottom = rect.y;
		float top = bottom + rect.height;

		return new Vector2[] {
			new Vector2(left, top),
			new Vector2(right, top),
			new Vector2(right, bottom),
			new Vector2(left, bottom)
		};
	}

	public float GetWidthToHeightRatio () {
		if (sprite == null) return 1;
		return sprite.textureRect.width / sprite.textureRect.height;
	}

	public Mesh GetMesh (PrettyPolyPoint[] points, bool closed) {
		FixParams();
		Vector3[] positions = GetOffset(points);
		if (positions.Length < 2) return null;
		Clear();
		
		float pathLength = positions.PathLength(closed);
				
		switch (layerType) {
			case (LayerType.ScatterEdge):
				AddScatterEdge(positions, pathLength, closed);
				break;
			case (LayerType.SolidEdge):
				AddSolidEdge(positions, pathLength, closed);
				break;
			case (LayerType.ScatterFill):
				AddScatterFill(positions, pathLength);
				break;
			case (LayerType.SolidFill):
				AddSolidFill(positions, pathLength);
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

	public void AddSolidEdge (Vector3[] points, float pathLength, bool closed) {
		Random.seed = seed;
		int segments = points.Length + (closed?1:0);
		int index = 0;
		float distTraveled = 0;
		float uvFrac = 0;
		
		Vector3 prev = points[points.Length-1];
		Vector3 curr = points[0];
		Vector3 next = points[1];
		Vector3 future = points[2%points.Length];
		
		Vector3 currDir = (prev - curr).normalized;
		Vector3 nextDir = (next - curr).normalized;
		Vector3 futureDir = (future - next).normalized;

		Vector3 prevOut = Vector3.Cross(currDir, -Vector3.forward);
		Vector3 currOut = Vector3.Cross(nextDir, -Vector3.forward);
		Vector3 nextOut = Vector3.Cross(futureDir, -Vector3.forward);
		
		bool prevOutExists = ExistsInDirection(prevOut);
		bool currOutExists = ExistsInDirection(currOut);
		bool nextOutExists = ExistsInDirection(nextOut);
		
		float currCavity = Vector3.Cross(prevOut, currOut).z;
		float nextCavity = Vector3.Cross(currOut, nextOut).z;
		
		for (int i = 1; i < segments; i++) {
			Random.seed = index + seed;
			
			prev = curr;
			curr = next;
			next = future;
			future = points[(i+2)%points.Length];

			currDir = nextDir;
			nextDir = futureDir;
			futureDir = (future - next).normalized;
			
			prevOut = currOut;
			currOut = nextOut;
			nextOut = Vector3.Cross(futureDir, -Vector3.forward);

			prevOutExists = currOutExists;
			currOutExists = nextOutExists;
			nextOutExists = ExistsInDirection(nextOut);

			currCavity = nextCavity;
			nextCavity = Vector3.Cross(currOut, nextOut).z;

			distTraveled += Vector3.Distance(curr, next);
			float t = distTraveled / pathLength;
			float size = GetSize(t);
			Color c = GetShiftedColor(color, t);

			if (currOutExists) {
				RandomizeSprite();
				Vector3 a = curr;
				Vector3 b = next;
				Line2D lineA = new Line2D(prevOut, prevOut + currDir);
				Line2D lineB = new Line2D(currOut, currOut + nextDir);
				Line2D lineC = new Line2D(nextOut, nextOut + futureDir);
				Vector3 abIntersect = (Vector3)lineA.Intersect(lineB);
				Vector3 bcIntersect = (Vector3)lineB.Intersect(lineC);
				
				if (prevOutExists && (closed || i != 1)) {
					if (currCavity < 0) {
						switch (outerJoinType) {
							case JoinType.Miter:
								AddSolidEdgeMiterJoin(curr, currOut, prevOut, size, c, false, ref index, ref uvFrac);
								break;
							case JoinType.Bevel:
								AddSolidEdgeBevelJoin(curr, currOut, prevOut, abIntersect, size, c, false, ref index, ref uvFrac);
								break;
							case JoinType.Rounded:
								float rot = Vector3.Angle(prevOut, currOut);
								AddSolidEdgeRoundedJoin(curr, currOut, prevOut, rot, size, c, false, ref index, ref uvFrac);
								break;
						}
					}
					else {
						a = curr + (abIntersect - currOut) * size;
						Vector3 pivot = curr + abIntersect * size;
						switch (innerJoinType) {
							case JoinType.Miter:
								AddSolidEdgeMiterJoin(pivot, -currOut, -prevOut, size, c, true, ref index, ref uvFrac);
								break;
							case JoinType.Bevel:
								AddSolidEdgeBevelJoin(pivot, -currOut, -prevOut, -abIntersect, size, c, true, ref index, ref uvFrac);
								break;
							case JoinType.Rounded:
								float rot = Vector3.Angle(prevOut, currOut);
								AddSolidEdgeRoundedJoin(pivot, -prevOut, -currOut, rot, size, c, true, ref index, ref uvFrac);
								break;
						}
					}
				}

				if (nextOutExists && nextCavity > 0) {
					b = next + (bcIntersect - currOut) * size;
				}
				
				RandomizeSprite();
				if (closed || i != segments-1) {
					AddSolidEdgeSegment(a, b, currOut, size, c, ref index, ref uvFrac);
				}
				else {
					AddSolidEdgeSegment(a, points[(i+1)%points.Length], currOut, size, c, ref index, ref uvFrac);
				}
			}
		}
	}

	public void AddSolidEdgeRoundedJoin (Vector3 pos, Vector3 outward, Vector3 prevOut, float rotation, float size, Color c, bool flipUVs, ref int index, ref float uvFrac) {
		int segments = Mathf.CeilToInt(Mathf.Abs(rotation));
		if (segments == 0) return;
		
		Vector2[] quadUVs = GetSpriteUVs();
		Vector4 tan = (Vector4)Vector3.right;
		tan.w = 1;
		tans.AddRange(new Vector4[] {tan, tan});
		norms.AddRange(new Vector3[] {-Vector3.forward, -Vector3.forward});
		colors.AddRange(new Color[] {c, c});
		verts.AddRange(new Vector3[] {pos, pos + prevOut * size});
		uvs.AddRange(new Vector2[] {(quadUVs[2] + quadUVs[3]) * 0.5f, quadUVs[0]});

		Quaternion rot = Quaternion.AngleAxis(rotation / (float)segments, -Vector3.forward);
		prevOut = rot * prevOut;
		for (int i = 1; i < segments; i++) {
			float frac = (float)i / (float)segments;
			prevOut = rot * prevOut;
			verts.Add(pos + prevOut * size);
			uvs.Add(Vector2.Lerp(quadUVs[0], quadUVs[1], frac));
			colors.Add(c);
			norms.Add(-Vector3.forward);
			tans.Add(tan);
			tris.AddRange(new int[] {index+i, index+i+1, index});
		}

		verts.Add(pos + outward * size);
		uvs.Add(quadUVs[1]);
		colors.Add(c);
		norms.Add(-Vector3.forward);
		tan = (Vector4)Vector3.right;
		tan.w = 1;
		tans.Add(tan);
		tris.AddRange(new int[] {index+segments, index+segments+1, index});

		index += segments + 2;
	}

	public void AddSolidEdgeMiterJoin (Vector3 pos, Vector3 outward, Vector3 prevOut, float size, Color c, bool flipUVs, ref int index, ref float uvFrac) {
		verts.AddRange(new Vector3[] {
			pos + prevOut * size,
			pos + outward * size,
			pos
		});

		Vector2[] quadUVs = GetSpriteUVs();
		int[] triArray = new int[] {index, index+1, index+2};

		if (flipUVs) {
			quadUVs.Reverse();
			triArray.Reverse();
		}
		Vector4 tan = (Vector4)Vector3.right;
		tan.w = 1;
		
		uvs.AddRange(new Vector2[] {quadUVs[0], quadUVs[1], (quadUVs[2] + quadUVs[3]) * 0.5f});
		colors.AddRange(new Color[] {c, c, c});
		norms.AddRange(new Vector3[] {-Vector3.forward, -Vector3.forward, -Vector3.forward});
		tans.AddRange(new Vector4[] {tan, tan, tan});
		tris.AddRange(triArray);
		index += 3;
	}

	public void AddSolidEdgeBevelJoin (Vector3 pos, Vector3 outward, Vector3 prevOut, Vector3 bevelOut, float size, Color c, bool flipUVs, ref int index, ref float uvFrac) {
		verts.AddRange(new Vector3[] {
			pos + prevOut * size,
			pos + bevelOut * size,
			pos,
			pos + bevelOut * size,
			pos + outward * size,
			pos,
		});

		Vector2[] quadUVs = GetSpriteUVs();
		int[] triArray = new int[] {index, index+1, index+2, index+3, index+4, index+5};
		
		Vector4 tan = (Vector4)Vector3.right;
		tan.w = 1;
		
		uvs.AddRange(new Vector2[] {quadUVs[0], quadUVs[1], quadUVs[3], quadUVs[0], quadUVs[1], quadUVs[2]});
		colors.AddRange(new Color[] {c, c, c, c, c, c});
		norms.AddRange(new Vector3[] {-Vector3.forward, -Vector3.forward, -Vector3.forward, -Vector3.forward, -Vector3.forward, -Vector3.forward});
		tans.AddRange(new Vector4[] {tan, tan, tan, tan, tan, tan});
		tris.AddRange(triArray);
		index += 6;
	}

	public void AddSolidEdgeSegment (Vector3 a, Vector3 b, Vector3 outward, float size, Color c, ref int index, ref float uvFrac) {
		Vector3 dir = (b - a).normalized;
		float dist = Vector3.Distance(a, b);
		float segLen = size * GetWidthToHeightRatio();
		outward *= size;
		Vector4 tan = (Vector4)Vector3.right;
		tan.w = 1;

		float distTraveled = 0;
		float distToNext = Mathf.Min(segLen * (1 - uvFrac), dist);
		float nextUvFrac = uvFrac + (distToNext / segLen);
		Vector3 curr = a;
		Vector3 next = a + dir * distToNext;
		if (allowStretching) {
			distToNext = dist;
			uvFrac = 0;
			nextUvFrac = 1;
			curr = a;
			next = b;
		}

		while (distTraveled < dist) {
			
			verts.AddRange(new Vector3[] {
				curr + outward,
				next + outward,
				next,
				curr
			});
			
			uvs.AddRange(GetSpriteUVs(uvFrac, nextUvFrac));
			colors.AddRange(new Color[] {c, c, c, c});
			norms.AddRange(new Vector3[] {-Vector3.forward, -Vector3.forward, -Vector3.forward, -Vector3.forward});
			tans.AddRange(new Vector4[] {tan, tan, tan, tan});
			tris.AddRange(new int[] {index, index+1, index+3, index+1, index+2, index+3});
			index += 4;

			distTraveled += distToNext;
			uvFrac = nextUvFrac % 1f;
			distToNext = Mathf.Min(segLen * (1 - uvFrac), dist - distTraveled);
			nextUvFrac = uvFrac + (distToNext / segLen);
			curr = next;
			next = curr + dir * distToNext;
		}
	}

	public void AddScatterEdge (Vector3[] points, float pathLength, bool closed) {
		int segments = points.Length + (closed?1:0);
		int index = 0;
		float distTraveled = 0;
		for (int i = 1; i < segments; i++) {
			AddScatterEdgeSegment(points[i-1], points[i%points.Length], pathLength, ref distTraveled, ref index);
		}
	}

	public void AddScatterEdgeSegment (Vector3 a, Vector3 b, float pathLength, ref float distTraveled, ref int index) {	
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
			AddScatterQuad(p, dir, ref index, (distTraveled + dist * frac) / pathLength);
		}
		distTraveled += dist;
	}
		
	public void AddScatterQuad (Vector3 position, Vector3 dir, ref int index, float t) {
		if (placementFrequency < Random.value) return;

		Random.seed = index + seed;
		RandomizeSprite();
		dir = GetDirection(dir, index, t);
		Vector3 right = Vector3.Cross(dir, -Vector3.forward);
		Vector3 up = Vector3.Cross(-Vector3.forward, right);

		float s = GetSize(t);
		Vector3 p = GetPosition(position, right, up, s);

		if (sprite != null) up *= sprite.bounds.extents.y / sprite.bounds.extents.x;
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

	public void AddSolidFill (Vector3[] points, float pathLength) {

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

	public void AddScatterFill (Vector3[] points, float pathLength) {
		Polygon poly = new Polygon(points);
		Bounds b = points.GetBounds();
		float s = size * 2 * spacing;
		
		if (allowOverflow) {
			poly = poly.GetOffset(2 * s);
			b.Expand(s);
		}
		
		int index = 0;
		for (float x = b.min.x; x < b.max.x; x += s) {
			for (float y = b.min.y; y < b.max.y; y += s) {
				Vector3 p = new Vector3(x, y, 0);
				if (poly.Contains(p)) {
					AddScatterQuad (p, Vector3.right, ref index, 0);
				}
			}
		}
	}
}
}
