using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

using alib.Debugging;
using alib.Enumerable;

namespace xie
{
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	[DebuggerDisplay("{ToString(),nq}")]
	public abstract class Part : text_dp_base, IPart
	{
		static Part()
		{
			dps.PartsHostProperty.AddOwner(typeof(Part));
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public IParts PartsHost
		{
			get { return (IParts)this.GetValue(dps.PartsHostProperty); }
			set { this.SetValue(dps.PartsHostProperty, value); }
		}

		public sealed override IItems Host
		{
			get { return PartsHost; }
			set { PartsHost = (IParts)value; }
		}

		public override string ToString()
		{
			return String.Format("{0} {1}", this.GetType().Name, this.Text);
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	[DebuggerDisplay("{ToString(),nq}")]
	public sealed class TextPart : Part, IEditText
	{
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	[DebuggerDisplay("{ToString(),nq}")]
	public sealed class SegPart : Part
	{
		static SegPart()
		{
			DependencyProperty dp;
			PropertyMetadata dpm;

			dpm = (dp = TextProperty).DefaultMetadata;
			dp.AddOwner(typeof(SegPart), new PropertyMetadata
			{
				DefaultValue = dpm.DefaultValue,
				PropertyChangedCallback = dpm.PropertyChangedCallback,
				CoerceValueCallback = (d, o) => ((SegPart)d).coerce_text((String)o),
			});

			dpm = (dp = dps.FromCharProperty).DefaultMetadata;
			dp.AddOwner(typeof(SegPart), new PropertyMetadata
			{
				DefaultValue = dpm.DefaultValue,
				PropertyChangedCallback = (o, e) => ((SegPart)o).CoerceValue(TextProperty),
				CoerceValueCallback = dpm.CoerceValueCallback,
			});

			dpm = (dp = dps.ToCharProperty).DefaultMetadata;
			dp.AddOwner(typeof(SegPart), new PropertyMetadata
			{
				DefaultValue = dpm.DefaultValue,
				PropertyChangedCallback = (o, e) => ((SegPart)o).CoerceValue(TextProperty),
				CoerceValueCallback = dpm.CoerceValueCallback,
			});

			dpm = (dp = dps.SourceTierProperty).DefaultMetadata;
			dp.AddOwner(typeof(SegPart), new PropertyMetadata
			{
				DefaultValue = dpm.DefaultValue,
				PropertyChangedCallback = (o, e) => ((SegPart)o).CoerceValue(TextProperty),
				CoerceValueCallback = dpm.CoerceValueCallback,
			});
		}

		String coerce_text(String s)
		{
			int i_from, i_to, cch;
			if ((i_from = FromChar) < 0 || (i_to = ToChar) < 0 || (cch = i_to - i_from) < 0)
				return null;

			TextTier st;
			String st_txt;

			if (cch == 0 || (st = SourceTier) == null || (st_txt = st.Text) == null || i_to > st_txt.Length)
				return String.Empty;

			return st_txt.Substring(i_from, cch);
		}

		public TextTier SourceTier
		{
			get { return (TextTier)GetValue(dps.SourceTierProperty); }
			set { SetValue(dps.SourceTierProperty, value); }
		}

		public int FromChar
		{
			get { return (int)GetValue(dps.FromCharProperty); }
			set { SetValue(dps.FromCharProperty, value); }
		}

		public int ToChar
		{
			get { return (int)GetValue(dps.ToCharProperty); }
			set { SetValue(dps.ToCharProperty, value); }
		}

		public override String ToString()
		{
			return String.Format("<{0},{1}> [{2}]", FromChar, ToChar, Text);
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	[DebuggerDisplay("{ToString(),nq}")]
	public class MergePart : Part, IParts
	{
		static DependencyPropertyKey PartsPropertyKey;
		public static DependencyProperty PartsProperty { get { return PartsPropertyKey.DependencyProperty; } }

		static MergePart()
		{
			TextProperty.AddOwner(typeof(MergePart), new PropertyMetadata(default(String),
				(o, e) => { },
				(d, o) => ((MergePart)d).coerce_text((String)o)));


			PartsPropertyKey = DependencyProperty.RegisterReadOnly("Parts", typeof(Iset<IPart>), typeof(MergePart),
				new PropertyMetadata(default(Iset<IPart>),
					(o, e) =>
					{
						var _this = (MergePart)o;
						_this.CoerceValue(TextProperty);
					}));
		}

		String coerce_text(String s)
		{
			return Parts.Select(x => x.Text).StringJoin(String.Empty);
		}

		public Iset<IPart> Parts { get { return (Iset<IPart>)GetValue(PartsProperty); } }
		Iset<IPart> Iitems<IPart>.Items { get { return this.Parts; } }
		IList IListSource.GetList() { return this.Parts; }
		bool IListSource.ContainsListCollection { get { return false; } }

		public IPart this[int index]
		{
			get { return Parts[index]; }
		}

		public int Count { get { return Parts.Count; } }

		public IEnumerator<IPart> GetEnumerator() { return Parts.GetEnumerator(); }

		IEnumerator IEnumerable.GetEnumerator() { return Parts.GetEnumerator(); }

		public MergePart()
		{
			var ps = new OwnerPartsSet(this);
			ps.CollectionChanged += (o, e) => CoerceValue(TextProperty);
			SetValue(PartsPropertyKey, ps);
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	[DebuggerDisplay("{ToString(),nq}")]
	public class GroupPart : Part, IParts
	{
		static DependencyPropertyKey PartsPropertyKey;
		public static DependencyProperty PartsProperty { get { return PartsPropertyKey.DependencyProperty; } }

		static GroupPart()
		{
			TextProperty.AddOwner(typeof(GroupPart), new PropertyMetadata(default(String),
				(o, e) => { },
				(d, o) => ((GroupPart)d).coerce_text((String)o)));


			PartsPropertyKey = DependencyProperty.RegisterReadOnly("Parts", typeof(Iset<IPart>), typeof(GroupPart),
				new PropertyMetadata(default(Iset<IPart>),
					(o, e) =>
					{
						var _this = (GroupPart)o;
						_this.CoerceValue(TextProperty);
					}));
		}

		String coerce_text(String s)
		{
			return Parts.Select(x => x.Text).StringJoin(" "/*Igt.IgtCorpus.Delimiter*/);
		}

		public Iset<IPart> Parts { get { return (Iset<IPart>)GetValue(PartsProperty); } }
		Iset<IPart> Iitems<IPart>.Items { get { return this.Parts; } }
		IList IListSource.GetList() { return this.Parts; }
		bool IListSource.ContainsListCollection { get { return false; } }

		public IPart this[int index]
		{
			get { return Parts[index]; }
		}

		public int Count { get { return Parts.Count; } }

		public IEnumerator<IPart> GetEnumerator() { return Parts.GetEnumerator(); }

		IEnumerator IEnumerable.GetEnumerator() { return Parts.GetEnumerator(); }

		public GroupPart()
		{
			SetValue(PartsPropertyKey, new OwnerPartsSet(this));
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// Dependency, or copy of a part to a different tier. The text of the CopyPart is inherited from the copied part.
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	[DebuggerDisplay("{ToString(),nq}")]
	public class CopyPart : Part
	{
		static CopyPart()
		{
			TextProperty.AddOwner(typeof(CopyPart), new PropertyMetadata(default(String),
				null,//(o, e) => ((CopyPart)o).SourcePart.Text = (String)e.NewValue,
				(d, o) =>
				{
					IPart src;
					return !String.IsNullOrEmpty((String)o) || (src = ((CopyPart)d).SourcePart) == null ? o : src.Text;
				}));

			dps.SourcePartProperty.AddOwner(typeof(CopyPart), new PropertyMetadata(default(IPart),
				(o, e) =>
				{
					var _this = (CopyPart)o;
					_this.CoerceValue(TextProperty);
				}));
		}

		public IPart SourcePart
		{
			get { return (IPart)GetValue(dps.SourcePartProperty); }
			set { SetValue(dps.SourcePartProperty, value); }
		}

		//	public Dependency DependsOn;
		//	public bool Equals(Dependency other)
		//	{
		//		if (!base.Equals(other))
		//			return false;
		//		if ((Object)this.DependsOn == (Object)other.DependsOn)
		//			return true;
		//		if ((Object)this.DependsOn == null)
		//			return false;
		//		return this.DependsOn.Equals(other.DependsOn);
		//	}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// Alignment: a CopyPart/Dependency which includes a reference to a 'Target', which is another part from some other 
	/// tier. The text of this AlignPart itself is inherited from the target, but the source might have different text,
	/// which can be displayed in some modes.
	[DebuggerDisplay("{ToString(),nq}")]
	public class AlignPart : CopyPart //, IParts
	{
		static DependencyPropertyKey AlignedPartsPropertyKey;
		public static DependencyProperty AlignedPartsProperty { get { return AlignedPartsPropertyKey.DependencyProperty; } }

		static AlignPart()
		{
			AlignedPartsPropertyKey = DependencyProperty.RegisterReadOnly("AlignedParts", typeof(PartRefSet), typeof(AlignPart),
				new PropertyMetadata(default(PartRefSet),
				(o, e) =>
				{
					var _this = (AlignPart)o;
					//_this.CoerceValue(dps.TextProperty);
				}));
		}

		public PartRefSet AlignedParts
		{
			get { return (PartRefSet)GetValue(AlignedPartsPropertyKey.DependencyProperty); }
		}

		//public Iset<IPart> AlignedParts { get { return (Iset<IPart>)GetValue(AlignedPartsProperty); } }
		//Iset<IPart> Iitems<IPart>.Items { get { return this.AlignedParts; } }
		//IList IListSource.GetList() { return this.AlignedParts; }
		//bool IListSource.ContainsListCollection { get { return false; } }

		//public IPart this[int index]
		//{
		//	get { return AlignedParts[index]; }
		//	set { AlignedParts[index] = value; }
		//}

		//public int Count { get { return AlignedParts.Count; } }

		//public IEnumerator<IPart> GetEnumerator() { return AlignedParts.GetEnumerator(); }

		//IEnumerator IEnumerable.GetEnumerator() { return AlignedParts.GetEnumerator(); }

		public AlignPart()
		{
			SetValue(AlignedPartsPropertyKey, new PartRefSet());
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	//[DebuggerDisplay("{ToString(),nq}")]
	//public class EditAlignPart : AlignPart, IEditText
	//{
	//	static EditAlignPart()
	//	{
	//		dps.TextProperty.AddOwner(typeof(EditAlignPart), new PropertyMetadata(default(String),
	//			(o, e) => ((EditAlignPart)o).Target.Text = (String)e.NewValue,
	//			(d, o) =>
	//			{
	//				var _this = (EditAlignPart)d;
	//				if (_this.Target == null)
	//					return o;
	//				return _this.Target.Text;
	//			}));

	//		dps.TargetProperty.AddOwner(typeof(EditAlignPart), new PropertyMetadata(default(IPart),
	//			(o, e) =>
	//			{
	//				var _this = (EditAlignPart)o;
	//				_this.CoerceValue(dps.TextProperty);
	//			}));
	//	}
	//};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	///
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	[DebuggerDisplay("{ToString(),nq}")]
	public class TagPart : CopyPart
	{
		static TagPart()
		{
			TextProperty.AddOwner(typeof(TagPart), new PropertyMetadata("unk"));
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	///
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	[DebuggerDisplay("{ToString(),nq}")]
	public class DepPart : CopyPart//, IParts
	{
#if false
		static DependencyPropertyKey PartsPropertyKey;
		public static DependencyProperty PartsProperty { get { return PartsPropertyKey.DependencyProperty; } }
#endif
		static DepPart()
		{
#if false
			PartsPropertyKey = DependencyProperty.RegisterReadOnly("Parts", typeof(Iset<IPart>), typeof(DepPart),
				new PropertyMetadata(default(Iset<IPart>)
				//,(o, e) =>
				//{
				//	var _this = (DepPart)o;
				//	_this.CoerceValue(dps.TextProperty);
				//}
					));
#endif
			Selector.IsSelectedProperty.AddOwner(typeof(DepPart), new FrameworkPropertyMetadata((o, e) => ((DepPart)o)._select()));
		}

		public DepPart Head
		{
			get { return (DepPart)GetValue(HeadProperty); }
			set { SetValue(HeadProperty, value); }
		}

		public static readonly DependencyProperty HeadProperty =
			DependencyProperty.Register("Head", typeof(DepPart), typeof(DepPart), new PropertyMetadata(default(DepPart)));

		[DefaultValue(default(String))]
		public String DependencyType
		{
			get { return (String)GetValue(DependencyTypeProperty); }
			set { SetValue(DependencyTypeProperty, value); }
		}

		public static readonly DependencyProperty DependencyTypeProperty =
			DependencyProperty.Register("DependencyType", typeof(String), typeof(DepPart), new PropertyMetadata(default(String)));


		public IList<DepPart> AvailableHeads
		{
			get
			{
				return PartsHost
						.OfType<DepPart>()
						.Where(p => p != this && !p.ancestors().Contains(this))
						.ToArray();
			}
		}

		IEnumerable<DepPart> ancestors()
		{
			var _tmp = this.Head;
			if (_tmp != null)
			{
				yield return _tmp;
				foreach (var a in _tmp.ancestors())
					yield return a;
			}
		}

		public IEnumerable<DepPart> Children()
		{
			return PartsHost.OfType<DepPart>().Where(p => p.Head == this);
		}


		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool IsSelected
		{
			get { return (bool)GetValue(Selector.IsSelectedProperty); }
			set { SetValue(Selector.IsSelectedProperty, value); }
		}

		void _select()
		{
			var tier = (DependenciesTier)PartsHost;
			if (!this.IsSelected)
			{
				if (tier.SelectingHead == this)
					tier.SelectingHead = null;
				return;
			}
			tier.SelectingHead = this;
			//Debug.Print("{0} {1}", this.Text, IsSelected);
		}
#if false
		public Iset<IPart> Parts { get { return (Iset<IPart>)GetValue(PartsProperty); } }
		Iset<IPart> Iitems<IPart>.Items { get { return this.Parts; } }
		IList IListSource.GetList() { return this.Parts; }
		bool IListSource.ContainsListCollection { get { return false; } }

		public IPart this[int index]
		{
			get { return Parts[index]; }
			set { Parts[index] = value; }
		}

		public int Count { get { return Parts.Count; } }

		public IEnumerator<IPart> GetEnumerator() { return Parts.GetEnumerator(); }

		IEnumerator IEnumerable.GetEnumerator() { return Parts.GetEnumerator(); }
#endif
		public DepPart()
		{
#if false
			SetValue(PartsPropertyKey, new _PartsSet(this));
#endif
		}
	};
}
