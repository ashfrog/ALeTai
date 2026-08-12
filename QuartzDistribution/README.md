# 高纯石英资源分布互动程序

面向阿勒泰展厅墙面触控大屏的 Unity 互动展示项目。通过“全球 → 全国 → 新疆 → 阿勒泰”四级地图叙事，展示高纯石英资源分布、矿点信息和重点区域详情。

## 项目状态

- Unity：2022.3.62f3
- 目标设备：65–86 英寸 4K 横向触控一体机
- 推荐分辨率：3840×2160；兼容 1920×1080
- 展示方式：本地资源、全屏 Kiosk、可持续运行
- 当前仓库同时包含正式展项场景、热点标注功能和可重建的演示场景

## 目录

| 路径 | 说明 |
| --- | --- |
| `Assets/Scenes/QUARTZ3D.unity` | 主要石英资源展示场景 |
| `Assets/Scenes/FlatHotspotDemo.unity` | 平面热点标注演示场景 |
| `Assets/Scenes/SampleScene2.unity`、`SampleScene2 1.unity` | 开发/验证场景 |
| `Assets/HotspotAnnotations/` | 热点预制体、运行时脚本、编辑器工具和测试 |
| `Assets/StreamingAssets/` | `ObjectData.xml`、`Config.xml` 等运行时配置 |
| `Assets/Plugins/MarkerDetect.dll` | MarkerDetect 运行库 |

## 设计内容

### 四级地图板块

1. **全球**：六大洲代表矿区与资源类型概览。
2. **全国**：中国各类石英资源、分布省区和统计数据。
3. **新疆**：新疆资源类型、重点矿区及阿勒泰跳转入口。
4. **阿勒泰**：13 个伟晶岩型矿点和 2 个脉石英型矿点的详情。

### 交互约定

- 底部固定 Tab 切换四个板块。
- 地图标注点支持点击、悬停/按下和列表联动高亮。
- 详情卡展示矿区名称、类型、储量、品位、位置和备注等信息。
- 支持全屏地图、重置视图、页面淡入淡出和标注点生长动画。
- 60 秒无操作后可自动轮播；任意触控或鼠标操作会停止待机播放。

## 在 Unity 中运行

1. 使用 Unity Hub 打开本目录，选择 `2022.3.62f3`。
2. 打开 `Assets/Scenes/QUARTZ3D.unity` 查看主要展项，或打开 `FlatHotspotDemo.unity` 验证热点功能。
3. 点击 Play。正式检测依赖 `Assets/Plugins/MarkerDetect.dll` 与 `Assets/StreamingAssets/` 配置。
4. 首次运行或更换设备后，检查 Canvas、触控输入、屏幕分辨率和显示器排列。

### 重建平面热点演示

在 Unity 菜单选择 **Tools > Quartz Distribution > Rebuild Flat Hotspot Demo**。该工具会重新生成 `Assets/Scenes/FlatHotspotDemo.unity`，适合在修改预制体或连线布局后使用。

热点功能的详细说明见 [`Assets/HotspotAnnotations/README.md`](Assets/HotspotAnnotations/README.md)。

## 运行时数据

生产环境由 MarkerDetect 填充 `ObjectDetect.mObjectDic`。每个对象通过 `mObjectID` 匹配标注点；`Start` 和 `Move` 显示标注组，`End` 与 `Undetect` 隐藏标注组。

开发阶段可在 `MarKActions` 中启用模拟数据，以验证坐标转换、跟随、连线生长和淡入淡出，而无需连接检测 DLL。

示例数据结构：

```json
{
  "id": "G1",
  "section": "global",
  "name": "Spruce Pine",
  "type": "granite-pegmatite",
  "position": { "lon": -82.0, "lat": 35.9 },
  "data": {
    "reserves": "约 1,000 万吨",
    "grade": "SiO₂ > 99.99%"
  }
}
```

## 视觉与性能基线

- 深蓝科技风：背景约为 `#0a1e3a`–`#0e2750`，选中态使用金色 `#f5b942`。
- 中文建议使用思源黑体或微软雅黑；英文/数字建议使用 Rajdhani、Orbitron 或 DIN。
- 首次加载 ≤ 3 秒，板块切换响应 ≤ 300 毫秒，目标帧率 ≥ 60 fps。
- 地图、标注点和配置尽量本地化；异常时降级为静态展示。

## 已知事项

- `EditorBuildSettings.asset` 当前未配置正式构建场景，请在发布前确认构建入口。
- 地图与矿点数据中的储量、品位等内容属于展陈资料，更新时应同步校对素材与 JSON/XML 配置。
- 生产部署需要根据现场设备配置开机自启动、全屏、禁用系统切换和多点触控策略。
