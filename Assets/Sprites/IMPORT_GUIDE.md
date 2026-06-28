# Unity 导入配置指南 — 圣诞主题三消游戏

## 精灵清单

所有精灵位于 `Assets/Sprites/`，共 11 个 PNG 文件：

| PNG 文件 | 尺寸 | PPU | 用途 | VisualTheme 字段 |
|---|---|---|---|---|
| Element_Apple.png | 256×256 | 400 | Red 元素 → 苹果 | spriteRed |
| Element_Stocking.png | 256×256 | 400 | Blue 元素 → 圣诞袜 | spriteBlue |
| Element_Bell.png | 256×256 | 400 | Yellow 元素 → 铃铛 | spriteYellow |
| Element_Tree.png | 256×256 | 400 | Green 元素 → 圣诞树 | spriteGreen |
| Element_GiftBox.png | 256×256 | 400 | Suitcase 元素 → 礼物盒 | spriteSuitcase |
| Special_Rocket.png | 256×256 | 400 | Rocket 道具 → 烟花 | spriteRocket (需添加) |
| Special_Cracker.png | 256×256 | 400 | Bomb 道具 → 拉花弹 | spriteBomb (需添加) |
| Special_Boomerang.png | 256×256 | 400 | Propeller 道具 → 回旋镖 | spritePropeller (需添加) |
| Icon_Gift.png | 256×256 | 100 | UI 礼物图标 | TopBarView.giftIconSprite |
| Icon_Steps.png | 256×256 | 100 | UI 步数时钟图标 | TopBarView.stepsIconSprite |
| Background_ChristmasNight.png | 390×844 | 100 | 游戏背景 | ChristmasBackground.backgroundSprite |

## PPU 说明

程序化精灵为 64×64 像素、100 PPU = 0.64 世界单位，适配 0.7 的单元格。导出的 PNG 为 256×256 像素，必须设为 400 PPU 才能达到同样的 0.64 世界单位（256÷400=0.64）。若用 100 PPU 则为 2.56 世界单位，是单元格的 3.66 倍，元素会严重重叠。

UI 图标由 RectTransform 控制显示大小，PPU 不影响 UI 布局，保持 100 即可。背景由 ScaleToCover 方法缩放，该方法硬编码使用 100 PPU 计算，也保持 100。

## 自动修复

项目已包含编辑器脚本 `Assets/Editor/SpriteImportFixer.cs`，可在 Unity 菜单栏选择 HappyMatch → Fix Sprite Import Settings 一键修复所有精灵的 PPU 和导入设置。

---

## 第一步：修复精灵导入设置

在 Unity 中选择菜单 HappyMatch → Fix Sprite Import Settings，或手动选中 `Assets/Sprites/` 下的 Element_*.png 和 Special_*.png 文件，在 Inspector 中将 Pixels Per Unit 设为 400，点击 Apply。

## 第二步：创建 VisualTheme 并配置元素精灵

1. Project 窗口右键 `Assets/` → Create → HappyMatch → VisualTheme，命名为 `ChristmasTheme`
2. 在 Inspector 的 Optional Sprite Overrides 区域拖入对应精灵：

```
spriteRed      → Element_Apple
spriteBlue     → Element_Stocking
spriteYellow   → Element_Bell
spriteGreen    → Element_Tree
spriteSuitcase → Element_GiftBox
```

3. 选中 Hierarchy 中挂有 GameManager 的对象，将 ChristmasTheme 拖入 Visual Theme 字段

## 第三步：为特殊道具添加精灵覆盖

VisualTheme.cs 当前没有特殊道具的 override 字段。在 `Assets/Scripts/Core/VisualTheme.cs` 的 Optional Sprite Overrides 区域下方添加：

```csharp
[Header("Optional Special Sprite Overrides (leave null for procedural)")]
public Sprite spriteRocket;
public Sprite spriteBomb;
public Sprite spritePropeller;
```

修改三个 getter 方法，在开头加入 null 检查：

```csharp
public Sprite GetRocketSprite(ElementType type)
{
    if (spriteRocket != null) return spriteRocket;
    Color c = GetColorForType(type);
    if (!_rocketCache.ContainsKey(c))
        _rocketCache[c] = SpriteGenerator.CreateRocketSprite(c);
    return _rocketCache[c];
}

public Sprite GetBombSprite(ElementType type)
{
    if (spriteBomb != null) return spriteBomb;
    Color c = GetColorForType(type);
    if (!_bombCache.ContainsKey(c))
        _bombCache[c] = SpriteGenerator.CreateBombSprite(c);
    return _bombCache[c];
}

public Sprite GetPropellerSprite(ElementType type)
{
    if (spritePropeller != null) return spritePropeller;
    Color c = GetColorForType(type);
    if (!_propellerCache.ContainsKey(c))
        _propellerCache[c] = SpriteGenerator.CreatePropellerSprite(c);
    return _propellerCache[c];
}
```

回到 ChristmasTheme 的 Inspector，拖入：

```
spriteRocket    → Special_Rocket
spriteBomb      → Special_Cracker
spritePropeller → Special_Boomerang
```

## 第四步：使用 PNG 背景

在 ChristmasBackground.cs 中添加公共字段并在 Awake 开头插入背景加载逻辑：

```csharp
public Sprite backgroundSprite; // Inspector 中拖入 Background_ChristmasNight

private void Awake()
{
    if (backgroundSprite != null)
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        float viewH = cam.orthographicSize * 2f;
        float viewW = viewH * cam.aspect;

        GameObject bgGO = new GameObject("Background");
        SpriteRenderer sr = bgGO.AddComponent<SpriteRenderer>();
        sr.sprite = backgroundSprite;
        sr.sortingOrder = -12;
        bgGO.transform.SetParent(transform, false);
        bgGO.transform.position = Vector3.zero;

        const float ppu = 100f;
        float sw = backgroundSprite.textureRect.width / ppu;
        float sh = backgroundSprite.textureRect.height / ppu;
        float scale = Mathf.Max(viewW / sw, viewH / sh);
        bgGO.transform.localScale = new Vector3(scale, scale, 1f);
        return;
    }
    // ... 原有程序化背景代码
}
```

然后在 Inspector 中将 Background_ChristmasNight 拖入 ChristmasBackground 的 backgroundSprite 字段。

## 第五步：UI 图标（可选）

在 TopBarView.cs 中添加字段并在 CreateUI 中创建 Image 组件：

```csharp
public Sprite giftIconSprite;
public Sprite stepsIconSprite;
```

在 CreateUI() 的 targetText 旁添加图标 Image，sizeDelta 设为 (60, 60)，将 Icon_Gift 和 Icon_Steps 拖入对应字段。

---

## 验证

1. 运行游戏，检查棋盘元素是否正常排列（不重叠）
2. 消除 4+ 元素触发特殊道具，检查火箭/拉花弹/回旋镖
3. 检查背景和顶部栏图标

若元素仍重叠，确认 Element_*.png 和 Special_*.png 的 PPU 为 400。
