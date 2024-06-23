using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;


public class FeetIK : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private KinematicCharacterController kcc;
    
    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void Update() {
        transform.position = kcc.transform.position;

        if (kcc.Velocity.magnitude > 0) transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(kcc.Velocity, kcc.Up), kcc.Up);

        
    }
}
