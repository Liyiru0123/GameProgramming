# Movement And Snow Logic

本文档说明当前项目里玩家移动、长按跳跃、贴墙、墙跳、Dash，以及场景下雪效果的实现方式。

主要代码和资源位置：

- 玩家移动主逻辑：[Assets/Scripts/Movement.cs](/c:/AAA-ThirdSem/GameProgramming/Thebluebridindream/BlueBird/Assets/Scripts/Movement.cs)
- 跳跃手感修正：[Assets/Scripts/BetterJumping.cs](/c:/AAA-ThirdSem/GameProgramming/Thebluebridindream/BlueBird/Assets/Scripts/BetterJumping.cs)
- 地面/墙体检测：[Assets/Scripts/Collision.cs](/c:/AAA-ThirdSem/GameProgramming/Thebluebridindream/BlueBird/Assets/Scripts/Collision.cs)
- 输入系统：[Assets/Scripts/GameInput.cs](/c:/AAA-ThirdSem/GameProgramming/Thebluebridindream/BlueBird/Assets/Scripts/GameInput.cs)
- Dash 解锁状态：[Assets/Scripts/PlayerAbilities.cs](/c:/AAA-ThirdSem/GameProgramming/Thebluebridindream/BlueBird/Assets/Scripts/PlayerAbilities.cs)
- 场景里的雪粒子：[Assets/Scenes/SampleScene.unity](/c:/AAA-ThirdSem/GameProgramming/Thebluebridindream/BlueBird/Assets/Scenes/SampleScene.unity)

## 1. 输入层

默认输入在 [GameInput.cs](/c:/AAA-ThirdSem/GameProgramming/Thebluebridindream/BlueBird/Assets/Scripts/GameInput.cs) 里定义：

- 左右移动：`A` / `D`
- 上下输入：`W` / `S`
- 跳跃：`Space`
- Dash：`Enter`
- 抓墙：`LeftShift`

`Movement` 不直接依赖 Unity 旧输入轴，而是优先通过 `GameInput.Instance` 读键位，所以后续改键位时，不需要改移动代码。

## 2. 移动系统整体结构

`Movement.Update()` 每帧的处理顺序基本是：

1. 读取输入
2. 更新跳跃缓冲和土狼时间
3. 更新落地状态
4. 处理水平移动 `Walk`
5. 更新墙体状态 `UpdateWallState`
6. 处理缓冲跳 `HandleBufferedJump`
7. 处理 Dash `HandleDashInput`
8. 更新粒子和朝向

这里的关键点是，地面跳、墙跳、抓墙、Dash 不是互相独立的脚本，而是都集中在 `Movement.cs` 里，由一些状态位协调：

- `canMove`
- `wallGrab`
- `wallJumped`
- `wallSlide`
- `isDashing`
- `hasDashed`

## 3. 地面移动

普通横向移动在 `Walk(Vector2 dir)` 里：

- 如果当前没有抓墙、没有 Dash、并且允许移动，`rb.velocity.x` 会直接被设置成 `dir.x * speed`
- 当前场景里角色 `speed = 7`，配置在 [SampleScene.unity](/c:/AAA-ThirdSem/GameProgramming/Thebluebridindream/BlueBird/Assets/Scenes/SampleScene.unity)

这意味着地面横移是“直接给速度”，不是加速度模型，所以手感偏干脆。

## 4. 为什么长按跳跃会跳得更远

这个效果不是 `Movement.cs` 直接做的，而是 [BetterJumping.cs](/c:/AAA-ThirdSem/GameProgramming/Thebluebridindream/BlueBird/Assets/Scripts/BetterJumping.cs) 在额外修正刚体速度。

### 4.1 起跳瞬间

真正起跳发生在 `Movement.Jump(Vector2 dir, bool wall)`：

- 先把当前竖直速度清零：`rb.velocity = new Vector2(rb.velocity.x, 0f)`
- 再加上 `dir * jumpForce`

地面跳时 `dir` 是 `Vector2.up`，所以相当于给一个向上的初速度。

当前场景参数：

- `jumpForce = 12`

### 4.2 长按和点按的差别

`BetterJumping.Update()` 做了两段额外处理：

1. 当角色正在下落，也就是 `rb.velocity.y < 0` 时：
   - 额外再施加一次向下加速度
   - 用的是 `fallMultiplier`
   - 当前场景值是 `3`

2. 当角色正在上升，而且你已经松开跳跃键，也就是 `rb.velocity.y > 0 && !jumpHeld` 时：
   - 额外再施加一次向下加速度
   - 用的是 `lowJumpMultiplier`
   - 当前场景值是 `8`

结果就是：

- 长按跳跃键：上升阶段不会被这段“提前压下去”的力打断，所以跳得更高
- 轻点跳跃键：角色还在上升时，就会被额外向下拉，形成短跳

用户体感上会觉得“长按跳得更远”，原因不是横向速度变大了，而是：

- 空中停留时间更长
- 同样的水平速度下，抛物线更长
- 所以落点更远

这是一种很常见的平台跳跃手感写法。

## 5. 跳跃缓冲和土狼时间

这两项都在 [Movement.cs](/c:/AAA-ThirdSem/GameProgramming/Thebluebridindream/BlueBird/Assets/Scripts/Movement.cs) 的 `UpdateJumpTimers()` 和 `HandleBufferedJump()` 里。

### 5.1 土狼时间 `coyoteTime`

如果角色刚离开地面的一小段时间内按跳，仍然允许起跳。

当前场景值：

- `coyoteTime = 0.1`

实现方式：

- 在地面上时，`coyoteCounter = coyoteTime`
- 离地后每帧递减
- 只要 `coyoteCounter > 0`，就还能触发普通跳

### 5.2 跳跃缓冲 `jumpBufferTime`

如果角色在落地前很短时间内提前按了跳，落地后会自动接上一次跳跃。

当前场景值：

- `jumpBufferTime = 0.1`

实现方式：

- 按下跳跃时，`jumpBufferCounter = jumpBufferTime`
- 之后每帧递减
- 只要角色满足可跳条件，并且 `jumpBufferCounter > 0`，就会执行跳跃

这两个机制一起解决“明明按了跳却没跳出来”的问题。

## 6. 地面和墙体检测

角色是否在地面、是否贴墙，不是依赖碰撞回调，而是 [Collision.cs](/c:/AAA-ThirdSem/GameProgramming/Thebluebridindream/BlueBird/Assets/Scripts/Collision.cs) 每帧用 `Physics2D.OverlapBox` 检查。

检测内容：

- `onGround`：角色脚下检测盒是否碰到 `groundLayer`
- `onRightWall`：角色右侧检测盒是否碰到墙层
- `onLeftWall`：角色左侧检测盒是否碰到墙层
- `onWall`：左右任意一边贴墙
- `wallSide`：
  - 右墙时是 `-1`
  - 左墙时是 `1`
  - 没贴墙时是 `0`

当前默认检测盒大小：

- `groundCheckSize = (0.5, 0.12)`
- `wallCheckSize = (0.12, 0.7)`

这部分很重要，因为所有贴墙、墙跳、抓墙逻辑，都是基于这些布尔值。

## 7. 贴墙和爬墙逻辑

### 7.1 现在没有“自动缓慢下滑”

当前版本已经移除了“贴墙自动减速下落”的核心移动效果。

也就是说：

- 只要你只是碰到墙，但没有按抓墙键
- 角色不会进入专门的慢速滑墙状态
- 下落仍然按正常重力处理

`wallSlide` 这个字段目前主要是给旧动画/粒子逻辑兼容留着，移动本身已经不再依赖它限制下落速度。

### 7.2 什么时候会进入抓墙

抓墙逻辑在 `UpdateWallState(float verticalInput, bool wallGrabHeld)`：

只有同时满足以下条件才会抓墙：

- `canMove == true`
- 当前不在 Dash
- 角色在空中
- `coll.onWall == true`
- 玩家按住抓墙键

进入抓墙后：

- `wallGrab = true`
- `rb.gravityScale = 0`
- 角色不再被重力下拉
- 朝向会被强制设为背离墙面

### 7.3 抓墙时上下移动怎么做

抓墙时的上下速度由 `GetWallGrabVerticalSpeed(verticalInput)` 决定：

- 按 `W`
  - 如果还有爬墙体力，就返回 `wallClimbSpeed`
  - 同时消耗 `wallClimbTimeRemaining`
- 不按方向
  - 返回 `0`
  - 角色挂在墙上不动
- 按 `S`
  - 返回 `-slideSpeed`
  - 角色主动向下滑

当前场景参数：

- `wallClimbSpeed = 3.5`
- `slideSpeed = 1`
- `maxWallClimbTime = 0.75`
- `wallClimbRecoverRate = 1.25`

### 7.4 爬墙体力怎么恢复

恢复逻辑在 `RecoverWallClimb(float verticalInput)`：

- 落地时，直接回满
- 空中时，如果贴墙，或者没有继续按上爬，就会按 `wallClimbRecoverRate` 缓慢回复

所以它的设计是：

- 向上爬会消耗资源
- 停下来挂墙或重新接触墙面，会慢慢回一点

## 8. 墙跳逻辑

墙跳在 `WallJump(float horizontalInput, float verticalInput)` 里。

### 8.1 墙跳方向是怎么判定的

先决定“离墙方向”：

- 在右墙上，离墙方向是 `Vector2.left`
- 在左墙上，离墙方向是 `Vector2.right`

然后判断你有没有“明确按离墙方向”：

- 左墙时，按右 `D` 算离墙
- 右墙时，按左 `A` 算离墙

还会判断你是否按了上：

- `verticalInput > 0.1` 算按上

### 8.2 向上墙跳

满足以下任一条件就走“向上墙跳”分支：

- 你按了 `W`
- 你没有按离墙方向

这时使用：

- 水平只有很小的离墙推力
- 竖直方向更强

代码里常量是：

- `UpwardWallJumpHorizontalPush = 0.2`
- `UpwardWallJumpVerticalMultiplier = 1.1`

所以像下面这些输入都会主要往上：

- 左墙 + `Jump`
- 左墙 + `W + Jump`
- 左墙 + `A + Jump`

这就是你之前要求的“贴墙可以往上起跳，而不是一定被弹开”。

### 8.3 蹬墙跳开

只有当你明确按了“离墙方向”时，才走斜向蹬墙跳：

- 左墙 + `D + Jump`
- 右墙 + `A + Jump`

这时使用 `Vector2.up + wallDir` 作为起跳方向，所以会明显跳离墙面。

### 8.4 为什么墙跳后不能立刻把自己拉回去

墙跳后会调用 `LockMovement(time)` 短暂锁输入：

- 向上墙跳锁 `0.08` 秒
- 蹬墙跳开锁 `0.1` 秒

这段时间 `canMove = false`，目的是避免：

- 你刚墙跳出去
- 普通横向输入立刻把速度覆盖掉

另外在 `Walk()` 里，`wallJumped == true` 时不会直接硬改速度，而是用 `Vector2.Lerp` 逐渐回到目标速度，强度由 `wallJumpLerp` 决定。

当前场景值：

- `wallJumpLerp = 5`

## 9. Dash 逻辑

Dash 入口是 `HandleDashInput(bool dashPressed, float xRaw, float yRaw)`。

只有同时满足以下条件时才允许 Dash：

- 这一帧按下了 Dash
- `hasDashed == false`
- `PlayerAbilities.DashUnlocked == true`
- 当前有方向输入，不能是 `(0, 0)`

### 9.1 Dash 方向

Dash 使用的是原始输入：

- `A/D/W/S`
- 支持八方向

例如：

- `D + Dash`：向右 Dash
- `W + Dash`：向上 Dash
- `W + D + Dash`：右上 Dash

代码里会先把方向向量归一化，再乘 `dashSpeed`，避免斜向 Dash 比正向更快。

当前场景值：

- `dashSpeed = 40`

### 9.2 Dash 期间发生了什么

在 `Dash(float x, float y)` 和 `DashWait()` 里：

- 先清空当前速度
- 立刻赋予 Dash 速度
- `hasDashed = true`
- `isDashing = true`
- 重力设为 `0`
- 临时禁用 `BetterJumping`
- 普通走路逻辑暂停覆盖速度

同时还会触发一些表现层效果：

- 相机抖动
- Ripple 效果
- GhostTrail
- Dash 粒子
- Dash 音效

Dash 持续约 `0.3` 秒，结束后：

- 恢复重力
- 重新启用 `BetterJumping`
- `isDashing = false`

### 9.3 Dash 什么时候恢复

有两种恢复方式：

1. 正常落地
   - `GroundTouch()` 里直接把 `hasDashed = false`

2. 地面短窗口恢复
   - `GroundDash()` 等待 `0.15` 秒
   - 如果这时角色已经在地面上，也会恢复 Dash

这个短窗口是为了让贴地 Dash 更顺手，不至于很僵硬。

## 10. 当前场景里的核心移动参数

当前 `SampleScene` 中角色主要参数是：

- `speed = 7`
- `jumpForce = 12`
- `slideSpeed = 1`
- `wallClimbSpeed = 3.5`
- `maxWallClimbTime = 0.75`
- `wallClimbRecoverRate = 1.25`
- `wallJumpLerp = 5`
- `dashSpeed = 40`
- `coyoteTime = 0.1`
- `jumpBufferTime = 0.1`
- `fallMultiplier = 3`
- `lowJumpMultiplier = 8`

如果你之后要调手感，优先改这些值，而不是先改逻辑。

## 11. 下雪逻辑

当前的雪不是脚本生成的，而是直接放在场景里的 `ParticleSystem`，名字就叫 `Snow`，在 [SampleScene.unity](/c:/AAA-ThirdSem/GameProgramming/Thebluebridindream/BlueBird/Assets/Scenes/SampleScene.unity) 里。

也就是说：

- 没有单独的 `SnowController.cs`
- 雪的范围、密度、飘动，都是 Unity 粒子系统参数决定的

### 11.1 当前这套雪是怎么构成的

从场景数据看，这个 `Snow` 粒子系统目前大致是：

- `looping = 1`
- `prewarm = 1`
- `playOnAwake = 1`
- `startLifetime = 5`
- `startSpeed = 0.1`
- `startSize = 0.04761905`
- `Emission rateOverTime = 80`
- `Shape radius = 7.09`
- `ForceModule.enabled = 1`
- `ForceModule.x = -3.56`
- `NoiseModule.enabled = 1`
- `Noise strength = 1`
- `Noise strengthY = 1.3`

可以把它理解成：

1. `Emission`
   - 决定每秒生成多少雪粒子

2. `Shape`
   - 决定雪从多大的区域里生成
   - 现在半径大约是 `7.09`

3. `startLifetime`
   - 决定每片雪能活多久
   - 活得越久，就能飘得越远

4. `ForceModule`
   - 给雪一个整体偏移趋势
   - 现在 `x = -3.56`，所以雪会整体往左偏

5. `NoiseModule`
   - 给雪额外抖动和随机漂浮感
   - 不然看起来会太直、太机械

### 11.2 为什么现在雪没有覆盖整个地图

核心原因一般就三个：

1. 发射区域不够大
   - 现在 `Shape radius = 7.09`

2. 粒子寿命不够长
   - 现在 `startLifetime = 5`

3. 粒子系统本身只放在场景固定位置
   - 当前 `Snow` 物体在场景原点附近
   - 如果地图很大，固定发射器覆盖不到所有区域

## 12. 怎么把雪的范围变大到覆盖整个地图

最直接的做法是在 Unity 里选中 `Snow` 粒子系统，然后按下面顺序调。

### 12.1 先改 Shape

优先改 `Shape` 模块的发射范围。

如果你只是想“同一个雪层覆盖更宽的区域”，先把：

- `Shape radius`

调大。

如果你把半径从 `7.09` 调到更大，比如 `20`、`30`，雪会从更宽的横向区域生成。

这是最先该改的参数。

### 12.2 再改 Emission

范围变大后，如果 `rateOverTime` 还是 `80`，单位面积里的雪会变稀。

所以通常要同步把：

- `Emission > Rate over Time`

一起调高。

简单理解：

- 区域变 2 倍大
- 发射数最好也差不多跟着加

不然看起来就像“雪范围大了，但雪量不够”。

### 12.3 再改 Lifetime

如果你希望雪从高处飘下来，能穿过更大的场景高度，就要调：

- `Start Lifetime`

寿命太短时，雪还没飘到下方就消失了。

地图越高，这个值通常越要大。

### 12.4 注意 Max Particles

如果你把发射范围和发射率都加大，记得留意粒子系统的：

- `Max Particles`

当前是 `1000`。

如果雪突然断断续续，很可能是达到上限了。

### 12.5 更推荐的大地图方案

如果你的地图非常大，不建议只靠“一个固定在原地的大粒子系统”硬撑。

更稳的方案有两个：

1. 让 `Snow` 跟着摄像机移动
   - 把 `Snow` 挂到主摄像机下面，或者写脚本让它跟相机位置走
   - 这样你只需要覆盖“当前视野”而不是“整个世界”

2. 分区放多个雪粒子系统
   - 每个大房间/区域一个
   - 镜头进房间时只显示当前区域的雪

对 2D 平台游戏来说，第一种通常更省性能，也更好控制。

## 13. 如果你现在就要手动改雪，建议顺序

1. 在 Unity 里选中 `Snow`
2. 先把 `Shape` 范围调大
3. 再把 `Emission Rate over Time` 调高
4. 如果高度不够，再加 `Start Lifetime`
5. 如果粒子被吃掉，再提高 `Max Particles`
6. 如果地图很大，改成跟随摄像机

## 14. 后续如果你要继续改手感，优先从这些点入手

- 想让长按和短按差别更明显：改 `lowJumpMultiplier`
- 想让下落更干脆：改 `fallMultiplier`
- 想让爬墙更强或更弱：改 `wallClimbSpeed` 和 `maxWallClimbTime`
- 想让墙跳更偏向上或更偏向外：改 `UpwardWallJumpHorizontalPush` 和 `UpwardWallJumpVerticalMultiplier`
- 想让 Dash 更猛或更保守：改 `dashSpeed`
- 想让跳跃容错更宽松：改 `coyoteTime` 和 `jumpBufferTime`

如果你要，我下一步可以继续帮你做两件事里的任意一个：

1. 直接把 `Snow` 改成“跟随相机覆盖整张地图”的实现
2. 再单独写一份更短的“给组员看”的中文操作文档
