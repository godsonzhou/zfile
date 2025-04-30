using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using WinShell;

namespace zfile
{
    /// <summary>
    /// 控制面板文件源，用于访问Windows控制面板项目
    /// </summary>
    public class ControlPanelFileSource : FileSource
    {
        private readonly IShellFolder _controlPanelFolder;

        public ControlPanelFileSource()
        {
            // 初始化COM
            w32.InitializeCOM();

            // 获取控制面板文件夹
            _controlPanelFolder = w32.GetControlPanelFolder(out _);

            if (_controlPanelFolder == null)
            {
                throw new Exception("无法获取控制面板文件夹");
            }
        }

        public override FileSourceOperation CreateListOperation(string path)
        {
            return new ControlPanelListOperation(this, path);
        }

        public override FileSourceOperation CreateExecuteOperation(FileEntry file, string path, string parameters)
        {
            return new ControlPanelExecuteOperation(this, file, path, parameters);
        }

        public override FileSourceProperties Properties => FileSourceProperties.Virtual | FileSourceProperties.DirectAccess;

        public IShellFolder GetControlPanelFolder()
        {
            return _controlPanelFolder;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_controlPanelFolder != null)
                {
                    Marshal.ReleaseComObject(_controlPanelFolder);
                }

                // 释放COM
                w32.UninitializeCOM();
            }

            base.Dispose(disposing);
        }

        public override Uri Uri => new("controlpanel://");

        public override string FileSystem => "ControlPanel";

        public override string CurrentAddress => "controlpanel://";

        public override string CurrentWorkingDirectory => "controlpanel://";

        public override FileSourceOperationType OperationsTypes => FileSourceOperationType.List | FileSourceOperationType.Execute;
    }

    /// <summary>
    /// 控制面板列表操作，用于列出控制面板项目
    /// </summary>
    public class ControlPanelListOperation : FileSourceListOperation
    {
        private readonly ControlPanelFileSource _fileSource;

        public ControlPanelListOperation(ControlPanelFileSource fileSource, string path)
            : base(fileSource, path)
        {
            _fileSource = fileSource;
        }

        protected override void MainExecute()
        {
            try
            {
                Files = new FileEntries();

                // 添加上级目录项
                Files.Add(new FileEntry
                {
                    Name = "..",
                    IsDirectory = true,
                    Size = 0,
                    Attributes = FileAttributes.Directory,
                    CreationTime = DateTime.Now,
                    ModificationTime = DateTime.Now,
                    LastAccessTime = DateTime.Now
                });

                // 获取控制面板文件夹
                IShellFolder controlPanelFolder = _fileSource.GetControlPanelFolder();

                // 枚举控制面板项目
                var flags = SHCONTF.FOLDERS;
                controlPanelFolder.EnumObjects(IntPtr.Zero, flags, out IntPtr enumPtr);

                if (enumPtr != IntPtr.Zero)
                {
                    IEnumIDList enumIdList = (IEnumIDList)Marshal.GetObjectForIUnknown(enumPtr);

                    while (enumIdList.Next(1, out IntPtr pidl, out uint fetched) == 0 && fetched == 1)
                    {
                        try
                        {
							//IntPtr pszname = Marshal.AllocHGlobal(260);
							// 获取项目名称
							//controlPanelFolder.GetDisplayNameOf(pidl, SHGDN.INFOLDER, pszname);
							string? name = w32.GetDisplayName(controlPanelFolder, pidl, SHGDN.INFOLDER); //Marshal.PtrToStringAuto(pszname);
							// 获取项目属性
							SFGAO attributes = 0;
                            controlPanelFolder.GetAttributesOf(1, new IntPtr[] { pidl }, ref attributes);

                            // 创建文件条目
                            var fileEntry = new FileEntry
                            {
                                Name = name,
                                IsDirectory = (attributes & SFGAO.FOLDER) != 0,
                                Size = 0,
                                Attributes = FileAttributes.System,
                                CreationTime = DateTime.Now,
                                ModificationTime = DateTime.Now,
                                LastAccessTime = DateTime.Now
                            };

                            // 保存PIDL作为自定义数据，用于后续执行操作
                            IntPtr pidlClone = API.ILClone(pidl);
                            fileEntry.FileType = pidlClone.ToString(); // 使用FileType属性存储PIDL指针

                            Files.Add(fileEntry);
                        }
                        finally
                        {
                            if (pidl != IntPtr.Zero)
                            {
                                API.ILFree(pidl);
                            }
                        }
                    }

                    Marshal.ReleaseComObject(enumIdList);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"列出控制面板项目时出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    /// <summary>
    /// 控制面板执行操作，用于打开控制面板项目
    /// </summary>
    public class ControlPanelExecuteOperation : FileSourceExecuteOperation
    {
        private readonly ControlPanelFileSource _fileSource;

        public ControlPanelExecuteOperation(ControlPanelFileSource fileSource, FileEntry file, string path, string parameters)
            : base(fileSource, file, path, parameters)
        {
            _fileSource = fileSource;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern bool ShellExecuteEx(ref SHELLEXECUTEINFO lpExecInfo);

        protected override void MainExecute()
        {
            try
            {
                // 获取控制面板文件夹
                IShellFolder controlPanelFolder = _fileSource.GetControlPanelFolder();

                // 获取PIDL
                IntPtr pidl = IntPtr.Zero;

                // 尝试通过名称解析PIDL
                uint attributes = 0;
                controlPanelFolder.ParseDisplayName(IntPtr.Zero, IntPtr.Zero, ExecutableFile.Name, out uint eaten, out pidl, ref attributes);

                if (pidl != IntPtr.Zero)
                {
                    try
                    {
                        // 创建Shell执行信息
                        var execInfo = new SHELLEXECUTEINFO
                        {
                            cbSize = Marshal.SizeOf(typeof(SHELLEXECUTEINFO)),
                            fMask = 0x0000000C, // SEE_MASK_INVOKEIDLIST
                            hwnd = IntPtr.Zero,
                            lpVerb = "open",
                            lpIDList = pidl,
                            nShow = (int)SW.SHOWNORMAL
                        };

                        // 执行Shell命令
                        if (!ShellExecuteEx(ref execInfo))
                        {
                            int error = Marshal.GetLastWin32Error();
                            throw new Exception($"ShellExecuteEx失败，错误代码: {error}");
                        }

                        ExecuteOperationResult = FileSourceExecuteOperationResult.Success;
                    }
                    finally
                    {
                        API.ILFree(pidl);
                    }
                }
                else
                {
                    throw new Exception("无法获取控制面板项目的PIDL");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"执行控制面板项目时出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ExecuteOperationResult = FileSourceExecuteOperationResult.Error;
            }
        }

        public override string GetDescription(FileSourceOperationDescriptionDetails details)
        {
            return $"执行控制面板项目: {ExecutableFile.Name}";
        }

        protected override void UpdateStatisticsAtStartTime()
        {
            // 空实现
        }
    }
}
