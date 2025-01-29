//程序开发：lc_mtt
//CSDN博客：http://lemony.cnblogs.com
//个人主页：http://www.3lsoft.com
//注：此代码禁止用于商业用途。有修改者发我一份，谢谢！
//---------------- 开源世界，你我更进步 ----------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;

using System.Runtime.InteropServices;
using WinShell;

namespace Sample3
{
    public partial class Sample3 : Form
    {
        private IShellFolder iDeskTop;

        public Sample3()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //获得桌面 PIDL
            IntPtr deskTopPtr;
            iDeskTop = w32.GetDesktopFolder(out deskTopPtr);

            //添加 桌面 节点
            TreeNode tnDesktop = new TreeNode("桌面");
            tnDesktop.Tag = new ShellItem(deskTopPtr, iDeskTop);
            tnDesktop.Nodes.Add("...");

            //把节点添加到树中
            Tree1.Nodes.Add(tnDesktop);
            tnDesktop.Expand();
        }

        private void Tree1_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            #region 判断节点是否已经展开
            if (e.Node.Nodes.Count != 1)
            {
                return;
            }
            else
            {
                if (e.Node.FirstNode.Text != "...")
                {
                    return;
                }
            }

            e.Node.Nodes.Clear(); 
            #endregion

            ShellItem sItem = (ShellItem)e.Node.Tag;
            IShellFolder root = sItem.ShellFolder;

            //循环查找子项
            IEnumIDList Enum = null;
            IntPtr EnumPtr = IntPtr.Zero;
            IntPtr pidlSub;
            uint celtFetched;

            if (root.EnumObjects(this.Handle, SHCONTF.FOLDERS, out EnumPtr) == w32.S_OK)
            {
                Enum = (IEnumIDList)Marshal.GetObjectForIUnknown(EnumPtr);
                while (Enum.Next(1, out pidlSub, out celtFetched) == 0 && celtFetched == w32.S_FALSE)
                {
                    string name = w32.GetNameByIShell(root, pidlSub);
                    IShellFolder iSub;
                    root.BindToObject(pidlSub, IntPtr.Zero, ref Guids.IID_IShellFolder, out iSub);
                    
                    TreeNode nodeSub = new TreeNode(name);
                    nodeSub.Tag = new ShellItem(pidlSub, iSub);
                    nodeSub.Nodes.Add("...");
                    e.Node.Nodes.Add(nodeSub);
                }
            }
        }

        private void Sample3_FormClosing(object sender, FormClosingEventArgs e)
        {
            //释放资源
            Marshal.ReleaseComObject(iDeskTop);
        }

        private void Tree1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                Tree1.SelectedNode = Tree1.GetNodeAt(e.X, e.Y);
            }
        }

        private void Tree1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (Tree1.SelectedNode != null)
                {
                    //获得当前节点的 PIDL
                    ShellItem sItem = (ShellItem)Tree1.SelectedNode.Tag;
                    IntPtr PIDL = sItem.PIDL;

                    //获得父节点的 IShellFolder 接口
                    IShellFolder IParent = iDeskTop;
                    if (Tree1.SelectedNode.Parent != null)
                    {
                        IParent = ((ShellItem)Tree1.SelectedNode.Parent.Tag).ShellFolder;
                    }
                    else
                    {
                        //桌面的真实路径的 PIDL
                        string path = w32.GetSpecialFolderPath(this.Handle, ShellSpecialFolders.DESKTOPDIRECTORY);
                        w32.GetShellFolder(iDeskTop, path, out PIDL);
                    }

                    //存放 PIDL 的数组
                    IntPtr[] pidls = new IntPtr[1];
                    pidls[0] = PIDL;

                    //得到 IContextMenu 接口
                    IntPtr iContextMenuPtr = IntPtr.Zero;
                    iContextMenuPtr = IParent.GetUIObjectOf(IntPtr.Zero, (uint)pidls.Length, 
                        pidls, ref Guids.IID_IContextMenu, out iContextMenuPtr);
                    IContextMenu iContextMenu = (IContextMenu)Marshal.GetObjectForIUnknown(iContextMenuPtr);

                    //提供一个弹出式菜单的句柄
                    IntPtr contextMenu = API.CreatePopupMenu();
                    iContextMenu.QueryContextMenu(contextMenu, 0,
                        w32.CMD_FIRST, w32.CMD_LAST, CMF.NORMAL | CMF.EXPLORE);

                    //弹出菜单
                    uint cmd = API.TrackPopupMenuEx(contextMenu,TPM.RETURNCMD,
                        MousePosition.X, MousePosition.Y, this.Handle, IntPtr.Zero);

                    //获取命令序号，执行菜单命令
                    if (cmd >= w32.CMD_FIRST)
                    {
                        CMINVOKECOMMANDINFOEX invoke = new CMINVOKECOMMANDINFOEX();
                        invoke.cbSize = Marshal.SizeOf(typeof(CMINVOKECOMMANDINFOEX));
                        invoke.lpVerb = (IntPtr)(cmd - 1);
                        invoke.lpDirectory = string.Empty;
                        invoke.fMask = 0;
                        invoke.ptInvoke = new POINT(MousePosition.X, MousePosition.Y);
                        invoke.nShow = 1;
                        iContextMenu.InvokeCommand(ref invoke);
                    }
                }
            }
        }

    }
}