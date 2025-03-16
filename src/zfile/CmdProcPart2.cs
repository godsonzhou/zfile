using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using System.Windows.Forms;

namespace zfile
{
    public partial class CmdProc
    {

		private void do_cm_decode(string param)
		{
			if (param.Equals(string.Empty))
			{
				var listView = owner.activeListView;
				if (listView == null || listView.SelectedItems.Count == 0)
				{
					MessageBox.Show("请先选择要解码的文件", "提示");
					return;
				}

				using var dialog = new EncodeDialog();
				//if (dialog.ShowDialog() != DialogResult.OK) return;

				string targetPath = string.IsNullOrEmpty(dialog.TargetPath) ?
					owner.currentDirectory[owner.isleft] : dialog.TargetPath;

				foreach (ListViewItem item in listView.SelectedItems)
				{
					var sourcePath = Path.Combine(owner.currentDirectory[owner.isleft], item.Text);
					string decodedContent;
					try
					{
						string encodedContent = File.ReadAllText(sourcePath);
						decodedContent = Path.GetExtension(sourcePath) switch
						{
							".B64" => DecodeBase64(encodedContent),
							".UUE" => DecodeUUEncode(encodedContent),
							".XXE" => DecodeXXEncode(encodedContent),
							_ => throw new InvalidOperationException("仅支持编码格式B64/UUE/XXE")
						};

						var targetFile = Path.Combine(targetPath, Path.GetFileNameWithoutExtension(item.Text));
						File.WriteAllText(targetFile, decodedContent);
					}
					catch (Exception ex)
					{
						MessageBox.Show($"解码文件 {item.Text} 时出错: {ex.Message}", "错误");
					}
				}
			}
			else
			{
				var files = owner.se.PrepareParameter(param, null, "");
				var targetPath = owner.currentDirectory[owner.isleft];
				foreach (var file in files)
				{
					var sourcePath = Path.Combine(owner.currentDirectory[owner.isleft], file);
					string decodedContent;
					try
					{
						string encodedContent = File.ReadAllText(sourcePath);
						decodedContent = Path.GetExtension(sourcePath) switch
						{
							"B64" => DecodeBase64(encodedContent),
							"UUE" => DecodeUUEncode(encodedContent),
							"XXE" => DecodeXXEncode(encodedContent),
							_ => throw new InvalidOperationException("仅支持编码格式B64/UUE/XXE")
						};

						var targetFile = Path.Combine(targetPath, Path.GetFileNameWithoutExtension(file));
						File.WriteAllText(targetFile, decodedContent);
					}
					catch (Exception ex)
					{
						MessageBox.Show($"解码文件 {file} 时出错: {ex.Message}", "错误");
					}
				}

			}
			owner.RefreshPanel();
		}

		private string DecodeBase64(string encodedContent)
		{
			byte[] bytes = Convert.FromBase64String(encodedContent);
			return Encoding.UTF8.GetString(bytes);
		}

		private string DecodeUUEncode(string encodedContent)
		{
			// 实现 UUEncode 解码逻辑
			var decodedBytes = new List<byte>();
			var lines = encodedContent.Split('\n');

			foreach (var line in lines)
			{
				if (string.IsNullOrWhiteSpace(line) || line == "end" || line == "`")
					continue;

				int length = line[0] - 32;
				if (length <= 0)
					continue;

				for (int i = 1; i < line.Length; i += 4)
				{
					byte a = (byte)(line[i] - 32);
					byte b = (byte)(line[i + 1] - 32);
					byte c = (byte)(line[i + 2] - 32);
					byte d = (byte)(line[i + 3] - 32);

					byte e = (byte)((a << 2) | (b >> 4));
					byte f = (byte)((b << 4) | (c >> 2));
					byte g = (byte)((c << 6) | d);

					decodedBytes.Add(e);
					if (decodedBytes.Count < length) decodedBytes.Add(f);
					if (decodedBytes.Count < length) decodedBytes.Add(g);
				}
			}

			return Encoding.UTF8.GetString(decodedBytes.ToArray());
		}

		private string DecodeXXEncode(string encodedContent)
		{
			// 实现 XXEncode 解码逻辑
			var decodedBytes = new List<byte>();
			var lines = encodedContent.Split('\n');

			foreach (var line in lines)
			{
				if (string.IsNullOrWhiteSpace(line) || line == "end" || line == "+")
					continue;

				int length = line[0] - 32;
				if (length <= 0)
					continue;

				for (int i = 1; i < line.Length; i += 4)
				{
					byte a = (byte)(line[i] - 42);
					byte b = (byte)(line[i + 1] - 42);
					byte c = (byte)(line[i + 2] - 42);
					byte d = (byte)(line[i + 3] - 42);

					byte e = (byte)((a << 2) | (b >> 4));
					byte f = (byte)((b << 4) | (c >> 2));
					byte g = (byte)((c << 6) | d);

					decodedBytes.Add(e);
					if (decodedBytes.Count < length) decodedBytes.Add(f);
					if (decodedBytes.Count < length) decodedBytes.Add(g);
				}
			}

			return Encoding.UTF8.GetString(decodedBytes.ToArray());
		}
		private void encode_files(string sourcePath)
		{

		}
		private void do_cm_encode(string param)
		{
			if (param.Equals(string.Empty))
			{
				var listView = owner.activeListView;
				if (listView == null || listView.SelectedItems.Count == 0)
				{
					MessageBox.Show("请先选择要编码的文件", "提示");
					return;
				}
				using var dialog = new EncodeDialog();
				if (dialog.ShowDialog() != DialogResult.OK) return;

				string targetPath = string.IsNullOrEmpty(dialog.TargetPath) ?
					owner.currentDirectory[owner.isleft] : dialog.TargetPath;

				foreach (ListViewItem item in listView.SelectedItems)
				{
					var sourcePath = Path.Combine(owner.currentDirectory[owner.isleft], item.Text);
					string extension = dialog.SelectedEncoding switch
					{
						"MIME (Base64)" => ".B64",
						"UUEncode" => ".UUE",
						"XXEncode" => ".XXE",
						_ => ".B64"
					};

					var targetFile = Path.Combine(targetPath,
						Path.GetFileNameWithoutExtension(item.Text) + extension);

					try
					{
						byte[] fileContent = File.ReadAllBytes(sourcePath);
						string encodedContent = dialog.SelectedEncoding switch
						{
							"MIME (Base64)" => Convert.ToBase64String(fileContent),
							"UUEncode" => UUEncode(fileContent),
							"XXEncode" => XXEncode(fileContent),
							_ => Convert.ToBase64String(fileContent)
						};

						// 添加文件头
						var header = string.Empty;// $"begin {Path.GetFileName(sourcePath)}\n";

						if (dialog.FileSize > 0 || dialog.LineCount > 0)
						{
							// 分割编码后的内容
							var parts = SplitEncodedContent(encodedContent, dialog.FileSize, dialog.LineCount);
							for (int i = 0; i < parts.Count; i++)
							{
								var partFile = i == 0 ? targetFile :
									Path.Combine(targetPath,
										Path.GetFileNameWithoutExtension(targetFile) +
										$"_{i + 1}{extension}");
								File.WriteAllText(partFile, header + parts[i]);
							}
						}
						else
						{
							File.WriteAllText(targetFile, header + encodedContent);
						}
					}
					catch (Exception ex)
					{
						MessageBox.Show($"编码文件 {item.Text} 时出错: {ex.Message}", "错误");
					}
				}
			}
			else
			{
				using var dialog = new EncodeDialog();
				if (dialog.ShowDialog() != DialogResult.OK) return;

				string targetPath = string.IsNullOrEmpty(dialog.TargetPath) ?
					owner.currentDirectory[owner.isleft] : dialog.TargetPath;

				var files = owner.se.PrepareParameter(param, null, "");
				foreach (var file in files)
				{
					var sourcePath = Path.Combine(owner.currentDirectory[owner.isleft], file);
					string extension = dialog.SelectedEncoding switch
					{
						"MIME (Base64)" => ".B64",
						"UUEncode" => ".UUE",
						"XXEncode" => ".XXE",
						_ => ".B64"
					};

					var targetFile = Path.Combine(targetPath,
						Path.GetFileNameWithoutExtension(file) + extension);

					try
					{
						byte[] fileContent = File.ReadAllBytes(sourcePath);
						string encodedContent = dialog.SelectedEncoding switch
						{
							"MIME (Base64)" => Convert.ToBase64String(fileContent),
							"UUEncode" => UUEncode(fileContent),
							"XXEncode" => XXEncode(fileContent),
							_ => Convert.ToBase64String(fileContent)
						};

						// 添加文件头
						var header = $"begin {Path.GetFileName(sourcePath)}\n";

						if (dialog.FileSize > 0 || dialog.LineCount > 0)
						{
							// 分割编码后的内容
							var parts = SplitEncodedContent(encodedContent, dialog.FileSize, dialog.LineCount);
							for (int i = 0; i < parts.Count; i++)
							{
								var partFile = i == 0 ? targetFile :
									Path.Combine(targetPath,
										Path.GetFileNameWithoutExtension(targetFile) +
										$"_{i + 1}{extension}");
								File.WriteAllText(partFile, header + parts[i]);
							}
						}
						else
						{
							File.WriteAllText(targetFile, header + encodedContent);
						}
					}
					catch (Exception ex)
					{
						MessageBox.Show($"编码文件 {file} 时出错: {ex.Message}", "错误");
					}
				}
			}


			owner.RefreshPanel();
		}

		private string UUEncode(byte[] data)
		{
			var sb = new StringBuilder();
			const int lineLength = 45;

			for (int i = 0; i < data.Length; i += lineLength)
			{
				int blockLength = Math.Min(lineLength, data.Length - i);
				sb.Append((char)(blockLength + 32));

				for (int j = 0; j < blockLength; j += 3)
				{
					byte a = data[i + j];
					byte b = j + 1 < blockLength ? data[i + j + 1] : (byte)0;
					byte c = j + 2 < blockLength ? data[i + j + 2] : (byte)0;

					byte d = (byte)((a >> 2) & 0x3f);
					byte e = (byte)(((a << 4) | ((b >> 4) & 0xf)) & 0x3f);
					byte f = (byte)(((b << 2) | ((c >> 6) & 0x3)) & 0x3f);
					byte g = (byte)(c & 0x3f);

					sb.Append((char)(d + 32));
					sb.Append((char)(e + 32));
					if (j + 1 < blockLength) sb.Append((char)(f + 32));
					if (j + 2 < blockLength) sb.Append((char)(g + 32));
				}
				sb.AppendLine();
			}
			sb.AppendLine("`");
			return sb.ToString();
		}

		private string XXEncode(byte[] data)
		{
			var sb = new StringBuilder();
			const int lineLength = 45;

			for (int i = 0; i < data.Length; i += lineLength)
			{
				int blockLength = Math.Min(lineLength, data.Length - i);
				sb.Append((char)(blockLength + 32));

				for (int j = 0; j < blockLength; j += 3)
				{
					byte a = data[i + j];
					byte b = j + 1 < blockLength ? data[i + j + 1] : (byte)0;
					byte c = j + 2 < blockLength ? data[i + j + 2] : (byte)0;

					byte d = (byte)((a >> 2) & 0x3f);
					byte e = (byte)(((a << 4) | ((b >> 4) & 0xf)) & 0x3f);
					byte f = (byte)(((b << 2) | ((c >> 6) & 0x3)) & 0x3f);
					byte g = (byte)(c & 0x3f);

					sb.Append(XXEncodeChar(d));
					sb.Append(XXEncodeChar(e));
					if (j + 1 < blockLength) sb.Append(XXEncodeChar(f));
					if (j + 2 < blockLength) sb.Append(XXEncodeChar(g));
				}
				sb.AppendLine();
			}
			sb.AppendLine("+");
			return sb.ToString();
		}

		private char XXEncodeChar(byte b)
		{
			return (char)(b + 42);
		}

		private List<string> SplitEncodedContent(string content, int maxSize, int maxLines)
		{
			var result = new List<string>();
			var lines = content.Split('\n');

			if (maxLines > 0)
			{
				for (int i = 0; i < lines.Length; i += maxLines)
				{
					result.Add(string.Join("\n",
						lines.Skip(i).Take(maxLines)));
				}
			}
			else if (maxSize > 0)
			{
				var currentPart = new StringBuilder();
				foreach (var line in lines)
				{
					if (currentPart.Length + line.Length > maxSize)
					{
						result.Add(currentPart.ToString());
						currentPart.Clear();
					}
					currentPart.AppendLine(line);
				}
				if (currentPart.Length > 0)
				{
					result.Add(currentPart.ToString());
				}
			}
			else
			{
				result.Add(content);
			}

			return result;
		}

		private void ShowDirectoryTreeSearch()
		{
			// 获取当前驱动器
			string currentDrive = Path.GetPathRoot(owner.currentDirectory[owner.isleft]);
			if (string.IsNullOrEmpty(currentDrive))
				currentDrive = "C:\\";

			// 创建并显示目录树查找窗口
			var treeSearchForm = new DirectoryTreeSearchForm(owner, currentDrive);
			treeSearchForm.ShowDialog();
		}

		private void ShowCommandBrowser()
		{
			// 显示窗体
			var form = new CommandBrowserForm(owner.cmdProcessor);
			form.ShowDialog();
		}

		// 添加新方法实现刷新功能
		private void do_cm_rereadsource()
		{
			var listView = owner.activeListView;
			if (listView == null) return;

			try
			{
				// 刷新当前面板
				owner.RefreshPanel();
			}
			catch (Exception ex)
			{
				MessageBox.Show($"刷新目录失败: {ex.Message}", "错误",
					MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
		private void do_cm_DirBranch()
		{
			owner.fsManager.isDirBranchMode = !owner.fsManager.isDirBranchMode;
			owner.RefreshPanel();
		}
		// 全选
		private void do_cm_SelectAll()
		{
			var lv = owner.activeListView;
			if (lv == null) return;
			foreach (ListViewItem item in lv.Items)
			{
				item.Selected = true;
			}
			owner.uiManager.setArgs();
		}

		// 取消全选
		private void do_cm_ClearAll()
		{
			var lv = owner.activeListView;
			if (lv == null) return;
			foreach (ListViewItem item in lv.Items)
			{
				item.Selected = false;
			}
			owner.uiManager.setArgs();
		}

		// 反选
		private void do_cm_InvertSelection()
		{
			var lv = owner.activeListView;
			if (lv == null) return;
			foreach (ListViewItem item in lv.Items)
			{
				item.Selected = !item.Selected;
			}
			owner.uiManager.setArgs();
		}

		// 选择相同扩展名文件
		private void do_cm_SelectByExt()
		{
			var lv = owner.activeListView;
			if (lv == null || lv.SelectedItems.Count == 0) return;

			var ext = Path.GetExtension(lv.SelectedItems[0].Text);
			foreach (ListViewItem item in lv.Items)
			{
				if (Path.GetExtension(item.Text).Equals(ext, StringComparison.OrdinalIgnoreCase))
				{
					item.Selected = true;
				}
			}
			owner.uiManager.setArgs();
		}

		// 存储的选择集合
		private List<string> savedSelection = new();

		// 保存选择
		private void do_cm_SaveSelection()
		{
			var lv = owner.activeListView;
			if (lv == null) return;

			savedSelection.Clear();
			foreach (ListViewItem item in lv.SelectedItems)
			{
				savedSelection.Add(item.Text);
			}
		}

		// 恢复选择
		private void do_cm_RestoreSelection()
		{
			var lv = owner.activeListView;
			if (lv == null) return;

			foreach (ListViewItem item in lv.Items)
			{
				item.Selected = savedSelection.Contains(item.Text);
			}
			owner.uiManager.setArgs();
		}

		// 复制文件名到剪贴板
		private void do_cm_CopyNamesToClip()
		{
			var lv = owner.activeListView;
			if (lv == null || lv.SelectedItems.Count == 0) return;

			var names = string.Join(Environment.NewLine,
				lv.SelectedItems.Cast<ListViewItem>().Select(i => i.Text));
			Clipboard.SetText(names);
		}

		// 复制完整路径到剪贴板
		private void do_cm_CopyFullNamesToClip()
		{
			var lv = owner.activeListView;
			if (lv == null || lv.SelectedItems.Count == 0) return;

			var paths = string.Join(Environment.NewLine,
				lv.SelectedItems.Cast<ListViewItem>()
					.Select(i => Path.Combine(owner.currentDirectory[owner.isleft], i.Text)));
			Clipboard.SetText(paths);
		}

		// 复制文件详细信息
		private void do_cm_CopyDetailsToClip()
		{
			var lv = owner.activeListView;
			if (lv == null || lv.SelectedItems.Count == 0) return;

			var details = new StringBuilder();
			foreach (ListViewItem item in lv.SelectedItems)
			{
				details.AppendLine(string.Join("\t", item.SubItems.Cast<ListViewItem.ListViewSubItem>().Select(si => si.Text)));
			}
			Clipboard.SetText(details.ToString());
		}

		// 复制文件详细信息及完整路径
		private void do_cm_CopyFullDetailsToClip()
		{
			var lv = owner.activeListView;
			if (lv == null || lv.SelectedItems.Count == 0) return;

			var details = new StringBuilder();
			foreach (ListViewItem item in lv.SelectedItems)
			{
				details.AppendLine(Path.Combine(owner.currentDirectory[owner.isleft], item.Text) + "\t" +
					string.Join("\t", item.SubItems.Cast<ListViewItem.ListViewSubItem>().Skip(1).Select(si => si.Text)));
			}
			Clipboard.SetText(details.ToString());
		}
		// 添加导航命令的实现
		private void do_cm_gotopreviousdir()
		{
			if (owner.backStack.Count > 0)
			{
				// 将当前目录存入前进栈
				owner.forwardStack.Push(owner.currentDirectory[owner.isleft]);
				// 从后退栈获取上一个目录
				string previousPath = owner.backStack.Pop();
				// 导航到该目录，但不记录到历史（避免重复记录）
				owner.NavigateToPath(previousPath, false);
			}
		}

		private void do_cm_gotonextdir()
		{
			if (owner.forwardStack.Count > 0)
			{
				// 将当前目录存入后退栈
				owner.backStack.Push(owner.currentDirectory[owner.isleft]);
				// 从前进栈获取下一个目录
				string nextPath = owner.forwardStack.Pop();
				// 导航到该目录，但不记录到历史（避免重复记录）
				owner.NavigateToPath(nextPath, false);
			}
		}

		private void do_cm_gotoparent()
		{
			string? parentPath = Path.GetDirectoryName(owner.currentDirectory[owner.isleft]);
			if (!string.IsNullOrEmpty(parentPath))
			{
				// 记录当前目录到历史
				owner.RecordDirectoryHistory(parentPath);
				// 导航到父目录
				owner.NavigateToPath(parentPath);
			}
		}
		// 复制选中的文件
		private bool CopySelectedFiles()
		{
			var listView = owner.activeListView;
			if (listView == null || listView.SelectedItems.Count <= 0) return false;

			var srcPath = Helper.getFSpath(!owner.uiManager.isleft ? owner.uiManager.RightTree.SelectedNode.FullPath : owner.uiManager.LeftTree.SelectedNode.FullPath);
			var targetTree = owner.uiManager.isleft ? owner.uiManager.RightTree : owner.uiManager.LeftTree;
			var targetPath = Helper.getFSpath(targetTree.SelectedNode.FullPath);
			var isSamePath = targetPath.Equals(srcPath);

			var sourceFiles = listView.SelectedItems.Cast<ListViewItem>()
				.Select(item => owner.GetListItemPath(item))
				.ToArray();

			var targetlist = owner.uiManager.isleft ? owner.uiManager.RightList : owner.uiManager.LeftList;
			try
			{
				// 处理压缩文件的情况
				if (owner.IsArchiveFile(srcPath))
				{
					foreach (string fileName in sourceFiles)
					{
						owner.ExtractArchiveFile(srcPath, fileName, targetPath);
					}
					return true;
				}

				if (owner.IsArchiveFile(targetPath))
				{
					string[] files = sourceFiles.Select(f => Path.Combine(srcPath, f)).ToArray();
					owner.AddToArchive(targetPath, files);
					var items = owner.LoadArchiveContents(targetPath);
					var targetListView = (owner.uiManager.isleft ? owner.uiManager.RightList : owner.uiManager.LeftList);
					targetListView.Items.Clear();
					targetListView.Items.AddRange(items.ToArray());
					return true;
				}

				// 检查源路径和目标路径是否为FTP路径
				bool isSourceFtp = owner.fTPMGR.IsFtpPath(srcPath);
				bool isTargetFtp = owner.fTPMGR.IsFtpPath(targetPath);

				if (isSourceFtp && !isTargetFtp)
				{
					// 从FTP下载到本地
					var ftpSource = owner.fTPMGR.GetFtpSource(srcPath);
					if (ftpSource != null)
					{
						foreach (string remotePath in sourceFiles)
						{
							string fileName = Path.GetFileName(remotePath);
							string localPath = Path.Combine(targetPath, fileName);
							string tempFile = ftpSource.DownloadFile(remotePath);
							if (!string.IsNullOrEmpty(tempFile))
							{
								try
								{
									File.Copy(tempFile, localPath, true);
								}
								finally
								{
									// 清理临时文件
									if (File.Exists(tempFile))
										File.Delete(tempFile);
								}
							}
						}
					}
				}
				else if (!isSourceFtp && isTargetFtp)
				{
					// 从本地上传到FTP
					var ftpTarget = owner.fTPMGR.GetFtpSource(targetPath);
					if (ftpTarget != null)
					{
						foreach (string localFile in sourceFiles)
						{
							string fileName = Path.GetFileName(localFile);
							string remotePath = Path.Combine(targetPath, fileName).Replace("\\", "/");
							ftpTarget.UploadFile(localFile, remotePath);
						}
					}
				}
				else if (isSourceFtp && isTargetFtp)
				{
					// FTP到FTP的复制
					var sourceFtp = owner.fTPMGR.GetFtpSource(srcPath);
					var targetFtp = owner.fTPMGR.GetFtpSource(targetPath);
					if (sourceFtp != null && targetFtp != null)
					{
						foreach (string remotePath in sourceFiles)
						{
							// 先下载到临时目录
							string tempFile = sourceFtp.DownloadFile(remotePath);
							if (!string.IsNullOrEmpty(tempFile))
							{
								try
								{
									// 再上传到目标FTP
									string fileName = Path.GetFileName(remotePath);
									string targetRemotePath = Path.Combine(targetPath, fileName).Replace("\\", "/");
									targetFtp.UploadFile(tempFile, targetRemotePath);
								}
								finally
								{
									// 清理临时文件
									if (File.Exists(tempFile))
										File.Delete(tempFile);
								}
							}
						}
					}
				}
				else
				{
					// 本地文件之间的复制
					FileSystemManager.CopyFilesAndDirectories(sourceFiles, targetPath);
				}

				owner.RefreshPanel(targetlist);
				return true;
			}
			catch (Exception ex)
			{
				MessageBox.Show($"复制文件失败: {ex.Message}", "错误");
				return false;
			}
		}

		// 移动选中的文件
		public void MoveSelectedFiles()
		{
			var listView = owner.activeListView;
			if (listView == null || listView.SelectedItems.Count <= 0) return;

			var srcpath = Helper.getFSpath(owner.activeTreeview.SelectedNode.FullPath);
			var sourceFiles = listView.SelectedItems.Cast<ListViewItem>()
				.Select(item => owner.GetListItemPath(item))
				.ToArray();

			var targettree = owner.uiManager.isleft ? owner.uiManager.RightTree : owner.uiManager.LeftTree;
			var targetPath = Helper.getFSpath(targettree.SelectedNode.FullPath);
			if (string.IsNullOrEmpty(targetPath))
			{
				MessageBox.Show("无效的目标路径", "错误");
				return;
			}
			if (srcpath.Equals(targetPath))
			{
				return;     //if srcpath eq targetpath, do not need move, do rename 
			}

			try
			{
				// 检查源路径和目标路径是否为FTP路径
				bool isSourceFtp = owner.fTPMGR.IsFtpPath(srcpath);
				bool isTargetFtp = owner.fTPMGR.IsFtpPath(targetPath);

				if (isSourceFtp || isTargetFtp)
				{
					// 如果涉及FTP，先复制后删除
					if (CopySelectedFiles())
					{
						// 如果源是FTP，使用FTP删除
						if (isSourceFtp)
						{
							var ftpSource = owner.fTPMGR.GetFtpSource(srcpath);
							if (ftpSource != null)
							{
								foreach (string remotePath in sourceFiles)
								{
									ftpSource.DeleteFile(remotePath);
								}
							}
						}
						else
						{
							// 源是本地文件，使用本地删除
							DeleteSelectedFiles(false);
						}
					}
				}
				else
				{
					// 本地文件之间的移动
					if (CopySelectedFiles())
						DeleteSelectedFiles(false);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"移动文件失败: {ex.Message}", "错误");
			}
		}

		// 删除选中的文件
		private void DeleteSelectedFiles(bool needConfirm = true)
		{
			Debug.Print("Delete files : >>");
			var listView = owner.activeListView;
			if (listView == null || listView.SelectedItems.Count <= 0) return;

			var files = listView.SelectedItems.Cast<ListViewItem>()
				.Select(item => owner.GetListItemPath(item))
				.ToArray();

			var currentPath = owner.currentDirectory[owner.isleft];
			var result = DialogResult.Yes;
			if (needConfirm)
			{
				result = MessageBox.Show(
					$"确定要删除选中的 {files.Length} 个文件吗？",
					"确认删除",
					MessageBoxButtons.YesNo,
					MessageBoxIcon.Question
				);
			}
			if (result == DialogResult.Yes)
			{
				try
				{
					if (owner.IsArchiveFile(owner.currentDirectory[owner.isleft]))
					{
						if (owner.DeleteFromArchive(owner.currentDirectory[owner.isleft], files.ToArray()))
						{
							var items = owner.LoadArchiveContents(owner.currentDirectory[owner.isleft]);
							owner.activeListView.Items.Clear();
							owner.activeListView.Items.AddRange(items.ToArray());
						}
						return;
					}

					// 检查是否为FTP路径
					if (owner.fTPMGR.IsFtpPath(currentPath))
					{
						var ftpSource = owner.fTPMGR.GetFtpSource(currentPath);
						if (ftpSource != null)
						{
							foreach (string remotePath in files)
							{
								ftpSource.DeleteFile(remotePath);
							}
						}
					}
					else
					{
						// 本地文件删除
						foreach (var file in files)
						{
							FileSystemManager.DeleteFile(file);
						}
					}
					owner.RefreshPanel(listView);
				}
				catch (Exception ex)
				{
					MessageBox.Show($"删除文件失败: {ex.Message}", "错误");
				}
			}
		}

		// 创建新文件夹
		private void CreateNewFolder(string folderName = "新建文件夹")
		{
			var path = owner.currentDirectory[owner.isleft];

			try
			{
				if (owner.fTPMGR.IsFtpPath(path))
				{
					// FTP创建文件夹
					var ftpSource = owner.fTPMGR.GetFtpSource(path);
					if (ftpSource != null)
					{
						string newFolderPath = Path.Combine(path, folderName).Replace("\\", "/");
						if (ftpSource.CreateDirectory(newFolderPath))
						{
							owner.RefreshPanel(owner.activeListView);
						}
					}
				}
				else
				{
					// 本地创建文件夹
					var newFolderPath = Path.Combine(path, folderName);
					FileSystemManager.CreateDirectory(newFolderPath);
					owner.RefreshPanel(owner.activeListView);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"创建文件夹失败: {ex.Message}", "错误");
			}
		}

		// 重命名选中的文件或文件夹
		private void RenameSelected()
		{
			var listView = owner.activeListView;
			if (listView == null || listView.SelectedItems.Count <= 0) return;

			var selectedItem = listView.SelectedItems[0];

			// 启用编辑模式
			selectedItem.BeginEdit();
		}

		// 搜索文件
		private void SearchFiles()
		{
			var searchForm = new Form
			{
				Text = "搜索文件",
				Size = new Size(400, 200),
				FormBorderStyle = FormBorderStyle.FixedDialog,
				StartPosition = FormStartPosition.CenterParent,
				MaximizeBox = false,
				MinimizeBox = false
			};

			var searchBox = new TextBox
			{
				Location = new Point(10, 10),
				Size = new Size(360, 20),
				PlaceholderText = "输入搜索关键词"
			};

			var searchButton = new Button
			{
				Text = "搜索",
				Location = new Point(150, 100),
				DialogResult = DialogResult.OK
			};

			searchForm.Controls.AddRange(new Control[] { searchBox, searchButton });
			searchForm.AcceptButton = searchButton;

			if (searchForm.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(searchBox.Text))
			{
				var searchPattern = searchBox.Text;
				var searchPath = owner.currentDirectory[owner.isleft];

				try
				{
					var files = Directory.GetFiles(searchPath, $"*{searchPattern}*", SearchOption.AllDirectories);
					var results = new Form
					{
						Text = "搜索结果",
						Size = new Size(600, 400),
						StartPosition = FormStartPosition.CenterParent
					};

					var resultList = new ListView
					{
						Dock = DockStyle.Fill,
						View = View.Details
					};

					resultList.Columns.Add("文件名", 200);
					resultList.Columns.Add("路径", 350);

					foreach (var file in files)
					{
						var item = new ListViewItem(Path.GetFileName(file));
						item.SubItems.Add(Path.GetDirectoryName(file));
						resultList.Items.Add(item);
					}

					results.Controls.Add(resultList);
					results.Show();
				}
				catch (Exception ex)
				{
					MessageBox.Show($"搜索文件时出错: {ex.Message}", "错误");
				}
			}
		}

		// 显示文件属性
		private void ShowFileProperties()
		{
			var listView = owner.activeListView;
			if (listView == null || listView.SelectedItems.Count <= 0) return;

			var selectedItem = listView.SelectedItems[0];
			var filePath = Path.Combine(owner.currentDirectory[owner.isleft], selectedItem.Text);

			try
			{
				var info = new FileInfo(filePath);
				var sb = new StringBuilder();
				sb.AppendLine($"名称: {info.Name}");
				sb.AppendLine($"类型: {(info.Attributes.HasFlag(FileAttributes.Directory) ? "文件夹" : "文件")}");
				sb.AppendLine($"位置: {info.DirectoryName}");
				sb.AppendLine($"大小: {FormatFileSize(info.Length)}");
				sb.AppendLine($"创建时间: {info.CreationTime}");
				sb.AppendLine($"修改时间: {info.LastWriteTime}");
				sb.AppendLine($"访问时间: {info.LastAccessTime}");
				sb.AppendLine($"属性: {info.Attributes}");

				MessageBox.Show(sb.ToString(), "文件属性", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"无法获取文件属性: {ex.Message}", "错误");
			}
		}

		// 格式化文件大小
		private string FormatFileSize(long bytes)
		{
			string[] sizes = { "B", "KB", "MB", "GB", "TB" };
			int order = 0;
			double size = bytes;
			while (size >= 1024 && order < sizes.Length - 1)
			{
				order++;
				size = size / 1024;
			}
			return $"{size:0.##} {sizes[order]}";
		}

		// 比较文件
		private void CompareFiles()
		{
			var listView = owner.activeListView;
			if (listView == null || listView.SelectedItems.Count != 2)
			{
				MessageBox.Show("请选择两个文件进行比较", "提示");
				return;
			}

			var file1 = Path.Combine(owner.currentDirectory[owner.isleft], listView.SelectedItems[0].Text);
			var file2 = Path.Combine(owner.currentDirectory[owner.isleft], listView.SelectedItems[1].Text);

			try
			{
				if (!File.Exists(file1) || !File.Exists(file2))
				{
					MessageBox.Show("所选文件不存在", "错误");
					return;
				}

				var form = new Form
				{
					Text = "文件比较",
					Size = new Size(800, 600),
					StartPosition = FormStartPosition.CenterScreen
				};

				var splitContainer = new SplitContainer
				{
					Dock = DockStyle.Fill,
					Orientation = Orientation.Horizontal
				};

				var textBox1 = new RichTextBox
				{
					Dock = DockStyle.Fill,
					ReadOnly = true,
					Font = new Font("Consolas", 10)
				};

				var textBox2 = new RichTextBox
				{
					Dock = DockStyle.Fill,
					ReadOnly = true,
					Font = new Font("Consolas", 10)
				};

				splitContainer.Panel1.Controls.Add(textBox1);
				splitContainer.Panel2.Controls.Add(textBox2);
				form.Controls.Add(splitContainer);

				// 读取文件内容
				textBox1.Text = File.ReadAllText(file1);
				textBox2.Text = File.ReadAllText(file2);

				// 高亮显示差异
				HighlightDifferences(textBox1, textBox2);

				form.Show();
			}
			catch (Exception ex)
			{
				MessageBox.Show($"比较文件时出错: {ex.Message}", "错误");
			}
		}

		// 高亮显示文本差异
		private void HighlightDifferences(RichTextBox box1, RichTextBox box2)
		{
			var lines1 = box1.Text.Split('\n');
			var lines2 = box2.Text.Split('\n');

			box1.Text = "";
			box2.Text = "";

			for (int i = 0; i < Math.Max(lines1.Length, lines2.Length); i++)
			{
				var line1 = i < lines1.Length ? lines1[i] : "";
				var line2 = i < lines2.Length ? lines2[i] : "";

				if (line1 != line2)
				{
					box1.SelectionBackColor = Color.LightPink;
					box2.SelectionBackColor = Color.LightPink;
				}
				else
				{
					box1.SelectionBackColor = Color.White;
					box2.SelectionBackColor = Color.White;
				}

				box1.AppendText(line1 + "\n");
				box2.AppendText(line2 + "\n");
			}
		}

		// 打包文件
		private void PackFiles()
		{
			var listView = owner.activeListView;
			if (listView == null || listView.SelectedItems.Count == 0) return;

			// 显示压缩选项对话框
			var packOptionDialog = new PackOptionDialog();
			if (packOptionDialog.ShowDialog() != DialogResult.OK)
				return;

			// 获取源面板和目标面板
			var sourcePanel = owner.activeListView;
			var targetPanel = owner.unactiveListView;
			var sourcePath = owner.uiManager.srcDir;
			var targetPath = owner.uiManager.targetDir;

			// 检查源路径和目标路径是否为FTP路径
			bool isSourceFtp = owner.fTPMGR.IsFtpPath(sourcePath);
			bool isTargetFtp = owner.fTPMGR.IsFtpPath(targetPath);

			string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
			string targetFile = "";

			try
			{
				var selectedFiles = listView.SelectedItems.Cast<ListViewItem>()
					.Select(item => owner.GetListItemPath(item))
					.ToArray();

				if (isSourceFtp)
				{
					// 如果源路径是FTP路径，先下载到临时目录
					Directory.CreateDirectory(tempDir);
					var ftpSource = owner.fTPMGR.GetFtpSource(sourcePath);
					if (ftpSource == null)
					{
						MessageBox.Show("无法获取FTP源");
						return;
					}
					foreach (var file in selectedFiles)
					{
						var localfile = ftpSource.DownloadFile(file);
						if (!File.Exists(localfile))
						{
							MessageBox.Show($"下载文件失败: {file}");
							Directory.Delete(tempDir, true);
							return;
						}
					}
					sourcePath = tempDir;
				}

				// 根据压缩选项对话框的选择决定目标文件名和压缩方式
				string extension = packOptionDialog.CompressMethod.ToLower() switch
				{
					"zip" => ".zip",
					"rar" => ".rar",
					"tar" => ".tar",
					"arj" => ".arj",
					"uc2" => ".uc2",
					"gz" => ".gz",
					"lha" => ".lha",
					"ace" => ".ace",
					"tgz" => ".tgz",
					_ => ".zip"
				};

				if (packOptionDialog.SeparateArchives)
				{
					// 每个文件创建单独的压缩包
					foreach (var file in selectedFiles)
					{
						string singleTargetFile = Path.Combine(targetPath, Path.GetFileNameWithoutExtension(file) + extension);
						if (File.Exists(singleTargetFile))
						{
							var result = MessageBox.Show($"文件 {singleTargetFile} 已存在，是否覆盖？", "确认", MessageBoxButtons.YesNoCancel);
							if (result == DialogResult.No) continue;
							if (result == DialogResult.Cancel) return;
						}

						var wcxModule = owner.wcxModuleList.GetModuleByExt(extension.TrimStart('.'));
						if (wcxModule != null)
						{
							int flags = packOptionDialog.IncludePath ? 1 : 0;
							wcxModule.PackFiles(singleTargetFile, "", sourcePath, file, flags);
						}
					}
				}
				else
				{
					// 创建单个压缩包
					targetFile = Path.Combine(targetPath, Path.GetFileNameWithoutExtension(selectedFiles[0]) + extension);
					if (File.Exists(targetFile))
					{
						var result = MessageBox.Show($"文件 {targetFile} 已存在，是否覆盖？", "确认", MessageBoxButtons.YesNo);
						if (result == DialogResult.No) return;
					}

					var wcxModule = owner.wcxModuleList.GetModuleByExt(extension.TrimStart('.'));
					if (wcxModule != null)
					{
						int flags = packOptionDialog.IncludePath ? 1 : 0;
						string fileList = string.Join("\n", selectedFiles);
						wcxModule.PackFiles(targetFile, "", sourcePath, fileList, flags);
					}
				}

				// 如果源路径是临时目录，删除它
				if (sourcePath == tempDir)
				{
					Directory.Delete(tempDir, true);
				}

				// 如果目标路径是FTP路径，上传压缩文件
				if (isTargetFtp)
				{
					var ftpTarget = owner.fTPMGR.GetFtpSource(targetPath);
					if (ftpTarget == null)
					{
						MessageBox.Show("无法获取FTP目标");
						return;
					}

					if (packOptionDialog.SeparateArchives)
					{
						foreach (var file in selectedFiles)
						{
							string singleTargetFile = Path.Combine(targetPath, Path.GetFileNameWithoutExtension(file) + extension);
							string localFile = Path.Combine(Path.GetTempPath(), Path.GetFileName(singleTargetFile));
							if (!ftpTarget.UploadFile(localFile, singleTargetFile))
							{
								MessageBox.Show($"上传文件失败: {singleTargetFile}");
								return;
							}
							File.Delete(localFile);
						}
					}
					else
					{
						string localFile = Path.Combine(Path.GetTempPath(), Path.GetFileName(targetFile));
						if (!ftpTarget.UploadFile(localFile, targetFile))
						{
							MessageBox.Show($"上传文件失败: {targetFile}");
							return;
						}
						File.Delete(localFile);
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"压缩文件时出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			finally
			{
				// 清理临时目录
				if (Directory.Exists(tempDir))
					Directory.Delete(tempDir, true);
			}

			// 刷新面板
			owner.RefreshPanel();
		}

		// 解压文件
		private void UnpackFiles()
		{
			var listView = owner.activeListView;
			if (listView == null || listView.SelectedItems.Count == 0) return;

			var selectedItem = listView.SelectedItems[0];
			var zipPath = Path.Combine(owner.currentDirectory[owner.isleft], selectedItem.Text);

			if (!zipPath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))      //TODO:其他压缩格式的支持，使用插件
			{
				MessageBox.Show("请选择 ZIP 文件", "提示");
				return;
			}

			var folderDialog = new FolderBrowserDialog
			{
				Description = "选择解压目标文件夹"
			};

			if (folderDialog.ShowDialog() == DialogResult.OK)
			{
				try
				{
					System.IO.Compression.ZipFile.ExtractToDirectory(
						zipPath,
						folderDialog.SelectedPath,
						Encoding.GetEncoding("GB2312"),
						true);

					MessageBox.Show("文件解压完成", "提示");
				}
				catch (Exception ex)
				{
					MessageBox.Show($"解压文件时出错: {ex.Message}", "错误");
				}
			}
		}

		private void ShowMultiRenameDialog()
		{
			var listView = owner.activeListView;
			if (listView == null || listView.SelectedItems.Count == 0)
			{
				MessageBox.Show("没有选中文件");
				return;
			}
			using var dialog = new MultiRenameForm(listView, owner.currentDirectory[owner.isleft]);
			if (dialog.ShowDialog() == DialogResult.OK)
			{
				owner.RefreshPanel(listView);
			}
		}

		private void ShowSyncDirsDialog()
		{
			var leftPath = owner.uiManager.LeftTree.SelectedNode != null
				? Helper.getFSpathbyTree(owner.uiManager.LeftTree.SelectedNode)
				: string.Empty;
			var rightPath = owner.uiManager.RightTree.SelectedNode != null
				? Helper.getFSpathbyTree(owner.uiManager.RightTree.SelectedNode)
				: string.Empty;

			var syncDlg = new SyncDirsDlg(leftPath, rightPath);
			syncDlg.Show();
		}
		private async Task<string> ShowAIassistDialog(List<string> filePaths, string prompt, bool isBackground = true)
		{
			string response = string.Empty;
			if (!isBackground)
			{
				var aiDlg = new AIassistDlg(filePaths, owner.lLM_Helper, owner.cmdProcessor);
				owner.Invoke(() => { aiDlg.Show(); });
			}
			else
			{
				foreach (var file in filePaths)
				{
					//read all text from file if it is a text file
					var result = Helper.Getfiletype(file);
					Debug.Print($"{file} => {result}");

					if (!result.Contains("text", StringComparison.OrdinalIgnoreCase)) continue;

					var content = File.ReadAllText(file);
					response = await owner.lLM_Helper.CallOllamaApiAsync(prompt + content);
				}
			}
			return response;
		}
	}
}

