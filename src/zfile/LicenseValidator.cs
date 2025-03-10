using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Management;
namespace WinFormsApp1
{
    public static class LicenseValidator
	{
		// 假设这里是解密哈希值的方法
		// 假设使用AES解密
		private static byte[] DecryptHash(string encryptedHashFilePath)
		{
			byte[] key = LicenseValidator.DeriveEncryptionKey();
			byte[] iv = new byte[16];
			using (Aes aes = Aes.Create())
			{
				aes.Key = key;
				aes.IV = iv;
				using (var decryptor = aes.CreateDecryptor())
				using (var fs = new FileStream(encryptedHashFilePath, FileMode.Open))
				{
					// 跳过校验头
					fs.Seek(2, SeekOrigin.Begin);
					using (var cryptoStream = new CryptoStream(fs, decryptor, CryptoStreamMode.Read))
					{
						using (var ms = new MemoryStream())
						{
							cryptoStream.CopyTo(ms);
							return ms.ToArray();
						}
					}
				}
			}
		}

		// 计算校验和的方法，这里简单示例为计算字符串的哈希值作为校验和
		private static string CalculateChecksum(string input)
		{
			using (var sha = SHA256.Create())
			{
				byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
				return BitConverter.ToString(hash).Replace("-", "");
			}
		}
		//public static void CheckMemoryTamper()
		//{
		//	byte[] expected = GetLicenseMemorySignature();
		//	IntPtr addr = GetLicenseValidationMethodAddress();
		//	byte[] current = ReadMemory(addr, expected.Length);
		//	if (!current.SequenceEqual(expected))
		//		TriggerDefense();
		//}
		//public static bool TransferLicense(string oldLicense, string newHwId)
		//{
		//	string oldHwId = DecryptHwIdFromLicense(oldLicense);
		//	if (!ValidateLicense(oldLicense, oldHwId)) return false;
		//	return GenerateNewLicense(newHwId);
		//}
		// 需要单独保存私钥private.pem
		public static string GenerateLicenseKey(string hardwareId)
		{
			using var rsa = new RSACryptoServiceProvider();
			rsa.ImportFromPem(File.ReadAllText("private.pem"));

			byte[] signature = rsa.SignData(
				Encoding.UTF8.GetBytes(hardwareId),
				HashAlgorithmName.SHA256,
				RSASignaturePadding.Pkcs1
			);

			return Convert.ToBase64String(signature);
		}
		public static void antiDebug()
		{// 反调试检测
			if (System.Diagnostics.Debugger.IsAttached)
			{
				Debug.Print("SelfDestruct()"); // 删除关键文件或破坏数据
				Environment.FailFast("Anti-debug triggered");
			}
		}
		// 代码完整性校验
		public static void VerifyAssemblyIntegrity()
		{
			byte[] currentHash;
			using (var stream = File.OpenRead(Assembly.GetExecutingAssembly().Location))
			{
				currentHash = SHA256.Create().ComputeHash(stream);
			}

			// 正确哈希值应加密存储
			byte[] expectedHash = DecryptHash("encryptedHash.bin");

			if (!currentHash.SequenceEqual(expectedHash))
			{
				Debug.Print("CorruptApplication();");
				Environment.Exit(1);
			}
		}

		// 代码自修改（高级技巧）
		public static void DynamicCodeModification()
		{
			// 运行时修改关键方法（示例）
			var method = typeof(LicenseValidator).GetMethod("ValidateLicense");
			RuntimeHelpers.PrepareMethod(method.MethodHandle);

			unsafe
			{
				byte* ptr = (byte*)method.MethodHandle.Value.ToPointer();
				ptr[0] = 0xC3; // 修改汇编指令（危险操作！）
			}
		}
		public static void SaveLicense(string licenseKey)
		{
			byte[] key = DeriveEncryptionKey();
			byte[] iv = new byte[16];

			using (Aes aes = Aes.Create())
			{
				aes.Key = key;
				aes.IV = iv;

				using (var encryptor = aes.CreateEncryptor())
				using (var fs = new FileStream("license.dat", FileMode.Create))
				{
					// 添加校验头
					fs.WriteByte(0xAB);
					fs.WriteByte(0xCD);

					// 加密数据
					using (var cryptoStream = new CryptoStream(fs, encryptor, CryptoStreamMode.Write))
					{
						byte[] data = Encoding.UTF8.GetBytes(licenseKey);
						cryptoStream.Write(data, 0, data.Length);
					}
				}
			}
		}

		public static byte[] DeriveEncryptionKey()
		{
			// 基于硬件特征生成密钥
			string hwInfo = GenerateHardwareId().Substring(5, 32);
			using (var sha = SHA256.Create())
			{
				return sha.ComputeHash(Encoding.UTF8.GetBytes(hwInfo + salt.ToString()));
			}
		}// 主验证入口
		public static bool IsLicenseValid(string licenseKey)
		{
			// 基本验证
			bool valid = ValidateLicense(licenseKey);

			// 二次验证（隐蔽验证）
			bool stealthCheck = StealthValidation(licenseKey);

			// 三次验证（时间分散）
			Task.Run(() => BackgroundValidationAsync(licenseKey));

			return valid && stealthCheck;
		}

		// 隐蔽验证方法（分散在代码各处）
		private static bool StealthValidation(string licenseKey)
		{
			if (string.IsNullOrEmpty(licenseKey)) return false;
			// 使用硬件ID的部分特征验证
			string hwId = GenerateHardwareId();
			return licenseKey.Substring(5, 8) ==
				   CalculateChecksum(hwId.Substring(10, 16));
		}

		// 异步后台验证
		private static async Task BackgroundValidationAsync(string licenseKey)
		{
			await Task.Delay(new Random().Next(500, 2000));
			if (!ValidateLicense(licenseKey))
			{
				// 触发失效逻辑（如关闭核心功能）
				Debug.Print("DisableCriticalFeatures();");
			}
		}
		// 生成RSA密钥对（开发者预生成）
		// 命令行生成：openssl genrsa -out private.pem 2048
		//            openssl rsa -in private.pem -pubout -out public.pem

		// 将公钥分段存储（示例）
		private static string[] publicKeyFragments = new[] {
			// 真实情况需要更复杂的拆分
			"MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAu/F/6gLm0bIPwPQfXHA5\r\nx/5HHBUrrTfeSjT6TgWhwspQFpWkq6L/Y/Lwex+IH02Bs76PYRgXHtzBJ5qNATrn\r\nRzeOFSFVzFoL92JXOSqvzT/8ToBFhftGCba9jGJNl1TMcjB2cAjQOMeADGBBwBqQ\r\nOLoVVTghfLBxSFM3Z/H5cvNnsqbA57mXCHVQL/Ut2/zRc1fcxdNSemTNdRJXPhe/\r\nmS8tAhzZXxSTMvHvDWRpfc3XbkjoLtN2JScopnWKg4rOVx3mbLXmGAi0JHnHlj69\r\naXzNp39bVOD69W5Ja5ut8cWZqzabhaTDlX9JfrIzDa3RfiiSv5e34xBAADr1uEXU\r\n5QIDAQAB"
		};
		public static void RSAKey(out string xmlKeys, out string xmlPublicKey)
		{
			RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
			xmlKeys = rsa.ToXmlString(true);
			xmlPublicKey = rsa.ToXmlString(false);
		}
		// 运行时动态组合公钥
		public static RSAParameters LoadPublicKey()
		{
			string fullKey = string.Concat(publicKeyFragments)
								 .Replace("X", "A") // 添加混淆字符
								 .Substring(2);      // 偏移处理

			using (var rsa = new RSACryptoServiceProvider())
			{
				rsa.FromXmlString(fullKey);
				return rsa.ExportParameters(false);
			}
		}

		// 验证注册码
		public static bool ValidateLicense(string licenseKey)
		{
			try
			{
				// 获取当前硬件ID
				string currentHwId = GenerateHardwareId();

				// 加载公钥
				RSAParameters publicKey = LoadPublicKey();

				// Base64解码
				byte[] signature = Convert.FromBase64String(licenseKey);

				using (var rsa = new RSACryptoServiceProvider())
				{
					rsa.ImportParameters(publicKey);
					return rsa.VerifyData(
						Encoding.UTF8.GetBytes(currentHwId),
						signature,
						HashAlgorithmName.SHA256,
						RSASignaturePadding.Pkcs1
					);
				}
			}
			catch
			{
				return false;
			}
		}// 通过算法生成动态盐值
		public static byte[] GenerateDynamicSalt()
		{
			byte[] baseSalt = { 0x12, 0x34, 0x56, 0x78 };
			int dayOfYear = DateTime.Now.DayOfYear % 256;
			for (int i = 0; i < baseSalt.Length; i++)
			{
				baseSalt[i] ^= (byte)(dayOfYear + i);
			}
			return baseSalt;
		}

		// 在硬件指纹生成时调用
		public static byte[] salt = GenerateDynamicSalt();
		public static string GenerateHardwareId()
		{
			var sb = new StringBuilder();

			// 多硬件信息组合
			using (var searcher = new ManagementObjectSearcher("SELECT ProcessorId, Name FROM Win32_Processor"))
			{
				foreach (ManagementObject mo in searcher.Get())
				{
					sb.Append(mo["ProcessorId"]?.ToString().Trim());
					sb.Append(mo["Name"]?.ToString().GetHashCode());
					break;
				}
			}

			using (var searcher = new ManagementObjectSearcher("SELECT SerialNumber, Model FROM Win32_DiskDrive WHERE Index = 0"))
			{
				foreach (ManagementObject mo in searcher.Get())
				{
					sb.Append(mo["SerialNumber"]?.ToString().Trim());
					sb.Append(mo["Model"]?.ToString().GetHashCode());
					break;
				}
			}

			// 加入系统特征
			sb.Append(Environment.ProcessorCount);
			sb.Append(Environment.MachineName.GetStableHash());

			// 多层哈希加盐
			byte[] salt = { 0x12, 0xAB, 0x56, 0xFF }; // 动态盐值见下文
			using (var hmac = new HMACSHA256(salt))
			{
				byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
				return BitConverter.ToString(hash).Replace("-", "");
			}
		}

		// 稳定哈希扩展方法
		public static int GetStableHash(this string str)
		{
			unchecked
			{
				int hash = 23;
				foreach (char c in str)
					hash = hash * 31 + c;
				return hash;
			}
		}
	}
}
