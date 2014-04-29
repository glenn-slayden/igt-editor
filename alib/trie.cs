using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace alib.Trie
{
	using String = System.String;

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	///
	///
	///
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public class Trie<TValue> : ICollection, IEnumerable<Trie<TValue>.TrieNodeBase>
	{
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		///
		/// Tested total Trie memory usage with and without base class and virtual functions; it made no difference.
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#if DEBUG
		[DebuggerDisplay("{parent_char}")]
#endif
		public abstract class TrieNodeBase
		{
#if DEBUG
			public static Trie<TValue> trie;
			public Char parent_char;
			public String _key()
			{
				if (trie.Root == this)
					return "root";
				return trie.GetKey(this);
			}
#endif
			protected TValue m_value = default(TValue);

			public TValue Value
			{
				get { return m_value; }
				set { m_value = value; }
			}

			public bool HasValue { get { return !Object.Equals(m_value, default(TValue)); } }
			public abstract bool IsLeaf { get; }

			public abstract TrieNodeBase this[char c] { get; }

			public abstract TrieNodeBase[] Nodes { get; }

			public abstract void SetLeaf();

			public abstract int ChildCount { get; }

			public abstract bool ShouldOptimize { get; }

			public abstract KeyValuePair<Char, TrieNodeBase>[] CharNodePairs();

			public abstract TrieNodeBase AddChild(char c, ref int node_count);

			/// <summary>
			/// Includes current node value
			/// </summary>
			/// <returns></returns>
			public IEnumerable<TValue> SubsumedValues()
			{
				if (Value != null)
					yield return Value;
				if (Nodes != null)
					foreach (TrieNodeBase child in Nodes)
						if (child != null)
							foreach (TValue t in child.SubsumedValues())
								yield return t;
			}

			/// <summary>
			/// Includes current node
			/// </summary>
			/// <returns></returns>
			public IEnumerable<TrieNodeBase> SubsumedNodes()
			{
				yield return this;
				if (Nodes != null)
					foreach (TrieNodeBase child in Nodes)
						if (child != null)
							foreach (TrieNodeBase n in child.SubsumedNodes())
								yield return n;
			}

			/// <summary>
			/// Doesn't include current node
			/// </summary>
			/// <returns></returns>
			public IEnumerable<TrieNodeBase> SubsumedNodesExceptThis()
			{
				if (Nodes != null)
					foreach (TrieNodeBase child in Nodes)
						if (child != null)
							foreach (TrieNodeBase n in child.SubsumedNodes())
								yield return n;
			}

			/// <summary>
			/// Note: doesn't de-optimize optimized nodes if re-run later
			/// </summary>
			public void OptimizeChildNodes()
			{
				if (Nodes != null)
					foreach (var q in CharNodePairs())
					{
						TrieNodeBase n_old = q.Value;
						if (n_old.ShouldOptimize)
						{
							TrieNodeBase n_new = new SparseTrieNode(n_old.CharNodePairs());
							n_new.m_value = n_old.m_value;
#if DEBUG
							n_new.parent_char = n_old.parent_char;
#endif
							//Trie<TValue>.c_sparse_nodes++;
							ReplaceChild(q.Key, n_new);
						}
						n_old.OptimizeChildNodes();
					}
			}

			public abstract void ReplaceChild(Char c, TrieNodeBase n);
		};

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		///
		/// Sparse Trie Node
		///
		/// currently, this one's "nodes" value is never null, because we leave leaf nodes as the non-sparse type,
		/// (with nodes==null) and they currently never get converted back. Consequently, IsLeaf should always be 'false'.
		/// However, we're gonna do the check anyway.
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public class SparseTrieNode : TrieNodeBase
		{
			Dictionary<Char, TrieNodeBase> d;

			public SparseTrieNode(IEnumerable<KeyValuePair<Char, TrieNodeBase>> ie)
			{
				d = new Dictionary<char, TrieNodeBase>();
				foreach (var kvp in ie)
					d.Add(kvp.Key, kvp.Value);
			}

			public override TrieNodeBase this[Char c]
			{
				get
				{
					TrieNodeBase node;
					return d.TryGetValue(c, out node) ? node : null;
				}
			}

			public override TrieNodeBase[] Nodes
			{
				get
				{
					var a = new TrieNodeBase[d.Count];
					d.Values.CopyTo(a, 0);
					return a;
				}
			}

			/// <summary>
			/// do not use in current form. This means, run OptimizeSparseNodes *after* any pruning
			/// </summary>
			public override void SetLeaf() { d = null; }

			public override int ChildCount { get { return d.Count; } }

			public override KeyValuePair<Char, TrieNodeBase>[] CharNodePairs()
			{
				var a = new KeyValuePair<Char, TrieNodeBase>[d.Count];
				var e = d.GetEnumerator();
				for (int i = 0; i < a.Length; i++)
				{
					e.MoveNext();
					a[i] = e.Current;
				}
				return a;
			}

			public override TrieNodeBase AddChild(char c, ref int node_count)
			{
				TrieNodeBase node;
				if (!d.TryGetValue(c, out node))
				{
					node = new TrieNode();
					node_count++;
#if DEBUG
					node.parent_char = c;
#endif
					d.Add(c, node);
				}
				return node;
			}

			public override void ReplaceChild(Char c, TrieNodeBase n)
			{
#if DEBUG
				if (!d.ContainsKey(c))
					throw new Exception();
#endif
				d[c] = n;
			}

			public override bool ShouldOptimize { get { return false; } }
			public override bool IsLeaf { get { return d == null; } }

		};

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		///
		/// Non-sparse Trie Node
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public class TrieNode : TrieNodeBase
		{
			private TrieNodeBase[] nodes = null;
			private Char m_base;

			public override int ChildCount
			{
				get
				{
					int c = 0;
					TrieNodeBase[] _tmp;
					if ((_tmp = nodes) != null)
						for (int i = 0; i < _tmp.Length; i++)
							if (_tmp[i] != null)
								c++;
					return c;
				}
			}
			public int AllocatedChildCount
			{
				get
				{
					TrieNodeBase[] _tmp;
					return (_tmp = nodes) != null ? _tmp.Length : 0;
				}
			}

			public override TrieNodeBase[] Nodes { get { return nodes; } }

			public override void SetLeaf() { nodes = null; }

			public override KeyValuePair<Char, TrieNodeBase>[] CharNodePairs()
			{
				var rg = new KeyValuePair<char, TrieNodeBase>[ChildCount];
				Char ch = m_base;
				int i = 0;
				foreach (TrieNodeBase child in nodes)
				{
					if (child != null)
						rg[i++] = new KeyValuePair<char, TrieNodeBase>(ch, child);
					ch++;
				}
				return rg;
			}

			public override TrieNodeBase this[char c]
			{
				get
				{
					TrieNodeBase[] _tmp;
					if ((_tmp = nodes) != null && m_base <= c && c < m_base + _tmp.Length)
						return _tmp[c - m_base];
					return null;
				}
			}

			TrieNodeBase[] ensure(char c)
			{
				TrieNodeBase[] _tmp;
				if ((_tmp = this.nodes) == null)
				{
					m_base = c;
					_tmp = new TrieNodeBase[1];
				}
				else if (c >= m_base + _tmp.Length)
				{
					_tmp = alib.Array.arr.Resize(_tmp, c - m_base + 1);
				}
				else if (c < m_base)
				{
					Char c_new = (Char)(m_base - c);
					var _new = new TrieNodeBase[_tmp.Length + c_new];
					_tmp.CopyTo(_new, c_new);
					m_base = c;
					_tmp = _new;
				}
				else
					return _tmp;

				return this.nodes = _tmp;
			}

			public override TrieNodeBase AddChild(char c, ref int node_count)
			{
				var _tmp = ensure(c);
				var node = _tmp[c - m_base];
				if (node == null)
				{
					node = new TrieNode();
					node_count++;
#if DEBUG
					node.parent_char = c;
#endif
					_tmp[c - m_base] = node;
				}
				return node;
			}

			public override void ReplaceChild(Char c, TrieNodeBase n)
			{
				if (nodes == null || c >= m_base + nodes.Length || c < m_base)
					throw new Exception();
				nodes[c - m_base] = n;
			}

			public override bool ShouldOptimize
			{
				get
				{
					if (nodes == null)
						return false;
					return (ChildCount * 9 < nodes.Length);		// empirically determined optimal value (space & time)
				}
			}

			public override bool IsLeaf { get { return nodes == null; } }
		};

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		/// Trie proper begins here
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		private TrieNodeBase _root = new TrieNode();
		public int c_nodes = 0;
		//public static int c_sparse_nodes = 0;

#if DEBUG
		public Trie()
		{
			TrieNode.trie = this;
		}
#endif

		public IEnumerable<TValue> Values { get { return _root.SubsumedValues(); } }

		public void OptimizeSparseNodes()
		{
			if (_root.ShouldOptimize)
			{
				_root = new SparseTrieNode(_root.CharNodePairs());
				//c_sparse_nodes++;
			}
			_root.OptimizeChildNodes();
		}

		public TrieNodeBase Root { [DebuggerStepThrough] get { return _root; } }

		public TrieNodeBase Add(String s, TValue v)
		{
			TrieNodeBase node = _root;
			foreach (Char c in s)
				node = node.AddChild(c, ref c_nodes);

			node.Value = v;
			return node;
		}

		public bool Contains(String s)
		{
			TrieNodeBase node = _root;
			foreach (Char c in s)
			{
				node = node[c];
				if (node == null)
					return false;
			}
			return node.HasValue;
		}

		/// <summary>
		/// Debug only; this is hideously inefficient
		/// </summary>
		public String GetKey(TrieNodeBase seek)
		{
			List<Char> sofar = new List<Char>();

			GetKeyHelper fn = null;
			fn = (TrieNodeBase cur) =>
			{
				sofar.Add(' ');	// placeholder
				foreach (var kvp in cur.CharNodePairs())
				{
					sofar[sofar.Count - 1] = kvp.Key;
					if (kvp.Value == seek)
						return true;
					if (kvp.Value.Nodes != null && fn(kvp.Value))
						return true;
				}
				sofar.RemoveAt(sofar.Count - 1);
				return false;
			};

			if (fn(_root))
				return new String(sofar.ToArray());
			return null;
		}


		/// <summary>
		/// Debug only; this is hideously inefficient
		/// </summary>
		delegate bool GetKeyHelper(TrieNodeBase cur);
		public String GetKey(TValue seek)
		{
			List<Char> sofar = new List<Char>();

			GetKeyHelper fn = null;
			fn = (TrieNodeBase cur) =>
			{
				sofar.Add(' ');	// placeholder
				foreach (var kvp in cur.CharNodePairs())
				{
					sofar[sofar.Count - 1] = kvp.Key;
					if (kvp.Value.Value != null && kvp.Value.Value.Equals(seek))
						return true;
					if (kvp.Value.Nodes != null && fn(kvp.Value))
						return true;
				}
				sofar.RemoveAt(sofar.Count - 1);
				return false;
			};

			if (fn(_root))
				return new String(sofar.ToArray());
			return null;
		}

#if false
		public List<String> FindMask(String mask)
		{
			return FindMask(mask, _root);
		}

		private List<String> FindMask(String mask, TrieNode<TValue> node)
		{
			char c = mask[0];
			mask = mask.Substring(1);
			List<String> list = new List<String>();
			if (c == '*')
			{
				c = node.Base;
				foreach (TrieNode<TValue> child in node.Nodes)
				{
					if (child != null)
					{
						if (mask.Length == 0)
						{
							if (child.IsEnd)
								list.Add(c.ToString());
						}
						else
						{
							foreach (String s in FindMask(mask, child))
								list.Add(c.ToString() + s);
						}
					}
					c++;
				}
			}
			else
			{
				TrieNode<TValue> child = node[c];
				if (child != null)
				{
					if (mask.Length == 0)
					{
						if (child.IsEnd)
							list.Add(c.ToString());
					}
					else
					{
						foreach (String s in FindMask(mask, child))
							list.Add(c.ToString() + s);
					}
				}
			}
			return list;
		}
#endif

		public TrieNodeBase FindNode(String s)
		{
			var node = _root;
			for (int i = 0; i < s.Length; i++)
				if ((node = node[s[i]]) == null)
					return null;
			return node;
		}

		/// <summary>
		/// If continuation from the terminal node is possible with a different input string, then that node is not
		/// returned as a 'last' node for the given input. In other words, 'last' nodes must be leaf nodes, where
		/// continuation possibility is truly unknown. The presence of a nodes array that we couldn't match to 
		/// means the search fails; it is not the design of the 'OrLast' feature to provide 'closest' or 'best'
		/// matching but rather to enable truncated tails still in the context of exact prefix matching.
		/// </summary>
		public TrieNodeBase FindNodeOrLast(String s_in, out bool f_exact)
		{
			TrieNodeBase node = _root;
			foreach (Char c in s_in)
			{
				if (node.IsLeaf)
				{
					f_exact = false;
					return node;
				}
				if ((node = node[c]) == null)
				{
					f_exact = false;
					return null;
				}
			}
			f_exact = true;
			return node;
		}

		// even though I found some articles that attest that using a foreach enumerator with arrays (and Lists)
		// returns a value type, thus avoiding spurious garbage, I had already changed the code to not use enumerator.
		public unsafe TValue Find(String s_in)
		{
			TrieNodeBase node = _root;
			fixed (Char* pin_s = s_in)
			{
				Char* p = pin_s;
				Char* p_end = p + s_in.Length;
				while (p < p_end)
				{
					if ((node = node[*p]) == null)
						return default(TValue);
					p++;
				}
				return node.Value;
			}
		}

		public unsafe TValue Find(Char* p_tag, int cb_ctag)
		{
			TrieNodeBase node = _root;
			Char* p_end = p_tag + cb_ctag;
			while (p_tag < p_end)
			{
				if ((node = node[*p_tag]) == null)
					return default(TValue);
				p_tag++;
			}
			return node.Value;
		}

		public IEnumerable<TValue> FindAll(String s_in)
		{
			TrieNodeBase node = _root;
			foreach (Char c in s_in)
			{
				if ((node = node[c]) == null)
					break;
				if (node.Value != null)
					yield return node.Value;
			}
		}

		public IEnumerable<TValue> SubsumedValues(String s)
		{
			TrieNodeBase node = FindNode(s);
			if (node == null)
				return alib.Collections.Collection<TValue>.Empty;
			return node.SubsumedValues();
		}

		public IEnumerable<TrieNodeBase> SubsumedNodes(String s)
		{
			TrieNodeBase node = FindNode(s);
			if (node == null)
				return alib.Collections.Collection<TrieNodeBase>.Empty;
			return node.SubsumedNodes();
		}

		public IEnumerable<TValue> AllSubstringValues(String s)
		{
			int i_cur = 0;
			while (i_cur < s.Length)
			{
				TrieNodeBase node = _root;
				int i = i_cur;
				while (i < s.Length)
				{
					node = node[s[i]];
					if (node == null)
						break;
					if (node.Value != null)
						yield return node.Value;
					i++;
				}
				i_cur++;
			}
		}

		/// <summary>
		/// note: only returns nodes with non-null values
		/// </summary>
		public void DepthFirstTraverse(Action<String, TrieNodeBase> callback)
		{
			Char[] rgch = new Char[100];
			int depth = 0;

			Action<TrieNodeBase> fn = null;
			fn = (TrieNodeBase cur) =>
			{
				if (depth >= rgch.Length)
				{
					Char[] tmp = new Char[rgch.Length * 2];
					System.Array.Copy(rgch, 0, tmp, 0, rgch.Length);
					rgch = tmp;
				}
				foreach (var kvp in cur.CharNodePairs())
				{
					rgch[depth] = kvp.Key;
					TrieNodeBase n = kvp.Value;
					if (n.Nodes != null)
					{
						depth++;
						fn(n);
						depth--;
					}
					else if (n.Value == null)		// leaf nodes should always have a value
						throw new Exception();

					if (!Object.Equals(n.Value, default(TValue)))
						callback(new String(rgch, 0, depth + 1), n);
				}
			};

			fn(_root);
		}


		/// <summary>
		/// note: only returns nodes with non-null values
		/// </summary>
		public void EnumerateLeafPaths(Action<String, IEnumerable<TrieNodeBase>> callback)
		{
			Stack<TrieNodeBase> stk = new Stack<TrieNodeBase>();
			Char[] rgch = new Char[100];

			Action<TrieNodeBase> fn = null;
			fn = (TrieNodeBase cur) =>
			{
				if (stk.Count >= rgch.Length)
				{
					Char[] tmp = new Char[rgch.Length * 2];
					System.Array.Copy(rgch, 0, tmp, 0, rgch.Length);
					rgch = tmp;
				}
				foreach (var kvp in cur.CharNodePairs())
				{
					rgch[stk.Count] = kvp.Key;
					TrieNodeBase n = kvp.Value;
					stk.Push(n);
					if (n.Nodes != null)
						fn(n);
					else
					{
						if (n.Value == null)		// leaf nodes should always have a value
							throw new Exception();
						callback(new String(rgch, 0, stk.Count), stk);
					}
					stk.Pop();
				}
			};

			fn(_root);
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		///
		/// Convert a trie with one value type to another
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public Trie<TNew> ToTrie<TNew>(Func<TValue, TNew> value_converter)
		{
			Trie<TNew> t = new Trie<TNew>();
			DepthFirstTraverse((s, n) =>
			{
				t.Add(s, value_converter(n.Value));
			});
			return t;
		}

		public int Count { get { return c_nodes; } }

		public IEnumerator GetEnumerator()
		{
			return _root.SubsumedNodes().GetEnumerator();
		}

		IEnumerator<TrieNodeBase> IEnumerable<TrieNodeBase>.GetEnumerator()
		{
			return _root.SubsumedNodes().GetEnumerator();
		}

		public void CopyTo(System.Array array, int index)
		{
			foreach (var n in this)
				array.SetValue(n, index++);
		}

		bool ICollection.IsSynchronized { get { return false; } }
		Object ICollection.SyncRoot { get { return null; } }
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	///
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public static class TrieExtension
	{
		public static Trie<TValue> ToTrie<TValue>(this IEnumerable<String> src, Func<String, int, TValue> selector)
		{
			Trie<TValue> t = new Trie<TValue>();
			int idx = 0;
			foreach (String s in src)
				t.Add(s, selector(s, idx++));
			return t;
		}

		public static Trie<TValue> ToTrie<TValue>(this Dictionary<String, TValue> src)
		{
			Trie<TValue> t = new Trie<TValue>();
			foreach (var kvp in src)
				t.Add(kvp.Key, kvp.Value);
			return t;
		}

		public static IEnumerable<TValue> AllSubstringValues<TValue>(this String s, Trie<TValue> trie)
		{
			return trie.AllSubstringValues(s);
		}
	};
}
