using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ClientItemsControl : MonoBehaviour
{
    [SerializeField]
    public FHClientController fhClientController;
    /// <summary>
    /// 设备IPNO 就是ip的最后一个值 如果重复比如串口服务器某一路就是ip的最后一个值加端口号 
    /// </summary>
    [SerializeField]
    public DataTypeEnum deviceIPNO;
    [SerializeField]
    public OrderTypeEnum orderType = OrderTypeEnum.Str;
    [SerializeField]
    Button btnOn;
    [SerializeField]
    Button btnOff;
    /// <summary>
    /// On指令
    /// </summary>
    [SerializeField]
    public List<string> onCmd;
    /// <summary>
    /// Off指令
    /// </summary>
    [SerializeField]
    public List<string> offCmd;
    /// <summary>
    /// 发送的CMD是16进制字符
    /// </summary>
    [SerializeField]
    public bool isHexCmd = false;
    /// <summary>
    /// 补全CRC16
    /// </summary>
    [SerializeField]
    public bool appendCRC16;

    /// <summary>
    /// 消息发送间隔时间(秒)
    /// </summary>
    [SerializeField]
    float messageInterval = 0.1f;

    [SerializeField, Min(1), InspectorName("发送次数"), Tooltip("每条 On/Off 指令的发送次数")]
    int sendCount = 1;

    /// <summary>
    /// 绑定执行ON/Off
    /// </summary>
    [SerializeField]
    List<GameObject> BindControls;

    [SerializeField]
    bool showConfirmDialog;

    // Start is called before the first frame update
    void Start()
    {
        fhClientController = FindObjectOfType<FHClientController>();

    }

    public void On()
    {
        // 执行绑定的控件
        StartCoroutine(ExecuteBindsWithInterval(true));
    }

    public void Off()
    {
        // 执行绑定的控件
        StartCoroutine(ExecuteBindsWithInterval(false));
    }

    /// <summary>
    /// 用于处理DeviceIPNO等不同的指令
    /// </summary>
    /// <param name="on">是否为开启操作</param>
    private IEnumerator ExecuteBindsWithInterval(bool on)
    {
        EnqueueConfiguredCommands(on);

        List<ClientItemsControl> boundControls = new List<ClientItemsControl>();
        CollectBoundControls(boundControls, new HashSet<ClientItemsControl> { this });
        foreach (ClientItemsControl control in boundControls)
        {
            if (control.fhClientController == null)
            {
                control.fhClientController = fhClientController;
            }

            control.EnqueueConfiguredCommands(on);
        }

        yield return null;
    }

    private void CollectBoundControls(List<ClientItemsControl> controls, HashSet<ClientItemsControl> visited)
    {
        if (BindControls == null)
        {
            return;
        }

        foreach (GameObject bindControlObj in BindControls)
        {
            if (bindControlObj == null)
            {
                continue;
            }

            foreach (ClientItemsControl control in bindControlObj.GetComponentsInChildren<ClientItemsControl>(true))
            {
                if (control == null || !visited.Add(control))
                {
                    continue;
                }

                controls.Add(control);
                control.CollectBoundControls(controls, visited);
            }
        }
    }

    private void EnqueueConfiguredCommands(bool on)
    {
        List<string> commands = on ? onCmd : offCmd;
        if (commands == null)
        {
            return;
        }

        foreach (string command in commands)
        {
            AddCommandToQueue(command);
        }
    }

    /// <summary>
    /// 添加单个指令到全局队列
    /// </summary>
    public void AddCommandToQueue(string cmd)
    {
        int repeatCount = Mathf.Max(1, sendCount);
        for (int i = 0; i < repeatCount; i++)
        {
            CommandQueueManager.CommandData cmdData = new CommandQueueManager.CommandData
            {
                controller = fhClientController,
                deviceID = deviceIPNO,
                orderType = orderType,
                command = cmd,
                isHex = isHexCmd,
                appendCRC16 = appendCRC16,
                messageInterval = messageInterval
            };

            CommandQueueManager.Instance.EnqueueCommand(cmdData);
        }
    }
}
