# 验证记录与游戏内验收清单

日期：2026-11-17。这里只记录已实际执行的检查；未打勾的项目尚未完成。

## 存档安全审计

**当前版本仍需游戏内存档验收。** 原始问题记录见[安全审计报告](SAFETY_AUDIT.md)，已完成的代码整改和限制见[修复说明](SAFETY_FIXES.md)。

- [x] 核对本机 Policy / RouteModifier 序列化及原版车库模式的容量写入路径。
- [x] 根据当时 active playset、当前日志和实际缓存版本执行定向兼容性静态扫描。
- [x] 再次运行现有数学回归产物，通过 167,670 项断言。
- [x] 完成安全审计 F1–F6 的代码整改及准备卸载流程，实现默认关闭 / 1 倍。
- [x] 修复版 Release 编译通过，0 警告、0 错误。
- [x] 安全回归通过 3,647 项断言，涵盖逐位置写入前/后故障、回滚冲突与失败、重试、容量和客货图关系。
- [x] 修复版在 Cities2 关闭时完成构建与部署，部署 DLL 与构建产物 SHA256 一致：65E9A909B7C0670CF63070ACE5EC8ABE00BBD371FEE803BF21D5D6503E1D2A16（2026-11-26）。
- [ ] 在实际游戏中验证 F1–F6、状态显示及准备卸载操作。
- [ ] 完成测试副本的保存、重启重载、关闭功能加载和无 mod 往返验收。

## 已完成

- [x] 本机项目显式引用的 11 个游戏/Unity/Colossal DLL 均存在。
- [x] SDK 8.0.428 便携安装在工程 .tools/dotnet，SHA512 与微软发布元数据一致。
- [x] Microsoft.NETFramework.ReferenceAssemblies 1.0.3 及 net48 引用程序集还原成功。
- [x] 初稿首次 Release 构建成功，0 警告、0 错误。
- [x] 修正后的 Release 构建成功，0 警告、0 错误。
- [x] 独立数学回归程序通过 167,670 项断言。
- [x] Git origin 可通过系统现有代理访问，读取时远端为空。
- [x] 构建及 SDK 准备脚本均通过 PowerShell 语法检查。
- [x] 游戏运行时执行 -Deploy，被保护逻辑在写入部署目录之前拒绝。
- [x] 最终 DLL 元数据确认目标为 .NETFramework v4.8，包含修复后的 RefreshAffectedLines 方法。

### 数学验证覆盖

测试直接编译 Core/CeilingMath.cs，而非另写一份待测算法。
使用合成数据，不把样例范围误称为游戏的真实运行基线。

- InverseRelative、Relative 两种有效方向。
- 原始上限与默认值不同的情况；倍率以原始上限为基准。
- 1 倍保留原始端点；2–10 倍扩大输入范围。
- 原范围及低于下限的输入经过真实的 saturate 语义后，映射在容差内不变。
- 新增范围内按原版 remap 反算 adjustment 的往返一致性。
- 零跨度、错误方向、无效倍率、NaN、Infinity、未知模式拒绝修改。
- 固定随机种子的 400 组合成范围及 3 组固定范围。

容差为 1e-5 × max(1, |expected|)。这不是游戏内测试，也不是逐比特相等证明。

### 本机 Game.dll 元数据/IL 核对

- PolicySliderData.m_Range、m_Default；RouteModifierData.m_Range；TransportDepotData.m_TransportType、m_VehicleCapacity 存在。
- PrefabSystem.GetSingletonPrefab<T>(EntityQuery)、GetEntity(PrefabBase)、GetPrefab<T>(Entity) 存在。
- GameSystemBase.OnGameLoadingComplete(Purpose, GameMode) 签名确认。
- GetModifierDelta 正向转换调用 math.saturate，然后 math.lerp。
- GetPolicyAdjustmentFromModifierDelta 反向转换调用 math.remap，然后 math.clamp。
- VehicleCountSection.CalculateVehicleCountJob.Execute 从 policy slider 范围计算车辆数上下限。
- VehicleCountSection.OnSetVehicleCount 使用该 policy 的数据反算 adjustment。
- RouteModifierRefreshData.GetModifierDelta 与 AddModifierData 是可供本 mod 调用的公开 API。
- 原版 RouteModifierInitializeSystem 的初始化查询与 ModifiedSystem 的策略事件路径不会因为本 mod 单独写 prefab 范围就自动遍历所有既有线路。因此补充了事件式的受影响线路缓存刷新。

不在仓库分发游戏程序集、反编译源码或本机 mod 列表。

## 游戏内待验收

修复版已部署到用户数据 Mods/VehicleCapUnlock，仅包含本 mod 的 DLL。2026-11-26 已在游戏内加载测试存档，确认车库扩容生效（`candidates=34; eligible=6; changes=6`）；逐线路配车、保存重载与卸载流程仍未验收。

正常退出游戏后，运行 scripts/build.ps1 -Test -Deploy，并仅使用测试存档副本执行下列验收。默认功能关闭、倍率 1，需要明确启用后测试。

- [ ] 日志出现 OnLoad、systems registered、[基线]、[线路校正]、[线路完成]，没有异常。
- [ ] 记录真实 slider、default、modifier、mode，不能拿交接示例当实测值。
- [ ] 原版线路面板滑块能超过初始最大值。
- [ ] 线路 A 提高配车后，线路 B 的原有配车设置保持原义。
- [ ] 公交 / 有轨 / 地铁 / 火车 / 船 / 轮渡 / 飞机分别验证。
- [ ] 线路编辑与重建后仍按当前设置生效。
- [ ] 实际车辆数随目标变化，车库容量同步生效。
- [ ] 5 → 2 → 1 → 5 倍，不连续叠乘；较高 adjustment 的缓存同步刷新。
- [ ] 主开关、独立车库开关与两个倍率均按预期生效。
- [ ] 同一进程切换城市，基线不变成已修改后的值。
- [ ] 返回菜单再加载、重启游戏后加载，保存的逐线路设置保持可解释。
- [ ] OnDispose 发生时出现同步还原记录；World 销毁没有异常。
- [ ] 准备卸载后，两项状态显示已准备，超限 adjustment 收回初始范围，另存副本；移除 mod 重载→运行→保存→再重载成功。
- [ ] 外部修改或未知来源的缓存使状态显示冲突，保留外部值，不报告准备卸载成功。
- [ ] 客货混用设施被跳过；用途未知与可重复容量升级的客运设施会被扩容（2026-11-26 判据调整），实际扩容集合符合日志。
- [ ] 读取当时 playset_config.json 与 Logs/Modding.log 后，才对当前 mod 组合做兼容性结论。

以上运行行为在实际完成前都不能标记为通过。
