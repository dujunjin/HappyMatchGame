# Happy Match 指定资源版自验收报告

日期：2026-06-29  
Unity：2022.3.62f3c1  
平台：macOS Standalone / Metal / Apple M4  
目标分辨率：820×1022

## 结论

本轮指定资源替换通过自验收。玩法逻辑、目标计数、特殊道具和胜负流程保持稳定；核心棋子、行李箱、三种特殊道具、目标栏和两类字体已由用户指定目录提供的资源替换。真实 macOS Player 已完成连续自动演示与录屏。

## 自动化证据

- 基线：替换前 EditMode 49/49 通过。
- 最终 EditMode：56/56 通过，0 failed，结果文件 `Artifacts/AssetAcceptance/editmode-results.xml`。
- 编译：Unity 批处理退出码 0，未出现 `error CS`。
- 构建：`HappyMatchAcceptanceBuild.BuildMac` 成功，构建日志包含 `[HappyMatchAcceptanceBuild] built`。
- 运行：窗口化 Metal Player 写出 `acceptance-complete.txt`，并生成 `initial.png`、`specials.png`、`target-flight.png`、`special-action.png`、`victory.png`。
- 日志：运行日志使用 Apple M4 Metal，未发现 C# 编译错误、未处理异常、`NullReferenceException` 或 `MissingReferenceException`。

## 视觉核对

- 初始画面：顶部为指定奶油目标栏与橙色行李箱图标；棋盘显示红帽、黄帽、蓝垫、绿叶和 33 个橙色行李箱。
- 特殊道具：火箭、`BOOM` 炸弹和三叶螺旋桨均显示指定图片，触发清行/范围/追踪效果。
- 目标反馈：行李箱按弧线飞向目标栏，显示计数从 33 递减到 0。
- 胜利演出：棋盘退场，Poetsen One `Great!` 出现，指定橙色行李箱放大并伴随光效、烟花与纸屑，随后显示 Retry/Replay。
- 未发现缺图、旧礼物目标图标、旧木质宝箱、越界、控制台或桌面内容泄露。

## Demo 录屏

- 文件：`~/Desktop/HappyMatchGame-指定资源版-Demo.mp4`
- 时长：12.113 秒
- 视频：H.264，820×1022，60 FPS，yuv420p
- 音频：AAC，48 kHz，双声道
- 黑帧检测：无连续黑帧区间
- 内容：开局、特殊道具、目标飞行、目标归零、棋盘退场、`Great` 与胜利行李箱完整连续展示

## 资源不足与边界

- 用户指定目录没有完整背景和音效，因此保留已登记的原创冬夜背景与程序化音效作为占位。
- 本轮未做 iOS/Android 真机验收，也未扩展多关卡、联网、账号、商店或存档。
- 用户素材的商业公开分发许可需由素材提供方再次确认，详见 `Document/ASSET_LICENSES.md`。

自验收结果：通过。
