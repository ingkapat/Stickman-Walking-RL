using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Offset")]
    public Vector3 offset = new Vector3(3f, 1f, -10f);

    [Header("Follow Settings")]
    public float smoothSpeed = 5f;
    public bool followX = true;
    public bool followY = true;

    [Header("Zoom (Orthographic)")]
    public bool autoZoom = false;
    public float minSize = 5f;
    public float maxSize = 10f;

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = transform.position;

        if (followX) desiredPosition.x = target.position.x + offset.x;
        if (followY) desiredPosition.y = target.position.y + offset.y;
        desiredPosition.z = offset.z;

        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
    }
}
