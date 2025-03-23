
● F4Menu v0.59
http://www.shannycn.com
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━


★ F4Menu 介绍
───────────────────────────────────────
顾名思义，F4Menu就是按F4键弹出的菜单，菜单上列出各编辑程序，用户可按文件类型
选择不同的程序。也可让F4Menu在后台运行，不显示菜单，自动按配置选择相应的程序
打开。
不过从0.50版开始，F4Menu支持用户自定义快捷键，不再局限于按F4了。
F4Menu允许被任意程序调用，尤其适合作为Total Commander的F4编辑程序。
F4是Total Commander/TC编辑文件的快捷键，即调用指定编辑程序打开所选文件。
然而，由于TC只允许为F4绑定一个编辑程序，因此它无法根据文件类型选择打开程序，
更无法为同一类型选择不同的程序。F4Menu恰好能完美地解决这些问题。

★ 功能简介
───────────────────────────────────────

1. 一键菜单: 按一键弹出程序选择菜单，并用所选程序打开文件

2. 一键打开: 按一键用不同的编辑程序一次性打开多个文件

3. 默认打开: 按一键用默认编辑程序打开文件

4. 菜单项支持拖放，方便用户调整

5. 提供设置窗口，方便配置编辑程序

★ 使用说明
───────────────────────────────────────

1. 启动方式
　1.1 直接执行
　　　F4Menu.exe [-bdfmpq] [filelist]
　　　　-b　　　　强制后台模式，忽略快捷键设置
　　　　-d　　　　强制默认模式，忽略快捷键设置
　　　　-f　　　　强制前台模式，忽略快捷键设置
　　　　-m　　　　菜单模式，若无filelist则弹出菜单，用于创建新文件
　　　　-p　　　　参数模式，忽略TC状态，仅从参数获取文件名列表
　　　　-q　　　　静默模式，若无filelist则直接退出，不显示任何对话框
　　　　filelist　空格分隔的多个文件名，文件名若包含空格，需在前后加双引号
　　　　　　　　　指定filelist后参数 -q, -m 将被忽略
　　　若未指定任何参数则打开配置对话框
　1.2 由TC调用
　　　需先将F4Menu.exe设置成Total Commander的F4编辑程序：
　　　　Total Commander -> 配置 -> 选项 -> 编辑/查看 -> 编辑程序
　　　然后在TC窗口按F4键即可

2. 运行模式
　2.1 默认打开模式
　　　直接用默认编辑程序打开，默认设置对应快捷键为F1，可通过配置修改：
　　　　弹出菜单->设置->选项->运行模式-调用按键->默认打开模式
　　　注意：快捷键需同时在TC或其他调用F4Menu的软件中设置
　2.2 前台模式
　　　运行时会弹出程序选择菜单，默认设置下按F4使用该模式，可通过配置修改：
　　　　弹出菜单->设置->选项->运行模式-调用按键->前台模式
　　　1. 菜单首项为当前要打开的文件名，若显示为"*.xxx"，则说明有多个该类型文件，
　　　　单击该项会调用系统关联程序打开文件，若无程序关联，则显示为灰色。
　　　2. 菜单次项为默认编辑程序，所有未关联的文件类型都默认使用该程序打开
　　　3. 其他编辑程序，已关联到当前文件的显示为粗体，路径失效的则显示为灰色
　2.3 后台模式
　　　运行时直接按配置打开文件，不弹出程序选择菜单
　　　默认设置下按WIN+F4使用该模式，可通过配置修改：
　　　　弹出菜单->设置->选项->运行模式-调用按键->后台模式
　　　1. 若文件有程序关联，则用最靠前的关联程序打开，否则使用默认编辑程序打开；
　　　若文件同时被默认编辑程序和其他程序关联，则优先使用默认编辑程序

3. 快捷键
　3.1 弹出菜单时
　　　以下快捷键仅在弹出菜单上有效
　　　　Space 用最适合的关联编辑程序打开，若无关联则使用默认编辑程序打开
　　　　Enter 用所选程序打开文件
　　　　Ctrl+Enter 关联并用所选程序打开文件
　　　　菜单项首字母 为菜单项的隐含助记键，其优先级低于 '&' 前缀的助记键
　3.2 程序启动时
　　　所有快捷键均可自定义，但需要先在其他应用程序里设置调用F4Menu的快捷键，
　　　请参照FAQ7及FAQ8。
　　　默认的启动快捷键如下
　　　　F1 默认打开模式
　　　　F4 前台模式启动
　　　　WIN+F4 后台模式启动

4. 选项设置
　4.1 编辑程序设置窗口
　　　用于设置编辑程序的参数，可通过 弹出菜单->设置 或直接运行 F4Menu 打开
　　　1. 程序路径支持所有系统环境变量，如" SystemRoot%", "COMMANDER_PATH" 等
　　　2. 程序路径支持相对路径，相对于F4Menu所在目录
　　　3. 扩展名支持通配符'*'及'?'，前者匹配任意长度字串，后者匹配任意单字符
　　　4. 扩展名之间用逗号","分隔
　　　5. 上下拖动列表项可以改变次序
　　　6. 支持拖放可执行文件或快捷方式到设置窗口来添加程序项
　4.2 选项窗口
　　　用于设置 F4Menu 选项，可通过 弹出菜单->设置->选项 或直接运行 F4Menu 打开
　　　1. 可设置弹出菜单的位置，部分选项仅当通过 TC 启动时生效
　　　2. 可设置一次最多打开的文件数，以防止误操作
　　　3. 可自定义默认打开、前台及后台运行模式的快捷键

★ 软件安装
───────────────────────────────────────

下载地址: F4Menu.zip (53Kb，Unicode) http://www.shannycn.com
系统需求：Windows NT/2000/XP/2003/Vista/2008，Total Commander 7.0或更新版本
安装方法：把软件解压到任意目录即可 (若结合TC使用请参照用法说明1.2）

★ 常见问题
───────────────────────────────────────

1. 编辑程序可以关联空扩展名吗，比如Makefile？
答：可以，扩展名为空即可，例如"txt,,"即说明关联到txt和无扩展名的文件。

2. 在F4Menu设置窗口的编辑程序列表中删除第一行后，其他行并没有上移，这是bug吗？
答：不是，列表中第一行为默认编辑程序，被删除后F4Menu认为没有设置默认编辑程序，
因此该行留空，而不会把后续编辑程序自动提升为默认。用户可以在列表里用鼠标拖动各
编辑程序位置。

3. F4Menu可以执行vbs或js脚本吗？
答：可以，将编辑程序设置为wscript.exe，脚本路径设为其参数即可。

4. 在压缩文件里选中多个文件，F4Menu只能打开一个？
答：实际上TC并不会直接打开压缩包里的文件，而是先将它解压缩到临时目录再打开，这
种情况下TC只会解压缩光标所在的文件，而不是所有选中的文件，所以F4Menu也只能打开
该文件。

5. 进入压缩文件目录后按ESC+F4有时候无法弹出菜单，而显示“用户中止”的对话框，
怎么办？
答：如4所述，查看压缩包里的文件时，TC会先解压，这时候TC可能会认为按下ESC就是取
消操作，所以显示“用户中止”对话框，这时候可以改按CapsLock+F4.

6. 可以不需要默认编辑程序吗？
答：可以，在菜单里将默认编辑程序拖下来即可，也可在设置窗口里拖放。

7. 如何在其他任何程序里调用F4Menu？
答：在TC里按F4其实是打开指定的编辑程序F4Menu，并把光标所在文件名作为参数。
在其他程序里也一样，只需要在调用F4Menu时将文件名或文件列表作为其参数即可，只不过
不一定是按F4调用罢了。
如在Directory Opus中，可以为F4Menu建一个工具栏按钮并将{allfilepath}作为其参数，
然后指定任意快捷键。
在资源管理器中，无法直接调用外部程序，但可将文件关联到F4Menu，这样回车打开该类型时
文件就会调用F4Menu弹出选择菜单。
当然，F4Menu还可以用在其他非文件管理的程序中，例如作为IE，Firefox的源代码编辑器，
这样在IE或Firfox中查看网页代码就可以打开F4Menu选择了。

8. 如何设置自定义快捷键？
答：首先我们需要在其他应用程序里设置调用F4Menu的快捷键，请参照FAQ7。
以TC为例，默认TC可用F4和Win+F4打开指定的编辑程序F4Menu，如果我们需要在F4Menu里使用
其他快捷键，如F1键，则需要把F1关联为TC启动编辑程序F4Menu的快捷键:
在TC配置窗口->其他->快捷键->选定F1->命令选为cm_Edit
然后在F4Menu的配置里，可以将F1设为默认打开模式的快捷键，F4作为前台模式快捷键，其他
任何按键如Win+F4则作为后台模式快捷键。

9. 如何继续使用快捷键ESC+F4/F4？
答：有些用户已经习惯了旧版ESC+F4/F4的快捷键，其实0.50版照样可以支持:
只需要在设置里把前台或后台模式的快捷键设置成 ESC 就行了，这样按ESC+F4或只按F4就
是不同的启动模式了。

★ 版本历史
───────────────────────────────────────
[+]新增		[!]修正		[*]改动
[2010-11-30	0.59]
+ F4Menu.ini [Option] UseWinAssoc=0 后台模式下用若文件无关联程序则用系统关联程序打开(thanks to MVV@tc)
+ 提升快捷键处理的可靠性 (thanks to Daocang@newsmth)
* 语言文件更新 Russian (thanks to MVV@tc)

[2010-11-28	0.58]
+ F4Menu.ini [Option] ShowMultiMatch=0 后台模式下有多个关联程序时是否显示菜单：1-显示，0-不显示 (thanks to MVV@tc)
+ 命令行参数 -b/-d/-f，强制后台/默认/前台模式，忽略快捷键设置
! 修正Esc+F4等快捷键可能处理出错的问题 (thanks to happyTC@tc)
! 修正偶尔程序崩溃的问题 (thanks to happyTC@tc)
! 网址修改为www.shannycn.com
! 调整 "更多程序" 菜单项的位置
* 语言文件更新 Romanian (thanks to eugensyl@tc)
- 删除主菜单中的关于项

[2010-10-10	0.55]
+ F4Menu.ini [Option] ShellMenu=1 是否在资源管理器显示右键菜单项
! 无法处理带空格且未加双引号的文件名参数 (thanks to Balderstrom@tc)

[2010-10-08	0.51]
+ F4Menu.ini [Option] OpenSel=0 打开所选文件而不考虑光标位置 (thanks to byblo@tc)
! 添加或删除编辑程序出错 (thanks to MVV@tc)
* 语言文件更新 Danish (thanks to petermad@tc)

[2010-10-01	0.50 final]
+ 命令行参数-p，参数模式，仅从命令行参数获取文件名列表，忽略TC (thanks to MVV@tc)
+ 光标所在文件未选中时仅打开该文件，否则打开所有被选文件 (thanks to MVV@tc)
+ 增强编辑器列表窗口的拖动效果 (thanks to MVV@tc)
! 默认程序不可用时自动高亮可能无效
! 拖动菜单项可能导致crash并破坏配置文件 (thanks to MVV@tc)
! 文件列表窗口中拖动后光标所在位置错误 (thanks to MVV@tc)
! 菜单模式下若程序未设置启动路径，则将当前路径作为启动路径 (thanks to MVV@tc)
! 文件名参数不带路径时应使用当前工作路径 (thanks to MVV@tc)
! 光标所在文件的标记状态在退出后应还原 (thanks to Balderstrom@tc)
+ 临时命令行参数，-a/-r (添加/删除资源管理器右键菜单项)

[2010-09-18	0.50 beta6]
 ! 大幅提高启动速度 (thanks to happyTC@newsmth)
 ! 某些情况下提示信息显示位置不当
 ! 其他若干小问题
 * 菜单模式下带参数启动编辑器 (thanks to MVV@tc)
 * 语言文件更新 Dutch (thanks to RolandD)
 * 语言文件更新 Swedish (thanks to Leif Larsson@tc)
 + FAQ: 如何继续使用快捷键ESC+F4/F4？

[2010-09-07	0.50 beta4]
 ! 修正无默认编辑程序时崩溃的问题 (thanks to MVV@tc)
 ! 调整自动高亮编辑程序的优先逻辑
 * 语言文件更新 Korea (thanks to dis1hades2)
 * 语言文件更新 Polish (thanks to dong@tc)

[2010-09-06	0.50 beta3]
 ! 以列表方式打开多个文件出错 (thanks to byblo@tc)
 * 语言文件更新 French (thanks to byblo@tc)
 * 语言文件更新 German (thanks to icfu@tc)
 * 语言文件更新 Danish (thanks to petermad@tc)
 * 语言文件更新 Russian (thanks to MVV@tc)

[2010-09-05	0.50 beta1]
 + 支持自定义快捷键! F4Menu 可能需要改名了 (thanks to haytcb@newsmth)
 + 允许用户指定编辑器图标 (thanks to icfu@tc)
 + 增加默认打开模式，可设置快捷键直接用默认编辑程序打开
 + 支持设置编辑程序总在主菜单显示 (thanks to leepg, haytcb@newsmth)
 + 弹出菜单时自动高亮最适合的编辑程序 (thanks to leepg)
 + 菜单首项提示中显示当前文件的系统关联程序路径及参数信息
 + 菜单首项显示当前文件的文件类型图标
 + 右键菜单增加 "关联" 及 "总显示在主菜单" 功能
 ! 若文件同时被默认编辑程序和其他程序关联，优先启动默认编辑程序 (thanks to petermad@tc)
 ! 启动时无配置文件可能崩溃
 ! 修正其他小问题
 - 取消功能: F4 双击用默认编辑程序打开所选文件
 + FAQ: 如何设置自定义快捷键

[2010-08-27	v0.25 final]
 + 语言文件 Swedish (thanks to Leif Larsson@tc)
 * 语言文件更新 German (thanks to icfu@tc)

[2010-08-22	v0.25 beta2]
 ! 修正若干小问题 (thanks to MVV@tc)
 * 语言文件更新 Russian (thanks to MVV@tc)
 * 语言文件更新 Danish (thanks to petermad@tc)
 * 语言文件更新 Korean (thanks to dis1hades2)

[2010-08-21	v0.25 beta1]
 + 增加子菜单，非关联程序放入子菜单 (thanks to icfu@tc, zeroxia@DRL)
 + 命令行参数-m，菜单模式, 若无文件名参数则弹出菜单，用于创建空文件 (thanks to MVV@tc)
 + 编辑程序设置窗口支持 Ctrl+Up/Down 移动列表项 (thanks to Gustavo Sánchez)
 + FAQ: 如何在其他任何程序里调用F4Menu
 ! 菜单提示偶尔会重叠显示

[2010-08-08	v0.20 final]
 + 若程序未设置启动路径，则将当前文件路径作为启动路径
 + 命令行参数-q，静默模式, 若无文件名参数则不显示对话框提示直接退出
 ! Shift+F4新建文件可能打开其他文件 (thanks to Ralph)

[2009-05-26	v0.20 rc4]
 ! 编辑器打开方式设为 "1 - 所有文件以列表方式打开" 时无法正常工作 (thanks to byblo@tc)
 ! Shift+F4 在 TC 7.0x 不能正常工作

[2009-05-25	v0.20 rc1]
 + 不再仅限于支持 TC，允许被任何程序调用，例如资源管理器等
 + 支持添加文件名列表作为启动参数，文件名之间用空格分隔
 + F4 双击用默认编辑程序打开所选文件 (thanks to xuanqing@newsmth)
 + 按 Caps+F4 启动时，不改变系统大小写锁状态
 ! 彻底解决待打开文件无法找到的问题
 ! 解决与 TC 7.50 版本的 Shift+F4 兼容问题 (thanks to ehab@tc, matt123@newsmth)
 ! 解决双击 F4 文件打开多次的问题

[2008-01-06	v0.15 final]
 + 语言文件 Korean (thanks to dis1hades2)
 ! 无法处理UNC路径 (thanks to SanskritFritz@tc)
 ! 调整选项对话框宽度

[2007-10-02	v0.15rc1]
 + 语言文件 Polish (thanks to dong@tc)
 + 支持拖放可执行文件或快捷方式到设置窗口. (thanks to lancaster@DRL, icfu@tc)
 + 菜单提示中可显示编辑程序路径及参数信息. (thanks to lancaster@DRL)
 + 菜单显示位置：当前文件右侧/对面列表窗口/窗口正中 (thanks to sergeich28@tc, icfu@tc)
 + 编辑程序支持相对路径 (thanks to redgiant@DRL)
 + 编辑程序支持启动路径 (thanks to eaglesky@DRL, byblo@tc)
 + 选定文件数超过最大设定时，可选择是否仍打开所有文件
 + 增加manifest文件 (thanks to SanskritFritz@tc)
 ! 部分解决非自然的unicode文件名问题
 ! 无法打开压缩包内或FTP上的文件
 ! 打开文件数最多只能设为16
 ! 无法打开网上邻居的共享文件 (thanks to leepg)
 ! 编辑程序列表项选中后可能会被误拖动 (thanks to icfu@tc)

[2007-09-23	v0.141]
 + Danish 说明文件 (thanks to petermad)
 + 设定语言下未定义的字串使用 English 字串代替
 ! 在压缩包内试图打开多个文件时程序崩溃 (Raymond@tc, petermad@tc)
 * 删除语言文件内 "确定", "取消" 的助记键 (thanks to icfu@tc)

[2007-09-22	v0.14]
 + 语言文件 German (thanks to icfu@tc)
 + 语言文件 Russian (thanks to bayarookie@tc)
 + 语言文件 French (thanks to byblo@tc)
 + 语言文件 Romanian (thanks to eugensyl@tc)
 + 语言文件 Hungarian-magyar (thanks to SanskritFritz@tc)
 + 语言文件 Spanish (thanks to dott@tc)
 + 将菜单项首字母视为隐含助记键，其优先级低于 '&' 前缀的助记键 (thanks to 
   zhuangv@newsmth)
 + 可清除已打开文件的选择标记 (thanks to TucknDar@tc)
 + 增加窗口选项 常规窗口/最大化/最小化 (Fantast@DRL, byblo@tc)
 + 编辑程序列表增加新快捷键: Del 删除编辑程序, Shift+Del 不经确认直接删除.
   (thanks to icfu@tc)
 + 在文件属性里增加版本信息 (thanks to roentgen@tc)
 ! 菜单提示的文本颜色与系统设置不符 (thanks to icfu@tc)
 ! 在被Universal Shield隐藏的目录内里，F4Menu无法启动 (thanks to Fantast@DRL)
 ! 在特定语言下，选项对话框的语言名称后跟有怪字符 (thanks to petermad@tc)
 ! 丹麦语下, 选项对话框的初始语言与设定不符 (thanks to petermad@tc)
 ! 法语下, 菜单显示位置的设置 (当前文件/鼠标指针) 与实际相反 (thanks to
   byblo@tc)
 ! 将 Winrar 设为编辑程序时崩溃 (thanks to roentgen@tc)
 * 修改 English 语言文件及说明文件 (thanks to frank@newsmth)
 * 删除内部语言文件主选菜单上的助记键(&A &O &S)

[2007-09-16	v0.13]
注意!
该版本更新了ini文件的格式，旧版ini不再适用
使用前请先执行INI_to_v013.exe转换ini文件。

 + 单个文件F4后置为标记状态, F4Menu退出后还原 (thanks to zhuangv@newsmth, xuanqing@newsmth)
 + 新添编辑程序以文件描述作为默认名称 (thanks to nevermind@DRL)
 + 改进Ini文件格式, 方便用户从ChoiceEditor迁移 (thanks to icfu@tc)
 + 语言文件 Danish.lng (thanks to petermad)
 + 语言文件 Dutch.lng (thanks to RolandD)
 ! 搜索结果列表中不能使用F4Menu
 * 压缩包内ini文件改名为F4Menu.ini.template, 初始统一使用%programfiles%路径
   (thanks to icfu@tc)
 * 对话框字体选用 "MS Shell dlg 2" (thanks to icfu@tc)
 * 删除预置助记键(&A,&S,&O)

[2007-09-15	v0.12]
 + 单个文件F4后置为标记状态 (thanks to zhuangv@newsmth)
 + 文件打开模式：各文件分别打开/以列表方式打开 (thanks to nevermind@DRL)
 + 菜单显示位置：当前文件/鼠标指针 (thanks to render@newsmth, zhuangv@newsmth)
 ! Vista/2008下无法正常运行

[2007-09-13	v0.11]
 ! TC文件大小动态显示时F4Menu无法显示菜单 (thanks to BillGates@newsmth)

[2007-09-12	v0.1]
 初始版本


★ 使用声明
───────────────────────────────────────
    1.本软件是免费软件，版权归本人所有。
    2.使用者若要以任何形式从事商业营利用途（包括收录于商业网站或光盘），必须事
先取得本人同意；在非商业意图下，则允许随意散布及使用。
    3.欢迎散布作品，但请保持作品的完整性，不可将其修改，或重新打包，或以其它名
义散布。
    4.您必须自行承担使用该软件所有可能引起的后果及损失，本人不提供对软件的技术
支持及服务，也不对因使用本软件而造成任何形式的损失负责。
    5.在使用过程中如果发现错误或问题，欢迎报告。

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Shao Shanny (PRC)
http://www.shannycn.com/
Shannycn@gmail.com