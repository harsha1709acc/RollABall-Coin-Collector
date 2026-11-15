using UnityEngine;

// LateUpdate runs after player physics/movement so the camera follows the latest position.
public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 9f, -9f);
    public float smoothSpeed = 5f;

    private void LateUpdate()
    {
        if (target == null) return;
        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.LookAt(target);
    }
}
