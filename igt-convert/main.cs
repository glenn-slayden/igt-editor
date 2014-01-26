using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace xigt2
{
	public class IgtConvert
	{
		const String s_usage = @"Usage:

{0} [input-dir] [output-dir]

Converts each ODIN text format IGT file (*.txt) in the input directory to a 
XAML IGT file (*.xml) in the output directory. Existing files with a 
conflicting name in the output directory are overwritten.

ODIN text files are UTF-8 files containing zero or more IGT instances which
adhere to the (e.g.) following format. Instances must be separated by a 
blank line. Line feed format can be either Unix or DOS/Windows.

doc_id=807 2764 2766 L G T
language: spanish (spa) + english (eng)
line=2764 tag=L:         (77a) *Juan está eat-iendo
line=2765 tag=G:               Juan be/1Ss eat-DUR
line=2766 tag=T:               `Juan is eating.'

The output file format is a XAML serialization of the object graph for the
in-memory object model of the WPF IGT editor.
";

		static void Main(String[] args)
		{
			xigt_config = config.XigtConfig.Load();

			if (args.Length != 2)
			{
				var s_app = typeof(IgtConvert).Assembly.Location;
				if (s_app.Contains(' '))
					s_app = "\"" + s_app + "\"";
				Console.Error.WriteLine(s_usage, s_app);

				return;
			}
			convert_igt_dir(Path.GetFullPath(args[0]), args[1]);
		}

		public static config.XigtConfig xigt_config;


		public static int convert_igt_dir(String dirname, String xigtdir)
		{
			if (!Directory.Exists(dirname))
			{
				Console.WriteLine("The directory:\r\r{0}\r\rwas not found.", dirname);
				return 1;
			}

			if (!Directory.Exists(xigtdir))
			{
				Directory.CreateDirectory(xigtdir);
			}

			foreach (var filename in Directory.GetFiles(dirname, "*.txt"))
			{
				var fn = Path.GetFullPath(filename);
				
				Console.Error.WriteLine(fn);

				var corpus = xigt_config.LoadTxtFile(fn);

				corpus.Save(xigtdir);
			}

			return 0;
		}
	};
}
