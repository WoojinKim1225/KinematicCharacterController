using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FakeDecalShadow : MonoBehaviour
{
    [SerializeField] private KinematicCharacterController characterController;
    [SerializeField] private LayerMask whatIsGround;

    void Update()
    {
        if (Physics.Raycast(characterController.transform.position + characterController.transform.up, Vector3.down, out RaycastHit hit, 100f, whatIsGround)) {
            transform.position = new Vector3(transform.position.x, hit.point.y, transform.position.z);
        }
    }
}
