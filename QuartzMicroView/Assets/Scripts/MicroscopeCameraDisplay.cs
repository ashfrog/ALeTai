using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 通过 Windows UVC 摄像头显示显微镜画面，并处理设备热插拔。
/// </summary>
[RequireComponent(typeof(RawImage))]
public class MicroscopeCameraDisplay : MonoBehaviour
{
    [SerializeField] private RawImage target;
    [SerializeField] private TMP_Text statusText;

    private WebCamTexture webcamTexture;
    private Coroutine monitorCoroutine;
    private bool requested;
    private string activeDeviceName;
    private string lastDeviceFingerprint;
    private string lastMissingConfiguredDevice;

    public bool IsStreaming => webcamTexture != null && webcamTexture.isPlaying;
    public string ActiveDeviceName => activeDeviceName;

    private void Awake()
    {
        if (target == null)
        {
            target = GetComponent<RawImage>();
        }

        SetWaitingVisual();
    }

    private void OnEnable()
    {
        if (requested)
        {
            EnsureMonitor();
        }
    }

    private void OnDisable()
    {
        StopMonitor();
        ReleaseTexture();
    }

    private void OnDestroy()
    {
        StopCamera();
    }

    public void StartCamera()
    {
        requested = true;
        if (isActiveAndEnabled)
        {
            EnsureMonitor();
        }
    }

    public void StopCamera()
    {
        requested = false;
        StopMonitor();
        ReleaseTexture();
    }

    private void EnsureMonitor()
    {
        if (monitorCoroutine == null)
        {
            monitorCoroutine = StartCoroutine(MonitorDevices());
        }
    }

    private void StopMonitor()
    {
        if (monitorCoroutine != null)
        {
            StopCoroutine(monitorCoroutine);
            monitorCoroutine = null;
        }
    }

    private IEnumerator MonitorDevices()
    {
        while (requested && isActiveAndEnabled)
        {
            WebCamDevice[] devices = GetDevices();
            string fingerprint = BuildFingerprint(devices);
            bool activeDeviceStillExists = ContainsDevice(devices, activeDeviceName);

            if (webcamTexture != null && (!activeDeviceStillExists || !webcamTexture.isPlaying || fingerprint != lastDeviceFingerprint))
            {
                ReleaseTexture();
            }

            lastDeviceFingerprint = fingerprint;
            if (webcamTexture == null && devices.Length > 0)
            {
                string selectedDevice = SelectDevice(devices);
                if (!string.IsNullOrEmpty(selectedDevice))
                {
                    StartTexture(selectedDevice);
                }
            }

            UpdatePresentation();
            yield return new WaitForSecondsRealtime(Settings.ini.Camera.ReconnectInterval);
        }

        monitorCoroutine = null;
    }

    private WebCamDevice[] GetDevices()
    {
        try
        {
            return WebCamTexture.devices ?? Array.Empty<WebCamDevice>();
        }
        catch (Exception exception)
        {
            Debug.LogWarning("读取摄像头设备列表失败: " + exception.Message);
            return Array.Empty<WebCamDevice>();
        }
    }

    private static string BuildFingerprint(WebCamDevice[] devices)
    {
        if (devices == null || devices.Length == 0)
        {
            return string.Empty;
        }

        string fingerprint = string.Empty;
        for (int i = 0; i < devices.Length; i++)
        {
            if (i > 0)
            {
                fingerprint += "|";
            }

            fingerprint += devices[i].name;
        }

        return fingerprint;
    }

    private static bool ContainsDevice(WebCamDevice[] devices, string deviceName)
    {
        if (string.IsNullOrEmpty(deviceName) || devices == null)
        {
            return false;
        }

        for (int i = 0; i < devices.Length; i++)
        {
            if (string.Equals(devices[i].name, deviceName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private string SelectDevice(WebCamDevice[] devices)
    {
        string configuredName = Settings.ini.Camera.DeviceName.Trim();
        if (!string.IsNullOrEmpty(configuredName))
        {
            for (int i = 0; i < devices.Length; i++)
            {
                if (string.Equals(devices[i].name, configuredName, StringComparison.OrdinalIgnoreCase))
                {
                    lastMissingConfiguredDevice = null;
                    return devices[i].name;
                }
            }

            if (!string.Equals(lastMissingConfiguredDevice, configuredName, StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning("未找到配置的摄像头，回退到第一个可用设备: " + configuredName);
                lastMissingConfiguredDevice = configuredName;
            }
        }

        return devices.Length > 0 ? devices[0].name : null;
    }

    private void StartTexture(string deviceName)
    {
        ReleaseTexture();

        var resolution = Settings.ini.Camera.Resolution;
        int fps = Settings.ini.Camera.FPS;
        webcamTexture = new WebCamTexture(deviceName, resolution.width, resolution.height, fps);
        activeDeviceName = deviceName;
        SetWaitingVisual();

        try
        {
            webcamTexture.Play();
            Debug.Log($"已启动显微镜摄像头: {deviceName} ({resolution.width}x{resolution.height}@{fps})");
        }
        catch (Exception exception)
        {
            Debug.LogWarning("启动显微镜摄像头失败: " + exception.Message);
            ReleaseTexture();
        }
    }

    private void ReleaseTexture()
    {
        if (webcamTexture != null)
        {
            if (webcamTexture.isPlaying)
            {
                webcamTexture.Stop();
            }

            if (target != null && target.texture == webcamTexture)
            {
                target.texture = null;
            }

            Destroy(webcamTexture);
            webcamTexture = null;
        }

        activeDeviceName = null;
        SetWaitingVisual();
    }

    private void SetWaitingVisual()
    {
        if (target == null)
        {
            return;
        }

        target.texture = null;
        target.color = Color.black;
        target.uvRect = new Rect(0f, 0f, 1f, 1f);
        target.rectTransform.localEulerAngles = Vector3.zero;
        SetStatus("摄像头已断开，等待重新连接...");
    }

    private void UpdatePresentation()
    {
        if (target == null || webcamTexture == null)
        {
            return;
        }

        if (target.texture != webcamTexture)
        {
            target.texture = webcamTexture;
        }

        if (webcamTexture.width > 16 && webcamTexture.height > 16)
        {
            target.color = Color.white;
            SetStatus(string.Empty);
        }

        int rotation = Settings.ini.Camera.Rotation + webcamTexture.videoRotationAngle;
        rotation = ((rotation % 360) + 360) % 360;
        target.rectTransform.localEulerAngles = new Vector3(0f, 0f, -rotation);

        bool mirrorX = Settings.ini.Camera.MirrorX;
        bool mirrorY = Settings.ini.Camera.MirrorY ^ webcamTexture.videoVerticallyMirrored;
        target.uvRect = new Rect(
            mirrorX ? 1f : 0f,
            mirrorY ? 1f : 0f,
            mirrorX ? -1f : 1f,
            mirrorY ? -1f : 1f);
    }

    private void SetStatus(string message)
    {
        if (statusText == null)
        {
            return;
        }

        statusText.text = message;
        statusText.gameObject.SetActive(!string.IsNullOrEmpty(message));
    }
}
