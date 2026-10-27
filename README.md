# MCTL — Minecraft Terminal Launcher

**MCTL** 是一个轻量级、纯命令行的 Minecraft Java 版启动器。

## 环境要求

| 依赖 | 说明 |
|---|---|
| **.NET 10 Runtime** | 运行时所需 |
| **Java** | Minecraft 1.17+ 需 Java 17+，1.20.5+ 需 Java 21+，并确保 `java` 在 PATH 中 |
| **Fabric 安装** | 需在 MCTL 可执行文件同级目录放置 [`fabric-installer.jar`](https://fabricmc.net/use/installer/) |

## 快速开始

### 1. 初始化游戏目录
首次运行时，请先指定你的 `.minecraft` 目录（一次配置永久生效）：

```bash
mctl config --game-dir="D:\Minecraft"
```

### 2. 登录账户

**微软正版登录暂不可用**

**离线登录**：
```bash
mctl login offline Steve
```

### 3. 安装游戏
```bash
# 原版
mctl install 1.20.4

# 安装 Fabric 并安装最新版本
mctl install 1.20.4 --modloader=fabric

# 安装 Forge 并命名
mctl install 1.20.4 --modloader=forge --name=my-forge-pack
```

### 4. 启动游戏
```bash
mctl list               # 查看已安装版本
mctl launch 1.20.4      # 启动原版
mctl launch my-forge-pack  # 启动已克隆的隔离版本
```

## 常用命令

### `mctl login [offline <username>]`
- `mctl login` — 发起微软设备代码流登录（暂不可用）
- `mctl login offline <username>` — 切换为离线账户登录

### `mctl list`
扫描本地 `.minecraft/versions/` 目录，列出所有已安装的版本（包括原版、Forge/Fabric、克隆版本）。

### `mctl install <mc_version> [options]`
| 选项 | 说明 |
|---|---|
| `--modloader=<forge\|fabric>` | 指定模组加载器 |
| `--loader-version=<ver>` | 指定加载器版本。Fabric 未指定时自动查询最新兼容版本 |
| `--name=<name>` | 安装完成后自动克隆为独立版本（自带隔离的 `mods` 目录） |

### `mctl clone <src> <target>`
克隆一个已存在的版本到新名字，自动重写 JSON 中的 ID、JAR 引用，并创建独立 `mods` 文件夹。

### `mctl mod <query> <mc_version> <loader>`
在 Modrinth 搜索 `<query>`，自动按游戏版本和加载器过滤，并下载**最优匹配**的第一个文件到 `mods` 目录。

**示例**：
```bash
mctl mod "sodium" 1.20.4 fabric
mctl mod "jei" 1.20.4 forge
```

### `mctl launch <version>`
以当前配置启动指定版本。

### `mctl config [options]`
不带选项时查看全部配置。支持以下参数：

| 选项 | 说明 |
|---|---|
| `--game-dir=<path>` | 指定 `.minecraft` 目录 |
| `--memory=<MB>` | 最大堆内存（默认 4096） |
| `--jvm-args="<args>"` | 追加 JVM 参数，如 `-XX:+UseG1GC` |
| `--isolation=<true\|false>` | 开启/关闭版本隔离 |

**示例**：
```bash
mctl config --memory=8192 --jvm-args="-XX:+UseG1GC -XX:+ParallelRefProcEnabled"
mctl config --isolation=true
```

### 全局选项
所有命令都支持通过 `-d <path>` 或 `--dir <path>` **临时**指定游戏目录，不影响全局配置：
```bash
mctl list -d "E:\OtherMC"
```

## 配置文件

MCTL 的配置保存在用户目录下：

- **Windows**: `%APPDATA%\MCTL\config.json`
- **Linux**: `~/.config/MCTL/config.json`
- **macOS**: `~/Library/Application Support/MCTL/config.json`

字段说明：

```json
{
  "gameDirectory": "D:\\Minecraft",           // 游戏目录
  "clientId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",  // Azure 应用 ID
  "accessToken": "....",                       // 微软登录 Token（自动维护）
  "username": "Steve",                         // 当前账户名
  "uuid": "....",                              // 账户 UUID
  "maxRamMb": 8192,                            // 最大堆内存
  "jvmArgs": "-XX:+UseG1GC",                   // 额外 JVM 参数
  "enableVersionIsolation": true,              // 是否开启版本隔离
  "isOfflineMode": false                       // 是否为离线模式
}
```

## 版本隔离说明

| 项目 | 隔离后读取位置 |
|---|---|
| `mods` | `versions/<版本名>/mods/` |
| `config`、`saves`、`options.txt` | `versions/<版本名>/` |
| `libraries`、`assets` | 全局共享（`.minecraft/libraries`、`.minecraft/assets`） |

> 使用 `mctl install ... --name=<name>` 时会自动创建隔离目录；开启 `--isolation=true` 后，所有版本会统一按此规则运行。

## 常见问题

**Q: Fabric 安装报错 `缺少 fabric-installer.jar`？**
A: 请前往 [Fabric 官网](https://fabricmc.net/use/installer/) 下载安装程序，并将其命名为 `fabric-installer.jar` 放在 `MCTL.exe` 同级目录。

**Q: 启动报 Java 相关错误？**
A: 确保 `java.exe` 在系统 PATH 中，且版本符合所启动 Minecraft 版本的要求（1.21 需要 Java 21+）。

**Q: 装了模组但游戏里没有生效？**
A: 检查是否开启了版本隔离。若开启，请确认模组位于 `versions/<版本名>/mods/` 下，而非全局 `.minecraft/mods/`。

## 使用的 Nuget 包

- [.NET 10](https://dotnet.microsoft.com/)
- [CmlLib.Core](https://github.com/CmlLib/CmlLib.Core) — Minecraft 安装与启动核心
- [CmlLib.Core.Auth.Microsoft](https://github.com/CmlLib/CmlLib.Core.Auth.Microsoft) — 微软账户认证
- [CmlLib.Core.Installer.Forge](https://github.com/CmlLib/CmlLib.Core.Installer.Forge) — Forge 安装
- [Modrinth API v2](https://docs.modrinth.com/) — 模组搜索与下载

## License

AGPL-3.0
