using System.Diagnostics;
using System.Text.RegularExpressions;

namespace zfile
{
    public class ShellExecuteHelper
    {
        private readonly Dictionary<string, Func<string[], string>> _variableHandlers;
		private Form1 owner;

        public ShellExecuteHelper(Form1 parent)
        {
			owner = parent;
		
			_variableHandlers = new Dictionary<string, Func<string[], string>>
            {
				{ "1", HandleFileNames},
				{ "F", HandleLongFileNames },         // %F - 长文件名
                { "f", HandleFileNames },         // %f - 文件名
                { "d", HandleDirectories },       // %d - 目录路径
                { "p", HandleFullPaths },         // %p - 完整路径
                { "D", HandleCurrentPanel },      // %D - 当前面板路径
                { "L", HandleFileList },          // %L - 文件列表
                { "R", HandleRelativePaths },      // %R - 相对路径
				{ "M", HandleTargetFile },           // %M - 插入目标文件夹的当前文件名
				{ "N", HandleSrcFile },           // %N - 插入光标所在的文件名。
				{ "T", HandleTargetPath },           // %T - 插入当前目标路径
				{ "P", HandleSrcPath },           // %P - 插入来源路径
				{ "O", HandleNone }
			};
        }
		private string HandleNone(string[] files)
		{
			return string.Empty;
		}
		private string HandleLongFileNames(string[] files)
		{
			return files[0].Replace("%F", owner.uiManager.targetfiles);
		}

		private string HandleTargetFile(string[] files)
		{
			return files[0].Replace("%M", owner.uiManager.targetfiles);
		}
		private string HandleSrcFile(string[] files)
		{
			return files[0].Replace("%N", owner.uiManager.srcfiles);
		}

		private string HandleTargetPath(string[] files)
		{
			return files[0].Replace("%T", owner.uiManager.targetDir);
		}
		private string HandleSrcPath(string[] files)
		{
			return files[0].Replace("%P", owner.uiManager.srcDir);
		}

		public List<string> ReplaceVariableParams(string cmd, string[] files, string currentPath, bool useQuotes = true)
        {
			bool debugMode = false;
			if (cmd.StartsWith('?'))
			{
				debugMode = true;
				cmd = cmd.TrimStart('?');
			}
			
			// 处理引号控制			
			if (useQuotes)
				cmd = cmd.Replace("%\"0", "").Replace("%\"1", "\"");

			// 处理提示框变量 %[prompt]
			cmd = Regex.Replace(cmd, @"%\[(.*?)\]", match =>
            {
                var prompt = match.Groups[1].Value;
                return Microsoft.VisualBasic.Interaction.InputBox(prompt, "Input Required", "");
            });
			var cmds = cmd.Split('|').ToList();

			// 处理文件变量
			foreach (var handler in _variableHandlers)
				cmds = processEachcmd($"%{handler.Key}", cmds);

			if (debugMode)
				MessageBox.Show(string.Join(' ', cmds));
            return cmds;
        }
		private List<string> processEachcmd(string pattern, List<string> cmds)
		{
			var expandcmds = cmds;
			foreach (var c in cmds)
			{
				if (c.Contains(pattern))
				{
					//result = result.Replace(pattern, handler.Value(files));
					var filelist = owner.uiManager.args[pattern];
					if(filelist.Length != 0)
						expandcmds = ExpandCmdStringByArg(c, filelist, pattern);//TODO: BUGFIX: WILL OVERWRITE PARENT CMDS CONTENTS
				}
			}
			return expandcmds;
		}
		private List<string> ExpandCmdStringByArg(string cmd, string files, string pattern)
		{
			var cmds = new List<string>();
			var filelist = SplitStringWithSeparator(files);
			foreach (var file in filelist)
				cmds.Add(cmd.Replace(pattern, file));
			return cmds;
		}
		public static List<string> SplitStringWithSeparator(string input)
		{
			List<string> result = new List<string>();
			bool inQuotes = false;
			string currentPart = "";

			foreach (char c in input)
			{
				if (c == '"')
					inQuotes = !inQuotes;
				else if (c == '|' && !inQuotes)
				{
					if (!string.IsNullOrEmpty(currentPart))
					{
						result.Add(currentPart);
						currentPart = "";
					}
				}
				else
					currentPart += c;
			}

			// 处理最后一部分
			if (!string.IsNullOrEmpty(currentPart))
				result.Add(currentPart);

			return result;
		}
		//public bool ExecuteCommand(string command, string[] files, string currentPath, bool keepTerminalOpen = false)
  //      {
		//	//TODO: 
  //          //try
  //          //{
  //          //    var processedCommand = PrepareParameter(command, files, currentPath);

  //          //    // 检查特殊命令标记
  //          //    if (processedCommand.Contains("{!SHELL}"))
  //          //    {
  //          //        return ExecuteInTerminal(processedCommand, keepTerminalOpen);
  //          //    }
  //          //    else if (processedCommand.Contains("{!EDITOR}"))
  //          //    {
  //          //        return ExecuteInEditor(processedCommand);
  //          //    }
  //          //    else if (processedCommand.Contains("{!VIEWER}"))
  //          //    {
  //          //        return ExecuteInViewer(processedCommand);
  //          //    }
                
  //          //    // 普通命令执行
  //          //    return ExecuteNormalProcess(processedCommand);
  //          //}
  //          //catch (Exception ex)
  //          //{
  //          //    Debug.WriteLine($"Execute command failed: {ex.Message}");
  //          //    return false;
  //          //}
  //      }

        public List<string> PrepareParameter(string command, string[] files, string currentPath)
        {
            // 环境变量替换
			var cmd = Helper.GetPathByEnv(command);
            // 波浪号展开
            if (cmd.StartsWith("~"))
                cmd = cmd.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

            // 变量替换
            return ReplaceVariableParams(cmd, files, currentPath);
        }

        private bool ExecuteInTerminal(string command, bool keepOpen)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {command} {(keepOpen ? "& pause" : "")}",
                UseShellExecute = true,
                CreateNoWindow = false
            };

            using var process = Process.Start(startInfo);
            return true;
        }

        private bool ExecuteInEditor(string command)
        {
            // 这里可以实现调用配置的编辑器
            throw new NotImplementedException();
        }

        private bool ExecuteInViewer(string command)
        {
            // 这里可以实现调用配置的查看器
            throw new NotImplementedException();
        }

        private bool ExecuteNormalProcess(string command)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {command}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            return process != null;
        }

        #region Variable Handlers
        private string HandleFileNames(string[] files)
        {
            return string.Join(" ", files.Select(Path.GetFileName));
        }

        private string HandleDirectories(string[] files)
        {
            return string.Join(" ", files.Select(Path.GetDirectoryName));
        }

        private string HandleFullPaths(string[] files)
        {
            return string.Join(" ", files);
        }

        private string HandleCurrentPanel(string[] files)
        {
            return files.Length > 0 ? Path.GetDirectoryName(files[0]) : "";
        }

        private string HandleFileList(string[] files)
        {
            // 创建临时文件列表
            var tempFile = Path.GetTempFileName();
            File.WriteAllLines(tempFile, files);
            return tempFile;
        }

        private string HandleRelativePaths(string[] files)
        {
            var currentDir = Directory.GetCurrentDirectory();
            return string.Join(" ", files.Select(f => Path.GetRelativePath(currentDir, f)));
        }
		#endregion
	}
}
