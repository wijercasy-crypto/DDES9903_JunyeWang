using UnityEngine;

public class DoorPromptTrigger : MonoBehaviour
{
    [SerializeField] private DoorPromptFade promptFade;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && promptFade != null)
        {
            promptFade.ShowPrompt();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && promptFade != null)
        {
            promptFade.HidePrompt();
        }
    }
}