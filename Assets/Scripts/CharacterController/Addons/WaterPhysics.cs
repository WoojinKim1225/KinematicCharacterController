using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterSettings;

public class WaterPhysics : MonoBehaviour
{
    public KinematicCharacterController kcc;
    public Vector3 v;
    public float waterHeightWS;
    

    void OnTriggerStay(Collider other)
    {
        kcc = other.GetComponentInParent<KinematicCharacterController>();
        if (kcc != null) {
            v = Vector3.Lerp(Vector3.zero, Vector3.up * 21f, (waterHeightWS - kcc.transform.position.y) / kcc.HeightValue);
            Debug.DrawRay(kcc.transform.position, kcc.Velocity, Color.yellow);
            kcc.MovementMode = KinematicCharacterSettingExtensions.EMovementMode.Swim;

            
            kcc.AddForce(v - kcc.Velocity * 0.9f);
        }
    }

    void OnTriggerExit(Collider other)
    {
        kcc = other.GetComponentInParent<KinematicCharacterController>();
        if (kcc != null) {
            kcc.MovementMode = KinematicCharacterSettingExtensions.EMovementMode.Ground;
            kcc.AddForce(Vector3.zero);
            //kcc.SetVelocity(Vector3.zero);
        }
    }
}
