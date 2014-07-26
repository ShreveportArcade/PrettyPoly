using UnityEngine;
using System.Collections;
using Paraphernalia.Utils;

public class PrettyPolyExporter : MonoBehaviour {

	public MeshFilter[] meshFilters;

	[ContextMenu("Export Scene")]
	public void ExportScene () {

		// this doesn't seem to work for very complex things, consider another way to export
		ColladaExporter exporter = new ColladaExporter(Application.dataPath + "/exporter.dae", true);

		foreach (MeshFilter meshFilter in meshFilters) {
			if (Application.isPlaying) {
				exporter.AddGeometry(meshFilter.name, meshFilter.mesh);
			}
			else {
				exporter.AddGeometry(meshFilter.name, meshFilter.sharedMesh);
			}
		}

		exporter.Save();
	}
}
