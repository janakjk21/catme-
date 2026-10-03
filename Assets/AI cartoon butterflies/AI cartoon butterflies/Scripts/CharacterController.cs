using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterController : MonoBehaviour {
    Rigidbody rb;
    public void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void FixedUpdate() {
        transform.position+=new Vector3(Input.GetAxis("Horizontal"),0f, Input.GetAxis("Vertical"));    
    }
}
