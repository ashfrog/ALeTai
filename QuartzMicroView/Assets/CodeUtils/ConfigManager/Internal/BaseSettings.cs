/// <summary>
/// 基础设置类 (配置设置的基类，提供通用的读写方法)
/// </summary>
public abstract class BaseSettings
{
    protected readonly IniConfig config;
    protected readonly string section;

    protected BaseSettings(IniConfig config, string section)
    {
        this.config = config;
        this.section = section;
    }

    /// <summary>
    /// 尝试读取字符串值；如果键不存在且提供了 defaultValue，则写入一次默认值并返回。
    /// 如果 defaultValue 为 null 或空，则仅返回 defaultValue，不写入。
    /// </summary>
    protected string ReadValue(string key, string defaultValue = "")
    {
        // 注意：此处调用的 IniConfig.ReadValue 的 defaultValue 传 null，
        // 表示“如果不存在就返回 null”，而不是写入默认值。
        string value = config.ReadValue(section, key, null);

        if (value == null && !string.IsNullOrEmpty(defaultValue))
        {
            // 键确实不存在，此时才把默认值写入文件
            config.WriteValue(section, key, defaultValue);
            return defaultValue;
        }

        return value ?? defaultValue;
    }

    protected float ReadFloat(string key, float defaultValue = 0f)
    {
        string value = config.ReadValue(section, key, null);
        if (value == null && !float.IsNaN(defaultValue) && !float.IsInfinity(defaultValue))
        {
            config.WriteValue(section, key, defaultValue.ToString());
            return defaultValue;
        }
        return config.ReadFloat(section, key, defaultValue);
    }

    protected int ReadInt(string key, int defaultValue = 0)
    {
        string value = config.ReadValue(section, key, null);
        if (value == null)
        {
            config.WriteValue(section, key, defaultValue.ToString());
            return defaultValue;
        }
        return config.ReadInt(section, key, defaultValue);
    }

    protected bool ReadBool(string key, bool defaultValue = false)
    {
        string value = config.ReadValue(section, key, null);
        if (value == null)
        {
            config.WriteValue(section, key, defaultValue.ToString());
            return defaultValue;
        }
        return config.ReadBool(section, key, defaultValue);
    }

    protected void WriteValue(string key, string value) => config.WriteValue(section, key, value);
}