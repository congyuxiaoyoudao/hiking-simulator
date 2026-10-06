# 项目说明与开发约定

Unity 6（6000.3.7f1）旅途原型，使用 URP3D。以当前代码、场景和配置为准；早期圆环行走方案已改为“直线旅途、最终圆环全景”。

## 当前玩法

开始 → 第 1 站首块 → 自动逐块行走 → 投放点选择操作 → 继续前行 → 自动换站 → 第 6 站完成 → 圆环全景。

- 默认预建 6 站，每站固定 12 块，按 4 块 × 3 组排列。每组第 2 块为投放点（全站第 2、6、10 块），第 4 块为灰蓝色过渡块（全站第 4、8、12 块）。过渡块仍属于本站，具有本站主题的初始水分。
- 每站独立随机选择雪山（白）、草原（绿）、沙漠（黄）；每块独立水分，默认分别为 4、6、2。地块下方显示水分，白框内数字仅表示已投种子数量。
- 正交镜头显示整站，不滚轮平移。旧的“回到赤狐”按钮已隐藏。狐狸抵达倒数第二块后镜头开始推进，按剩余站内移动与站间移动的时长分配进度；抵达下一站首块时镜头恰好前进一站宽度（12 块），狐狸在左侧。投放决策和全景预览期间暂停推进，最后一站镜头保持固定。
- 开始界面的模式按钮在“默认模式／开发快速”之间切换，显示各自地块移动与站间时长。默认选中默认模式（地块间 5 秒、站间 8 秒）；开发快速为地块间 0.5 秒、站间 1.5 秒。模式在开始旅途时确定，运行中不能切换，返回开始保留选择。
- 点击开始后，狐狸从第一块立即开始首步；每相邻块默认移动 5 秒，抵达普通块立即衔接下一步，不再休息。玩家不再通过点击地块移动狐狸。
- 抵达投放点后停止自动前进，先弹出“投放／不投放”；选择投放才打开材料与数量界面。支持水、种子，数量可加减或直接输入，并限制在库存范围内。
- 确认投放 N 份后，库存扣 N；水让当前地块水分 +N，种子让当前投放点种子数 +N，然后立即继续行走。选择不投放也立即前行；材料面板“返回”回到是否投放选择，不扣库存。右键/Esc 在材料面板返回，在选择面板跳过投放。
- 决策期间不会自行前进；一个投放点完成本次操作后立即前进，不重复弹窗。库存不足不能确认。底部材料栏只显示库存。
- 抵达末块后自动换站，不再要求最短驻留时间或离站确认。换站使用独立的站间移动时间，让镜头有更长时间推进；到达下一站首块立即继续。最后一站抵达末块后自动进入原有圆环全景。
- 全景预览暂停自动移动，退出后续接；投放弹窗打开时不可预览。返回开始清理运动及投放状态。
- 狐狸沿用橙色方块占位；尚无生态演化或存档。

## 结构与入口

| 路径 / 类型 | 职责 |
| --- | --- |
| `Assets/Scenes/GameScene.unity` | 主场景：GameRoot、相机、Traveler、Canvas |
| `Assets/Scripts/Journey` | 运行时程序集 `Hiking.Journey` |
| `GameFlowController` / `JourneySession` | 连续行走和投放决策协调 / 站点状态、库存和投放记录 |
| `TravelerController.Progress01` | 0～1 的旅途进度，按每段时长推进 |
| `MapProvider.CreateMap` / `RingMapAssembler.Build` | 地图来源 / 直线 Patch 装配；沿用旧名称，不再调用 Ring3DMeshBuilder |
| `JourneyMap` / `RouteGeometry` | 路线坐标、网格归环及显示切换 |
| `CameraController` | 整站固定视口、站间推进、投影衔接和全景预览 |
| `GameUIController` | Canvas 的按钮事件、动态文字与面板显隐 |
| `Assets/Arts/Prototype` | 图形、字体、材质与历史网格资源 |
| `Packages/com.farlocus.locus`、`Locus` | 编辑器插件及知识目录 |

## 调整位置

- `JourneyConfig.asset` 的“镜头从倒数第几块开始” `cameraTransitionStartFromEnd` 默认 2，范围 1～12；1 表示最后一块，2 表示倒数第二块。

- `JourneyConfig.asset` 的“地图与赤狐下移比例” `journeyVerticalOffset` 默认 0.06，即画面高度的 6%；正值下移、负值上移，可运行时调整。相机沿屏幕上方向偏移，保持地图、狐狸、水分标签一起下移，站间过渡保持偏移；不改变游戏坐标或 Canvas 界面。

- `Assets/Settings/JourneyConfig.asset`：“每块移动时间（秒）” `tileMoveSeconds` 默认 5；“快速移动时间（秒）” `fastTileMoveSeconds` 默认 0.5；独立的“默认模式站间移动时间（秒）” `stationTransitionSeconds` 默认 8、“快速模式站间移动时间（秒）” `fastStationTransitionSeconds` 默认 1.5。时间下限为 0.1 秒。旧休息参数保留序列化值但隐藏且不再使用。
- `Assets/Settings/RingMapSettings.asset`：站点数量（6）、每站路线长度（12）、过渡地块颜色、三种主题颜色、初始水分、标签字体及全景尺寸。地块排布固定 12 块，不再以旧 Patch 数量配置生成。
- `GameScene/Canvas/PlacementDecision`：是否投放选择；`PlacementPrompt`：材料和数量面板，包含数量加减与 InputField。布局保存在场景，事件在 `GameUIController.Initialize` 注册。
- 旧 `StationExitPrompt`、离站按钮及“回到赤狐”按钮保留场景引用但隐藏，不参与自动流程。
- Canvas 填充条显示全程路程进度；顶部显示当前站点、地块及行走／换站／投放等待状态。
- `GameRoot/CameraController`：原有全景高度、俯角和过渡参数。

## 扩展与维护约定

- UI 布局、静态文字和 Button OnClick 直接在 Canvas 编辑，逻辑写在独立脚本；不恢复运行时 UI 生成代码。提示文字引用允许为空。
- `Canvas/WorldTransitionSnapshot` 是场景中保存的 RawImage，默认关闭且不接收射线；运行时仅分配快照纹理。退出或中断过渡须清理纹理、隐藏该层、恢复默认投影及狐狸偏移。
- PCG 使用路线坐标 `(沿线距离 X, 高度 Y, 横向偏移 Z)`。通过 `JourneyMap.RegisterRouteMesh` 注册网格、`RegisterRouteObject` 注册装饰，归环计算由地图统一处理。
- 注册网格的顶点使用路线坐标，网格对象相对地图根节点不加额外变换；运行时网格归地图持有并销毁。变形不自动更新额外的 MeshCollider，目前全景无物理交互。
- 保留场景、配置及脚本的 GUID 和现有按钮绑定；不要用早期资源或旧方案覆盖用户的新修改。
- 主 Unity 编辑器可能正在运行。编译与 PlayMode 验证使用 `.utmp/JourneyValidation` 隔离项目，不打断主编辑器；不要恢复用户已删除的旧测试目录。
- 隔离 PlayMode 验证源为 `.utmp/JourneyValidation/Assets/RouteValidation/RouteTests.cs`，结果 `Logs/early-camera-tests.xml`（9 项通过，含倒数第 1／2／3 块启动镜头及抵站恰好平移 12 块）。覆盖 12 块排布与颜色、固定整站镜头、连续行走和独立站间时长、两层投放决策、数量与库存边界、18 个投放点及六站自动完成、全景暂停恢复、重开清理，以及开始界面模式切换和实际时长。截图为 `Logs/automatic-station.png`、`Logs/automatic-placement-choice.png`、`Logs/automatic-quantity.png`。
