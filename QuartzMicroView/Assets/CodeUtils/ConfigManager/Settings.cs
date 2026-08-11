using System.IO;
using UnityEngine;


/// <summary>
/// 静态访问入口
/// </summary>
public static class Settings
{
    public static SettingsManager ini => ConfigManager.Instance.Settings;
}

/// <summary>
/// 图形设置
/// </summary>
public class SGraphicsSettings : BaseSettings
{
    public SGraphicsSettings(IniConfig config) : base(config, "Graphics")
    {
    }

    public float WebResolution
    {
        get => ReadFloat("WebResolution", 0.5f);
        set => WriteValue("WebResolution", value.ToString());
    }

    public string Quality
    {
        get => ReadValue("Quality", "Medium");
        set => WriteValue("Quality", value);
    }

    public bool EnableMask
    {
        get => ReadBool("EnableMask", false);
        set => WriteValue("EnableMask", value.ToString());
    }

    public bool FullScreen
    {
        get => ReadBool("FullScreen", true);
        set => WriteValue("FullScreen", value.ToString());
    }

    public bool HideCursor
    {
        get => ReadBool("HideCursor", true);
        set => WriteValue("HideCursor", value.ToString());
    }
    public bool TopMost
    {
        get => ReadBool("TopMost", true);
        set => WriteValue("TopMost", value.ToString());
    }

    public string ScreenSaver
    {
        get => ReadValue("ScreenSaver", "屏保.png");
        set => WriteValue("ScreenSaver", value);
    }

    public int TabMode
    {
        get => ReadInt("TabMode", 0);
        set => WriteValue("TabMode", value.ToString());
    }

    public (int width, int height) Resolution
    {
        get
        {
            string resolution = ReadValue("Resolution", "1920*1080");
            string[] parts = resolution.Split('*');
            if (parts.Length == 2 && int.TryParse(parts[0], out int width) && int.TryParse(parts[1], out int height))
            {
                return (width, height);
            }
            return (1920, 1080);
        }
        set => WriteValue("Resolution", $"{value.width}*{value.height}");
    }
}

/// <summary>
/// 游戏设置
/// </summary>
public class GameSettings : BaseSettings
{
    private static readonly string[] validDifficulties = { "Easy", "Normal", "Hard" };

    public GameSettings(IniConfig config) : base(config, "Game")
    {
    }

    public float Volumn
    {
        get => ReadFloat("Volumn", 1f);
        set => WriteValue("Volumn", value.ToString());
    }

    public string LoopMode
    {
        get => ReadValue("LoopMode", "all");
        set => WriteValue("LoopMode", value.ToString());
    }

    public float AutoResetTime
    {
        get => ReadFloat("AutoResetTime", 8f);
        set => WriteValue("AutoResetTime", value.ToString());
    }

    public float ReloadUrlAfterDelay
    {
        get => ReadFloat("ReloadUrlAfterDelay", 5f);
        set => WriteValue("ReloadUrlAfterDelay", value.ToString());
    }

    public int VideoIndex
    {
        get => ReadInt("VideoIndex", 0);
        set => WriteValue("VideoIndex", value.ToString());
    }

    public bool SkipVerify
    {
        get => ReadBool("SkipVerify", false);
        set => WriteValue("SkipVerify", value.ToString());
    }

    public string Difficulty
    {
        get => ReadValue("Difficulty", "Normal");
        set
        {
            if (System.Array.Exists(validDifficulties, d => d == value))
            {
                WriteValue("Difficulty", value);
            }
        }
    }

    public string Language
    {
        get => ReadValue("Language", "en");
        set => WriteValue("Language", value);
    }
}

public class IPSettings : BaseSettings
{
    public IPSettings(IniConfig config) : base(config, "IPHost")
    {
    }

    public string QueServer
    {
        get => ReadValue("QueServer", "127.0.0.1");
        set => WriteValue("QueServer", value.ToString());
    }

    public string FHServerIP
    {
        get => ReadValue("FHServerIP", "127.0.0.1");
        set => WriteValue("FHServerIP", value.ToString());
    }

    public string ServerIPHost
    {
        get => ReadValue("ServerIPHost", "127.0.0.1:7790");
        set => WriteValue("ServerIPHost", value.ToString());
    }

    public string DoorIPHost
    {
        get => ReadValue("DoorIPHost", "");
        set => WriteValue("DoorIPHost", value.ToString());
    }

    public float MediaTime
    {
        get => ReadFloat("MediaTime", 30f);
        set => WriteValue("MediaTime", value.ToString());
    }

    public int DeviceDataTypeID
    {
        get => ReadInt("DeviceDataTypeID", 0);
        set => WriteValue("DeviceDataTypeID", value.ToString());
    }

}

public class PathSettings : BaseSettings
{
    public PathSettings(IniConfig config) : base(config, "PathSettings")
    {
    }

    public string MediaPath
    {
        get => ReadValue("MediaPath", "");
        set => WriteValue("MediaPath", value);
    }
}
