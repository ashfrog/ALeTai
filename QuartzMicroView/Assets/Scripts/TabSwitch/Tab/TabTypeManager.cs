using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "TabSwitcher/TabTypeManager")]
public class TabTypeManager : ScriptableObject
{
    [Tooltip("Tab类型名称列表（类似enum，可随时增加/删除）")]
    public List<string> tabTypes = new List<string>();
}