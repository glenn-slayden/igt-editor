using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Markup;

using alib.Debugging;
using alib.Enumerable;

namespace xigt2
{
	public class dps
	{
		//public static DependencyProperty NameProperty =
		//	DependencyProperty.RegisterAttached("Name", typeof(String), typeof(dps), new PropertyMetadata(default(String)));

		public static DependencyProperty FilenameProperty =
			DependencyProperty.RegisterAttached("Filename", typeof(String), typeof(dps), new PropertyMetadata(String.Empty));

		public static DependencyProperty DelimiterProperty =
			DependencyProperty.RegisterAttached("Delimiter", typeof(String), typeof(dps), new PropertyMetadata(" "));

		public static DependencyProperty IsVisibleProperty =
			DependencyProperty.RegisterAttached("IsVisible", typeof(bool), typeof(dps), new PropertyMetadata(true));

		public static DependencyProperty IsReadOnlyProperty =
			DependencyProperty.RegisterAttached("IsReadOnly", typeof(bool), typeof(dps), new PropertyMetadata(false));

		public static DependencyProperty TextProperty =
			DependencyProperty.RegisterAttached("Text", typeof(String), typeof(dps), new PropertyMetadata("", null, (d, _s) =>
				{
					var txt = (String)_s;
					if (txt == null)
						return txt;

					Char[] rgch = null;
					for (int i = 0; i < txt.Length; i++)
					{
						switch (txt[i])
						{
							case '\u0000':
							case '\u0001':
							case '\u0002':
							case '\u0003':
							case '\u0004':
							case '\u0005':
							case '\u0006':
							case '\u0007':
							case '\u0008':
							case '\u000C':
								if (rgch == null)
									rgch = txt.ToCharArray();
								rgch[i] = ' ';
								break;
						}
					}
					return rgch != null ? new String(rgch) : txt;
				}));

		public static DependencyProperty StatusProperty =
			DependencyProperty.RegisterAttached("Status", typeof(String), typeof(dps));

		public static DependencyProperty DocIdProperty =
			DependencyProperty.RegisterAttached("DocId", typeof(String), typeof(dps));

		public static DependencyProperty DocInfoProperty =
			DependencyProperty.RegisterAttached("DocInfo", typeof(String), typeof(dps));

		public static DependencyProperty LanguageProperty =
			DependencyProperty.RegisterAttached("Language", typeof(String), typeof(dps), new PropertyMetadata(default(String)));

		public static DependencyProperty FromLineProperty =
			DependencyProperty.RegisterAttached("FromLine", typeof(int), typeof(dps), new PropertyMetadata(-1));

		public static DependencyProperty ToLineProperty =
			DependencyProperty.RegisterAttached("ToLine", typeof(int), typeof(dps), new PropertyMetadata(-1));

		public static DependencyProperty FromCharProperty =
			DependencyProperty.RegisterAttached("FromChar", typeof(int), typeof(dps), new PropertyMetadata(-1));

		public static DependencyProperty ToCharProperty =
			DependencyProperty.RegisterAttached("ToChar", typeof(int), typeof(dps), new PropertyMetadata(-1));

		public static DependencyProperty TierTypeProperty =
			DependencyProperty.RegisterAttached("TierType", typeof(String), typeof(dps), new PropertyMetadata(default(String)));

		public static DependencyProperty TiersHostProperty =
			DependencyProperty.RegisterAttached("TiersHost", typeof(ITiers), typeof(dps), new PropertyMetadata(default(ITiers)));

		public static DependencyProperty PartsHostProperty =
			DependencyProperty.RegisterAttached("PartsHost", typeof(IParts), typeof(dps), new PropertyMetadata(default(IParts)));

		public static DependencyProperty IgtCorpusProperty =
		   DependencyProperty.RegisterAttached("IgtCorpus", typeof(IgtCorpus), typeof(dps), new PropertyMetadata(default(IgtCorpus)));

		public static DependencyProperty SourceTierProperty =
		   DependencyProperty.RegisterAttached("SourceTier", typeof(TextTier), typeof(dps), new PropertyMetadata(default(TextTier)));

		public static DependencyProperty TargetTierProperty =
		   DependencyProperty.RegisterAttached("TargetTier", typeof(tier_base), typeof(dps), new PropertyMetadata(default(tier_base)));

		public static DependencyProperty SourceProperty =
			DependencyProperty.RegisterAttached("Source", typeof(IPart), typeof(dps), new PropertyMetadata(default(IPart)));

		public static DependencyProperty TargetProperty =
		   DependencyProperty.RegisterAttached("Target", typeof(IPart), typeof(dps), new PropertyMetadata(default(IPart)));

		public static DependencyPropertyKey TiersPropertyKey =
			DependencyProperty.RegisterAttachedReadOnly("Tiers", typeof(TierSet), typeof(dps), new PropertyMetadata(default(TierSet)));

		public static DependencyProperty TiersProperty { get { return TiersPropertyKey.DependencyProperty; } }
	};

	[RuntimeNameProperty("Name")]
	public abstract class name_dp_base : DependencyObject, IHostedItem
	{
		static name_dp_base()
		{
			FrameworkElement.NameProperty.AddOwner(typeof(name_dp_base));
		}

		public name_dp_base()
		{
			this.Name = "_" + Guid.NewGuid().ToString("N");
		}

		public String Name
		{
			get { return (String)this.GetValue(FrameworkElement.NameProperty); }
			set { this.SetValue(FrameworkElement.NameProperty, value); }
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public abstract IItems Host { get; set; }

		public int OuterIndex { get { return Host.GetList().IndexOf(this); } }
	};

	public abstract class text_dp_base : name_dp_base, ITextItem
	{
		static text_dp_base()
		{
			dps.TextProperty.AddOwner(typeof(text_dp_base));
		}
		[DefaultValue("")]
		public String Text
		{
			get { return (String)this.GetValue(dps.TextProperty); }
			set { this.SetValue(dps.TextProperty, value); }
		}
	}
}
