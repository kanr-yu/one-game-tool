# 开发说明

## v0.1.1 代码入口

| 文件 | 职责 |
| --- | --- |
| App.xaml / .xaml.cs | 深色资源与强调色 |
| MainWindow.xaml / .xaml.cs | 主窗口、标题栏与导航 |
| Views/Pages/GameLibraryPage.* | 游戏库视图、首次加载和 ViewModel 绑定 |
| ViewModels/GameLibraryViewModel.cs | ObservableCollection、过滤、动态统计、异步命令与状态 |
| Models/GameItem.cs | 数据库字段、属性通知和游玩时间显示 |
| Services/DatabaseService.cs | SQLite 初始化、参数化 CRUD 和按 ID 删除 |
| Services/GameService.cs | 添加、启动和移除游戏，协调图标、目录删除、统计和时间保存 |
| Services/IconService.cs | 256px 优先图标提取及 PNG 缓存 |
| Services/DirectorySizeService.cs | 递归容量统计与不可访问条目跳过 |
| Services/UserDialogs.cs | EXE 选择器、删除方式选择、永久删除确认和错误提示 |
| Infrastructure/AsyncRelayCommand.cs | 防重复执行与异步异常边界 |
| Infrastructure/LocalCoverConverter.cs | 封面读取、释放文件句柄与缺图回退 |
| Infrastructure/PreviewActions.cs | 其余功能的占位提示 |
| Models/PreviewData.cs | 仅保留缓存与备份页面的演示数据 |
| Tests/GameDeck.IntegrationTests/ | 隔离数据库与受控启动集成校验 |

## 构建和校验

~~~powershell
dotnet restore GameDeck.sln --locked-mode
dotnet build GameDeck.sln --configuration Release --no-restore
dotnet run --project Tests/GameDeck.IntegrationTests/GameDeck.IntegrationTests.csproj --configuration Release --no-build
dotnet run --project GameDeck.csproj --configuration Release --no-build
~~~

测试程序使用 WPF Dispatcher 保证集合与绑定在同一线程访问，覆盖 14 组检查：

1. SQLite 新建、重开读取、带引号名称、大小写路径查重及增删改。
2. 递归目录大小与单位格式化。
3. EXE 导入、256px PNG、重复与无效路径。
4. 真实受控子进程观察到正确的安装工作目录。
5. 启动失败不写时间，启动成功但数据库缺失记录时返回保存警告。
6. ViewModel 加载、添加、过滤、统计、重开、取消与属性更新。
7. 数据库目录不可写/不可用和缺失封面的错误边界。
8. 取消删除与仅移除列表，确认本体和图标保留、数量与总容量刷新。
9. 彻底删除与已不存在的目录，确认数据库和图标缓存一致。
10. 文件占用、共享游戏目录和确认后路径变化时拒绝删除并保留记录。
11. 纯路径校验拒绝系统/根目录和歧义路径；不对这些真实目录尝试删除。
12. 图标缓存占用或指向缓存外部时，保留无关文件并报告警告。
13. 实例化 WPF 页面并检查添加、启动、删除按钮的命令与 GameItem 参数。
14. 在实际 Wpf.Ui 导航宿主中确认无额外滚动包装，再验证 Page 根级、卡片、间隙和空白区域的预览滚轮偏移。

校验只在独立临时目录创建数据，不启动实际游戏。测试图标是本项目生成的简单 256px 色块资源，用于确定性验证高清提取；没有额外第三方素材。测试程序不依赖 Python。

## 依赖

当前通过 dotnet CLI 添加 Microsoft.Data.Sqlite 和 System.Drawing.Common 8.0.33，保留 WPF-UI 4.3.0。`global.json` 限定 .NET 8；主项目和测试项目各自保留 packages.lock.json。

修改包版本后：

~~~powershell
dotnet restore GameDeck.sln --force-evaluate
dotnet build GameDeck.sln --configuration Release --no-restore
~~~

将相关项目文件和锁文件一起提交。Windows CI 使用 --locked-mode 并执行集成校验；本地结果不等于远程工作流已执行。

## 本机 SDK（可选）

如果本机保留 .tools/dotnet，可使用项目内 SDK；该目录不会提交。克隆源码后请自行安装所需 .NET SDK，或指定已有 SDK 的完整路径。

## 后续开发边界

当前真实功能仅覆盖本地游戏库。平台扫描、清理、备份、恢复、搬家、提权、提醒与 HUD 保留占位交互。

数据库位置、表结构、异常处理和大小统计限制见 [DATABASE.md](DATABASE.md)。数据路径由 LocalApplicationData 计算，不依赖仓库绝对路径。

## 滚轮路由与删除确认

GameLibraryPage 根级通过 PreviewMouseWheel="Root_PreviewMouseWheel" 直接驱动唯一的 GameListScrollViewer。卡片使用纯 ItemsControl + UniformGrid，其控件模板只有 ItemsPresenter，不包含内部 ScrollViewer。根 Grid、ScrollViewer、内容 StackPanel 和网格均使用透明背景。

必须同时在 Page 根级设置 ScrollViewer.CanContentScroll=False。Wpf.Ui 4.3.0 的 NavigationViewContentPresenter 根据页面的这个附加属性决定是否套入 DynamicScrollViewer；只设置内部 ScrollViewer 的 CanContentScroll 不能禁用导航宿主包装。页面根事件直接调用 GameListScrollViewer.ScrollToVerticalOffset(VerticalOffset - e.Delta)，并标记 e.Handled=true，边界由目标滚动控件钳制。

滚轮回归检查在实际 NavigationViewContentPresenter 中加载应用使用的 Wpf.Ui 样式，确认动态包装关闭、ItemsControl 没有内部滚动容器，再校验 Page 根级和卡片/空白命中点的事件。测试不打开可见窗口，也不等同于真实鼠标硬件操作。

参考：[Wpf.Ui 导航宿主实现](https://github.com/lepoco/wpfui/blob/4.3.0/src/Wpf.Ui/Controls/NavigationView/NavigationViewContentPresenter.cs)。

DeleteGameCommand 在确认和删除期间禁用其他修改命令。用户取消或对话框失败时不调用删除服务。GameDeletionSafety 在递归删除前验证实际目标和保护范围，文件操作由后台线程执行。
