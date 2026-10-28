# GitHub 协作与上传

仓库：<https://github.com/kanr-yu/one-game-tool>

## 使用 GitHub Desktop

1. 在 GitHub Desktop 打开 `one-game-tool`。
2. 修改源码后，在 Changes 中检查提交范围。
3. 填写提交说明并选择 Commit to main，再选择 Push origin。
4. 开始下一次修改前先使用 Fetch origin / Pull origin 同步远程。

## 提交范围

提交源码、XAML、解决方案、依赖锁文件、文档、许可证和 `.github` 工作流。根目录 `.gitignore` 排除本地 SDK、NuGet 缓存、构建结果和 IDE 用户设置。

通过 `packages.lock.json` 锁定依赖图。修改包版本时，显式更新并一起提交 `.csproj` 和锁文件。

## 命令行

~~~powershell
git status --short
git add .
git diff --cached --stat
git diff --cached
git commit -m "Describe your change"
git push origin main
~~~

使用本机已经配置的 Git 作者和认证，不在源码或配置文件中保存凭据。

## 源码与程序包

- 源码仓库只放可还原、可构建的源文件。
- 不要提交 `.git/`、`.tools/`、`bin/`、`obj/` 或本地源码 ZIP。
- 可运行程序可通过 GitHub Releases 单独发布。当前框架依赖构建需要 .NET 8 Desktop Runtime，应整体打包输出目录，不能只分发 EXE。
- `v0.1.0` 是首个 UI 原型版本，真实清理、备份、启动与 HUD 均未实现。

push / pull request 会触发 Windows 构建工作流，也可在 Actions 中手动运行。工作流只检查构建，不运行 GUI。

工作流参考：[GitHub .NET 文档](https://docs.github.com/en/actions/tutorials/build-and-test-code/net)、[actions/checkout](https://github.com/actions/checkout)、[actions/setup-dotnet](https://github.com/actions/setup-dotnet)。
