using System;
using UnityEngine;
using UnityEngine.UI;

public class TabSwitcher : MonoBehaviour
{
    // 定义一个Button数组来存储所有的Tab按钮
    public Button[] tabButtons;

    [Tooltip("按Tab索引对应的ToggleButtonWithImage，可手动指定非子节点按钮")]
    public ToggleButtonWithImage[] toggleButtons;

    // 当前选中的Tab索引
    public int currentTabIndex = 0;
    // 定义一个GameObject数组来存储所有的Tab页面
    public GameObject[] tabPages;

    /// <summary>
    /// 是否在开始时初始化Tab页面Enable状态 让Action事件在Enable中注册
    /// </summary>
    public bool initTabPages;

    /// <summary>
    /// 在按钮完成页面切换后通知外部业务控制器。
    /// </summary>
    public event Action<int> TabButtonClicked;



    private void Start()
    {
        EnsureToggleButtonsInitialized();
        // 为每个Tab按钮添加点击事件
        for (int i = 0; i < tabButtons.Length; i++)
        {
            int index = i; // 缓存索引
            tabButtons[i].onClick.AddListener(() =>
            {
                SwitchTab(index);
                TabButtonClicked?.Invoke(index);
            });
        }
        if (initTabPages)
        {
            // Enable所有Tab页面
            InitTabPages();
        }
        // 初始化Tab页面显示状态
        UpdateTabPages();
        UpdateToggleButtons();
    }

    // 当Tab按钮被点击时调用
    public void SwitchTab(int index)
    {
        currentTabIndex = index;
        UpdateTabPages();
        UpdateToggleButtons();
    }

    private void EnsureToggleButtonsInitialized()
    {
        if (tabButtons == null || toggleButtons == null || toggleButtons.Length != tabButtons.Length)
        {
            if (tabButtons == null)
            {
                return;
            }

            ToggleButtonWithImage[] configuredButtons = toggleButtons;
            toggleButtons = new ToggleButtonWithImage[tabButtons.Length];
            if (configuredButtons != null)
            {
                System.Array.Copy(configuredButtons, toggleButtons, Mathf.Min(configuredButtons.Length, toggleButtons.Length));
            }
        }

        for (int i = 0; i < toggleButtons.Length && i < tabButtons.Length; i++)
        {
            if (toggleButtons[i] == null && tabButtons[i] != null)
            {
                toggleButtons[i] = tabButtons[i].GetComponent<ToggleButtonWithImage>();
            }

            if (toggleButtons[i] != null)
            {
                toggleButtons[i].SetStateControlledExternally(true);
            }
        }
    }

    private void UpdateToggleButtons()
    {
        EnsureToggleButtonsInitialized();
        if (toggleButtons == null)
        {
            return;
        }

        for (int i = 0; i < toggleButtons.Length; i++)
        {
            if (toggleButtons[i] != null)
            {
                toggleButtons[i].SetIsOn(i == currentTabIndex);
            }
        }
    }

    // 更新Tab页面的显示状态
    private void UpdateTabPages()
    {
        GameObject selectedPage = currentTabIndex >= 0 && currentTabIndex < tabPages.Length
            ? tabPages[currentTabIndex]
            : null;

        for (int i = 0; i < tabPages.Length; i++)
        {
            if (tabPages[i] != null)
            {
                // 多个Tab可以共享同一个页面对象；按引用判断可避免后面的重复项将其再次隐藏。
                tabPages[i].SetActive(tabPages[i] == selectedPage);
            }
        }
    }

    private void InitTabPages()
    {
        for (int i = 0; i < tabPages.Length; i++)
        {
            tabPages[i].SetActive(true);
        }
    }
}
