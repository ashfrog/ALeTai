using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ToggleButtonWithImage : MonoBehaviour
{
    public Button button;
    public Image buttonImage;
    public Sprite onSprite;
    Color defaltTextColor;
    [SerializeField]
    Color TextColor = Color.white;
    public Sprite offSprite;
    public bool isOn = false;
    public bool isSingleSelection = false;

    TMP_Text text;

    private ToggleButtonGroup parentGroup;

    private void Start()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (buttonImage == null)
        {
            buttonImage = GetComponent<Image>();
        }

        if (text == null)
        {
            text = GetComponentInChildren<TMP_Text>();
            defaltTextColor = text.color;
        }

        if (button != null)
        {
            button.onClick.AddListener(ToggleState);
        }

        parentGroup = GetComponentInParent<ToggleButtonGroup>();

        UpdateButtonImage();
    }

    private void ToggleState()
    {
        if (parentGroup != null && isSingleSelection)
        {
            parentGroup.DeselectAll();
        }

        isOn = !isOn;
        UpdateButtonImage();

        if (parentGroup != null)
        {
            parentGroup.OnToggleStateChanged(this);
        }
    }

    public void UpdateButtonImage()
    {
        if (buttonImage != null)
        {
            buttonImage.sprite = isOn ? onSprite : offSprite;
            text.color = isOn ? TextColor : defaltTextColor;
        }
    }
}