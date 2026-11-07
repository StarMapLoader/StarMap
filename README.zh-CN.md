# StarMap

[English](README.md) | 简体中文

一个用于 Kitten Space Agency 的任意代码模组加载器。

## 文档

[StarMap 文档](https://starmaploader.github.io/StarMap/)介绍了安装、使用和模组开发，Markdown 源文件位于 [`docs`](docs/) 目录。

## 发布

文档站点跟随当前发布版本更新，不为旧版本保留独立的文档快照。当前候选发布版的文档可在 [RC 文档站点](https://starmaploader.github.io/StarMap/rc/)查看。

## 安装

-   从 [Releases](https://github.com/StarMapLoader/StarMap/releases/latest) 下载并解压发布包。
-   如果尚未安装，请先安装 Windows x64 版 [.NET 10 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)。
-   首次运行 `StarMap.exe` 时，程序会启动失败并生成 `StarMapConfig.json`。
-   打开 `StarMapConfig.json`，设置 KSA 的安装位置。
    -   `GameLocation` 应直接指向 Kitten Space Agency 的安装目录（例如 `C:\\games\\Kitten Space Agency\\`）。
    -   `RepositoryLocation` 可以留空。
-   再次运行 `StarMap.exe`，此时应能启动 KSA 并加载模组。

## 模组位置

模组可以放在以下任一位置：

-   `Documents/My Games/Kitten Space Agency/mods/<ModName>/`（推荐；游戏更新后仍可保留）
-   `<KSA Installation>/Content/<ModName>/`

StarMap 使用游戏的清单（manifest）系统，从这两个位置发现模组。

## 模组开发

有关模组开发的更多信息，请参阅示例模组仓库：[StarMap-ExampleMods](https://github.com/StarMapLoader/StarMap-ExampleMods)。

## 未来计划

目标是开发一个模组加载器，提供类似《异星工厂》（Factorio）的模组功能：玩家可在游戏内从远程仓库选择模组，游戏自动重启后再安装。
目前，加载器通过两个进程分别托管游戏，以支持重启。
设想中，仓库只索引模组、版本和下载地址；模组下载可托管在其他位置，例如 GitHub Releases。

## 致谢

-   Lexi - [KSALoader](https://github.com/cheese3660/KsaLoader)
