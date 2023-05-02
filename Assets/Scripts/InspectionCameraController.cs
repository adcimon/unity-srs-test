using UnityEngine;

[RequireComponent(typeof(Camera))]
public class InspectionCameraController : MonoBehaviour
{
    [Header("Pivot")]
    public float pivotDistance = 10;
    public Transform pivot { get; private set; }

    [Header("Orbit")]
    public bool orbit = true;
    public string orbitAxisX = "Mouse X";
    public string orbitAxisY = "Mouse Y";
    public bool invertX = false;
    public bool invertY = false;
    public bool useKey = true;
    public KeyCode orbitKey = KeyCode.Mouse0;
    public float orbitSensitivity = 4;
    public float orbitDampening = 10;

    [Header("Zoom")]
    public bool zoom = true;
    public string zoomAxis = "Mouse ScrollWheel";
    public float zoomSensitivity = 2;
    public float zoomDampening = 6;
    public Vector2 zoomRange = new Vector2(1.5f, 100f);

    [Header("Pan")]
    public bool pan = true;
    public KeyCode panKey = KeyCode.Mouse1;
    public float panSensitivity = 20;

    [Header("Movement")]
    public bool movement = true;
    public KeyCode forwardKey = KeyCode.UpArrow;
    public KeyCode backwardKey = KeyCode.DownArrow;
    public KeyCode rightKey = KeyCode.RightArrow;
    public KeyCode leftKey = KeyCode.LeftArrow;
    public float movementSpeed = 20;

    [Space(10)]
    public KeyCode disableKey = KeyCode.LeftControl;

    private Vector2 inputRotation = Vector2.zero;
    private Vector3 lastPanPosition = Vector3.zero;
    private float cameraDistance;

    private bool controllerDisabled = false;

    private void Start()
    {
        // Instantiate the pivot.
        pivot = new GameObject("CameraPivot").transform;
        pivot.localRotation = transform.localRotation;
        pivot.transform.position = transform.position + pivotDistance * transform.forward;

        // Calculate the initial rotation.
        inputRotation = new Vector3(transform.localRotation.eulerAngles.y, -transform.localRotation.eulerAngles.x, 0);

        // Set the pivot's parent.
        if( transform.parent != null )
        {
            pivot.SetParent(transform.parent);
        }

        // Set the camera's parent.
        transform.SetParent(pivot);

        // Calculate the distance from camera to pivot.
        cameraDistance = Mathf.Abs((pivot.position - transform.position).magnitude);
    }

    private void LateUpdate()
    {
        if( Input.GetKeyDown(disableKey) )
        {
            controllerDisabled = !controllerDisabled;
        }

        if( controllerDisabled )
        {
            return;
        }

        if( orbit )
        {
            Orbit();
        }

        if( zoom )
        {
            Zoom();
        }

        if( pan )
        {
            Pan();
        }

        if( movement )
        {
            Movement();
        }
    }

    /// <summary>
    /// Orbit based on the mouse axes.
    /// </summary>
    private void Orbit()
    {
        if( !(!Input.GetKey(orbitKey) && useKey) && (Input.GetAxis(orbitAxisX) != 0 || Input.GetAxis(orbitAxisY) != 0) )
        {
            // Calculate the rotation amount.
            inputRotation.x += (invertX ? -1 : 1) * Input.GetAxis(orbitAxisX) * orbitSensitivity;
            inputRotation.y += (invertY ? -1 : 1) * Input.GetAxis(orbitAxisY) * orbitSensitivity;
        }

        // Perform the orbiting.
        Quaternion rotation = Quaternion.Euler(-inputRotation.y, inputRotation.x, 0);
        pivot.rotation = Quaternion.Lerp(pivot.rotation, rotation, Time.deltaTime * orbitDampening);
    }

    /// <summary>
    /// Zoom based on the mouse scroll wheel axis.
    /// </summary>
    private void Zoom()
    {
        if( Input.GetAxis(zoomAxis) != 0 )
        {
            // Calculate the movement amount.
            float zoomAmount = Input.GetAxis(zoomAxis) * zoomSensitivity;
            zoomAmount *= (cameraDistance * 0.3f);
            cameraDistance += -zoomAmount;
            cameraDistance = Mathf.Clamp(cameraDistance, zoomRange.x, zoomRange.y);
        }

        if( transform.localPosition.z != -cameraDistance )
        {
            // Perform the zooming.
            transform.localPosition = new Vector3(0, 0, Mathf.Lerp(transform.localPosition.z, -cameraDistance, Time.deltaTime * zoomDampening));
        }
    }

    /// <summary>
    /// Pan based on the mouse position.
    /// </summary>
    private void Pan()
    {
        if( Input.GetKeyDown(panKey) )
        {
            // Update the position.
            lastPanPosition = Input.mousePosition;
        }

        if( Input.GetKey(panKey) )
        {
            Vector3 newPanPosition = Input.mousePosition;

            // Calculate the movement amount.
            Vector3 delta = Camera.main.ScreenToViewportPoint(newPanPosition - lastPanPosition);
            Vector3 translation = new Vector3(delta.x, delta.y, 0);
            translation *= panSensitivity + panSensitivity * (cameraDistance / zoomRange.y);

            // Perform the panning.
            pivot.Translate(-translation, Space.Self);

            // Update the position.
            lastPanPosition = Input.mousePosition;
        }
    }

    /// <summary>
    /// Movement on the plane XZ based on the direction keys.
    /// </summary>
    private void Movement()
    {
        if( Input.GetKey(forwardKey) || Input.GetKey(backwardKey) || Input.GetKey(rightKey) || Input.GetKey(leftKey) )
        {
            Vector3 translation = Vector3.zero;

            // Movement on the X axis.
            if( Input.GetKey(rightKey) || Input.GetKey(leftKey) )
            {
                // Calculate the movement amount.
                float direction = (Input.GetKey(rightKey)) ? 1 : -1;
                translation = new Vector3(direction * (movementSpeed + movementSpeed * (cameraDistance / zoomRange.y)) * Time.deltaTime, 0, 0);

                // Perform the movement.
                pivot.Translate(translation, Space.Self);
            }

            // Movement on the Z axis.
            if( Input.GetKey(forwardKey) || Input.GetKey(backwardKey) )
            {
                // Calculate the movement amount.
                float direction = (Input.GetKey(forwardKey)) ? 1 : -1;
                float angle = pivot.eulerAngles.y * Mathf.Deg2Rad;
                translation.x = Mathf.Sin(angle) * (direction * (movementSpeed + movementSpeed * (cameraDistance / zoomRange.y)) * Time.deltaTime);
                translation.z = Mathf.Cos(angle) * (direction * (movementSpeed + movementSpeed * (cameraDistance / zoomRange.y)) * Time.deltaTime);

                // Perform the movement.
                pivot.position += translation;
            }
        }
    }
}