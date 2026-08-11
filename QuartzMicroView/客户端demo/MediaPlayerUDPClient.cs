using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;

public class MediaPlayerUDPClient : MonoBehaviour
{
    public string serverIP = "127.0.0.1";
    public int serverPort = 4848;
    public int localPort = 8889;

    private UdpClient client;
    private IPEndPoint remoteEndPoint;
    private Thread receiveThread;
    private bool threadRunning = false;

    [Header("播放器")]
    [SerializeField]
    private Button btn_play;

    [SerializeField]
    private Button btn_pause;

    [SerializeField]
    private Button btn_stop;

    [SerializeField]
    private Button btn_playprev;

    [SerializeField]
    private Button btn_playnext;

    [SerializeField]
    private Scrollbar scrollbar;

    [SerializeField]
    private Text MediaFileName;

    [SerializeField]
    private Text curMediaTime;

    [SerializeField]
    private Text totalMediaTile;

    [SerializeField]
    private Slider Slider_Volumn;

    private bool setValueByMsgTrigger; //由接收消息触发Slider值改变
    private bool setScrollBarByMsgTrigger; //接收消息触发进度条改变

    [SerializeField]
    private TMPro.TMP_Dropdown dropdown;

    [SerializeField]
    private TMPro.TMP_Dropdown dropdown_LoopMode;

    [SerializeField]
    private Button btn_return;

    [SerializeField]
    private GameObject Panel_MediaPlayerControlPanel;

    public void ConnectMediaServer(string serverip)
    {
        this.serverIP = serverip;
        dropdown.ClearOptions();
        InitializeUDP();
    }

    private void Start()
    {
        btn_play.onClick.AddListener(() =>
        {
            SendMsg("PlayVideo");
        });
        btn_pause.onClick.AddListener(() =>
        {
            SendMsg("PauseVideo");
        });
        btn_stop.onClick.AddListener(() =>
        {
            SendMsg("StopVideo");
        });
        btn_playprev.onClick.AddListener(() =>
        {
            SendMsg("PlayPrevious");
        });
        btn_playnext.onClick.AddListener(() =>
        {
            SendMsg("PlayNext");
        });
        Slider_Volumn.onValueChanged.AddListener((e) =>
        {
            if (setValueByMsgTrigger)
            {
                setValueByMsgTrigger = false;
                return;
            }
            SendMsg($"SetVolumn|{e}");
        });
        scrollbar.onValueChanged.AddListener((e) =>
        {
            if (setScrollBarByMsgTrigger)
            {
                setScrollBarByMsgTrigger = false;
                return;
            }
            SendMsg($"VideoSeek|{e}");
        });
        btn_return.onClick.AddListener(() =>
        {
            StopUDP();
        });

        dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
        dropdown_LoopMode.onValueChanged.AddListener(OnDropdown_LoopModeValueChanged);
    }

    public void OnDropdownValueChanged(int value)
    {
        Debug.Log("Selected option: " + dropdown.options[value].text);
        SendMsg($"PlayVideo|{dropdown.options[value].text}");
    }

    private bool loopModeTrigger;
    private string loopmode;

    public void OnDropdown_LoopModeValueChanged(int value)
    {
        if (loopModeTrigger)
        {
            loopModeTrigger = false;//吸收从消息触发的事件
            return;
        }

        switch (value)
        {
            case 0:
                loopmode = "none";
                break;

            case 1:
                loopmode = "one";
                break;

            case 2:
                loopmode = "all";
                break;
        }

        SendMsg($"Loop|{loopmode}");
    }

    private void OnDisable()
    {
        StopUDP();
    }

    private void InitializeUDP()
    {
        remoteEndPoint = new IPEndPoint(IPAddress.Parse(serverIP), serverPort);
        client = new UdpClient(localPort);

        threadRunning = true;
        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();

        Debug.Log("UDP initialized.");

        StartCoroutine(StateRequest());
    }

    private IEnumerator StateRequest()
    {
        yield return new WaitForSeconds(0.2f);
        SendMsg("FileList");
        yield return new WaitForSeconds(0.2f);
        SendMsg("GetVolumn");
        yield return new WaitForSeconds(0.2f);
        SendMsg("GetLoop");
        while (threadRunning)
        {
            yield return new WaitForSeconds(0.5f);
            SendMsg("GetPlayInfo");
            yield return new WaitForSeconds(0.5f);
            //SendMsg("GetVolumn");
        }
    }

    private void StopUDP()
    {
        Panel_MediaPlayerControlPanel.SetActive(false);
        if (threadRunning)
        {
            threadRunning = false;
            receiveThread.Abort();
            client.Close();
            Debug.Log("UDP stopped.");
        }
    }

    private void Update()
    {
    }

    public void SendMsg(string message)
    {
        try
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            client.Send(data, data.Length, remoteEndPoint);
            Debug.Log("Sent: " + message);
        }
        catch (SocketException e)
        {
            Debug.LogError("Socket error: " + e.ToString());
        }
    }

    private void ReceiveData()
    {
        while (threadRunning)
        {
            try
            {
                IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = client.Receive(ref anyIP);
                string message = Encoding.UTF8.GetString(data);

                // 使用主线程处理接收到的消息
                Loom.QueueOnMainThread(() =>
                {
                    Debug.Log("Received: " + message);
                    ParseMsg(message);

                    // 在这里处理接收到的消息
                });
            }
            catch (SocketException e)
            {
                Debug.LogError("Socket error: " + e.ToString());
            }
        }
    }

    private void ParseMsg(string msg)
    {
        if (string.IsNullOrEmpty(msg))
        {
            return;
        }
        string command, data;
        GetCmdData(msg, out command, out data);
        switch (command.ToLower())
        {
            case "playinfo":
                Debug.Log("data   " + data);//90079.4,180074.7,0,0.m4v
                string[] parts = data.Split(new char[] { ',' }, 4);

                float.TryParse(parts[0], out float curms);
                float.TryParse(parts[1], out float totalms);
                MediaFileName.text = parts[3];
                curMediaTime.text = SecondsToTimeString(curms);
                totalMediaTile.text = SecondsToTimeString(totalms);
                if (totalms > 0)
                {
                    setScrollBarByMsgTrigger = true;
                    scrollbar.value = curms / totalms;
                }

                break;

            case "volumn":
                float.TryParse(data, out float volumn);
                setValueByMsgTrigger = true;
                Slider_Volumn.value = volumn;
                break;

            case "loop":
                switch (data)
                {
                    case "none":
                        dropdown_LoopMode.value = 0;
                        break;

                    case "one":
                        dropdown_LoopMode.value = 1;
                        break;

                    case "all":
                        dropdown_LoopMode.value = 2;
                        break;
                }
                break;

            case "filelist":
                string[] mediaFileNames = data.Split(new char[] { ',' });
                dropdown.ClearOptions();
                if (mediaFileNames != null && mediaFileNames.Length > 0)
                {
                    dropdown.AddOptions(mediaFileNames.ToList());
                }

                break;
        }
    }

    public static string SecondsToTimeString(double seconds)
    {
        TimeSpan timeSpan = TimeSpan.FromMilliseconds(seconds);
        return string.Format("{0:D2}:{1:D2}:{2:D2}",
            timeSpan.Hours,
            timeSpan.Minutes,
            timeSpan.Seconds);
    }

    private static void GetCmdData(string input, out string command, out string data)
    {
        string[] parts = input.Split(new char[] { '|' }, 2);
        command = parts[0].Trim().ToLower();
        data = parts.Length > 1 ? parts[1].Trim() : "";
    }

    //TCP UDP 4848 http(http://localhost/control?cmdstr=下面指令)

    //播放 PlayVideo

    //暂停 PauseVideo

    //停止并返回 StopVideo       如果设置了屏保的就显示屏保
    //播放上一个      PlayNext        (自动跳过屏保)
    //播放下一个      PlayPrevious    (自动跳过屏保)
    //音量加            SoundUp         (一次加0.1)
    // 音量减 SoundDown
    // 获取音量 GetVolumn       返回字符串示例 Volumn|0.5 (范围0-1)
    // 设置音量 SetVolumn|0.5
    // 索引播放视频 PlayVideo|*0
    // 文件名播放视频 PlayVideo|abc.mp4

    //播放单个文件后停止 Loop|none
    //列表视频循环播放   Loop|all
    //单个视频循环播放   Loop|one

    //视频列表读取     FileList 返回字符串示例 FileList|0.jpg,1.jpg,2.mp4

    //拖动进度条      VideoSeek|0.5      		(范围0-1)
    // 获取进度条等状态 GetPlayInfo             返回字符串示例 PlayInfo|12844.67,42960,1,2024-06-29 13-22-50.mkv 备注: PlayInfo|当前播放时长,视频总时长,视频在列表中下标,视频文件名

    //设置屏保           SetScreenSaver|abc.jpg
    //获取屏保           GetScreenSaver 返回字符串示例 ScreenSaver|abc.jpg

    //获取帮助           Help
}