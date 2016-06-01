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
public class PrettyPolyLayer : ScriptableObject {

	public enum LayerType {
		ScatterEdge,
		SolidEdge,
		ScatterFill,
		SolidFill
	}

	[Tooltip("Defines the layer's drawing ruleset.")]
    public LayerType layerType = LayerType.SolidFill;
	
	[Tooltip("Seed value for random number generator.")]
    public int seed = 0;
	
	[Space(10)]
	[Header("Position Settings")]
	
	[Tooltip("Shifts polygon's points in a direction.")]
    public Vector3 posOffset = Vector3.zero;
	
	[Tooltip("Shrinks or grows polygon's points along normals.")]
    public float dirOffset = 0;
	
	[Tooltip("Space between objects in scatter layers.")]
    public float spacing = 1;
	
	[Tooltip("Amount to randomly offset positions of objects in scatter layers.")]
    public float positionVariation = 0;
	
	[Tooltip("How often objects in scatter layers should be placed.")]
    [Range(0,1)] public float placementFrequency = 1;

    [Tooltip("If false, ScatterFill rejects tiles with at least 1 vertex out of bounds; otherwise, all verts must be out of bounds to be rejected.")]
    public bool allowOverflow = false;
	
	[Space(10)]
	[Header("Rotation Settings")]
	
	[Tooltip("If true, object rotations are relative to normals.")]
    public bool followPath = true;
	
	[Tooltip("If true, every other object is rotated in the opposite direction.")]
    public bool alternateAngles = false;
	
	[Tooltip("Base rotation of each scattered object.")]
    [Range(0,360)] public float angle = 0;

	[Tooltip("Amount to randomly offset rotations of objects in scatter layers.")]
    [Range(0,180)] public float angleVariation = 0;
	
	[Tooltip("Normal direction to draw an edge.")]
    [Range(-180,180)] public float naturalAngle = 0;
	
	[Tooltip("Angular range for edge placement.\n0 = only in natural direction\n1 = all the way around")]
    [Range(0,1)] public float angularPlacementRange = 0;
	
	[Space(10)]
	[Header("Scale Settings")]

	[Tooltip("Size of scattered objects.")]
    public float size = 1;

	[Tooltip("Amount to randomly offset sizes of objects in scatter layers.")]
    public float sizeVariation;
	
	[Space(10)]
	[Header("Color Settings")]
	
	[Tooltip("Base color.")]
    public Color color = Color.white;	

	[Tooltip("Amount to randomly offset the hue.")]
    [Range(0,1)] public float hueVariation;

	[Tooltip("Amount to randomly offset the saturation.")]
    [Range(0,1)] public float saturationVariation;

	[Tooltip("Amount to randomly offset the color value.")]
    [Range(0,1)] public float valueVariation;

	[Tooltip("Amount to randomly offset the alpha.")]
    [Range(0,1)] public float alphaVariation;

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
		hsv.x = Mathf.Clamp01((hsv.x + Random.Range(-hueVariation,hueVariation) + 1f) % 1f);
		hsv.y = Mathf.Clamp01(hsv.y + Random.Range(-saturationVariation,saturationVariation));
		hsv.z = Mathf.Clamp01(hsv.z + Random.Range(-valueVariation,valueVariation));
		hsv.w = Mathf.Clamp01(hsv.w + Random.Range(-alphaVariation,alphaVariation));
		return ColorUtils.HSVtoRGB(hsv);
	}

	public bool ExistsInDirection (Vector2 dir) {
		float dev = -Vector3.Dot(dir, naturalDirection);
		float range = 1 - angularPlacementRange * 2;
		return (dev <= range);
	}

	public float GetSize (float t) {
		float s = size * (1 - Random.Range(-sizeVariation, sizeVariation));
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
		if (alternateAngles && (index % 2) == 1) a = 180 - a;
		if (!followPath) {
			if (Vector3.Dot(Vector3.up, normal) > 0) dir = Vector3.Cross(-Vector3.up, normal).normalized;
			else dir = Vector3.Cross(Vector3.right, normal).normalized;
		}

		return Quaternion.AngleAxis(a + Random.Range(-angleVariation, angleVariation), normal) * dir;
	}
}
}
