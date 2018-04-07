using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace GK {
	public class Benchmarks : MonoBehaviour {

		public int PointCount = 1000000;

		ConvexHullCalculator calc1 = new ConvexHullCalculator();
		ConvexHullShaderCalculator calc2 = new ConvexHullShaderCalculator();
		ConvexHullParallelCalculator calc3 = new ConvexHullParallelCalculator();

		List<Vector3> points = new List<Vector3>();
		List<Vector3> verts = new List<Vector3>();
		List<int> tris = new List<int>();
		List<Vector3> normals = new List<Vector3>();

		void Update() {
			points.Clear();

			while (points.Count < PointCount) {
				var point = new Vector3(Random.value, Random.value, Random.value);

				// if ((point - 0.5f * Vector3.one).magnitude <= 1.0f) {
				// 	points.Add(point);
				// }

				points.Add(point);
			}

			Profiler.BeginSample("Generate hull type " + ((int)Time.time % 4));

			calc2.GenerateHull(points, ref verts, ref tris, ref normals);
			// if ((int)Time.time % 4 == 1) {
			// 	calc1.GenerateHull(points, ref verts, ref tris, ref normals);
			// } else if ((int)Time.time % 4 == 2) {
			// 	calc2.GenerateHull(points, ref verts, ref tris, ref normals);
			// } else if ((int)Time.time % 4 == 3) {
			// 	calc3.GenerateHull(points, ref verts, ref tris, ref normals);
			// }

			Profiler.EndSample();
		}

	}
}
