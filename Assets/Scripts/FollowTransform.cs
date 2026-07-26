using UnityEngine;

public class FollowTransform : MonoBehaviour
{
    private Transform targetTransform; 
    public void SetTargetTransform(Transform targetTransform)
    {
        this.targetTransform = targetTransform;
    }
    //要求在LateUpdate中更新位置和旋转，以确保在所有其他更新之后进行跟随
    private void LateUpdate()
    {
        if (targetTransform != null)
        {
            transform.position = targetTransform.position;
            transform.rotation = targetTransform.rotation;
        }
    }
}
