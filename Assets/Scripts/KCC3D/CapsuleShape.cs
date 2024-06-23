using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class CapsuleShape : MonoBehaviour
{
    private Transform Bone, Bone001;
    public KinematicCharacterController kcc;

    void Awake()
    {
        kcc = GetComponentInParent<KinematicCharacterController>();
        Bone = transform.Find("Armature").Find("Bone");
        Bone001 = transform.Find("Armature").Find("Bone.001");
    }

    void Update()
    {   
        if (kcc.Forward != Vector3.zero) transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(kcc.Forward, kcc.Up), kcc.Up);
        Bone.localScale = kcc.CharacterSizeSettings._capsuleRadius * 2f * Vector3.one;
        Bone.localPosition = (kcc.CharacterSizeSettings._capsuleRadius - 1)* Vector3.up;
        Bone001.localScale = kcc.CharacterSizeSettings._capsuleRadius * 2f * Vector3.one;
        Bone001.localPosition = (kcc.height - kcc.CharacterSizeSettings._capsuleRadius - 1) * Vector3.up;
    }
}
