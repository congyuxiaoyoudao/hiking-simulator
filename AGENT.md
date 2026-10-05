# 项目说明与开发约定

Unity 6（6000.3.7f1）旅途原型，使用 URP3D。以当前代码、场景和配置为准；早期圆环行走方案已改为“直线旅途、最终圆环全景”。

## 当前玩法

开始 → 出生点 0% 等待点击 → 第 1 站驻足 → 恢复行动点 → 手动前往下一站 → 最后一次出发到第 6 站末尾（100%）→ 透视全景 → 返回开始。

- 旅途中正交视窗固定宽 1 站。地图预建等长的中性色起点段和各色站点段；驻足时视窗恰好覆盖当前站。出生到首站及站间出发时地图各左移 1 站长；最后一次出发时镜头保持第 6 站，狐狸走到该站末尾。
- 狐狸屏幕横向位置始终等于总进度（0% 左端、100% 右端）。起点道路与狐狸左边缘贴屏幕；狐狸向内补偿半个自身宽度，避免裁切。
- 出生不自动行走，首次出发无需等待；到站后恢复行动点，上限 1，满后仍需点击出发。行走时间与行动点等待时间独立。
- 默认 `stopPositions = 0.5` 时，出生点、各站和终点的狐狸屏幕进度等间隔；6 站时每次增加约 14.3%。`stopPositions` 可单独提前或推迟某站驻足，实体驻足点随进度映射到对应站路线坐标。每站包含多个独立 Patch；Patch 数量不改变站数。
- 直线和全景圆环共用占位颜色，`RingMapSettings.stationColors` 和起点段 `transitionColor` 可编辑；颜色属于网格，运行中不会随相机改变。
- 退出左侧的 Patch 在后台逐段归入 XZ 圆环，终点补齐末段并闭合。直线与圆环共享路线数据，使用两套显示网格。
- 全景切换时完整圆环始终存在。先叠化上一帧并平滑过渡投影，让相机在狐狸附近抬升、俯视局部圆环；随后反向螺旋上升一圈，同时逐步扩大视角直至看见整圈。结束后可拖拽旋转、滚轮缩放。
- 出生、行走、驻足时均可预览全景；预览暂停行走和行动点计时。“返回旅途”恢复原进度，支持动画中途退出。
- 当前狐狸为橙色方块、地图为道路占位；三个投放槽仍隐藏。尚无实际随机 PCG、生态演化或存档。

## 结构与入口

| 路径 / 类型 | 职责 |
| --- | --- |
| `Assets/Scenes/GameScene.unity` | 主场景：GameRoot、相机、Traveler、Canvas |
| `Assets/Scripts/Journey` | 运行时程序集 `Hiking.Journey` |
| `GameFlowController` / `JourneySession` | 场景协调 / 独立的状态、行动点、库存和投放记录 |
| `TravelerController.Progress01` | 0～1 的旅途进度，按每段时长推进 |
| `MapProvider.CreateMap` / `RingMapAssembler.Build` | 地图来源 / 直线 Patch 装配；沿用旧名称，不再调用 Ring3DMeshBuilder |
| `JourneyMap` / `RouteGeometry` | 路线坐标、网格归环及显示切换 |
| `CameraController` | 局部滚动、投影衔接、螺旋全景和预览相机 |
| `GameUIController` | Canvas 的按钮事件、动态文字与面板显隐 |
| `Assets/Arts/Prototype` | 图形、字体、材质与历史网格资源 |
| `Packages/com.farlocus.locus`、`Locus` | 编辑器插件及知识目录 |

## 调整位置

- `Assets/Settings/JourneyConfig.asset`：等待时间（演示 3 秒、普通 300 秒、快速 30 秒）、材料库存；默认行走 10 秒，各站可按目的站顺序覆盖，空值/0 使用默认值；最后一次出发默认 10 秒。
- `Assets/Settings/RingMapSettings.asset`：站数、每站长度、Patch 数量、道路厚度、驻足比例、起点颜色和全景尺寸。当前资产为 6 站、每站长 6、每段 18 Patch；加上起点共 7 段、路线总长 42。全景倍率默认 1.5，圆环半径 = 路线总长 / 2π × 倍率。地图配置在新旅途生效。
- `GameScene → GameRoot → CameraController`：局部视窗固定 1 站；`overviewHeight` 直接指定最终全景相机相对环心的高度（默认 30），不再自动抬高；值太小可能使圆环超出画面。`panoramaScreenWidth` 控制最终透视视角，1 对应 50°。`revealStartHeight`、`revealStartOutward`、`revealStartFieldOfView` 控制螺旋起始镜头。画面衔接默认 0.8 秒，螺旋上升默认 6 秒，俯角默认 45°；预览与完成全景共用参数。
- Canvas 的行动点填充条表示出发资格，与狐狸的总旅途进度分开。

## 扩展与维护约定

- UI 布局、静态文字和 Button OnClick 直接在 Canvas 编辑，逻辑写在独立脚本；不恢复运行时 UI 生成代码。提示文字引用允许为空。
- `Canvas/WorldTransitionSnapshot` 是场景中保存的 RawImage，默认关闭且不接收射线；运行时仅分配快照纹理。退出或中断过渡须清理纹理、隐藏该层、恢复默认投影及狐狸偏移。
- PCG 使用路线坐标 `(沿线距离 X, 高度 Y, 横向偏移 Z)`。通过 `JourneyMap.RegisterRouteMesh` 注册网格、`RegisterRouteObject` 注册装饰，归环计算由地图统一处理。
- 注册网格的顶点使用路线坐标，网格对象相对地图根节点不加额外变换；运行时网格归地图持有并销毁。变形不自动更新额外的 MeshCollider，目前全景无物理交互。
- 保留场景、配置及脚本的 GUID 和现有按钮绑定；不要用早期资源或旧方案覆盖用户的新修改。
- 主 Unity 编辑器可能正在运行。编译与 PlayMode 验证使用 `.utmp/JourneyValidation` 隔离项目，不打断主编辑器；不要恢复用户已删除的旧测试目录。
- 最近 8 项隔离 PlayMode 测试通过，结果为 `Logs/equal-progress-tests.xml`。覆盖等间隔进度、起点中性色、一站视窗、各站驻足时的完整站点配色、狐狸屏幕进度、站间等距滑移、末站内走向终点、预览暂停恢复及全景相机衔接。动画观感仍需在正常 Game 视窗确认。

## 本分支开发任务（crm/ui · UI/UX）

- 分支 `crm/ui`，开发场景 `Assets/Scenes/crm-DevScene.unity`（由 GameScene 复制）。
- 2026/10/05 进度：
  - 修复反馈链路。`GameUIController.toast` 原本为 null 且场景中不存在 Toast 对象，`Notify` 全部静默返回，投放、到站、材料不足等提示一条都不会显示。已新增 `Canvas/Toast`（Image + Label，默认隐藏），接线 `toastRoot` / `toast`，提示时长改为可调 `toastSec`（默认 2.5 秒），对应 DEV12。
  - 材料卡。库存为 0 时按钮显示 `theme.materialEmpty` 暗色，选中后提示行显示「该材料库存不足」，按钮仍可选中以便查看，对应 DEV07 / DEV12。
  - 样式表 `Assets/Scripts/Journey/UI/UITheme.cs`。材料按钮配色与提示条背景／文字配色、`toastSec` 集中到 `GameUIController.theme`，运行时由脚本写入；字号与布局仍在 Canvas 中编辑。对应 DEV40 的参数集中要求。
  - 材料栏抽成 Prefab `Assets/Prefabs/MaterialBar.prefab`，承载 `Material_water` / `Material_seed`，是 `Canvas/JourneyPanel/Controls/MaterialBar` 的实例。抽取前后两个按钮的世界坐标完全一致，`GameUIController.materialButtons` 的引用未变。以后材料栏的改动只影响该 Prefab，不再直接改场景，`JourneyPanel` 其余部分（出发按钮、行动点进度条、全景预览按钮）保持原样未动。
  - 开始界面的模式选择由「点一下循环切换」改为三个显式按钮 `Mode_Normal` / `Mode_Quick` / `Mode_Demo`，选中项高亮，下方 `ModeInfo` 显示当前等待时长，对应 DEV36。同时把 `GameUIController.mode` 的默认值从 `Demo` 改为 `Normal`——此前玩家不点按钮直接开始，进的是每站 3 秒的演示模式，而不是 300 秒的普通模式。
  - `ModeInfo` 补上全程时长，由 `MapProvider.ringSettings.blockCount` 与 `Config.WaitSeconds(mode)` 算出，不写死：普通模式显示「每站等待 300 秒 · 全程等待约 30 分钟」，与 DEV06 验收标准的「六站约 30 分钟」一致。
  - 按钮状态反馈统一。`GameUIController.ApplyButtonFeedback` 在初始化时遍历 Canvas 下所有 Button，按 `UITheme` 里的悬停／按下／禁用倍率设置 ColorBlock；出发按钮与材料按钮在不可用时把文字切到 `theme.textDisabled`。以后新增按钮会自动套用，不用逐个改场景。
  - 面板自带脚本化（Kevin 确认合并走 Prefab 之后做的）：新增 `MaterialBarView`（挂在 `Assets/Prefabs/MaterialBar.prefab` 根上）与 `ModeSelectorView`（挂在新增的 `Assets/Prefabs/StartPanel.prefab` 根上）。两个脚本自己持有面板内部的按钮和文字引用，并在 `Bind` 时用代码注册点击。`GameUIController` 不再持有 `materialButtons` / `modeOptions` / `modeText` / `startButton`，改为 `Initialize` 时用 `GetComponentsInChildren<T>(true)` 找到面板并 `Bind`。这样合并到 GameScene 时只需把 Prefab 拖进 Canvas，内部引用跟着 Prefab 走，不需要重新拖十几个引用。
  - 番茄钟的计时部分（DEV31）：新增 `FocusSession`（纯计时状态，用真实时间戳推进，不受模拟倍率、镜头预览暂停和帧率波动影响）与 `FocusTimerView`，控件做成 `Assets/Prefabs/FocusTimer.prefab`，常驻在 Canvas 顶层右上角。它不挂在任何面板下面，所以切换开始界面／旅途／全景都不会中断计时。`focusDurationSec`（默认 1500 秒）与 `refreshSec` 放在控件自己的 Inspector 上，暂时没有放进 `JourneyConfig`，避免再动主程的配置文件。
  - 当前模式角标（DEV36 的「界面明确标记当前模式」）：新增 `ModeBadgeView`，做成 `Assets/Prefabs/ModeBadge.prefab`，常驻 Canvas 左上角（右上角是番茄钟，两边对称）。开始界面隐藏，创建旅程后显示当前模式；快速模式用 `UITheme.quickModeAccent` 的暖色强调，一眼能看出不是普通模式。开关是组件上的 `modeBadgeVisible`（对应 DEV36 的同名参数）。根节点保持激活、只切换子节点显隐——脚本挂在根上，把自己 `SetActive(false)` 之后就再也醒不过来了。

## UI 开发踩过的坑

- **Prefab 资产保存不了对场景对象的引用。** 把 MaterialBar 存成 Prefab 之后，两个材料按钮上原本指向场景里 `GameUIController.SelectMaterial` 的持久化 `onClick` 全部变成 `m_Target: {fileID: 0}`，点击直接失效。表现形式很隐蔽：Inspector 里方法名还在，只是目标为空。**解决办法是点击一律在 Prefab 自己的脚本里用 `onClick.AddListener` 注册**，不要跨 Prefab 边界写持久化引用。同类问题也存在于从场景对象移进 Prefab 的 `StartButton`（已一并处理）。
- 新脚本必须放在 `Assets/Scripts/Journey/` 之内。该目录有 `Hiking.Journey.asmdef`，放到外面的脚本会编译进 `Assembly-CSharp`，`Hiking.Journey` 程序集引用不到，报 CS0234 / CS0246。
- 通过 MCP 新建的 UI 对象默认 `localScale` 是 1.5135（用于抵消画布缩放），必须手动改回 1，否则比同层其它 UI 大 1.5 倍。
- `manage_gameobject` 找不到未激活层级里的对象。修改 `JourneyPanel` 下的内容前要先激活该面板，改完还原。
- Screen Space - Overlay 的界面无法用常规 MCP 截图拿到（走相机渲染会排除 Overlay 层，Scene View 又不渲染 Overlay 的文字）。可行做法：临时把 Canvas 切到 `ScreenSpaceCamera`、挂一个临时相机渲染到 RenderTexture 存 PNG，用完在 `finally` 里还原，且不要保存场景。
- `Text.font` 无法通过 `manage_components` 设置（序列化字段是 `m_FontData.font`，嵌套路径也不支持）。需要指定字体时，从已有 Label 复制一份再改其余属性。
- 批量创建 UI、复制样式、清空 Button 上遗留的持久化 `onClick`、以及给 `modeOptions` 这类自定义数组赋值，用 `execute_code` 跑一段编辑器 C# 比逐个对象改要可靠得多（`m_OnClick.m_PersistentCalls.m_Calls` 只能这样清）。改完记得 `EditorUtility.SetDirty` + `MarkSceneDirty`。
- 按钮监听不要写成持久化 onClick：在 `GameUIController.Initialize` 里用 `onClick.AddListener` 注册，这样公开方法改名或删除时不会在场景里留下丢失的引用。

- 待办：数量加减与「准备投放 N 份」（DEV07）、2 秒撤销窗口（DEV10）、当前环境状态提示（DEV11）、番茄钟与补给（DEV31–33）、`JourneyPanel` 整体 Prefab 化（需先确认合并流程）。
- 待办（续）：`JourneyPanel` 与 `PanoramaPanel` 尚未 Prefab 化；它们里面混有主程的出发按钮、行动点进度条、全景预览按钮，动之前要跟他确认归属。
- 待办（续）：DEV32／33（专注完成生成待领补给、选择材料领取）需要给 `JourneySession` 增加入库方法才能发放奖励，属于主程的核心数据类，要先确认。另外「状态保存」还不存在，待领补给目前只能存在内存里，DEV32 的「重开不重复生成」暂时无法验收。
- 待策划拍板：终点全景目前是圆环（`RingMapSettings` + `CameraController` 螺旋俯视），而需求文档 DEV03 / DEV25 / DEV30 写的是「长卷」，DEV30 还要求导出完整长卷图片。直线与圆环共用同一份路线数据，两种形态切换成本不高。
- 已知问题（未处理）：旅途中上下两栏合计占 33% 屏高（顶部 90 + 底部 150，画布高 727），顶部只放一行状态文字偏胖；两栏是固定像素高度，窗口变矮时占比会上升（1280×600 约 36.5%，1280×400 约 45%），需要给世界区设最小高度，或让两栏随窗口收缩。
