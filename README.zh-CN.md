# StarMap

[English](README.md) | 简体中文

一个用于 Kitten Space Agency 的任意代码模组加载器。

## 文档

[StarMap 文档](https://starmaploader.github.io/StarMap/)介绍了安装、使用和模组开发，Markdown 源文件位于 [`docs`](docs/) 目录。

## 发布

文档站点跟随当前发布版本更新，不为旧版本保留独立的文档快照。当前候选发布版的文档可在 [RC 文档站点](https://starmaploader.github.io/StarMap/rc/)查看。

## 安装

- 从 [Releases](https://github.com/StarMapLoader/StarMap/releases/latest) 下载 ZIP 或 Windows 安装程序。
- 如果尚未安装，请先安装 Windows x64 版 [.NET 10 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)。
- 使用安装程序时，请选择包含 `KSA.dll` 的 KSA 目录；安装程序会创建 `StarMapConfig.json`。使用 ZIP 时，先解压并运行一次 `StarMap.exe` 以生成配置，然后将 `GameLocation` 设置为 KSA 目录或 `KSA.dll` 的路径。`RepositoryLocation` 可以留空。
- 将配置文件保留在 `StarMap.exe` 旁边，并从 StarMap 安装目录启动程序，以加载已启用的模组并启动 KSA。

详细步骤请参阅[安装指南](https://starmaploader.github.io/StarMap/installation/)。

## 模组位置

模组可以放在以下任一位置：

-   `Documents/My Games/Kitten Space Agency/mods/<ModName>/`（推荐；游戏更新后仍可保留）
-   `<KSA Installation>/Content/<ModName>/`

StarMap 使用游戏的清单（manifest）系统，从这两个位置发现模组。

## 模组开发

有关模组开发的更多信息，请参阅示例模组仓库：[StarMap-ExampleMods](https://github.com/StarMapLoader/StarMap-ExampleMods)。

## 未来计划

长远目标是在游戏内提供模组管理器，让玩家从远程目录选择模组，并在重启后安装。目前 StarMap 只加载本地模组。计划中的目录只索引模组 ID、版本和下载地址；模组文件可托管在其他位置，例如 GitHub Releases。

## 致谢

-   Lexi - [KSALoader](https://github.com/cheese3660/KsaLoader)
