using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlatformMover : MonoBehaviour
{
    Rigidbody rb;
    public AnimationCurve curve;
    Vector3 initPos;
    Vector3 pos;
    public Vector3 GetPosition => pos;
    float t = 0;
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        initPos = transform.position;
    }


    void FixedUpdate()
    {
        pos = initPos + curve.Evaluate(t) * Vector3.right;
        rb.MovePosition(pos);  
        t += Time.fixedDeltaTime;      
    }
}
