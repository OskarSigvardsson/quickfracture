using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace GK {
	public class Rocket : MonoBehaviour {

		public float ExplosionRadius = 0.5f;
		public float ExplosionForce = 10.0f;
		public float Speed = 3.0f;
		public float Lifetime = 3.0f;

		Rigidbody rb;
		float spawned;

		void Start() {
			spawned = Time.fixedTime;
		}

		void FixedUpdate() {
			if (Time.time - spawned > Lifetime) {
				Destroy(gameObject);
			} else {
				if (rb == null) {
					rb = GetComponent<Rigidbody>();
					rb.velocity = Speed * transform.forward;
				}
			}
		}

		void OnCollisionEnter(Collision collision) {
			var fracture = collision.gameObject.GetComponent<Fracture>();

			if (fracture != null) {
				var world = collision.contacts[0].point;
				var local = fracture.transform.InverseTransformPoint(world);

				Profiler.BeginSample("Do fracture call");

				//fracture.DoFracture(local);
				fracture.DoFracture();

				Profiler.EndSample();

				StartCoroutine(Stupid(world));
			}
		}

		IEnumerator Stupid(Vector3 worldPoint) {
			yield return null;
			yield return null;
			yield return null;

			foreach (var coll in Physics.OverlapSphere(worldPoint, ExplosionRadius)) {
				var otherRb = coll.GetComponent<Rigidbody>();

				if (otherRb != null) {
					otherRb.AddExplosionForce(ExplosionForce, worldPoint, ExplosionRadius);
				}
			}

			Destroy(gameObject);
		}
	}
}
