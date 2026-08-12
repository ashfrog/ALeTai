# 石英模型检测识别程序

基于 Unity 的 MarkerDetect 识别与双屏热点面板演示项目。程序读取 `MarkerDetect.dll` 输出的对象状态，在 Canvas 上显示跟随热点、扫描环、放射状连线和信息面板。

## 项目状态

- Unity：2022.3.62f3
- 默认演示场景：`Assets/Scenes/MarkerPanelDemo.unity`
- 运行库：`Assets/MarkerDetect.dll`
- 配置：`Assets/StreamingAssets/Config.xml`、`ObjectData.xml`
- 目标显示：支持独立屏幕及横向拼接坐标；示例默认按 3840×2160 单屏、7680×2160 拼接屏设计

## 目录

| 路径 | 说明 |
| --- | --- |
| `Assets/Scripts/MarKActions.cs` | 从 `ObjectDetect.mObjectDic` 发布 Start/Move/End/Undetected 事件 |
| `Assets/Scripts/MarkerPanelPresenter.cs` | 坐标转换、热点跟随、面板淡入淡出 |
| `Assets/Scripts/RadialPanelLines.cs` | 放射状连线与生长动画 |
| `Assets/Scripts/MarkerDetectSimulationDriver.cs` | 开发阶段注入模拟检测数据 |
| `Assets/Prefabs/MarkerPanelGroup.prefab` | 热点面板组预制体 |
| `Assets/Editor/MarkerPanelDemoBuilder.cs` | 重建演示场景的编辑器工具 |
| `Assets/Scenes/MarkerPanelDemo.unity` | 双屏面板演示场景 |

## 在 Unity 中运行

1. 用 Unity Hub 打开本目录，选择 `2022.3.62f3`。
2. 打开 `Assets/Scenes/MarkerPanelDemo.unity` 并点击 Play。
3. 检查 Canvas 的 Target Display、现场分辨率和显示器排列。
4. 生产运行时确认 DLL 与 `StreamingAssets` 配置文件已随构建发布。

### 重建演示场景

选择 **Tools > Marker Detect > Rebuild Panel Growth Demo**。工具会生成包含多个对象 ID、面板卡片和模拟驱动器的 `MarkerPanelDemo.unity`。

## 数据流

```text
MarkerDetect.dll
    ↓
ObjectDetect.mObjectDic (mObjectID → DetectObjectDetails)
    ↓
MarKActions  ── Start/Move/End/Undetected ──>  MarkerPanelPresenter
                                                   ↓
                                      连线、扫描环、信息卡片
```

- `Start`：首次检测到对象，显示面板并播放连线生长。
- `Move`：更新位置和可选旋转。
- `End`：结束跟踪并隐藏面板。
- `Undetect` 或字典中不存在 ID：淡出并隐藏面板。

## 开发模拟

`MarkerDetectSimulationDriver` 可注入 `Start → Move → End → Undetect` 循环。可在 Inspector 调整对象 ID、起点、移动偏移、各阶段时长和是否循环，也可通过组件 Context Menu 单独模拟各状态。

## 坐标与多屏

启用 `useCombinedDisplayCoordinates` 时，检测坐标按整块拼接屏左上角为原点；`displayIndex`、`combinedDisplayWidth/Height` 和 `singleDisplayWidth` 决定目标 Canvas 的转换。`MultiDisplayActivator` 会在独立程序启动时激活已连接的扩展屏。

## 已知事项

- 演示场景包含模拟驱动器；发布前应关闭模拟或移除模拟对象。
- DLL 是外部二进制依赖，若检测不到对象，优先检查 DLL、配置文件、摄像头/识别设备和 `mObjectID` 是否一致。
- 面板资源位于 `Assets/UI/`，替换图片时需同步检查 Canvas 尺寸和连线锚点。
