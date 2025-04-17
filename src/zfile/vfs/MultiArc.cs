using System.Text;
using System.Text.RegularExpressions;

namespace zfile
{
	/// <summary>
	/// Constants for multi archiver support
	/// </summary>
	public static class MultiArcConsts
	{
		public const int MaxSignSize = 1024;
		public const int SignSeekRange = 1024 * 1024;

		// Flags for file path handling
		public const int MAF_UNIX_PATH = 1;        // Use Unix path delimiter (/)
		public const int MAF_WIN_PATH = 2;         // Use Windows path delimiter (\)
		public const int MAF_UNIX_ATTR = 4;        // Use Unix file attributes
		public const int MAF_WIN_ATTR = 8;         // Use Windows file attributes
	}

	/// <summary>
	/// Flags for multi archiver operations
	/// </summary>
	[Flags]
	public enum MultiArcFlag
	{
		FileNameList = 1
	}

	/// <summary>
	/// Signature structure for file format detection
	/// </summary>
	public class Signature
	{
		public byte[] Value { get; set; } = new byte[MultiArcConsts.MaxSignSize];
		public int Size { get; set; }
	}

	/// <summary>
	/// List of signatures for file format detection
	/// </summary>
	public class SignatureList : List<Signature>
	{
		public void Clean()
		{
			Clear();
		}
	}

	/// <summary>
	/// Signature position for file format detection
	/// </summary>
	public class SignaturePosition
	{
		public int Value { get; set; }
		public bool Sign { get; set; }
	}

	/// <summary>
	/// List of signature positions for file format detection
	/// </summary>
	public class SignaturePositionList : List<SignaturePosition>
	{
		public void Clean()
		{
			Clear();
		}
	}

	/// <summary>
	/// Archive item information
	/// </summary>
	public class ArchiveItem : ICloneable
	{
		public string FileName { get; set; }
		public string FileExt { get; set; }
		public string FileLink { get; set; }
		public long PackSize { get; set; }
		public long UnpSize { get; set; }
		public int Year { get; set; }
		public int Month { get; set; }
		public int Day { get; set; }
		public int Hour { get; set; }
		public int Minute { get; set; }
		public int Second { get; set; }
		public DateTime DateTime { get; set; }
		public FileAttributes Attributes { get; set; }

		public object Clone()
		{
			return new ArchiveItem
			{
				FileName = FileName,
				FileExt = FileExt,
				FileLink = FileLink,
				PackSize = PackSize,
				UnpSize = UnpSize,
				Year = Year,
				Month = Month,
				Day = Day,
				Hour = Hour,
				Minute = Minute,
				Second = Second,
				Attributes = Attributes
			};
		}
	}

	/// <summary>
	/// Multi archiver item configuration
	/// </summary>
	public class MultiArcItem
	{
		private string _ext;
		private List<Regex> _maskList;
		private bool _seekAfterSignPos;
		private string _signature;
		private string _signaturePosition;
		private int _signatureSeekRange;
		private SignatureList _signatureList;
		private SignaturePositionList _signaturePositionList;

		// Public properties
		public string Packer { get; set; }
		public string Archiver { get; set; }
		public string Description { get; set; }
		public string Start { get; set; }
		public string End { get; set; }
		public List<string> Format { get; } = new List<string>();
		public string List { get; set; }
		public string Extract { get; set; }
		public string ExtractWithoutPath { get; set; }
		public string Test { get; set; }
		public string Delete { get; set; }
		public string Add { get; set; }
		public string AddSelfExtract { get; set; }
		public string PasswordQuery { get; set; }
		public int FormMode { get; set; }
		public MultiArcFlag Flags { get; set; }
		public bool Enabled { get; set; }
		public bool Output { get; set; }
		public bool Debug { get; set; }

		// Properties with custom getters/setters
		public string Extension
		{
			get => _ext;
			set
			{
				if (_ext != value)
				{
					_ext = value;
					_maskList = new List<Regex>();

					if (!string.IsNullOrEmpty(value))
					{
						string[] extensions = value.Split(',');
						foreach (string ext in extensions)
						{
							string pattern = "^.*\\." + Regex.Escape(ext.Trim()) + "$";
							_maskList.Add(new Regex(pattern, RegexOptions.IgnoreCase));
						}
					}
				}
			}
		}

		public string ID
		{
			get => _signature;
			set
			{
				_signature = value;
				_signatureList.Clean();

				if (string.IsNullOrEmpty(value))
					return;

				string[] signatures = value.Split(',');
				foreach (string signStr in signatures)
				{
					var signature = new Signature();
					int index = 0;

					string[] bytes = signStr.Trim().Split(' ');
					foreach (string byteStr in bytes)
					{
						if (string.IsNullOrEmpty(byteStr))
							continue;

						if (index < MultiArcConsts.MaxSignSize)
						{
							try
							{
								signature.Value[index] = Convert.ToByte(byteStr, 16);
								index++;
							}
							catch (Exception)
							{
								// Skip invalid byte values
							}
						}
					}

					signature.Size = index;
					if (index > 0)
						_signatureList.Add(signature);
				}
			}
		}

		public string IDPos
		{
			get => _signaturePosition;
			set
			{
				_signaturePosition = value;
				_signaturePositionList.Clean();
				_seekAfterSignPos = false;

				if (string.IsNullOrEmpty(value))
					return;

				string[] positions = value.Replace("0x", "$").Split(',');
				foreach (string posStr in positions)
				{
					string trimmedPos = posStr.Trim();

					if (trimmedPos == "<SeekID>")
					{
						_seekAfterSignPos = true;
					}
					else
					{
						try
						{
							int posValue;
							if (trimmedPos.StartsWith("$"))
								posValue = Convert.ToInt32(trimmedPos.Substring(1), 16);
							else
								posValue = Convert.ToInt32(trimmedPos);

							var signPos = new SignaturePosition
							{
								Sign = posValue >= 0,
								Value = Math.Abs(posValue)
							};

							_signaturePositionList.Add(signPos);
						}
						catch (Exception)
						{
							// Skip invalid position values
						}
					}
				}
			}
		}

		public string IDSeekRange
		{
			get => _signatureSeekRange == MultiArcConsts.SignSeekRange ? string.Empty : _signatureSeekRange.ToString();
			set
			{
				if (!int.TryParse(value, out _signatureSeekRange))
					_signatureSeekRange = MultiArcConsts.SignSeekRange;
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		public MultiArcItem()
		{
			_signatureList = new SignatureList();
			_signaturePositionList = new SignaturePositionList();
			_signatureSeekRange = MultiArcConsts.SignSeekRange;
			Enabled = true;
		}

		/// <summary>
		/// Check if file matches the extension pattern
		/// </summary>
		public bool Matches(string fileName)
		{
			if (_maskList == null || _maskList.Count == 0)
				return false;

			return _maskList.Any(mask => mask.IsMatch(fileName));
		}

		/// <summary>
		/// Check if this archiver can handle the specified file
		/// </summary>
		public bool CanYouHandleThisFile(string fileName)
		{
			if (!File.Exists(fileName))
				return false;

			// Check by signature positions
			if (_signaturePositionList.Count > 0 && _signatureList.Count > 0)
			{
				using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				{
					foreach (var signPos in _signaturePositionList)
					{
						long position = signPos.Value;
						if (!signPos.Sign) // Negative position means from end
						{
							position = fs.Length - position;
						}

						if (position < 0 || position >= fs.Length)
							continue;

						fs.Position = position;

						byte[] buffer = new byte[MultiArcConsts.MaxSignSize];
						int bytesRead = fs.Read(buffer, 0, buffer.Length);

						if (bytesRead > 0)
						{
							foreach (var signature in _signatureList)
							{
								if (signature.Size <= bytesRead)
								{
									bool match = true;
									for (int i = 0; i < signature.Size; i++)
									{
										if (buffer[i] != signature.Value[i])
										{
											match = false;
											break;
										}
									}

									if (match)
										return true;
								}
							}
						}
					}
				}
			}

			// Try raw seek ID if enabled
			if (_seekAfterSignPos && _signatureList.Count > 0)
			{
				using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				{
					int searchRange = (int)Math.Min(_signatureSeekRange, fs.Length);
					byte[] buffer = new byte[searchRange];
					int bytesRead = fs.Read(buffer, 0, searchRange);

					if (bytesRead > 0)
					{
						for (int i = 0; i < bytesRead; i++)
						{
							foreach (var signature in _signatureList)
							{
								if (i + signature.Size <= bytesRead)
								{
									bool match = true;
									for (int j = 0; j < signature.Size; j++)
									{
										if (buffer[i + j] != signature.Value[j])
										{
											match = false;
											break;
										}
									}

									if (match)
										return true;
								}
							}
						}
					}
				}
			}

			return false;
		}

		/// <summary>
		/// Create a clone of this MultiArcItem
		/// </summary>
		public MultiArcItem Clone()
		{
			var result = new MultiArcItem
			{
				Packer = Packer,
				Archiver = Archiver,
				Description = Description,
				Extension = Extension,
				Start = Start,
				End = End,
				List = List,
				Extract = Extract,
				ExtractWithoutPath = ExtractWithoutPath,
				Test = Test,
				Delete = Delete,
				Add = Add,
				AddSelfExtract = AddSelfExtract,
				PasswordQuery = PasswordQuery,
				Flags = Flags,
				FormMode = FormMode,
				Enabled = Enabled,
				Output = Output,
				Debug = Debug,
				ID = ID,
				IDPos = IDPos,
				IDSeekRange = IDSeekRange
			};

			// Copy format strings
			foreach (var format in Format)
			{
				result.Format.Add(format);
			}

			return result;
		}
	}

	/// <summary>
	/// List of multi archiver configurations
	/// </summary>
	public class MultiArcList
	{
		private readonly Dictionary<string, MultiArcItem> _items = new Dictionary<string, MultiArcItem>();

		/// <summary>
		/// Get the number of items in the list
		/// </summary>
		public int Count => _items.Count;

		/// <summary>
		/// Get an item by index
		/// </summary>
		public MultiArcItem this[int index]
		{
			get
			{
				if (index < 0 || index >= _items.Count)
					throw new IndexOutOfRangeException();

				return _items.Values.ElementAt(index);
			}
		}

		/// <summary>
		/// Get or set a name by index
		/// </summary>
		public string GetName(int index)
		{
			if (index < 0 || index >= _items.Count)
				throw new IndexOutOfRangeException();

			return _items.Keys.ElementAt(index);
		}

		/// <summary>
		/// Set a name by index
		/// </summary>
		public void SetName(int index, string value)
		{
			if (index < 0 || index >= _items.Count)
				throw new IndexOutOfRangeException();

			string oldKey = _items.Keys.ElementAt(index);
			MultiArcItem item = _items[oldKey];

			_items.Remove(oldKey);
			_items.Add(value, item);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		public MultiArcList()
		{
		}

		/// <summary>
		/// Auto-configure archivers based on available executables
		/// </summary>
		public void AutoConfigure()
		{
			foreach (var item in _items.Values)
			{
				string exePath = item.Archiver;

				// Replace environment variables in path
				exePath = Environment.ExpandEnvironmentVariables(exePath);

				if (!File.Exists(exePath))
				{
					// Try to find in PATH
					string fileName = Path.GetFileName(exePath);
					string pathEnv = Environment.GetEnvironmentVariable("PATH");
					string[] paths = pathEnv.Split(Path.PathSeparator);

					bool found = false;
					foreach (string path in paths)
					{
						string fullPath = Path.Combine(path, fileName);
						if (File.Exists(fullPath))
						{
							item.Archiver = fullPath;
							item.Enabled = true;
							found = true;
							break;
						}
					}

					if (!found)
						item.Enabled = false;
				}
				else
				{
					item.Enabled = true;
				}
			}
		}

		/// <summary>
		/// Clear all items
		/// </summary>
		public void Clear()
		{
			_items.Clear();
		}

		/// <summary>
		/// Load configuration from INI file
		/// </summary>
		public void LoadFromFile(string fileName)
		{
			Clear();

			if (!File.Exists(fileName))
				return;

			try
			{
				// Simple INI parser
				string currentSection = null;
				MultiArcItem currentItem = null;
				bool firstTime = true;

				string[] lines = File.ReadAllLines(fileName);
				foreach (string line in lines)
				{
					string trimmedLine = line.Trim();

					// Skip comments and empty lines
					if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith(";"))
						continue;

					// Section
					if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
					{
						currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2);

						if (currentSection.Equals("MultiArc", StringComparison.OrdinalIgnoreCase))
						{
							// Special section for global settings
							continue;
						}

						currentItem = new MultiArcItem { Packer = currentSection };
						_items.Add(currentSection, currentItem);
					}
					// Key-value pair
					else if (currentSection != null && trimmedLine.Contains("="))
					{
						int equalPos = trimmedLine.IndexOf('=');
						string key = trimmedLine.Substring(0, equalPos).Trim();
						string value = trimmedLine.Substring(equalPos + 1).Trim();

						// Remove quotes if present
						if (value.StartsWith("\"") && value.EndsWith("\""))
							value = value.Substring(1, value.Length - 2);

						if (currentSection.Equals("MultiArc", StringComparison.OrdinalIgnoreCase))
						{
							if (key.Equals("FirstTime", StringComparison.OrdinalIgnoreCase))
								firstTime = bool.Parse(value);
						}
						else if (currentItem != null)
						{
							// Handle properties
							if (key.Equals("Archiver", StringComparison.OrdinalIgnoreCase))
								currentItem.Archiver = value;
							else if (key.Equals("Description", StringComparison.OrdinalIgnoreCase))
								currentItem.Description = value;
							else if (key.Equals("ID", StringComparison.OrdinalIgnoreCase))
								currentItem.ID = value;
							else if (key.Equals("IDPos", StringComparison.OrdinalIgnoreCase))
								currentItem.IDPos = value;
							else if (key.Equals("IDSeekRange", StringComparison.OrdinalIgnoreCase))
								currentItem.IDSeekRange = value;
							else if (key.Equals("Extension", StringComparison.OrdinalIgnoreCase))
								currentItem.Extension = value;
							else if (key.Equals("Start", StringComparison.OrdinalIgnoreCase))
								currentItem.Start = value;
							else if (key.Equals("End", StringComparison.OrdinalIgnoreCase))
								currentItem.End = value;
							else if (key.StartsWith("Format", StringComparison.OrdinalIgnoreCase))
							{
								if (!string.IsNullOrEmpty(value))
									currentItem.Format.Add(value);
							}
							else if (key.Equals("List", StringComparison.OrdinalIgnoreCase))
								currentItem.List = value;
							else if (key.Equals("Extract", StringComparison.OrdinalIgnoreCase))
								currentItem.Extract = value;
							else if (key.Equals("ExtractWithoutPath", StringComparison.OrdinalIgnoreCase))
								currentItem.ExtractWithoutPath = value;
							else if (key.Equals("Test", StringComparison.OrdinalIgnoreCase))
								currentItem.Test = value;
							else if (key.Equals("Delete", StringComparison.OrdinalIgnoreCase))
								currentItem.Delete = value;
							else if (key.Equals("Add", StringComparison.OrdinalIgnoreCase))
								currentItem.Add = value;
							else if (key.Equals("AddSelfExtract", StringComparison.OrdinalIgnoreCase))
								currentItem.AddSelfExtract = value;
							else if (key.Equals("PasswordQuery", StringComparison.OrdinalIgnoreCase))
								currentItem.PasswordQuery = value;
							else if (key.Equals("Flags", StringComparison.OrdinalIgnoreCase))
								currentItem.Flags = (MultiArcFlag)int.Parse(value);
							else if (key.Equals("FormMode", StringComparison.OrdinalIgnoreCase))
								currentItem.FormMode = int.Parse(value);
							else if (key.Equals("Enabled", StringComparison.OrdinalIgnoreCase))
								currentItem.Enabled = bool.Parse(value);
							else if (key.Equals("Output", StringComparison.OrdinalIgnoreCase))
								currentItem.Output = bool.Parse(value);
							else if (key.Equals("Debug", StringComparison.OrdinalIgnoreCase))
								currentItem.Debug = bool.Parse(value);
						}
					}
				}

				if (firstTime)
					AutoConfigure();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error loading MultiArc configuration: {ex.Message}");
			}
		}

		/// <summary>
		/// Save configuration to INI file
		/// </summary>
		public void SaveToFile(string fileName)
		{
			try
			{
				using (StreamWriter writer = new StreamWriter(fileName))
				{
					// Write items
					foreach (var pair in _items)
					{
						string section = pair.Key;
						MultiArcItem item = pair.Value;

						writer.WriteLine($"[{section}]");
						writer.WriteLine($"Archiver={item.Archiver}");
						writer.WriteLine($"Description={item.Description}");
						writer.WriteLine($"ID={item.ID}");
						writer.WriteLine($"IDPos={item.IDPos}");
						writer.WriteLine($"IDSeekRange={item.IDSeekRange}");
						writer.WriteLine($"Extension={item.Extension}");
						writer.WriteLine($"Start={item.Start}");
						writer.WriteLine($"End={item.End}");

						for (int i = 0; i < item.Format.Count; i++)
						{
							writer.WriteLine($"Format{i}={item.Format[i]}");
						}

						writer.WriteLine($"List={item.List}");
						writer.WriteLine($"Extract={item.Extract}");
						writer.WriteLine($"ExtractWithoutPath={item.ExtractWithoutPath}");
						writer.WriteLine($"Test={item.Test}");
						writer.WriteLine($"Delete={item.Delete}");
						writer.WriteLine($"Add={item.Add}");
						writer.WriteLine($"AddSelfExtract={item.AddSelfExtract}");
						writer.WriteLine($"PasswordQuery={item.PasswordQuery}");
						writer.WriteLine($"Flags={Convert.ToInt32(item.Flags)}");
						writer.WriteLine($"FormMode={item.FormMode}");
						writer.WriteLine($"Enabled={item.Enabled.ToString().ToLower()}");
						writer.WriteLine($"Output={item.Output.ToString().ToLower()}");
						writer.WriteLine($"Debug={item.Debug.ToString().ToLower()}");
						writer.WriteLine();
					}

					// Write global settings
					writer.WriteLine("[MultiArc]");
					writer.WriteLine("FirstTime=false");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error saving MultiArc configuration: {ex.Message}");
			}
		}

		/// <summary>
		/// Add a new item to the list
		/// </summary>
		public int Add(string name, MultiArcItem item)
		{
			if (_items.ContainsKey(name))
				throw new ArgumentException($"Item with name '{name}' already exists");

			_items.Add(name, item);
			return _items.Count - 1;
		}

		/// <summary>
		/// Insert a new item at the specified index
		/// </summary>
		public int Insert(int index, string name, MultiArcItem item)
		{
			if (_items.ContainsKey(name))
				throw new ArgumentException($"Item with name '{name}' already exists");

			if (index < 0 || index > _items.Count)
				throw new ArgumentOutOfRangeException(nameof(index));

			// Create a new dictionary with the item inserted at the specified position
			var newItems = new Dictionary<string, MultiArcItem>();
			int currentIndex = 0;

			foreach (var pair in _items)
			{
				if (currentIndex == index)
					newItems.Add(name, item);

				newItems.Add(pair.Key, pair.Value);
				currentIndex++;
			}

			// If inserting at the end
			if (index == _items.Count)
				newItems.Add(name, item);

			_items.Clear();
			foreach (var pair in newItems)
			{
				_items.Add(pair.Key, pair.Value);
			}

			return index;
		}

		/// <summary>
		/// Delete an item at the specified index
		/// </summary>
		public void Delete(int index)
		{
			if (index < 0 || index >= _items.Count)
				throw new IndexOutOfRangeException();

			string key = _items.Keys.ElementAt(index);
			_items.Remove(key);
		}

		/// <summary>
		/// Create a clone of this MultiArcList
		/// </summary>
		public MultiArcList Clone()
		{
			var result = new MultiArcList();

			foreach (var pair in _items)
			{
				result.Add(pair.Key, pair.Value.Clone());
			}

			return result;
		}

		// CRC32 table for signature computation
		private static readonly uint[] Crc32Table = {
			0x00000000, 0x77073096, 0xEE0E612C, 0x990951BA, 0x076DC419, 0x706AF48F, 0xE963A535, 0x9E6495A3,
			0x0EDB8832, 0x79DCB8A4, 0xE0D5E91E, 0x97D2D988, 0x09B64C2B, 0x7EB17CBD, 0xE7B82D07, 0x90BF1D91,
			0x1DB71064, 0x6AB020F2, 0xF3B97148, 0x84BE41DE, 0x1ADAD47D, 0x6DDDE4EB, 0xF4D4B551, 0x83D385C7,
			0x136C9856, 0x646BA8C0, 0xFD62F97A, 0x8A65C9EC, 0x14015C4F, 0x63066CD9, 0xFA0F3D63, 0x8D080DF5,
			0x3B6E20C8, 0x4C69105E, 0xD56041E4, 0xA2677172, 0x3C03E4D1, 0x4B04D447, 0xD20D85FD, 0xA50AB56B,
			0x35B5A8FA, 0x42B2986C, 0xDBBBC9D6, 0xACBCF940, 0x32D86CE3, 0x45DF5C75, 0xDCD60DCF, 0xABD13D59,
			0x26D930AC, 0x51DE003A, 0xC8D75180, 0xBFD06116, 0x21B4F4B5, 0x56B3C423, 0xCFBA9599, 0xB8BDA50F,
			0x2802B89E, 0x5F058808, 0xC60CD9B2, 0xB10BE924, 0x2F6F7C87, 0x58684C11, 0xC1611DAB, 0xB6662D3D,
			0x76DC4190, 0x01DB7106, 0x98D220BC, 0xEFD5102A, 0x71B18589, 0x06B6B51F, 0x9FBFE4A5, 0xE8B8D433,
			0x7807C9A2, 0x0F00F934, 0x9609A88E, 0xE10E9818, 0x7F6A0DBB, 0x086D3D2D, 0x91646C97, 0xE6635C01,
			0x6B6B51F4, 0x1C6C6162, 0x856530D8, 0xF262004E, 0x6C0695ED, 0x1B01A57B, 0x8208F4C1, 0xF50FC457,
			0x65B0D9C6, 0x12B7E950, 0x8BBEB8EA, 0xFCB9887C, 0x62DD1DDF, 0x15DA2D49, 0x8CD37CF3, 0xFBD44C65,
			0x4DB26158, 0x3AB551CE, 0xA3BC0074, 0xD4BB30E2, 0x4ADFA541, 0x3DD895D7, 0xA4D1C46D, 0xD3D6F4FB,
			0x4369E96A, 0x346ED9FC, 0xAD678846, 0xDA60B8D0, 0x44042D73, 0x33031DE5, 0xAA0A4C5F, 0xDD0D7CC9,
			0x5005713C, 0x270241AA, 0xBE0B1010, 0xC90C2086, 0x5768B525, 0x206F85B3, 0xB966D409, 0xCE61E49F,
			0x5EDEF90E, 0x29D9C998, 0xB0D09822, 0xC7D7A8B4, 0x59B33D17, 0x2EB40D81, 0xB7BD5C3B, 0xC0BA6CAD,
			0xEDB88320, 0x9ABFB3B6, 0x03B6E20C, 0x74B1D29A, 0xEAD54739, 0x9DD277AF, 0x04DB2615, 0x73DC1683,
			0xE3630B12, 0x94643B84, 0x0D6D6A3E, 0x7A6A5AA8, 0xE40ECF0B, 0x9309FF9D, 0x0A00AE27, 0x7D079EB1,
			0xF00F9344, 0x8708A3D2, 0x1E01F268, 0x6906C2FE, 0xF762575D, 0x806567CB, 0x196C3671, 0x6E6B06E7,
			0xFED41B76, 0x89D32BE0, 0x10DA7A5A, 0x67DD4ACC, 0xF9B9DF6F, 0x8EBEEFF9, 0x17B7BE43, 0x60B08ED5,
			0xD6D6A3E8, 0xA1D1937E, 0x38D8C2C4, 0x4FDFF252, 0xD1BB67F1, 0xA6BC5767, 0x3FB506DD, 0x48B2364B,
			0xD80D2BDA, 0xAF0A1B4C, 0x36034AF6, 0x41047A60, 0xDF60EFC3, 0xA867DF55, 0x316E8EEF, 0x4669BE79,
			0xCB61B38C, 0xBC66831A, 0x256FD2A0, 0x5268E236, 0xCC0C7795, 0xBB0B4703, 0x220216B9, 0x5505262F,
			0xC5BA3BBE, 0xB2BD0B28, 0x2BB45A92, 0x5CB36A04, 0xC2D7FFA7, 0xB5D0CF31, 0x2CD99E8B, 0x5BDEAE1D,
			0x9B64C2B0, 0xEC63F226, 0x756AA39C, 0x026D930A, 0x9C0906A9, 0xEB0E363F, 0x72076785, 0x05005713,
			0x95BF4A82, 0xE2B87A14, 0x7BB12BAE, 0x0CB61B38, 0x92D28E9B, 0xE5D5BE0D, 0x7CDCEFB7, 0x0BDBDF21,
			0x86D3D2D4, 0xF1D4E242, 0x68DDB3F8, 0x1FDA836E, 0x81BE16CD, 0xF6B9265B, 0x6FB077E1, 0x18B74777,
			0x88085AE6, 0xFF0F6A70, 0x66063BCA, 0x11010B5C, 0x8F659EFF, 0xF862AE69, 0x616BFFD3, 0x166CCF45,
			0xA00AE278, 0xD70DD2EE, 0x4E048354, 0x3903B3C2, 0xA7672661, 0xD06016F7, 0x4969474D, 0x3E6E77DB,
			0xAED16A4A, 0xD9D65ADC, 0x40DF0B66, 0x37D83BF0, 0xA9BCAE53, 0xDEBB9EC5, 0x47B2CF7F, 0x30B5FFE9,
			0xBDBDF21C, 0xCABAC28A, 0x53B39330, 0x24B4A3A6, 0xBAD03605, 0xCDD70693, 0x54DE5729, 0x23D967BF,
			0xB3667A2E, 0xC4614AB8, 0x5D681B02, 0x2A6F2B94, 0xB40BBE37, 0xC30C8EA1, 0x5A05DF1B, 0x2D02EF8D
		};

		/// <summary>
		/// Helper method to add a string to CRC calculation
		/// </summary>
		private void AddStringToCrc(ref uint crc, string value)
		{
			if (string.IsNullOrEmpty(value))
				return;

			byte[] bytes = Encoding.UTF8.GetBytes(value);
			foreach (byte b in bytes)
				crc = ((crc >> 8) & 0x00FFFFFF) ^ Crc32Table[(crc ^ b) & 0xFF];
		}

		/// <summary>
		/// Compute a signature (CRC32) of the configuration
		/// </summary>
		public uint ComputeSignature(uint seed = 0)
		{
			uint crc = seed;

			foreach (var pair in _items)
			{
				// Add name to CRC
				AddStringToCrc(ref crc, pair.Key);

				MultiArcItem item = pair.Value;

				// Add properties to CRC
				AddStringToCrc(ref crc, item.Description);
				AddStringToCrc(ref crc, item.Archiver);
				AddStringToCrc(ref crc, item.Extension);
				AddStringToCrc(ref crc, item.List);
				AddStringToCrc(ref crc, item.Start);
				AddStringToCrc(ref crc, item.End);

				// Add format strings
				foreach (string format in item.Format)
					AddStringToCrc(ref crc, format);

				AddStringToCrc(ref crc, item.Extract);
				AddStringToCrc(ref crc, item.Add);
				AddStringToCrc(ref crc, item.Delete);
				AddStringToCrc(ref crc, item.Test);
				AddStringToCrc(ref crc, item.ExtractWithoutPath);
				AddStringToCrc(ref crc, item.AddSelfExtract);
				AddStringToCrc(ref crc, item.PasswordQuery);
				AddStringToCrc(ref crc, item.ID);
				AddStringToCrc(ref crc, item.IDPos);
				AddStringToCrc(ref crc, item.IDSeekRange);

				// Add flags and other numeric values
				byte[] flagsBytes = BitConverter.GetBytes((int)item.Flags);
				foreach (byte b in flagsBytes)
					crc = ((crc >> 8) & 0x00FFFFFF) ^ Crc32Table[(crc ^ b) & 0xFF];

				byte[] formModeBytes = BitConverter.GetBytes(item.FormMode);
				foreach (byte b in formModeBytes)
					crc = ((crc >> 8) & 0x00FFFFFF) ^ Crc32Table[(crc ^ b) & 0xFF];

				byte[] enabledBytes = BitConverter.GetBytes(item.Enabled);
				foreach (byte b in enabledBytes)
					crc = ((crc >> 8) & 0x00FFFFFF) ^ Crc32Table[(crc ^ b) & 0xFF];

				byte[] outputBytes = BitConverter.GetBytes(item.Output);
				foreach (byte b in outputBytes)
					crc = ((crc >> 8) & 0x00FFFFFF) ^ Crc32Table[(crc ^ b) & 0xFF];

				byte[] debugBytes = BitConverter.GetBytes(item.Debug);
				foreach (byte b in debugBytes)
					crc = ((crc >> 8) & 0x00FFFFFF) ^ Crc32Table[(crc ^ b) & 0xFF];
			}

			return crc;
		}
	}
}