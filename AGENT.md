# 项目说明与开发约定

Unity 6（6000.3.7f1）旅途原型，使用 URP3D。以当前代码、场景和配置为准；早期圆环行走方案已改为“直线旅途、最终圆环全景”。

## 当前玩法

开始 → 出生点 0% 等待点击 → 第 1 站驻足 → 恢复行动点 → 手动前往下一站 → 最后一次出发到 100% → 透视全景 → 返回开始。

- 旅途中为局部正交视窗，默认可见约 1.25 站。地图向左滚动，旧区域从左侧退出，新区域从右侧进入。
- 狐狸屏幕横向位置对应总进度。起点道路与狐狸左边缘贴屏幕；狐狸向内补偿半个自身宽度，避免裁切。旅途贴边计算不使用旧的 `routeScreenWidth` 边距。
- 出生不自动行走，首次出发无需等待；到站后恢复行动点，上限 1，满后仍需点击出发。行走时间与行动点等待时间独立。
- 每站默认驻足在本站中点，可配置比例。每站包含多个独立 Patch；Patch 数量不改变站数。
- 退出左侧的 Patch 在后台逐段归入 XZ 圆环，终点补齐末段并闭合。直线与圆环共享路线数据，使用两套显示网格。
- 全景切换先叠化上一帧画面，并平滑过渡投影，再反向螺旋上升一圈；相机始终看向环心，结束后可拖拽旋转、滚轮缩放。
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

- `Assets/Settings/JourneyConfig.asset`：等待时间（演示 3 秒、普通 300 秒、快速 30 秒）、材料库存；默认行走 10 秒，各站可按目的站顺序覆盖，空值/0 使用默认值；终点段默认 10 秒。
- `Assets/Settings/RingMapSettings.asset`：站数、每站长度、Patch 数量、道路厚度、驻足比例和全景尺寸。当前资产为 6 站、每站长 6、每站 18 Patch；全景倍率默认 1.5，圆环半径 = 路线总长 / 2π × 倍率。地图配置在新旅途生效。
- `GameScene → GameRoot → CameraController`：`visibleStations` 控制局部范围；`panoramaScreenWidth` 仅控制全景构图。画面衔接默认 0.8 秒，螺旋上升默认 6 秒，全景最低高度默认 30，俯角默认 45°。预览与完成全景共用参数；地图较大时自动抬高以容纳整圈。
- Canvas 的行动点填充条表示出发资格，与狐狸的总旅途进度分开。

## 扩展与维护约定

- UI 布局、静态文字和 Button OnClick 直接在 Canvas 编辑，逻辑写在独立脚本；不恢复运行时 UI 生成代码。提示文字引用允许为空。
- `Canvas/WorldTransitionSnapshot` 是场景中保存的 RawImage，默认关闭且不接收射线；运行时仅分配快照纹理。退出或中断过渡须清理纹理、隐藏该层、恢复默认投影及狐狸偏移。
- PCG 使用路线坐标 `(沿线距离 X, 高度 Y, 横向偏移 Z)`。通过 `JourneyMap.RegisterRouteMesh` 注册网格、`RegisterRouteObject` 注册装饰，归环计算由地图统一处理。
- 注册网格的顶点使用路线坐标，网格对象相对地图根节点不加额外变换；运行时网格归地图持有并销毁。变形不自动更新额外的 MeshCollider，目前全景无物理交互。
- 保留场景、配置及脚本的 GUID 和现有按钮绑定；不要用早期资源或旧方案覆盖用户的新修改。
- 主 Unity 编辑器可能正在运行。编译与 PlayMode 验证使用 `.utmp/JourneyValidation` 隔离项目，不打断主编辑器；不要恢复用户已删除的旧测试目录。
- 最近 7 项隔离测试通过，结果为 `Logs/smooth-transition-tests.xml`，覆盖旅途、贴边、预览暂停恢复、投影首帧、淡出、绕圈和浏览接管。动画观感仍需在正常 Game 视窗确认，测试通过不等于视觉上完全顺滑。
