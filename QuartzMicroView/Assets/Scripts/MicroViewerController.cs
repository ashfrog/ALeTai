using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// MicroViewer 场景的上一个、下一个和摄像头按钮控制器。
/// </summary>
public class MicroViewerController : MonoBehaviour
{
    [Header("核心组件")]
    [SerializeField] private TabSwitcher tabSwitcher;
    [SerializeField] private LitVCR litVCR;
    [SerializeField] private MicroscopeCameraDisplay cameraDisplay;

    [Header("按钮")]
    [SerializeField] private Button buttonL;
    [SerializeField] private Button buttonR;
    [SerializeField] private Button buttonM;

    [Header("相邻视频文件名")]
    [SerializeField] private TMPVideoNameMarquee previousVideoName;
    [SerializeField] private TMPVideoNameMarquee nextVideoName;

    private bool cameraMode;

    private void Awake()
    {
        ResolveReferences();
    }

    private void Start()
    {
        ResolveReferences();

        if (tabSwitcher != null)
        {
            tabSwitcher.TabButtonClicked += OnTabButtonClicked;
        }

        if (litVCR != null)
        {
            litVCR.PlaylistChanged += RefreshVideoNames;
            litVCR.VideoChanged += OnVideoChanged;
        }

        cameraMode = tabSwitcher != null && tabSwitcher.currentTabIndex == 2;
        if (!cameraMode && cameraDisplay != null)
        {
            cameraDisplay.StopCamera();
        }

        RefreshVideoNames();
    }

    private void OnDestroy()
    {
        if (tabSwitcher != null)
        {
            tabSwitcher.TabButtonClicked -= OnTabButtonClicked;
        }

        if (litVCR != null)
        {
            litVCR.PlaylistChanged -= RefreshVideoNames;
            litVCR.VideoChanged -= OnVideoChanged;
        }
    }

    private void ResolveReferences()
    {
        if (tabSwitcher == null)
        {
            tabSwitcher = GetComponent<TabSwitcher>();
        }

        if (buttonL == null && tabSwitcher != null && tabSwitcher.tabButtons != null && tabSwitcher.tabButtons.Length > 0)
        {
            buttonL = tabSwitcher.tabButtons[0];
        }

        if (buttonR == null && tabSwitcher != null && tabSwitcher.tabButtons != null && tabSwitcher.tabButtons.Length > 1)
        {
            buttonR = tabSwitcher.tabButtons[1];
        }

        if (buttonM == null && tabSwitcher != null && tabSwitcher.tabButtons != null && tabSwitcher.tabButtons.Length > 2)
        {
            buttonM = tabSwitcher.tabButtons[2];
        }

        if (previousVideoName == null && buttonL != null)
        {
            previousVideoName = buttonL.GetComponentInChildren<TMPVideoNameMarquee>(true);
        }

        if (nextVideoName == null && buttonR != null)
        {
            nextVideoName = buttonR.GetComponentInChildren<TMPVideoNameMarquee>(true);
        }

        if (cameraDisplay == null)
        {
            cameraDisplay = GetComponentInChildren<MicroscopeCameraDisplay>(true);
        }

        if (litVCR == null)
        {
            litVCR = FindObjectOfType<LitVCR>(true);
        }
    }

    private void OnTabButtonClicked(int index)
    {
        if (index == 2)
        {
            if (cameraMode)
            {
                ExitCameraMode(true);
            }
            else
            {
                EnterCameraMode();
            }

            return;
        }

        if (index != 0 && index != 1)
        {
            return;
        }

        if (cameraMode)
        {
            ExitCameraMode(false);
        }

        // Tab 0 和 Tab 1 共享 AVPro 页面，统一落到视频页即可。
        if (tabSwitcher != null)
        {
            tabSwitcher.SwitchTab(0);
        }

        if (litVCR != null)
        {
            if (index == 0)
            {
                litVCR.PlayPrevious();
            }
            else
            {
                litVCR.PlayNext();
            }
        }

        RefreshVideoNames();
    }

    private void EnterCameraMode()
    {
        cameraMode = true;
        if (litVCR != null)
        {
            litVCR.OnPauseButton();
        }

        if (tabSwitcher != null)
        {
            tabSwitcher.SwitchTab(2);
        }

        if (cameraDisplay != null)
        {
            cameraDisplay.StartCamera();
        }
    }

    private void ExitCameraMode(bool resumeVideo)
    {
        cameraMode = false;
        if (cameraDisplay != null)
        {
            cameraDisplay.StopCamera();
        }

        if (tabSwitcher != null)
        {
            tabSwitcher.SwitchTab(0);
        }

        if (resumeVideo && litVCR != null)
        {
            litVCR.OnPlayButton();
        }
    }

    private void OnVideoChanged(int index)
    {
        RefreshVideoNames();
    }

    public void RefreshVideoNames()
    {
        if (litVCR == null)
        {
            return;
        }

        if (previousVideoName != null)
        {
            previousVideoName.SetText(litVCR.GetAdjacentVideoName(-1));
        }

        if (nextVideoName != null)
        {
            nextVideoName.SetText(litVCR.GetAdjacentVideoName(1));
        }
    }
}
