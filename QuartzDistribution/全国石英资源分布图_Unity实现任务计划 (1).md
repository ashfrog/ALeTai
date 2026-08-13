# 全国石英资源分布图 —— Unity MCP 实现任务计划

> 本文档用于交给 Codex（通过 Unity MCP 直接操作 Unity 编辑器）执行开发任务。
> 参考图：展厅大屏 / 触摸屏交互界面，深蓝科技风格，展示全国石英资源分布数据。

---

## 一、项目概述

在 Unity 2022.3.62f3（**内置渲染管线 Built-in RP**）中实现一块**触控屏**交互式数据可视化大屏，核心展示内容为**水晶矿产分布**，包含：

1. 顶部标题栏（中英文标题 + 科技装饰线条）
2. 顶部右侧 Tab 切换：`全国资源分布` / `阿勒泰资源分布`（两个数据视图切换，非跳场景，建议同场景内容器切换）
3. 左侧「石英资源类型」图例列表（7类资源，可上下翻页/滚动，右上角有分页圆点指示器），**每项为 Toggle 组件，支持多选**
4. 中间/右侧主区域：中国地图（发光线框风格），地图上按省份分布彩色菱形标记点（Marker），标记点颜色与左侧图例类型一一对应
5. **核心交互**：左侧图例项为 Toggle，点击选中某一种（或多种）水晶矿石类型后，地图上对应颜色的 Marker 进入**高亮闪烁**状态（呼吸灯/闪烁动效），未选中类型的 Marker 保持常态或转为暗淡；支持**多选叠加显示**——同时勾选多个类型时，右侧地图区域同时叠加显示这几类矿石的高亮闪烁标记
6. 全触控操作，无需 hover 态（点击/点触即为最终交互态，不依赖鼠标悬停）
7. 一个「锚点坐标」调试浮窗（X/Y px），用于开发阶段在地图图片上标定 Marker 像素坐标，正式运行时可隐藏

---

## 二、视觉与布局分析（基于原型图）

- 整体分辨率参考 1920×1080（16:9 展厅触摸屏 / 电视墙），深色科技背景 `#0A1628` 左右
- 顶部标题栏高度约占屏幕 8%，左侧为不规则科技感色块背景 + 中文大标题 + 英文小标题
- 顶部右侧两个 Tab 按钮，激活态为蓝色高亮胶囊按钮，未激活为深色描边
- 左侧图例面板宽度约占屏幕 25%，纵向卡片列表，每张卡片：
  - 左侧 80×80 缩略图（石材/矿物照片）
  - 右侧：资源名称（加粗白字）+ 描述文字（浅蓝灰小字，可两行）+ 右上角彩色菱形色标
  - 卡片带深蓝描边圆角矩形背景，选中/悬停态左侧竖线高亮
- 主区域：中国地图线框图（发光描边+地名标注），省份名称白字小字标注
- 地图标记点：菱形 Icon，颜色对应 7 类资源色值（见下表），单省份可能有 1~3 个不同颜色标记叠加/并排
- **交互方式（触控屏，非鼠标hover）**：左侧图例每项为 Toggle，勾选后该类型对应的地图 Marker 立即开始**闪烁/呼吸灯高亮**动效；支持多选，多个类型同时勾选时地图上对应的多组 Marker 同时闪烁叠加显示；全部取消勾选时地图恢复默认态（可设计为默认显示全部 Marker 常态，或默认全隐藏，二选一，建议默认**全部常态显示、无选中不闪烁**，勾选后才闪烁强调）
- 左上角「锚点」浮窗：半透明深色卡片，显示 `X: -2639.92 px` / `Y: -2526.43 px`，用于坐标标定调试（正式交付前应可开关隐藏）

### 资源类型与颜色映射表

| 序号 | 资源名称 | 图例色值(建议HEX) | 说明文案示例 |
|---|---|---|---|
| 1 | 石英岩 | `#E8384F` 红 | 全国储量约13.64亿吨 |
| 2 | 石英砂岩 | `#2ED9A0` 绿 | 全国储量约13.62亿吨 |
| 3 | 天然石英砂 | `#F5D033` 黄 | 福建探明约4亿吨 |
| 4 | 脉石英 | `#B24BE0` 紫 | 产地354处，保有约1.63亿吨 |
| 5 | 粉石英 | `#33B5F0` 蓝 | 全国631万吨 |
| 6 | 天然水晶 | `#F03D9E` 品红 | 压电水晶18697kg，熔炼418.70t，工艺水晶52kg |
| 7 | 伟晶岩型高纯石英 | `#2ED9C4` 青绿 | 全球主流高纯石英原料，光伏坩埚、半导体、光纤通信等 |

> 实际色值以设计稿截图吸色为准，Codex 实现时先用取色工具/PS吸色确认，此处仅为估算基准。

---

## 三、技术方案

- Unity 版本：Unity 2022.3.62f3 LTS，**内置渲染管线（Built-in Render Pipeline）**，不引入 URP，与团队现有展项技术栈一致
- UI 方案：uGUI（沿用团队现有习惯，便于后续接入 TouchSocket 等既有通信层）；地图采用 RawImage/Image 承载底图 + 子物体挂载 Marker 预制体，Marker 坐标用 **锚点像素坐标系** 换算为 UI 坐标（RectTransform anchoredPosition）
- 动画：DOTween（卡片选中过渡、Tab 切换淡入淡出、信息卡片弹出动效）
- 数据驱动：所有资源类型、Marker 坐标、地图切换配置均从 JSON 读取，不写死在场景中，便于后续换省份/换数据
- 无需 Modbus/串口通信（本项目为纯展示大屏，不涉及硬件联动），如后续需要联动灯光/边框效果可留接口

---

## 四、数据结构设计（JSON Schema）

### 4.1 资源类型配置 `QuartzResourceTypes.json`

```json
{
  "resourceTypes": [
    {
      "id": "quartzite",
      "name": "石英岩",
      "colorHex": "#E8384F",
      "iconThumb": "thumbs/quartzite.png",
      "description": "全国储量约13.64亿吨"
    },
    {
      "id": "quartz_sandstone",
      "name": "石英砂岩",
      "colorHex": "#2ED9A0",
      "iconThumb": "thumbs/quartz_sandstone.png",
      "description": "全国储量约13.62亿吨"
    }
  ]
}
```

### 4.2 地图标记点配置 `QuartzMapMarkers_National.json` / `QuartzMapMarkers_Altay.json`

```json
{
  "mapId": "national",
  "mapImageSize": { "width": 2668, "height": 1750 },
  "markers": [
    {
      "province": "新疆",
      "resourceTypeId": "quartz_sandstone",
      "anchorPx": { "x": -2639.92, "y": -2526.43 },
      "note": ""
    },
    {
      "province": "山东",
      "resourceTypeId": "quartzite",
      "anchorPx": { "x": 0, "y": 0 },
      "note": ""
    }
  ]
}
```

> `anchorPx` 坐标系与「锚点调试浮窗」实时显示的坐标系保持一致（以地图底图左上/中心为原点，具体原点位置由 Codex 在实现坐标标定工具时确认并在代码注释中写明）。

---

## 五、场景与UI层级结构建议

```
Canvas (Screen Space - Camera / Overlay, 1920x1080 参考分辨率, CanvasScaler-ScaleWithScreenSize)
 ├─ BG_Panel                      // 深蓝背景 + 粒子/网格装饰（可用现成科技风背景图或Shader）
 ├─ Header
 │   ├─ TitleBlock                // 标题+副标题+装饰色块
 │   └─ TabGroup
 │       ├─ Tab_National (Toggle)
 │       └─ Tab_Altay (Toggle)
 ├─ LeftLegendPanel
 │   ├─ LegendTitle ("石英资源类型" + 分页圆点)
 │   └─ ScrollView_LegendList
 │       └─ Content (Vertical Layout Group)
 │           └─ LegendItem (Prefab, 挂载 Toggle 组件) x N   // 缩略图+名称+描述+色标菱形，触控多选，不使用ToggleGroup
 ├─ MapPanel  (屏幕右侧主区域)
 │   ├─ MapImage_National (RawImage, 底图)
 │   ├─ MapImage_Altay (RawImage, 底图, 默认隐藏)
 │   ├─ MarkerLayer_National (Empty RectTransform, 容纳国家地图 Marker 实例)
 │   │   └─ MarkerItem (Prefab) x N        // 菱形Icon，响应多选高亮闪烁，点击可选弹出InfoCard
 │   ├─ MarkerLayer_Altay (默认隐藏)
 │   └─ InfoCardPopup (Marker点击后浮出的详情卡片, 默认隐藏, 可选功能)
 └─ DebugAnchorPanel (锚点坐标调试浮窗，Editor/Debug模式下显示，正式发布可关闭)
```

---

## 六、分阶段开发任务清单（建议Codex按顺序执行，每阶段完成后可验收）

### 阶段 1：基础框架与数据层
- [ ] 创建 `Assets/QuartzMap/` 目录结构（Scripts / Prefabs / Data / Sprites）
- [ ] 定义 C# 数据模型类：`ResourceTypeData`、`MapMarkerData`、`MapConfigData`（对应上方 JSON Schema，使用 `JsonUtility` 或 `Newtonsoft.Json`）
- [ ] 编写 `QuartzDataLoader.cs`：启动时从 `StreamingAssets` 读取并解析 JSON，提供静态数据供其他模块访问
- [ ] 搭建 Canvas 与上述 UI 层级空节点（先用占位色块，不做美术细节）

### 阶段 2：左侧图例列表（Toggle 多选）
- [ ] 制作 `LegendItem` Prefab：整项挂载 `Toggle` 组件（Touch友好，点击区域覆盖整张卡片而非仅色标小图标）+ 缩略图 Image + 名称 Text + 描述 Text + 色标菱形 Image；Toggle 选中态需有明显视觉反馈（卡片描边高亮/背景色变化/左侧竖线高亮）
- [ ] 编写 `LegendListController.cs`：根据 `QuartzResourceTypes.json` 动态生成列表项，支持 ScrollRect 滚动；**不使用 ToggleGroup（保持互不排斥，允许多选）**
- [ ] 实现分页圆点指示器（根据可视条目数量自动生成/更新高亮点）
- [ ] 每个 Toggle 的 `onValueChanged` → 维护一个当前选中类型集合 `HashSet<string> selectedResourceTypeIds`，变化时广播事件 `OnLegendSelectionChanged(selectedResourceTypeIds)`
- [ ] 触控优化：Toggle 命中区域不小于 80×80px（参考展厅触摸屏常见误触阈值），避免手指点击不中

### 阶段 3：地图与Marker系统（核心：多选叠加高亮闪烁）
- [ ] 导入地图底图（线框风格 China 地图 + 阿勒泰局部地图），设置为 RawImage，置于屏幕右侧主区域
- [ ] 制作 `MarkerItem` Prefab：菱形 Icon（按 `colorHex` 动态着色）+ 可选省份标签；预留“常态”与“高亮闪烁态”两套视觉（如：常态低透明度小图标，高亮态放大+外发光+透明度呼吸动画）
- [ ] 编写 `MapMarkerController.cs`：读取对应地图的 `MapMarkers.json`，按 `anchorPx` 坐标实例化 Marker，并做像素坐标→UI锚点坐标的换算
- [ ] 编写 `MarkerHighlightAnimator.cs`：用 DOTween 实现闪烁/呼吸灯效果（建议 Scale 或 Alpha 在 0.6~1.0 之间 `Yoyo` 循环，频率约 0.6~1s 一次），提供 `PlayHighlight()` / `StopHighlight()` 接口
- [ ] `MapMarkerController` 订阅 `OnLegendSelectionChanged` 事件：
  - 未选中任何类型时：所有 Marker 保持默认常态显示（不闪烁）
  - 选中一个或多个类型时：`resourceTypeId` 命中选中集合的 Marker 调用 `PlayHighlight()`，未命中的 Marker 调用 `StopHighlight()` 并可选择性降低透明度以突出对比
  - 多选时**所有命中类型的 Marker 同时闪烁**，实现叠加显示效果（无需额外分层，同一地图内直接按颜色区分即可）
- [ ] （可选，视是否保留详情卡片需求）Marker 点击 → 弹出 `InfoCardPopup`，展示省份+资源类型+说明文案，带 DOTween 淡入缩放动效；触控场景下建议合并到 Marker 点击而非 hover

### 阶段 4：Tab 切换（全国 / 阿勒泰）
- [ ] 编写 `MapViewSwitcher.cs`：控制 Tab 高亮态切换 + MapImage/MarkerLayer 显隐 + 淡入淡出过渡
- [ ] 阿勒泰视图加载独立底图与独立 Marker 数据集（`mapId: "altay"`）
- [ ] 切换时左侧图例是否联动刷新（若阿勒泰资源类型不同，需支持图例数据集也随 Tab 切换）— **待与用户确认阿勒泰视图的图例是否复用同一套7类还是单独一套**

### 阶段 5：锚点坐标调试工具
- [ ] 编写 `AnchorDebugTool.cs`：鼠标/触摸在地图区域移动时，实时计算并显示 `X / Y px` 坐标（与 Marker 坐标系一致）
- [ ] 支持点击地图直接复制当前坐标到剪贴板或 Console 输出，方便美术/策划标定 Marker 位置后填入 JSON
- [ ] 提供开关（Inspector 勾选或 F 快捷键）控制该调试浮窗显示/隐藏，正式发布时默认关闭

### 阶段 6：整体美术还原与细节打磨
- [ ] 顶部标题栏科技装饰色块、发光边框效果（Shader/图片皆可）
- [ ] 卡片圆角描边、悬停/选中态视觉反馈统一
- [ ] 地图发光线框效果（可用 Outline Shader 或预制发光素材图）
- [ ] 整体自适应不同分辨率展厅屏（CanvasScaler 匹配宽高）

### 阶段 7：联调与验收
- [ ] 全流程点击测试：图例筛选 ↔ Marker 高亮 ↔ 信息卡片 ↔ Tab切换
- [ ] 长时间挂机稳定性检查（复用团队现有 Watchdog 机制，若该大屏也需 7×24 运行）
- [ ] 导出 Windows 单机可执行程序，供展厅设备部署测试

---

## 七、Unity MCP 使用注意事项（给 Codex）

- 优先通过 Unity MCP 提供的编辑器操作接口（创建 GameObject、挂载组件、设置 RectTransform、创建 Prefab、导入资源）完成场景搭建，减少手写 `.unity`/`.prefab` 文本文件直接编辑带来的解析风险
- 每完成一个阶段，先在 Unity 编辑器内实际运行 Play 模式验证效果，再进入下一阶段
- JSON 数据文件放入 `Assets/StreamingAssets/QuartzMap/`，保证 Build 后也能读取
- 颜色统一从 JSON 的 `colorHex` 读取并用 `ColorUtility.TryParseHtmlString` 转换，不要在 Prefab 里手动写死颜色，便于后续改色
- 命名规范延续团队现有习惯（Pascal命名C#类，Prefab用中文或拼音+英文后缀均可，与现有项目保持一致）

---

## 八、需求确认情况

**已确认：**
- ✅ 触控屏，全程点触操作，无鼠标 hover 交互
- ✅ 核心展示内容为**水晶矿产（石英/水晶类）分布**
- ✅ 左侧图例为 **Toggle 多选**，选中类型 → 地图对应 Marker **高亮闪烁**
- ✅ 多选时地图**叠加显示**多个类型的高亮闪烁 Marker

**仍需确认：**
1. 阿勒泰资源分布视图的具体数据内容（图例类型是否与全国视图一致，还是有独立的阿勒泰专属矿种）？
2. 是否仍需要 Marker 点击弹出详情信息卡片（InfoCardPopup），还是仅保留“勾选类型→闪烁高亮”这一种交互，不再需要点击查看详情？
3. 是否需要自动轮播/无人操作时的待机动画（参考团队现有数字人待机状态机经验），例如无人触摸N秒后自动循环演示各类型高亮？
4. 地图底图美术资源（发光线框中国地图）由谁提供，是否已有素材文件？
5. 屏幕分辨率/尺寸参数（触摸屏具体型号与分辨率，便于 CanvasScaler 与触控命中区域适配）？

---

*文档生成时间：2026-08-13，配合 Unity MCP + Codex 分阶段执行使用。*
