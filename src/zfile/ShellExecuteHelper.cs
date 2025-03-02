using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace WinFormsApp1
{
    public class ShellExecuteHelper
    {
        private readonly Dictionary<string, Func<string[], string>> _variableHandlers;

        public ShellExecuteHelper()
        {
            _variableHandlers = new Dictionary<string, Func<string[], string>>
            {
                { "f", HandleFileNames },         // %f - 文件名
                { "d", HandleDirectories },       // %d - 目录路径
                { "p", HandleFullPaths },         // %p - 完整路径
                { "D", HandleCurrentPanel },      // %D - 当前面板路径
                { "L", HandleFileList },          // %L - 文件列表
                { "R", HandleRelativePaths }      // %R - 相对路径
            };
        }

        public string ReplaceVariableParams(string command, string[] files, string currentPath, bool useQuotes = true)
        {
            var result = command;

            // 处理提示框变量 %[prompt]
            result = Regex.Replace(result, @"%\[(.*?)\]", match =>
            {
                var prompt = match.Groups[1].Value;
                return Microsoft.VisualBasic.Interaction.InputBox(prompt, "Input Required", "");
            });

            // 处理文件变量
            foreach (var handler in _variableHandlers)
            {
                var pattern = $"%{handler.Key}";
                if (result.Contains(pattern))
                {
                    result = result.Replace(pattern, handler.Value(files));
                }
            }

            // 处理引号控制
            if (useQuotes)
            {
                result = result.Replace("%\"0", "").Replace("%\"1", "\"");
            }

            return result;
        }

        public bool ExecuteCommand(string command, string[] files, string currentPath, bool keepTerminalOpen = false)
        {
            try
            {
                var processedCommand = PrepareParameter(command, files, currentPath);

                // 检查特殊命令标记
                if (processedCommand.Contains("{!SHELL}"))
                {
                    return ExecuteInTerminal(processedCommand, keepTerminalOpen);
                }
                else if (processedCommand.Contains("{!EDITOR}"))
                {
                    return ExecuteInEditor(processedCommand);
                }
                else if (processedCommand.Contains("{!VIEWER}"))
                {
                    return ExecuteInViewer(processedCommand);
                }
                
                // 普通命令执行
                return ExecuteNormalProcess(processedCommand);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Execute command failed: {ex.Message}");
                return false;
            }
        }

        private string PrepareParameter(string command, string[] files, string currentPath)
        {
            // 环境变量替换
            var result = Environment.ExpandEnvironmentVariables(command);
            
            // 波浪号展开
            if (result.StartsWith("~"))
            {
                result = result.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            }

            // 变量替换
            result = ReplaceVariableParams(result, files, currentPath);

            return result;
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
