using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class climb : MonoBehaviour
{
    [SerializeField] private KinematicCharacterController kinematicCharacterController;
    
    void OnTriggerEnter(Collider other)
    {
        kinematicCharacterController.Gravity = kinematicCharacterController.gravity.InitialValue - Vector3.right * 20f;
    }

    void OnTriggerExit(Collider other)
    {
        kinematicCharacterController.gravity.Reset();
        kinematicCharacterController.Gravity = kinematicCharacterController.gravity.InitialValue;
    }
}
