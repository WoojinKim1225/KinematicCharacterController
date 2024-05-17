using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterPhysics : MonoBehaviour
{
    public KinematicCharacterController kcc;
    public Vector3 v;
    

    void OnTriggerStay(Collider other)
    {
        kcc = other.GetComponentInParent<KinematicCharacterController>();
        if (kcc != null) {
            v = Vector3.up * 23f - kcc.VerticalVelocity;
            
            kcc.AddForce(v);
        }
    }

    void OnTriggerExit(Collider other)
    {
        kcc = other.GetComponentInParent<KinematicCharacterController>();
        if (kcc != null) {
            kcc.AddForce(Vector3.zero);
        }
    }
}
