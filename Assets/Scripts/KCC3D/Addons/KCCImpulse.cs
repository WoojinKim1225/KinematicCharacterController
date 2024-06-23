using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class KCCImpulse : MonoBehaviour
{
    public KinematicCharacterController kcc;

    public InputActionProperty inputActions;

    public bool b, b1;
    public Vector3 v;
    public float f;

    void OnEnable()
    {
        inputActions.action.Enable();
    }

    void OnDisable()
    {
        inputActions.action.Disable();
    }

    void Update()
    {
        b = inputActions.action.ReadValue<float>() != 0;
        
        if (b && !b1) {
            kcc.AddForce(kcc.ViewDirection * f, this, ForceMode.VelocityChange);
        }
        if (!b && b1) {

        }
        b1 = b;
    }
}