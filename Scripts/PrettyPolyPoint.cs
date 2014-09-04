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
using System.Collections;

namespace PrettyPoly {
[System.Serializable]
public class PrettyPolyPoint {

	public Vector3 position = Vector3.zero;
	public Vector3 inTangent = -Vector3.right;
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
		this.inTangent = -Vector3.right;
		this.outTangent = Vector3.right;
		this.color = Color.white;
		this.size = 1;
	}

	public PrettyPolyPoint (Vector3 position, Vector3 inTangent, Vector3 outTangent) {
		this.position = position;
		this.inTangent = inTangent;
		this.outTangent = outTangent;
		this.color = Color.white;
		this.size = 1;
	}

	public PrettyPolyPoint (Vector3 position, Vector3 inTangent, Vector3 outTangent, Color color, float size) {
		this.position = position;
		this.inTangent = inTangent;
		this.outTangent = outTangent;
		this.color = color;
		this.size = size;
	}
}
}