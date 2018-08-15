/*
Copyright (C) 2018 Nolan Baker

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
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace PrettyPoly {
[CanEditMultipleObjects]
[CustomEditor(typeof(PrettyPolyObjectLayer))]
public class PrettyPolyObjectLayerEditor : Editor {

    PrettyPolyObjectLayer prettyPolyObjectLayer {
        get { return target as PrettyPolyObjectLayer; }
    }

    PrettyPolyObjectLayer[] prettyPolyObjectLayers {
        get { return targets as PrettyPolyObjectLayer[]; }
    }

    private static List<PrettyPoly> _prettyPolys;
    public static List<PrettyPoly> prettyPolys {
        get {
            if (_prettyPolys == null) {
                _prettyPolys = new List<PrettyPoly>(FindObjectsOfType(typeof(PrettyPoly)) as PrettyPoly[]);
            }
            return _prettyPolys;
        }
    }

    public override void OnInspectorGUI () {
        DrawDefaultInspector();

        if (GUI.changed) {
            EditorUtility.SetDirty(target);
            foreach (PrettyPoly p in prettyPolys) {
                EditorUtility.SetDirty(p);
                p.UpdateObjects();
            }
        }
    }
}
}