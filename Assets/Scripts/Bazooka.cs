using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GK {
	public class Bazooka : MonoBehaviour {

		public GameObject RocketPrefab;
		public Transform FireFrom;
		public float ShotDelay = 0.5f;
		float lastShot = 0.0f;

		void Update() {
			if (Time.time - lastShot >= ShotDelay && Input.GetButton("Fire1")) {
				var rocket = Instantiate(RocketPrefab);

				rocket.transform.position = FireFrom.position;
				rocket.transform.rotation = FireFrom.rotation;

				lastShot = Time.time;
			}
		}
	}
}
