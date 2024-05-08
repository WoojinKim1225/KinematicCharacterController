using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CapsuleShape : MonoBehaviour
{
    private Transform Bone, Bone001;
    public KinematicCharacterController kcc;

    /// <summary>
    /// Awake is called when the script instance is being loaded.
    /// </summary>
    void Awake()
    {
        Bone = transform.Find("Armature").Find("Bone");
        Bone001 = transform.Find("Armature").Find("Bone.001");
    }

    /// <summary>
    /// Update is called every frame, if the MonoBehaviour is enabled.
    /// </summary>
    void Update()
    {
        Bone.localScale = kcc.CapusleRadius * 200f * Vector3.one;
        Bone.localPosition = (kcc.IdleHeight * 0.5f - kcc.CapusleRadius) * Vector3.down;
        Bone001.localScale = kcc.CapusleRadius * 200f * Vector3.one;
        Bone001.localPosition = (-kcc.IdleHeight * 0.5f + kcc.CapsuleHeight - kcc.CapusleRadius) * Vector3.up;
    }
}