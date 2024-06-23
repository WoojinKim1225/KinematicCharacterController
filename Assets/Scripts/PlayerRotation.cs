using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerRotation : MonoBehaviour
{
    public KinematicCharacterController kcc;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (kcc.Forward == Vector3.zero) return;
        transform.rotation = Quaternion.LookRotation(kcc.Forward, kcc.Up);
    }
}
