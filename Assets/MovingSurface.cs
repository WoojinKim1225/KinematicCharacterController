using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingSurface : MonoBehaviour
{
    public KinematicCharacterController kcc;
    
    public Vector3 velocity;

    void OnTriggerEnter(Collider other)
    {
        kcc = other.attachedRigidbody.gameObject.GetComponent<KinematicCharacterController>();
        if (kcc != null) {
            kcc.ExternalGroundMove = transform.TransformDirection(velocity);
        }
    }

    void OnTriggerExit(Collider other)
    {
        kcc = other.attachedRigidbody.gameObject.GetComponent<KinematicCharacterController>();
        if (kcc != null) {
            kcc.ExternalGroundMove = Vector3.zero;
            kcc = null;
        }
    }
}
