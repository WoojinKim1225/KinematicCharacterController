using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KCCSettings;
using KCC;

public class WaterPhysics : MonoBehaviour
{
    public KinematicCharacterController kcc;
    public Vector3 v;
    public float f;
    public float waterHeightWS;
    private Vector3 beforeAccel, beforeVel;
    public Vector3 va;

    public float externalContactDrag, externalAirDrag;
    
    void OnTriggerEnter(Collider other)
    {
        kcc = other.GetComponentInParent<KinematicCharacterController>();
        if (kcc != null) {
            // kcc.movementSettings.MovementMode = KinematicCharacterSettingExtensions.EMovementMode.Swim;
            kcc.IsDownStepEnabled = false;

            kcc.ExternalContactDrag = externalContactDrag;
            kcc.ExternalAirDrag = externalAirDrag;
        }
    }

    void OnTriggerStay(Collider other)
    {
        Debug.Log(other.name);
        kcc = other.GetComponentInParent<KinematicCharacterController>();
        if (kcc != null) {
            v = Vector3.Lerp(Vector3.zero, Vector3.up * f, (waterHeightWS - kcc.transform.position.y) / kcc.CharacterSizeSettings.height.Value);
            Vector3 accel = (kcc.HorizontalVelocity - beforeVel) / Time.fixedDeltaTime;
            kcc.AddForce(v - kcc.Velocity);
            //Debug.Log("asdf");
            //kcc.AddForce(v - kcc.VerticalVelocity * 1f + va);
            //Debug.DrawRay(kcc.transform.position, accel * va.x + kcc.HorizontalVelocity * va.y, Color.white);
            //kcc.AddForce(-kcc.HorizontalVelocity);
            beforeAccel = accel;
            beforeVel = kcc.HorizontalVelocity;
        }
    }

    void OnTriggerExit(Collider other)
    {
        kcc = other.GetComponentInParent<KinematicCharacterController>();
        if (kcc != null) {
            kcc.IsDownStepEnabled = true;
            kcc.ExternalDragReset();
        }
    }
}
