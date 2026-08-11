using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// 指令类型
/// </summary>
public enum OrderTypeEnum
{
    /// <summary>
    /// FH客户端注册需要接收的DataType集合
    /// </summary>
    RegisterDataTypes = -1,

    AutoSwitchLine = 1,
    PowerOnMacAddress = 7,

    /// <summary>
    /// 发送str到配置的Udp客户端
    /// </summary>
    UdpClientStr = 8,


    LoadUrl = 9,//加载网页
    GoBack = 10,//网页返回
    GoForward = 11,//网页向后
    Reload = 12,//刷新网页
    EnableBrowser = 14,//设置显示网页
    GetEnableBrowser = 15,//获取显示网页开关
    GetPlayInfo = 18,//获取播放信息
    SetScreenSaver = 19,//设置屏保
    GetScreenSaver = 21,//获取屏保

    /// <summary>
    /// 由主控转发字符串
    /// </summary>
    Str = 20,

    /// <summary>
    /// 旋转增量
    /// </summary>
    Rotate = 30,

    /// <summary>
    /// 获取音量
    /// </summary>
    GetVolumn = 101,
    /// <summary>
    /// 设置音量
    /// </summary>
    SetVolumn = 102,

    /// <summary>
    /// 获取循环模式
    /// </summary>
    GetLoopMode = 200,

    /// <summary>
    /// 设置循环模式
    /// </summary>
    LoopMode = 201,

    /// <summary>
    /// 获取当前播放进度
    /// </summary>
    GetMovSeek = 501,
    /// <summary>
    /// 设置播放进度
    /// </summary>
    SetMovSeek = 502,

    /// <summary>
    /// 获取视频列表
    /// </summary>
    GetFileList = 301,

    /// <summary>
    /// 获取网页列表
    /// </summary>
    GetUrls = 401,

    /// <summary>
    /// 指定播放某个视频
    /// </summary>
    SetPlayMovie = 402,

    /// <summary>
    /// 指定轮播视频目录
    /// </summary>
    SetPlayMovieFolder = 400,
    /// <summary>
    /// 停止播放的视频
    /// </summary>
    StopMovie = 403,

    /// <summary>
    /// 播放下一个视频
    /// </summary>
    PlayNext = 404,

    /// <summary>
    /// 播放上一个视频
    /// </summary>
    PlayPrev = 405,

    /// <summary>
    /// 播放
    /// </summary>
    PlayMovie = 406,

    /// <summary>
    /// 暂停
    /// </summary>
    PauseMovie = 407,

    /// <summary>
    /// 获取当前播放视频名称
    /// </summary>
    GetCurMovieName = 408,

    /// <summary>
    /// 显示屏保
    /// </summary>
    PlayScreenSaver = 409,

    /// <summary>
    /// 获取视频总时长
    /// </summary>
    GetMovAllSecond = 601,

    /// <summary>
    /// 关机
    /// </summary>
    Shutdown = 1000,

    /// <summary>
    /// 打开电源
    /// </summary>
    PowerOn = 1001,

    /// <summary>
    /// 关闭电源
    /// </summary>
    PowerOff = 1002,

    /// <summary>
    /// 开门视频
    /// </summary>
    OpenTheDoor = 2000,

    /// <summary>
    /// 关门视频
    /// </summary>
    CloseTheDoor = 2001,

    ///遮罩
    ZheZhao = 3000,

    Browser = 4000,

    /// <summary>
    ///  关闭所有主机
    /// </summary>
    ShutDown = 10000,

    /// <summary>
    /// 开关灯指令
    /// </summary>
    SwitchLight = 10001,

    /// <summary>
    /// 三面转屏
    /// </summary>
    RoteteScreen3 = 20005
}

