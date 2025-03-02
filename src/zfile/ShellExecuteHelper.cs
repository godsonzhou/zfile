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
                { "f", HandleFileNames },         // %f - �ļ���
                { "d", HandleDirectories },       // %d - Ŀ¼·��
                { "p", HandleFullPaths },         // %p - ����·��
                { "D", HandleCurrentPanel },      // %D - ��ǰ���·��
                { "L", HandleFileList },          // %L - �ļ��б�
                { "R", HandleRelativePaths }      // %R - ���·��
            };
        }

        public string ReplaceVariableParams(string command, string[] files, string currentPath, bool useQuotes = true)
        {
            var result = command;

            // ������ʾ����� %[prompt]
            result = Regex.Replace(result, @"%\[(.*?)\]", match =>
            {
                var prompt = match.Groups[1].Value;
                return Microsoft.VisualBasic.Interaction.InputBox(prompt, "Input Required", "");
            });

            // �����ļ�����
            foreach (var handler in _variableHandlers)
            {
                var pattern = $"%{handler.Key}";
                if (result.Contains(pattern))
                {
                    result = result.Replace(pattern, handler.Value(files));
                }
            }

            // �������ſ���
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

                // �������������
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
                
                // ��ͨ����ִ��
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
            // ���������滻
            var result = Environment.ExpandEnvironmentVariables(command);
            
            // ���˺�չ��
            if (result.StartsWith("~"))
            {
                result = result.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            }

            // �����滻
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
            // �������ʵ�ֵ������õı༭��
            throw new NotImplementedException();
        }

        private bool ExecuteInViewer(string command)
        {
            // �������ʵ�ֵ������õĲ鿴��
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
            // ������ʱ�ļ��б�
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
