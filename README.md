# GiveGold

Slay the Spire 2 联机版队友金币赠送模组。点击顶部金币区域即可打开面板，选择队友并输入金额，一键赠送。

A Slay the Spire 2 co-op mod that lets you give gold to teammates. Click the gold display in the top bar, pick a teammate, enter an amount, and send.

## 功能 / Features

- 点击顶部栏金币图标打开/关闭赠送面板
- 下拉列表选择在线队友
- 输入正整数金额，一键赠送
- 自动检测游戏状态（仅限联机局、非战斗中可用）
- 中/英双语界面，跟随系统语言自动切换
- 赠送消息全队广播，接收方自动到账

---

- Click the gold icon in the top bar to open/close the panel
- Select an online teammate from the dropdown
- Enter a positive integer amount and send with one click
- Automatically validates game state (multiplayer only, out of combat)
- Bilingual UI (Chinese/English), auto-detects system language
- Transfer broadcast to all players, recipient auto-receives gold

## 安装 / Installation

1. 下载`GiveGold.zip`，解压到 Slay the Spire 2 的 `mods` 目录
2. 启动游戏即可

---

1. Download `GiveGold.zip` and extract it to the `mods` directory of Slay the Spire 2.
2. Launch the game

## 使用方式 / Usage

1. 进入多人联机局（非战斗状态）
2. 点击顶部栏的金币数字
3. 在下拉菜单中选择一位在线队友
4. 输入要赠送的金币数量（正整数）
5. 点击「赠送」按钮
6. 你和队友都会看到赠送结果提示

---

1. Enter a multiplayer run (outside of combat)
2. Click the gold number in the top bar
3. Select an online teammate from the dropdown
4. Enter the amount of gold to send (positive integer)
5. Click the "Send" button
6. Both you and your teammate will see the transfer result

## 限制 / Limitations

- 仅限真正的多人联机局（非单人伪联机）
- 战斗中不可赠送
- 必须有至少一位在线队友
- 赠送金额不能超过当前持有金币

---

- True multiplayer runs only (not single-player fake multiplayer)
- Cannot send gold during combat
- At least one online teammate required
- Amount cannot exceed your current gold

## 构建 / Build

```bash
dotnet build   # 编译 DLL 并复制到 mods 目录
dotnet publish # 额外导出 Godot .pck 到 mods 目录
```

构建前请在 `GiveGold.csproj` 中配置 `$(Sts2Path)` 和 `$(GodotPath)`。

Configure `$(Sts2Path)` and `$(GodotPath)` in `GiveGold.csproj` before building.

## 致谢 / Credits

- 作者 / Author: binglieyan
- 版本 / Version: v1.1.0
- 灵感来源 / Inspired by: https://github.com/blz111/giftgold-sts2-mod
