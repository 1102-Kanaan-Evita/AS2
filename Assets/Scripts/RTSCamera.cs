using UnityEngine;

public class RTSCamera : MonoBehaviour
{
    [Header("Movement Settings")]
    public float panSpeed = 10f;
    public float edgePanSpeed = 7f;
    public float edgePanBorderThickness = 10f;
    public bool enableEdgePanning = true;
    public bool lockYAxis = true;

    [Header("Zoom Settings")]
    public float zoomSpeed = 2f;
    public float minZoom = 5f;
    public float maxZoom = 50f;

    [Header("Rotation Settings")]
    public float rotationSpeed = 100f;
    public bool enableRotation = true;

    [Header("Bounds (Optional)")]
    public bool useBounds = false;
    public Vector2 minBounds = new Vector2(-50, -50);
    public Vector2 maxBounds = new Vector2(50, 50);

    [Header("Smooth Movement")]
    public float smoothTime = 0.1f;

    private Vector3 targetPosition;
    private float targetZoom;
    private Camera cam;
    private Vector3 velocity = Vector3.zero;
    private float zoomVelocity = 0f;
    private bool isOrthographic;

    void Start()
    {
        cam = GetComponent<Camera>();
        targetPosition = transform.position;

        isOrthographic = cam.orthographic;
        targetZoom = isOrthographic ? cam.orthographicSize : transform.position.y;
    }

    void Update()
    {
        HandleKeyboardPanning();
        HandleEdgePanning();
        HandleZoom();
        HandleRotation();
        HandleMiddleMouseDrag();
        ApplyMovement();
    }

    void HandleKeyboardPanning()
    {
        Vector3 move = Vector3.zero;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            move += Vector3.forward;

        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            move += Vector3.back;

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            move += Vector3.right;

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            move += Vector3.left;

        if (move != Vector3.zero)
        {
            targetPosition += move.normalized * panSpeed * Time.deltaTime;
        }
    }

    void HandleEdgePanning()
    {
        if (!enableEdgePanning) return;

        Vector3 move = Vector3.zero;

        if (Input.mousePosition.x >= Screen.width - edgePanBorderThickness)
            move += Vector3.right;
        if (Input.mousePosition.x <= edgePanBorderThickness)
            move += Vector3.left;
        if (Input.mousePosition.y >= Screen.height - edgePanBorderThickness)
            move += Vector3.forward;
        if (Input.mousePosition.y <= edgePanBorderThickness)
            move += Vector3.back;

        if (move != Vector3.zero)
        {
            targetPosition += move.normalized * edgePanSpeed * Time.deltaTime;
        }
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scroll) > 0.01f)
        {
            if (isOrthographic)
            {
                targetZoom -= scroll * zoomSpeed;
                targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
            }
            else
            {
                targetZoom -= scroll * zoomSpeed * 5f;
                targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
            }
        }
    }

    void HandleRotation()
    {
        if (!enableRotation) return;

        if (Input.GetKey(KeyCode.Q))
            transform.RotateAround(transform.position, Vector3.up, rotationSpeed * Time.deltaTime);

        if (Input.GetKey(KeyCode.E))
            transform.RotateAround(transform.position, Vector3.up, -rotationSpeed * Time.deltaTime);

        // ALT + Middle Mouse Drag (Orbit)
        if (Input.GetMouseButton(2) && Input.GetKey(KeyCode.LeftAlt))
        {
            float rotateX = Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
            transform.RotateAround(transform.position, Vector3.up, rotateX);
        }
    }

    void HandleMiddleMouseDrag()
    {
        if (Input.GetMouseButton(2) && !Input.GetKey(KeyCode.LeftAlt))
        {
            float moveX = -Input.GetAxis("Mouse X") * panSpeed * 0.1f;
            float moveZ = -Input.GetAxis("Mouse Y") * panSpeed * 0.1f;

            Vector3 drag = Vector3.right * moveX + Vector3.forward * moveZ;
            targetPosition += drag;
        }
    }

    void ApplyMovement()
    {
        if (useBounds)
        {
            targetPosition.x = Mathf.Clamp(targetPosition.x, minBounds.x, maxBounds.x);
            targetPosition.z = Mathf.Clamp(targetPosition.z, minBounds.y, maxBounds.y);
        }

        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);

        if (isOrthographic)
        {
            cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, targetZoom, ref zoomVelocity, smoothTime);
        }
        else
        {
            Vector3 pos = transform.position;
            pos.y = Mathf.SmoothDamp(pos.y, targetZoom, ref zoomVelocity, smoothTime);
            transform.position = pos;
        }
    }

    // Public control methods

    public void FocusOnPosition(Vector3 position)
    {
        targetPosition = new Vector3(position.x, transform.position.y, position.z);
    }

    public void SetZoom(float zoom)
    {
        targetZoom = Mathf.Clamp(zoom, minZoom, maxZoom);
    }

    public void ResetRotation()
    {
        transform.rotation = Quaternion.Euler(90f, 0f, 0f); // Straight down
    }

    void OnDrawGizmosSelected()
    {
        if (useBounds)
        {
            Gizmos.color = Color.yellow;
            Vector3 center = new Vector3((minBounds.x + maxBounds.x) / 2f, transform.position.y, (minBounds.y + maxBounds.y) / 2f);
            Vector3 size = new Vector3(maxBounds.x - minBounds.x, 1f, maxBounds.y - minBounds.y);
            Gizmos.DrawWireCube(center, size);
        }
    }
}
