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