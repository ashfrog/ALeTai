using UnityEngine;
using TMPro;
using System;
using System.Globalization;

public class TimeDisplay : MonoBehaviour
{
    [Header("UI组件")]
    public TextMeshProUGUI timeText;      // 显示时间的TMP组件
    public TextMeshProUGUI colonText;     // 显示冒号的TMP组件（用于闪烁效果，可选）
    public TextMeshProUGUI ampmText;      // 显示AM/PM的TMP组件

    [Header("设置")]
    public bool use24HourFormat = false;  // 是否使用24小时制
    public Color normalColor = Color.white;
    public Color blinkColor = Color.clear;

    private float blinkTimer = 0f;
    private bool colonVisible = true;
    private CultureInfo enCulture;

    void Start()
    {
        // 设置英文文化信息，确保AM/PM显示为英文
        enCulture = new CultureInfo("en-US");

        // 初始化颜色
        if (timeText != null)
        {
            timeText.color = normalColor;
        }

        if (colonText != null)
        {
            colonText.color = normalColor;
            colonText.text = ":";
        }

        if (ampmText != null)
        {
            ampmText.color = normalColor;
        }
    }

    void Update()
    {
        UpdateTime();
        UpdateColonBlink();
    }

    void UpdateTime()
    {
        if (timeText == null) return;

        DateTime currentTime = DateTime.Now;
        string timeString;

        if (use24HourFormat)
        {
            // 24小时制格式
            if (colonText != null)
            {
                // 如果有单独的冒号组件，时间不包含冒号
                timeString = currentTime.ToString("HH mm", enCulture);
            }
            else
            {
                // 如果没有单独的冒号组件，根据闪烁状态显示或隐藏冒号
                timeString = colonVisible ? currentTime.ToString("HH:mm", enCulture) : currentTime.ToString("HH mm", enCulture);
            }

            // 24小时制时隐藏AM/PM
            if (ampmText != null)
            {
                ampmText.text = "";
            }
        }
        else
        {
            // 12小时制格式
            if (colonText != null)
            {
                // 如果有单独的冒号组件，时间不包含冒号
                timeString = currentTime.ToString("hh mm", enCulture);
            }
            else
            {
                // 如果没有单独的冒号组件，根据闪烁状态显示或隐藏冒号
                timeString = colonVisible ? currentTime.ToString("hh:mm", enCulture) : currentTime.ToString("hh mm", enCulture);
            }

            // 单独显示AM/PM
            if (ampmText != null)
            {
                ampmText.text = currentTime.ToString("tt", enCulture);
            }
        }

        timeText.text = timeString;
    }

    void UpdateColonBlink()
    {
        blinkTimer += Time.deltaTime;

        if (blinkTimer >= 1f)
        {
            blinkTimer = 0f;
            colonVisible = !colonVisible;

            // 如果有单独的冒号TMP组件
            if (colonText != null)
            {
                colonText.color = colonVisible ? normalColor : blinkColor;
            }
        }
    }

    // 公共方法：切换时间格式
    public void ToggleTimeFormat()
    {
        use24HourFormat = !use24HourFormat;
    }

    // 公共方法：设置颜色
    public void SetColors(Color normal, Color blink)
    {
        normalColor = normal;
        blinkColor = blink;

        if (timeText != null)
        {
            timeText.color = normalColor;
        }

        if (ampmText != null)
        {
            ampmText.color = normalColor;
        }
    }
}

// 简化版本 - 使用TextMeshPro
public class SimpleTMPTimeDisplay : MonoBehaviour
{
    [Header("UI组件")]
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI ampmText;

    [Header("设置")]
    public bool use24HourFormat = false;

    private float blinkTimer = 0f;
    private bool showColon = true;
    private CultureInfo enCulture;

    void Start()
    {
        enCulture = new CultureInfo("en-US");
    }

    void Update()
    {
        if (timeText == null) return;

        // 更新闪烁计时器
        blinkTimer += Time.deltaTime;
        if (blinkTimer >= 1f)
        {
            blinkTimer = 0f;
            showColon = !showColon;
        }

        // 获取当前时间
        DateTime currentTime = DateTime.Now;
        string timeString;

        if (use24HourFormat)
        {
            // 24小时制
            timeString = showColon ?
                currentTime.ToString("HH:mm", enCulture) :
                currentTime.ToString("HH mm", enCulture);

            // 隐藏AM/PM
            if (ampmText != null)
            {
                ampmText.text = "";
            }
        }
        else
        {
            // 12小时制
            timeString = showColon ?
                currentTime.ToString("hh:mm", enCulture) :
                currentTime.ToString("hh mm", enCulture);

            // 单独显示AM/PM
            if (ampmText != null)
            {
                ampmText.text = currentTime.ToString("tt", enCulture);
            }
        }

        timeText.text = timeString;
    }
}