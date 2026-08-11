using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TouchSocket.Sockets;
using UnityEngine;

public class FHClientController : MonoBehaviour
{
    public static FHClientController ins;

    public FHTcpClient fhTcpClient;
    public Action<DTOInfo> receiveData;
    public string ipHost = "127.0.0.1:4849";
    public const string IPHOST_Key = "IPHOST";
    public Action<ITcpSession> Connected;
    public Action DisConnected;
    public bool userDisconnect;

    [SerializeField] private GameObject offLineStatue;

    private readonly SemaphoreSlim connectSemaphore = new SemaphoreSlim(1, 1);
    private bool exit;
    private bool configReady;
    private Coroutine reconnectCoroutine;
    private readonly HashSet<int> receiveDataTypes = new HashSet<int>();

    private void Awake()
    {
        if (ins == null)
        {
            ins = this;
        }

        if (fhTcpClient == null)
        {
            fhTcpClient = new FHTcpClient();
        }
    }

    private async void Start()
    {
        ipHost = Settings.ini.IPHost.ServerIPHost;

        fhTcpClient.FHTcpClientReceive = ReceiveData;
        fhTcpClient.Connected += client =>
        {
            Debug.Log($"FHTcp {client.IP}:{client.Port} 成功连接");
            SendRegisteredDataTypes();
            Connected?.Invoke(client);
            if (offLineStatue != null)
            {
                offLineStatue.SetActive(false);
            }
        };
        fhTcpClient.DisConnected += () =>
        {
            Debug.Log("FHTcp 断开连接");
            DisConnected?.Invoke();
            if (offLineStatue != null)
            {
                offLineStatue.SetActive(true);
            }
        };

        await ConfigureAsync(ipHost);
    }

    private IEnumerator LoopReconnect()
    {
        while (!exit)
        {
            if (!userDisconnect && configReady && fhTcpClient != null && !fhTcpClient.IsOnline())
            {
                TryReconnect();
            }

            yield return new WaitForSeconds(1f);
        }
    }

    private async void TryReconnect()
    {
        if (!await connectSemaphore.WaitAsync(0))
        {
            return;
        }

        try
        {
            if (!exit && !userDisconnect && !fhTcpClient.IsOnline())
            {
                await fhTcpClient.StartConnectAsync();
            }
        }
        finally
        {
            connectSemaphore.Release();
        }
    }

    private async Task ConfigureAsync(string newIpHost)
    {
        configReady = false;
        await fhTcpClient.CloseAsync();
        await fhTcpClient.InitConfigAsync(newIpHost);
        configReady = true;
    }

    private void ReceiveData(DTOInfo info)
    {
        receiveData?.Invoke(info);
    }

    public void Send<T>(DataTypeEnum dataType, OrderTypeEnum orderType, T data)
    {
        fhTcpClient.Send(dataType, orderType, data);
        if (orderType != OrderTypeEnum.GetPlayInfo)
        {
            Debug.Log(dataType + " " + orderType);
        }
    }

    public void SendStr(DataTypeEnum dataType, OrderTypeEnum orderType, string value)
    {
        fhTcpClient.SendStr(dataType, orderType, value);
        Debug.Log(dataType + " " + orderType + " " + value);
    }

    public void SendHex(DataTypeEnum dataType, OrderTypeEnum orderType, string value)
    {
        fhTcpClient.SendHexStr(dataType, orderType, value);
        Debug.Log(dataType + " " + orderType + " " + value);
    }

    public void RegisterReceiveDataType(DataTypeEnum dataType)
    {
        if (receiveDataTypes.Add((int)dataType) && fhTcpClient != null && fhTcpClient.IsOnline())
        {
            SendRegisteredDataTypes();
        }
    }

    private void SendRegisteredDataTypes()
    {
        if (fhTcpClient == null || !fhTcpClient.IsOnline() || receiveDataTypes.Count == 0)
        {
            return;
        }

        var dataTypes = new List<int>(receiveDataTypes);
        dataTypes.Sort();
        fhTcpClient.Send(DataTypeEnum.S_MainHost, OrderTypeEnum.RegisterDataTypes, dataTypes.ToArray());
    }

    private void OnEnable()
    {
        exit = false;
        reconnectCoroutine = StartCoroutine(LoopReconnect());
    }

    private void OnDisable()
    {
        exit = true;
        if (reconnectCoroutine != null)
        {
            StopCoroutine(reconnectCoroutine);
            reconnectCoroutine = null;
        }
        fhTcpClient?.Close();
    }

    public void DisConnect()
    {
        userDisconnect = true;
        fhTcpClient?.Close();
    }

    public async void Connect(string newIpHost)
    {
        ipHost = newIpHost;
        Settings.ini.IPHost.ServerIPHost = newIpHost;
        userDisconnect = false;
        await ConfigureAsync(newIpHost);
    }
}
