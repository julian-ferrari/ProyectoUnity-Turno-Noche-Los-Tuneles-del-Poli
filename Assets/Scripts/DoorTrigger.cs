using UnityEngine;

public class DoorTrigger : MonoBehaviour
{
    public Door parentDoor;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && parentDoor != null)
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                parentDoor.OnPlayerEnter(player);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && parentDoor != null)
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                parentDoor.OnPlayerExit(player);
            }
        }
    }
}