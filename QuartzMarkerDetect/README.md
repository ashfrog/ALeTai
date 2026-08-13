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

## 展开节点自动避让

`MarkerPanelPresenter` 默认只调整叶子面板和分支线，不移动 `MarkerPanelGroup` 根节点。两个可见组的叶子卡片发生覆盖时，系统在虚拟目标位置上进行多轮稳定求解：先对过小的叶子夹角做小幅均分，再按最小重叠方向推开卡片，并将叶子偏移限制在初始位置附近；根节点仍严格跟随 Marker，因此不会因避让产生抖动。

可在每个 `MarkerPanelGroup` 的 Inspector 调整：

- `Auto Arrange Leaves`：启用/禁用叶子自动布局。
- `Equalize Leaf Angles`：启用过小夹角的均分补偿。
- `Minimum Leaf Angle`：同组叶子的最小夹角，默认 44°。
- `Max Leaf Angle Adjustment`：单个叶子的最大角度调整，默认 68°。
- `Leaf Overlap Padding`：叶子卡片之间的最小间距，默认 48 Canvas 单位。
- `Max Leaf Offset`：叶子相对初始位置的最大偏移，默认 260 Canvas 单位。
- `Leaf Layout Speed`：叶子和分支线的平滑调整速度，默认 10。

演示场景构建器会为新生成的 `MarkerPanelGroup.prefab` 写入上述默认值。

## 坐标与多屏

启用 `useCombinedDisplayCoordinates` 时，检测坐标按整块拼接屏左上角为原点；`displayIndex`、`combinedDisplayWidth/Height` 和 `singleDisplayWidth` 决定目标 Canvas 的转换。`MultiDisplayActivator` 会在独立程序启动时激活已连接的扩展屏。

## 已知事项

- 演示场景包含模拟驱动器；发布前应关闭模拟或移除模拟对象。
- DLL 是外部二进制依赖，若检测不到对象，优先检查 DLL、配置文件、摄像头/识别设备和 `mObjectID` 是否一致。
- 面板资源位于 `Assets/UI/`，替换图片时需同步检查 Canvas 尺寸和连线锚点。
