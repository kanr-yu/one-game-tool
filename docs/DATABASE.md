# 数据库与本地数据

## 默认位置

通过 `Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)` 计算：

~~~text
%LOCALAPPDATA%/GameSentinel/
├── data.db
└── Covers/
    └── <Guid>.png
~~~

数据库和封面在首次使用时按需创建，安装目录与仓库目录不保存这些用户数据。SQLite 运行时可能生成 WAL/SHM 辅助文件。

## Games 表

| 字段 | SQLite 类型 | 含义 |
| --- | --- | --- |
| Id | INTEGER PRIMARY KEY AUTOINCREMENT | 自增记录 ID |
| Name | TEXT | 默认 EXE 主文件名 |
| ExePath | TEXT UNIQUE COLLATE NOCASE | 规范化的绝对 EXE 路径 |
| InstallDirectory | TEXT | EXE 所在目录、启动工作目录 |
| SizeBytes | INTEGER | 可访问文件大小总和 |
| SizeFormatted | TEXT | 使用 1024 进制换算的 B / KB / MB / GB 等字符串 |
| Platform | TEXT | 当前为 LOCAL |
| CoverPath | TEXT | PNG 的绝对路径；缺失时为空字符串 |
| LastPlayed | TEXT | 带时区偏移的 ISO 8601 时间；尚未启动时为空字符串 |

列设置 NOT NULL 和适当默认值；读取时仍兼容已有记录中的 NULL。

## 服务

`DatabaseService` 每次操作使用独立连接，在后台线程执行，使用参数化 SQL、WAL 模式和有限的忙等待。提供：

- `InitializeAsync`
- `GetAllGamesAsync`
- `GetByExePathAsync`
- `AddGameAsync`
- `UpdateGameAsync`
- `UpdateLastPlayedAsync`
- `DeleteGameAsync` / `DeleteGameById(int id)`（返回 Task<bool>，参数化删除记录）
- `GetGameByIdAsync`

DatabaseService 删除 API 仅删除数据库记录。卡片右上角的删除命令由 GameService 协调两种模式：

- 仅从列表移除：删除记录，更新界面集合，保留游戏目录和图标缓存。
- 彻底删除：再次确认完整安装目录，后台校验并递归删除目录，成功后删除记录并清理该游戏的 GUID PNG 缓存。

永久删除的目标必须是数据库中已确认的绝对路径，且与 EXE 所在目录一致。根目录、系统/用户资料根目录、本软件及数据目录、符号链接祖先和包含其他已收录游戏的目录会被拒绝。缓存删除只接受本软件 Covers 目录内的 GUID PNG，避免数据库中的异常路径指向其他文件。

文件删除失败时保留数据库记录及卡片，并提示关闭占用程序或检查权限。递归删除不具有事务性：失败前可能已经移除部分文件。目录已删除而数据库写入失败时，会提示使用「仅从列表移除」重试；图标清理失败则移除卡片并单独显示缓存警告。

Windows 的 Directory.Delete 不递归进入子目录中的重解析点；链接本身会移除，目标文件夹不会被遍历。整个安装目录的祖先含链接时，本软件会直接拒绝永久删除。

`GameService.AddGameAsync` 先规范化并校验路径，再查重、提取图标和扫描安装目录。扫描与图标处理使用 Task.Run。最后保存记录；保存失败时尽力删除本次生成的 GUID 封面，唯一约束也阻止并发重复插入。

图标优先使用 Windows `SHDefExtractIconW` 请求 256px 资源，再使用关联图标回退。最终清晰度取决于 EXE 自身的资源；无法读取或保存时保留默认封面，不阻断导入。所有原生图标句柄和图片流都释放。

`GameService.LaunchGameAsync` 显式设置 `FileName`、`WorkingDirectory` 和 `UseShellExecute=true`。只有启动返回进程后才记录时间。进程创建成功不等于游戏已完成加载，也不跟踪游戏退出时间。

如果启动成功而时间写入失败，会返回单独警告并显示本次内存中的游玩时间，避免将启动误报为失败。重启后仍以数据库中最后成功保存的时间为准。

## 大小与限制

统计会读取隐藏及系统文件的元数据，但会跳过无法访问的条目和重解析点，防止目录链接循环。目录扫描期间文件发生变化时，结果可能只是近似值。多个 EXE 位于相同目录时，其目录大小会分别进入统计；本版本没有去重安装目录或自动重扫描。

## 异常边界

数据库和游戏服务将异常转换为可显示错误。AsyncRelayCommand 捕获异步命令异常，ViewModel 提供加载错误、重新加载、忙碌状态和操作提示。损坏或缺失的封面由图片转换器回退处理。

开发校验可为 DatabaseService 注入临时数据库路径；不会创建或改动真实的 GameSentinel 数据。

参考：[Microsoft.Data.Sqlite 异步限制](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/async)、[SHDefExtractIconW](https://learn.microsoft.com/en-us/windows/win32/api/shlobj_core/nf-shlobj_core-shdefextracticonw)。
