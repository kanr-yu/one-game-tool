# 更新记录 / Changelog

## v0.1.1 — 2026-10-29

### 新增 / Added

- 实现本地 EXE 游戏导入，自动记录名称、安装目录和 LOCAL 平台。 / Import local game executables with their name, installation directory, and LOCAL platform.
- 新增 SQLite 游戏库，重启后自动加载已收录记录。 / Persist the game library in SQLite and reload it after restarting.
- 优先提取 256px 应用图标并缓存为 PNG，提供缺图回退。 / Extract 256px executable icons when available, cache them as PNG, and fall back gracefully.
- 后台统计安装目录大小，动态显示游戏数量与总容量。 / Calculate installation sizes in the background and display live library totals.
- 实现游戏启动，显式设置工作目录，并保存最后游玩时间。 / Launch games with an explicit working directory and persist last-played timestamps.
- 支持仅从列表移除，或经二次确认永久删除安装目录及图标缓存。 / Remove library entries or permanently delete game files and icon caches after additional confirmation.

### 修复 / Fixed

- 将滚轮监听提升到 Page 根级，直接驱动唯一的 GameListScrollViewer。 / Route wheel input at the Page root to a single GameListScrollViewer.
- 禁用 Wpf.Ui 导航宿主自动添加的动态滚动包装，避免嵌套滚动。 / Disable the Wpf.Ui navigation host's automatic scroll wrapper.
- 卡片采用纯 ItemsControl + UniformGrid，容器显式设置透明背景参与命中测试。 / Use a plain ItemsControl and UniformGrid with explicit transparent hit-test backgrounds.
- 统一刷新删除后的数量、容量、搜索结果与空列表状态。 / Refresh totals, filtered results, and empty states after removal.

### 数据与异常处理 / Data and error handling

- 规范化 EXE 路径，并通过 SQLite 唯一约束拦截重复添加。 / Normalize executable paths and reject duplicates with a SQLite unique constraint.
- 目录统计跳过受限文件和重解析点。 / Skip inaccessible entries and reparse points during size scans.
- 永久删除前校验完整目标，保护根目录、系统与用户资料目录、本软件数据和其他已收录游戏。 / Validate deletion targets and protect root/system/user-data directories, application data, and other registered games.
- 目录删除失败时保留记录；图标清理或游玩时间保存失败时单独提示。 / Preserve records on directory deletion failure and report cache/timestamp failures separately.

### 构建与校验 / Build and validation

- 添加 Microsoft.Data.Sqlite 8.0.33 和 System.Drawing.Common 8.0.33，保留 .NET 8 与依赖锁文件。 / Add Microsoft.Data.Sqlite and System.Drawing.Common 8.0.33 with .NET 8 and locked dependencies.
- 新增 14 组隔离集成检查，并接入 Windows CI。 / Add fourteen isolated integration checks to Windows CI.
- 本地 Release 构建为 0 警告、0 错误。滚轮检查覆盖真实 Wpf.Ui 导航模板、Page 根级、卡片和空白命中区域。 / The local Release build has zero warnings and errors; wheel checks cover the Wpf.Ui navigation template, page root, cards, and whitespace targets.

### 范围与限制 / Scope and limitations

- 存储清理、备份、恢复、搬家、平台扫描、提权、休息提醒和实际 HUD 仍为占位功能。 / Storage cleanup, backup/restore, migration, platform scanning, elevation, reminders, and a real HUD remain placeholders.
- 永久删除不经过回收站，目录中的存档和模组也会被删除；中途失败可能已移除部分文件。 / Permanent deletion bypasses the Recycle Bin and includes saves/mods inside the installation directory; partial deletion is possible on failure.
- 滚轮修复已通过程序化回归检查；本次尚无实机鼠标验收记录。 / Wheel fixes passed programmatic regression checks; physical-mouse acceptance has not been recorded for this build.


## v0.1.0 — 2026-10-28

首个 UI 原型版本 / Initial UI prototype.

### 新增 / Added

- 深色 FluentWindow、自定义标题栏与窗口按钮。 / Dark FluentWindow with custom title bar and window controls.
- 游戏库、存储与缓存、性能与面板、设置与自律四页导航。 / Four-page navigation for the game library, storage, performance, and settings.
- 三个虚拟游戏、三类缓存与三条历史备份。 / Three fictional games, three cache categories, and three sample backups.
- 游戏关键词过滤、空结果提示与共享占位弹窗。 / Game filtering, empty results, and shared placeholder dialogs.
- 页面状态缓存、静态 HUD 卡片与休息提醒开关。 / Cached page state, static HUD preview, and a break reminder switch.
- MIT 许可证、中英双语 README、依赖锁定与 Windows CI。 / MIT license, bilingual README, locked dependencies, and Windows CI.

### 范围 / Scope

本版本仅包含 UI 与占位交互，尚未实现真实业务后台。

This version contains the UI shell and placeholder interactions only; backend features are not implemented.
