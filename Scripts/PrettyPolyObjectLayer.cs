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
public class PrettyPolyObjectLayer : PrettyPolyLayer {
	
	public Object prefab;

	public void UpdateObjects (Transform root, PrettyPolyPoint[] points, bool closed) {
		if (prefab == null) return;
		root.DestroyChildren();

		Vector3[] positions = System.Array.ConvertAll(points, p => p.position);
		if (points.Length < 2) return;

		float pathLength = positions.PathLength(closed);
		
		switch (layerType) {
			case (LayerType.Stroke):
				AddStroke(root, positions, pathLength, closed);
				break;
			case (LayerType.Line):
				// AddLine(root, positions, pathLength, closed);
				break;
			case (LayerType.Cap):
				// AddCap(root, positions);
				break;
			case (LayerType.InnerFill):
				AddInnerFill(root, positions, pathLength);
				break;
			case (LayerType.OuterFill):
				// AddOuterFill(root, positions, pathLength);
				break;
		}
	}

	public void AddStroke (Transform root, Vector3[] points, float pathLength, bool closed) {
		int segments = points.Length + (closed?1:0);
		int index = 0;
		float distTraveled = 0;
		for (int i = 1; i < segments; i++) {
			AddStrokeSegment(root, points[i-1], points[i%points.Length], pathLength, ref distTraveled, ref index);
		}
	}

	public void AddStrokeSegment (Transform root, Vector3 a, Vector3 b, float pathLength, ref float distTraveled, ref int index) {	
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
			AddObject(root, p, dir, ref index, (distTraveled + dist * frac) / pathLength);
		}
		distTraveled += dist;
	}
		
	public void AddObject (Transform root, Vector3 position, Vector3 dir, ref int index, float t) {
		if (placementFrequency < Random.value) return;

		Vector3 normal = -Vector3.forward;

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
		
		GameObject g = GameObject.Instantiate(prefab) as GameObject;
		g.transform.parent = root;
		g.transform.localPosition = p;
		g.transform.localRotation.SetLookRotation(Vector3.forward, up);
		g.transform.localScale = Vector3.one * s;
		SpriteRenderer spriteRenderer = g.GetComponent<SpriteRenderer>();
		if (spriteRenderer) {
			spriteRenderer.color = GetShiftedColor(color, t);
		}

		index++;
	}

	public void AddInnerFill (Transform root, Vector3[] points, float pathLength) {
		Polygon poly = new Polygon(points);
		Bounds b = points.GetBounds();
		float s = size * 2 * spacing;
		int index = 0;
		for (float x = b.min.x; x < b.max.x; x += s) {
			for (float y = b.min.y; y < b.max.y; y += s) {
				Vector3 p = new Vector3(x, y, 0);
				if (poly.Contains(p)) {
					AddObject(root, p, Vector3.right, ref index, 0);
				}
			}
		}
	}

	public void AddOuterFill (Transform root, Vector3[] points, float pathLength) {

	}
}
}
