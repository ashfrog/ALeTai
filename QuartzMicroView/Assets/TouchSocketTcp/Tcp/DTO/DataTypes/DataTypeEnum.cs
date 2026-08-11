using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// IPNO枚举
/// </summary>
public enum DataTypeEnum
{
    /// <summary>
    /// mac地址 魔法包开机用
    /// </summary>
    UDP = 0,

    /// <summary>
    /// 中控主机标识
    /// </summary>
    S_MainHost = 1,

    /// <summary>
    /// 发送到平板
    /// </summary>
    S_Pad = 2,

    /// <summary>
    /// 串口服务器
    /// </summary>
    SP_9 = 9,

    /// <summary>
    /// 开关ipNo 192.168.3.10-3.17
    /// </summary>
    SW_XZTY_10 = 10, //新增投影
    SW_XTDP_11 = 11, //序厅大屏
    SW_HDTY_12 = 12, //互动投影
    SW_QXTY_13 = 13, //全息投影
    SW_ZM_14 = 14, //逐梦
    SW_QC_15 = 15, //强策
    SW_HM_16 = 16, //汇盟
    SW_ZY_17 = 17, //智御
    /// <summary>
    /// 电脑ipNo 192.168.3.18-3.25
    /// </summary>
    Media_XZTY_18 = 18, //新增投影
    Media_XTDP_19 = 19, //序厅大屏
    Media_HDTY_20 = 20, //互动投影
    Media_QXTY_21 = 21, //全息投影
    Media_ZM_22 = 22, //逐梦
    Media_QC_23 = 23, //强策
    Media_HM_24 = 24, //汇盟
    Media_ZY_25 = 25, //智御 安全


    TY_PROJECTOR_26 = 26,//  投影机
    TY_PROJECOTR_27 = 27,//投影机

    Media_DBCY_28 = 28,//电波传音
    Media_MDFY_29 = 29, //密电风云

    Door_31 = 31, //门禁机1
    Door_32 = 32, //门禁机2
    Door_33 = 33, //门禁机3
    Door_34 = 34, //门禁机4


    /// <summary>
    /// 控制端 平板播控组
    /// </summary>
    S_Pad118 = 118, //平板投影
    S_Pad119 = 119, //平板序厅大屏
    S_Pad120 = 120, //平板互动投影
    S_Pad121 = 121, //平板全息投影
    S_Pad122 = 122, //平板逐梦
    S_Pad123 = 123, //平板强策
    S_Pad124 = 124, //平板汇盟
    S_Pad125 = 125, //平板智御 安全
    S_Pad128 = 128, //平板电波传音
    S_Pad129 = 129, //平板密电风云

    /// <summary>
    /// 展厅区域灯光插座ipNo IP：192.168.3.9 端口：20001
    /// </summary>
    SW_DG_9_20001 = 20001,
    /// <summary>
    /// 办公区域灯光插座ipNo IP：192.168.3.9 端口：20002
    /// </summary>
    SW_DG_9_20002 = 20002,
    /// <summary>
    /// LED大屏控制ipNo IP：192.168.3.9 端口：20003
    /// </summary>
    SW_DG_9_20003 = 20003,
    /// <summary>
    /// 互动投影开关ipNo IP：192.168.3.9 端口：20004
    /// </summary>
    SW_DG_9_20004 = 20004,
    /// <summary>
    /// 3转屏转动
    /// </summary>
    SW_RS_9_20005 = 20005,
    /// <summary>
    /// 展厅区域灯LED大屏开关ipNo IP：192.168.3.9 端口：20006
    /// </summary>
    SW_DG_9_20006 = 20006,
    /// <summary>
    /// 大屏功放电源时序器ipNo  IP：192.168.3.9 端口：20007
    /// </summary>
    SW_GF_9_20007 = 20007,
    /// <summary>
    /// 展厅区域电动门开关ipNo IP：192.168.3.9 端口：20008
    /// </summary>
    SW_DG_9_20008 = 20008,

}

