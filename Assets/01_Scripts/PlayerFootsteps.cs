using UnityEngine;

public class PlayerFootsteps : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] footstepClips; // 여기에 5개 사운드 등록

    // 애니메이션 이벤트(Animation Event) 또는 이동 로직에서 이 메서드를 호출
    public void PlayFootstepSound()
    {
        if (footstepClips == null || footstepClips.Length == 0) return;

        // 5개 중 랜덤으로 하나 선택
        int randomIndex = Random.Range(0, footstepClips.Length);
        AudioClip clip = footstepClips[randomIndex];

        // 발자국 소리가 겹치거나 끊기지 않게 PlayOneShot으로 재생
        audioSource.PlayOneShot(clip);
    }
}