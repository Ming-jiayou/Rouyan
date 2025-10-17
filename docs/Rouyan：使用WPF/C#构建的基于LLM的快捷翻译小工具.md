# Rouyan：使用WPF/C#构建的基于LLM的快捷翻译小工具

## 前言
都说技术服务于业务，对我个人而言可能谈不上有什么业务，但是确实有一些个人的需求。我很喜欢C#也很喜欢WPF，最近刚学了Stylet这个框架，就想着先试着用它搞一个解决自己阅读英文文献一些小需求的小工具，现在开源出来，希望也能帮助到跟我有一样需求的朋友。

## Rouyan介绍

`Rouyan`是一个使用WPF/C#构建的基于LLM的快捷翻译(也能自定义其它功能）的小工具。

`Rouyan`的简介是**Less Copying,More Convenience**，说实话实现的功能用ChatBox类工具如CherryStudio也都能实现，所以没什么特别的，只是对我而言有些场景减少了复制粘贴。接下来我将以自己的使用场景来介绍`Rouyan`的功能。

**1、直接翻译到文件**

有时候我们会想把翻译内容直接保存到一个文件，使用ChatBox类工具流程可能是这样的：

```csharp
flowchart LR
    A[复制原始文本] -->B[粘贴到ChatBox]
    B --> C[获取LLM返回内容]
    C --> D[复制翻译文本]
    D --> E[粘贴到文件]
```

![](https://files.mdnice.com/user/50031/5d8fbcde-9d0e-4267-a709-8f884e8d8c4c.png)

使用`Rouyan`的流程是这样的：

```csharp
flowchart LR
    A[复制原始文本] -->B[按下快捷键]
    B --> C[翻译内容到文件]
```


![](https://files.mdnice.com/user/50031/1765733d-3ae3-4a61-aa9c-7727f4b0cb3c.png)

**实际使用过程**

打开`Rouyan`，先选择翻译内容要保存至的文件：


![](https://files.mdnice.com/user/50031/aac3f5ff-63f3-4a26-b430-0e9fcd048ec1.png)

复制想要翻译的文本：

![](https://files.mdnice.com/user/50031/95addc5e-b580-45b4-bb6c-da3a954e01bb.png)

按下`Tab + K`快捷键：

出现等待窗体：

![](https://files.mdnice.com/user/50031/71819737-0689-46c3-a418-4060f0279580.png)

翻译内容直接写入文件：

![](https://files.mdnice.com/user/50031/606ee1d1-ba88-40af-a2a9-4608fa95b56c.png)

**2、直接流式显示**

有时候不需要保存至文件，比如我们只是想知道这段话是什么意思即可。

还是一样复制文本，按下`Tab + L`即可流式显示翻译内容：

![](https://files.mdnice.com/user/50031/cf3b6872-23cb-495d-8515-f556e8b63868.png)

**3、解释图表**

有时候光有LLM还不够，还需要VLM，比如解释图表的功能。

随便截图一张：

![](https://files.mdnice.com/user/50031/b8dd5b07-4eda-4c19-8eec-4c1b701be409.png)

按下`Tab + D`流式解释图表内容：

![](https://files.mdnice.com/user/50031/bce19b62-5f6b-419a-b79e-9df35142f23b.png)

当然你也可以扩展自己的功能，目前`Rouyan`的设计是这样的，总共有8个快捷键绑定，可以从关于页面看到：

![](https://files.mdnice.com/user/50031/7a928f92-97ca-4481-9a1b-7ee6208626c9.png)

**如何增加基于提示词的扩展功能**

接下来我将向大家介绍一下如何扩展自定义的功能。

比如在看英文文献的时候，遇到不懂的单词，想要选中可以解释意思。

首先我们看当前`Rouyan`的提示词管理：

![](https://files.mdnice.com/user/50031/6acd7bfb-d810-42c0-af9a-a41910ae4775.png)

打开PromptConfig.txt：

![](https://files.mdnice.com/user/50031/22f53e2c-9929-4ba2-8807-6cf7e4370771.png)

这里配置了LLM与VLM的两个提示词分别是什么。

现在我们在LLMPrompts新增一个03.txt：

![](https://files.mdnice.com/user/50031/74687e7c-387a-4915-ad8e-e70dce4cefef.png)

打开`Rouyan`来到提示词管理界面：

![](https://files.mdnice.com/user/50031/fbb73699-1fb7-4f59-9e07-290709254336.png)

可以看到我们刚刚增加的提示词，然后将其设置为LLM提示词2。

会发现PromptConfig.txt中已经改了：

![](https://files.mdnice.com/user/50031/3c0c5b8e-1991-48fa-9f41-5e9b9c7e0cca.png)

现在按`Tab + I`即可使用了：

![](https://files.mdnice.com/user/50031/2dd9db35-98ff-4a33-bf49-9290ab1fa754.png)

## Rouyan安装使用

`Rouyan`提供两种方式安装，一种直接压缩包解压，一种安装包安装。

`Rouyan`开源地址：https://github.com/Ming-jiayou/Rouyan

下载地址：https://github.com/Ming-jiayou/Rouyan/releases/tag/Rouyan-v1.0.0

![](https://files.mdnice.com/user/50031/8a88dc3b-cce5-4cd3-a3c4-74e5e3a0d8ae.png)

第一个包含了.net8框架，剩下两个不包含，如果提示没有安装.net8，安装一下即可。

推荐使用下面那两个不包含框架的。

如果不方便访问GitHub，可以向公众号发送Rouyan，获取网盘链接。


![](https://files.mdnice.com/user/50031/6c6621b7-c58e-47f4-b460-0fd65d9498cb.png)

在设置页面填入apikey即可，如果没有额度，可以使用免费模型，智谱有免费的LLM与VLM可以用。