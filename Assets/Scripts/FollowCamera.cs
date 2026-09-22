using UnityEngine;

/// <summary>Câmara em 3.ª pessoa que segue a cápsula.</summary>
public class FollowCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 6f, -9f);
    [SerializeField] private float smoothSpeed = 12f;
    [SerializeField] private float lookAtHeight = 0.75f;

    private void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition,
                                          Time.deltaTime * smoothSpeed);

        Vector3 lookPoint = target.position + Vector3.up * lookAtHeight;
        Vector3 lookDirection = lookPoint - transform.position;
        if (lookDirection.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(lookDirection);
    }
}
