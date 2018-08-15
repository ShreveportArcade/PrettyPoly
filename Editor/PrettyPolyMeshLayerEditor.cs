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
using Paraphernalia.Extensions;

namespace PrettyPoly {
[CanEditMultipleObjects]
[CustomEditor(typeof(PrettyPolyMeshLayer))]
public class PrettyPolyMeshLayerEditor : Editor {

    private static int layer = 30;

    PrettyPolyMeshLayer prettyPolyMeshLayer {
        get { return target as PrettyPolyMeshLayer; }
    }

    private static PrettyPoly[] _prettyPolys;
    public static PrettyPoly[] prettyPolys {
        get {
            if (_prettyPolys == null) {
                _prettyPolys = FindObjectsOfType(typeof(PrettyPoly)) as PrettyPoly[];
            }
            return _prettyPolys;
        }
    }

    private static PrettyPoly _previewPoly;
    public PrettyPoly previewPoly {
        get {
            if (_previewPoly == null) {
                Vector3[] points = new Vector3[] {
                    new Vector3(-10, 10),
                    new Vector3(10, 10),
                    new Vector3(10, -10),
                    new Vector3(-10, -10)
                };
                GameObject go = GameObject.Find("previewPoly");
                if (go == null) go = new GameObject("previewPoly");
                // go.hideFlags = HideFlags.HideAndDontSave;
                go.layer = layer;
                _previewPoly = go.GetOrAddComponent<PrettyPoly>();
                _previewPoly.closed = true;
                _previewPoly.points = System.Array.ConvertAll(points, (p) => new PrettyPolyPoint(p));
            }

            return _previewPoly;
        }
    }

    private static Camera _previewPolyCam;
    public Camera previewPolyCam {
        get {
            if (_previewPolyCam == null) {
                GameObject go = GameObject.Find("previewPolyCam");
                if (go == null) go = new GameObject("previewPolyCam");
                // go.hideFlags = HideFlags.HideAndDontSave;
                go.transform.position = Vector3.forward * -10;
                _previewPolyCam = go.GetOrAddComponent<Camera>();
                _previewPolyCam.cullingMask = (int)Mathf.Pow(2, layer);
                _previewPolyCam.targetTexture = renderTexture;
            }

            return _previewPolyCam;
        }
    }

    private static RenderTexture _renderTexture;
    public RenderTexture renderTexture {
        get {
            if (_renderTexture == null) {
                _renderTexture = new RenderTexture(512, 
                    512, 
                    0, 
                    RenderTextureFormat.ARGB32);

                _renderTexture.Create();
            }
            return _renderTexture;
        }
    }

    public override void OnInspectorGUI () {
        DrawDefaultInspector();

        if (GUI.changed) {
            // if (targets != null && targets.Length > 0) previewPoly.meshLayers = targets as PrettyPolyMeshLayer[];
            // else previewPoly.meshLayers = new PrettyPolyMeshLayer[]{prettyPolyMeshLayer};
        
            EditorUtility.SetDirty(target);
            // EditorUtility.SetDirty(previewPoly.gameObject);
            foreach (PrettyPoly p in prettyPolys) {
                if (p != null && p.meshLayers != null && System.Array.Exists(p.meshLayers, (l) => l != null && prettyPolyMeshLayer != null && l == prettyPolyMeshLayer)) {
                    EditorUtility.SetDirty(p);
                    p.UpdateMaterials();
                    p.UpdateMesh();
                }
            }
        }
    }

  //   public override bool HasPreviewGUI() {
  //       return true;
  //   }

  //   public override void OnPreviewGUI(Rect r, GUIStyle background) {
  //       if (Event.current.type != EventType.Repaint) return;
  //       previewPoly.UpdateMaterials();
        // previewPoly.UpdateMesh();
        // previewPolyCam.Render();
        // EditorGUI.DrawPreviewTexture(r, renderTexture.ToTexture2D());
  //   }
}
}