# CS2_Unlimited_Vehicle_Better_Death

两个**互相独立**的《都市：天际线 II》模组，共用这一个仓库。

| 模组 | 目录 | 做什么 | 状态 |
|---|---|---|---|
| **VehicleCapUnlock** | 仓库根（`./`） | 提高公共交通线路面板「配车数量」滑块上限，可选放大车库容量 | 安全审计 F1–F6 已整改，**未完成游戏内存档验收** |
| **DeathcareTune** | [`DeathcareTune/`](DeathcareTune/) | 殡葬调优：火化速度、灵车数量、尸体存放容量 | 已构建 + 换算回归通过，**未做游戏内验收** |

两者不共享任何代码、不依赖彼此，各自编译出独立 DLL，可以单独安装、单独启用、单独卸载。
同时启用不会冲突：它们改的是完全不同的数据（前者是线路 policy 与车库容量，后者是 `DeathcareFacilityData` prefab）。

---

## 仓库结构

```text
CS2_Unlimited_Vehicle_Better_Death/     ← 本机路径 Mods\VehicleCapUnlock
├─ VehicleCapUnlock.csproj              ← 模组 1（仓库根）
├─ Mod.cs  Setting.cs
├─ Core/  Systems/  tests/  scripts/
├─ docs/  website/
├─ DeathcareTune/                       ← 模组 2
│  ├─ DeathcareTune.csproj
│  ├─ Mod.cs  Setting.cs
│  ├─ Core/  Systems/  tests/  scripts/
│  └─ README.md
├─ .gitignore  .gitattributes  global.json
└─ .tools/                              ← 便携 SDK 与 NuGet 缓存，不入 Git
```

`VehicleCapUnlock.csproj` 已显式 `<Compile Remove="DeathcareTune\**\*.cs" />`，避免父项目的默认 glob
把子模组源码一起编进同一个 DLL。改动 csproj 后用元数据扫描确认父 DLL 内不含 `DeathcareTune` 类型。

## 构建

两个模组都是 net48 + C# 9，共用仓库根的便携 SDK，互不影响。

```powershell
# 模组 1：仓库根执行
.\scripts\build.ps1                 # 仅构建
.\scripts\build.ps1 -Test           # 构建 + 数学/安全回归
.\scripts\build.ps1 -Deploy         # 正常退出游戏后部署

# 模组 2：进入子目录执行
cd DeathcareTune
.\scripts\build.ps1 -Configuration Release -Test -Deploy
```

- 便携 SDK 8.0.428 位于 `.tools/dotnet`，不修改系统 PATH。
- 两个构建脚本都会在 Cities2 进程存在时拒绝 `-Deploy`，默认只构建不部署。
- `DeathcareTune` 的 dotnet 解析顺序：`$env:CSII_DOTNET` → 本工程 `.tools\dotnet` → 仓库根 `.tools\dotnet` → 系统 dotnet。
- 游戏引用路径默认 `Cities2_Data/Managed`，其他安装位置用 `-ManagedPath` 指定。

## 部署目标

源码在仓库里，游戏实际加载的是用户数据目录下两个独立文件夹：

```text
%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Mods\VehicleCapUnlock\
%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Mods\DeathcareTune\
```

游戏 DLL、SDK、NuGet 缓存、编译产物均不纳入 Git。

## 获取

```text
https://github.com/Nanako-Arasaka/CS2_Unlimited_Vehicle_Better_Death.git
```

> 本仓库原名 `CS2_Unlimited_PubilcVehicle`（`Public` 拼写错误），改名后旧链接由 GitHub 自动重定向。

---

# 模组 1：VehicleCapUnlock

为《都市天际线 II》提高原版线路面板「配车数量 / Assigned Vehicles」滑块的上限。
每条线路仍然独立调节；全局选项只决定可调上限，不会把所有线路的当前配车数一起乘倍数。

## 当前验证状态（2026-11-17）

- **安全审计 F1–F6 已完成代码整改，仍未通过游戏内存档验收。** 修复版用于测试副本，详见[修复说明](docs/SAFETY_FIXES.md)和[原始审计记录](docs/SAFETY_AUDIT.md)。
- 已对本机游戏程序集完成 Release 编译：0 警告、0 错误。
- 数学回归程序通过 167,670 项断言。
- 安全回归程序通过 3,647 项断言，覆盖故障注入、回滚、写入归属、容量预算和客货设施关系。
- 尚未在游戏内验收滑块、实际出车、跨城市加载和卸载还原。
- 目标覆盖公交、有轨电车、地铁、火车、船、轮渡、飞机。共享 policy 路径和车库白名单已实现，7 类交通的运行效果仍需逐项实测。
- 不使用 Harmony，不替换原版策略系统，不在每帧持续写线路数据。

## 目录

本机工程目录（也是仓库根）：

```text
E:\SteamLibrary\steamapps\common\Cities Skylines II\Mods\VehicleCapUnlock
```

这是源码/构建目录，部署目标是上面的 `...\Mods\VehicleCapUnlock`。

原始交接工程仍保留在 E:\cs2-mod-study\VehicleCapUnlock，后续开发以本仓库为准。

## 设置

| 选项 | 默认值 | 含义 |
|---|---|---|
| 解锁出车数量上限 | 关闭 | 扩展共享 policy 允许的范围，具体配车仍逐线路设置 |
| 线路出车上限倍率 | 1，范围 1–10 | 相对于捕获基线的最大配车水平，1 倍恢复初始范围 |
| 同步放大车库容量 | 关闭 | 扩容「有客运用途、无货运用途」的设施；货运与客货共用设施跳过，用途未知与可重复容量升级不再跳过（2026-11-26 起） |
| 车库容量倍率 | 1，范围 1–10 | 相对于每个 prefab 首次捕获容量的倍率，不连续叠乘 |
| 准备卸载（当前城市） | 按钮 | 确认后关闭两项功能，还原受管数据并将当前城市超限 adjustment 收回初始范围 |
| 当前状态 | 只读 | 分别显示线路、车库的等待、完成、冲突或失败状态 |

默认值适用于新设置；已有设置文件的值仍会加载。普通关闭功能保留逐线路设置，准备卸载会收回超限设置。
与货运共用交通类型的火车场站仍会被跳过（实测 `RailYard02`），因此不能承诺所有 7 类交通车库都扩容。本机实测：34 个候选 prefab 中 6 个合格并扩容，24 个类型不支持，4 个含货运或无客运。

这里的基线是本系统首次应用前观察到的数据；若其他 mod 更早修改同一 prefab，不能把它断言为纯原版值。
本项目尚未完成当前 playset 的兼容性验收。

## 换算依据和修复

本机 Game.dll 中，RouteModifierRefreshData.GetModifierDelta 的真实逻辑是：

```text
a = saturate((adjustment - sliderMin) / (sliderMax - sliderMin))
L = lerp(modifierMin, modifierMax, a)
```

注意：交接材料中「不 clamp」的说法不适用于本机程序集；它确实调用 saturate。
同时扩展输入范围和效果范围仍然成立：新增输入位于扩展后的合法范围内，
原区间内的直线斜率保持不变。浮点运算按数值容差验证，不保证逐比特相等。

保持 sliderMin 和 modifierMin 不动。设 k 为目标倍率、M 为初始 modifierMax：

```text
InverseRelative: target = k * (1 + M) - 1
Relative:        target = (1 + M) / k - 1
newSliderMax = sliderMin
             + (target - modifierMin) * (sliderMax - sliderMin) / (modifierMax - modifierMin)
```

计算以初始最大值为基准，不再误用默认 adjustment。
Relative 的有效扩展方向与 InverseRelative 不同，已分别校验。
Absolute 不能为所有线路表达同一个配车倍率，因此保持初始范围并记录告警，而非静默使用不准确的替代公式。

原版 VehicleCountSection 的上限计算会读取 PolicySliderData.m_Range.max，
设置配车数时又按相同 policy 数据反算 adjustment。本次没有引入 UI 补丁；
前端实际表现仍需在游戏中确认。

### 还原与生命周期

- 基线属于系统实例及其 ECS World，不再是跨 World 的静态数值缓存。
- 城市加载回调只请求重新应用，不清空同一 World 的基线。
- 通过 Entity 和 managed prefab 身份共同判断是否仍是同一对象。
- IMod.OnDispose 同步执行还原，不等待可能永远不会发生的下一帧。
- World 系统销毁时也尝试还原，随后移除实例登记。
- 保留每条线路存储的 adjustment 和启用标记，不覆盖玩家的逐线路设置。
- 城市加载、设置变化和还原时，均核验正式线路的 VehicleInterval；不依赖范围发生变化或 adjustment 超限。
- 仅处理具有 Route、TransportLine、Policy、RouteModifier 的正式实体，排除临时、删除和初始化中的实体，并等待原版线路初始化完成。
- 缓存必须能由当前或本 mod 支持的历史倍率映射解释；未知来源的直接写入会阻止本次修改并显示冲突。
- 记录最后写入值；其他系统改变 policy 或车库数据后，跳过覆盖。原版车库模式变更也按冲突处理。
- 先计算和预检，再写入；异常回滚仅恢复仍属于本批次的值。普通错误最多尝试 3 次，回滚不完整时停止至重启。
- 车库乘法使用 long，拒绝非正基线及溢出结果；对全部车库 prefab 的拟议容量执行保守合并预算检查。
- 上述处理发生在加载或设置请求时，不是每帧持续覆盖。临时 NativeArray 使用 using 释放。

普通关闭功能会按初始范围重新计算可识别的缓存，保留逐线路 adjustment，供重新启用时使用。
准备卸载时，先恢复模拟，等待线路和车库两项状态都显示“已准备”，再另存测试副本。
卸载后的重载和保存往返仍需实测；不能把调用还原方法或看到“已准备”当作存档安全证明。

## 验收

见 docs/VALIDATION.md。数学测试不能替代 Unity/ECS 生命周期与游戏前端测试。
提高可用车数不保证立即出车：实际结果还受线路条件、车库、道路和模拟状态影响。

---

# 模组 2：DeathcareTune

针对火葬场「处理速度跟不上死亡速度」的调优模组。只改 prefab 数据，不依赖 Harmony，不动任何 Burst 编译的控制流。

## 它改什么

`Game.Prefabs.DeathcareFacilityData`（挂在 prefab 实体上）的三个字段：

| 字段 | 含义 | 默认倍率 |
|---|---|---|
| `m_ProcessingRate` | 每次更新把待处理尸体转成已处理的速率 | ×4 |
| `m_HearseCapacity` | 设施同时可派出的灵车数（车队规模） | ×3 |
| `m_StorageCapacity` | 等待处理期间的尸体存放上限 | ×3 |

已有建筑立刻生效，不需要拆除重建：模拟层每 tick 用建筑的 `PrefabRef` 去
`ComponentLookup<DeathcareFacilityData>` 取 prefab 上的值。

## 为什么「灵车一次多装几具」改数据做不到

反编译 `Game.dll`（v1.1.x，2026-11-11 构建）核实：

1. `HearseData.m_CorpseCapacity` 的全部引用只有 4 处：自身 `.ctor` / `Serialize` / `Deserialize`，
   以及 `DeveloperInfoUISystem.HasPassengerInfo` / `UpdatePassengerInfo`（**只用于 UI 显示**）。
   `HearseAISystem` 与 `DeathcareFacilityAISystem` 从不读取它 —— 改它只会让界面上的数字说谎。
2. `HearseTickJob.LoadCorpses` 一旦匹配到目标尸体就 `m_State |= 4`（AtTarget）并 `return true`；
   `HearseTickJob.Tick` 随后立刻 `TransportToFacility`。整条链路上没有任何容量比较，
   「一趟一具」是控制流写死的。
3. 这些逻辑位于 `[BurstCompile]` 的 `IJobChunk.Execute` 内，Harmony 补丁作用不到 Burst 编译后的机器码。
   要让一辆车真装多具，等价于重写灵车 AI，风险与收益不成比例。

因此本模组用**车队规模倍率**做等效替代：一趟一具 × N 辆车 = 同量级的收运吞吐，
且完全走数据层，可逆。

## 设置

| 设置项 | 默认 | 范围 | 说明 |
|---|---|---|---|
| 启用殡葬调优 | 开 | 开 / 关 | 关闭即还原 |
| 火化速度 | 4 | 1–20 | 墓地基线为 0，保持 0 |
| 灵车数量 | 3 | 1–10 | 收运吞吐的唯一杠杆 |
| 尸体存放容量 | 3 | 1–10 | 基线为 0 的设施不动 |
| 准备卸载 | — | 按钮 | 全部归 1 倍并写回原版值 |

## 安全边界

- 只写 prefab 实体，不写建筑实例、不写存档内的城市数据。
- 首次触碰某 prefab 时记录基线并写日志（`[殡葬基线]`），还原时按基线写回。
- 若数据被其他来源改过（现值 ≠ 本模组上次写入值），**不覆盖**，只报冲突并跳过。
- 零值、负值、NaN/Infinity 一律原样保留，不做「造容量」。
- 数量上限 100000、速率上限 100000，截断会告警。

## 已知风险（未验证项）

- **存档是否持久化 prefab 数据尚未验证。** 如果确实持久化，在开启调优的状态下保存，
  移除模组后数值可能仍留在存档里。移除前请先点「准备卸载」并等到状态显示已还原，再另存副本。
- 车队规模受建筑停车位/车库约束，`m_HearseCapacity` 抬高后实际出车数可能仍被停车位限制。
- 尚未做游戏内验收（城市加载、死亡潮、存档往返、卸载还原）。

## 验证记录

- Release 构建：0 警告 0 错误。
- `Core/DeathcareScale` 换算回归 17 项断言通过（零值/负值/NaN/Infinity 保留、上限截断、截断标记）。
- 详细说明见 [DeathcareTune/README.md](DeathcareTune/README.md)。
- 日志：`%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Logs\DeathcareTune.log`
