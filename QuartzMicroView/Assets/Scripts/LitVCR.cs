#if UNITY_4_6 || UNITY_4_7 || UNITY_4_8 || UNITY_5 || UNITY_5_4_OR_NEWER
#define UNITY_FEATURE_UGUI
#endif

using UnityEngine;

#if UNITY_FEATURE_UGUI

using RenderHeads.Media.AVProVideo;
using System.IO;
using System.Collections.Generic;
using System;
using UnityEngine.UI;
using System.Collections;
using System.Linq;
using Newtonsoft.Json.Linq;
using Unity.VisualScripting;
using UnityEngine.Windows;
using File = System.IO.File;
using Directory = System.IO.Directory;
using Input = UnityEngine.Input;
using Vuplex.WebView;
using System.Threading.Tasks;

//-----------------------------------------------------------------------------
// Copyright 2015-2018 RenderHeads Ltd.  All rights reserverd.
//-----------------------------------------------------------------------------

/// <summary>
/// A demo of a simple video player using uGUI for display
/// Uses two MediaPlayer components, with one displaying the current video
/// while the other loads the next video.  MediaPlayers are then swapped
/// once the video is loaded and has a frame available for display.
/// This gives a more seamless display than simply using a single MediaPlayer
/// as its texture will be destroyed when it loads a new video
/// </summary>
public class LitVCR : MonoBehaviour
{
    public DisplayUGUI _mediaDisplay;
    public List<DisplayUGUI> _mediaDisplay_binds;
    public MediaPlayer _mediaPlayer;
    public MediaPlayer _mediaPlayerB;
    public MediaPlayer.FileLocation _location = MediaPlayer.FileLocation.AbsolutePathOrURL;

    private MediaPlayer _loadingPlayer;

    public List<Text> titleBinds;

    public string filterSectionNo;

    private const string shaderMask = "AVProVideo/UI/ColorCorrected";

    private string currentPlayingVideo = "";

    bool enablemask;
    float autoResetTime;
    Coroutine autoResetCroutine;

    //private const string VOLUMN_KEY = "volumn";//音量设置key

    //private const string LOOPMODE = "loopmode";//循环模式key

    //private const string SCREENSAVER = "screensaver";//屏保key

    /// <summary>
    /// 标题绑定文件/文件夹名
    /// </summary>
    [SerializeField]
    private bool titleBindFileName;

    public int movTitleIndexOffset = 1;

    public bool autoPlayNext;

    /// <summary>
    /// 同一个文件夹下的视频文件 (播放列表)
    /// </summary>
    public List<string> videoPaths;

    /// <summary>
    /// 视频文件上级文件夹名
    /// </summary>
    private List<string> shortFolderNames;

    /// <summary>
    /// 当前播放视频文件索引
    /// </summary>
    public int videoindex = 0;

    /// <summary>
    /// 播放列表和当前视频变化通知，供本地 UI 和远程控制同步标题。
    /// </summary>
    public event Action PlaylistChanged;
    public event Action<int> VideoChanged;

    /// <summary>
    /// 文件夹内视频播完一次后调用
    /// </summary>
    public Action OncePlayFolder;

    /// <summary>
    /// 播放完一轮标记
    /// </summary>
    public bool radarPlaying;

    /// <summary>
    /// 正常视频文件夹如 0.00.000
    /// </summary>
    private string _normalVideoFolder;

    /// <summary>
    /// 强制缩放比例差值 图片长宽比和RawImage组件长宽差值超过该值则自适应图片框 小于该值则强制拉伸 保证图片拉伸不离谱
    /// </summary>
    private const float ratioDelta = 1.77f;

    public bool hideWhiteImgWhenShowImg;

    public static string persistentDataPath;

    public float imgSeconds = 5;
    private float curImgSec;

    public List<Texture> maskTextures;
    [SerializeField]
    private RawImage RawImagePre;
    [SerializeField]
    private MaskableGraphic maskDisplay;

    public enum LoopMode
    {
        none,
        one,
        all
    }

    public enum TabMode
    {
        视频, 网页, 时间
    }
    [SerializeField]
    TabSwitcher tabSwitcher;

    [SerializeField]
    private CanvasWebViewPrefab canvasWebViewPrefab;

    /// <summary>
    /// 雷达视频文件夹如 /0.00
    /// </summary>
    private string _radarVideoFolter
    {
        get
        {
            if (!string.IsNullOrEmpty(_normalVideoFolder) && _normalVideoFolder.Length > 3)
            {
                return _normalVideoFolder.Substring(0, _normalVideoFolder.Length - 5);// /0.00.000/ -> /0.00
            }
            else
            {
                return "";
            }
        }
    }

    /// <summary>
    /// 通过设置_loadingPlayer来设置PlayingPlayer
    /// </summary>
    public MediaPlayer PlayingPlayer
    {
        get
        {
            if (LoadingPlayer == _mediaPlayer)
            {
                return _mediaPlayerB;
            }
            return _mediaPlayer;
        }
    }

    public void SetVolumn(float volumn)
    {
        //PlayerPrefs.SetFloat(VOLUMN_KEY, volumn);
        Settings.ini.Game.Volumn = volumn;
        LoadVolumn();
    }

    public void LoadVolumn()
    {
        float volumn = Settings.ini.Game.Volumn;
        if (PlayingPlayer && PlayingPlayer.Control != null)
        {
            PlayingPlayer.Control.SetVolume(volumn);
            PlayingPlayer.m_Volume = volumn;
        }
        if (LoadingPlayer && PlayingPlayer.Control != null)
        {
            LoadingPlayer.Control.SetVolume(volumn);
            LoadingPlayer.m_Volume = volumn;
        }
    }

    public void VolumnDown()
    {
        if (GetVolumn() > 0f)
        {
            SetVolumn(GetVolumn() - 0.1f);
        }
    }

    public void VolumnUp()
    {
        if (GetVolumn() < 1f)
        {
            SetVolumn(GetVolumn() + 0.1f);
        }
    }

    public float GetVolumn()
    {
        //return PlayerPrefs.GetFloat(VOLUMN_KEY, 1f);
        return Settings.ini.Game.Volumn;
    }

    public void PlayPrevious()
    {
        if (videoPaths == null || videoPaths.Count == 0)
        {
            return;
        }

        videoindex = GetAdjacentVideoIndex(-1);
        OpenVideoByIndex(videoindex, true);
    }

    public void PlayNext()
    {
        if (enablePlayNext && videoPaths != null && videoPaths.Count > 0)
        {
            videoindex = GetAdjacentVideoIndex(1);
            OpenVideoByIndex(videoindex, true);
        }
    }

    bool enablePlayNext = true;

    public void StartPlay()
    {
        OpenVideoFolder(persistentDataPath);
    }

    private void SkipNextScreenSaver()
    {
        if (videoPaths == null || videoPaths.Count == 0)
        {
            return;
        }

        int netxtindex = NormalizeVideoIndex(videoindex + 1);
        string nextfilename = Path.GetFileName(videoPaths[netxtindex]);
        if (nextfilename.Equals(GetScreenSaver()))//下一个是屏保
        {
            videoindex = netxtindex;//跳过屏保
        }
    }

    private void SkipPrevScreenSaver()
    {
        if (videoPaths == null || videoPaths.Count == 0)
        {
            return;
        }

        int netxtindex = NormalizeVideoIndex(videoindex - 1);
        string nextfilename = Path.GetFileName(videoPaths[netxtindex]);
        if (nextfilename.Equals(GetScreenSaver()))//下一个是屏保
        {
            videoindex = netxtindex;//跳过屏保
        }
    }

    private int NormalizeVideoIndex(int index)
    {
        if (videoPaths == null || videoPaths.Count == 0)
        {
            return -1;
        }

        index %= videoPaths.Count;
        if (index < 0)
        {
            index += videoPaths.Count;
        }

        return index;
    }

    /// <summary>
    /// 获取按当前播放列表规则计算的上一个/下一个视频索引。
    /// 屏保文件会被跳过，且索引循环。
    /// </summary>
    public int GetAdjacentVideoIndex(int direction)
    {
        if (videoPaths == null || videoPaths.Count == 0 || direction == 0)
        {
            return -1;
        }

        direction = direction < 0 ? -1 : 1;
        int candidate = NormalizeVideoIndex(videoindex + direction);
        int firstCandidate = candidate;

        for (int i = 0; i < videoPaths.Count; i++)
        {
            if (!Path.GetFileName(videoPaths[candidate]).Equals(GetScreenSaver(), StringComparison.OrdinalIgnoreCase)
                || candidate == videoindex)
            {
                return candidate;
            }

            candidate = NormalizeVideoIndex(candidate + direction);
            if (candidate == firstCandidate)
            {
                break;
            }
        }

        return firstCandidate;
    }

    /// <summary>
    /// 返回不含扩展名的播放列表文件名。
    /// </summary>
    public string GetVideoName(int index)
    {
        if (videoPaths == null || index < 0 || index >= videoPaths.Count || string.IsNullOrEmpty(videoPaths[index]))
        {
            return "无视频";
        }

        string path = videoPaths[index].Trim();
        try
        {
            if (IsWebUrl(path))
            {
                path = new Uri(path).AbsolutePath;
            }
        }
        catch
        {
            // 对无法解析的 URL 继续按普通路径处理。
        }

        string name = Path.GetFileNameWithoutExtension(Uri.UnescapeDataString(path));
        return string.IsNullOrEmpty(name) ? "无视频" : name;
    }

    public string GetAdjacentVideoName(int direction)
    {
        return GetVideoName(GetAdjacentVideoIndex(direction));
    }

    /// <summary>
    /// 播放屏保  成功播放屏保返回true
    /// </summary>
    /// <returns></returns>
    public bool PlayScreenSaver()
    {
        bool success = false;
        string filename = GetScreenSaver();
        if (!String.IsNullOrEmpty(filename))
        {
            string screensaverfile = Path.Combine(persistentDataPath, filename);
            if (File.Exists(screensaverfile)) //设置了屏保图片或者视频的
            {
                PlayingPlayer.m_Loop = true;
                LoadingPlayer.m_Loop = true;
                imgstopped = true;
                OpenVideoByFileName(filename, imgstopped);
                success = true;
            }
        }
        return success;
    }

    public bool isPaused()
    {
        return !PlayingPlayer.Control.IsPlaying() && imgstopped;
    }

    public bool isMovPaused()
    {
        if (PlayingPlayer)
        {
            return !PlayingPlayer.Control.IsPlaying();
        }
        return false;
    }

    public MediaPlayer LoadingPlayer
    {
        get
        {
            return _loadingPlayer;
        }
    }

    private void SwapPlayers()
    {
        // Pause the previously playing video
        PlayingPlayer.Control.Pause();

        // Swap the videos
        if (LoadingPlayer == _mediaPlayer)
        {
            _loadingPlayer = _mediaPlayerB;
        }
        else
        {
            _loadingPlayer = _mediaPlayer;
        }

        // Change the displaying video
        _mediaDisplay.CurrentMediaPlayer = PlayingPlayer;
        if (_mediaDisplay_binds != null && _mediaDisplay_binds.Count > 0)
        {
            foreach (var bind in _mediaDisplay_binds)
            {
                if (bind != null)
                {
                    bind.CurrentMediaPlayer = _mediaDisplay.CurrentMediaPlayer;
                }
            }
        }
    }

    public void OnVideoSeekSlider(float seekvalue)
    {
        if (PlayingPlayer)
        {
            PlayingPlayer.Control.Seek(seekvalue * PlayingPlayer.Info.GetDurationMs());
        }
    }

    public string GetPlayInfo()
    {
        string playinginfodto = "";
        if (FileUtils.IsMovFile(videoPaths[videoindex]))
        {
            if (PlayingPlayer)
            {
                float time = PlayingPlayer.Control.GetCurrentTimeMs();
                float duration = PlayingPlayer.Info.GetDurationMs();
                //视频播放时间ms，视频总时长ms，视频 index，文件名称
                playinginfodto = $"PlayInfo|{time},{duration},{videoindex},{Path.GetFileName(PlayingPlayer.m_VideoPath)}";
            }
        }
        else
        {
            playinginfodto = $"PlayInfo|{curImgSec * 1000},{imgSeconds * 1000},{videoindex},{Path.GetFileName(videoPaths[videoindex])}";
        }


        return playinginfodto;
    }

    /// <summary>
    /// 播放视频
    /// </summary>
    /// <param name="videopath">视频绝对路径</param>
    /// <param name="reload">强制重新加载视频</param>
    public void OpenVideoFile(string videopath, bool reload = false)
    {
        //视频存在 && 当前播放的路径不是这个视频
        bool reloadCurrent = (!String.Equals(videopath.Trim(), currentPlayingVideo.Trim(), StringComparison.OrdinalIgnoreCase) || reload);
        if (LoadingPlayer != null && !string.IsNullOrEmpty(videopath) && File.Exists(videopath) && reloadCurrent)
        {
            LoadingPlayer.m_VideoPath = videopath;

            if (!Path.GetFileName(videopath).Equals(Settings.ini.Graphics.ScreenSaver)) //当前视频不是屏保视频
            {
                if (LoopMode.one.ToString().Equals(GetLoopMode())) //单个循环播放
                {
                    PlayingPlayer.m_Loop = true;
                    LoadingPlayer.m_Loop = true;
                }
                else //多个视频
                {
                    PlayingPlayer.m_Loop = false;
                    LoadingPlayer.m_Loop = false;
                }
            }
            else
            {
                PlayingPlayer.m_Loop = true;
                LoadingPlayer.m_Loop = true;
            }

            LoadingPlayer.OpenVideoFromFile(_location, LoadingPlayer.m_VideoPath, true);

            currentPlayingVideo = videopath;
        }
        else
        {
            LoadingPlayer.CloseVideo();
        }
    }

    public void PlayRadarMov()
    {
        //播放雷达文件夹内视频
        OpenVideoFolder(_radarVideoFolter, true, () =>
        {
            OpenVideoFolder(_normalVideoFolder, true);//雷达播放完播放正常文件夹内的视频
        });
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            PlayRadarMov();
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            PlayPrevious();
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            PlayNext();
        }
    }
    const string playlistFile = "playlist.json";
    public string ReloadFileList()
    {
        string playlistFilePath = Path.Combine(persistentDataPath, playlistFile);
        if (File.Exists(playlistFilePath))
        {
            try
            {
                string arrStr = File.ReadAllText(playlistFilePath);
                var files = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(arrStr);
                videoPaths = files == null
                    ? new List<string>()
                    : files.Where(s => !string.IsNullOrWhiteSpace(s))
                        .Select(s => IsWebUrl(s) ? s : Path.Combine(persistentDataPath, s)).ToList();
                PlaylistChanged?.Invoke();
                return string.Join(", ", videoPaths); // 改为更适合输出的方式
            }
            catch (Exception ex)
            {
                Debug.Log("解析文件列表playlist.json失败:" + ex.Message);
            }
        }

        videoPaths = FileUtils.GetMediaFiles(persistentDataPath);
        PlaylistChanged?.Invoke();
        return string.Join(", ", videoPaths); // 改为更适合输出的方式
    }

    public string GetFileListStr()
    {
        return ConvertListToString(videoPaths);
    }

    public static string ConvertListToString(List<string> list, string separator = ",")
    {
        return string.Join(separator, list.Where(item => !string.IsNullOrEmpty(item)).Select(item => Path.GetFileName(item.Trim())));
    }

    /// <summary>
    /// 顺序播放文件夹下的视频
    /// </summary>
    /// <param name="videofolder"></param>
    /// <param name="oncePlayFolder">文件夹下视频播放完一轮后调用</param>
    /// <returns>文件夹是否有视频文件</returns>
    public bool OpenVideoFolder(string videofolder, bool reload = false, Action oncePlayFolder = null)
    {
        //SetMask(_mediaDisplay, true);
        if (oncePlayFolder == null)
        {
            persistentDataPath = videofolder;
            ReloadFileList();
        }
        videoindex = Settings.ini.Game.VideoIndex;
        OpenVideoByIndex(videoindex, reload);

        return true;
    }

    public void CloseVideo()
    {
        if (PlayingPlayer.VideoOpened)
        {
            PlayingPlayer.CloseVideo();
        }
        if (LoadingPlayer.VideoOpened)
        {
            LoadingPlayer.CloseVideo();
        }
    }

    public void OnPlayButton()
    {
        if (PlayingPlayer && PlayingPlayer.Control != null)
        {
            if (String.IsNullOrEmpty(PlayingPlayer.m_VideoPath) || IsWebUrl(videoPaths[videoindex]))
            {
                OpenVideoByIndex(videoindex);
            }
            else
            {
                if (FileUtils.IsMovFile(videoPaths[videoindex]))
                {
                    
                    PlayingPlayer.Control.Play();
                    DisableImg(rawimage, _mediaDisplay);
                }
            }
        }
        imgstopped = false;
    }

    public void OnPauseButton()
    {
        if (PlayingPlayer && PlayingPlayer.Control != null)
        {
            PlayingPlayer.Control.Pause();
        }
        imgstopped = true;
    }

    public void OnRewindButton(int index)
    {
        if (PlayingPlayer)
        {
            PlayingPlayer.Rewind(true);
            OpenVideoByIndex(index);
        }
        imgstopped = false;
    }

    public void OnRewindButton()
    {
        if (PlayingPlayer)
        {
            PlayingPlayer.Rewind(true);
            OpenVideoByIndex(videoindex);
        }
        imgstopped = false;
    }

    private void Awake()
    {
    }

    private void OnEnable()
    {
        _loadingPlayer = _mediaPlayerB;

        if (!Directory.Exists(Settings.ini.Path.MediaPath))
        {
            Settings.ini.Path.MediaPath = System.IO.Path.Combine(Application.streamingAssetsPath, "媒体文件");
        }

        if (!System.IO.Directory.Exists(Settings.ini.Path.MediaPath))
        {
            System.IO.Directory.CreateDirectory(Settings.ini.Path.MediaPath);
        }

        persistentDataPath = Settings.ini.Path.MediaPath;



        String ImgSecondsFilePath = Path.Combine(persistentDataPath, "imgSwapConfig.json");
        if (System.IO.File.Exists(ImgSecondsFilePath)) //媒体文件夹下的SwitchSeconds.txt 图片轮播间隔时间
        {
            string ImgSecondsStr = System.IO.File.ReadAllText(ImgSecondsFilePath);
            if (!String.IsNullOrEmpty(ImgSecondsStr))
            {
                var jObj = JObject.Parse(ImgSecondsStr);
                bool enable = (bool)jObj["Enable"];
                imgSeconds = (int)jObj["Second"];
            }
        }
    }

    private void Start()
    {
        enablemask = Settings.ini.Graphics.EnableMask;
        autoResetTime = Settings.ini.Game.AutoResetTime;
        if (enablemask)
        {
            //读取StreamAssets下mask目录内的图片到maskTexture
            string maskpath = Path.Combine(Application.streamingAssetsPath, "mask");
            if (Directory.Exists(maskpath))
            {
                // 只获取图片文件（png, jpg, jpeg 可自行扩展）
                string[] maskfiles = Directory.GetFiles(maskpath)
                    .Where(f => f.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                             || f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                             || f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(f => Path.GetFileName(f)) // 按文件名排序
                    .ToArray();

                for (int i = 0; i < maskfiles.Length; i++)
                {
                    if (File.Exists(maskfiles[i]))
                    {
                        Texture2D texture = new Texture2D(2, 2);
                        texture.LoadImage(File.ReadAllBytes(maskfiles[i]));
                        maskTextures.Add(texture);
                    }
                }
            }

            //maskDisplay.material = new Material(Shader.Find(shaderMask));//替换为可加遮罩的material00
            maskDisplay.material = new Material(maskDisplay.material);
            //shader 的 _FilterTex 指定一个texture
            //if (maskTextures != null && maskTextures.Count > 0)
            //{
            //    maskDisplay.material.SetTexture("_FilterTex", maskTextures[0]);
            //}

        }

        if (PlayingPlayer)
        {
            PlayingPlayer.Events.AddListener(OnVideoEvent);

            if (LoadingPlayer)
            {
                LoadingPlayer.Events.AddListener(OnVideoEvent);
            }
        }

        StartCoroutine(ReloadUrlAfterDelay());
    }

    IEnumerator ReloadUrlAfterDelay()
    {
        float delaytime = Settings.ini.Game.ReloadUrlAfterDelay;
        if (delaytime > 0)
        {
            yield return new WaitForSeconds(Settings.ini.Game.ReloadUrlAfterDelay);
            if (canvasWebViewPrefab != null && canvasWebViewPrefab.WebView != null)
            {
                if (videoPaths!=null && videoPaths.Count>0 && IsWebUrl(videoPaths[videoindex]))
                {
                    LoadUrlWhenReady(videoPaths[videoindex]);
                }
            }
        }
        yield return null;
    }

    private Texture2D lastTextureRef;

    /// <summary>
    /// 图片/视频 组件加遮罩
    /// </summary>
    /// <param name="mg"></param>
    public void SetMask(MaskableGraphic mg, bool needreverse = false)
    {
        if (shaderMask != mg.material.name)
        {
            mg.material = new Material(Shader.Find(shaderMask));//替换为可加遮罩的material
        }
    }

    /// <summary>
    /// 暂停或停止视频的时候停止轮播图片
    /// </summary>
    private bool imgstopped = false;

    public void Stop()
    {
        if (PlayingPlayer)
        {
            PlayingPlayer.Rewind(true);
            //PlayingPlayer.Stop();
        }
        imgstopped = true;
        curImgSec = 0f;
    }

    /// <summary>
    /// none one all
    /// </summary>
    /// <param name="loopMode"></param>
    public void SetLoopMode(LoopMode loopMode)
    {
        //PlayerPrefs.SetString(LOOPMODE, loopMode.ToString());
        Settings.ini.Game.LoopMode = loopMode.ToString();
        if (PlayingPlayer != null)
        {
            PlayingPlayer.m_Loop = (loopMode == LoopMode.one);
        }
        if (LoadingPlayer != null)
        {
            LoadingPlayer.m_Loop = (loopMode == LoopMode.one);
        }
    }

    public String GetLoopMode()
    {
        //return PlayerPrefs.GetString(LOOPMODE, LoopMode.all.ToString());
        return Settings.ini.Game.LoopMode;
    }

    /// <summary>
    /// 屏保设置
    /// </summary>
    /// <param name="filename">短文件名</param>
    public void SetScreenSaver(String filename)
    {
        //PlayerPrefs.SetString(SCREENSAVER, filename);
        Settings.ini.Graphics.ScreenSaver = filename;
    }

    public String GetScreenSaver()
    {
        //return PlayerPrefs.GetString(SCREENSAVER, "");
        return Settings.ini.Graphics.ScreenSaver;
    }

    private void OnDestroy()
    {
        if (LoadingPlayer)
        {
            LoadingPlayer.Events.RemoveListener(OnVideoEvent);
        }
        if (PlayingPlayer)
        {
            PlayingPlayer.Events.RemoveListener(OnVideoEvent);
        }
    }

    Coroutine imgCroutine;
    /// <summary>
    /// 检查是否以 http:// 或 https:// 开头
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    static bool IsWebUrl(string path)
    {
        // 检查是否以 http:// 或 https:// 开头
        return path.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
    }
    /// <summary>
    /// 根据index播放指定视频
    /// </summary>
    /// <param name="videoindex"></param>
    public void OpenVideoByIndex(int videoindex, bool reload = true, bool imgstopped = false)
    {
        if (videoPaths == null || videoPaths.Count == 0)
        {
            this.videoindex = 0;
            return;
        }

        if (videoindex < 0 || videoindex >= videoPaths.Count)
        {
            videoindex = 0;
        }
        this.videoindex = videoindex;
        this.imgstopped = imgstopped;
        VideoChanged?.Invoke(this.videoindex);
        if (videoindex < videoPaths.Count)
        {
            if (autoResetCroutine != null)
            {
                StopCoroutine(autoResetCroutine);
                autoResetCroutine = null;
            }
            //如果videoPaths[videoindex]是网页路径
            if (IsWebUrl(videoPaths[videoindex]))
            {
                
                LoadUrlWhenReady(videoPaths[videoindex]);
                Stop();
            }
            else
            {
                
                if (File.Exists(videoPaths[videoindex]))
                {
                    this.videoindex = videoindex;

                    if (FileUtils.IsImgFile(videoPaths[videoindex])) //播放图片文件
                    {
                        if (imgCroutine != null)
                        {
                            StopCoroutine(imgCroutine);
                            imgCroutine = null;
                        }
                        imgCroutine = StartCoroutine(asyncLoadImg(videoPaths[videoindex]));
                        if (PlayingPlayer && PlayingPlayer.Control != null && !isMovPaused())
                        {
                            PlayingPlayer.Control.Pause();
                        }
                    }
                    else if (FileUtils.IsMovFile(videoPaths[videoindex]))//播放视频文件
                    {
                        if (imgCroutine != null)
                        {
                            StopCoroutine(imgCroutine);
                            imgCroutine = null;
                        }
                        LoadingPlayer.m_VideoPath = videoPaths[videoindex];
                        OpenVideoFile(videoPaths[videoindex], reload);
                    }
                    SetMask(maskDisplay, videoindex);
                }
                else
                {
                    Debug.Log("文件不存在:" + videoPaths[videoindex]);
                    //videoPaths.RemoveAt(videoindex);
                }
            }
        }
    }

    async void LoadUrlWhenReady(string url)
    {
        // Get a reference to the CanvasWebViewPrefab.
        // https://support.vuplex.com/articles/how-to-reference-a-webview
        if (canvasWebViewPrefab == null)
        {
            canvasWebViewPrefab = GameObject.Find("CanvasWebViewPrefab").GetComponent<CanvasWebViewPrefab>();

        }

        // Wait for the prefab to initialize because its WebView property is null until then.
        // https://developer.vuplex.com/webview/WebViewPrefab#WaitUntilInitialized
        await canvasWebViewPrefab.WaitUntilInitialized();
        canvasWebViewPrefab.WebView.LoadProgressChanged -= OnLoadProgressChanged;
        canvasWebViewPrefab.WebView.LoadProgressChanged += OnLoadProgressChanged;
        canvasWebViewPrefab.WebView.PageLoadFailed -= OnLoadFailed;
        canvasWebViewPrefab.WebView.PageLoadFailed += OnLoadFailed;
        canvasWebViewPrefab.WebView.LoadUrl(url);
    }
    void OnLoadProgressChanged(object sender, ProgressChangedEventArgs e)
    {
        Debug.Log("加载进度: " + e.Progress);
    }
    async void OnLoadFailed(object sender, EventArgs e)
    {
        Debug.LogError("网页加载失败");
        await Task.Delay(5000); // 延迟5秒再重试
        canvasWebViewPrefab.WebView.Reload();
    }
    private void SetMask(MaskableGraphic grath, int videoindex)
    {
        if (enablemask)
        {
            string fileName = Path.GetFileNameWithoutExtension(videoPaths[videoindex]);

            //从字符串包含"(no)" 中 取出no
            int start = fileName.IndexOf('(');
            int end = fileName.IndexOf(')');

            string maskid = "";
            if (start != -1 && end != -1 && end > start)
            {
                maskid = fileName.Substring(start + 1, end - start - 1);
            }


            if (int.TryParse(maskid, out int maskindex) && maskindex < maskTextures.Count)
            {
                grath.material.SetTexture("_FilterTex", maskTextures[maskindex]);
            }
        }
    }

    public void OpenVideoByFileName(String filename, bool imgstopped = false)
    {
        int index = -1;
        for (int i = 0; i < videoPaths.Count; i++)
        {
            var name = Path.GetFileName(videoPaths[i]);
            if (name.Equals(filename, StringComparison.OrdinalIgnoreCase))
            {
                index = i;
                break;
            }
        }
        if (index != -1) //在播放列表中
        {
            OpenVideoByIndex(index, true, imgstopped);
        }
        else //不在播放列表中 
        {
            
            string screensaverfile = Path.Combine(persistentDataPath, GetScreenSaver());
            if (File.Exists(screensaverfile)) //屏保图片存在
            {
                if (imgCroutine != null)
                {
                    StopCoroutine(imgCroutine);
                }
                imgCroutine = StartCoroutine(asyncLoadImg(screensaverfile));
                if (PlayingPlayer && PlayingPlayer.Control != null && !isMovPaused())
                {
                    PlayingPlayer.Control.Pause();
                }
            }
        }
    }

    private void DisableImg(RawImage rawimg, DisplayUGUI mediaDisplay)
    {
        if (rawimg != null)
        {
            rawimg.gameObject.SetActive(false);
        }
        if (mediaDisplay != null)
        {
            mediaDisplay.color = new Color(1, 1, 1, 1);
        }
    }

    IEnumerator AutoReset()
    {
        yield return new WaitForSeconds(autoResetTime);
        Stop();
        PlayScreenSaver();
    }

    // Callback function to handle events
    public void OnVideoEvent(MediaPlayer mp, MediaPlayerEvent.EventType et, ErrorCode errorCode)
    {
        switch (et)
        {
            case MediaPlayerEvent.EventType.ReadyToPlay:
                break;

            case MediaPlayerEvent.EventType.Started:
                DisableImg(rawimage, _mediaDisplay); //开始播视频才隐藏图片
                if (rawimagebinds != null)
                {
                    for (int i = 0; i < rawimagebinds.Count; i++)
                    {
                        DisableImg(rawimagebinds[i], _mediaDisplay_binds[i]);
                    }
                }
                break;

            case MediaPlayerEvent.EventType.FirstFrameReady:
                //有画面了再显示
                SwapPlayers();
                LoadVolumn();
                //SetVolumn(PlayerPrefs.GetFloat(VOLUMN_KEY, 1f));//这时候设置音量
                break;

            case MediaPlayerEvent.EventType.FinishedPlaying:
                Debug.Log("视频播放停止事件:" + mp.m_VideoPath);

                if (LoopMode.none.ToString().Equals(GetLoopMode()))
                {
                    if (autoResetTime > 0)
                    {
                        //等待自动重置视频
                        autoResetCroutine = StartCoroutine(AutoReset());
                        return;
                    }
                }

                if (LoopMode.all.ToString().Equals(GetLoopMode())) //列表循环播放
                {
                    if (videoPaths != null && videoPaths.Count > 0)
                    {
                        if (videoindex == videoPaths.Count - 1)//雷达触发文件夹比如0.00文件夹内视频播放完
                        {
                            if (OncePlayFolder != null)
                            {
                                radarPlaying = false;
                                OncePlayFolder.Invoke();
                                break;
                            }
                        }
                        //videoindex = (++videoindex) % videoPaths.Count;

                        //OpenVideoByIndex(videoindex);
                        PlayNext();
                    }
                }
                else if (LoopMode.one.ToString().Equals(GetLoopMode())) //单个循环
                {
                    OpenVideoByIndex(videoindex);
                }
                break;
        }
    }

    #region 兼容图片

    private RawImage rawimage;
    private List<RawImage> rawimagebinds = new List<RawImage>();



    private RawImage InstantiateImg(DisplayUGUI displayUGUI)
    {
        Transform displaytransform = displayUGUI.transform;
        RawImage newrawimage = null;
        Transform mediatransform = displaytransform;
        if (mediatransform.childCount >= 1)
        {
            newrawimage = mediatransform.GetChild(0).GetComponent<RawImage>();
        }
        else
        {
            newrawimage = Instantiate(RawImagePre, mediatransform);
        }

        newrawimage.uvRect = displayUGUI.uvRect;

        return newrawimage;
    }


    private System.Collections.IEnumerator asyncLoadImg(string imgfile)
    {
        rawimage = InstantiateImg(_mediaDisplay);

        bool success = false;

        //文件读取流
        FileStream filestream = new FileStream(imgfile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        filestream.Seek(0, SeekOrigin.Begin);

        //创建文件长度缓冲区
        byte[] bytes = new byte[filestream.Length];

        success = false;

        //读取文件
        Loom.RunAsync(() =>
        {
            filestream.Read(bytes, 0, (int)filestream.Length);
            //释放文件读取流
            filestream.Close();
            filestream.Dispose();
            success = true;
        });

        while (!success)
        {
            //等待异步读取文件
            yield return new WaitForEndOfFrame();
        }

        ShowImg(rawimage, _mediaDisplay, bytes, false);

        SetMask(rawimage, videoindex);


        if (rawimagebinds != null)
        {
            for (int i = 0; i < rawimagebinds.Count; i++)
            {
                ShowImg(rawimagebinds[i], _mediaDisplay_binds[i], bytes, false);
            }
        }

        curImgSec = 0;
        float deltaTime = 0.1f;
        while (curImgSec < imgSeconds)
        {
            if (imgstopped)
            {
                break;
            }
            curImgSec += deltaTime;
            yield return new WaitForSeconds(deltaTime);
        }

        if (!imgstopped && LoopMode.all.ToString().Equals(GetLoopMode())) //图片不停止轮播 并 开启列表循环
        {
            Debug.Log("图片播放完成事件!");
            OnVideoEvent(PlayingPlayer, MediaPlayerEvent.EventType.FinishedPlaying, ErrorCode.None);
        }
    }

    private void ShowImg(RawImage rawimage, DisplayUGUI displayugui, byte[] bytes, bool hasUserConfig, AspectRatioFitter.AspectMode aspectMode = 0)
    {
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(bytes);
        if (rawimage != null && rawimage.texture != null)
        {
            Destroy(rawimage.texture);//防止内存泄露
        }
        rawimage.texture = texture;

        //控制比例
        float ratio = (float)texture.width / texture.height;
        rawimage.GetComponent<AspectRatioFitter>().aspectRatio = ratio;
        float rawimageBoxRatio = transform.GetComponent<RectTransform>().rect.size.x / transform.GetComponent<RectTransform>().rect.size.y;

        if (hasUserConfig && (int)aspectMode >= 0 && (int)aspectMode <= 4)
        {
            //有配置文件 并且配置的AspectMode在[0-4]范围内
            if (!rawimage.GetComponent<AspectRatioFitter>().enabled)
            {
                rawimage.GetComponent<AspectRatioFitter>().enabled = true;
            }
            if (rawimage.GetComponent<AspectRatioFitter>().aspectMode != aspectMode)
            {
                //根据配置来设置图片缩放或者自适应
                rawimage.GetComponent<AspectRatioFitter>().aspectMode = aspectMode;
            }
        }
        else
        {   //没有配置文件 或者 配置文件的AspectMode为-1等无效值
            if (Mathf.Abs(rawimageBoxRatio - ratio) < ratioDelta * rawimageBoxRatio)
            {
                //比例适合拉伸
                if (!rawimage.GetComponent<AspectRatioFitter>().enabled)
                {
                    rawimage.GetComponent<AspectRatioFitter>().enabled = true;
                }
                if (rawimage.GetComponent<AspectRatioFitter>().aspectMode != AspectRatioFitter.AspectMode.None)
                {
                    //根据配置来设置图片缩放或者自适应
                    rawimage.GetComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.None;
                    //动态修改 RectTransform 的Left，Top，Right和Bottom值
                    rawimage.GetComponent<RectTransform>().offsetMin = new Vector2(0.0f, 0.0f);
                    rawimage.GetComponent<RectTransform>().offsetMax = new Vector2(0.0f, 0.0f);
                }
            }
            else
            {
                //比例如果拉伸会变形 只能长宽适应窗口
                if (!rawimage.GetComponent<AspectRatioFitter>().enabled)
                {
                    rawimage.GetComponent<AspectRatioFitter>().enabled = true;
                }
                if (rawimage.GetComponent<AspectRatioFitter>().aspectMode != AspectRatioFitter.AspectMode.FitInParent)
                {
                    //根据配置来设置图片缩放或者自适应
                    rawimage.GetComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                }
            }
        }
        SetBlack(rawimage, displayugui);


    }

    private void SetBlack(RawImage rawimage, DisplayUGUI displayUGUI)
    {
        if (rawimage != null)
        {
            rawimage.gameObject.SetActive(true);

            if (displayUGUI != null)
            {
                displayUGUI.color = new Color(0, 0, 0, 0);
                if (displayUGUI.transform.parent != null && displayUGUI.transform.parent.parent != null)
                {
                    //显示图片的时候 将板块底板改为透明
                    var img = displayUGUI.transform.parent.parent.GetComponent<Image>();
                    if (img != null && hideWhiteImgWhenShowImg)
                    {
                        img.enabled = false;
                    }
                }
            }
        }
    }

    #endregion 兼容图片
}

#endif
