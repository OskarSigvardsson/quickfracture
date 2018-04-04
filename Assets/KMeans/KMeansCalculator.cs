using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace GK {
	public class KMeansCalculator {

		List<int> clusterCounts;
		List<Vector3> newClusters;

		public void CalculateClusters(
			List<Vector3> points,
			int clusterCount,
			float threshold,
			ref List<Vector3> clusters,
			ref List<int> assignedClusters)
		{
			Initialize(points, clusterCount, ref clusters, ref assignedClusters);

			float reassigned;
			var i = 0;
			do {
				reassigned = AssignClusters(points, clusterCount, ref clusters, ref assignedClusters);
			} while (i++ < 100 && reassigned > threshold);
		}

		void Initialize(
			List<Vector3> points,
			int clusterCount,
			ref List<Vector3> clusters,
			ref List<int> assignedClusters)
		{
			if (points.Count < clusterCount) {
				throw new ArgumentException("There are less clusters than there are points");
			}

			if (clusterCounts == null) {
				clusterCounts = new List<int>(clusterCount);
				newClusters = new List<Vector3>(clusterCount);
			} else {
				clusterCounts.Clear();
				newClusters.Clear();

				if (clusterCounts.Capacity < clusterCount) {
					clusterCounts.Capacity = clusterCount;
				}
				if (assignedClusters.Capacity < points.Count) {
					assignedClusters.Capacity = points.Count;
				}
				if (newClusters.Capacity < clusterCount) {
					newClusters.Capacity = clusterCount;
				}
			}

			if (assignedClusters == null) {
				assignedClusters = new List<int>(points.Count);
			} else {
				assignedClusters.Clear();

				if (assignedClusters.Capacity < points.Count) {
					assignedClusters.Capacity = points.Count;
				}
			}

			if (clusters == null) {
				clusters = new List<Vector3>(clusterCount);
			} else {
				clusters.Clear();

				if (clusters.Capacity < clusterCount) {
					clusters.Capacity = clusterCount;
				}
			}

			for (int i = 0; i < points.Count; i++) {
				assignedClusters.Add(-1);
			}

			for (int i = 0; i < clusterCount; i++) {
				clusterCounts.Add(0);
				clusters.Add(points[i]);
				newClusters.Add(Vector3.zero);
			}
		}

		float AssignClusters(
			List<Vector3> points,
			int clusterCount,
			ref List<Vector3> clusters,
			ref List<int> assignedClusters)
		{
			var pointCount = points.Count;
			var reassigned = 0;

			for (int i = 0; i < clusterCount; i++) {
				clusterCounts[i] = 0;
			}

			for (int i = 0; i < pointCount; i++) {
				var assignedTo = -1;
				var minDistSqr = float.PositiveInfinity;
				var point = points[i];

				for (int j = 0; j < clusterCount; j++) {
					var cluster = clusters[j];
					var distSqr = DistSqr(point, cluster);

					if (distSqr < minDistSqr) {
						minDistSqr = distSqr;
						assignedTo = j;
					}
				}

				if (assignedClusters[i] != assignedTo) {
					reassigned += 1;
				}

				assignedClusters[i] = assignedTo;

				var count = clusterCounts[assignedTo] + 1;

				newClusters[assignedTo] =
					(((float)(count - 1) / (float)count) * newClusters[assignedTo])
					+ ((1.0f / count) * point);

				clusterCounts[assignedTo] = count;
			}

			for (int i = 0; i < clusterCount; i++) {
				clusters[i] = newClusters[i];
			}

			return (float)reassigned / (float)pointCount;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		float DistSqr(Vector3 a, Vector3 b) {
			var dx = a.x - b.x;
			var dy = a.y - b.y;
			var dz = a.z - b.z;

			return dx*dx + dy*dy + dz*dz;
		}
	}
}
