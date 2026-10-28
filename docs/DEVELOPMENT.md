# 开发说明

## 代码入口

| 文件 | 职责 |
| --- | --- |
| `App.xaml` / `.xaml.cs` | 加载 WPF UI 资源、固定深色主题与强调色 |
| `MainWindow.xaml` / `.xaml.cs` | 主窗口、标题栏交互、NavigationView 与初始导航 |
| `Views/Pages/GameLibraryPage.*` | 游戏网格、关键词过滤与空结果状态 |
| `Views/Pages/StorageCachePage.*` | 静态磁盘用量、缓存卡片与历史备份列表 |
| `Views/Pages/PerformancePage.*` | 性能开关和静态 HUD 预览 |
| `Views/Pages/SettingsPage.*` | 应用维护入口和休息提醒开关 |
| `Models/PreviewData.cs` | 不可变的演示记录与数据集合 |
| `Infrastructure/PreviewActions.cs` | 按钮和开关共用的占位命令与深色弹窗 |
| `Themes/Styles.xaml` | 调色板、卡片、按钮、文字和标题栏样式 |

这是一个小型界面原型，目前使用简单的 code-behind；尚未引入数据库、后台服务或完整 MVVM 层。继续开发时，可逐项用实际命令或服务替换 `PreviewActions`，并将演示数据替换为 ViewModel。

## 依赖与 SDK

`global.json` 将 SDK 限定在 .NET 8，允许使用更高的 8.0 feature band / patch。`packages.lock.json` 固定 NuGet 依赖图；常规构建使用 `--locked-mode`。

修改包版本后，显式更新锁文件，并一起提交 `.csproj` 和锁文件：

~~~powershell
dotnet restore GameDeck.sln --force-evaluate
dotnet build GameDeck.sln --configuration Release --no-restore
~~~

## 仅用于本机的 SDK

当前开发目录可能保留 `.tools/dotnet`，供未全局安装 SDK 的机器使用。该目录不是源码的一部分，克隆或下载源码后不会包含它。

本机存在该目录时，可在 PowerShell 中执行：

~~~powershell
$env:DOTNET_CLI_HOME = Join-Path $PWD '.tools/cli'
$env:MSBUILDDISABLENODEREUSE = '1'
.\.tools\dotnet\dotnet.exe restore GameDeck.sln --locked-mode --packages .tools/nuget
.\.tools\dotnet\dotnet.exe build GameDeck.sln --configuration Release --no-restore -p:UseSharedCompilation=false -nodeReuse:false
.\.tools\dotnet\dotnet.exe run --project GameDeck.csproj --configuration Release --no-build
~~~

## 验证

项目尚无自动化测试。`.github/workflows/build.yml` 在 Windows 上执行锁定还原与 Release 编译，验证 XAML 与 C# 能否构建；它不运行 GUI，也不代替人工交互检查。

人工检查时依次确认：

1. 四个导航项可切换，搜索和开关状态在切页后保留。
2. 中文 / 英文 / 平台关键词可过滤游戏；无匹配时显示空结果提示。
3. 功能按钮与开关只出现占位提示，不执行真实业务操作。
4. 窗口可拖动、最小化、最大化、还原和关闭。
5. 缩小至最小窗口尺寸后，内容可滚动，按钮仍可访问。

2026-10-28 已完成 Release 编译以及导航、中文搜索、空结果、启动提示、HUD 开关状态保留、最大化、还原和关闭的人工检查。

## 本地路径

源码和项目配置使用相对路径。移动目录后，如 MSBuild 缓存仍指向旧位置，可关闭程序及 IDE，删除自动生成的 `obj/`，再运行还原与构建。`bin/`、`obj/`、`.tools/` 均不应提交。
