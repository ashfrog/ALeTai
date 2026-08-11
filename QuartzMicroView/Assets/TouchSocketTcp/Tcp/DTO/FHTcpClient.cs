using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TouchSocket.Core;
using TouchSocket.Sockets;
using UnityEngine;

public class FHTcpClient
{
    public static FHTcpClient ins;

    private readonly TcpClient m_tcpClient = new TcpClient();
    private string iplog;
    private bool configReady;

    public Action<ITcpSession> Connected;
    public Action DisConnected;
    public Action<DTOInfo> FHTcpClientReceive;

    public FHTcpClient()
    {
        ins = this;
    }

    public bool IsOnline()
    {
        return m_tcpClient.Online;
    }

    public async Task InitConfigAsync(string ipPort)
    {
        iplog = ipPort;
        var config = new TouchSocketConfig()
            .SetRemoteIPHost(new IPHost(ipPort))
            .SetTcpDataHandlingAdapter(() => new MyFixedHeaderCustomDataHandlingAdapter())
            .ConfigurePlugins(plugins =>
            {
                plugins.AddTcpConnectedPlugin(async (client, e) =>
                {
                    if (m_tcpClient.Online && Connected != null)
                    {
                        Loom.QueueOnMainThread(() => Connected.Invoke(client));
                    }
                    await e.InvokeNext();
                });
                plugins.AddTcpClosedPlugin(async (client, e) =>
                {
                    if (DisConnected != null)
                    {
                        Loom.QueueOnMainThread(() => DisConnected.Invoke());
                    }
                    await e.InvokeNext();
                });
                plugins.AddTcpReceivedPlugin(async (client, e) =>
                {
                    DTOInfo info = e.RequestInfo as DTOInfo;
                    if (info != null && FHTcpClientReceive != null)
                    {
                        Loom.QueueOnMainThread(() =>
                        {
                            try
                            {
                                FHTcpClientReceive.Invoke(info);
                            }
                            catch (Exception ex)
                            {
                                logmsg(ex.Message);
                            }
                        });
                    }
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

    public async void Send(string message)
    {
        await SendBytesAsync(Encoding.UTF8.GetBytes(message));
    }

    public async void Send(byte[] data)
    {
        await SendBytesAsync(data);
    }

    public async void Send<T>(DataTypeEnum dataType, OrderTypeEnum orderType, T value)
    {
        byte[] body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value));
        await SendFrameAsync(dataType, orderType, body);
    }

    public async void SendStr(DataTypeEnum dataType, OrderTypeEnum orderType, string value)
    {
        await SendFrameAsync(dataType, orderType, Encoding.GetEncoding("gb2312").GetBytes(value));
    }

    public async void SendHexStr(DataTypeEnum dataType, OrderTypeEnum orderType, string value)
    {
        await SendFrameAsync(dataType, orderType, ConvertUtil.HexStrTobyte(value));
    }

    public async void SendBytes(DataTypeEnum dataType, OrderTypeEnum orderType, byte[] value)
    {
        await SendFrameAsync(dataType, orderType, value);
    }

    public async void SendASKII(DataTypeEnum dataType, OrderTypeEnum orderType, string value)
    {
        await SendFrameAsync(dataType, orderType, Encoding.ASCII.GetBytes(value));
    }

    public static byte[] PackInfo(DataTypeEnum dataType, OrderTypeEnum orderType, byte[] body)
    {
        byte[] data = new byte[body.Length + 12];
        Buffer.BlockCopy(BitConverter.GetBytes(body.Length + 12), 0, data, 0, 4);
        Buffer.BlockCopy(BitConverter.GetBytes((int)dataType), 0, data, 4, 4);
        Buffer.BlockCopy(BitConverter.GetBytes((int)orderType), 0, data, 8, 4);
        Buffer.BlockCopy(body, 0, data, 12, body.Length);
        return data;
    }

    private async Task SendFrameAsync(DataTypeEnum dataType, OrderTypeEnum orderType, byte[] body)
    {
        await SendBytesAsync(PackInfo(dataType, orderType, body));
    }

    private async Task SendBytesAsync(byte[] data)
    {
        try
        {
            await m_tcpClient.SendAsync(new ReadOnlyMemory<byte>(data), CancellationToken.None);
        }
        catch (Exception ex)
        {
            logmsg("发送失败: " + ex.Message);
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

    public void logmsg(string msg)
    {
        Debug.Log(msg);
    }
}
