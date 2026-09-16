# Unity 操作与微调指南

本指南针对本项目，不要求先掌握全部 Unity。先学会打开正确场景、修改一份机关配置、观察运行结果、退出运行后保存。

## 1. 打开已配置的工程

本机许可证已恢复，2026-09-16 已成功构建。以后若再次提示许可问题，在 Unity Hub 设置的 Licenses / 许可证页按正规流程处理。不要向他人发送密码、验证码或许可证文件。

本机已安装 Unity 6000.3.10f1。编辑器位置：`C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe`。Hub 位于 `C:\Applications\Unity Hub`。

在 Hub 中添加项目文件夹 `D:\工程文件\Unity\A Light Within（心灯）`，选择 6000.3.10f1 打开。首次导入角色、字体和动画可能较慢；等待右下角进度结束，不要在导入时关闭编辑器。

## 2. 认识四个窗口

- Hierarchy：当前场景中的对象，例如玩家、桥梁、机关和灯光。
- Scene：编辑地图的工作视图；选中对象后按 F 聚焦。
- Game：真正由游戏相机看到的画面。
- Inspector：当前选中对象或资源的可编辑参数。

Project 窗口显示磁盘资源。Console 显示错误与日志。若 Console 有红色编译错误，先修复，不要继续构建。

## 3. 打开独立场景

直接双击 `Assets/Heartlight/Scenes/Heartlight.unity`。当前工程已经与课程场景生成器分离；常规修改后使用 Ctrl+S 保存，不需要执行场景重建。

## 4. 第一次试玩

在 Project 中双击 `Assets/Heartlight/Scenes/Heartlight.unity`。点击顶部 Play 三角形，然后在 Game 视图点击开始旅程。

测试鼠标：主菜单可以点击；开始后鼠标隐藏锁定、可转动视角；Esc 后鼠标重新出现。需要离开编辑器 Game 视图时，再按一次顶部 Play 停止运行。

运行中在 Inspector 修改的数值通常不会自动保留。可以先测试一组数值，记下结果，停止 Play 后再正式修改并保存。

## 5. 练习一：调整机关触发范围

1. 选中 `Assets/Heartlight/Config/bridge.asset`。
2. 观察 Title、Clue、Hint 和 Radius。
3. 小幅调整 Radius，保存。
4. 进入游戏，检查操作提示出现的距离以及隔墙是否会错误触发。
5. 记录「修改理由、体验差异、是否保留」。机关现在统一为单击 E，不再配置长按时间。

Id 是逻辑标识，不用于显示，随意改名会破坏流程。Prerequisites 是前置条件，不理解依赖前不要删除。

## 6. 练习二：调整方向机关

`bridge_align.asset` 对应桥前引流盘，`boss_link.asset` 对应终局引流盘。0、1、2、3 分别代表北、东、南、西。

改变 Target Rotation 时，**还必须同步调整场景中 Direction_Target 的位置**，让金色标记与正确方向一致。只改数据不改视觉会产生“玩家按线索操作却失败”的错误，这是需要主动测试的配置风险。

## 7. 练习三：调整主角和路径

在 Hierarchy 选中 `PLAYER_Traveler`。Character Controller 决定碰撞尺寸；子对象 `Traveler_Textured_Model` 决定视觉尺寸，两者不是一回事。

如果只想调整外观，先小幅修改模型子对象的 Scale，不要同时改变玩家根对象比例。确认脚底贴地、角色没有陷入地面，且跳跃和镜头仍正常。当前目标视觉身高约 1.58 米。

移动装饰物使用 W；旋转用 E；缩放用 R（这些是 Scene 编辑器快捷键，不是游戏内快捷键）。路径边缘装饰应避开实际步行和镜头空间。缺少 Collider 的模型只是可见，不一定可站立；给地面或桥加了 Collider 后也要在 Game 中真实行走测试。

## 8. 日常验证与构建

手动修改后先 Ctrl+S，再使用「心灯 → 验证当前场景」。该校验只覆盖关键配置与引用，不代表完整通关。

使用「心灯 → 构建 Windows 试玩版」。它读取已保存的 Heartlight 场景，输出到：

`D:\工程文件\Unity\A Light Within（心灯）\Builds\Windows`

双击 `A Light Within.exe`，在不启动 Unity 的情况下重新测试。若构建失败，Console/日志中的第一条实际错误比后面的连带报错更重要。

本轮不生成压缩包。以后手动压缩时保留全部 Unity 运行依赖以及资源许可文件，不要加入 Library、工程源文件、QA 日志或下载源包。

## 9. 自动测试的正确理解

构建后可以通过测试脚本启动显式测试模式；普通双击游戏不会运行测试、不会生成测试报告。测试会自动移动玩家和操作机关，验证部分流程与物理结果，但不能替代你实际使用键鼠通关或让陌生玩家判断提示是否易懂。

源码检查工具 `Tools/Check-HeartlightCompile.ps1` 只检查 C# 编译，不导入场景、不渲染、不保证模型动画绑定正确。测试阶段与真实完成状态见 QA_STATUS.md。

熟悉 PowerShell 后可从项目目录运行 `./Tools/Run-Heartlight.ps1 -Action Test` 启动已有包体的自动回归。`Build` 构建保存的场景，`Validate` 校验保存的场景。脚本显示进程编号只代表启动，必须看日志中的最终成功标志与错误信息。
