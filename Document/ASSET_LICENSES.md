# 资源来源与授权登记

本工程包含三类资源：用户提供素材、项目原创/AI 辅助素材、以及程序化/Unity 官方资源。以下登记用于明确本次面试 Demo 的实际运行资源与公开分发边界。

## 用户提供并用于本次 Demo 的资源

来源目录为工程根目录的 `图片/` 与 `font/`。Unity 不直接导入整个原始目录，只将实际使用文件镜像到 `Assets/Resources/HappyMatch/`。

| 用途 | 原始文件 | Unity 路径 |
|---|---|---|
| 红色棋子 | `图片/img_ig_candy/candy_1_1_1.png` | `Assets/Resources/HappyMatch/Pieces/Red.png` |
| 黄色棋子 | `图片/img_ig_candy/candy_1_2_1.png` | `Assets/Resources/HappyMatch/Pieces/Yellow.png` |
| 蓝色棋子 | `图片/img_ig_candy/candy_1_3_1.png` | `Assets/Resources/HappyMatch/Pieces/Blue.png` |
| 绿色棋子 | `图片/img_ig_candy/candy_1_4_1.png` | `Assets/Resources/HappyMatch/Pieces/Green.png` |
| 行李箱/胜利主物件 | `图片/img_ig_candy/candy_7_0_1.png` | `Assets/Resources/HappyMatch/Pieces/Suitcase.png` |
| 火箭 | `图片/img_ig_candy/boost_candy_hv.png` | `Assets/Resources/HappyMatch/Specials/Rocket.png` |
| 炸弹 | `图片/img_ig_candy/boost_candy_bomb.png` | `Assets/Resources/HappyMatch/Specials/Bomb.png` |
| 螺旋桨 | `图片/img_ig_candy/royal_leaves_feiji.png` | `Assets/Resources/HappyMatch/Specials/Propeller.png` |
| 顶部目标栏 | `图片/img_game_common/UIpanel_top_Minigame.png` | `Assets/Resources/HappyMatch/UI/TargetPanel.png` |
| HUD 字体 | `font/Passion One.ttf` | `Assets/Resources/HappyMatch/Fonts/PassionOne.ttf` |
| 展示字体 | `font/PoetsenOne-Regular-1.ttf` | `Assets/Resources/HappyMatch/Fonts/PoetsenOne.ttf` |

上述文件由用户明确提供给本次工程使用，但素材包未附带单独的著作权或商业再分发许可证。本次交付按用户授权用于面试 Demo；若后续公开上架、营销或商业分发，应由素材提供方再次确认权利链和许可范围。

## 项目原创与 AI 辅助资源

| 资源 | 生成方式 | 日期 | 工程路径 | 当前用途 |
|---|---|---|---|---|
| 精修圣诞冬夜背景 | OpenAI 图像生成后本地整理为竖屏背景 | 2026-06-28 | `Assets/Sprites/Background_ChristmasNight_Polished.png` | 指定图片中没有完整环境背景，因此作为占位背景保留 |
| 胜利木箱五状态 | OpenAI 图像生成与本地去背 | 2026-06-28 | `Assets/Resources/VictoryChest/` | 已不用于当前胜利主物件，仅保留历史资源 |

## 程序化生成资源

| 类别 | 生成方式 | 文件 | 当前用途 |
|---|---|---|---|
| 棋子、行李箱、特殊道具 | `SpriteGenerator` 运行时生成 | `Assets/Scripts/Utils/SpriteGenerator.cs` | 指定资源缺失时的容错回退 |
| 粒子与光效 | `SpriteGenerator`、`VfxSystem`、`WinSequence` | 对应脚本 | 消除、火箭、炸弹、螺旋桨、目标飞行和胜利反馈 |
| 音效 | `ProceduralAudio` 运行时合成 | `Assets/Scripts/Audio/ProceduralAudio.cs` | 交换、消除、道具、目标与结算音效 |

## Unity 官方包

- TextMeshPro：Unity 官方包，用于运行时字体资产与 UI 文本。
- Unity Test Framework：Unity 官方包，仅用于 Editor 测试。

## 结论

当前运行画面的核心棋子、目标物、三种特殊道具、目标栏和字体均来自用户指定目录；冬夜背景、粒子和音效为明确登记的占位/程序化资源。商业公开分发前唯一需要额外确认的是用户提供素材的权利链。
