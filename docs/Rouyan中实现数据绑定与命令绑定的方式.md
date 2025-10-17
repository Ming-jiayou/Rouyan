# Rouyan 中的数据绑定与命令绑定（精简说明）

本文以文字为主，概述项目在 Stylet MVVM 下的数据绑定与命令绑定做法，帮助快速理解整体设计与运行机制。

一、背景与总体设计

- 采用 Stylet 的 MVVM 模式。视图与视图模型按约定组织，由外层承载器统一管理当前显示页面；左侧是固定导航区域，右侧是动态内容区域。
- 通过依赖注入配置导航控制器与页面 ViewModel 工厂；应用启动时完成导航委托关系绑定并进入首页。

二、数据绑定（Data Binding）

- 单向与双向：常用双向绑定以便视图与视图模型属性保持同步，变更立即生效。
- 变更通知：视图模型基于 Stylet 的基类，设置属性时会触发变更通知，从而推动界面更新。
- 常见绑定类型：
  - 文本类（例如剪贴板文本、文件路径）用于输入与显示。
  - 图片类（例如剪贴板图片）用于预览展示。
- 状态驱动的界面：视图模型中的状态属性变化会自动影响控件可用性与显示，例如运行中的状态会禁用“运行”按钮并启用“取消”按钮。

三、命令绑定（Command/Action）

- 按钮与公开方法之间采用动作绑定，点击即可调用对应的视图模型方法。
- 支持命令参数：可将界面上当前选择或输入的值作为参数传入方法，实现一个方法处理多种操作。
- 可执行性（CanExecute）：只需在视图模型中提供对应的布尔属性，即可自动控制相关按钮的启用/禁用，无需在界面层额外写条件。

四、导航与页面切换

- 统一导航：导航控制器根据请求创建目标页面的视图模型，通过委托通知外层承载器切换当前激活项。
- 承载与显示：外层承载器负责激活目标视图模型，右侧内容区域自动展示其对应视图；左侧导航区域绑定的是一个长期存在的视图模型，用于触发各类导航操作。
- 启动流程：应用启动时配置依赖注入、建立导航委托关系，并默认进入首页，保证应用一打开就有合理的初始界面。

五、典型交互链路（以文字描述）

- 从侧边导航进入某页面：用户点击导航项 → 触发导航方法 → 导航控制器生成目标视图模型并通过委托请求切换 → 承载器激活该视图模型 → 右侧内容区域随之更新。
- 在页面内执行操作：用户在页面上选择操作并点击执行 → 当前选择作为参数传入视图模型方法 → 视图模型执行业务逻辑并更新属性 → 绑定到这些属性的控件自动刷新显示。

六、设计优势

- 解耦：视图只负责呈现与触发，业务在视图模型中统一实现，导航也通过独立控制器管理。
- 可维护：属性与命令的命名及职责清晰，新增页面或操作只需扩展对应视图模型与导航控制器。
- 可测试：业务逻辑集中在视图模型，便于隔离测试。
- 体验一致：可执行性与状态由视图模型集中控制，按钮可用性等行为一致、可预测。

参考文件

- [`src/Rouyan/Bootstrapper.cs`](src/Rouyan/Bootstrapper.cs)
- [`src/Rouyan/NavigationController.cs`](src/Rouyan/NavigationController.cs)
- [`src/Rouyan/Pages/View/ShellView.xaml`](src/Rouyan/Pages/View/ShellView.xaml)
- [`src/Rouyan/Pages/ViewModel/ShellViewModel.cs`](src/Rouyan/Pages/ViewModel/ShellViewModel.cs)
- [`src/Rouyan/Pages/View/HeaderView.xaml`](src/Rouyan/Pages/View/HeaderView.xaml)
- [`src/Rouyan/Pages/ViewModel/HeaderViewModel.cs`](src/Rouyan/Pages/ViewModel/HeaderViewModel.cs)
- [`src/Rouyan/Pages/View/HomeView.xaml`](src/Rouyan/Pages/View/HomeView.xaml)
- [`src/Rouyan/Pages/ViewModel/HomeViewModel.cs`](src/Rouyan/Pages/ViewModel/HomeViewModel.cs)
- [`src/Rouyan/Pages/View/TerminalAgentView.xaml`](src/Rouyan/Pages/View/TerminalAgentView.xaml)
- [`src/Rouyan/Pages/ViewModel/TerminalAgentViewModel.cs`](src/Rouyan/Pages/ViewModel/TerminalAgentViewModel.cs)