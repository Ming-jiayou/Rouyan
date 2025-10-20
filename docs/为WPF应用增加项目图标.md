# 为WPF应用增加项目图标

## 前言

本文梳理一下怎么给WPF应用增加自己的项目图标，还是以Rouyan为例进行说明。

## 过程

首先想一下这个项目图标想要运用在哪些地方。在WPF中你想为你的应用增加你的项目图标主要在这三个地方。

1、应用程序图标设置

2、窗口图标设置

3、系统托盘图标设置

**应用程序图标设置**

一个一个来，先来看下应用程序图标设置：

首先准备好自己的ico图标文件，一般习惯在项目中新建一个Assets存放图标文件，如下所示：

![](https://mingupupup.oss-cn-wuhan-lr.aliyuncs.com/imgs/d25a29bd-0ac7-4705-af19-fa9fcdbe1283.png)

准备好一个ico文件，如下所示：

![](https://mingupupup.oss-cn-wuhan-lr.aliyuncs.com/imgs/eeedb424-1f77-475e-a666-763187350046.png)

一般为了美观会增加一点圆角，可以使用在线的工具增加一下圆角，然后通过png转ico工具制作ico图片。

右键项目，点击编辑项目文件：

![](https://mingupupup.oss-cn-wuhan-lr.aliyuncs.com/imgs/18575f76-fe6e-42b7-b47e-a3881a0b5fa6.png)

首先将这个图标文件设置为资源：

![](https://mingupupup.oss-cn-wuhan-lr.aliyuncs.com/imgs/fddadd65-58eb-4019-b623-1b88a2364ce7.png)

将指定的文件标记为WPF应用程序的资源文件。

这意味着：

这些文件会被编译到程序集中

可以在XAML代码中通过相对路径直接引用

文件会随应用程序一起分发，不需要单独部署

然后在这里设置应用程序图标：

![](https://mingupupup.oss-cn-wuhan-lr.aliyuncs.com/imgs/c7d403ba-195d-4782-bd3b-20f3a8bfd747.png)

应用程序图标就是exe那个图标，在这里可以看到：

![](https://mingupupup.oss-cn-wuhan-lr.aliyuncs.com/imgs/c099fbd7-2715-4af4-b0c3-c69964c8e6e0.png)

也是任务栏这个图标：

![](https://mingupupup.oss-cn-wuhan-lr.aliyuncs.com/imgs/7a6152c7-9271-45ae-a82b-380d24ecba03.png)

**窗口图标设置**

在Window中设置ICon属性就设置了窗口图标：

![](https://mingupupup.oss-cn-wuhan-lr.aliyuncs.com/imgs/5335ceae-10ae-45b7-8a85-d44819cd7aca.png)

就是这个位置的图标：

![](https://mingupupup.oss-cn-wuhan-lr.aliyuncs.com/imgs/ec5fcc7b-1332-418f-87c0-2b1126575442.png)

**系统托盘图标设置**

![](https://mingupupup.oss-cn-wuhan-lr.aliyuncs.com/imgs/4102cc4a-65df-47b0-ae3f-1201bc596ced.png)

就是对应这个位置的图标：

![](https://mingupupup.oss-cn-wuhan-lr.aliyuncs.com/imgs/e36c0cbc-0ce9-4beb-b279-3ee0a07c4996.png)

以上就是在开发WPF应用时如果你想为这个应用添加自己的图标最常设置的几个地方，希望对你有所帮助。
