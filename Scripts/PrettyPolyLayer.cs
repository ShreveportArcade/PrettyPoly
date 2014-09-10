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
public class PrettyPolyLayer {

	public string name = "New Layer";

	public enum LayerType {
		Stroke,
		Line,
		Cap,
		InnerFill,
		OuterFill,
		StrokeFill
	}

	public LayerType layerType = LayerType.Stroke;
	public int seed = 0;
	
	// position
	public Vector3 posOffset = Vector3.zero;
	public float dirOffset = 0;
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
	public float minTileSize = 100;

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
		name = "New Layer";
		spacing = 1;
		placementFrequency = 1;
		followPath = true;
		size = 1;
		minTileSize = 100;
		color = Color.white;
		angleOffsets = new AnimationCurve(new Keyframe(0,0), new Keyframe(1,0));
		sizeOffsets = new AnimationCurve(new Keyframe(0,0), new Keyframe(1,0));
		hueOffsets = new AnimationCurve(new Keyframe(0,0), new Keyframe(1,0));
		saturationOffsets = new AnimationCurve(new Keyframe(0,0), new Keyframe(1,0));
		valueOffsets = new AnimationCurve(new Keyframe(0,0), new Keyframe(1,0));
		alphaOffsets = new AnimationCurve(new Keyframe(0,0), new Keyframe(1,0));
	}

	public Vector3 naturalDirection {
		get { 
			float r = Mathf.Deg2Rad * naturalAngle;
			return new Vector3(Mathf.Cos(r), Mathf.Sin(r), 0); 
		}
	}

	public Vector3[] GetOffset (PrettyPolyPoint[] points) {
		Vector2[] path = System.Array.ConvertAll(points, p => (Vector2)p.position);
		Polygon poly = new Polygon(path);
		return System.Array.ConvertAll(poly.GetOffsetPath(dirOffset), p => (Vector3)p + posOffset);
	}

	public void FixParams () {
		if (spacing < 0.01f) spacing = 0.01f;
		if (size < 0.01f) size = 0.01f;
	}

	public Color GetShiftedColor (Color color, float t) {
		Vector4 hsv = ColorUtils.RGBtoHSV(color);
		hsv.x = Mathf.Clamp01((hsv.x + Random.Range(-hueVariation,hueVariation) + 1f + hueOffsets.Evaluate(t)) % 1f);
		hsv.y = Mathf.Clamp01(hsv.y + Random.Range(-saturationVariation,saturationVariation) + saturationOffsets.Evaluate(t));
		hsv.z = Mathf.Clamp01(hsv.z + Random.Range(-valueVariation,valueVariation) + valueOffsets.Evaluate(t));
		hsv.w = Mathf.Clamp01(hsv.w + Random.Range(-alphaVariation,alphaVariation) + alphaOffsets.Evaluate(t));
		return ColorUtils.HSVtoRGB(hsv);
	}

	public bool ExistsInDirection (Vector2 dir) {
		float dev = -Vector3.Dot(dir, naturalDirection);
		return (dev <= 1 - angularPlacementRange * 2);
	}

	public float GetSize (float t) {
		float s = size * (1 - Random.Range(-sizeVariation, sizeVariation)) + sizeOffsets.Evaluate(t);
		if (s < 0.001f && s > -0.001f) s = 0.001f * Mathf.Sign(s);
		return s;
	}

	public Vector3 GetPosition (Vector3 position, Vector3 right, Vector3 up, float size) {
		return position +
			right * Random.Range(-positionVariation, positionVariation) * size + 
			up * Random.Range(-positionVariation, positionVariation) * size;
	}

	public Vector3 GetDirection (Vector3 dir, int index, float t) {
		Vector3 normal = -Vector3.forward;
		float a = angle;
		a += angleOffsets.Evaluate(t);
		if (alternateAngles && (index % 2) == 1) a = 180 - a;
		if (!followPath) {
			if (Vector3.Dot(Vector3.up, normal) > 0) dir = Vector3.Cross(-Vector3.up, normal).normalized;
			else dir = Vector3.Cross(Vector3.right, normal).normalized;
		}

		return Quaternion.AngleAxis(a + Random.Range(-angleVariation, angleVariation), normal) * dir;
	}
}
}
