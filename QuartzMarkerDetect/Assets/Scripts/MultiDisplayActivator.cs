using UnityEngine;

/// <summary>在独立程序启动时激活扩展屏，使 targetDisplay=1 的 Canvas 输出到 Display 2。</summary>
[DefaultExecutionOrder(-1000)]
public sealed class MultiDisplayActivator : MonoBehaviour
{
    [SerializeField] private bool activateAllConnectedDisplays = true;

    private void Start()
    {
        if (!activateAllConnectedDisplays) return;

        for (int i = 1; i < Display.displays.Length; i++)
            Display.displays[i].Activate();

        Debug.Log($"已激活 {Display.displays.Length} 个 Unity Display。Display 1/2 Canvas 将分别输出到两块扩展屏。");
    }
}
