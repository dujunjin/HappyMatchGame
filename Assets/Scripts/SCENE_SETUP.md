# HappyMatchGame - Unity 场景搭建指南

## 前置要求
- Unity 2022.3 LTS（2022.3.62f3c1 或更高版本）
- 2D 模板项目

## 第一步：脚本编译
1. 所有脚本位于 `Assets/Scripts/`（按子文件夹组织）
2. 在 Unity Editor 中打开项目
3. 等待编译完成（检查 Console 是否有错误）
4. 所有脚本使用全局命名空间，不需要程序集定义

## 第二步：场景搭建（SampleScene）

### 摄像机
1. 在 Hierarchy 中选择 `Main Camera`
2. 将 Projection 设为 **Orthographic**
3. 将 **Size** 设为 `5.0`
4. 将 **Background** 设为深蓝色（#1a1a2e 或类似颜色）
5. 将 Transform Position 设为 `(0, 0, -10)`（默认值）

### 创建 GameManager
1. 在 Hierarchy 中右键 → Create Empty
2. 命名为 `GameManager`
3. Add Component → Scripts → Core → `GameManager`
4. GameManager 会在 Start() 中自动创建所有其他系统

### 删除已有对象
1. 删除任何现有的 `BoardManager` GameObject（旧的空脚本）
2. 如有现有的 Canvas 对象也一并删除

## 第三步：程序集定义（可选但推荐）

为避免跨文件夹引用的编译问题，可以创建程序集定义：

1. 右键点击 `Assets/Scripts/` → Create → Assembly Definition
2. 命名为：`GameScripts`
3. 在 Inspector 中：
   - 勾选 "Include Platform References"
   - 添加引用：`UnityEngine`、`UnityEngine.UI`、`Unity.TextMeshPro`
4. 确保所有脚本可以互相引用

## 第四步：TextMeshPro 配置
1. 如果弹出导入 TMP 必备组件的提示，点击 "Import TMP Essentials"
2. TopBarView 和 ResultDialog 的文字渲染需要此组件

## 第五步：运行游戏
1. 在 Editor 中点击 Play
2. 棋盘应显示 4 种颜色的圆形 + 橙色行李箱
3. 点击/轻触两个相邻的元素进行交换
4. 行/列中 3 个以上相同元素触发消除
5. 4 连消除生成火箭（点击可清除整行/整列）
6. T/L 形消除生成炸弹（点击产生 3x3 爆炸）
7. 清除 33 个行李箱 = 胜利
8. 25 步用完 = 失败

## 调试快捷键
- 按 `1` — 在 (3,4) 位置生成横向火箭
- 按 `2` — 在 (4,4) 位置生成炸弹

## 故障排查

### "The type or namespace name could not be found"
- 确保所有脚本编译成功（查看 Console）
- 如果使用了程序集定义，确保添加了所有必要的引用

### 棋盘不显示
- 检查摄像机是否为 Orthographic，Size=5.0
- 棋盘原点在 (0, -0.5, 0) — 应在视口中可见
- 确保所有棋盘元素的 Z 坐标为 0

### 点击无反应
- 确保每个元素都添加了 CircleCollider2D（已自动添加）
- 确保摄像机没有 PhysicsRaycaster 阻挡（2D 使用 Physics2D）

### 行李箱不生成
- 确保 `SuitcaseManager` 已初始化（查看 Console 错误）
- 调试键 1 和 2 可用于测试特殊道具

## 文件结构参考
```
Assets/Scripts/
├── Enums/
│   ├── ElementType.cs         # 元素类型（红、蓝、黄、绿、行李箱、空）
│   └── GameState.cs           # 游戏状态机状态
├── Core/
│   ├── GameConfig.cs          # 所有游戏常量与配置
│   └── GameManager.cs         # 中央协调器与状态机
├── Board/
│   ├── BoardController.cs     # 棋盘网格数据与视觉管理
│   └── CellData.cs            # 单元格数据结构
├── Gameplay/
│   ├── MatchDetector.cs       # 消除检测（横向、纵向、T/L形）
│   ├── SwapHandler.cs         # 选择与交换逻辑
│   ├── GravitySystem.cs       # 重力与填充机制
│   └── CascadeManager.cs      # 主连锁循环调度器
├── Special/
│   ├── SpecialFactory.cs      # 创建火箭与炸弹
│   ├── RocketBehavior.cs      # 火箭点击激活行为
│   └── BombBehavior.cs        # 炸弹点击激活行为
├── Target/
│   └── SuitcaseManager.cs     # 行李箱放置与相邻检测
├── Input/
│   └── BoardInput.cs          # 点击与拖拽输入处理
├── UI/
│   ├── GameUI.cs              # 主 UI 管理器
│   ├── TopBarView.cs          # 目标与步数计数器显示
│   └── ResultDialog.cs        # 胜利/失败弹窗
└── Utils/
    ├── SpriteGenerator.cs     # 运行时精灵生成
    ├── AnimationHelper.cs     # 协程动画补间
    └── Helper.cs              # 工具方法
```
