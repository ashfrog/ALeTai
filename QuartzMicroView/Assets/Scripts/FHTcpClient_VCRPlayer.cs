using Newtonsoft.Json;
using RenderHeads.Media.AVProVideo;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;


public class FHTcpClient_VCRPlayer : MonoBehaviour
{
    //所有电脑设备都是只需要一个Tcp客户端 包括平板  服务端只有一台就是负责转发给所有的那台设备 这里模拟航线这台设备

    public FHClientController tcpClient;
    /// <summary>
    /// New configuration: the media device data type ID. The pad response
    /// data type is derived automatically.
    /// </summary>
    [SerializeField, Tooltip("设备唯一编号：1-1000，Pad和播放器必须填写相同编号。")]
    private int DeviceDataTypeID;
    private DataTypeEnum receiveDataTypeEnum;
    private DataTypeEnum sendDataTypeEnum;
    public LitVCR _vcr;
    float setVolumn = 1f;

    void Start()
    {
        Screen.fullScreen = true;  //设置成全屏

        setVolumn = PlayerPrefs.GetFloat("volumn", 0.5f);


        int deviceDataTypeID = Settings.ini.IPHost.DeviceDataTypeID;
        if (deviceDataTypeID <= 0)
        {
            deviceDataTypeID = DeviceDataTypeID;
        }
        if (!DeviceDataTypeBinding.IsValidDeviceDataTypeID(deviceDataTypeID))
        {
            Debug.LogError("DeviceDataTypeID必须在1-1000之间，播放器控制已禁用。");
            enabled = false;
            return;
        }
        receiveDataTypeEnum = (DataTypeEnum)DeviceDataTypeBinding.GetMediaDataTypeID(deviceDataTypeID);
        sendDataTypeEnum = (DataTypeEnum)DeviceDataTypeBinding.GetPadDataTypeID(deviceDataTypeID);
        tcpClient.RegisterReceiveDataType(receiveDataTypeEnum);



        tcpClient.receiveData += (info) =>
        {
            //所有设备都收到相同的数据 通过DataTypeEnum区分
            //Debug.Log("设备:" + (DataTypeEnum)info.DataType + " 指令:" + (OrderTypeEnum)info.OrderType);
            if (info.DataType == (int)receiveDataTypeEnum)
            {
                switch ((OrderTypeEnum)info.OrderType)//处理指令类型
                {
                    case OrderTypeEnum.GetFileList:                  //获取文件列表
                        string filesStr = _vcr.GetFileListStr();
                        filesStr = filesStr.Replace("," + Settings.ini.Graphics.ScreenSaver, "");
                        tcpClient.Send(sendDataTypeEnum, OrderTypeEnum.GetFileList, filesStr);
                        break;
                    case OrderTypeEnum.GetVolumn:                    //获取当前音量
                        float getVolumn = _vcr.GetVolumn();
                        tcpClient.Send(sendDataTypeEnum, OrderTypeEnum.GetVolumn, getVolumn);
                        break;
                    case OrderTypeEnum.GetPlayInfo:                   //获取当前播放进度
                        string playinfo = _vcr.GetPlayInfo();
                        tcpClient.Send(sendDataTypeEnum, OrderTypeEnum.GetPlayInfo, playinfo);
                        break;
                    case OrderTypeEnum.SetVolumn:                    //设置音量
                        setVolumn = JsonConvert.DeserializeObject<float>(Encoding.UTF8.GetString(info.Body));
                        PlayerPrefs.SetFloat("volumn", setVolumn);
                        _vcr.SetVolumn(setVolumn);
                        break;
                    case OrderTypeEnum.PauseMovie:
                        _vcr.OnPauseButton();          //暂停
                        break;
                    case OrderTypeEnum.PlayScreenSaver:
                        _vcr.PlayScreenSaver();
                        break;
                    case OrderTypeEnum.PlayMovie:
                        _vcr.OnPlayButton();           //播放
                        break;
                    case OrderTypeEnum.StopMovie:
                        _vcr.Stop();
                        _vcr.PlayScreenSaver();
                        break;
                    case OrderTypeEnum.SetMovSeek:                   //设置播放进度
                        float setSeek = JsonConvert.DeserializeObject<float>(Encoding.UTF8.GetString(info.Body));
                        _vcr.OnVideoSeekSlider(setSeek);
                        break;
                    case OrderTypeEnum.PlayPrev:                     //播放上一个视频                    
                        _vcr.PlayPrevious();
                        break;
                    case OrderTypeEnum.PlayNext:                     //播放下一个视频                    
                        _vcr.PlayNext();

                        break;
                    case OrderTypeEnum.SetPlayMovie:                 //指定播放某个视频
                        string videoPath = JsonConvert.DeserializeObject<string>(Encoding.UTF8.GetString(info.Body));
                        _vcr.OpenVideoByFileName(videoPath);
                        break;
                    case OrderTypeEnum.GetLoopMode:
                        {
                            string loopmode = _vcr.GetLoopMode();
                            tcpClient.Send(sendDataTypeEnum, OrderTypeEnum.GetLoopMode, loopmode);
                        }
                        break;
                    case OrderTypeEnum.LoopMode:
                        {
                            string loopModeBody = Encoding.UTF8.GetString(info.Body ?? new byte[0]);
                            if (TryParseLoopMode(loopModeBody, out LitVCR.LoopMode loopMode))
                            {
                                _vcr.SetLoopMode(loopMode);
                            }
                            else
                            {
                                Debug.LogError("循环模式消息无效: " + loopModeBody);
                            }
                        }
                        break;
                    case OrderTypeEnum.SetScreenSaver:
                        {
                            string screenSaver = JsonConvert.DeserializeObject<string>(Encoding.UTF8.GetString(info.Body));
                            _vcr.SetScreenSaver(screenSaver);
                        }
                        break;
                    case OrderTypeEnum.GetScreenSaver:
                        {
                            tcpClient.Send(sendDataTypeEnum, OrderTypeEnum.GetScreenSaver, _vcr.GetScreenSaver());
                        }
                        break;
                    case OrderTypeEnum.Browser:

                    case OrderTypeEnum.GetUrls:

                        break;
                }
            }
        };

    }

    private static bool TryParseLoopMode(string body, out LitVCR.LoopMode loopMode)
    {
        loopMode = LitVCR.LoopMode.none;
        string value = (body ?? string.Empty).Trim();
        if (value.Length == 0)
        {
            return false;
        }

        if (value.StartsWith("\"", StringComparison.Ordinal))
        {
            try
            {
                value = JsonConvert.DeserializeObject<string>(value) ?? string.Empty;
            }
            catch
            {
                return false;
            }
        }

        const string prefix = "Loop|";
        if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            value = value.Substring(prefix.Length).Trim();
        }

        if (int.TryParse(value, out int numericValue)
            && Enum.IsDefined(typeof(LitVCR.LoopMode), numericValue))
        {
            loopMode = (LitVCR.LoopMode)numericValue;
            return true;
        }

        return Enum.TryParse(value, true, out loopMode)
            && Enum.IsDefined(typeof(LitVCR.LoopMode), loopMode);
    }
}
