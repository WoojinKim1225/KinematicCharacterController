using System.Collections;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;

[System.Serializable]
public struct RigidTransform {
    public Vector3 pos;
    public Quaternion rot;

    public void SetTransform(Vector3 p, Quaternion q) {
        pos = p;
        rot = q;
    }

    public void SetTransform(Transform trs) {
        pos = trs.position;
        rot = trs.rotation;
    }

    public void SetTransformFromBone(Animator animator, HumanBodyBones boneID) {
        pos = animator.GetBoneTransform(boneID).position;
        rot = animator.GetBoneTransform(boneID).rotation;
    }

    public void SetLocalTransformFromBone(Animator animator, HumanBodyBones boneID, Transform parent) {
        pos = parent.InverseTransformPoint(animator.GetBoneTransform(boneID).position);
        rot = Quaternion.Inverse(parent.rotation) * animator.GetBoneTransform(boneID).rotation;
    }

    public void SetTransformFromBone(Animator animator, HumanBodyBones boneID, Quaternion init) {
        pos = animator.GetBoneTransform(boneID).position;
        rot = animator.GetBoneTransform(boneID).rotation * Quaternion.Inverse(init);
    }
}

public class CharacterAnimator : MonoBehaviour
{
    [SerializeField] private KinematicCharacterController kcc;
    [SerializeField] private Animator animator;

    private Vector3 velocity => kcc.Velocity;

    private RigidTransform leftFootInit, rightFootInit;

    [SerializeField] private RigidTransform leftFootBone, rightFootBone, leftFootBoneOS, rightFootBoneOS;

    private Vector3 leftFootBonePosBefore, rightFootBonePosBefore;

    private RigidTransform leftFootCalculated, rightFootCalculated;

    void Awake()
    {
        animator = GetComponent<Animator>();
        leftFootInit.SetTransformFromBone(animator, HumanBodyBones.LeftFoot);
        rightFootInit.SetTransformFromBone(animator,HumanBodyBones.RightFoot);

        /*
        leftFootInit.pos -= kcc.transform.position;
        rightFootInit.pos -= kcc.transform.position;
        leftToesInit.pos -= kcc.transform.position;
        rightToesInit.pos -= kcc.transform.position;
        */
    }

    void LateUpdate()
    {
        animator.transform.parent.position = kcc.transform.position;
        if (Vector3.ProjectOnPlane(kcc.HorizontalDirection, kcc.Up) != Vector3.zero) animator.transform.parent.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(kcc.HorizontalDirection, kcc.Up), kcc.Up);
    }


    void OnAnimatorIK(int layerIndex)
    {
        leftFootBone.SetTransformFromBone(animator, HumanBodyBones.LeftFoot, leftFootInit.rot);
        rightFootBone.SetTransformFromBone(animator, HumanBodyBones.RightFoot, rightFootInit.rot);
        leftFootBoneOS.SetLocalTransformFromBone(animator, HumanBodyBones.LeftFoot, transform.parent);
        rightFootBoneOS.SetLocalTransformFromBone(animator, HumanBodyBones.RightFoot, transform.parent);

        Debug.DrawRay(leftFootBone.pos, (leftFootBone.pos - leftFootBonePosBefore) / Time.deltaTime, Color.white);
        Debug.DrawRay(rightFootBone.pos, (rightFootBone.pos - rightFootBonePosBefore) / Time.deltaTime, Color.white);

        bool isLeftFootTargetHit = Physics.Raycast(leftFootBone.pos, (leftFootBone.pos - leftFootBonePosBefore).normalized, out RaycastHit TargetHitL, 1f, kcc.WhatIsGround);
        bool isRightFootTargetHit = Physics.Raycast(rightFootBone.pos, (rightFootBone.pos - rightFootBonePosBefore).normalized, out RaycastHit TargetHitR, 1f, kcc.WhatIsGround);

        Debug.DrawRay(TargetHitL.point, TargetHitL.normal, Color.white);
        Debug.DrawRay(TargetHitR.point, TargetHitR.normal, Color.white);

        bool isLeftFootHit = Physics.Raycast(Vector3.ProjectOnPlane(leftFootBone.pos, kcc.Up) + Vector3.Project(kcc.transform.position, kcc.Up) + kcc.Up * 0.5f, -kcc.Up, out RaycastHit hitLeftFoot, 0.6f, kcc.WhatIsGround);
        bool isRightFootHit = Physics.Raycast(Vector3.ProjectOnPlane(rightFootBone.pos, kcc.Up) + Vector3.Project(kcc.transform.position, kcc.Up) + kcc.Up * 0.5f, -kcc.Up, out RaycastHit hitRightFoot, 0.6f, kcc.WhatIsGround);

        //Debug.DrawLine(Vector3.ProjectOnPlane(leftFootBone.pos, kcc.Up), Vector3.ProjectOnPlane(rightFootBone.pos, kcc.Up), Color.white);

        if (isLeftFootHit) {
            if (isRightFootHit) {
                animator.transform.localPosition = Vector3.up * Mathf.Min(-hitLeftFoot.distance + 0.5f, -hitRightFoot.distance + 0.5f);
            } else {
                animator.transform.localPosition = Vector3.up * (-hitLeftFoot.distance + 0.5f);
            }
        }
        else {
            if (isRightFootHit) {
                animator.transform.localPosition = Vector3.up * (-hitRightFoot.distance + 0.5f);
            } else {
                animator.transform.localPosition = Vector3.zero;
            }
        }

        

        if (isLeftFootHit) {
            Debug.DrawRay(leftFootBone.pos + kcc.Up * 0.5f, -kcc.Up, transform.InverseTransformPoint(leftFootBone.pos).y < 0.12f ? Color.white : Color.black);
            Debug.Log(transform.InverseTransformPoint(leftFootBone.pos));
            Debug.DrawRay(hitLeftFoot.point, hitLeftFoot.normal, transform.InverseTransformPoint(leftFootBone.pos).y < 0.12f ? Color.white : Color.black);
            leftFootCalculated.pos = hitLeftFoot.point + transform.InverseTransformPoint(leftFootBone.pos).y * kcc.Up / Vector3.Dot(kcc.Up, hitLeftFoot.normal);
            leftFootCalculated.rot = Quaternion.FromToRotation(kcc.Up, hitLeftFoot.normal) * leftFootBone.rot;

            animator.SetIKPosition(AvatarIKGoal.LeftFoot, leftFootCalculated.pos);
            animator.SetIKRotation(AvatarIKGoal.LeftFoot, leftFootCalculated.rot);
            animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 1);
        } else {
            animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 0);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 0);
        }

        if (isRightFootHit) {
            Debug.DrawRay(rightFootBone.pos + kcc.Up * 0.5f, -kcc.Up, transform.InverseTransformPoint(rightFootBone.pos).y < 0.12f ? Color.white : Color.black);
            Debug.DrawRay(hitRightFoot.point, hitRightFoot.normal, transform.InverseTransformPoint(rightFootBone.pos).y < 0.12f ? Color.white : Color.black);
            rightFootCalculated.pos = hitRightFoot.point + transform.InverseTransformPoint(rightFootBone.pos).y * kcc.Up / Vector3.Dot(kcc.Up, hitRightFoot.normal);
            rightFootCalculated.rot = Quaternion.FromToRotation(kcc.Up, hitRightFoot.normal) * rightFootBone.rot;

            animator.SetIKPosition(AvatarIKGoal.RightFoot, rightFootCalculated.pos);
            animator.SetIKRotation(AvatarIKGoal.RightFoot, rightFootCalculated.rot);
            animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1);
            animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 1);
        } else {
            animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0);
            animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 0);
        }
        animator.SetFloat("Speed", kcc.Speed);
        
        leftFootBonePosBefore = leftFootBone.pos;
        rightFootBonePosBefore = rightFootBone.pos;
        
    }
}
