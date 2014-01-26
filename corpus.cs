using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Text;
using System.Windows.Markup;
using System.Xaml;
using System.Xml;

using alib;
using alib.Debugging;
using alib.Enumerable;

namespace xigt2
{
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public class IgtCorpora : CorpusSet, Iitems<IgtCorpus>
	{
		public IgtCorpora()
			: base(null)
		{
		}

		public String Name { get; set; }

		public new Iset<IgtCorpus> Items { get { return this; } }
		public IList GetList() { return this; }
		public bool ContainsListCollection { get { return true; } }

		public Object GetValue(DependencyProperty dp) { throw not.impl; }

		public void SetValue(DependencyProperty dp, Object value) { throw not.impl; }

		public bool ContainsFile(String filename)
		{
			var fn_check = Path.GetFileNameWithoutExtension(filename);
			return this.Any(x => String.Equals(x.FilenameWithoutExtension, fn_check, StringComparison.OrdinalIgnoreCase));
		}
	};


	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public class IgtCorpus : name_dp_base, Iitems<Igt>, IHostedItem
	{
		static IgtCorpus()
		{
			dps.FilenameProperty.AddOwner(typeof(IgtCorpus), new PropertyMetadata(default(String), (o, e) =>
				{
					var _this = (IgtCorpus)o;
					_this.ShortFilename =
						Path.GetDirectoryName(_this.Filename).Split(Path.DirectorySeparatorChar).Last() + "/" +
						Path.GetFileName(_this.Filename);
				}));
			dps.DelimiterProperty.AddOwner(typeof(IgtCorpus));
		}

		public IgtCorpus()
		{
			this.Igts = new IgtsSet(this);
		}

		readonly IgtsSet Igts;

		public Igt this[int index]
		{
			get { return Igts[index]; }
			set { Igts[index] = value; }
		}

		public Iset<Igt> Items { get { return Igts; } }

		public int Count { get { return Igts.Count; } }

		public IEnumerator<Igt> GetEnumerator() { return Igts.GetEnumerator(); }

		IEnumerator IEnumerable.GetEnumerator() { return Igts.GetEnumerator(); }

		bool IListSource.ContainsListCollection { get { return true; } }
		IList IListSource.GetList() { return Igts; }

		Iitems<IgtCorpus> host;
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public override IItems Host
		{
			get { return host; }
			set { host = (Iitems<IgtCorpus>)value; }
		}

		public String Filename
		{
			get { return (String)GetValue(dps.FilenameProperty); }
			set { SetValue(dps.FilenameProperty, value); }
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public String Delimiter
		{
			get { return (String)GetValue(dps.DelimiterProperty); }
			set { SetValue(dps.DelimiterProperty, value); }
		}


		public String ShortFilename
		{
			get { return (String)GetValue(ShortFilenameProperty); }
			set { SetValue(ShortFilenameProperty, value); }
		}

		public static readonly DependencyProperty ShortFilenameProperty =
			DependencyProperty.Register("ShortFilename", typeof(String), typeof(IgtCorpus), new PropertyMetadata(default(String)));


		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public String FilenameWithoutExtension
		{
			get { return Path.GetFileNameWithoutExtension(this.Filename); }
		}

		public void Save(String xigtdir)
		{
			var fn = Path.Combine(xigtdir, Path.GetFileNameWithoutExtension(Filename) + ".xml");

			this.Filename = fn;

			using (var sw = XmlWriter.Create(fn, new XmlWriterSettings
			{
				Indent = true,
				NewLineOnAttributes = true,
				NamespaceHandling = NamespaceHandling.OmitDuplicates,
				OmitXmlDeclaration = true,
				Encoding = Encoding.UTF8,
				CloseOutput = true,
			}))
			{
				using (var xr = new XamlObjectReader(this, App.ctx))
				using (var xw = new XamlXmlWriter(sw, App.ctx))
				{
					XamlServices.Transform(xr, xw);
				}
				sw.Close();
			}
		}

		public static IgtCorpus LoadXaml(String fn)
		{
			IgtCorpus ret;
			using (var xr = new XamlXmlReader(fn, App.ctx))
			using (var xw = new XamlObjectWriter(App.ctx))
			{
				XamlServices.Transform(xr, xw);

				ret = (IgtCorpus)xw.Result;
			}
			return ret;
		}

	};
}