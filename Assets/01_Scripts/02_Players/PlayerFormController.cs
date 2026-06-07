using System;
using Unity.Cinemachine;
using UnityEngine;


[Serializable]
public enum PlayerForm
{
    Humanoid,
    Soul
}
public class PlayerFormController : MonoBehaviour
{
    [Header("모델")]
    public GameObject humanoidForm;
    public GameObject soulForm;

    [Header("콜라이더")]
    public BoxCollider humanoidCollider;
    public SphereCollider soulCollider;

    [Header("카메라")]
    public GameObject soutTrackingTarget;
    public GameObject humanoidTrackingTarget;
    public CinemachineCamera cCam;

    public PlayerForm currentForm { get; private set; } = PlayerForm.Soul;

    private InputSystem_Actions _controls;

    public int sizeIndex = 0;

    void Awake() => _controls = new InputSystem_Actions();
    void OnEnable() => _controls.Enable();
    void OnDisable() => _controls.Disable();
    public SoulCore playerData;
    public PlayerParticleSystem pps;

    private void Start()
    {
        playerData = GetComponent<SoulCore>();
    }
    private void Update()
    {
        if (_controls.Player.FormChange.triggered)
        {
            FormChange();
            playerData.currentForm = this.currentForm;
        }
    }
    void FormChange()
    {
        var targetConfig = cCam.Target;
        if(currentForm == PlayerForm.Humanoid)
        {
            this.gameObject.GetComponent<Rigidbody>().mass = 1;
            currentForm = PlayerForm.Soul;
            humanoidForm.SetActive(false);
            soulForm.SetActive(true);
            humanoidCollider.enabled = false;
            soulCollider.enabled = true;
            pps.SetSoul(sizeIndex);
            sizeIndex = 0;
            targetConfig.TrackingTarget = soutTrackingTarget.transform;
            cCam.Target = targetConfig;
        }
        else
        {
            this.gameObject.GetComponent<Rigidbody>().mass = 4;
            currentForm = PlayerForm.Humanoid;
            humanoidForm.SetActive(true);
            soulForm.SetActive(false);
            humanoidCollider.enabled = true;
            soulCollider.enabled = false;
            sizeIndex += pps.SetHumanoid();
            if(sizeIndex > 100)
            {
                float size = (1 + sizeIndex) * 1.0f / 250.0f;
                humanoidForm.gameObject.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f) * size;
                //humanoidCollider.height = 1.7f * sizeIndex * 1.0f / 250.0f;
                humanoidCollider.size = new Vector3(1.0f, 1.0f, 1.0f) * size;
                this.transform.position = this.transform.position + new Vector3(0, size/2, 0);
            }
            targetConfig.TrackingTarget = humanoidTrackingTarget.transform;
            cCam.Target = targetConfig;
        }
    }
}
