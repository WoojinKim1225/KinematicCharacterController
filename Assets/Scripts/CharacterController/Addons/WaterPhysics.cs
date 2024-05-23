using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterSettings;

public class WaterPhysics : MonoBehaviour
{
    public KinematicCharacterController kcc;
    public Vector3 v;
    public float waterHeightWS;

    public float externalGroundDecel, externalAirDecel;
    public float externalGroundDamp, externalAirDamp;
    
    void OnTriggerEnter(Collider other)
    {
        kcc = other.GetComponentInParent<KinematicCharacterController>();
        if (kcc != null) {
            kcc.MovementMode = KinematicCharacterSettingExtensions.EMovementMode.Swim;
            kcc.IsDownStepEnabled = false;

            kcc.ExternalGroundDecel = externalGroundDecel;
            kcc.ExternalAirDecel = externalAirDecel;
            kcc.ExternalGroundDamp = externalGroundDamp;
            kcc.ExternalAirDamp = externalAirDamp;
        }
    }

    void OnTriggerStay(Collider other)
    {
        kcc = other.GetComponentInParent<KinematicCharacterController>();
        if (kcc != null) {
            v = Vector3.Lerp(Vector3.zero, Vector3.up * 21f, (waterHeightWS - kcc.transform.position.y) / kcc.HeightValue);
            
            kcc.AddForce(v - kcc.Velocity * 0.9f, this);
        }
    }

    void OnTriggerExit(Collider other)
    {
        kcc = other.GetComponentInParent<KinematicCharacterController>();
        if (kcc != null) {
            kcc.MovementMode = KinematicCharacterSettingExtensions.EMovementMode.Ground;
            kcc.IsDownStepEnabled = true;
            kcc.AddForce(Vector3.zero, this);
            kcc.ExternalDampReset();
            kcc.ExternalDecelReset();
        }
    }
}
