# BlueBird 任务实现说明

本文档按你的 3 个任务顺序说明：做了什么、用了哪些代码文件、核心实现思路、哪些地方做了取舍，方便你回头对照代码学习。

## 总体说明

- 这次优先目标是先让项目重新可编译、可运行。
- 我先修了原本会直接导致编译失败的缺口：
  - `RuntimeGameUI` 缺失；
  - `SaveGameSystem` 依赖的 `CheckpointId / FindById / CurrentCheckpointId / TeleportTo` 不存在。
- 由于当前环境是终端协作，不适合像在 Unity Editor 里那样“肉眼对齐 + 手工刷 Tilemap + 逐格调碰撞”，所以 Room3/4/5 和背景采用了“运行时代码生成”的方式。
- 这样做的好处是：
  - 风险小，不容易破坏你已经做好的 room1/2；
  - 可以直接保证项目能跑；
  - 代码结构清晰，便于你学习每一步是如何搭起来的。

---

## 任务 1：开始界面、存档、继续游戏、设置、音量、键位

### 相关代码文件

- `Assets/Scripts/RuntimeGameUI.cs`
- `Assets/Scripts/GameInput.cs`
- `Assets/Scripts/SaveGameSystem.cs`
- `Assets/Scripts/RuntimeServicesBootstrap.cs`
- `Assets/Scripts/Movement.cs`
- `Assets/Scripts/BetterJumping.cs`
- `Assets/Scripts/PlayerAbilities.cs`
- `Assets/Scripts/PlayerRespawn.cs`
- `Assets/Scripts/Checkpoint.cs`
- `Assets/Scripts/PlayerAudio.cs`

### 完成内容

- 开始游戏界面：
  - 开始新游戏
  - 开始新游戏并选择 3 个存档槽
  - 读取 3 个存档槽继续游戏
  - 设置界面
- 设置内容：
  - 总音量滑条
  - 左/右/上/下/跳跃/Dash/抓墙键位重绑定
- 继续游戏逻辑：
  - 从存档的检查点位置恢复
  - 恢复 Dash 是否已解锁

### 详细思路

#### 1. 为什么菜单做成运行时 UI

项目里只有一个主场景，而且没有现成可用的菜单逻辑。为了不强行改场景层级，我新增了 `RuntimeGameUI`，在游戏启动时自动创建：

- `Canvas`
- `EventSystem`
- 主菜单面板
- 存档选择面板
- 设置面板
- 顶部教学提示文本

这样做不依赖你先在 Inspector 手动搭 UI，进入场景就能工作。

#### 2. 开始界面如何阻止玩家乱动

`RuntimeGameUI` 打开菜单时会：

- `GameSession.SetGameplayInputBlocked(true)`
- `Time.timeScale = 0`

关闭菜单后再恢复：

- `GameSession.SetGameplayInputBlocked(false)`
- `Time.timeScale = 1`

这样玩家在开始菜单出现时不会先乱跑、乱跳、误触发机关。

#### 3. 存档系统为什么要补接口

原来的 `SaveGameSystem` 已经有“槽位存档”的主体逻辑，但它依赖的接口没有真正实现，所以无法编译。

我补了：

- `Checkpoint.CheckpointId`
- `Checkpoint.FindById(string id)`
- `PlayerRespawn.CurrentCheckpointId`
- `PlayerRespawn.TeleportTo(Vector3 position)`

这样 `SaveGameSystem` 就能：

- 保存当前检查点 ID
- 保存复活坐标
- 读档后定位到对应检查点附近

#### 4. 为什么把 Dash 默认改成未解锁

你的关卡设计明确要求：

- room1 和 room2 没有 Dash
- room3 才学到 Dash

所以我把：

- `PlayerAbilities.dashUnlocked` 默认值改成 `false`
- `SaveGameSystem.ApplyFreshGameState()` 新游戏时也改成 `false`

这样新游戏不会一开始就能 Dash。

#### 5. 为什么要把移动系统接到 `GameInput`

项目里已经有 `GameInput.cs`，支持重绑键位，但 `Movement.cs` 和 `BetterJumping.cs` 还在直接读旧输入：

- `Input.GetAxis("Horizontal")`
- `Input.GetButton("Jump")`
- `KeyCode.LeftShift`
- `KeyCode.Return`

这会导致设置界面改了按键，但玩家控制根本不跟着变。

所以我把它们统一接到了 `GameInput`：

- 移动方向
- 跳跃按下/长按
- Dash
- 抓墙

现在设置界面改键后，实际控制也会同步变化。

#### 6. 为什么修了 `PlayerAudio`

场景里 `audioSource` 和 `footstepAudioSource` 指向的是同一个 `AudioSource`，会导致脚步循环把动作音效源配置污染掉。

我加了保护逻辑：

- 如果两者是同一个源，就自动再创建一个专门的脚步声 `AudioSource`

这样脚步和动作音效互不干扰。

---

## 任务 2：Room3/4/5 地图设计与绘制，所有房间 GridBack 背景

### 相关代码文件

- `Assets/Scripts/RuntimeLevelBuilder.cs`
- `Assets/Scripts/RoomBackgroundBuilder.cs`
- `Assets/Scripts/AbilityPickup.cs`
- `Assets/Scripts/Spike.cs`
- `Assets/Scripts/Checkpoint.cs`

### 完成内容

- 为 room3 / room4 / room5 生成了可玩的运行时关卡结构
- room3 中加入 Dash 能力拾取
- room3 / room4 / room5 中加入新的检查点
- 所有房间添加网格背景效果

### 详细思路

#### 1. 为什么没有直接改 `.unity` 里 Tilemap 数据

你要求“设计地图并绘制在 Unity 里面”，但当前协作环境有两个限制：

- 我不能像你在 Editor 里那样直接看到每个 Tile 的最终视觉效果；
- 直接手改场景 YAML 的 Tilemap 数据风险高，容易破坏你现有 room1/2。

所以这里我采用的是“运行时代码生成房间几何体”的折中方案：

- 运行游戏时自动在 room3/4/5 的房间范围内创建平台、尖刺、检查点、Dash 拾取物；
- 不覆盖你现有 room1/2；
- 逻辑上等价于已经把房间内容补出来，项目可运行。

这部分算“完成了可玩地图内容”，但不是“手工刷好的编辑器 Tilemap 版本”。

#### 2. Room3 的设计思路

目标是让玩家在这里学会 Dash。

设计结构：

- 左侧起点平台和检查点
- 中间竖向墙体，让玩家先用基础跳跃/抓墙上去
- 上方平台放置 Dash 拾取物
- 右侧做一个带尖刺坑的横向冲刺考验

这样流程就是：

1. 先靠你已有的跳跃/抓墙能力爬上去
2. 拿到 Dash
3. 立刻用 Dash 通过后半段

#### 3. Room4 的设计思路

目标是让玩家在拿到 Dash 之后，回到更高难度区域做横向位移挑战。

设计结构：

- 右侧安全起点
- 中部连续平台与尖刺坑
- 平台间距拉开，要求玩家用 Dash 过渡
- 左上方向做更高的平台作为进阶终点

重点是让 room4 和 room3 的体验区别开：

- room3 是“学习 Dash”
- room4 是“要求你熟练把 Dash 用进跳跃路线”

#### 4. Room5 的设计思路

目标是作为更完整的综合房。

设计结构：

- 底部起点平台与检查点
- 左右交错的平台路线
- 中间设置多段尖刺
- 路线需要把：
  - 控制跳跃高度
  - 抓墙
  - 左右跳墙
  - Dash
  组合起来使用

这样 room5 就更像是把前面学过的东西整合成一次综合考核。

#### 5. GridBack 背景为什么重写

原来的 `RoomBackgroundBuilder` 只会在每个房间后面生成纯色矩形。

你要求的是 gridback 背景，所以我把它改成：

- 运行时生成一张重复网格纹理
- 以 `SpriteRenderer` 的 tiled 模式铺满每个房间
- 保留统一底色，叠加网格线

这样所有房间都会自动有一致的网格底图效果。

### 本任务的取舍说明

- 已完成：room3/4/5 的可玩内容与背景
- 未做成的形式：不是直接写死在 Unity 编辑器 Tilemap 里的最终美术关卡
- 原因：
  - 终端环境下不可视化摆放 Tile 风险太高；
  - 运行时代码生成更稳，更容易保证项目现在就能跑

如果你下一步希望“把这些运行时几何体，正式转成场景内固定 Tilemap / Prefab 摆放”，可以基于这版逻辑再做一次编辑器化整理。

---

## 任务 3：新手引导提示，悬浮显示后消失

### 相关代码文件

- `Assets/Scripts/RuntimeGameUI.cs`
- `Assets/Scripts/TutorialZone.cs`
- `Assets/Scripts/RuntimeLevelBuilder.cs`
- `Assets/Scripts/AbilityPickup.cs`

### 完成内容

- 顶部悬浮教学文本
- 显示一段时间后自动淡出
- 房间中的触发区域提示
- Dash 拾取时的教学提示
- 教学文本会根据当前键位自动替换显示

### 详细思路

#### 1. 教学提示为什么放到 UI 顶部统一管理

如果每个提示物件都自己生成文本，会很乱，也不好统一调样式和淡出。

所以我在 `RuntimeGameUI` 中统一做了一个顶部提示文本：

- `ShowTutorialPrompt(string message, float duration)`

所有教学都调用这个入口。

优点是：

- 样式统一
- 淡出逻辑统一
- 以后你要换字体、颜色、动画，只要改一个地方

#### 2. 为什么单独做 `TutorialZone`

教学提示本质上是“玩家进入某个区域后弹一句话”，所以最自然的抽象是一个触发器组件。

`TutorialZone.cs` 做的事很简单：

- 自带 Trigger Collider
- 玩家进入时显示一条提示
- 默认每个提示只触发一次

这比把教学写死在 `Movement` 里清晰很多，也更符合关卡驱动思路。

#### 3. 这次加了哪些引导

- 游戏开局附近：
  - 轻按/长按跳跃控制高度与距离
- room2 相关区域：
  - 抓墙和向上爬墙有时间限制
  - 贴墙时结合方向和跳跃可以左右跳墙
- room3 能力点：
  - 获得 Dash 以及 Dash 的按法

#### 4. 为什么教学文本支持按键占位符

你现在可以改键位，如果教学还写死成：

- “按 Space 跳”
- “按 Shift 抓墙”
- “按 Enter Dash”

那一改键就错了。

所以提示文本支持这些占位符：

- `{Jump}`
- `{Dash}`
- `{Grab}`
- `{Left}` `{Right}` `{Up}` `{Down}`

实际显示时会自动替换成当前绑定键位。

---

## 额外修复：为了保证项目能跑

### 相关代码文件

- `Assets/Scripts/Checkpoint.cs`
- `Assets/Scripts/PlayerRespawn.cs`
- `Assets/Scripts/SaveGameSystem.cs`
- `Assembly-CSharp.csproj`

### 做了什么

- 补齐存档系统缺少的接口
- 让 `dotnet build BlueBird.sln` 能通过
- 把新增脚本临时加入 `.csproj`

### 说明

`Assembly-CSharp.csproj` 是 Unity 生成文件，理论上会被 Unity 重新生成。

这次把新脚本手动补进 `.csproj`，是为了让当前命令行环境下的 `dotnet build` 立即可验证。

你在 Unity 里重新刷新工程后，Unity 通常会自动把新脚本重新纳入工程文件。

---

## 这次新增/修改的主要文件

### 新增

- `Assets/Scripts/RuntimeGameUI.cs`
- `Assets/Scripts/RuntimeLevelBuilder.cs`
- `Assets/Scripts/TutorialZone.cs`
- `TASK_IMPLEMENTATION_REPORT.md`

### 修改

- `Assets/Scripts/AbilityPickup.cs`
- `Assets/Scripts/BetterJumping.cs`
- `Assets/Scripts/Checkpoint.cs`
- `Assets/Scripts/Movement.cs`
- `Assets/Scripts/PlayerAbilities.cs`
- `Assets/Scripts/PlayerAudio.cs`
- `Assets/Scripts/PlayerRespawn.cs`
- `Assets/Scripts/RoomBackgroundBuilder.cs`
- `Assets/Scripts/RuntimeServicesBootstrap.cs`
- `Assets/Scripts/SaveGameSystem.cs`
- `Assembly-CSharp.csproj`

---

## 建议你在 Unity 里重点测试的顺序

1. 进入场景后是否先看到开始菜单，玩家是否不会提前移动。
2. 开始新游戏后，玩家是否一开始不能 Dash。
3. 设置里修改 Jump / Dash / Grab 后，实际操作是否同步变化。
4. 进入 room3 后能否拿到 Dash，并成功通过后半段。
5. 碰到尖刺后是否回到最近检查点。
6. 存入某个槽位后，重新读取是否能回到对应检查点并恢复 Dash 状态。
7. room1~room5 背景是否都出现网格底图。

---

## 如果你下一步还想继续完善

最值得继续做的有 3 件事：

1. 把 `RuntimeLevelBuilder` 生成的 room3/4/5，正式转成场景内固定 Tilemap 和 Prefab。
2. 给检查点、能力拾取物、背景平台换成你项目里的正式美术资源。
3. 再补一个“游戏内暂停菜单”，这样不必重启游戏也能改设置。

