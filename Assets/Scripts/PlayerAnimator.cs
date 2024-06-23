using UnityEngine;
using UnityEngine.Animations;

[System.Serializable]
public class FootIK{
    private Animator _anim;

    public Transform root;
    public Transform mid;
    public Transform tip;

    private AvatarIKGoal _ikGoal;
    private AvatarIKHint _ikHint;

    public Transform rayHint;
    public Transform IKTarget;

    public Matrix4x4 rayHintToTip, tipToRayHint;

    public float weight;

    public FootIK(Animator anim, Transform rH, Transform target, bool isRight) {
        _anim = anim;
        rayHint = rH;
        IKTarget = target;
        if (isRight) {
            _ikGoal = AvatarIKGoal.RightFoot;
            _ikHint = AvatarIKHint.RightKnee;
            root = _anim.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            mid = _anim.GetBoneTransform(HumanBodyBones.RightLowerLeg);
            tip = _anim.GetBoneTransform(HumanBodyBones.RightFoot);
        } else {
            _ikGoal = AvatarIKGoal.LeftFoot;
            _ikHint = AvatarIKHint.LeftKnee;
            root = _anim.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            mid = _anim.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
            tip = _anim.GetBoneTransform(HumanBodyBones.LeftFoot);
        }
        rayHintToTip = tip.localToWorldMatrix * rayHint.worldToLocalMatrix;
        tipToRayHint = rayHint.localToWorldMatrix * tip.worldToLocalMatrix;

    }

    public void CalcIK(LayerMask layer) {
        Vector3 p = tip.position + tipToRayHint.GetPosition();
        Debug.DrawRay(tip.position, tipToRayHint.GetPosition());
        Debug.DrawRay(p + Vector3.up, Vector3.down * 2f, Color.cyan);
    }
    
}

public class PlayerAnimator : MonoBehaviour
{
    public KinematicCharacterController kcc;
    public Animator animator;

    public Transform RayHint_L, RayHint_R;

    public Transform RayHit_L, RayHit_R;

    public LayerMask whatIsGround;
    public Quaternion offset;

    //public FootIK l, r;

    void Awake()
    {
        //l = new(animator, RayHint_L, FootTarget_L, false);
        //r = new(animator, RayHint_R, FootTarget_R, true);
        //l.weight = 1;
        //r.weight = 1;
    }

    void Update()
    {
        transform.parent.position = kcc.transform.position;
        transform.parent.rotation = Quaternion.Slerp(transform.parent.rotation, Quaternion.LookRotation(Vector3.ProjectOnPlane(kcc.ViewDirection, kcc.transform.up), kcc.transform.up), 1f - Mathf.Exp(-Time.deltaTime * 4f));
        animator.SetFloat("r", Vector3.ProjectOnPlane(kcc.Velocity, transform.parent.up).magnitude);
        float angle = Vector3.SignedAngle(transform.parent.forward, Vector3.ProjectOnPlane(kcc.Velocity, transform.parent.up), transform.parent.up);
        animator.SetFloat("theta", angle);
    }

    void OnAnimatorIK(int layerIndex)
    {
        Vector3 tipPos_L = RayHint_L.position;
        Vector3 tipForward_L = RayHint_L.forward;

        Vector3 tipPos_R = RayHint_R.position;
        Vector3 tipForward_R = RayHint_R.forward;
        
        if (Physics.Raycast(tipPos_L + Vector3.up * 0.5f, Vector3.down, out RaycastHit hitL, 1f, whatIsGround)) {
            Vector3 ikPos = hitL.point;
            Vector3 ikUp = hitL.normal;
            Vector3 ikForward = tipForward_L - Vector3.Dot(tipForward_L, hitL.normal) / hitL.normal.y * Vector3.up;

            RayHit_L.rotation = Quaternion.LookRotation(ikForward, ikUp);
            RayHit_L.position = ikPos;

            SetIKTransform(animator, AvatarIKGoal.LeftFoot, RayHit_L.GetChild(0).position, RayHit_L.rotation, 1);
        }

        if (Physics.Raycast(tipPos_R + Vector3.up * 0.5f, Vector3.down, out RaycastHit hitR, 1f, whatIsGround)) {
            Vector3 ikPos = hitR.point;
            Vector3 ikUp = hitR.normal;
            Vector3 ikForward = tipForward_R - Vector3.Dot(tipForward_R, hitR.normal) / hitR.normal.y * Vector3.up;

            RayHit_R.rotation = Quaternion.LookRotation(ikForward, ikUp);
            RayHit_R.position = ikPos;

            SetIKTransform(animator, AvatarIKGoal.RightFoot, RayHit_R.GetChild(0).position, RayHit_R.rotation, 1);
        }
    }

    void SetIKTransform(Animator animator, AvatarIKGoal goal, Vector3 pos, Quaternion rot, float weight) {
        animator.SetIKPosition(goal, pos);
        animator.SetIKPositionWeight(goal, 1);
        animator.SetIKRotation(goal, rot);
        animator.SetIKRotationWeight(goal, 1);
    }
}
