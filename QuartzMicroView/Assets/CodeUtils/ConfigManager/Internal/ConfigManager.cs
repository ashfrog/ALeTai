using UnityEngine;

/// <summary>
/// 配置管理器
/// </summary>
public class ConfigManager : MonoBehaviour
{
    private static ConfigManager instance;
    private IniConfig config;
    private SettingsManager settings;

    public static ConfigManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<ConfigManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("ConfigManager");
                    instance = go.AddComponent<ConfigManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }

    public SettingsManager Settings => settings ??= new SettingsManager(Config);
    public IniConfig Config => config ??= new IniConfig("settings.ini");

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }
}

