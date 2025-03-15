using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Xml;

public class AutoUpdater
{
	private const string UpdateUrl = "https://yourserver.com/update.xml";
	private const string TempFolder = "UpdaterTemp";

	public static async Task CheckForUpdates(bool silentCheck)
	{
		try
		{
			// 创建临时目录
			Directory.CreateDirectory(TempFolder);

			// 获取当前版本
			Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version;

			// 获取服务器版本信息
			var (latestVersion, packageUrl, packageHash) = await GetServerVersionInfo();

			if (latestVersion > currentVersion)
			{
				if (!silentCheck || ShowUpdatePrompt(latestVersion))
				{
					string packagePath = await DownloadPackage(packageUrl);
					if (VerifyPackage(packagePath, packageHash))
					{
						LaunchUpdater(packagePath);
						Environment.Exit(0);
					}
				}
			}
		}
		catch (Exception ex)
		{
			//LogError($"Update failed: {ex.Message}");
			//if (!silentCheck) ShowErrorDialog("更新检查失败");
			MessageBox.Show("更新检查失败");
		}
	}
	private static bool ShowUpdatePrompt(Version version)
	{
		return MessageBox.Show("update to {0}", version.ToString(), MessageBoxButtons.YesNo) == DialogResult.Yes;
	}
	private static async Task<(Version, string, string)> GetServerVersionInfo()
	{
		using var client = new WebClient();
		string xml = await client.DownloadStringTaskAsync(UpdateUrl);

		XmlDocument doc = new XmlDocument();
		doc.LoadXml(xml);

		return (
			Version.Parse(doc.SelectSingleNode("/Update/Version")?.InnerText),
			doc.SelectSingleNode("/Update/PackageUrl")?.InnerText,
			doc.SelectSingleNode("/Update/PackageHash")?.InnerText
		);
	}

	private static async Task<string> DownloadPackage(string url)
	{
		string fileName = Path.GetFileName(url);
		string savePath = Path.Combine(TempFolder, fileName);

		using var client = new WebClient();
		await client.DownloadFileTaskAsync(url, savePath);
		return savePath;
	}

	private static bool VerifyPackage(string path, string expectedHash)
	{
		using var stream = File.OpenRead(path);
		using var sha = System.Security.Cryptography.SHA256.Create();
		byte[] hash = sha.ComputeHash(stream);
		string actualHash = BitConverter.ToString(hash).Replace("-", "");

		return actualHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase);
	}

	private static void LaunchUpdater(string packagePath)
	{
		string updaterExe = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Updater.exe");
		string arguments = $"\"{packagePath}\" \"{Process.GetCurrentProcess().ProcessName}\"";

		Process.Start(new ProcessStartInfo(updaterExe, arguments));
	}
}

	   