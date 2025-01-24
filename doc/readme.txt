TC的插件现在分为3类，Lister插件（.wlx）、FileSystem插件（.wfx）和Packer插件（.wcx）（另外还有传说中6.5新加的Content插件（.wdx），现在还不见庐山真面目，先按下不表）。尽管扩展名穿上了不同的马甲，但其本质都是一样的，都是Windows的DLL动态链接库，我们需要做的仅仅是按照TC的规范，实现其给定的DLL接口函数，最后把 dll扩展名改成相应扩展名就万事大吉了（使用不同的扩展名，只不过使其容易区分而已）。

1. 开发方法、环境和工具
如前所述，TC插件本质上都是Windows的DLL动态链接库，其开发方法和普通的DLL程序开发并没有什么不同，任何一个可用来开发DLL的环境和工具都可以用来开发TC插件。
因此，写TC插件首先得熟悉DLL的编写，更高一点的要求是熟悉一些常用的Windows API，对于一个程序员老手来说应该是很容易的事情，而用什么开发环境和工具都是次要的。但是对于新手，笔者推荐使用VC或者Delphi来编写，一方面的原因是TC作者提供的函数头文件声明只有C/C++和Pascal，可以省却改写成别的语言的麻烦；另一方面原因是TC作者给的帮助、例子，甚至网上公开源码的插件大都是基于此两种环境的，而有一个可参考的源码例子能在很大程度上提供编写帮助。另外由于TC作者提供的例子（VC环境下）已经搭建起了 插件的整个框架，我们甚至可以直接在此例子上修改开发。

Lister插件的官方源码例子：http://ghisler.fileburst.com/lsplugins/listplugsample.zip。
FS插件的官方源码例子：http://ghisler.fileburst.com/fsplugins/sampleplugin.zip。
其它第3方插件的源码例子可以从以下各个插件站点找到：
http://www.ghisler.com/plugins.htm
http://www.totalcmd.net
http://clubtotal.free.fr

2. Lister 插件
首先说明一下Lister插件的工作过程：默认快捷键情况下，当在TC中对一个文件按下F3或者Ctrl+Q的时候，TC会检查wincmd.ini中[ListerPlugins]一节，以下是个例子：
[ListerPlugins]
0=%COMMANDER_PATH%\plugins\Imagine\Imagine.wlx
0_detect="MULTIMEDIA"
1=%COMMANDER_PATH%\plugins\FlashView\FlashView.wlx
1_detect="([0]="F" & [1]="W" & [2]="S")|([0]="C" & [1]="W" & [2]="S")"

TC会顺序检查该节中每个插件对应的x_detect字段，该字段实际上是一个逻辑判断表达式，如果此表达式结果为真，TC就会Load该插件并调用其ListLoad函数，否则检查下一个插件。如果插件对应的x_detect字段根本就不存在，TC会调 用插件的ListGetDetectString函数，如果此函数存在，TC会将函数的返回结果保存在x_detect字段中再检查，如果此函数仍然不存在，则TC就直接调用插件的ListLoad函数。最后，如果调用了ListLoad函数，还要判断该 函数的返回值，如果该值是一个Windows句柄，则插件调用成功；若返回值为0（NULL），则调用失败，继续检查下一个插件。

Lister插件的详细接口函数介绍可以从网上下到：http://ghisler.fileburst.com/lsplugins/listplughelp1.2.zip。

实际上，Lister插件必需的函数只有ListLoad一个，它是插件的核心实现函数。插件必须在此函数中读入文件内容，创建一个窗口并显示文件内容，最后返回这个窗口的句柄，Lister会获得并Subclass该句柄，并在Lister内显示。

其余的接口函数都是可选函数，但其中有两个比较重要的函数：ListGetDetectString和ListCloseWindow，这两个函数与ListLoad一起构成了插件的主干部分。

ListGetDetectString虽然是可选函数，但是我强烈建议实现这个函数，这对Lister的效率有很大的影响。从Lister工作过程可以看出，这个函数仅仅是在插件第一次被调用时才被调用，功能是返回一个检测字符串以填写x_detec t字段，在此之后，Lister都将只检查此字段以决定是否调用插件。因此，一个好的检测字符串可以让Lister迅速判断插件是否适用于显示文件，如果没有这个，Lister每次显示文件都将不得不把插件一个个都Load进来、分配空间、调用ListL oad，直到找到一个合适的，这个速度可是偏离了Lister快速查看的本意。例如：检测字符串是“ext="HTM" | ext="HTML"”，这时TC只需要根据文件扩展名是否是htm或html就可以直接判断该插件是否适合，而无需读入任何文件，这就是为什么在插件众多的时候，Lister仍然能很快显示的原因。当然，在某些情况下，确实难以给出一个合适的检测字符串，这就要求ListL oad函数在文件类型判断上的速度应该尽可能快。

ListColseWindow是在用户关闭Lister或在Lister中显示另一个文件时被调用，如果此函数不存在，Lister将直接调用DestroyWindow()关闭插件窗口。通常情况下，我们需要在这里做窗口关闭时的善后工作，包括释放 资源等等。

除以上3个函数外，其它的几个可选函数都涉及一些具体的附加功能，取决于具体需求。

2. FileSystem插件
与Lister插件不同，当用户安装一个FS插件时，该插件就会被第一次Load进来，并调用FsGetDefRootName以获得插件名称，也是该FS根目录的名字，如果这个函数不存在，TC会直接使用wfx文件的名字做插件名称（去掉文件扩展名） ，该名称会保存在wincmd.ini文件[FileSystemPlugins]一节，下面是个例子：
[FileSystemPlugins]
Linux-drives=%COMMANDER_PATH%\plugins\ex2fs\ex2fs.wfx
Calendar=%COMMANDER_PATH%\plugins\calendar\calendar.wfx
Shared files=%COMMANDER_PATH%\plugins\netmon\NetMon.wfx
这样，当用户进入网上邻居时，TC不需要Load插件就可以把所有插件列出来，插件只有在用户试图进入FS插件目录时才真正被Load进来。

大致结构上，FS插件需要提供的接口函数与一个真正文件系统的基本函数有些类似。其必需的函数有4个：FsInit、FsFindFirst、FsFindNext和 FsFindClose，是不是看了很眼熟，就和平时列举一个目录下所有文件所用的函数 结构一样。顾名思义，FsInit是用于插件初始化的函数，同时TC会传给插件3个TC提供给FS插件调用的callback函数地址（下面会介绍）；FsFindFirst和FsFindNext用于列举一个目录下所有的文件；FsFindClose用 于终止FsFindFirst/FsFindNext的文件列举。有了这4个函数，就构成了最小的FS插件，就可以浏览FS插件的各个目录了。

有了文件目录结构后，就到了根据需要提供各种文件功能的时候了，包括删除文件FsDeleteFile；删除目录FsRemoveDir；建立目录 FsMkDir；执行文件FsExecuteFile；设置文件属性FsSetAttr；设置文件时间Fs SetTime；拷贝文件FsGetFile/FsPutFile/FsRenMovFile。大致上都和普通文件操作功能差不多，需要说明的是拷贝文件，由于FS插件的特殊性，拷贝文件分成了3种情况：FsGetFile是从FS中往本地硬盘拷贝；FsPutFile是从本地硬盘往FS拷贝；FsRenMovFile是 在FS内部拷贝、移动或重命名文件。

此外，根据FS插件的需要，TC还提供了3个callback函数以供其调用：
1) ProgressProc，用于显示一个进度条，例如拷贝文件时的进度条。
2) LogProc，用于显示FTP工具栏，插件可以在工具栏中显示log信息，并写入log文件。如果显示了FTP工具栏，点击“断开连接”按钮时还将调用插件的FsDisconnect函数。
3) RequestProc，用于显示一个输入对话框，例如要求用户输入用户名和密码等等。

最后，FS插件还可以实现一个函数FsStatusInfo，如果这个函数被实现，TC在调用插件的任何函数（除了FsInit和FsDisconnect）之前和之后都将调用此函数，以方便插件释放资源等等操作。

FS插件的详细接口函数介绍可以从网上下载：http://ghisler.fileburst.com/fsplugins/fspluginhelp1.3.zip。