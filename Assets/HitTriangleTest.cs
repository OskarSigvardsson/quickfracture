using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gro {
	public class HitTriangleTest : MonoBehaviour {

		public int SensorCount = 500;
		public MeshRenderer SensorPrefab;
		public MeshFilter MeshToTest;

		void Start() {
			var mesh = MeshToTest.sharedMesh;
			var bounds = mesh.bounds;

			for (int i = 0; i < SensorCount; i++) {
				var mr = Instantiate(SensorPrefab, MeshToTest.transform);

				var pos = new Vector3(
					Random.Range(bounds.min.x, bounds.max.x),
					Random.Range(bounds.min.y, bounds.max.y),
					Random.Range(bounds.min.z, bounds.max.z));

				mr.transform.localPosition = pos;

				mr.material.color =
					InsideMesh(mesh, pos)
					? Color.green
					: Color.red;
			}

		}

		void Update() {
			var mesh = MeshToTest.sharedMesh;

			GetComponent<MeshRenderer>().material.color =
				InsideMesh(mesh, MeshToTest.transform.InverseTransformPoint(transform.position))
				? Color.green
				: Color.red;
		}

		bool InsideMesh(Mesh mesh, Vector3 point) {
			var ray = new Ray(point, Vector3.up);

			var verts = new List<Vector3>();
			var tris  = new List<int>();

			mesh.GetVertices(verts);
			mesh.GetTriangles(tris, 0);

			var outside = true;

			for (int i = 0; i < tris.Count; i+=3) {
				var p0 = verts[tris[i]];
				var p1 = verts[tris[i+1]];
				var p2 = verts[tris[i+2]];

				if (IntersectsTriangle(ray, p0, p1, p2)) {
					outside = !outside;
				}
			}

			return !outside;
		}


		bool IntersectsTriangle(Ray ray, Vector3 v0, Vector3 v1, Vector3 v2) {
			var e1 = v1 - v0;
			var e2 = v2 - v0;
			var t = ray.origin - v0;
			var p = Vector3.Cross(ray.direction, e2);
			var q = Vector3.Cross(t, e1);

			var m = 1.0f / Vector3.Dot(p, e1);

			var tuv = m * new Vector3(
				Vector3.Dot(q,e2),
				Vector3.Dot(p,t),
				Vector3.Dot(q,ray.direction));

			return tuv.x > 0.0f
				&& tuv.y > 0.0f
				&& tuv.z > 0.0f
				&& tuv.y + tuv.z < 1.0f;
		}
	}
}
