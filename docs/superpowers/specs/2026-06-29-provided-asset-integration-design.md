# Happy Match 指定资源替换设计

日期：2026-06-29

## 目标

在不改动已通过 49 项 EditMode 测试的三消规则前提下，将用户提供的 `图片/` 与 `font/` 资源接入 Unity，替换现有程序化棋子、目标栏、特殊道具、目标图标和胜利主物件，使 9×8 棋盘、33 个行李箱、火箭/炸弹/螺旋桨反馈及 `Great` 演出更接近 43.56 秒参考视频。

## 参考结论

- 视频比例为 1640×2044，项目继续以等比的 820×1022 竖屏运行。
- 四类普通棋子对应红帽、黄帽、蓝色软垫、绿色叶片；目标物是橙色行李箱。
- 顶栏为奶油色圆角胶囊；特殊道具包括条纹火箭、圆形炸弹和三叶螺旋桨。
- 指定目录未提供与视频中寒冷室内人物场景等价的完整背景图，因此现有冬夜背景作为明确记录的占位背景保留；雨雪、棋盘玻璃层和环境音继续复用现有实现。

## 方案比较与选择

1. 精选资源镜像（采用）：只将实际使用的 10 个 PNG 和 2 个 TTF 镜像到 `Assets/Resources/HappyMatch`，通过集中式目录加载并保留程序化回退。优点是工程导入快、映射可测试、缺图不阻断运行。
2. 整目录导入：把两千余张图片全部放入 `Assets`。覆盖面最大，但会显著增加导入时间、仓库噪声和交付体积。
3. 重新打大图集：运行性能最好，但需要重建切图元数据，风险和工作量不适合本次单关卡 Demo。

## 资源映射

| 游戏用途 | 用户资源 | Unity 资源名 |
|---|---|---|
| 红色棋子 | `图片/img_ig_candy/candy_1_1_1.png` | `HappyMatch/Pieces/Red` |
| 黄色棋子 | `图片/img_ig_candy/candy_1_2_1.png` | `HappyMatch/Pieces/Yellow` |
| 蓝色棋子 | `图片/img_ig_candy/candy_1_3_1.png` | `HappyMatch/Pieces/Blue` |
| 绿色棋子 | `图片/img_ig_candy/candy_1_4_1.png` | `HappyMatch/Pieces/Green` |
| 行李箱 | `图片/img_ig_candy/candy_7_0_1.png` | `HappyMatch/Pieces/Suitcase` |
| 火箭 | `图片/img_ig_candy/boost_candy_hv.png` | `HappyMatch/Specials/Rocket` |
| 炸弹 | `图片/img_ig_candy/boost_candy_bomb.png` | `HappyMatch/Specials/Bomb` |
| 螺旋桨 | `图片/img_ig_candy/royal_leaves_feiji.png` | `HappyMatch/Specials/Propeller` |
| 顶部目标栏 | `图片/img_game_common/UIpanel_top_Minigame.png` | `HappyMatch/UI/TargetPanel` |
| 目标栏图标 | 同行李箱 | `HappyMatch/Pieces/Suitcase` |
| 数字与 HUD 字体 | `font/Passion One.ttf` | `HappyMatch/Fonts/PassionOne` |
| `Great` 与按钮字体 | `font/PoetsenOne-Regular-1.ttf` | `HappyMatch/Fonts/PoetsenOne` |

## 架构

- 新增 `HappyMatchAssetCatalog`，集中声明资源路径、加载 Sprite、创建并缓存动态 TMP 字体资产，并暴露 `HasProvidedCoreAssets` 供验收与诊断。
- `VisualTheme` 保持 Inspector 覆盖优先；字段为空时先取指定资源，资源缺失才使用原程序化 Sprite。这样既完成替换，也保证源码工程可独立运行。
- `TopBarView` 使用提供的奶油目标栏和行李箱图标，所有 TMP 文本通过目录应用 Passion One。
- `ResultDialog` 与 `WinSequence` 使用 Poetsen One；胜利主物件改为提供的橙色行李箱，不再展示与参考视频不一致的木质宝箱。
- Editor 导入器按文件用途设置 Sprite、透明通道、压缩和 Pixels Per Unit；字体保留为 Unity Font 资源，由运行时创建 TMP 动态字体。

## 数据流与回退

启动时 `Bootstrap -> GameManager -> VisualTheme` 获取核心 Sprite；UI 初始化时从 `HappyMatchAssetCatalog` 获取目标栏、图标和字体。若某个资源不存在，目录返回空值，调用方继续走现有程序化回退，同时在验收报告中指出缺失路径。任何资源替换都不改变棋盘数据、匹配检测、目标计数、步数或特殊道具清除集合。

## 测试与验收

- EditMode 新增目录路径、资源完整性、VisualTheme 优先级、TopBar 资源和胜利行李箱依赖测试，先验证失败再实现。
- 全量 EditMode 测试必须 0 failed；Unity 批处理编译和 macOS Release Player 构建必须退出码 0。
- 自动演示必须生成初始、特殊道具、目标飞行和胜利截图；逐张确认使用指定棋子、行李箱和奶油目标栏，且无缺图、越界或 Console 错误。
- 录屏必须从实际构建运行产生，H.264 竖屏，含游戏音轨，并复制到桌面。
- `项目说明.md` 记录运行方式、资源映射、完成情况、占位资源和已知问题。

## 范围边界

不新增多关卡、商业化、账号、存档或联网；不导入未使用的大批资源；不声称背景是指定资源。原始 `图片/` 和 `font/` 保留不删，Unity 只消费经过筛选的镜像文件。
