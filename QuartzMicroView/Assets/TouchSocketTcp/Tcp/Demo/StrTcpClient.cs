using System;
using System.Collections;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TouchSocket.Core;
using TouchSocket.Sockets;
using UnityEngine;

public class StrTcpClient : MonoBehaviour
{
    private enum PanelType
    {
        视频,
        网页,
        时间
    }

    [SerializeField] private TabSwitcher tabSwitcher_time;
    [SerializeField] private FHTcpClient_VCRPlayer fHTcpClient_VCRPlayer;

    private readonly TcpClient m_tcpClient = new TcpClient();
    private readonly SemaphoreSlim connectSemaphore = new SemaphoreSlim(1, 1);
    private bool configReady;
    private bool exited;
    private string iplog;
    private Coroutine reconnectCoroutine;
    private float curt;
    private readonly float wt = 30;

    public Action<string> StrTcpClientReceive;
    public Action<ITcpSession> Connected;
    public Action DisConnected;
    public string ipHost = "127.0.0.1:4850";

    private async void OnEnable()
    {
        ipHost = Settings.ini.IPHost.DoorIPHost;
        if (string.IsNullOrEmpty(ipHost))
        {
            Debug.Log("DoorIPHost未配置开门联动IPHost");
            return;
        }

        exited = false;
        await InitConfigAsync(ipHost);
        await StartConnectAsync();
        reconnectCoroutine = StartCoroutine(LoopReconnect());
    }

    public async Task InitConfigAsync(string ipPort)
    {
        iplog = ipPort;
        var config = new TouchSocketConfig()
            .SetRemoteIPHost(new IPHost(ipPort))
            .ConfigureContainer(a => a.AddLogger(group =>
            {
                group.AddLogger(new EasyLogger(logmsg));
                group.AddLogger(new FileLogger());
            }))
            .ConfigurePlugins(plugins =>
            {
                plugins.AddTcpConnectedPlugin(async (client, e) =>
                {
                    Loom.QueueOnMainThread(() =>
                    {
                        Connected?.Invoke(client);
                        Debug.Log($"{client.IP}:{client.Port}成功连接");
                    });
                    await e.InvokeNext();
                });
                plugins.AddTcpClosedPlugin(async (client, e) =>
                {
                    Loom.QueueOnMainThread(() =>
                    {
                        Debug.Log($"断开连接，信息：{e.Message}");
                        DisConnected?.Invoke();
                    });
                    await e.InvokeNext();
                });
                plugins.AddTcpReceivedPlugin(async (client, e) =>
                {
                    byte[] data = e.Memory.ToArray();
                    string hexString = BitConverter.ToString(data).Replace("-", string.Empty);
                    string message = Encoding.UTF8.GetString(data);
                    Loom.QueueOnMainThread(() => HandleReceived(hexString, message));
                    await e.InvokeNext();
                });
            });

        await m_tcpClient.SetupAsync(config);
        configReady = true;
    }

    public async Task<bool> StartConnectAsync()
    {
        if (!configReady || m_tcpClient.Online)
        {
            return m_tcpClient.Online;
        }

        try
        {
            await m_tcpClient.ConnectAsync(CancellationToken.None);
            return m_tcpClient.Online;
        }
        catch (Exception ex)
        {
            logmsg($"连接失败: {iplog} {ex.Message}");
            return false;
        }
    }

    private IEnumerator LoopReconnect()
    {
        while (!exited)
        {
            if (configReady && !m_tcpClient.Online)
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
            if (!exited && !m_tcpClient.Online)
            {
                await StartConnectAsync();
            }
        }
        finally
        {
            connectSemaphore.Release();
        }
    }

    private void HandleReceived(string hexString, string message)
    {
    }

    public async void Send(string message)
    {
        await SendAsync(Encoding.UTF8.GetBytes(message));
    }

    public async void Send(byte[] data)
    {
        await SendAsync(data);
    }

    private async Task SendAsync(byte[] data)
    {
        try
        {
            await m_tcpClient.SendAsync(new ReadOnlyMemory<byte>(data), CancellationToken.None);
        }
        catch (Exception ex)
        {
            logmsg(ex.Message);
        }
    }

    public async Task CloseAsync()
    {
        if (m_tcpClient.Online)
        {
            await m_tcpClient.CloseAsync("客户端关闭", CancellationToken.None);
        }
    }

    public async void Close()
    {
        await CloseAsync();
    }

    private void OnDisable()
    {
        exited = true;
        if (reconnectCoroutine != null)
        {
            StopCoroutine(reconnectCoroutine);
            reconnectCoroutine = null;
        }
        Close();
    }

    public void logmsg(string msg)
    {
        Debug.Log(msg);
    }
}
