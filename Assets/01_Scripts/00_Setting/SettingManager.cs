using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class SettingManager : MonoBehaviour
{
    public static SettingManager Instance { get; private set; }

    public SettingData CurrentData { get; private set; } = new SettingData();
    [Header("Input System")]
    public InputActionAsset inputActions;
    // 설정 변경을 하위 모듈에 알릴 이벤트들
    public event Action OnAudioSettingsChanged;
    public event Action OnVideoSettingsChanged;
    public event Action OnLocalizationChanged; 
    [SerializeField] private AudioMixer mainMixer;
    public GameObject settingUI;

    private string saveFilePath;

    // 인게임(챔버)에서 설정창을 닫았을 때 복구할 마우스 상태.
    // 로비에서는 항상 마우스가 보여야 하므로 씬별로 다르게 처리한다.
    private const string LobySceneName = "Loby";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            saveFilePath = Path.Combine(Application.persistentDataPath, "settings.json");
            LoadSettings();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    void OnEnable()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void Start()
    {
        // 최초 로드된 씬은 sceneLoaded 이벤트가 발생하기 전이므로 직접 처리
        RefreshForScene(SceneManager.GetActiveScene());
    }

    // 씬이 바뀔 때마다 settingUI 참조와 마우스 상태를 새 씬에 맞게 갱신
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshForScene(scene);
    }

    private void RefreshForScene(Scene scene)
    {
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            // 씬 내에 있는 오브젝트인지 확인 (에디터 에셋 제외)
            if (obj.scene.IsValid() && obj.name == "SETTING")
            {
                settingUI = obj;
            }
        }

        Time.timeScale = 1f;
        ApplyDefaultCursorState(scene.name);
    }

    // 설정창이 닫혀 있을 때의 기본 마우스 상태.
    // 로비: 항상 보이고 자유롭게 움직임 / 인게임: 잠금 + 숨김
    private void ApplyDefaultCursorState(string sceneName)
    {
        if (sceneName == LobySceneName)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    //private void Update()
    //{
    //    if (settingUI == null) return;

    //    if (Input.GetKeyDown(KeyCode.Escape))
    //    {
    //        if (settingUI.activeSelf)
    //        {
    //            // 설정창 닫기
    //            settingUI.SetActive(false);        // ✅ false로 닫음
    //            Time.timeScale = 1f;
    //            SaveSettings();

    //            // 커서 상태를 현재 씬에 맞게 복구
    //            ApplyDefaultCursorState(SceneManager.GetActiveScene().name);
    //        }
    //        else
    //        {
    //            // 설정창 열기
    //            settingUI.SetActive(true);         // ✅ true로 열음
    //            Time.timeScale = 0f;

    //            // 커서 보이게 + 화면 안에서 자유롭게 움직이게
    //            Cursor.lockState = CursorLockMode.None;
    //            Cursor.visible = true;
    //        }
    //    }
    //}
    private void Update()
    {
        if (settingUI == null) return;

        // ★ 추가: 설정창이 닫혀있는 인게임 상태인데 커서가 풀려있으면 강제로 다시 잠금
        if (!settingUI.activeSelf)
        {
            string currentScene = SceneManager.GetActiveScene().name;
            if (currentScene != LobySceneName)
            {
                if (Cursor.lockState != CursorLockMode.Locked || Cursor.visible)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (settingUI.activeSelf)
            {
                settingUI.SetActive(false);
                Time.timeScale = 1f;
                SaveSettings();
                ApplyDefaultCursorState(SceneManager.GetActiveScene().name);
            }
            else
            {
                settingUI.SetActive(true);
                Time.timeScale = 0f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }

    // JSON 파일로 저장
    public void SaveSettings()
    {
        try
        {
            string json = JsonUtility.ToJson(CurrentData, true);
            File.WriteAllText(saveFilePath, json);
            Debug.Log($"설정 저장 완료: {saveFilePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"설정 저장 실패: {e.Message}");
        }
    }

    // JSON 파일 로드
    public void LoadSettings()
    {
        Time.timeScale = 1;
        if (File.Exists(saveFilePath))
        {
            try
            {
                string json = File.ReadAllText(saveFilePath);
                CurrentData = JsonUtility.FromJson<SettingData>(json);
                Debug.Log("설정 로드 완료");
            }
            catch (Exception e)
            {
                Debug.LogError($"설정 로드 실패(기본값 사용): {e.Message}");
                CurrentData = new SettingData();
            }
            if (inputActions != null && !string.IsNullOrEmpty(CurrentData.keyBindingsJson))
            {
                inputActions.LoadBindingOverridesFromJson(CurrentData.keyBindingsJson);
            }
        }
        else
        {
            CurrentData = new SettingData(); // 파일 없으면 기본값
        }
    }

    // 데이터 갱신 후 하위 모듈에 전파하는 함수들
    public void UpdateAudioSettings(float master, float bgm, float sfx)
    {
        CurrentData.masterVolume = master;
        CurrentData.sfxVolume = sfx;
        CurrentData.bgmVolume = bgm;
        OnAudioSettingsChanged?.Invoke();// 각각의 오디오 믹서 파라미터 제어
        SetMixerVolume("Master_Volume", master);
        SetMixerVolume("SFX_Volume", sfx);
        SetMixerVolume("BGM_Volume", bgm);

        // (선택사항) 나중에 게임을 다시 켰을 때를 위해 값을 저장해둡니다.
        PlayerPrefs.SetFloat("Master_Vol_Key", master);
        PlayerPrefs.SetFloat("SFX_Vol_Key", sfx);
        PlayerPrefs.SetFloat("BGM_Vol_Key", bgm);
    }
    private void SetMixerVolume(string parameterName, float sliderValue)
    {
        if (mainMixer == null) return;

        // 슬라이더 값이 0일 때는 소리를 완전히 끄기 위해 -80dB로 설정
        // 그 외의 값은 로그 함수를 이용해 자연스러운 볼륨 곡선으로 변환
        float volume = sliderValue <= 0 ? -80f : Mathf.Log10(sliderValue) * 20f;

        mainMixer.SetFloat(parameterName, volume);
    }

    public void UpdateVideoSettings(int width, int height, int fullScreenIdx, int fps)
    {
        CurrentData.resolutionWidth = width;
        CurrentData.resolutionHeight = height;
        CurrentData.screenModeIndex = fullScreenIdx;
        CurrentData.fpsLimit = fps;
        OnVideoSettingsChanged?.Invoke();
    }

    public void UpdateLanguage(string langCode)
    {
        CurrentData.languageCode = langCode;
        OnLocalizationChanged?.Invoke();
    }
    public void UpdateKeyBindings(string overridesJson)
    {
        CurrentData.keyBindingsJson = overridesJson;
    }
}