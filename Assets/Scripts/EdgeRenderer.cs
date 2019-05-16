using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EdgeRenderer : MonoBehaviour {

	[SerializeField] LineRenderer _lineRenderer;

	public Transform Body1;
    public Transform Body2;

	// Update is called once per frame
	void LateUpdate () {
		var points = new Vector3[2]{Body1.position, Body2.position};
		_lineRenderer.SetPositions(points);

	}
}
