# VehicleCapUnlock — 公共交通配车上限解锁

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

## 目录和仓库

本机工程目录：

```text
E:\SteamLibrary\steamapps\common\Cities Skylines II\Mods\VehicleCapUnlock
```

Git origin：

```text
https://github.com/Nanako-Arasaka/CS2_Unlimited_PubilcVehicle.git
```

这是源码/构建目录。部署目标是用户数据中的独立目录：

```text
%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Mods\VehicleCapUnlock
```

原始交接工程仍保留在 E:\cs2-mod-study\VehicleCapUnlock，后续开发以新工程目录为准。
游戏 DLL、SDK、NuGet 缓存、编译产物均不纳入 Git。

## 构建

目标框架 net48，C# 9。本机无需安装完整官方 Unity modding 工具链即可编译。
便携 SDK 8.0.428 放在 .tools/dotnet，不修改系统 PATH。

在工程目录运行：

```powershell
# 首次准备环境：从微软官方下载 SDK，校验 SHA512 后解压。
.\scripts\bootstrap-sdk.ps1

# 仅构建。
.\scripts\build.ps1

# 构建并运行数学和安全回归验证。
.\scripts\build.ps1 -Test

# 正常退出游戏后，明确部署 DLL。
.\scripts\build.ps1 -Deploy
```

构建脚本在 Cities2 进程存在时拒绝 -Deploy，编译/测试结束后写入前再次检查；默认构建不部署。
同时使用 -Test -Deploy 时，两组回归通过后才复制 DLL，部署后核验 SHA256。
产物是 bin/Release/VehicleCapUnlock.dll。

默认游戏引用路径是安装目录的 Cities2_Data/Managed。其他安装位置可指定：

```powershell
.\scripts\build.ps1 -ManagedPath 'D:\Games\Cities Skylines II\Cities2_Data\Managed'
```

net48 引用程序集由 Microsoft.NETFramework.ReferenceAssemblies 1.0.3 提供，首次还原需要网络。
官方 CSII_TOOLPATH 导入分支保留，但本次只验证了无官方工具链的构建路线；发布流程尚未配置。

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
