using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using TMPro;
using UnityEngine.UI;

[System.Serializable]
public class WeatherData
{
    public string code;
    public string updateTime;
    public string fxLink;
    public WeatherNow now;
    public WeatherRefer refer;
}

[System.Serializable]
public class WeatherNow
{
    public string obsTime;
    public string temp;
    public string feelsLike;
    public string icon;
    public string text;
    public string wind360;
    public string windDir;
    public string windScale;
    public string windSpeed;
    public string humidity;
    public string precip;
    public string pressure;
    public string vis;
    public string cloud;
    public string dew;
}

[System.Serializable]
public class WeatherRefer
{
    public string[] sources;
    public string[] license;
}

public class QWeatherAPI : MonoBehaviour
{
    [Header("API配置")]
    public string apiKey = "your-api-key-here";
    public string baseUrl = "https://p75ctu5wrj.re.qweatherapi.com/v7/weather/now";

    [Header("位置设置")]
    public string locationId = "101040100"; // 北京的位置ID

    [Header("认证方式选择")]
    public bool useHeaderAuth = true; // true使用Header认证，false使用参数认证

    public RawImage weatherIcon;
    public TMP_Text text_温度;
    public TMP_Text text_天气状况;
    public TMP_Text text_湿度;

    void Start()
    {
        // 启动时获取天气数据
        GetWeatherData();
    }
    float curt = 0;
    /// <summary>
    /// 1小时获取一次天气
    /// </summary>
    float wt = 3600;
    private void Update()
    {
        curt += Time.deltaTime;
        if (curt > wt)
        {
            curt = 0;
            GetWeatherData();
        }
    }

    public void GetWeatherData()
    {
        StartCoroutine(FetchWeatherData());
    }

    IEnumerator FetchWeatherData()
    {
        UnityWebRequest request;

        if (useHeaderAuth)
        {
            // 方式1：使用Header认证
            string url = $"{baseUrl}?location={locationId}";
            request = UnityWebRequest.Get(url);
            request.SetRequestHeader("X-QW-Api-Key", apiKey);
        }
        else
        {
            // 方式2：使用请求参数认证
            string url = $"{baseUrl}?location={locationId}&key={apiKey}";
            request = UnityWebRequest.Get(url);
        }

        // 设置通用请求头
        request.SetRequestHeader("Accept-Encoding", "gzip");
        request.SetRequestHeader("Accept", "application/json");

        Debug.Log($"发送请求到: {request.url}");
        Debug.Log($"认证方式: {(useHeaderAuth ? "Header认证" : "参数认证")}");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("请求成功!");
            Debug.Log($"响应数据: {request.downloadHandler.text}");

            try
            {
                // 解析JSON数据
                WeatherData weatherData = Newtonsoft.Json.JsonConvert.DeserializeObject<WeatherData>(request.downloadHandler.text);
                ProcessWeatherData(weatherData);
            }
            catch (Exception e)
            {
                Debug.LogError($"JSON解析错误: {e.Message}");
            }
        }
        else
        {
            Debug.LogError($"请求失败: {request.error}");
            Debug.LogError($"响应码: {request.responseCode}");
            Debug.LogError($"错误详情: {request.downloadHandler.text}");
        }

        request.Dispose();
    }

    void ProcessWeatherData(WeatherData data)
    {
        if (data.code == "200")
        {
            Debug.Log("=== 天气数据 ===");
            Debug.Log($"温度: {data.now.temp}°C");
            Debug.Log($"体感温度: {data.now.feelsLike}°C");
            Debug.Log($"天气状况: {data.now.text}");
            Debug.Log($"湿度: {data.now.humidity}%");
            Debug.Log($"风向: {data.now.windDir}");
            Debug.Log($"风速: {data.now.windSpeed} km/h");
            Debug.Log($"更新时间: {data.now.obsTime}");

            // 在这里你可以更新UI或执行其他逻辑
            OnWeatherDataReceived(data);
        }
        else
        {
            Debug.LogError($"API返回错误码: {data.code}");
        }
    }

    // 天气数据接收回调
    void OnWeatherDataReceived(WeatherData data)
    {
        // 在这里添加你的天气数据处理逻辑
        // 例如更新UI、触发游戏事件等
        //{"code":"200","updateTime":"2025-06-04T14:36+08:00","fxLink":"https://www.qweather.com/weather/chongqing-101040100.html",
        //"now":{"obsTime":"2025-06-04T14:32+08:00","temp":"27","feelsLike":"28","icon":"104","text":"阴","wind360":"180",
        //"windDir":"南风","windScale":"2","windSpeed":"8","humidity":"62","precip":"0.0","pressure":"979","vis":"9",
        //"cloud":"91","dew":"20"},
        //"refer":{"sources":["QWeather"],"license":["QWeather Developers License"]}}
        Debug.Log("=== 天气数据 ===");
        Debug.Log($"温度: {data.now.temp}°C");
        //Debug.Log($"体感温度: {data.now.feelsLike}°C");
        Debug.Log($"天气状况: {data.now.text}");
        Debug.Log($"湿度: {data.now.humidity}%");
        //Debug.Log($"风向: {data.now.windDir}");
        //Debug.Log($"风速: {data.now.windSpeed} km/h");
        Debug.Log($"icon图标: {data.now.icon}");

        text_温度.text = $"{data.now.temp}°C";
        text_天气状况.text = $"{data.now.text}";
        text_湿度.text = $"{data.now.humidity}";

        //将StreamingAssets\weather_icons路径下的png图片显示出来
        string iconPath = $"weather_icons/{data.now.icon}.png";
        string fullPath = System.IO.Path.Combine(Application.streamingAssetsPath, iconPath);
        Debug.Log($"图标路径: {fullPath}");
        if (System.IO.File.Exists(fullPath))
        {
            Debug.Log("图标文件存在，加载中...");
            StartCoroutine(LoadWeatherIcon(fullPath));
        }
    }

    /// <summary>
    /// Coroutine to load the weather icon from the specified path.
    /// </summary>
    /// <param name="fullPath">Full path to the weather icon file.</param>
    /// <returns>IEnumerator for coroutine.</returns>
    public IEnumerator LoadWeatherIcon(string fullPath)
    {
        // Check if the file exists
        if (!System.IO.File.Exists(fullPath))
        {
            Debug.LogError($"File not found at path: {fullPath}");
            yield break;
        }

        // Load the image as a Texture2D
        byte[] fileData = System.IO.File.ReadAllBytes(fullPath);
        Texture2D texture = new Texture2D(2, 2);
        if (!texture.LoadImage(fileData))
        {
            Debug.LogError("Failed to load image from file.");
            yield break;
        }

        if (weatherIcon != null)
        {
            Destroy(weatherIcon.texture);
            weatherIcon.texture = texture;
        }
        yield return null;
    }

    // 公共方法：获取指定城市的天气
    public void GetWeatherForLocation(string newLocationId)
    {
        locationId = newLocationId;
        GetWeatherData();
    }

    // 公共方法：切换认证方式
    public void SwitchAuthMethod()
    {
        useHeaderAuth = !useHeaderAuth;
        Debug.Log($"切换到: {(useHeaderAuth ? "Header认证" : "参数认证")}");
    }
}