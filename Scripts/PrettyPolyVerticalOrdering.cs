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
using System.Collections;

namespace PrettyPoly {
[ExecuteInEditMode]
[RequireComponent(typeof(PrettyPoly))]
public class PrettyPolyVerticalOrdering : MonoBehaviour {

    public float orderMultiplier = 100;
    public int orderOffset = 0;
    public bool isStatic = true;

    [SerializeField, HideInInspector]private PrettyPoly _prettyPoly;
    public PrettyPoly prettyPoly {
        get {
            if (_prettyPoly == null) {
                _prettyPoly = GetComponent<PrettyPoly>();
            }
            return _prettyPoly;
        }
    }
    
    void Start () {
        UpdateSortOrder();
    }

    #if UNITY_EDITOR
    void Update () {
        if (!Application.isPlaying) UpdateSortOrder();
    }
    #endif

    void OnEnable () {
        if (!isStatic) StartCoroutine("SortCoroutine");
    }

    IEnumerator SortCoroutine () {
        while (enabled && !isStatic) {
            UpdateSortOrder();
            yield return new WaitForEndOfFrame();
        }
    }

    [ContextMenu("Update Sort Order")]
    public void UpdateSortOrder () {
        prettyPoly.sortingOrder = (int)(-transform.position.y * orderMultiplier) + orderOffset;
    }
}
}