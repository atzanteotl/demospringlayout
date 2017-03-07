using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GraphSpringJoint : MonoBehaviour {

	public Rigidbody Body1;
	public Rigidbody Body2;
    
    public float length = 0.5f;
    public float springk = 12.2f;
    

	void FixedUpdate(){
		ApplyHookesLaw();
	}

    void ApplyHookesLaw(){
        Vector3 d = Body2.position - Body1.position;
        float displacement = length - d.magnitude;
        Vector3 direction = d.normalized;

        Body1.AddForce(springk * direction * displacement * -0.5f);
        Body2.AddForce(springk * direction * displacement * 0.5f);
    }
}
