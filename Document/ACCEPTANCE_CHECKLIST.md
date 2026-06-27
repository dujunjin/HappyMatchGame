# 验收清单 (Acceptance Checklist)

对应 `docs/superpowers/specs/2026-06-27-happy-match-reference-polish-design.md` §7。勾选表示已实现并自测通过。

## 1. 确定性与可解性
- [x] 9×8 单关卡，固定、可解、无开局自动匹配。
- [x] 33 个行李箱目标。
- [x] `LevelConfig.Default` 同一 seed 每次生成相同布局（EditMode 测试 `Generate_SameSeed_ProducesIdenticalGrid`）。
- [x] 起手无 3 连（EditMode 测试 `Generate_Default_HasNoThreeInARow`）。
- [x] 至少 1 个合法交换（(0,2)↔(1,2)，EditMode 测试 `Generate_Default_LegalSwapProducesMatch`）。
- [x] 死局检测 + 洗牌（`DeadBoardDetector.HasLegalSwap`/`Shuffle`，级联 settle 后触发）。

## 2. 玩法机制
- [x] 相邻点击/拖拽交换、无效回退、匹配、消除、下落、补充、连锁。
- [x] 行李箱不可拖动（仅被相邻消除/特殊道具命中清除）。
- [x] 横向/纵向火箭、3×3 炸弹、自动追踪行李箱的螺旋桨。
- [x] 火箭×火箭 / 火箭×炸弹 / 火箭×螺旋桨 三种组合。
- [x] 影响集合去重、同格只结算一次（`HashSet` 去重；EditMode 测试 `Merge_TShape_UnionsIntoOneGroupOfFive`）。
- [x] 无重复计数：行李箱显示计数与逻辑计数同步（`OnSuitcaseHit` null-go 兜底；螺旋桨/组合在销毁前路由）。

## 3. 特殊生成规则
- [x] 4 连 → 火箭（EditMode 测试 `DetectSpecial_FourLine_YieldsRocket`）。
- [x] 5+ 连 → 螺旋桨（EditMode 测试 `DetectSpecial_FiveLine_YieldsPropeller`）。
- [x] T/L 十字 → 炸弹（EditMode 测试 `DetectSpecial_TJunction_YieldsBomb`）。
- [x] 合并匹配：T 形竖向臂不再丢失（`MergeMatches` 并集合并）。

## 4. 目标收集与胜负
- [x] 行李箱受击闪白/压缩 → 贝塞尔弧线飞向目标栏 → 抵达弹跳 + 显示计数递减。
- [x] 逻辑/显示/飞行三计数同步。
- [x] 胜利屏障：最后一个飞行体抵达且消除队列结束才进胜利演出。
- [x] 步数限制 + 失败重试；目标归零 → 胜利锁定。

## 5. 胜利演出
- [x] 棋盘退场（缩小+淡出+下移）。
- [x] "Great" 弹性文字 + 背景提亮。
- [x] 宝箱开盖四阶段（启封/绽放/涌宝/余韵）。
- [x] 重试/再演示入口。

## 6. 视觉与音频
- [x] 冬夜圣诞背景（天空/月亮/松林/木屋/圣诞树/雪橇/驯鹿/圣诞老人）。
- [x] 双层雨雪粒子（前大而虚、后小而密），对象池。
- [x] 棋子轮廓升级（圣诞袜/雪花/星/圣诞树）。
- [x] 棋盘暗背板，棋子从暗背景跳出。
- [x] 消除/火箭/炸弹/螺旋桨粒子（`VfxSystem` 池化）。
- [x] AudioManager 四混音组（Master/Ambient/SFX/UI）+ 程序化音色 + 冷却/最大并发。

## 7. 稳定性
- [x] 连锁与特殊道具连续触发不遗留空格/重复对象/卡死（级联 gravity+refill + 死局洗牌兜底）。
- [x] `AnimationHelper` 协程 null 安全（tween 中途对象销毁不抛异常）。
- [x] Bootstrap 场景入口，从磁盘打开即运行。

## 8. 关键截图

参考分辨率 **820×1022**（与参考视频 1640×2044 等比）。建议在 Unity Game 视图下拉 Fixed Resolution 中加一条 820×1022，逐项截图：

- [ ] 初始状态（棋盘 + 圣诞背景 + 雪花）
- [ ] 普通消除（彩色碎片+星点+扩散环）
- [ ] 三种特殊道具（火箭光束 / 炸弹冲击环 / 螺旋桨拖尾+星爆）
- [ ] 目标飞行（行李箱弧线飞向目标栏 + 弹跳）
- [ ] 胜利演出（棋盘退场 / Great / 宝箱开盖 / 彩带烟花）

截图脚本：`ScreenshotCapture`（运行时按 **F12** 保存当前帧到 `Application.persistentDataPath`）。

## 9. 测试
- [x] EditMode 测试（`Assets/Tests/EditMode/`）：BoardGenerator / LevelConfig / MatchDetector。
  - 运行方式：Unity → Window → General → Test Runner → EditMode → Run All。
  - 需安装 Test Framework 包（Unity 2022.3 默认自带）。
