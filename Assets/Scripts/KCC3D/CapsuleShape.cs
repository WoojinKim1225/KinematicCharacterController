using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class CapsuleShape : MonoBehaviour
{
    private Transform Bone, Bone001;
    public KinematicCharacterController kcc;
    public CapsuleCollider collider;

    private float _height, _radius;

    void Awake()
    {
        kcc = GetComponentInParent<KinematicCharacterController>();
        Bone = transform.Find("Armature").Find("Bone");
        Bone001 = transform.Find("Armature").Find("Bone.001");
    }

    void Update()
    {   
        if (kcc.Forward != Vector3.zero) transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(kcc.Forward, kcc.Up), kcc.Up);

        if (Application.isPlaying) {
            _height = kcc.CharacterSizeSettings.height.Value;
            _radius = kcc.CharacterSizeSettings.capsuleRadius.Value;
            
        } else {
            _height = kcc.CharacterSizeSettings.height.InitialValue;
            _radius = kcc.CharacterSizeSettings.capsuleRadius.InitialValue;
            if (collider != null) {
                collider.height = _height;
                collider.radius = _radius;
            }

        }
        if (!Application.isPlaying || kcc.CharacterSizeSettings.height.IsChanged || kcc.CharacterSizeSettings.capsuleRadius.IsChanged) {
            Bone.localScale = _radius * 2f * Vector3.one;
            Bone.localPosition = _radius * Vector3.up - Vector3.up;
            Bone001.localScale = _radius * 2f * Vector3.one;
            Bone001.localPosition = (_height - _radius) * Vector3.up - Vector3.up;
        }
    }
}
