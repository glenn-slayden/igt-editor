using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using alib.Bits;
using alib.Debugging;

namespace alib.file
{
	using String = System.String;

	public static class _file_ext
	{
		static _file_ext()
		{
			UTF8NoBom = new UTF8Encoding(false);
		}

		public static readonly UTF8Encoding UTF8NoBom;

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static String FileVersion(String fn)
		{
			try
			{
				return System.Diagnostics.FileVersionInfo.GetVersionInfo(fn).ProductVersion;
			}
			catch (FileNotFoundException)
			{
				return String.Empty;
			}
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static DateTime BuildTime(this System.Reflection.Assembly a)
		{
			return GetLinkerDateTime(a.Location);
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static unsafe DateTime GetLinkerDateTime(String exe_file)
		{
			byte[] b = new byte[2048];
			using (Stream s = new FileStream(exe_file, FileMode.Open, FileAccess.Read))
				s.Read(b, 0, b.Length);

			fixed (byte* pb = b)
			{
				int* pi = (int*)pb;
				return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
							.AddSeconds(pi[(pi[15] >> 2) + 2])
							.ToLocalTime();
			}
		}

		static string[] sizes = { "B", "KB", "MB", "GB", "TB" };
		public static String HumanReadableSize(String file)
		{
			double len = new FileInfo(file).Length;
			int order = 0;
			while (len >= 1024 && order + 1 < sizes.Length) {
				order++;
				len = len/1024;
			}
			// Adjust the format string to your preferences. For example "{0:0.#}{1}" would
			// show a single decimal place, and no space.
			return String.Format("{0:0.##} {1}", len, sizes[order]);
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// Case-insensitive search for a file
		/// </summary>
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static String CaseInsensitiveFilename(String s_dir, String file)
		{
			return Directory.EnumerateFiles(s_dir, "*", SearchOption.TopDirectoryOnly)
											.FirstOrDefault(f => Path.GetFileName(f).ToLower() == file);
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static IEnumerable<FileInfo> EnumerateFiles(String s_dir, String search, SearchOption opt = SearchOption.TopDirectoryOnly)
		{
			return Directory.EnumerateFiles(s_dir, "*", SearchOption.TopDirectoryOnly).Select(s => new FileInfo(s));
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static String Read(this FileInfo fi)
		{
			int c = (int)fi.Length;
			if (c == 0)
				return String.Empty;
			byte[] rgb = new byte[fi.Length];
			using (FileStream fs = fi.Open(FileMode.Open, FileAccess.Read, FileShare.Read))
				c = fs.Read(rgb, 0, rgb.Length);

			if (c >= 4 && rgb[0] == 0 && rgb[1] == 0 && rgb[2] == 0xfe && rgb[3] == 0xff)	// UTF-32
				return Encoding.UTF32.GetString(rgb, 4, c - 4);

			if (c >= 3)
				if (rgb[0] == 0xef && rgb[1] == 0xbb && rgb[2] == 0xbf)			// UTF-8
					return Encoding.UTF8.GetString(rgb, 3, c - 3);
				else if (rgb[0] == 0x2b && rgb[1] == 0x2f && rgb[2] == 0x76)	// UTF-7
					return Encoding.UTF7.GetString(rgb, 3, c - 3);

			if (c >= 2)
				if (rgb[0] == 0xFE && rgb[1] == 0xFF) 					// Unicode (Big-Endian)
					return Encoding.BigEndianUnicode.GetString(rgb, 2, c - 2);
				else if (rgb[0] == 0xFF && rgb[1] == 0xFE) 				// Unicode (Little-Endian)
					return Encoding.Unicode.GetString(rgb, 2, c - 2);

			return Encoding.UTF8.GetString(rgb);
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static String Read(String szfile /* , out int i_offs*/)
		{
#if true
			FileInfo fi = new FileInfo(szfile);
			if (!fi.Exists)
				throw new FileNotFoundException("File not found.", szfile);

			return _file_ext.Read(fi);
#else
			String ret = String.Empty;
			i_offs = 0;
			try
			{
				using (FileStream fs = System.IO.File.Open(szfile, FileMode.Open, FileAccess.Read, FileShare.Read))
				{
					// ?? manually detect BOM, because can't seem to set default encoding to 874 using 'detect from BOM' feature of StreamReader constructor 
					byte[] BOM = new byte[4];
					fs.Read(BOM, 0, 4);
					fs.Seek(0, SeekOrigin.Begin);

					if ((BOM[0] == 0xef && BOM[1] == 0xbb && BOM[2] == 0xbf) ||				// UTF-8
						(BOM[0] == 0xFE && BOM[1] == 0xFF) ||								// Unicode (Big-Endian)
						(BOM[0] == 0xFF && BOM[1] == 0xFE) ||								// Unicode (Little-Endian)
						(BOM[0] == 0 && BOM[1] == 0 && BOM[2] == 0xfe && BOM[3] == 0xff) ||	// UTF-32
						(BOM[0] == 0x2b && BOM[1] == 0x2f && BOM[2] == 0x76))				// UTF-7
					{
						using (StreamReader sr = new StreamReader(fs, true))	// (auto-detect BOM)
							ret = sr.ReadToEnd();
						i_offs = 2;
					}
					else
						using (StreamReader sr = new StreamReader(fs /*, Encoding.Default */))
							ret = sr.ReadToEnd();
				}
			}
			catch (FileNotFoundException fnf)
			{
				Console.WriteLine("File not found: {0}", fnf.FileName);
				Environment.Exit(1);
			}
			return ret;
#endif
		}
	};
}
