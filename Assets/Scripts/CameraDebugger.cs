using UnityEngine;
using UnityEngine.InputSystem;

public class CameraDebugger : MonoBehaviour
{
    void Update()
    {
        // Mostrar información cada frame
        if (Input.GetKey(KeyCode.Tab)) // Mantén TAB para ver info
        {
            DebugCameraInfo();
        }

        // Resetear cámara con T
        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            ForceResetCamera();
        }

        // Test manual de movimiento con IJKL
        if (Input.GetKey(KeyCode.I)) Camera.main.transform.Translate(0, 0, 1 * Time.deltaTime);
        if (Input.GetKey(KeyCode.K)) Camera.main.transform.Translate(0, 0, -1 * Time.deltaTime);
        if (Input.GetKey(KeyCode.J)) Camera.main.transform.Translate(-1 * Time.deltaTime, 0, 0);
        if (Input.GetKey(KeyCode.L)) Camera.main.transform.Translate(1 * Time.deltaTime, 0, 0);
        if (Input.GetKey(KeyCode.U)) Camera.main.transform.Translate(0, 1 * Time.deltaTime, 0);
        if (Input.GetKey(KeyCode.O)) Camera.main.transform.Translate(0, -1 * Time.deltaTime, 0);
    }

    void DebugCameraInfo()
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            Debug.Log("=== CAMERA DEBUG ===");
            Debug.Log("Position: " + cam.transform.position);
            Debug.Log("Rotation: " + cam.transform.rotation);
            Debug.Log("Local Position: " + cam.transform.localPosition);
            Debug.Log("Local Rotation: " + cam.transform.localRotation);
            Debug.Log("Parent: " + (cam.transform.parent?.name ?? "null"));
            Debug.Log("Active: " + cam.gameObject.activeInHierarchy);
            Debug.Log("Enabled: " + cam.enabled);
            Debug.Log("====================");
        }

        // Info del player
        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            Debug.Log("Player Position: " + player.transform.position);
            Debug.Log("Player Rotation: " + player.transform.rotation);
        }
    }

    void ForceResetCamera()
    {
        Debug.Log("RESETEANDO CÁMARA FORZADAMENTE...");

        Camera cam = Camera.main;
        PlayerController player = FindAnyObjectByType<PlayerController>();

        if (cam != null && player != null)
        {
            // Resetear completamente
            cam.transform.parent = null;
            cam.transform.position = player.transform.position + Vector3.up * 2f;
            cam.transform.rotation = Quaternion.identity;
            cam.transform.localScale = Vector3.one;

            Debug.Log("Cámara reseteada a: " + cam.transform.position);
        }
    }

    void OnGUI()
    {
        GUI.Label(new Rect(10, Screen.height - 100, 300, 80),
            "CAMERA DEBUG:\n" +
            "TAB: Ver info en consola\n" +
            "T: Reset forzado de cámara\n" +
            "IJKL UO: Mover cámara manual");
    }
}