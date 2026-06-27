# 资源许可证登记 (ASSET_LICENSES)

Happy Match Game 所有关卡内使用的美术、音频、字体资源均为**程序化生成**或在 Unity 编辑器内由代码运行时创建，未引入任何外部受版权保护的素材。

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

当前工程（Phase A–F）不依赖任何需要额外授权的外部素材，可独立编译运行。
