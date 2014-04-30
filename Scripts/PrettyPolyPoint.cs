using UnityEngine;
using System.Collections;

namespace PrettyPoly {
[System.Serializable]
public class PrettyPolyPoint {

	public Vector3 position = Vector3.zero;
	public Vector3 inTangent = Vector3.right;
	public Vector3 outTangent = Vector3.right;
	public Color color = Color.white;
	public float size = 1;

	public PrettyPolyPoint (PrettyPolyPoint platformPoint) {
		this.position = platformPoint.position;
		this.inTangent = platformPoint.inTangent;
		this.outTangent = platformPoint.outTangent;
		this.color = platformPoint.color;
		this.size = platformPoint.size;
	}

	public PrettyPolyPoint (Vector3 position) {
		this.position = position;
		this.inTangent = Vector3.right;
		this.outTangent = Vector3.right;
		this.color = Color.white;
		this.size = 1;
	}

	public PrettyPolyPoint (Vector3 position, Vector3 tangent) {
		this.position = position;
		this.inTangent = tangent;
		this.outTangent = tangent;
		this.color = Color.white;
		this.size = 1;
	}

	public PrettyPolyPoint (Vector3 position, Vector3 tangent, Color color, float size) {
		this.position = position;
		this.inTangent = tangent;
		this.outTangent = tangent;
		this.color = color;
		this.size = size;
	}
}
}