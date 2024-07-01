using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;


public class FeetIK : MonoBehaviour
{
    private Animator animator;
    [SerializeField] private KinematicCharacterController kcc;

    [SerializeField] private ExtractTransformConstraint leftFootExtract, rightFootExtract;
    [SerializeField] private OverrideTransform hip;
    [SerializeField] private TwoBoneIKConstraint leftFoot, rightFoot;
    
    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void Update() {
        transform.position = kcc.transform.position;

        if (kcc.Velocity.magnitude > 0) transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(kcc.ViewDirection, kcc.Up), kcc.Up);

        hip.data.sourceObject.localPosition = Vector3.up * (kcc.height - 2f);
        animator.SetFloat("r", kcc.HorizontalVelocity.magnitude);
        animator.SetFloat("theta", Vector3.SignedAngle(Vector3.ProjectOnPlane(kcc.ViewDirection, Vector3.up), Vector3.ProjectOnPlane(kcc.Velocity, Vector3.up), Vector3.up));
    }
}
