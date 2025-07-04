; CLW file contains information for the MFC ClassWizard

[General Info]
Version=1
LastClass=ListOCX
LastTemplate=CDialog
NewFileInclude1=#include "stdafx.h"
NewFileInclude2=#include "fileinfo1.h"
LastPage=0

ClassCount=16
Class1=CHyperLink
Class2=CListGrid
Class3=CListpagePty
Class4=CListplugApp
Class5=CListWnd
Class6=CPageTree
Class7=CPropertySheetRz
Class8=CResizePage
Class9=CWait
Class10=CAbout
Class11=CFileinfoListWnd
Class12=CListExport
Class13=CListSheet
Class14=COption

ResourceCount=10
Resource1=IDD_Wait
Resource2=IDD_Ocx (English (U.S.))
Resource3=IDD_PageDll (English (U.S.))
Resource4=IDD_OCX (English (U.S.))
Resource5=IDD_About (English (U.S.))
Class15=ListDlg
Resource6=IDD_Export (English (U.S.))
Resource7=IDD_NotFound (English (U.S.))
Resource8=IDD_Wait (English (U.S.))
Resource9=IDD_Option (English (U.S.))
Class16=ListOCX
Resource10=IDD_fi (English (U.S.))

[CLS:CHyperLink]
Type=0
BaseClass=CStatic
HeaderFile=\Program Files\Microsoft Visual Studio\MyProjects\wincmd\commun\hyperlink.h
ImplementationFile=\Program Files\Microsoft Visual Studio\MyProjects\wincmd\commun\HyperLink.cpp

[CLS:CListGrid]
Type=0
BaseClass=CResizePage
HeaderFile=\Program Files\Microsoft Visual Studio\MyProjects\wincmd\commun\ListGrid.h
ImplementationFile=\Program Files\Microsoft Visual Studio\MyProjects\wincmd\commun\ListGrid.cpp

[CLS:CListpagePty]
Type=0
BaseClass=CResizePage
HeaderFile=\Program Files\Microsoft Visual Studio\MyProjects\wincmd\commun\ListpagePty.h
ImplementationFile=\Program Files\Microsoft Visual Studio\MyProjects\wincmd\commun\ListpagePty.cpp

[CLS:CListplugApp]
Type=0
BaseClass=CWinApp
HeaderFile=\Program Files\Microsoft Visual Studio\MyProjects\wincmd\commun\listplug.h
ImplementationFile=fileinfo.cpp

[CLS:CListWnd]
Type=0
BaseClass=CWnd
HeaderFile=\Program Files\Microsoft Visual Studio\MyProjects\wincmd\commun\ListWnd.h
ImplementationFile=\Program Files\Microsoft Visual Studio\MyProjects\wincmd\commun\ListWnd.cpp

[CLS:CPageTree]
Type=0
BaseClass=CResizePage
HeaderFile=\Program Files\Microsoft Visual Studio\MyProjects\wincmd\commun\PageTree.h
ImplementationFile=\Program Files\Microsoft Visual Studio\MyProjects\wincmd\commun\PageTree.cpp

[CLS:CPropertySheetRz]
Type=0
BaseClass=CPropertySheet
HeaderFile=\Program Files\Microsoft Visual Studio\MyProjects\wincmd\commun\PropertySheetRz.h
ImplementationFile=\Program Files\Microsoft Visual Studio\MyProjects\wincmd\commun\PropertySheetRz.cpp

[CLS:CResizePage]
Type=0
BaseClass=CPropertyPage
HeaderFile=\Program Files\Microsoft Visual Studio\MyProjects\wincmd\commun\resizepage.h
ImplementationFile=\Program Files\Microsoft Visual Studio\MyProjects\wincmd\commun\ResizePage.cpp

[CLS:CAbout]
Type=0
BaseClass=CResizePage
HeaderFile=About.h
ImplementationFile=About.cpp
LastObject=CAbout

[CLS:CFileinfoListWnd]
Type=0
BaseClass=CListWnd
HeaderFile=FileinfoListWnd.h
ImplementationFile=FileinfoListWnd.cpp

[CLS:CListExport]
Type=0
BaseClass=CPropertyPage
HeaderFile=ListExport.h
ImplementationFile=ListExport.cpp
Filter=D
VirtualFilter=idWC
LastObject=CListExport

[CLS:CListSheet]
Type=0
BaseClass=CPropertySheetRz
HeaderFile=ListSheet.h
ImplementationFile=ListSheet.cpp

[CLS:COption]
Type=0
BaseClass=CResizePage
HeaderFile=Option.h
ImplementationFile=Option.cpp

[DLG:IDD_GRID]
Type=1
Class=CListGrid

[DLG:IDD_fi]
Type=1
Class=CListpagePty

[DLG:IDD_PageDll]
Type=1
Class=CPageTree

[DLG:IDD_Waiting]
Type=1
Class=Cwait

[DLG:IDD_About]
Type=1
Class=CAbout

[DLG:IDD_Export]
Type=1
Class=CListExport

[DLG:IDD_fi (English (U.S.))]
Type=1
Class=?
ControlCount=1
Control1=IDC_RicheditFI,RICHEDIT,1353779588

[DLG:IDD_About (English (U.S.))]
Type=1
Class=?
ControlCount=5
Control1=IDC_RICHEDIT,RICHEDIT,1352730948
Control2=IDC_STATIC,static,1342308352
Control3=IDC_STATIC,static,1342181902
Control4=IDC_followme,static,1342177294
Control5=IDC_mailme,static,1342177294

[DLG:IDD_PageDll (English (U.S.))]
Type=1
Class=?
ControlCount=1
Control1=IDC_TREE1,SysTreeView32,1350632471

[DLG:IDD_Option (English (U.S.))]
Type=1
Class=?
ControlCount=24
Control1=IDC_autosave,button,1342242819
Control2=IDC_STATIC,button,1342177287
Control3=IDC_Remember,button,1342251011
Control4=IDC_STATIC,static,1342177294
Control5=IDC_STATIC,static,1342177294
Control6=IDC_STATIC,static,1342177294
Control7=IDC_STATIC,static,1342177294
Control8=IDC_STATIC,static,1342308352
Control9=IDC_STATIC,static,1342308352
Control10=IDC_STATIC,static,1342308352
Control11=IDC_STATIC,static,1342308352
Control12=IDC_undec,button,1342242819
Control13=IDC_STATIC,static,1342308352
Control14=IDC_debug,button,1342242819
Control15=IDC_res,button,1342242819
Control16=IDC_STATIC,static,1342177294
Control17=IDC_STATIC,static,1342177294
Control18=IDC_STATIC,static,1342308352
Control19=IDC_STATIC,static,1342308352
Control20=IDC_STATIC,static,1342308352
Control21=IDC_STATIC,static,1342308352
Control22=IDC_MaxD,combobox,1344340226
Control23=IDC_pdata,button,1342242819
Control24=IDC_reloc,button,1342242819

[DLG:IDD_Export (English (U.S.))]
Type=1
Class=CListExport
ControlCount=10
Control1=IDC_LIST1,listbox,1352728833
Control2=IDC_LIST2,listbox,1345388801
Control3=IDC_undecorate,button,1342242819
Control4=IDC_BUTTON1,button,1342242816
Control5=IDC_STATIC,static,1342308352
Control6=IDC_STATIC,static,1342308352
Control7=IDC_CHECK1,button,1342242819
Control8=IDC_LIST3,SysListView32,1350631425
Control9=IDC_STATIC,static,1342308352
Control10=IDC_DLL,static,1342308352

[DLG:IDD_NotFound (English (U.S.))]
Type=1
Class=ListDlg
ControlCount=2
Control1=IDOK,button,1342242817
Control2=IDC_LIST1,listbox,1352728833

[CLS:ListDlg]
Type=0
HeaderFile=ListDlg.h
ImplementationFile=ListDlg.cpp
BaseClass=CDialog
Filter=D
VirtualFilter=dWC
LastObject=IDC_LIST1

[CLS:CWait]
Type=0
HeaderFile=\Program Files\Microsoft Visual Studio\Projects\wincmd\commun\wait.h
ImplementationFile=\Program Files\Microsoft Visual Studio\Projects\wincmd\commun\wait.cpp
BaseClass=CDialog
LastObject=CWait

[DLG:IDD_Ocx (English (U.S.))]
Type=1
Class=ListOCX
ControlCount=2
Control1=IDC_BUTTON1,button,1342242816
Control2=IDC_BUTTON2,button,1342242816

[DLG:IDD_Wait (English (U.S.))]
Type=1
Class=CWait
ControlCount=2
Control1=IDC_STATIC,static,1342308352
Control2=IDC_status,static,1342308352

[DLG:IDD_Wait]
Type=1
Class=CWait
ControlCount=2
Control1=IDC_STATIC,static,1342308352
Control2=IDC_status,static,1342308352

[CLS:ListOCX]
Type=0
HeaderFile=ListOCX.h
ImplementationFile=ListOCX.cpp
BaseClass=CDialog
Filter=D
LastObject=IDC_RegServer
VirtualFilter=dWC

[DLG:IDD_OCX (English (U.S.))]
Type=1
Class=ListOCX
ControlCount=4
Control1=IDC_REocx,RICHEDIT,1353779588
Control2=IDC_RegServer,button,1476460544
Control3=IDC_UnRegServer,button,1476460544
Control4=IDC_Listocx,SysListView32,1350633489

