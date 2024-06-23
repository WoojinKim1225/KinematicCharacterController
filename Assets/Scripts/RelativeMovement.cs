using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RelativeMovement : MonoBehaviour
{
    public Transform referenceObject; // 상대적인 위치와 회전을 기준으로 할 오브젝트
    private Vector3 initialPositionOffset;
    private Quaternion initialRotationOffset;

    void Start()
    {
        // 초기 위치와 회전 오프셋 계산
        initialPositionOffset = Quaternion.Inverse(referenceObject.rotation) * (transform.position - referenceObject.position);
        initialRotationOffset = Quaternion.Inverse(referenceObject.rotation) * transform.rotation;
    }

    void Update()
    {
        // 새로운 위치와 회전을 계산
        Vector3 newPosition = referenceObject.position + referenceObject.rotation * initialPositionOffset;
        Quaternion newRotation = referenceObject.rotation * initialRotationOffset;

        // 현재 오브젝트의 위치와 회전에 적용
        transform.position = newPosition;
        transform.rotation = newRotation;
    }
}
