# QuartzMicroView

显微镜展项的 Unity 播放与控制程序：在多触控屏上播放显微镜视频，支持上一条/下一条切换、音量与循环控制，并可切换到显微镜摄像头实时画面。项目同时提供 TCP、UDP 和 HTTP 控制接口，便于展厅中控或远程管理。

## 项目状态

- Unity：2022.3.62f3
- 主要场景：`Assets/Scenes/MicroViewer.unity`
- 播放服务场景：`Assets/Scenes/MediaPlayerServer.unity`
- 触控屏：3 块，单块 1280×800（以现场配置为准）
- 显微镜摄像头：目标输入 3840×2160
- 视频播放：AVPro Video；UI 使用 uGUI/TextMeshPro

## 目录

| 路径 | 说明 |
| --- | --- |
| `Assets/Scripts/LitVCR.cs` | 播放列表、视频切换、音量、循环和屏保 |
| `Assets/Scripts/MicroViewerController.cs` | 上一条/下一条/摄像头模式控制 |
| `Assets/Scripts/MicroscopeCameraDisplay.cs` | 摄像头枚举、采集、旋转和镜像 |
| `Assets/Scripts/TCPUDPServer.cs` | TCP/UDP 远程播放控制，默认端口 4848 |
| `Assets/Scripts/SimpleHttpServer.cs` | HTTP 文件管理与控制，默认端口 8080 |
| `Assets/CodeUtils/ConfigManager/` | `settings.ini` 配置读写 |
| `客户端demo/` | UDP 控制客户端示例 |
| `小工具/CRC_16/` | CRC-16 Windows 小工具 |
| `Assets/Scenes/` | 播放服务和显微镜查看器场景 |

## 在 Unity 中运行

1. 用 Unity Hub 打开本目录，选择 `2022.3.62f3`。
2. 打开 `Assets/Scenes/MicroViewer.unity`，点击 Play 验证触控界面和摄像头页。
3. 若需单独运行视频服务，打开 `Assets/Scenes/MediaPlayerServer.unity`。
4. 在 `settings.ini` 中配置媒体目录、屏保、音量、循环模式和摄像头参数；配置文件由运行时自动创建/更新。

## 媒体文件

`LitVCR` 从 `Settings.ini.Path.MediaPath` 读取媒体目录。目录不存在时会回退到：

```text
Application.streamingAssetsPath/媒体文件
```

将视频、屏保和可选的 `playlist` 文件放入该目录后，启动时会建立播放列表。HTTP 文件接口也以该目录作为管理根目录。

## 远程控制

### TCP / UDP

`TCPUDPServer` 默认同时监听 TCP 和 UDP `4848` 端口，命令格式为 `Command|Data`。常用命令：

| 命令 | 示例 | 说明 |
| --- | --- | --- |
| `PlayVideo` | `PlayVideo|*0` 或 `PlayVideo|文件名` | 播放指定索引/文件；无参数时继续播放 |
| `PauseVideo` / `StopVideo` | `PauseVideo` | 暂停或停止并播放屏保 |
| `PlayNext` / `PlayPrevious` | `PlayNext` | 切换相邻视频 |
| `VideoSeek` | `VideoSeek|0.5` | 按 0–1 比例跳转 |
| `SetVolumn` / `GetVolumn` | `SetVolumn|0.8` | 设置/读取音量 |
| `Loop` / `GetLoop` | `Loop|all` | `none`、`one` 或 `all` |
| `FileList` / `GetPlayInfo` | `FileList` | 获取文件列表或播放状态 |
| `Help` | `Help` | 返回命令帮助 |

### HTTP

`SimpleHttpServer` 默认监听 `8080`，提供 `/control`、`/filelist`、`/upload`、`/delete`、`/rename` 等接口。具体参数以 `Assets/Scripts/SimpleHttpServer.cs` 的路由实现为准。

## 发布检查

- 确认 `AVProVideo` 的 x86/x86_64 原生库随构建发布。
- 确认媒体目录可读写，屏保文件名与 `settings.ini` 一致。
- 确认 TCP/UDP 4848、HTTP 8080 未被防火墙拦截；只在受信任的展厅网络开放。
- 确认摄像头设备名、分辨率、FPS、旋转和镜像参数。
- 独立程序退出时会停止网络监听；重新进入场景前检查端口是否已释放。

## 已知事项

- `TCPUDPServer` 与 `SimpleHttpServer` 属于局域网控制接口，当前未提供鉴权，不应直接暴露到公网。
- 远程命令和媒体文件名使用 UTF-8；客户端示例位于 `客户端demo/`。
- 仓库内包含第三方插件和若干历史解决方案文件，构建时以 Unity 项目配置及当前场景为准。
