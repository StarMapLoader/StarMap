# StarMap

[English](README.md) | 简体中文

StarMap 是用于 Kitten Space Agency 的概念验证（POC）/原型代码模组加载器，可加载自定义代码。
目前，加载器既可在后台运行，也可作为仅负责加载模组的简单加载器使用。
它使用程序集加载上下文（Assembly Load Contexts）分别管理模组依赖，以减少冲突。

## 安装

-   从 [Releases](https://github.com/StarMapLoader/StarMap/releases/latest) 下载并解压发布包。
-   首次运行 `StarMap.exe` 时会因缺少配置而无法启动游戏，并生成 `StarMapConfig.json`。
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

目标是提供类似《异星工厂》（Factorio）的模组使用体验：玩家可在游戏内从远程仓库选择模组，游戏自动重启后安装这些模组。
目前，加载器通过两个进程单独运行游戏，以便重启游戏。
设想中的仓库仅提供模组、版本和下载地址的索引；实际下载文件存放在其他位置，例如 GitHub Releases。

## 致谢

-   Lexi - [KSALoader](https://github.com/cheese3660/KsaLoader)
