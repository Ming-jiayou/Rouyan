# Rouyan 项目图标使用说明

本文梳理项目中图标的来源、引用方式与替换方法，涵盖应用程序图标、窗口图标与系统托盘图标三部分。

## 概览
- 图标资源文件： [src/Rouyan/Assets/福州肉燕.ico](src/Rouyan/Assets/福州肉燕.ico)
- 应用程序图标设置： [ApplicationIcon](src/Rouyan/Rouyan.csproj:9)
- 窗口图标设置： [Window.Icon](src/Rouyan/Pages/View/ShellView.xaml:11)
- 系统托盘图标设置： [tb:TaskbarIcon IconSource](src/Rouyan/Pages/View/ShellView.xaml:36)
- 相关依赖： [PackageReference Include="H.NotifyIcon.Wpf"](src/Rouyan/Rouyan.csproj:56), [xmlns:tb="http://www.hardcodet.net/taskbar"](src/Rouyan/Pages/View/ShellView.xaml:8)

## 应用程序图标（exe 图标）
应用程序图标通过 csproj 的属性 [ApplicationIcon](src/Rouyan/Rouyan.csproj:9) 指定。它指向项目内的 ICO 文件，并在构建时嵌入到最终的可执行文件中。
- 当前配置： [ApplicationIcon](src/Rouyan/Rouyan.csproj:9) => Assets\福州肉燕.ico
- 资源包含： csproj 中通过 [Resource Include="Assets\福州肉燕.ico"](src/Rouyan/Rouyan.csproj:50) 将该 ICO 作为资源加入项目。
注意：更换应用程序图标时，仅修改 [ApplicationIcon](src/Rouyan/Rouyan.csproj:9) 指向的新路径即可；确保新 ICO 文件已被项目包含（建议仍置于 Assets 目录，并在 csproj 中以 [Resource Include](src/Rouyan/Rouyan.csproj:50) 方式加入）。

## 窗口图标（UI 标题栏）
主窗口 Shell 使用 WPF 的 [Window.Icon](src/Rouyan/Pages/View/ShellView.xaml:11) 属性设定标题栏图标，引用同一份 ICO 资源。
- 当前设置： [Window.Icon](src/Rouyan/Pages/View/ShellView.xaml:11) => /Assets/福州肉燕.ico
- 引用方式：使用 WPF 资源路径（以“/”开头的相对资源 URI），解析为应用程序内的资源。该方式要求图标文件以资源形式包含在项目中（参见 [Resource Include](src/Rouyan/Rouyan.csproj:50)）。
更换窗口图标时，将 [Window.Icon](src/Rouyan/Pages/View/ShellView.xaml:11) 的路径更新为新的 ICO 资源路径即可。

## 系统托盘图标（TaskbarIcon）
项目通过 H.NotifyIcon.Wpf 提供的托盘控件呈现系统托盘图标：
- 依赖包： [PackageReference Include="H.NotifyIcon.Wpf"](src/Rouyan/Rouyan.csproj:56)
- XAML 命名空间： [xmlns:tb="http://www.hardcodet.net/taskbar"](src/Rouyan/Pages/View/ShellView.xaml:8)
- 托盘控件： [tb:TaskbarIcon](src/Rouyan/Pages/View/ShellView.xaml:35)
- 图标设置： [tb:TaskbarIcon IconSource](src/Rouyan/Pages/View/ShellView.xaml:36) => /Assets/福州肉燕.ico
托盘控件的 IconSource 与窗口图标采用同一资源路径，因此替换图标时只需同步更新该属性的路径。

## 资源文件位置与包含方式
- 物理文件路径： [src/Rouyan/Assets/福州肉燕.ico](src/Rouyan/Assets/福州肉燕.ico)
- 项目资源包含： [Resource Include="Assets\福州肉燕.ico"](src/Rouyan/Rouyan.csproj:50)
- XAML 引用路径：以“/Assets/xxx.ico”形式引用（WPF 资源 URI），由框架解析为程序集内资源。
建议统一在 Assets 目录管理图标资源，保持应用图标、窗口图标与托盘图标来源一致，便于维护与替换。

## 更换或新增图标的建议步骤
1. 将新的 ICO 文件放入 Assets 目录（例如 NewApp.ico）。
2. 在 csproj 中确保以资源方式包含新图标（如需要），参考 [Resource Include](src/Rouyan/Rouyan.csproj:50) 的写法。
3. 更新以下引用：
   - 应用程序图标： [ApplicationIcon](src/Rouyan/Rouyan.csproj:9) => Assets\NewApp.ico
   - 主窗口图标： [Window.Icon](src/Rouyan/Pages/View/ShellView.xaml:11) => /Assets/NewApp.ico
   - 托盘图标： [tb:TaskbarIcon IconSource](src/Rouyan/Pages/View/ShellView.xaml:36) => /Assets/NewApp.ico
4. 重新构建项目，验证 exe 文件与运行时 UI/托盘图标均已生效。

## ICO 文件规格建议
为获得良好的显示效果与兼容性，建议使用包含多尺寸位图的多分辨率 ICO：
- 最小：16×16（托盘与小型 UI）
- 常用：32×32、48×48（窗口与列表）
- 高分：256×256（应用程序图标与高 DPI）
同时确保透明背景与清晰的边缘处理，以适配浅色/深色主题。

## 常见问题
- 仅更改 XAML 路径但未包含资源：如果 [Window.Icon](src/Rouyan/Pages/View/ShellView.xaml:11) 或 [IconSource](src/Rouyan/Pages/View/ShellView.xaml:36) 指向的文件未被项目包含为资源，运行时可能无法加载图标。请检查 csproj 中的 [Resource Include](src/Rouyan/Rouyan.csproj:50)。
- 使用 PNG/SVG：WPF 的 [Window.Icon](src/Rouyan/Pages/View/ShellView.xaml:11) 与托盘 [IconSource](src/Rouyan/Pages/View/ShellView.xaml:36) 推荐使用 ICO，以确保在所有 DPI 与系统位置显示无误；若需在控件中显示矢量图标，可另外使用矢量资源或图标库，不影响应用/托盘图标。

## 结论
Rouyan 项目采用统一的 ICO 资源 [src/Rouyan/Assets/福州肉燕.ico](src/Rouyan/Assets/福州肉燕.ico)，分别通过 [ApplicationIcon](src/Rouyan/Rouyan.csproj:9)、[Window.Icon](src/Rouyan/Pages/View/ShellView.xaml:11) 与 [tb:TaskbarIcon IconSource](src/Rouyan/Pages/View/ShellView.xaml:36) 实现应用程序图标、窗口图标与系统托盘图标的设置。按本文步骤即可安全替换或扩展图标资源。