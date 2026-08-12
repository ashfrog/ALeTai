# Flat Hotspot Annotations

`HotspotAnnotations` 是 QuartzDistribution 的平面热点标注模块：从 MarkerDetect 读取对象状态，将屏幕坐标转换到 Canvas，驱动扫描环、正交连线和信息卡片的显隐。

## 组成

- `Prefabs/FlatAnnotationGroup.prefab`：推荐的平面标注组预制体。
- `Runtime/MarKActions.cs`：读取 `ObjectDetect.mObjectDic`、跟随坐标并控制显隐。
- `Runtime/OrthogonalLiveLine.cs`：从热点锚点到信息卡的动态正交连线。
- `Runtime/ScanRingPulse.cs`、`ProceduralRingGraphic.cs`：扫描环与程序化图形。
- `Editor/HotspotDemoSceneBuilder.cs`：重建演示场景的编辑器菜单。
- `Tests/`：编辑模式和播放模式测试。

## 快速使用

1. 在 Screen Space Canvas 下实例化 `Prefabs/FlatAnnotationGroup.prefab`。
2. 保留预制体根节点上的 `MarKActions`，并为每组设置唯一的 `mObjectID`。
3. 确认 Canvas 分辨率、渲染模式和 `simulationReferenceResolution` 与现场屏幕一致。
4. 运行时由 MarkerDetect 写入 `ObjectDetect.mObjectDic`：
   - `Start` / `Move`：显示整组并跟随热点；
   - `End` / `Undetect`：淡出并隐藏整组。

`objectCenterPosition` 使用屏幕像素坐标。模块会将其转换为 Canvas 坐标，移动扫描环锚点，并在每帧从该锚点重建连线。

## 开发模拟

在 Inspector 中启用 `simulateTrackingData`：

- `simulatedPosition`：以 `simulationReferenceResolution` 为基准的坐标；
- `simulatedAngle`：热点旋转角度；
- `simulateMotion`：让 DLL 模拟点连续移动，验证连线跟随。

代码也可调用：

```csharp
SetSimulatedTrackingData(position, angle, detected);
PushDllSimulationSample(screenPosition, angle, state);
StopSimulation();
```

模拟数据同样写入 `ObjectDetect.mObjectDic`，因此使用与生产检测相同的读取路径。

## 重建演示场景

打开 `FlatHotspotDemo.unity` 可查看两个模拟跟踪 ID，每个 ID 连接三张固定信息卡。修改预制体或布局后，在 Unity 菜单选择 **Tools > Quartz Distribution > Rebuild Flat Hotspot Demo** 重新生成场景。

## MarkerDetect 接入说明

`Assets/Plugins/MarkerDetect.dll` 及其 `StreamingAssets` 配置来自 QuartzMarkerDetect。当前开发演示使用 `MarKActions` 的 DLL 模拟入口，以绕过供应 DLL 示例 `ObjectDetect` 的注册辅助错误；生产环境可由检测组件正常填充同一个静态字典。
