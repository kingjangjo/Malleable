using Cinemachine;
using UnityEngine;

public class CameraMouseOffset : MonoBehaviour
{
    public CinemachineVirtualCamera vCam;
    public float maxOffset = 3f;
    public float smoothSpeed = 5f;

    private CinemachineTransposer transposer;
    private Vector3 baseOffset;
    private Vector3 currentOffset;

    void Awake()
    {
        transposer = vCam.GetCinemachineComponent<CinemachineTransposer>();
        baseOffset = transposer.m_FollowOffset;
    }

    void Update()
    {
        //Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Vector2 mousePos = Input.mousePosition;
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Vector2 normalized = (mousePos - screenCenter) / screenCenter;

        Vector3 targetOffset = new Vector3(
            normalized.x * maxOffset,
            normalized.y * maxOffset,
            0
        );

        currentOffset = Vector3.Lerp(currentOffset, targetOffset, Time.deltaTime * smoothSpeed);
        transposer.m_FollowOffset = baseOffset + currentOffset;
    }
}
