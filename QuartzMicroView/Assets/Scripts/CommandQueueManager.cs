using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 命令队列管理器，处理所有设备指令的全局队列
/// </summary>
public class CommandQueueManager : MonoBehaviour
{
    // 单例实例
    private static CommandQueueManager _instance;
    public static CommandQueueManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // 尝试查找现有实例
                _instance = FindObjectOfType<CommandQueueManager>();

                // 如果没有找到，则创建一个新的游戏对象并添加组件
                if (_instance == null)
                {
                    GameObject obj = new GameObject("CommandQueueManager");
                    _instance = obj.AddComponent<CommandQueueManager>();
                    // 确保在场景切换时不销毁
                    DontDestroyOnLoad(obj);
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// 指令数据结构
    /// </summary>
    public struct CommandData
    {
        public FHClientController controller;
        public DataTypeEnum deviceID;
        public OrderTypeEnum orderType;
        public string command;
        public bool isHex;
        public bool appendCRC16;
        public float messageInterval;
    }

    /// <summary>
    /// 全局队列，用于存储所有需要发送的指令
    /// </summary>
    private Queue<CommandData> _commandQueue = new Queue<CommandData>();

    /// <summary>
    /// 指示是否正在处理全局队列
    /// </summary>
    private bool _isProcessingQueue = false;

    private void OnEnable()
    {
        _isProcessingQueue = false;
    }

    /// <summary>
    /// 将命令添加到全局队列
    /// </summary>
    /// <param name="cmdData">命令数据</param>
    public void EnqueueCommand(CommandData cmdData)
    {
        _commandQueue.Enqueue(cmdData);

        // 启动队列处理（如果尚未启动）
        if (!_isProcessingQueue)
        {
            StartCoroutine(ProcessCommandQueue());
        }
    }

    /// <summary>
    /// 添加多个命令到队列
    /// </summary>
    /// <param name="cmdDataList">命令数据列表</param>
    public void EnqueueCommands(IEnumerable<CommandData> cmdDataList)
    {
        foreach (var cmdData in cmdDataList)
        {
            _commandQueue.Enqueue(cmdData);
        }

        // 启动队列处理（如果尚未启动）
        if (!_isProcessingQueue)
        {
            StartCoroutine(ProcessCommandQueue());
        }
    }

    /// <summary>
    /// 发送全局指令队列
    /// </summary>
    private IEnumerator ProcessCommandQueue()
    {
        _isProcessingQueue = true;

        while (_commandQueue.Count > 0)
        {
            // 获取队列中的下一个指令
            CommandData cmdData = _commandQueue.Dequeue();

            // 发送指令
            if (cmdData.isHex)
            {
                string finalCmd = cmdData.appendCRC16 ? CRC.GetCRCHexString(cmdData.command) : cmdData.command;
                cmdData.controller.SendHex(cmdData.deviceID, cmdData.orderType, finalCmd);
            }
            else
            {
                cmdData.controller.SendStr(cmdData.deviceID, cmdData.orderType, cmdData.command);
            }

            // 添加延迟，确保指令之间有间隔
            yield return new WaitForSeconds(cmdData.messageInterval);
        }

        _isProcessingQueue = false;
    }

    /// <summary>
    /// 清空命令队列
    /// </summary>
    public void ClearQueue()
    {
        _commandQueue.Clear();
    }

    /// <summary>
    /// 返回当前队列中的命令数量
    /// </summary>
    public int QueueCount
    {
        get { return _commandQueue.Count; }
    }
}