using System.Collections.Generic;
using UnityEngine;

public class ToggleButtonGroup : MonoBehaviour
{
    public bool allowMultipleSelection = false;
    public List<ToggleButtonWithImage> toggleButtons;

    private void Start()
    {
        if (toggleButtons == null)
        {
            toggleButtons = new List<ToggleButtonWithImage>(GetComponentsInChildren<ToggleButtonWithImage>());
        }

        foreach (var toggleButton in toggleButtons)
        {
            toggleButton.isSingleSelection = !allowMultipleSelection;
        }
    }

    public void DeselectAll()
    {
        foreach (var toggleButton in toggleButtons)
        {
            toggleButton.isOn = false;
            toggleButton.UpdateButtonImage();
        }
    }

    public void OnToggleStateChanged(ToggleButtonWithImage changedButton)
    {
        if (!allowMultipleSelection)
        {
            foreach (var toggleButton in toggleButtons)
            {
                if (toggleButton != changedButton)
                {
                    toggleButton.isOn = false;
                    toggleButton.UpdateButtonImage();
                }
            }
        }
    }
}