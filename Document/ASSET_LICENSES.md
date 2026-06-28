# 资源许可证登记 (ASSET_LICENSES)

Happy Match Game 所有关卡内使用的美术、音频、字体资源均为**项目内原创、AI 辅助生成、程序化生成**或 Unity 官方包内容，未引入来源不明的第三方商业游戏素材。

## 项目原创与 AI 辅助资源

| 资源 | 生成方式 | 日期 | 工程路径 | 使用说明 |
|---|---|---|---|---|
| 精修圣诞冬夜背景 | OpenAI 内置图像生成工具按项目专用提示生成，随后本地缩放为 820×1022；无品牌、文字或受版权角色 | 2026-06-28 | `Assets/Sprites/Background_ChristmasNight_Polished.png` | 本项目原创背景，可随项目使用、修改与分发 |
| 胜利宝箱五状态（闭合/微启/半开/大开/全开） | OpenAI 内置图像生成工具按项目专用提示生成；使用纯绿幕去背并本地生成 alpha PNG，无品牌、文字或受版权角色 | 2026-06-28 | `Assets/Resources/VictoryChest/ChestClosed.png`、`ChestCracked.png`、`ChestAjar.png`、`ChestWide.png`、`ChestOpen.png` | 本项目原创胜利演出资源，可随项目使用、修改与分发 |

## 程序化生成资源

| 类别 | 生成方式 | 文件 |
|---|---|---|
| 棋子轮廓（圣诞袜/雪花/星/圣诞树） | `SpriteGenerator` 运行时按像素生成 Texture2D | `Assets/Scripts/Utils/SpriteGenerator.cs` |
| 行李箱贴图 | `SpriteGenerator.CreateSuitcaseSprite` | 同上 |
| 特殊道具（火箭/炸弹/螺旋桨） | `SpriteGenerator.CreateRocketSprite` 等 | 同上 |
| 圣诞夜场景（天空/月亮/松林/木屋/圣诞树/雪橇/驯鹿/圣诞老人/雪地） | `ChristmasArt` 运行时按像素生成 | `Assets/Scripts/Environment/ChristmasArt.cs` |
| 粒子（碎片/星点/扩散环/星芒/烟花/彩带/宝箱/宝石/纸屑） | `SpriteGenerator` + `VfxSystem`/`WinSequence` 池化 | `Assets/Scripts/Utils/SpriteGenerator.cs` 等 |
| 音效（交换/消除/连锁/行李箱/火箭/炸弹/螺旋桨/胜利/失败/UI） | `ProceduralAudio` 运行时合成 AudioClip | `Assets/Scripts/Audio/ProceduralAudio.cs` |

以上资源无外部许可证约束，可自由使用、修改、分发。

## 第三方包

- **TextMeshPro (TMP)**：Unity 官方包，按 Unity 许可证使用（顶部目标栏与胜负弹层文字所需）。导入即随 Unity Compositor，无需额外登记。
- **Unity Test Framework**：Unity 官方包，按 Unity 许可证使用（仅 EditMode 测试，Editor 平台，不进包体）。

## 真实素材替换

未来若用真实美术/音频素材替换程序化占位：
1. 将素材拖入 `VisualTheme` / `AudioCatalog` ScriptableObject 容器的对应字段（无需改逻辑）。
2. 在本文件登记该素材的**来源、作者、许可证**（如 CC0/CC-BY/商业授权/自制）。
3. 确认许可证允许本项目用途后再提交。

## 结论

当前工程不依赖任何需要额外授权的外部素材，可独立编译运行。后续若替换资源，必须先补全本登记表。
