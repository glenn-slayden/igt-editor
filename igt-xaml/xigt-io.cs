using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Xml;


using alib;
using alib.Enumerable;
using alib.Debugging;

namespace xie
{
	public class xxx
	{
		public IgtCorpus bar(String fn)
		{
			fn = System.IO.Path.GetFullPath(fn);

			var xd = new XmlDocument();

			xd.Load(fn);

			//var ic = new IgtCorpus { Filename = fn };
			var ic = new IgtCorpus();// { Filename = fn };

			bar(xd, ic);

			return ic;
		}

		static readonly Char[] rgch_standoff_split = new[] { '[', ']', ':', ' ' };

		public void bar(XmlDocument xd, IgtCorpus ic)
		{
			tier_base t;
			Igt igt;
			IHostedItem hi;
			String s_attr;

			var id_map = new Dictionary<String, IHostedItem>();

			var _corpus = (XmlElement)xd.ChildNodes[1];

			foreach (XmlElement _igt in _corpus.ChildNodes)
			{
				ic.Add(igt = new Igt
				{
					Host = ic,

				});

				id_map.Clear();

				foreach (XmlElement _el in _igt.ChildNodes)
				{
					switch (_el.Name)
					{
						case "metadata":

							foreach (XmlElement _md in _el.ChildNodes)
							{
								if (_md.Name != "meta")
									throw not.expected;

								if ((s_attr = _md.GetAttribute("doc-id")).Length > 0)
									igt.DocId = s_attr;
								else if ((s_attr = _md.GetAttribute("iso-639-3")).Length > 0)
								{
									if (igt.Language == null)
										igt.Language = s_attr;
									else
										igt.Language += "; " + s_attr;
								}
							}
							break;

						case "tier":
							{
								t = null;

								if (_el.HasAttribute("content"))
								{
								}

								foreach (XmlElement _it in _el.ChildNodes)
								{
									if (_it.Name != "item")
										throw not.expected;

									if (t == null)
									{
										if (_it.HasAttribute("line"))
											t = new TextGroupTier();
										else if (_it.HasAttribute("content"))
											t = new SegTier();
										else
											throw not.expected;

										id_map.Add(t.Name = _el.GetAttribute("id"), t);
										t.TierType = _el.GetAttribute("type");
										igt.Add(t);
									}

									s_attr = _it.GetAttribute("id");

									if (t is TextGroupTier)
									{
										var tt = new TextTier
										{
											Text = _it.InnerText,
											LineNumbers = _it.GetAttribute("line")
															.Split(alib.Character.Charset.ws, StringSplitOptions.RemoveEmptyEntries)
															.Select(x => int.Parse(x))
															.ToArray(),
											TierType = s_attr,
										};
										((ITiers<ITier>)t).Add(tt);
										if (tt.Host == null)
											Nop.X();
										hi = tt;
									}
									else if (t is SegTier)
									{
										var z = _it.GetAttribute("content");

										//Debug.WriteLine(z);

										var rgs = z.Split(rgch_standoff_split, StringSplitOptions.RemoveEmptyEntries);
										if (rgs.Length != 3)
											throw not.expected;

										var sp = new SegPart
										{
											//Text = _it.InnerText
											//Name = _it.GetAttribute("id"),
											//SourceTier = (TextTier)id_map[rgs[0]],
											//FromChar = int.Parse(rgs[1]),
											//ToChar = int.Parse(rgs[2]),
										};
										((SegTier)t).Add(sp);
										hi = sp;
									}
									else
									{
										throw not.expected;
									}

									id_map.Add(hi.Name = s_attr, hi);
								}

								//if (t == null || t.Count == 0)
								//	throw not.expected;

								//igt.Add(t);
							}
							break;

						default:
							throw not.expected;
					}
				}

				//igt.CoerceValue(dps.FromLineProperty);
				//igt.CoerceValue(dps.ToLineProperty);
				Nop.X();
			}
		}
	};
}
