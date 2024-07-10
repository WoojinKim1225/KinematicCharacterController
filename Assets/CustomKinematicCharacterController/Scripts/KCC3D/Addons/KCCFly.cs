using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class KCCFly : MonoBehaviour
{
    public KinematicCharacterController kcc;

    public Vector2 WingAngleIS;
    public float WingFoldIS;

    public MeshRenderer meshRenderer;

    private void OnEnable() {
        kcc.AddForce(kcc.Velocity * 40f, ForceMode.Impulse);
    }

    void Update()
    {        
        if (WingFoldIS == 1) {
            kcc.ExternalAirDrag = 0;
            Vector3 velocity = -kcc.Velocity;
            Vector3 forward = kcc.ViewDirection.normalized;
            
            Vector3 right = Vector3.Normalize(
                Vector3.Cross(Vector3.up, forward) 
                + Vector3.SignedAngle(Vector3.ProjectOnPlane(forward, Vector3.up), Vector3.ProjectOnPlane(kcc.Velocity, Vector3.up), Vector3.up) * Vector3.up * 0.05f
                );
            //forward = forward;
            Vector3 up = Vector3.Cross(forward, right);
            float area = Vector3.Cross(Vector3.ProjectOnPlane(forward, velocity.normalized), Vector3.ProjectOnPlane(right, velocity.normalized)).magnitude;
            Vector3 force = 0.5f * 1.225f * Mathf.Min(velocity.magnitude * velocity.magnitude, 10000) * area * up * Mathf.Sign(Vector3.Dot(up, velocity)) * 0.2f;
            kcc.AddForce(force);

            transform.position = kcc.transform.position + Vector3.up;
            transform.rotation = Quaternion.LookRotation(forward, up);
            Debug.DrawRay(kcc.transform.position, kcc.Velocity.sqrMagnitude * kcc.Velocity.normalized, Color.yellow);
            Debug.DrawRay(kcc.transform.position, right, Color.cyan);
            meshRenderer.enabled = true;

        } else {
            kcc.ExternalDragReset();
            meshRenderer.enabled = false;
        }
        
        
    }
}
