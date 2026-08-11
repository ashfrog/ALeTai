using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.IO;

public class TCPUDPServer : MonoBehaviour
{
    public int tcpPort = 4848;
    public int udpPort = 4848;

    private TcpListener tcpListener;
    private UdpClient udpClient;
    private bool isRunning = false;

    const int BUFF_SIZE = 1024 * 1024 * 10;

    [SerializeField]
    LitVCR litVCR;

    void Start()
    {
        StartServer();
    }

    void OnDisable()
    {
        StopServer();
    }

    void StartServer()
    {
        isRunning = true;

        // 启动TCP服务器
        tcpListener = new TcpListener(IPAddress.Any, tcpPort);
        tcpListener.Start();
        Thread tcpThread = new Thread(new ThreadStart(TcpListenThread));
        tcpThread.Start();

        // 启动UDP服务器
        udpClient = new UdpClient(udpPort);
        Thread udpThread = new Thread(new ThreadStart(UdpListenThread));
        udpThread.Start();

        Debug.Log("Server started on TCP port " + tcpPort + " and UDP port " + udpPort);
    }

    void StopServer()
    {
        isRunning = false;
        if (tcpListener != null)
        {
            tcpListener.Stop();
        }
        if (udpClient != null)
        {
            udpClient.Close();
        }
    }

    void TcpListenThread()
    {
        while (isRunning)
        {
            try
            {
                TcpClient client = tcpListener.AcceptTcpClient();
                Thread clientThread = new Thread(new ParameterizedThreadStart(HandleTcpClient));
                clientThread.Start(client);
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message);
            }
        }
    }

    void HandleTcpClient(object obj)
    {
        TcpClient tcpClient = (TcpClient)obj;
        NetworkStream stream = tcpClient.GetStream();
        byte[] buffer = new byte[BUFF_SIZE];
        int bytesRead;

        while (isRunning)
        {
            bytesRead = 0;
            try
            {
                bytesRead = stream.Read(buffer, 0, buffer.Length);
            }
            catch
            {
                break;
            }

            if (bytesRead == 0)
                break;

            string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Loom.QueueOnMainThread(() => { ProcessCommand(message, stream, null); });
            //byte[] responseData = Encoding.ASCII.GetBytes(response);
            //stream.Write(responseData, 0, responseData.Length);
        }

        tcpClient.Close();
    }

    void UdpListenThread()
    {
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
        while (isRunning)
        {
            try
            {
                byte[] data = udpClient.Receive(ref remoteEP);
                string message = Encoding.UTF8.GetString(data);
                Loom.QueueOnMainThread(() => { ProcessCommand(message, null, remoteEP); });
                //byte[] responseData = Encoding.ASCII.GetBytes(response);
                //udpClient.Send(responseData, responseData.Length, remoteEP);
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message);
            }
        }
    }

    public String ProcessCommand(string input, NetworkStream stream, IPEndPoint remoteEP, HttpListenerContext httpListenerContext = null) //处理tcp udp http 控制指令消息
    {
        string command, data;
        GetCmdData(input, out command, out data);
        // 在这里处理不同的指令
        switch (command.ToLower())
        {
            case "playvideo":
                try
                {
                    if (!string.IsNullOrEmpty(data))
                    {
                        if (data.StartsWith('*') && int.TryParse(data.Substring(1), out int index)) //playvideo|*0  -> index=0
                        {
                            //播放指定视频
                            litVCR.OpenVideoByIndex(index);
                            SendData(stream, remoteEP, input, httpListenerContext);
                        }
                        else
                        {
                            litVCR.OpenVideoByFileName(data);//根据文件名播放指定视频
                            SendData(stream, remoteEP, input, httpListenerContext);
                        }
                    }
                    else
                    {
                        litVCR.OnPlayButton();
                        //播放视频
                        SendData(stream, remoteEP, input, httpListenerContext);
                    }
                }
                catch (Exception ex)
                {
                    string dto = $"PlayVideo|exception:{ex.Message}";
                    SendData(stream, remoteEP, dto, httpListenerContext);
                }
                break;
            case "pausevideo":
                litVCR.OnPauseButton();
                SendData(stream, remoteEP, input, httpListenerContext);
                break;
            case "setscreensaver"://设置屏保
                try
                {
                    if (!String.IsNullOrEmpty(data))
                    {
                        litVCR.SetScreenSaver(data);
                        SendData(stream, remoteEP, input, httpListenerContext);
                    }
                }
                catch (Exception ex)
                {
                    string dto = $"PlayVideo|exception:{ex.Message}";
                    SendData(stream, remoteEP, dto, httpListenerContext);
                }
                break;
            case "getscreensaver":
                string screensaver = $"ScreenSaver|{litVCR.GetScreenSaver()}";
                SendData(stream, remoteEP, screensaver, httpListenerContext);
                break;
            case "filelist":  //请求文件列表
                litVCR.ReloadFileList();
                string filelist = $"FileList|{litVCR.GetFileListStr()}";
                SendData(stream, remoteEP, filelist, httpListenerContext);
                break;
            case "videoseek":
                {
                    if (float.TryParse(data, out float value))
                    {
                        litVCR.OnVideoSeekSlider(value);
                        SendData(stream, remoteEP, input, httpListenerContext);
                    }
                }
                break;
            case "stopvideo":
                litVCR.Stop();
                litVCR.PlayScreenSaver();

                SendData(stream, remoteEP, input, httpListenerContext);
                break;
            case "playnext":
                litVCR.PlayNext();
                SendData(stream, remoteEP, input, httpListenerContext);
                break;
            case "playprevious":
                litVCR.PlayPrevious();
                SendData(stream, remoteEP, input, httpListenerContext);
                break;
            case "soundup":
                litVCR.VolumnUp();
                SendData(stream, remoteEP, input, httpListenerContext);
                break;
            case "sounddown":
                litVCR.VolumnDown();
                SendData(stream, remoteEP, input, httpListenerContext);
                break;
            case "getvolumn":
                {
                    string volumndto = $"Volumn|{litVCR.GetVolumn()}";
                    SendData(stream, remoteEP, volumndto, httpListenerContext);
                }
                break;
            case "setvolumn":
                {
                    float volumn = 1f;
                    float.TryParse(data, out volumn);
                    litVCR.SetVolumn(volumn);
                    SendData(stream, remoteEP, input, httpListenerContext);
                }
                break;
            case "loop":
                {
                    switch (data.ToLower())
                    {
                        case "none":
                            litVCR.SetLoopMode(LitVCR.LoopMode.none);
                            SendData(stream, remoteEP, input, httpListenerContext);
                            break;
                        case "one":
                            litVCR.SetLoopMode(LitVCR.LoopMode.one);
                            SendData(stream, remoteEP, input, httpListenerContext);
                            break;
                        case "all":
                            litVCR.SetLoopMode(LitVCR.LoopMode.all);
                            SendData(stream, remoteEP, input, httpListenerContext);
                            break;
                    }
                }
                break;
            case "getloop":
                {
                    string loopModedto = $"Loop|{litVCR.GetLoopMode()}";
                    SendData(stream, remoteEP, loopModedto, httpListenerContext);
                }
                break;
            case "getplayinfo":
                {
                    string dto = litVCR.GetPlayInfo();
                    SendData(stream, remoteEP, litVCR.GetPlayInfo(), httpListenerContext);
                }
                break;
            case "help":
                SendData(stream, remoteEP, HelpDocumentation.GetHelpDocument().ToString(), httpListenerContext);
                break;

            default:
                SendData(stream, remoteEP, "comand not exist", httpListenerContext);
                break;
        }
        return "success";
    }

    private void SendData(NetworkStream stream, IPEndPoint remoteEP, string sendstr, HttpListenerContext httpListenerContext = null)
    {
        if (stream != null) //tcp发送消息
        {
            byte[] responseData = Encoding.ASCII.GetBytes(sendstr);
            stream.Write(responseData, 0, responseData.Length);
        }
        if (remoteEP != null) //udp发送消息
        {
            byte[] responseData = Encoding.ASCII.GetBytes(sendstr);
            udpClient.Send(responseData, responseData.Length, remoteEP);
        }
        if (httpListenerContext != null) //改成通过函数返回值了
        {
            SimpleHttpServer.SendJsonResponse(httpListenerContext, 0, sendstr);
        }
    }

    private static void GetCmdData(string input, out string command, out string data)
    {
        string[] parts = input.Split(new char[] { '|' }, 2);
        command = parts[0].Trim().ToLower();
        data = parts.Length > 1 ? parts[1].Trim() : "";
    }

}