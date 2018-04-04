#define DEBUG_QUICKHULL

using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Profiling;

namespace GK {
	public class ConvexHullShaderCalculator {

		const float EPSILON = 0.0001f;

		struct Face {
			public int Vertex0;
			public int Vertex1;
			public int Vertex2;

			public int Opposite0;
			public int Opposite1;
			public int Opposite2;

			public Vector3 Normal;

			public List<PointFace> OpenSet;

			public Face(int v0,
				int v1,
				int v2,
				int o0,
				int o1,
				int o2,
				Vector3 normal,
				List<PointFace> openSet)
			{
				Vertex0 = v0;
				Vertex1 = v1;
				Vertex2 = v2;
				Opposite0 = o0;
				Opposite1 = o1;
				Opposite2 = o2;
				Normal = normal;
				OpenSet = openSet;
			}

			public bool Equals(Face other) {
				return (this.Vertex0   == other.Vertex0)
					&& (this.Vertex1   == other.Vertex1)
					&& (this.Vertex2   == other.Vertex2)
					&& (this.Opposite0 == other.Opposite0)
					&& (this.Opposite1 == other.Opposite1)
					&& (this.Opposite2 == other.Opposite2)
					&& (this.Normal    == other.Normal);
			}
		}

		struct PointFace {
			public int Point;
			public float Distance;

			public PointFace(int p, float d) {
				Point = p;
				Distance = d;
			}
		}

		struct HorizonEdge {
			public int Face;
			public int Edge0;
			public int Edge1;
		}

		struct AddedFace {
			public int FaceIndex;
			public Vector3 PointOnFace;
			public Vector3 Normal;
		}

		Dictionary<int, Face> faces;
		HashSet<int> litFaces;

		Stack<List<PointFace>> openSetPool;
		List<HorizonEdge> horizon;
		List<AddedFace> addedFaces;
		List<int> pointsToReassign;

		int faceCount = 0;
		int assignedPoints = 0;

#if DEBUG_QUICKHULL
		int lentOutSets = 0;
#endif

		public void GenerateHull(
			List<Vector3> points,
			ref List<Vector3> verts,
			ref List<int> tris,
			ref List<Vector3> normals)
		{

			Profiler.BeginSample("Initialize");
			Initialize(points);
			Profiler.EndSample();

			Profiler.BeginSample("Generate initial hull");
			GenerateInitialHull(points);
			Profiler.EndSample();

			while (assignedPoints > 0) {
				GrowHull(points);
			}

			Profiler.BeginSample("Export mesh");
			ExportMesh(points, ref verts, ref tris, ref normals);
			Profiler.EndSample();

			foreach (var kvp in faces) {
				ReturnOpenSet(kvp.Value.OpenSet);
			}

#if DEBUG_QUICKHULL
			var totalCapacity = 0;

			foreach (var openSet in openSetPool) {
				totalCapacity += openSet.Capacity;
			}

			UnityEngine.Debug.Log(totalCapacity);

			Assert(lentOutSets == 0);
#endif

			VerifyMesh(points, ref verts, ref tris);
		}

		void Initialize(List<Vector3> points) {
			faceCount = 0;

			if (faces == null) {
				faces = new Dictionary<int, Face>();
				litFaces = new HashSet<int>();
				horizon = new List<HorizonEdge>();
				addedFaces = new List<AddedFace>();
				openSetPool = new Stack<List<PointFace>>();
				pointsToReassign = new List<int>();
			} else {
				faces.Clear();
				litFaces.Clear();
				horizon.Clear();
			}
		}

		List<PointFace> GetOpenSet() {
#if DEBUG_QUICKHULL
			lentOutSets++;
#endif
			if (openSetPool.Count == 0) {
				return new List<PointFace>();
			} else {
				var openSet = openSetPool.Pop();
				openSet.Clear();
				return openSet;
			}
		}

		void ReturnOpenSet(List<PointFace> openSet) {
#if DEBUG_QUICKHULL
			lentOutSets--;
#endif

			openSetPool.Push(openSet);
		}


		void GenerateInitialHull(List<Vector3> points) {
			// TODO use extreme points to generate seed hull. I wonder
			// how much difference that actually makes, you would
			// imagine that even with a tiny seed hull, it would grow
			// pretty quickly. Anyway, the rest should be the same,
			// you only need to change how you find vi0/vi1/vi2/vi3

			// TODO i'm a bit worried what happens if these points are
			// too close to each other or if the fourth point is
			// coplanar with the triangle. I should loop through the
			// point set to find suitable points instead.
			var vi0 = 0;
			var vi1 = 1;
			var vi2 = 2;
			var vi3 = 3;

			var v0 = points[vi0];
			var v1 = points[vi1];
			var v2 = points[vi2];
			var v3 = points[vi3];

			var above = Dot(v3 - v1, Cross(v1 - v0, v2 - v0)) > 0.0f;

			// Create the faces of the seed hull. You need to draw a
			// diagram here, otherwise it's impossible to know what's
			// going on :)

			// Basically: there are two different possible
			// start-tetrahedrons, depending on whether the fourth
			// point is above or below the base triangle. If you draw
			// a tetrahedron with these coordinates (in a right-handed
			// coordinate-system):

			//   vi0 = (0,0,0)
			//   vi1 = (1,0,0)
			//   vi2 = (0,1,0)
			//   vi3 = (0,0,1)

			// you can see the first case (set vi3 = (0,0,-1) for the
			// second case). The faces are added with the proper
			// references to the faces opposite each vertex

			faceCount = 0;
			if (above) {
				faces[faceCount++] = new Face(vi0, vi2, vi1, 3, 1, 2, Normal(points[vi0], points[vi2], points[vi1]), GetOpenSet());
				faces[faceCount++] = new Face(vi0, vi1, vi3, 3, 2, 0, Normal(points[vi0], points[vi1], points[vi3]), GetOpenSet());
				faces[faceCount++] = new Face(vi0, vi3, vi2, 3, 0, 1, Normal(points[vi0], points[vi3], points[vi2]), GetOpenSet());
				faces[faceCount++] = new Face(vi1, vi2, vi3, 2, 1, 0, Normal(points[vi1], points[vi2], points[vi3]), GetOpenSet());
			} else {
				faces[faceCount++] = new Face(vi0, vi1, vi2, 3, 2, 1, Normal(points[vi0], points[vi1], points[vi2]), GetOpenSet());
				faces[faceCount++] = new Face(vi0, vi3, vi1, 3, 0, 2, Normal(points[vi0], points[vi3], points[vi1]), GetOpenSet());
				faces[faceCount++] = new Face(vi0, vi2, vi3, 3, 1, 0, Normal(points[vi0], points[vi2], points[vi3]), GetOpenSet());
				faces[faceCount++] = new Face(vi1, vi3, vi2, 2, 0, 1, Normal(points[vi1], points[vi3], points[vi2]), GetOpenSet());
			}

			assignedPoints = 0;

			VerifyFaces(points);

			var pointCount = points.Count;

			// TODO shader?
			for (int i = 0; i < pointCount; i++) {
				if (i == vi0 || i == vi1 || i == vi2 || i == vi3) continue;

				var point = points[i];

				Assert(faces.Count == 4);
				Assert(faces.Count == faceCount);
				for (int j = 0; j < 4; j++) {
					Assert(faces.ContainsKey(j));

					var face = faces[j];

					var dist = PointFaceDistance(
						point,
						points[face.Vertex0],
						face.Normal);

					if (dist > EPSILON) {
						assignedPoints++;
						face.OpenSet.Add(new PointFace(i, dist));
						break;
					}
				}
			}

			VerifyOpenSet(points);
		}

		void GrowHull(List<Vector3> points) {
			// Find farthest point and first lit face.
			var farthestDist = -1.0f;
			var farthestPointIndex = -1;
			var farthestFaceIndex = -1;

			foreach (var kvp in faces) {
				var faceIndex = kvp.Key;
				var openSet = kvp.Value.OpenSet;
				var openSetCount = openSet.Count;

				for (int i = 0; i < openSetCount; i++) {
					var pointFace = openSet[i];

					if (pointFace.Distance > farthestDist) {
						farthestDist = pointFace.Distance;
						farthestPointIndex = pointFace.Point;
						farthestFaceIndex = faceIndex;
					}
				}
			}


			Profiler.BeginSample("Find horizon");
			// Use lit face to find horizon and the rest of the lit
			// faces.
			FindHorizon(points, farthestPointIndex, farthestFaceIndex);
			Profiler.EndSample();

			VerifyHorizon();

			Profiler.BeginSample("Construct cone");
			// Construct new cone from horizon
			ConstructCone(points, farthestPointIndex);
			Profiler.EndSample();

			VerifyFaces(points);

			Profiler.BeginSample("Reassign point");
			// Reassign points
			ReassignPoints(points);
			Profiler.EndSample();
		}

		void FindHorizon(List<Vector3> points, int farthestPointIndex, int farthestFaceIndex) {
			Profiler.BeginSample("Find farthest point");
			var face = faces[farthestFaceIndex];
			var point = points[farthestPointIndex];
			Profiler.EndSample();

			litFaces.Clear();
			horizon.Clear();

			litFaces.Add(farthestFaceIndex);

			Assert(PointFaceDistance(point, points[face.Vertex0], face.Normal) > 0.0f);

			// For the rest of the recursive search calls, we first
			// check if the triangle has already been visited and is
			// part of litFaces. However, in this first call we can
			// skip that because we know it can't possibly have been
			// visited yet, since the only thing in litFaces is the
			// current triangle.
			{
				var oppositeFace = faces[face.Opposite0];

				var dist = PointFaceDistance(
					point,
					points[oppositeFace.Vertex0],
					oppositeFace.Normal);

				if (dist <= 0.0f) {
					horizon.Add(new HorizonEdge {
							Face = face.Opposite0,
							Edge0 = face.Vertex1,
							Edge1 = face.Vertex2,
						});
				} else {
					SearchHorizon(points, point, farthestFaceIndex, face.Opposite0, oppositeFace);
				}
			}

			if (!litFaces.Contains(face.Opposite1)) {
				var oppositeFace = faces[face.Opposite1];

				var dist = PointFaceDistance(
					point,
					points[oppositeFace.Vertex0],
					oppositeFace.Normal);

				if (dist <= 0.0f) {
					horizon.Add(new HorizonEdge {
							Face = face.Opposite1,
							Edge0 = face.Vertex2,
							Edge1 = face.Vertex0,
						});
				} else {
					SearchHorizon(points, point, farthestFaceIndex, face.Opposite1, oppositeFace);
				}
			}

			if (!litFaces.Contains(face.Opposite2)) {
				var oppositeFace = faces[face.Opposite2];

				var dist = PointFaceDistance(
					point,
					points[oppositeFace.Vertex0],
					oppositeFace.Normal);

				if (dist <= 0.0f) {
					horizon.Add(new HorizonEdge {
							Face = face.Opposite2,
							Edge0 = face.Vertex0,
							Edge1 = face.Vertex1,
						});
				} else {
					SearchHorizon(points, point, farthestFaceIndex, face.Opposite2, oppositeFace);
				}
			}
		}

		void SearchHorizon(List<Vector3> points, Vector3 point, int prevFaceIndex, int faceCount, Face face) {
			Assert(prevFaceIndex >= 0);
			Assert(litFaces.Contains(prevFaceIndex));
			Assert(!litFaces.Contains(faceCount));
			Assert(faces[faceCount].Equals(face));

			litFaces.Add(faceCount);

			// Use prevFaceIndex to determine what the next face to
			// search will be, and what edges we need to cross to get
			// there. It's important that the search proceeds in
			// counter-clockwise order from the previous face.
			int nextFaceIndex0;
			int nextFaceIndex1;
			int edge0;
			int edge1;
			int edge2;

			if (prevFaceIndex == face.Opposite0) {
				nextFaceIndex0 = face.Opposite1;
				nextFaceIndex1 = face.Opposite2;

				edge0 = face.Vertex2;
				edge1 = face.Vertex0;
				edge2 = face.Vertex1;
			} else if (prevFaceIndex == face.Opposite1) {
				nextFaceIndex0 = face.Opposite2;
				nextFaceIndex1 = face.Opposite0;

				edge0 = face.Vertex0;
				edge1 = face.Vertex1;
				edge2 = face.Vertex2;
			} else {
				Assert(prevFaceIndex == face.Opposite2);

				nextFaceIndex0 = face.Opposite0;
				nextFaceIndex1 = face.Opposite1;

				edge0 = face.Vertex1;
				edge1 = face.Vertex2;
				edge2 = face.Vertex0;
			}

			if (!litFaces.Contains(nextFaceIndex0)) {
				var oppositeFace = faces[nextFaceIndex0];

				var dist = PointFaceDistance(
					point,
					points[oppositeFace.Vertex0],
					oppositeFace.Normal);

				if (dist <= 0.0f) {
					horizon.Add(new HorizonEdge {
							Face = nextFaceIndex0,
							Edge0 = edge0,
							Edge1 = edge1,
						});
				} else {
					SearchHorizon(points, point, faceCount, nextFaceIndex0, oppositeFace);
				}
			}

			if (!litFaces.Contains(nextFaceIndex1)) {
				var oppositeFace = faces[nextFaceIndex1];

				var dist = PointFaceDistance(
					point,
					points[oppositeFace.Vertex0],
					oppositeFace.Normal);

				if (dist <= 0.0f) {
					horizon.Add(new HorizonEdge {
							Face = nextFaceIndex1,
							Edge0 = edge1,
							Edge1 = edge2,
						});
				} else {
					SearchHorizon(points, point, faceCount, nextFaceIndex1, oppositeFace);
				}
			}
		}

		void ConstructCone(List<Vector3> points, int farthestPointIndex) {
			pointsToReassign.Clear();

			foreach (var fi in litFaces) {
				Assert(faces.ContainsKey(fi));

				// TODO move this to FindHorizon/SearchHorizon, when
				// we're adding stuff to litFaces

				var face = faces[fi];
				var openSet = face.OpenSet;

				for (int i = 0; i < openSet.Count; i++) {
					pointsToReassign.Add(openSet[i].Point);
				}

				assignedPoints -= openSet.Count;

				ReturnOpenSet(openSet);

				faces.Remove(fi);
			}

			var firstAddedFace = faceCount;

			addedFaces.Clear();

			for (int i = 0; i < horizon.Count; i++) {
				// Vertices of the new face, the farthest point as
				// well as the edge on the horizon. Horizon edge is
				// CCW, so the triangle should be as well.
				var v0 = farthestPointIndex;
				var v1 = horizon[i].Edge0;
				var v2 = horizon[i].Edge1;

				// Opposite faces of the triangle. First, the edge on
				// the other side of the horizon, then the next/prev
				// faces on the new cone
				var o0 = horizon[i].Face;
				var o1 = (i == horizon.Count - 1) ? firstAddedFace : firstAddedFace + i + 1;
				var o2 = (i == 0) ? (firstAddedFace + horizon.Count - 1) : firstAddedFace + i - 1;

				var fi = faceCount++;

				var newFace = new Face(
					v0, v1, v2,
					o0, o1, o2,
					Normal(points[v0], points[v1], points[v2]),
					GetOpenSet());

				addedFaces.Add(new AddedFace {
						FaceIndex = fi,
						PointOnFace = points[v0],
						Normal = newFace.Normal,
					});

				faces[fi] = newFace;

				var horizonFace = faces[horizon[i].Face];

				if (horizonFace.Vertex0 == v1) {
					Assert(v2 == horizonFace.Vertex2);
					horizonFace.Opposite1 = fi;
				} else if (horizonFace.Vertex1 == v1) {
					Assert(v2 == horizonFace.Vertex0);
					horizonFace.Opposite2 = fi;
				} else {
					Assert(v1 == horizonFace.Vertex2);
					Assert(v2 == horizonFace.Vertex1);
					horizonFace.Opposite0 = fi;
				}

				faces[horizon[i].Face] = horizonFace;
			}
		}

		void ReassignPoints(List<Vector3> points) {
			for (int i = 0; i < pointsToReassign.Count; i++) {
				var pointIndex = pointsToReassign[i];
				var point = points[pointIndex];

				for (int j = 0; j < addedFaces.Count; j++) {
					var addedFace = addedFaces[j];

					var dist = PointFaceDistance(
						point,
						addedFace.PointOnFace,
						addedFace.Normal);

					if (dist > EPSILON) {
						assignedPoints += 1;

						faces[addedFace.FaceIndex].OpenSet.Add(new PointFace {
								Point = pointIndex,
								Distance = dist,
							});

						break;
					}
				}
			}
		}

		// void ReassignPoints(List<Vector3> points) {
		// 	for (int i = 0; i <= openSetTail; i++) {
		// 		var fp = openSet[i];

		// 		if (litFaces.Contains(fp.Face)) {
		// 			var assigned = false;
		// 			var point = points[fp.Point];

		// 			for (int j = 0; j < addedFaces.Count; j++) {
		// 				var addedFace = addedFaces[j];

		// 				var dist = PointFaceDistance(
		// 					point,
		// 					addedFace.PointOnFace,
		// 					addedFace.Normal);

		// 				if (dist > EPSILON) {
		// 					assigned = true;

		// 					fp.Face = addedFace.FaceIndex;
		// 					fp.Distance = dist;

		// 					openSet[i] = fp;
		// 					break;
		// 				}
		// 			}

		// 			if (!assigned) {
		// 				// If point hasn't been assigned, then it's
		// 				// inside the convex hull. Swap it with
		// 				// openSetTail, and decrement openSetTail. We
		// 				// also have to decrement i, because there's
		// 				// now a new thing in openSet[i], so we need i
		// 				// to remain the same the next iteration of
		// 				// the loop.
		// 				fp.Face = INSIDE;
		// 				fp.Distance = float.NaN;

		// 				openSet[i] = openSet[openSetTail];
		// 				openSet[openSetTail] = fp;

		// 				i--;
		// 				openSetTail--;
		// 			}
		// 		}
		// 	}
		// }

		void ExportMesh(
			List<Vector3> points,
			ref List<Vector3> verts,
			ref List<int> tris,
			ref List<Vector3> normals)
		{
			if (verts == null) {
				verts = new List<Vector3>();
			} else {
				verts.Clear();
			}

			if (tris == null) {
				tris = new List<int>();
			} else {
				tris.Clear();
			}

			if (normals == null) {
				normals = new List<Vector3>();
			} else {
				normals.Clear();
			}

			foreach (var face in faces.Values) {
				int vi0, vi1, vi2;

				vi0 = verts.Count; verts.Add(points[face.Vertex0]);
				vi1 = verts.Count; verts.Add(points[face.Vertex1]);
				vi2 = verts.Count; verts.Add(points[face.Vertex2]);

				normals.Add(face.Normal);
				normals.Add(face.Normal);
				normals.Add(face.Normal);

				tris.Add(vi0);
				tris.Add(vi1);
				tris.Add(vi2);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		float PointFaceDistance(Vector3 point, Vector3 pointOnFace, Vector3 normal) {
			return Dot(normal, point - pointOnFace);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		Vector3 Normal(Vector3 v0, Vector3 v1, Vector3 v2) {
			return Cross(v1 - v0, v2 - v0).normalized;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static float Dot(Vector3 a, Vector3 b) {
			return a.x*b.x + a.y*b.y + a.z*b.z;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static Vector3 Cross(Vector3 a, Vector3 b) {
			return new Vector3(
				a.y*b.z - a.z*b.y,
				a.z*b.x - a.x*b.z,
				a.x*b.y - a.y*b.x);
		}


		// [Conditional("DEBUG_QUICKHULL")]
		// void VerifyOpenSet(List<Vector3> points) {
		// 	for (int i = 0; i < openSet.Count; i++) {
		// 		if (i > openSetTail) {
		// 			Assert(openSet[i].Face == INSIDE);
		// 		} else {
		// 			Assert(openSet[i].Face != INSIDE);
		// 			Assert(openSet[i].Face != UNASSIGNED);

		// 			Assert(PointFaceDistance(
		// 					points[openSet[i].Point],
		// 					points[faces[openSet[i].Face].Vertex0],
		// 					faces[openSet[i].Face].Normal) > 0.0f);
		// 		}
		// 	}
		// }

		[Conditional("DEBUG_QUICKHULL")]
		void VerifyOpenSet(List<Vector3> points) {
			var count = 0;

			foreach (var kvp in faces) {
				var face = kvp.Value;

				count += face.OpenSet.Count;

				for (int i = 0; i < face.OpenSet.Count; i++) {
					Assert(face.OpenSet[i].Distance > EPSILON);
				}
			}

			Assert(assignedPoints == count);
		}

		[Conditional("DEBUG_QUICKHULL")]
		void VerifyHorizon() {
			for (int i = 0; i < horizon.Count; i++) {
				var prev = i == 0 ? horizon.Count - 1 : i - 1;

				Assert(horizon[prev].Edge1 == horizon[i].Edge0);
				Assert(HasEdge(faces[horizon[i].Face], horizon[i].Edge1, horizon[i].Edge0));
			}
		}

		[Conditional("DEBUG_QUICKHULL")]
		void VerifyFaces(List<Vector3> points) {
			foreach (var kvp in faces) {
				var fi = kvp.Key;
				var face = kvp.Value;

				Assert(faces.ContainsKey(face.Opposite0));
				Assert(faces.ContainsKey(face.Opposite1));
				Assert(faces.ContainsKey(face.Opposite2));

				Assert(face.Opposite0 != fi);
				Assert(face.Opposite1 != fi);
				Assert(face.Opposite2 != fi);

				Assert(face.Vertex0 != face.Vertex1);
				Assert(face.Vertex0 != face.Vertex2);
				Assert(face.Vertex1 != face.Vertex2);

				Assert(HasEdge(faces[face.Opposite0], face.Vertex2, face.Vertex1));
				Assert(HasEdge(faces[face.Opposite1], face.Vertex0, face.Vertex2));
				Assert(HasEdge(faces[face.Opposite2], face.Vertex1, face.Vertex0));

				Assert((face.Normal - Normal(
							points[face.Vertex0],
							points[face.Vertex1],
							points[face.Vertex2])).magnitude < EPSILON);
			}
		}

		[Conditional("DEBUG_QUICKHULL")]
		void VerifyMesh(List<Vector3> points, ref List<Vector3> verts, ref List<int> tris) {
			Assert(tris.Count % 3 == 0);

			for (int i = 0; i < points.Count; i++) {
				for (int j = 0; j < tris.Count; j+=3) {
					var t0 = verts[tris[j]];
					var t1 = verts[tris[j + 1]];
					var t2 = verts[tris[j + 2]];

					Assert(Dot(points[i] - t0, Vector3.Cross(t1 - t0, t2 - t0)) <= EPSILON);
				}

			}
		}

		bool HasEdge(Face f, int e0, int e1) {
			return (f.Vertex0 == e0 && f.Vertex1 == e1)
				|| (f.Vertex1 == e0 && f.Vertex2 == e1)
				|| (f.Vertex2 == e0 && f.Vertex0 == e1);
		}

		[Conditional("DEBUG_QUICKHULL")]
		static void Assert(bool condition) {
			if (!condition) {
				throw new UnityEngine.Assertions.AssertionException("Assertion failed", "");
			}
		}
	}
}
