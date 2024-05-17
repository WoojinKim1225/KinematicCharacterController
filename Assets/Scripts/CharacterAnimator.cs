using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.SocialPlatforms;

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

    private RigidTransform FootInit_l, FootInit_r;

    [SerializeField] private Vector3 solePoint_l;
    [SerializeField] private Vector3 solePoint_r;
    [SerializeField] private Vector3 solePointFromFoot_l;
    [SerializeField] private Vector3 solePointFromFoot_r;

    private Vector3 solePointOS_l, solePointOS_r;
    private float playerUpAmount, playerUpAmountSmooth;

    private Vector3 solePointWS_l, solePointWS_r;

    private float rayStartUpDist = 1f, rayEndDownDist = 1f;

    private bool isStepStart_l, isStepStart_r;

    [SerializeField] private RigidTransform FootBone_l, FootBone_r;

    void Awake()
    {
        animator = GetComponent<Animator>();
        FootInit_l.SetTransformFromBone(animator, HumanBodyBones.LeftFoot);
        
        FootInit_r.SetTransformFromBone(animator,HumanBodyBones.RightFoot);

        solePointFromFoot_l = animator.GetBoneTransform(HumanBodyBones.LeftFoot).InverseTransformPoint(animator.transform.TransformPoint(solePoint_l));
        solePointFromFoot_r = animator.GetBoneTransform(HumanBodyBones.RightFoot).InverseTransformPoint(animator.transform.TransformPoint(solePoint_r));
    }

    void LateUpdate()
    {
        
        if (Vector3.ProjectOnPlane(kcc.HorizontalDirection, kcc.Up) != Vector3.zero) animator.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(kcc.HorizontalDirection, kcc.Up), kcc.Up);
        animator.SetFloat("Speed", kcc.Speed);
    }

    private void OnAnimatorIK(int layerIndex) {
        animator.transform.position = kcc.transform.position;
        FootBone_l.SetTransformFromBone(animator, HumanBodyBones.LeftFoot);
        FootBone_r.SetTransformFromBone(animator, HumanBodyBones.RightFoot);

        solePointWS_l = FootBone_l.pos + FootBone_l.rot * solePointFromFoot_l;
        solePointWS_r = FootBone_r.pos + FootBone_r.rot * solePointFromFoot_r;
        solePointOS_l = transform.InverseTransformPoint(solePointWS_l);
        solePointOS_r = transform.InverseTransformPoint(solePointWS_r);
        
        Ray ray_l = new Ray(solePointWS_l + kcc.Up * rayStartUpDist, -kcc.Up);
        bool isFootHit_l = Physics.Raycast(ray_l, out RaycastHit h_l, rayStartUpDist + rayEndDownDist, kcc.WhatIsGround, QueryTriggerInteraction.Ignore);

        Ray ray_r = new Ray(solePointWS_r + kcc.Up * rayStartUpDist, -kcc.Up);
        bool isFootHit_r = Physics.Raycast(ray_r, out RaycastHit h_r, rayStartUpDist + rayEndDownDist, kcc.WhatIsGround, QueryTriggerInteraction.Ignore);

        
        if (isFootHit_l && isFootHit_r) playerUpAmount = -Mathf.Max(Vector3.Dot(kcc.transform.position - h_l.point, kcc.Up), Vector3.Dot(kcc.transform.position - h_r.point, kcc.Up));
        else playerUpAmount = 0f;

        playerUpAmountSmooth += (playerUpAmount - playerUpAmountSmooth) * Time.fixedDeltaTime * 20f;
        
        animator.transform.position = kcc.transform.position + kcc.Up * playerUpAmountSmooth;

        
        if (isFootHit_l) {
            Vector3 p = h_l.point - Quaternion.FromToRotation(kcc.Up, h_l.normal) * FootBone_l.rot * solePointFromFoot_l + Vector3.Project(solePointOS_l, kcc.Up);
            Quaternion q = Quaternion.FromToRotation(kcc.Up, h_l.normal) * (FootBone_l.rot * Quaternion.Inverse(FootInit_l.rot));

            animator.SetIKPosition(AvatarIKGoal.LeftFoot, p);
            animator.SetIKRotation(AvatarIKGoal.LeftFoot, q);
            animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 1);
            //animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, Mathf.Clamp01(1f - Vector3.Dot(animator.transform.position - h_l.point, kcc.Up) * 2f));
            //animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, Mathf.Clamp01(1f - Vector3.Dot(animator.transform.position - h_l.point, kcc.Up) * 2f));
        } else {
            animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 0);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 0);
        }

        if (isFootHit_r) {
            Vector3 p = h_r.point - Quaternion.FromToRotation(kcc.Up, h_r.normal) * FootBone_r.rot * solePointFromFoot_r + Vector3.Project(solePointOS_r, kcc.Up);
            Quaternion q = Quaternion.FromToRotation(kcc.Up, h_r.normal) * (FootBone_r.rot * Quaternion.Inverse(FootInit_r.rot));

            animator.SetIKPosition(AvatarIKGoal.RightFoot, p);
            animator.SetIKRotation(AvatarIKGoal.RightFoot, q);
            animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1);
            animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 1);
            //animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, Mathf.Clamp01(1f - Vector3.Dot(animator.transform.position - h_l.point, kcc.Up) * 2f));
            //animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, Mathf.Clamp01(1f - Vector3.Dot(animator.transform.position - h_l.point, kcc.Up) * 2f));
        } else {
            animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0);
            animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 0);
        }
        

        Debug.DrawLine(animator.transform.position, solePointWS_l, Color.white);
        Debug.DrawLine(animator.transform.position, solePointWS_r, Color.white);

    }
}
