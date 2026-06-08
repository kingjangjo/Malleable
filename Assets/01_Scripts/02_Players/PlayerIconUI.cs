using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerIconUI : MonoBehaviour
{
    public Image icon;
    public Image iconBack;
    public TextMeshProUGUI percentText;
    public Sprite humanoid;
    public Sprite soul;

    public PlayerFormController pfc;
    private void Update()
    {
        if(pfc.currentForm == PlayerForm.Soul)
        {
            icon.sprite = soul;
            iconBack.sprite = soul;
            icon.gameObject.GetComponent<RectTransform>().localScale = new Vector3(1f, 1f, 1.0f);
            iconBack.gameObject.GetComponent<RectTransform>().localScale = new Vector3(1f, 1f, 1.0f);
            icon.fillAmount = 1.0f;
            percentText.text = "Soul";
        }
        else
        {
            icon.sprite = humanoid;
            iconBack.sprite = humanoid;
            icon.gameObject.GetComponent<RectTransform>().localScale = new Vector3(1.5f, 1.5f, 1.5f);
            iconBack.gameObject.GetComponent<RectTransform>().localScale = new Vector3(1.5f, 1.5f, 1.5f);
            icon.fillAmount = pfc.sizeIndex/ (float)500.0f;
            percentText.text = "";
        }
    }
}