using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace GK {
	public class ComputeTest : MonoBehaviour {

		public ComputeShader TestShader;
		public int PointCount = 1000000;
		public int TrisCount = 200;

		struct Triangle {
			public Vector3 v0;
			public Vector3 v1;
			public Vector3 v2;
		}

		Vector3[] points;
		Triangle[] tris;
		int[] assignedCpu;
		int[] assignedGpu;
		int kernel;

		IEnumerator Start() {
			kernel = TestShader.FindKernel("Assign");

			yield return new WaitForSeconds(1.0f);

			points = new Vector3[PointCount];
			tris = new Triangle[TrisCount];
			assignedCpu = new int[PointCount];
			assignedGpu = new int[PointCount];

			while (true) {

				for (int i = 0; i < PointCount; i++) {
					points[i] = new Vector3(Random.value, Random.value, Random.value);
					assignedCpu[i] = -1;
					assignedGpu[i] = -1;
				}

				for (int i = 0; i < TrisCount; i++) {
					tris[i] = new Triangle {
						v0 = new Vector3(Random.value, Random.value, Random.value),
						v1 = new Vector3(Random.value, Random.value, Random.value),
						v2 = new Vector3(Random.value, Random.value, Random.value)
					};
				}

				yield return new WaitForSeconds(1.0f);

				Profiler.BeginSample("CPU assign");
				Assign();
				Profiler.EndSample();

				yield return new WaitForSeconds(1.0f);

				Profiler.BeginSample("GPU assign");
				AssignWithShader();
				Profiler.EndSample();

				yield return new WaitForSeconds(1.0f);

				Validate();

				yield return new WaitForSeconds(5.0f);
			}
		}

		void Validate() {
			for (int i = 0; i < assignedCpu.Length; i++) {
				if (assignedCpu[i] != assignedGpu[i]) {
					Debug.LogError("Missed assignment on " + i);
					Debug.LogError(assignedCpu[i]);
					Debug.LogError(assignedGpu[i]);
					return;
				}
			}

			Debug.Log("Validation passed");
		}

		void AssignWithShader() {

			var b0 = new ComputeBuffer(PointCount, 4 * 3);
			var b1 = new ComputeBuffer(TrisCount, 4 * 3 * 3);
			var b2 = new ComputeBuffer(PointCount, 4);

			b0.SetData(points);
			b1.SetData(tris);
			b2.SetData(assignedGpu);

			TestShader.SetInt("PointCount", PointCount);
			TestShader.SetInt("TrisCount", TrisCount);
			TestShader.SetBuffer(kernel, "Points", b0);
			TestShader.SetBuffer(kernel, "Tris", b1);
			TestShader.SetBuffer(kernel, "Assigned", b2);

			var stopwatch = new System.Diagnostics.Stopwatch();
			stopwatch.Start();

			TestShader.Dispatch(kernel, 1 + PointCount / 1024, 1, 1);

			Task.Run(() => {
					b2.GetData(assignedGpu, 0, 0, PointCount);
				}).Wait();

			stopwatch.Stop();

			Debug.Log(stopwatch.Elapsed.TotalMilliseconds);

			b0.Dispose();
			b1.Dispose();
			b2.Dispose();

			Debug.Log(assignedGpu[10]);
		}

		void Assign() {
			for (int i = 0; i < PointCount; i++) {
				var point = points[i];

				for (int j = 0; j < TrisCount; j++) {
					var t = tris[j];
					var above = Vector3.Dot(
						Vector3.Cross(t.v1 - t.v0, t.v2 - t.v0),
						point - t.v0)
						> 0.0f;

					if (above) {
						assignedCpu[i] = j;
					}
				}
			}
		}
	}
}
