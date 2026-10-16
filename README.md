# 大肥鱼代抽1.0

Windows 桌面陪伴软件。大肥鱼可以工作、看书、模拟抽烟，并通过背包和商店管理装备。界面支持人物大小调整、任务栏最小化和托盘收起。

这是正式版源码：新存档有 **200 元**，每工作 **20 秒赚 1 元**。不包含演示模式、无限金额、十倍工作计时、直接死亡按钮或时间滑块。已有存档继续沿用自己的余额；不是每次打开都重新赠送 200 元。

## 安装和使用

运行发布的 `大肥鱼代抽1.0.exe` 安装程序，完成后从桌面或开始菜单启动。支持 Windows 10 / 11，需要 .NET Framework 4.8 或更高的 4.x 版本。安装到当前用户目录，无需管理员权限。

1. 在商店买烟，再在背包中选择并点击“装备”。
2. 点击“开始工作”，大肥鱼出现在桌面；双击人物开始抽烟，单击可以加速。
3. 暂停后可以继续抽烟。醉态动画的 20 秒内不能开始抽烟，正在进行的抽吸也会暂停，结束后恢复。
4. 烟头或废弃设备点击 5 下清理。大米饭需要在桌面抓住并拖给死亡人物。
5. 点击窗口最小化进入任务栏；点击关闭时可选择退出或收起到右下角托盘。

存档位置：`%LOCALAPPDATA%\SmokeSimulator\save.xml`。卸载保留存档。不要把自己的存档上传到公开仓库。

## 从源码构建

使用 Windows PowerShell 5.1 和 .NET Framework 4.8。编译器由系统的 .NET Framework 提供，不依赖 Visual Studio、Node.js 或 NuGet 下载。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
```

输出：`bin\大肥鱼代抽1.0.exe`，运行时必须保留旁边的 `assets` 文件夹。

```powershell
# 运行逻辑和素材检查
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -Test
# 可选：额外打开独立测试窗口，验证 UI 和桌面交互；使用独立临时存档
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -Test -UiTest
```

UI 测试代码仅在 `UI_TESTS` 编译条件下进入测试程序，不进入正式程序。测试可执行文件不会进入安装包。

## 构建安装包

从 [NSIS 官网](https://nsis.sourceforge.io/Download) 获取 Unicode NSIS。本项目使用 NSIS 3.12 验证过。将 `makensis.exe` 的路径传入：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\packaging\build-installer.ps1 -MakeNsis 'C:\Tools\nsis-3.12\makensis.exe'
```

输出 `dist\大肥鱼代抽1.0.exe`，这是可分发的安装程序，包含卸载入口。构建脚本只收集正式程序、49 张运行素材和授权说明，不收集测试程序、测试数据或个人存档。

## 上传 GitHub

创建一个空仓库，把本文件所在目录中的全部内容上传即可；保留 `assets/runtime` 图片，否则人物和商品无法正常显示。源文件夹名称可以是中文，仓库名可用 `dafeiyu-daichou`。

| 文件/目录 | 内容 |
| --- | --- |
| `src/` | C# 程序源码 |
| `assets/runtime/` | 人物和商品 PNG 素材 |
| `assets/provenance/` | 素材来源和导入记录 |
| `tests/` | 逻辑与素材检查 |
| `packaging/`、`build.ps1` | 编译和安装包构建脚本 |
| `LICENSE`、`ASSETS_NOTICE.md` | 代码许可与素材说明 |

`bin/`、`dist/`、`test-output/` 已在 `.gitignore` 中排除。发布安装包时，可把 `dist` 中的 EXE 单独上传到 GitHub Releases。

## 授权

程序代码及随附构建脚本按 [MIT](LICENSE) 授权。**人物图片、商品照片、包装图案及商标不包含在 MIT 代码授权内**，来源和授权范围见 [素材说明](ASSETS_NOTICE.md)。
