using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseLook : MonoBehaviour
{
    [Range(1f, 1000f)]
    public float mouseSensitivity = 100f;

    public Transform player;

    private float upDownRotation = 0f;

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        // TODO: look into using the new input system?
        var mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        var mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        upDownRotation += mouseY;
        upDownRotation = Mathf.Clamp(upDownRotation, -89f, 89f);

        // we apply the different axes to different objects: mouseY (left/right) actually rotates
        // the parent player object, while mouseY (up/down) just rotates the camera (this transform)

        // up/down rotation translates into rotation around the x axis.
        //
        // when the player moves the mouse up, we get a positive mouseY value.
        // apparently unity uses a "left-handed" coordinate system, so the x axis
        // points to the right and rotations go counterclockwise when viewed in the x axis direction.
        // this means a positive rotation around the x axis causes the camera to look down, and we 
        // need to negate this value:
        transform.localRotation = Quaternion.Euler(-upDownRotation, 0f, 0f);

        // left/right rotation is simpler: we just ask the character controller to rotate the player
        // around the y axis
        player.Rotate(Vector3.up * mouseX);
    }
}
