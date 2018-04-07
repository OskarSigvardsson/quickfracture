using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GK {
	public class InsideMeshCalculator {

		Vector3[] verts;

		public InsideMeshCalculator(Mesh mesh) {
			var meshVerts = mesh.vertices;
			var meshTris = mesh.triangles;

			verts = new Vector3[meshTris.Length];

			for (int i = 0; i < meshTris.Length; i++) {
				this.verts[i] = meshVerts[meshTris[i]];
			}
		}

		public bool IsInside(Vector3 point) {
			var ray = new Ray(point, Vector3.up);

			var outside = true;
			var i = 0;

			while (i  < verts.Length) {
				var p0 = verts[i++];
				var p1 = verts[i++];
				var p2 = verts[i++];

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
