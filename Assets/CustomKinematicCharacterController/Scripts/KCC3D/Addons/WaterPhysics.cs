using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterSettings;

public class WaterPhysics : MonoBehaviour
{
    public KinematicCharacterController kcc;
    public Vector3 v;
    public float f;
    public float waterHeightWS;

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
        kcc = other.GetComponentInParent<KinematicCharacterController>();
        if (kcc != null) {
            v = Vector3.Lerp(Vector3.zero, Vector3.up * f, (waterHeightWS - kcc.transform.position.y) / kcc.CharacterSizeSettings.height.Value);
            
            kcc.AddForce(v - kcc.Velocity.y * Vector3.up * 0.1f - Vector3.ProjectOnPlane(kcc.Velocity, Vector3.up) * 1f);
        }
    }

    void OnTriggerExit(Collider other)
    {
        kcc = other.GetComponentInParent<KinematicCharacterController>();
        if (kcc != null) {
            // kcc.MovementMode = KinematicCharacterSettingExtensions.EMovementMode.Ground;
            kcc.IsDownStepEnabled = true;
            kcc.ExternalDragReset();
        }
    }
}
