# DeathcareTune / 殡葬调优

针对《都市：天际线 II》火葬场「处理速度跟不上死亡速度」的调优模组。只改 prefab 数据，不依赖 Harmony，不动任何 Burst 编译的控制流。

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
- 首次触碰某 prefab 时记录基线并写日志（`[基线]`），还原时按基线写回。
- 若数据被其他来源改过（现值 ≠ 本模组上次写入值），**不覆盖**，只报冲突并跳过。
- 零值、负值、NaN/Infinity 一律原样保留，不做「造容量」。
- 数量上限 100000、速率上限 100000，截断会告警。

## 已知风险（未验证项，如实记录）

- **存档是否持久化 prefab 数据尚未验证。** 如果确实持久化，在开启调优的状态下保存，
  移除模组后数值可能仍留在存档里。移除前请先点「准备卸载」并等到状态显示已还原，再另存副本。
- 车队规模受建筑停车位/车库约束，`m_HearseCapacity` 抬高后实际出车数可能仍被停车位限制。
- 尚未做游戏内验收（城市加载、死亡潮、存档往返、卸载还原）。

## 构建

```powershell
powershell -File scripts/build.ps1 -Configuration Release -Test -Deploy
```

dotnet 解析顺序：`$env:CSII_DOTNET` → 本工程 `.tools\dotnet` → 仓库根 `.tools\dotnet` → 系统 dotnet。
`-Deploy` 会在 Cities2 运行时拒绝执行。

部署目标：`%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Mods\DeathcareTune\DeathcareTune.dll`

## 日志

`%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Logs\DeathcareTune.log`

首次进入城市后查 `[基线]` 行，可以拿到该资产的原版数值。
