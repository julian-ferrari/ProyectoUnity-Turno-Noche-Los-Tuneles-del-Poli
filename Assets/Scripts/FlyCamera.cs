using UnityEngine;

public class FlyCamera : MonoBehaviour
{
    public float speed = 10f;
    public float sensitivity = 2f;

    void Update()
    {
        if (Input.GetMouseButton(1)) // botón derecho del mouse
        {
            float rotX = Input.GetAxis("Mouse X") * sensitivity;
            float rotY = -Input.GetAxis("Mouse Y") * sensitivity;
            transform.Rotate(Vector3.up * rotX, Space.World);
            transform.Rotate(Vector3.right * rotY, Space.Self);
        }

        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");
        float moveY = 0;

        if (Input.GetKey(KeyCode.E)) moveY += 1;
        if (Input.GetKey(KeyCode.Q)) moveY -= 1;

        Vector3 move = (transform.forward * moveZ + transform.right * moveX + transform.up * moveY) * speed * Time.deltaTime;
        transform.position += move;
    }
}
