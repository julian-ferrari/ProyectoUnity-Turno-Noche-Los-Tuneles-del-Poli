using UnityEngine;
using UnityEngine.InputSystem;

public class Jump : MonoBehaviour
{
    [SerializeField] LayerMask FloorLayer;
    float JumpHeightNewtons = 400f;
    float GroundRayLength = 0.2f;
    Vector3 GroundRayStart = new Vector3(0, 0.1f, 0);
    Rigidbody Body;

    private void Start()
    {
        Body = this.GetComponent<Rigidbody>();
        if (FloorLayer == 0)
        {
            Debug.LogWarning(string.Format("Floor layer not set on {0}", this.gameObject.name));
        }
    }

    bool IsGrounded()
    {
        Ray groundCheckRay = new Ray(this.transform.position + GroundRayStart, Vector3.down);
        //Debug.DrawRay(this.transform.position + GroundRayStart, Vector3.down, Color.red, 10f);
        return Physics.Raycast(groundCheckRay, GroundRayLength, FloorLayer);
    }

    public void OnJump(InputAction.CallbackContext Context)
    {
        if (Context.performed && IsGrounded())
        {
            Body.AddForce(Vector3.up * Body.mass * JumpHeightNewtons);
        }
    }

}
