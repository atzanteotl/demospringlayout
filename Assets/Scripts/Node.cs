using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node : MonoBehaviour {

	public Rigidbody Body { get { return GetComponent<Rigidbody>(); } }

	public Vector3 position {
		get { return pos; }
	}

	public Vector3 pos = new Vector3();
	public Vector3 acc = new Vector3();
	public Vector3 vel = new Vector3();

	public void AddForce(Vector3 f){
		acc += f * 10000;
	}

}
