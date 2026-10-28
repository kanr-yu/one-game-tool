# One Game Tool · GameDeck

**v0.1.0** · C# · .NET 8 · WPF · WPF UI · MIT

Windows 本地游戏管理与缓存清理界面原型。
Windows desktop UI prototype for game management and cache cleanup.

[简体中文](#简体中文) | [English](#english)

## 简体中文

### 项目介绍

One Game Tool 的首个版本以 GameDeck 为界面名称，提供深色 Fluent 窗口、四页导航与完整的占位交互，作为后续开发真实功能的基础。

> **当前版本仅为 UI 原型。** 游戏启动、平台扫描、备份、恢复、无损搬家、清理、自动提权和实际 HUD 均未接入后台。点击相关按钮或开关只显示“功能开发中”，不会执行这些业务操作。

### 页面与交互

| 页面 | 内容 |
| --- | --- |
| 🎮 游戏库 | 三个虚拟游戏卡片；关键词搜索；手动添加、扫描、启动、备份和搬家入口 |
| 💾 存储与缓存 | 磁盘空间进度；18.2 GB 着色器缓存、5.1 GB 下载残留、1.2 GB 临时垃圾；历史备份列表 |
| ⚡ 性能与面板 | Windows 11 风格开关；静态 HUD 预览；悬浮窗测试入口 |
| ⚙️ 设置与自律 | 封面缓存清理、数据库优化入口和休息提醒开关 |

已实现的本地交互包括窗口拖动、双击最大化、自定义最小化 / 最大化 / 关闭按钮、页面切换、中文 / 英文 / 平台关键词过滤、空结果提示及占位弹窗。搜索和开关状态在当前运行期间保留，重启后恢复默认值。

### 环境要求

- Windows。
- .NET 8 SDK：8.0.400 或更新的 8.0 SDK，用于从源码构建。
- 可选：支持上述 SDK 且安装了“.NET 桌面开发”工作负载的 Visual Studio。
- 运行构建后的程序需要 .NET 8 Desktop Runtime；首次还原依赖需要访问 NuGet。

### 快速开始

~~~powershell
git clone https://github.com/kanr-yu/one-game-tool.git
cd one-game-tool
dotnet restore GameDeck.sln --locked-mode
dotnet build GameDeck.sln --configuration Release --no-restore
dotnet run --project GameDeck.csproj --configuration Release --no-build
~~~

也可以在 Visual Studio 中打开 `GameDeck.sln`。

### 开发与验证

项目使用 `global.json` 选择 .NET 8 SDK，并使用 `packages.lock.json` 锁定依赖图。Windows 构建工作流在 push / pull request 时执行依赖还原与 Release 编译。

v0.1.0 已在 Windows 使用 .NET SDK 8.0.427 完成 Release 编译。四页导航、中文搜索、空结果提示、占位弹窗、HUD 开关状态保留及窗口控制已进行人工检查。项目目前没有自动化 GUI 测试；最新远程构建结果请查看 [Actions](https://github.com/kanr-yu/one-game-tool/actions)。

磁盘、帧率、日期、容量和游戏信息均为静态演示数据。海报由本地 XAML 绘制，不需要外部图片。

- [开发说明（中文）](docs/DEVELOPMENT.md)
- [GitHub 协作与上传说明（中文）](docs/GITHUB.md)
- [更新记录](CHANGELOG.md)

---

## English

### About

One Game Tool v0.1.0 introduces **GameDeck**, a Windows desktop UI shell with a dark Fluent window, four navigation pages, and placeholder interactions for future game management features.

> **This release is a UI prototype.** Game launching, platform scanning, save backup and restore, game migration, cleanup, privilege elevation, and a real in-game HUD have no backend implementation. Their buttons and switches only display a development notice; they do not perform those operations.

### Pages and interactions

| Page | Contents |
| --- | --- |
| 🎮 Game Library | Three fictional game cards; search; add, scan, launch, backup, and migration entry points |
| 💾 Storage & Cache | Disk usage bar; 18.2 GB shader cache, 5.1 GB download leftovers, 1.2 GB temporary files; backup history |
| ⚡ Performance & HUD | Windows 11 style switches; a static HUD preview; an overlay test entry point |
| ⚙️ Settings & Discipline | Cover cache and database maintenance entry points; a break reminder switch |

Working UI interactions include window dragging, double-click maximization, custom minimize / maximize / close controls, page navigation, filtering by Chinese or English game names and platform names, empty search results, and placeholder dialogs. Search and switch states persist for the current session and reset after restarting.

The application interface and development dialogs are currently in Chinese. This README provides both Chinese and English documentation.

### Requirements

- Windows.
- .NET 8 SDK, version 8.0.400 or a newer 8.0 SDK, to build from source.
- Optional: Visual Studio with support for that SDK and the .NET desktop development workload.
- .NET 8 Desktop Runtime to run the framework-dependent build; NuGet access for the initial dependency restore.

### Getting started

~~~powershell
git clone https://github.com/kanr-yu/one-game-tool.git
cd one-game-tool
dotnet restore GameDeck.sln --locked-mode
dotnet build GameDeck.sln --configuration Release --no-restore
dotnet run --project GameDeck.csproj --configuration Release --no-build
~~~

Alternatively, open `GameDeck.sln` in Visual Studio.

### Development and validation

`global.json` selects a .NET 8 SDK, and `packages.lock.json` locks the dependency graph. The Windows workflow restores dependencies and builds Release on pushes and pull requests.

v0.1.0 has been built locally on Windows with .NET SDK 8.0.427. Navigation, Chinese search, empty results, placeholder dialogs, HUD switch state retention, and window controls have been manually checked. There are no automated GUI tests yet. See [Actions](https://github.com/kanr-yu/one-game-tool/actions) for the latest remote build status.

All disk usage, frame rates, dates, sizes, and game details are static demo data. Poster placeholders are drawn in XAML and require no external images.

- [Development guide (Chinese)](docs/DEVELOPMENT.md)
- [GitHub collaboration guide (Chinese)](docs/GITHUB.md)
- [Changelog](CHANGELOG.md)

## 项目结构 / Project layout

~~~text
one-game-tool/
├── .github/workflows/build.yml   # Windows CI
├── docs/                        # Development and GitHub guides
├── Infrastructure/              # Shared placeholder command
├── Models/                      # Demo data
├── Themes/                      # Dark theme styles
├── Views/Pages/                 # Four pages
├── App.xaml                     # Application resources
├── MainWindow.xaml              # Window and navigation
├── GameDeck.csproj
├── GameDeck.sln
├── global.json
├── NuGet.Config
├── packages.lock.json
└── LICENSE
~~~

## 许可证与依赖 / License and dependencies

本项目使用 [MIT 许可证](LICENSE)。第三方依赖遵循各自许可证，通过 NuGet 还原，不随源码仓库提交。

This project is licensed under the [MIT License](LICENSE). Third-party dependencies retain their own licenses and are restored through NuGet rather than committed to this repository.

- [WPF UI / WPF-UI 4.3.0](https://github.com/lepoco/wpfui) — MIT.
- [Navigation documentation](https://wpfui.lepo.co/documentation/navigation-view.html) / [Theme documentation](https://wpfui.lepo.co/documentation/themes.html).
