using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class force : MonoBehaviour
{
    public bool b;
    public KinematicCharacterController kcc;
    public Vector3 f;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!b) return;
        kcc.AddForce(f);
    }
}
