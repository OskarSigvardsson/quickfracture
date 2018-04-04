using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GK {
	public class KMeansTest : MonoBehaviour {

		// public GameObject MeshPrefab;

		// List<GameObject> hulls;

		// IEnumerator Start() {
		// 	hulls = new List<GameObject>();
		// 	var calc = new KMeansCalculator();
		// 	var pointCount = 40000;
		// 	var clusterCount = 10;
		// 	var clusters = new List<Vector3>(clusterCount);
		// 	var assignedClusters = new List<int>(pointCount);

		// 	var points = new List<Vector3>(pointCount);

		// 	for (int i = 0; i < pointCount; i++) {
		// 		Vector3 point;

		// 		point = new Vector3(
		// 			2.0f * Random.value - 1.0f,
		// 			2.0f * Random.value - 1.0f,
		// 			2.0f * Random.value - 1.0f);

		// 		//do {
		// 		//	point = new Vector3(
		// 		//		2.0f * Random.value - 1.0f,
		// 		//		2.0f * Random.value - 1.0f,
		// 		//		2.0f * Random.value - 1.0f);
		// 		//} while (point.magnitude > 1.0f);

		// 		points.Add(point);

		// 		//var indicator = Instantiate(MeshPrefab);

		// 		//indicator.transform.SetParent(transform, false);
		// 		//indicator.transform.localPosition = points[i];

		// 		//hulls.Add(indicator);
		// 	}

		// 	calc.CalculateClusters(points, clusterCount, 0.1f, ref clusters, ref assignedClusters);

		// 	var cloud = new List<Vector3>();

		// 	for (int i = 0; i < clusterCount; i++) {
		// 		cloud.Clear();

		// 		for (int j = 0; j < points.Count; j++) {
		// 			if (assignedClusters[j] == i) {
		// 				cloud.Add(points[j]);
		// 			}
		// 		}

		// 		GenerateHull(cloud);
		// 	}

		// 	yield return new WaitForSeconds(10.0f);

		// 	var breakCluster = 0;

		// 	Destroy(hulls[breakCluster]);

		// 	var newCloud = new List<Vector3>();

		// 	for (int i = 0; i < points.Count; i++) {
		// 		if (assignedClusters[i] == breakCluster) {
		// 			newCloud.Add(points[i]);
		// 		}
		// 	}

		// 	calc.CalculateClusters(newCloud, cluster, 0.05f, ref clusters, ref assignedClusters);
		// }

		// void GenerateHull(List<Vector3> cloud) {
		// 	var quickhull = new ConvexHullCalculator();
		// 	var verts = new List<Vector3>();
		// 	var tris = new List<int>();
		// 	var normals = new List<Vector3>();

		// 	quickhull.GenerateHull(cloud, true, ref verts, ref tris, ref normals);

		// 	var mesh = new Mesh();
		// 	mesh.SetVertices(verts);
		// 	mesh.SetTriangles(tris, 0);
		// 	mesh.SetNormals(normals);

		// 	var go = Instantiate(MeshPrefab);
		// 	go.transform.SetParent(transform, false);
		// 	go.transform.localPosition = Vector3.zero;
		// 	go.transform.localRotation = Quaternion.identity;
		// 	go.transform.localScale = Vector3.one;

		// 	go.GetComponent<MeshFilter>().sharedMesh = mesh;
		// 	go.GetComponent<MeshCollider>().sharedMesh = mesh;

		// 	hulls.Add(go);
		// }
	}
}
