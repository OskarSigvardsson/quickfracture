using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace GK {
	public class Fracture : MonoBehaviour {

		public bool InitialMesh = true;

		public GameObject Prefab;
		public int PointCount = 1000000;
		public int ClusterCount = 20;

		[System.NonSerialized]
		List<Vector3> points;


		static ObjectPool<ConvexHullCalculator> calculators =
			new ObjectPool<ConvexHullCalculator>(() => new ConvexHullCalculator());

		static ObjectPool<List<Vector3>>  vertsPool   = new ObjectPool<List<Vector3>>(() => new List<Vector3>());
		static ObjectPool<List<int>>      trisPool    = new ObjectPool<List<int>>(()     => new List<int>());
		static ObjectPool<List<Vector3>>  normalsPool = new ObjectPool<List<Vector3>>(() => new List<Vector3>());

		bool generatedMesh;
		Task meshGenerationTask;

		volatile float mass;
		volatile Mesh mesh = null;
		volatile List<Vector3> verts;
		volatile List<int> tris;
		volatile List<Vector3> normals;

		// IEnumerator Start() {
		// 	var rb = GetComponent<Rigidbody>();

		// 	if (rb != null) {
		// 		rb.isKinematic = true;

		// 		yield return null;

		// 		rb.isKinematic = false;
		// 	}
		// }

		void Start() {
			if (InitialMesh) {
				points = new List<Vector3>(PointCount);

				//var mf = GetComponent<MeshFilter>();

				// points.AddRange(mf.sharedMesh.vertices);

				while (points.Count < PointCount) {
					var point = new Vector3(
						Random.value - 0.5f,
						Random.value - 0.5f,
						Random.value - 0.5f);

					// if (point.sqrMagnitude <= 0.2025f) {
					// 	points.Add(point);
					// }

					points.Add(point);
				}

				// StartGeneratingMesh();
			}
		}

		void StartGeneratingMesh() {
			if (points.Count < 4) {
				Debug.LogError("I should handle this case somehow :)");
				// TODO error something
			} else {
				mesh = new Mesh();
				mesh.name = "Shard";

				var scale = transform.lossyScale.x;

				meshGenerationTask = Task.Run(() => {
					var calc = calculators.TakeOut();
					var verts = vertsPool.TakeOut();
					var tris = trisPool.TakeOut();
					var normals = normalsPool.TakeOut();

					verts.Clear();
					tris.Clear();
					normals.Clear();

					calc.GenerateHull(points, ref verts, ref tris, ref normals);
					calculators.PutBack(calc);

					this.verts = verts;
 					this.tris = tris;
 					this.normals = normals;

					var vol = 0.0f;

					// set mass
					for (int i = 0; i < tris.Count; i+=3) {
						var p0 = scale * verts[tris[i]];
						var p1 = scale * verts[tris[i+1]];
						var p2 = scale * verts[tris[i+2]];

						var area = 0.5f * Vector3.Cross(p1 - p0, p2 - p0).magnitude;
						var normal = normals[i];

						vol += (1.0f/3.0f) * Vector3.Dot(p0, normal) * area;
					}

					mass = vol;
				});
			}
		}

		void FixedUpdate() {
			if (!InitialMesh && !generatedMesh) {
				//Debug.Assert(meshGenerationTask != null);

				meshGenerationTask.Wait();

				mesh.SetVertices(verts);
				mesh.SetNormals(normals);
				mesh.SetTriangles(tris, 0);

				vertsPool.PutBack(verts);
				trisPool.PutBack(tris);
				normalsPool.PutBack(normals);

				mesh.UploadMeshData(false);

				verts = null;
				normals = null;
				tris = null;

				var mf = GetComponent<MeshFilter>();
				var mc = GetComponent<MeshCollider>();
				var rb = GetComponent<Rigidbody>();

				mf.mesh = mesh;
				rb.mass = mass;

				if (mc != null) {
					mc.sharedMesh = mesh;
				}

				generatedMesh = true;
				mesh = null;
				meshGenerationTask = null;
			}
		}

		// void LateUpdate() {
		// 	if (points == null || points.Count == 0) {
		// 		points = new List<Vector3>(PointCount);

		// 		for (int i = 0; i < PointCount; i++) {
		// 			points.Add(new Vector3(Random.value - 0.5f, Random.value - 0.5f, Random.value - 0.5f));
		// 		}
		// 	}

		// 	if (generatedMesh != true) {
		// 		GenerateMesh();

		// 		generatedMesh = true;
		// 	}
		// }

		// void GenerateMesh() {
		// 	if (points == null) {
		// 		points = new List<Vector3>(PointCount);

		// 		for (int i = 0; i < PointCount; i++) {
		// 			points.Add(new Vector3(Random.value - 0.5f, Random.value - 0.5f, Random.value - 0.5f));
		// 		}
		// 	}

		// 	if (points.Count >= 4) {
		// 		verts.Clear();
		// 		tris.Clear();
		// 		normals.Clear();

		// 		calc.GenerateHull(points, ref verts, ref tris, ref normals);
		// 		SetMass();

		// 		var mesh = new Mesh();
		// 		mesh.name = "Shard";

		// 		mesh.SetVertices(verts);
		// 		mesh.SetNormals(normals);
		// 		mesh.SetTriangles(tris, 0);

		// 		var mf = GetComponent<MeshFilter>();
		// 		var mc = GetComponent<MeshCollider>();

		// 		mf.mesh = mesh;
		// 		mc.sharedMesh = mesh;

		// 	}
		// }

		// void SetMass() {
		// 	var rb = GetComponent<Rigidbody>();

		// 	if (rb != null) {
		// 		var scale = transform.lossyScale.x;
		// 		//var scale = 1.0f;
		// 		var vol = 0.0f;

		// 		for (int i = 0; i < tris.Count; i+=3) {
		// 			var p0 = scale * verts[tris[i]];
		// 			var p1 = scale * verts[tris[i+1]];
		// 			var p2 = scale * verts[tris[i+2]];

		// 			var area = 0.5f * Vector3.Cross(p1 - p0, p2 - p0).magnitude;
		// 			var normal = normals[i];

		// 			vol += (1.0f/3.0f) * Vector3.Dot(p0, normal) * area;
		// 		}

		// 		rb.mass = vol;
		// 	}
		// }

		void CallAtFixedUpdate(System.Action callback) {
			StartCoroutine(FixedUpdateCoroutine(callback));
		}

		IEnumerator FixedUpdateCoroutine(System.Action callback) {
			yield return new WaitForFixedUpdate();
			callback();
		}

		public void DoFracture(Vector3 center) {
			CallAtFixedUpdate(() => { gameObject.SetActive(false); });

			if (points.Count >= ClusterCount) {
				var velocity = GetComponent<Rigidbody>().velocity;

				var maxRadius = 0.5f;

				var clusters = new List<Vector3>(ClusterCount);
				var assignedClusters = new int[points.Count];

				while (clusters.Count < ClusterCount) {
					var dir = Random.onUnitSphere;
					var radius = Random.value * maxRadius;

					radius *= radius;
					// radius *= radius;

					var point = center + radius * dir;

					// var point = new Vector3(
					// 	Random.value - 0.5f,
					// 	Random.value - 0.5f,
					// 	Random.value - 0.5f);

					// var inside = (point - center).sqrMagnitude
					// 	< maxRadius * maxRadius;

					var inside =
						   point.x >= -0.5f
						&& point.x <= 0.5f
						&& point.y >= -0.5f
						&& point.y <= 0.5f
						&& point.z >= -0.5f
						&& point.z <= 0.5f;


					if (inside) {
						clusters.Add(point);
					}
				}

				var clouds = new List<Vector3>[ClusterCount];

				Parallel.For(0, points.Count, i => {
					var closestCluster = -1;
					var closestClusterDistanceSqr = float.PositiveInfinity;

					for (int j = 0; j < ClusterCount; j++) {
						var distSqr = (points[i] - clusters[j]).sqrMagnitude;

						if (distSqr < closestClusterDistanceSqr) {
							closestCluster = j;
							closestClusterDistanceSqr = distSqr;
						}
					}

					assignedClusters[i] = closestCluster;
				});

				Parallel.For(0, ClusterCount, i => {
					var cloud = new List<Vector3>();

					for (int j = 0; j < points.Count; j++) {
						if (assignedClusters[j] == i) {
							cloud.Add(points[j]);
						}
					}

					if (cloud.Count >= 4) {
						clouds[i] = cloud;
					}
				});

				var children = new Fracture[ClusterCount];

				for (int i = 0; i < ClusterCount; i++) {
					var child = Instantiate(Prefab, null);

					child.transform.localPosition = transform.localPosition;
					child.transform.localRotation = transform.localRotation;
					child.transform.localScale = transform.localScale;

					var frac = child.GetComponent<Fracture>();

					frac.Prefab = Prefab;
					frac.ClusterCount = ClusterCount;
					frac.InitialMesh = false;

					children[i] = frac;
				}

				for (int i = 0; i < ClusterCount; i++) {
					if (clouds[i] == null) {
						children[i].gameObject.SetActive(false);
						Destroy(children[i].gameObject);
					} else {
						children[i].points = clouds[i];
						children[i].StartGeneratingMesh();
						children[i].GetComponent<Rigidbody>().velocity = velocity;
					}
				}

			}
		}
	}
}
