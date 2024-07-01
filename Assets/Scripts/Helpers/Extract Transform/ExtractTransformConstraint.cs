using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;



[System.Serializable]
public struct ExtractTransformConstraintData : IAnimationJobData
{
    [SyncSceneToStream] public Transform bone;

    public Vector3 position;
    public Quaternion rotation;

    public bool IsValid()
    {
        return bone != null;
    }

    public void SetDefaultValues()
    {
        this.bone = null;

        this.position = Vector3.zero;
        this.rotation = Quaternion.identity;
    }
}

public struct ExtractTransformConstraintJob : IWeightedAnimationJob
{
    public ReadWriteTransformHandle bone;

    public FloatProperty jobWeight { get; set; }

    public Vector3Property position;
    public Vector4Property rotation;

    public void ProcessRootMotion(AnimationStream stream)
    { }

    public void ProcessAnimation(AnimationStream stream)
    {
        AnimationRuntimeUtils.PassThrough(stream, this.bone);

        Vector3 pos = this.bone.GetPosition(stream);
        Quaternion rot = this.bone.GetRotation(stream);

        this.position.Set(stream, pos);
        this.rotation.Set(stream, new Vector4(rot.x, rot.y, rot.z, rot.w));
    }
}

public class ExtractTransformConstraintJobBinder : AnimationJobBinder<
    ExtractTransformConstraintJob,
    ExtractTransformConstraintData>
{
    public override ExtractTransformConstraintJob Create(Animator animator,
        ref ExtractTransformConstraintData data, Component component)
    {
        return new ExtractTransformConstraintJob
        {
            bone = ReadWriteTransformHandle.Bind(animator, data.bone),
            position = Vector3Property.Bind(animator, component, "m_Data." + nameof(data.position)),
            rotation = Vector4Property.Bind(animator, component, "m_Data." + nameof(data.rotation))
        };
    }

    public override void Destroy(ExtractTransformConstraintJob job)
    { }
}

[DisallowMultipleComponent]
[AddComponentMenu("Animation Rigging/Extract Transform Constraint")]

public class ExtractTransformConstraint : RigConstraint<
    ExtractTransformConstraintJob,
    ExtractTransformConstraintData,
    ExtractTransformConstraintJobBinder>
{

}