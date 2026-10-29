# One Game Tool · GameDeck

**v0.1.1** · C# · .NET 8 · WPF · WPF UI · SQLite · MIT

Windows 本地游戏管理工具：添加本地游戏、保存游戏库并启动游戏。
A Windows game manager with local executable import, a persistent library, and game launching.

[简体中文](#简体中文) | [English](#english)

## 简体中文

### v0.1.1 已实现

- **手动添加游戏**：选择 EXE，以文件名作为默认游戏名，自动记录安装目录和 LOCAL 平台。
- **SQLite 持久化**：游戏记录保存在当前用户的 `%LOCALAPPDATA%\GameSentinel\data.db`，重启后自动读取。
- **应用图标封面**：优先提取 256px 图标，保存为 `GameSentinel\Covers\<Guid>.png`；源文件没有高清图标或提取失败时使用可用图标或默认封面。
- **异步容量统计**：后台递归统计安装目录，跳过受限文件和重解析点，避免遍历循环；自动更新游戏数量及容量总和。
- **启动游戏**：将进程工作目录显式设为安装目录，成功创建进程后保存带时区偏移的最后游玩时间。
- **滚轮与删除**：卡片区域采用像素级滚动；右上角删除按钮提供「仅从列表移除」和「彻底删除游戏本体」，永久删除还会显示目录进行第二次确认。删除后自动刷新数量和容量。
- **真实数据绑定**：使用 ObservableCollection 与 ViewModel，支持名称、平台、路径搜索、空列表、加载进度、重复添加提示和错误恢复。

> 存储清理、备份、恢复、搬家、平台扫描、自动提权、休息提醒和实际 HUD 仍是占位功能。存储与性能页面仍使用演示数据。游戏库不再预置虚拟游戏。

### 页面

| 页面 | 状态 |
| --- | --- |
| 🎮 游戏库 | 真实本地添加、持久化、搜索、统计与启动 |
| 💾 存储与缓存 | 界面预览：磁盘、缓存和历史备份均为演示数据 |
| ⚡ 性能与面板 | 界面预览：开关与静态 HUD |
| ⚙️ 设置与自律 | 界面预览：维护入口与提醒开关 |

窗口支持拖动、双击最大化及自定义最小化 / 最大化 / 关闭按钮。界面目前使用中文。

### 环境与运行

需要 Windows 和 .NET 8 SDK（8.0.400 或更新的 8.0 SDK）。运行框架依赖构建需要 .NET 8 Desktop Runtime，首次还原包需要访问 NuGet。

~~~powershell
git clone https://github.com/kanr-yu/one-game-tool.git
cd one-game-tool
dotnet restore GameDeck.sln --locked-mode
dotnet build GameDeck.sln --configuration Release --no-restore
dotnet run --project GameDeck.csproj --configuration Release --no-build
~~~

也可以在安装了“.NET 桌面开发”工作负载的 Visual Studio 中打开 `GameDeck.sln`。

### 使用与数据

1. 打开游戏库，点击「手动添加游戏」并选择游戏真正的 EXE。
2. 等待图标提取与目录统计完成，卡片和顶部统计会自动更新。
3. 点击「启动游戏」；重启软件后仍可查看已添加的记录和游玩时间。
4. 卡片右上角可移除游戏；仅移除保留本体，彻底删除会永久移除整个安装目录及其中全部文件，不经过回收站。

数据库和封面均在当前用户目录，不写入游戏安装目录。重复路径不会重复添加。容量按每条记录的安装目录统计；同目录的多个 EXE 会分别计入总和，受限文件和目录链接不计入大小。

首次升级自 v0.1.0 时，游戏库为空；旧版本的三张游戏卡片只是演示数据，不会导入数据库。

### 校验与文档

~~~powershell
dotnet run --project Tests/GameDeck.IntegrationTests/GameDeck.IntegrationTests.csproj --configuration Release --no-build
~~~

集成校验使用独立临时数据库、测试图标和受控子进程，不启动用户的真实游戏，也不修改真实游戏库。14 组检查涵盖持久化、导入、启动、删除与取消、文件占用、目录边界、缓存失败、动态统计及 WPF 命令和滚轮路由。

Windows CI 执行锁定还原、Release 构建与集成校验。最新远程结果见 [Actions](https://github.com/kanr-yu/one-game-tool/actions)。

- [开发说明](docs/DEVELOPMENT.md)
- [数据库与数据目录](docs/DATABASE.md)
- [GitHub 协作说明](docs/GITHUB.md)
- [v0.1.1 更新说明](docs/releases/v0.1.1.md)
- [更新记录](CHANGELOG.md)

---

## English

### Implemented in v0.1.1

- **Manual game import:** select an EXE; its filename becomes the default game name, with the installation folder and LOCAL platform recorded.
- **SQLite persistence:** the library is stored in `%LOCALAPPDATA%\GameSentinel\data.db` and loaded when the library page opens.
- **Executable icon covers:** request a 256px icon and save it as `GameSentinel\Covers\<Guid>.png`. Smaller source icons or a default cover are used when a high-resolution icon is unavailable.
- **Background size calculation:** recursively scan the installation directory, skip inaccessible entries and reparse points, and update the collection count and total size.
- **Game launching:** explicitly set the process working directory to the installation folder. Save an ISO 8601 last-played timestamp after a process has been started.
- **Scrolling and removal:** pixel-based wheel scrolling works over game cards. The top-right delete button offers list-only removal or permanent installation-folder deletion, with an additional path confirmation for permanent deletion. Totals refresh automatically.
- **Bound UI:** an ObservableCollection-backed ViewModel drives search, statistics, empty states, loading feedback, duplicate detection, and error recovery.

> Storage cleanup, save backup/restore, migration, platform scanning, privilege elevation, reminders, and a real HUD remain placeholders. Storage and performance pages still show demo data. The game library no longer contains seeded fictional games.

### Pages

| Page | Status |
| --- | --- |
| 🎮 Game Library | Real local import, persistence, search, statistics, and launch |
| 💾 Storage & Cache | UI preview with demo disk, cache, and backup records |
| ⚡ Performance & HUD | UI preview with switches and a static HUD |
| ⚙️ Settings & Discipline | UI preview with maintenance and reminder entry points |

Window dragging, double-click maximization, and custom window controls are available. The application interface is currently in Chinese.

### Requirements and setup

Use Windows and .NET 8 SDK 8.0.400 or a newer 8.0 SDK. The framework-dependent build needs .NET 8 Desktop Runtime. Initial package restore requires NuGet access.

~~~powershell
git clone https://github.com/kanr-yu/one-game-tool.git
cd one-game-tool
dotnet restore GameDeck.sln --locked-mode
dotnet build GameDeck.sln --configuration Release --no-restore
dotnet run --project GameDeck.csproj --configuration Release --no-build
~~~

Alternatively, open `GameDeck.sln` in Visual Studio with the .NET desktop development workload.

### Usage and data

1. Open the library, choose manual import, and select the game's actual executable.
2. Wait for icon extraction and directory scanning; cards and statistics update automatically.
3. Launch the game. Imported records and last-played timestamps remain available after restarting.
4. Use the card's top-right delete button. List-only removal preserves game files; permanent deletion removes the entire installation folder and its contents without using the Recycle Bin.

The application writes its database and covers to the current user's local application data directory, not the game directory. Duplicate paths are rejected. Size totals add each record's installation-directory size; multiple executables in one folder are counted separately. Inaccessible entries and directory links are excluded.

When upgrading from v0.1.0, the persistent library starts empty. The original three game cards were demo records and are not migrated.

### Validation and documentation

~~~powershell
dotnet run --project Tests/GameDeck.IntegrationTests/GameDeck.IntegrationTests.csproj --configuration Release --no-build
~~~

Integration checks use temporary databases, a test icon, and controlled subprocesses. They do not launch real games or modify the user's library. Fourteen checks cover persistence, import, launch, removal and cancellation, locked files, directory boundaries, cache failures, statistics, WPF command bindings, and wheel routing.

Windows CI runs a locked restore, Release build, and integration checks. See [Actions](https://github.com/kanr-yu/one-game-tool/actions) for remote results.

- [Development guide (Chinese)](docs/DEVELOPMENT.md)
- [Database and storage guide (Chinese)](docs/DATABASE.md)
- [GitHub collaboration guide (Chinese)](docs/GITHUB.md)
- [v0.1.1 release notes](docs/releases/v0.1.1.md)
- [Changelog](CHANGELOG.md)

## 项目结构 / Project layout

~~~text
one-game-tool/
├── .github/workflows/build.yml
├── docs/
├── Infrastructure/              # Commands, binding helpers, formatting
├── Models/                      # GameItem and remaining preview data
├── Services/                    # SQLite, import, icon, size, process, dialogs
├── ViewModels/                  # Observable game library state
├── Views/Pages/
├── Themes/
├── Tests/GameDeck.IntegrationTests/
├── App.xaml
├── MainWindow.xaml
├── GameDeck.csproj
├── GameDeck.sln
├── global.json
├── NuGet.Config
├── packages.lock.json
└── LICENSE
~~~

## 许可证与依赖 / License and dependencies

本项目使用 [MIT 许可证](LICENSE)。第三方依赖通过 NuGet 还原，遵循各自许可证。

This project uses the [MIT License](LICENSE). NuGet dependencies retain their own licenses.

- [WPF UI 4.3.0](https://github.com/lepoco/wpfui)
- [Microsoft.Data.Sqlite 8.0.33](https://www.nuget.org/packages/Microsoft.Data.Sqlite/8.0.33)
- [System.Drawing.Common 8.0.33](https://www.nuget.org/packages/System.Drawing.Common/8.0.33)
