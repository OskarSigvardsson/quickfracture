using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GK {
	public class InsideMeshTest : MonoBehaviour {

		public int SensorCount = 100;
		public MeshFilter MeshToTest;
		public MeshRenderer SensorPrefab;

		List<Vector3> verts = new List<Vector3>();
		List<int> tris = new List<int>();

		void Start() {
			var bounds = MeshToTest.sharedMesh.bounds;

			for (int i = 0; i < SensorCount; i++) {
				var sensor = Instantiate(SensorPrefab);

				var localPos = new Vector3(
					Random.Range(bounds.min.x, bounds.max.x),
					Random.Range(bounds.min.y, bounds.max.y),
					Random.Range(bounds.min.z, bounds.max.z));

				var worldPos = MeshToTest.transform.TransformPoint(localPos);

				sensor.transform.position = worldPos;

				ColorSensor(sensor);
			}
		}

		void ColorSensor(MeshRenderer sensor) {
			var mesh = MeshToTest.sharedMesh;

			mesh.GetVertices(verts);
			mesh.GetTriangles(tris, 0);

			var testPoint = MeshToTest.transform.InverseTransformPoint(sensor.transform.position);

			// if point is behind every face of the mesh, then it is inside

			// "behind" means that the point is underneath the plane of the triangle.

			// you check if something is inside, by taking the dot
			// product of the normal of the plane and the vector from
			// a point on the plane to the point to test

			var allInside = true;

			for (int i = 0; i < tris.Count; i+=3) {
				var p0 = verts[tris[i]];
				var p1 = verts[tris[i+1]];
				var p2 = verts[tris[i+2]];

				var normal = Vector3.Cross(p1 - p0, p2 - p0);
				var testVector = testPoint - p0;

				var projection = Vector3.Dot(normal, testVector);

				if (projection > 0.0f) {
					allInside = false;
					break;
				}
			}

			var rend = GetComponent<MeshRenderer>();

			if (allInside) {
				sensor.material.color = Color.green;
			} else {
				sensor.material.color = Color.red;
			}
		}
	}
}
