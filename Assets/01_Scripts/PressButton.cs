using UnityEngine;

public class PressButton : MonoBehaviour
{
    public AudioSource audioSource;
    public void PlaySound()
    {
        audioSource.Play();
    }
}
