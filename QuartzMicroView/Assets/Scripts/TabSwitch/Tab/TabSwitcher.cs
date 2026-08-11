using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using System.Linq;

[System.Serializable]
public class TabPageGroup
{
    [Tooltip("该Tab对应的所有页面")]
    public GameObject[] pages;

    [Tooltip("Tab类型名（下拉选择）")]
    public string tabType;
}

public class TabSwitcher : MonoBehaviour
{
    public Button[] tabButtons;
    public int currentTabIndex = -1;

    [Header("Tab类型名（类似enum，可增删）")]
    public List<string> allTabTypes = new List<string> { "key1", "key2" };

    [Header("每个Tab下挂载的页面组")]
    public List<TabPageGroup> tabPageGroups = new List<TabPageGroup>();

    public bool initTabPages;

    private void Start()
    {
        for (int i = 0; i < tabButtons.Length; i++)
        {
            int index = i;
            tabButtons[i].onClick.AddListener(() => SwitchTab(index));
        }
        if (initTabPages)
        {
            InitTabPages();
        }
        UpdateTabPages();
    }

    public void SwitchTab(int index)
    {
        currentTabIndex = index;
        UpdateTabPages();
    }

    public void SwitchTab(Enum label)
    {
        SwitchTab(label.ToString());
        Debug.Log("切换页面:" + label.ToString());
    }

    public void Hide()
    {
        InitTabPages(false);
    }

    /// <summary>
    /// 通过Tab类型名切换
    /// </summary>
    public void SwitchTab(string tabTypeName)
    {
        for (int i = 0; i < tabPageGroups.Count; i++)
        {
            if (tabPageGroups[i].tabType == tabTypeName)
            {
                currentTabIndex = i;
                UpdateTabPages();
                return;
            }
        }
        Debug.LogWarning("TabType " + tabTypeName + " not found in tabPageGroups.");
    }


    /// <summary>
    /// 获取当前Tab的名称
    /// </summary>
    /// <returns>当前Tab的类型名，如果索引无效则返回空字符串</returns>
    public string GetCurrentTabName()
    {
        if (currentTabIndex >= 0 && currentTabIndex < tabPageGroups.Count)
        {
            return tabPageGroups[currentTabIndex].tabType;
        }
        return string.Empty;
    }


    private void UpdateTabPages()
    {
        // 收集当前活动Tab的所有页面
        HashSet<GameObject> currentActivePages = new HashSet<GameObject>();
        if (currentTabIndex >= 0 && currentTabIndex < tabPageGroups.Count)
        {
            var currentGroup = tabPageGroups[currentTabIndex];
            if (currentGroup.pages != null)
            {
                foreach (var page in currentGroup.pages)
                {
                    if (page != null)
                        currentActivePages.Add(page);
                }
            }
        }

        // 收集所有需要处理的页面
        HashSet<GameObject> allPages = new HashSet<GameObject>();
        foreach (var group in tabPageGroups)
        {
            if (group.pages != null)
            {
                foreach (var page in group.pages)
                {
                    if (page != null)
                        allPages.Add(page);
                }
            }
        }

        // 更新页面状态
        foreach (var page in allPages)
        {
            // 如果页面在当前活动的Tab中，则显示
            // 否则隐藏
            bool shouldBeActive = currentActivePages.Contains(page);
            page.SetActive(shouldBeActive);
        }
    }

    private void InitTabPages(bool enable = true)
    {
        foreach (var group in tabPageGroups)
        {
            if (group.pages != null)
            {
                foreach (var page in group.pages)
                {
                    if (page != null)
                        page.SetActive(enable);
                }
            }
        }
    }

}