using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// INI配置文件管理 (INI文件的底层读写操作)
/// </summary>
public class IniConfig
{
    private readonly string iniFilePath;
    private readonly Dictionary<string, Dictionary<string, string>> cachedConfig;
    private bool isDirty;
    private bool replaceFallbackLogged;

    /// <summary>
    /// 用于简单的线程安全（同进程内）
    /// </summary>
    private readonly object _lock = new object();

    public IniConfig(string fileName)
    {
        cachedConfig = new Dictionary<string, Dictionary<string, string>>();

        string directory = Application.streamingAssetsPath;
#if UNITY_ANDROID && !UNITY_EDITOR
        directory = Application.persistentDataPath;
#endif

        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        iniFilePath = Path.Combine(directory, fileName);

        if (!File.Exists(iniFilePath))
            File.Create(iniFilePath).Dispose();

        LoadConfig();
    }

    private void LoadConfig()
    {
        lock (_lock)
        {
            cachedConfig.Clear();

            try
            {
                string[] lines = File.ReadAllLines(iniFilePath, Encoding.UTF8);
                string currentSection = "";

                foreach (string line in lines)
                {
                    string trimmedLine = line.Trim();
                    if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith(";"))
                        continue;

                    if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                    {
                        currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2);
                        if (!cachedConfig.ContainsKey(currentSection))
                            cachedConfig[currentSection] = new Dictionary<string, string>();
                    }
                    else
                    {
                        int equalPos = trimmedLine.IndexOf('=');
                        if (equalPos > 0 && !string.IsNullOrEmpty(currentSection))
                        {
                            string key = trimmedLine.Substring(0, equalPos).Trim();
                            string value = trimmedLine.Substring(equalPos + 1).Trim();
                            cachedConfig[currentSection][key] = value;
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load INI file: {e.Message}");
            }
        }
    }

    /// <summary>
    /// 原子化保存：写到临时文件，再 Move 覆盖
    /// </summary>
    private bool SaveConfig()
    {
        lock (_lock)
        {
            if (!isDirty) return true;

            string tempFilePath = iniFilePath + ".tmp";

            try
            {
                // 写入到临时文件
                using (StreamWriter writer = new StreamWriter(tempFilePath, false, Encoding.UTF8))
                {
                    foreach (var section in cachedConfig)
                    {
                        writer.WriteLine($"[{section.Key}]");
                        foreach (var kvp in section.Value)
                            writer.WriteLine($"{kvp.Key}={kvp.Value}");
                        writer.WriteLine();
                    }
                }

                // 优先使用原子替换；不支持 Replace 的文件系统回退到覆盖复制。
                if (File.Exists(iniFilePath))
                {
                    try
                    {
                        File.Replace(tempFilePath, iniFilePath, null);
                    }
                    catch (System.Exception replaceException)
                    {
                        if (File.Exists(tempFilePath))
                        {
                            File.Copy(tempFilePath, iniFilePath, true);
                            try
                            {
                                File.Delete(tempFilePath);
                            }
                            catch
                            {
                                // 临时文件删除失败不影响正式配置已经写入。
                            }
                        }
                        else if (!File.Exists(iniFilePath))
                        {
                            throw;
                        }

                        if (!replaceFallbackLogged)
                        {
                            Debug.LogWarning($"File.Replace 保存配置失败，已回退覆盖写入：{replaceException.Message}");
                            replaceFallbackLogged = true;
                        }
                    }
                }
                else
                {
                    File.Move(tempFilePath, iniFilePath);
                }

                isDirty = false;
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save INI file: {e.Message}");
                // 保留临时文件，避免目标文件被占用时丢失待保存配置。
                return false;
            }
        }
    }

    /// <summary>
    /// 直接从缓存读，不存在就返回 defaultValue，不触发写入
    /// </summary>
    public string ReadValue(string section, string key, string defaultValue = "")
    {
        lock (_lock)
        {
            if (cachedConfig.ContainsKey(section) && cachedConfig[section].ContainsKey(key))
                return cachedConfig[section][key];

            return defaultValue;
        }
    }

    /// <summary>
    /// 写入缓存并触发 Save（原子写入）
    /// </summary>
    public bool WriteValue(string section, string key, string value)
    {
        lock (_lock)
        {
            try
            {
                if (!cachedConfig.ContainsKey(section))
                    cachedConfig[section] = new Dictionary<string, string>();

                cachedConfig[section][key] = value;
                isDirty = true;
                return SaveConfig();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to write value: {e.Message}");
                return false;
            }
        }
    }

    public int ReadInt(string section, string key, int defaultValue = 0)
    {
        string v = ReadValue(section, key, null);
        if (v == null) return defaultValue;
        return int.TryParse(v, out int result) ? result : defaultValue;
    }

    public float ReadFloat(string section, string key, float defaultValue = 0f)
    {
        string v = ReadValue(section, key, null);
        if (v == null) return defaultValue;
        return float.TryParse(v, out float result) ? result : defaultValue;
    }

    public bool ReadBool(string section, string key, bool defaultValue = false)
    {
        string v = ReadValue(section, key, null);
        if (v == null) return defaultValue;

        if (bool.TryParse(v, out bool result)) return result;
        if (v == "1") return true;
        if (v == "0") return false;
        return defaultValue;
    }
}
