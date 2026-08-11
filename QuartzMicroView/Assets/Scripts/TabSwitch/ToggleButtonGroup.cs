using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 控制ToggleButtonWithImage的Group 只控制一层子物体
public class ToggleButtonGroup : MonoBehaviour
{
    /// <summary>
    /// 是否允许多选
    /// </summary>
    public bool allowMultipleSelection = false;

    /// <summary>
    /// 默认选中的索引
    /// </summary>
    public int defaultSelectedIndex = 0;

    private List<ToggleButtonWithImage> toggleButtons;

    /// <summary>
    /// 已通过函数设置过选中项
    /// </summary>
    private bool userSelected;

    /// <summary>
    /// 确保toggleButtons已初始化
    /// </summary>
    private void EnsureToggleButtonsInitialized()
    {
        if (toggleButtons == null || toggleButtons.Count == 0)
        {
            toggleButtons = new List<ToggleButtonWithImage>();
            foreach (var item in GetComponentsInChildren<ToggleButtonWithImage>())
            {
                if (item.transform.parent == transform)
                {
                    toggleButtons.Add(item);
                }
            }
        }
    }

    private void Start()
    {
        EnsureToggleButtonsInitialized();
        if (!userSelected)
        {
            if (toggleButtons.Count > 0 && defaultSelectedIndex >= 0 && defaultSelectedIndex < toggleButtons.Count)
            {
                toggleButtons[defaultSelectedIndex].SetIsOn(true);
            }
        }
    }
    /// <summary>
    /// 点击按钮触发
    /// </summary>
    /// <param name="changedButton"></param>
    public void OnToggleStateChanged(ToggleButtonWithImage changedButton)
    {
        EnsureToggleButtonsInitialized();
        if (!allowMultipleSelection)
        {
            foreach (var toggleButton in toggleButtons)
            {
                if (toggleButton != changedButton)
                {
                    // 这里要用SetIsOn而不是直接设置isOn
                    toggleButton.SetIsOn(false);
                }
            }
        }
    }

    public void SetSelectedByIndex(int index)
    {
        EnsureToggleButtonsInitialized();
        if (toggleButtons.Count > 0 && index >= 0 && index < toggleButtons.Count)
        {
            toggleButtons[index].SetIsOn(true);
            if (!allowMultipleSelection)
            {
                OnToggleStateChanged(toggleButtons[index]);
            }
        }
        userSelected = true;
    }

    public void SetSelectedByIndex(int index, float delay)
    {
        StartCoroutine(_DelayedSetSelectedByIndex(index, delay));
    }
    IEnumerator _DelayedSetSelectedByIndex(int index, float delay)
    {
        yield return new WaitForSeconds(delay);
        SetSelectedByIndex(index);
    }

}