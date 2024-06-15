using UnityEngine;
using UnityEngine.Animations;

public class PlayerAnimator : MonoBehaviour
{
    public KinematicCharacterController kcc;
    public Animator animator;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.parent.position = kcc.transform.position;
        transform.parent.rotation = Quaternion.Slerp(transform.parent.rotation, Quaternion.LookRotation(Vector3.ProjectOnPlane(kcc.ViewDirection, kcc.transform.up), kcc.transform.up), 1f - Mathf.Exp(-Time.deltaTime * 4f));
        animator.SetFloat("r", Vector3.ProjectOnPlane(kcc.Velocity, transform.parent.up).magnitude);
        float angle = Vector3.SignedAngle(transform.parent.forward, Vector3.ProjectOnPlane(kcc.Velocity, transform.parent.up), transform.parent.up);
        animator.SetFloat("theta", angle);
    }

    /// <summary>
    /// Callback for setting up animation IK (inverse kinematics).
    /// </summary>
    /// <param name="layerIndex">Index of the layer on which the IK solver is called.</param>
    void OnAnimatorIK(int layerIndex)
    {
        
    }
}
