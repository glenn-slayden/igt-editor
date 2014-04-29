using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;

namespace alib.Wpf.Templates
{
	using String = System.String;
	using Array = System.Array;

	/// <summary> ComplexGroupDataTemplateSelector provides additional functionality for finding DataTemplates in WPF </summary>
	public sealed class ComplexGroupDataTemplateSelector : DataTemplateSelector
	{
		public const String DEFAULT_TEMPLATE_KEY_FORMAT = "data-template[{0}]";
		public const String DEFAULT_GROUP_TEMPLATE_KEY_FORMAT = "IEnumerable[{0}]";
		public const DiscoveryMethods DEFAULT_DISCOVERY_METHOD = DiscoveryMethods.Key | DiscoveryMethods.Type | DiscoveryMethods.Interface | DiscoveryMethods.Hierarchy;

		public String TemplateKeyFormat { get; set; }

		public String GroupTemplateKeyFormat { get; set; }

		public DiscoveryMethods DiscoveryMethod { get; set; }

		Dictionary<Object, DataTemplate> m_cachedDataTemplates;

		[Flags]
		public enum DiscoveryMethods
		{
			/// <summary> create a resource key to find the DataTemplate <see>TemplateKeyFormat</see>  <see>GroupTemplateKeyFormat</see> </summary>
			Key = 0x01,

			/// <summary> use the item type to find the DataTemplate based on its DataType property </summary>
			Type = 0x02,

			/// <summary> scan the item types interfaces to find the DataTemplate based on its DataType property </summary>
			Interface = 0x04,

			/// <summary> scan the item types type hierachy to find the DataTemplate based on its DataType property </summary>
			Hierarchy = 0x08,

			/// <summary> look for the DataTemplate in the application resources first (Overrides the default resource finding method; </summary>
			GeneralToSpecific = 0x100,

			/// <summary>
			/// use the full type name when creating a resource key using <see>TemplateKeyFormat</see>  <see>GroupTemplateKeyFormat</see>
			/// If not set the unqualified type name will be used. The default is to use the unqualified type name
			/// </summary>
			FullTypeName = 0x400,

			/// <summary> Does not cache the result of the resource search. Note: using this flag can impact performance. </summary>
			NoCache = 0x800,
		};

		/// <summary>
		/// Internally cached data template to indicate that  the cached key was already searched and nothing was found.
		/// (Prevents additional useless searches)
		/// </summary>
		sealed class NullDataTemplate : DataTemplate
		{
			public static readonly NullDataTemplate Instance = new NullDataTemplate();
			NullDataTemplate()
			{
			}
		};

		public ComplexGroupDataTemplateSelector()
		{
			m_cachedDataTemplates = new Dictionary<Object, DataTemplate>();
			this.DiscoveryMethod = DEFAULT_DISCOVERY_METHOD;
			this.TemplateKeyFormat = DEFAULT_TEMPLATE_KEY_FORMAT;
			this.GroupTemplateKeyFormat = DEFAULT_GROUP_TEMPLATE_KEY_FORMAT;
		}
		/// <summary>
		/// Selects a template by resource key looking from the specific to the general containers in the application heirachy
		/// The resource key to attempted to be found in the following order: Application resources; The main window resources; 
		/// The active window resources; The container resources
		/// </summary>
		/// <param name="resourceKey">The resource key to look up</param>
		/// <param name="container">The container to start looking in</param>
		/// <returns></returns>
		DataTemplate selectByKeyGeneralToSpecific(Object resourceKey, DependencyObject container)
		{
			DataTemplate dataTemplate = null;
			dataTemplate = Application.Current.TryFindResource(resourceKey) as DataTemplate;
			if (dataTemplate == null)
				dataTemplate = Application.Current.MainWindow.TryFindResource(resourceKey) as DataTemplate;

			if (dataTemplate == null)
				foreach (Window window in Application.Current.Windows)
					if (window.IsActive)
					{
						dataTemplate = window.TryFindResource(resourceKey) as DataTemplate;
						break;
					}

			if (dataTemplate == null && container is FrameworkElement)
				dataTemplate = ((FrameworkElement)container).TryFindResource(resourceKey) as DataTemplate;

			return dataTemplate;
		}

		/// <summary> Selects a template by resource key. Where to look is based on the DiscoverMethod property </summary>
		/// <param name="resourceKey">The resource key to look up</param>
		/// <param name="container">The container to start looking in</param>
		DataTemplate selectByKey(Object resourceKey, DependencyObject container)
		{
			if ((this.DiscoveryMethod & DiscoveryMethods.GeneralToSpecific) != 0)
				return selectByKeyGeneralToSpecific(resourceKey, container);
			if (container is FrameworkElement)
				return ((FrameworkElement)container).TryFindResource(resourceKey) as DataTemplate;
			return null;
		}

		/// <summary> Selects a data template, through cache, by key </summary>
		/// <param name="templateKey">The template key to look for</param>
		/// <param name="container">The items container</param>
		/// <returns>The selected DataTemplate or null if not found</returns>
		DataTemplate selectThroughCacheByKey(Object templateKey, DependencyObject container)
		{
			DataTemplate dataTemplate = null;
			if (!m_cachedDataTemplates.TryGetValue(templateKey, out dataTemplate))
			{
				dataTemplate = selectByKey(templateKey, container);
				if (dataTemplate == null)
					m_cachedDataTemplates.Add(templateKey, NullDataTemplate.Instance);
				else
					m_cachedDataTemplates.Add(templateKey, dataTemplate);
			}
			else if (dataTemplate is NullDataTemplate)
				return null;

			return dataTemplate;
		}

		/// <summary> selects a DataTemplate by scanning the Type hierachy using a DataTemplateKey </summary>
		/// <param name="itemType">The item type to look for</param>
		/// <param name="container">The items container</param>
		/// <returns>The selected DataTemplate or null if not found</returns>
		DataTemplate selectByTypeHierachy(Type type, FrameworkElement container)
		{
			DataTemplate dataTemplate = null;
			while (dataTemplate == null && type != typeof(Object))
			{
				DataTemplateKey dataTemplateKey = new DataTemplateKey(type);
				dataTemplate = selectByKey(dataTemplateKey, container);
				type = type.BaseType;
			}
			return dataTemplate;
		}

		/// <summary> Selects a data template by type, interface, or hiearchy (Depending on the DiscoveryMethod property </summary>
		/// <param name="itemType">The item type to look for</param>
		/// <param name="container">The items container</param>
		/// <returns>The selected DataTemplate or null if not found</returns>
		DataTemplate selectByType(Type itemType, FrameworkElement container)
		{
			DataTemplate dataTemplate = null;
			if (container == null)
				return null;

			if ((this.DiscoveryMethod & DiscoveryMethods.Type) == DiscoveryMethods.Type)
			{
				DataTemplateKey dataTemplateKey = new DataTemplateKey(itemType);
				dataTemplate = selectByKey(dataTemplateKey, container);
			}

			if (dataTemplate == null)
				if ((this.DiscoveryMethod & DiscoveryMethods.Interface) == DiscoveryMethods.Interface)
				{
					Type[] interfaces = itemType.GetInterfaces();
					for (int i = interfaces.Length - 1; i >= 0; i--)
					{
						Type interfaceType = interfaces[i];
						DataTemplateKey dataTemplateKey = new DataTemplateKey(interfaceType);
						dataTemplate = selectByKey(dataTemplateKey, container);
						if (dataTemplate != null)
						{
							break;
						}
					}
				}

			if (dataTemplate == null && (this.DiscoveryMethod & DiscoveryMethods.Hierarchy) == DiscoveryMethods.Hierarchy)
				dataTemplate = selectByTypeHierachy(itemType.BaseType, container);

			return dataTemplate;
		}

		/// <summary> Selects a data template, through cache, by type, interface, or hiearchy (Depending on the 
		/// DiscoveryMethod property </summary>
		/// <param name="itemType">The item type to look for</param>
		/// <param name="container">The items container</param>
		/// <returns>The selected DataTemplate or null if not found</returns>
		DataTemplate selectThroughCacheByType(Type itemType, FrameworkElement container)
		{
			DataTemplate dataTemplate = null;
			DataTemplateKey dataTemplateKey = new DataTemplateKey(itemType);
			if (!m_cachedDataTemplates.TryGetValue(dataTemplateKey, out dataTemplate))
			{
				dataTemplate = selectByType(itemType, container);
				if (dataTemplate == null)
					m_cachedDataTemplates.Add(dataTemplateKey, NullDataTemplate.Instance);
				else
					m_cachedDataTemplates.Add(dataTemplateKey, dataTemplate);
			}
			else if (dataTemplate is NullDataTemplate)
				return null;

			return dataTemplate;
		}

		String getTypeNameForKey(Type type)
		{
			return (this.DiscoveryMethod & DiscoveryMethods.FullTypeName) != 0 ? type.FullName : type.Name;
		}

		/// <summary> override for DataTemplate SelectTemplate method </summary>
		/// <param name="item">The item to be templated</param>
		/// <param name="container">The items container</param>
		/// <returns>The selected DataTemplate</returns>
		public override DataTemplate SelectTemplate(Object item, DependencyObject container)
		{
			if (item == null || container == null)
				return null;

			String templateKey = null;
			DataTemplate dataTemplate = null;
			try
			{
				if ((this.DiscoveryMethod & DiscoveryMethods.Key) == DiscoveryMethods.Key)
				{
					if (item is IBindingGroup)
					{
						IBindingGroup bindingGroup = item as IBindingGroup;
						Type elementType = bindingGroup.ElementType;
						if (elementType != null)
						{
							if (bindingGroup.Parameter == null)
								templateKey = String.Format(this.GroupTemplateKeyFormat, getTypeNameForKey(elementType));
							else
								templateKey = bindingGroup.Parameter;

							if ((this.DiscoveryMethod & DiscoveryMethods.NoCache) != 0)
								dataTemplate = selectByKey(templateKey, container);
							else
								dataTemplate = selectThroughCacheByKey(templateKey, container);
						}
					}
					else if (item is IEnumerable)
					{
						Type elementType = BindingGroup.GetElementType(item as IEnumerable);
						if (elementType != null)
						{
							templateKey = String.Format(this.GroupTemplateKeyFormat, getTypeNameForKey(elementType));
							if ((this.DiscoveryMethod & DiscoveryMethods.NoCache) != 0)
								dataTemplate = selectByKey(templateKey, container);
							else
								dataTemplate = selectThroughCacheByKey(templateKey, container);
						}
					}
					else
					{
						templateKey = String.Format(this.TemplateKeyFormat, getTypeNameForKey(item.GetType()));
						if ((this.DiscoveryMethod & DiscoveryMethods.NoCache) != 0)
							dataTemplate = selectByKey(templateKey, container);
						else
							dataTemplate = selectThroughCacheByKey(templateKey, container);
					}
				}

				if (dataTemplate == null)
				{
					if ((this.DiscoveryMethod & (DiscoveryMethods.Type | DiscoveryMethods.Interface | DiscoveryMethods.Hierarchy)) != 0)
					{
						if ((this.DiscoveryMethod & DiscoveryMethods.NoCache) != 0)
							dataTemplate = selectByType(item.GetType(), container as FrameworkElement);
						else
							dataTemplate = selectThroughCacheByType(item.GetType(), container as FrameworkElement);
					}
				}
#if DEBUG
				if (dataTemplate == null)
				{
					String debugMessage = "DataTemplate not found for";
					if ((this.DiscoveryMethod & DiscoveryMethods.Key) != 0)
						debugMessage += " Resource Key \"" + templateKey + "\"";
					if ((this.DiscoveryMethod & (DiscoveryMethods.Type | DiscoveryMethods.Interface | DiscoveryMethods.Hierarchy)) != 0)
					{
						if ((this.DiscoveryMethod & DiscoveryMethods.Key) != 0)
							debugMessage += " or";
						debugMessage += " DataType \"" + item.GetType().FullName + "\"";
					}
					Debug.WriteLine(debugMessage, GetType().Name);
				}
#endif
			}
			catch
			{
				dataTemplate = base.SelectTemplate(item, container);
			}
			return dataTemplate;
		}
	};

	public interface IBindingGroup
	{
		Type ElementType { get; }
		IEnumerable Items { get; }
		String Parameter { get; }
	};

	public sealed class BindingGroup : IEnumerable, IBindingGroup
	{
		public String Parameter { get; private set; }
		public IEnumerable Items { get; private set; }

		public BindingGroup(IEnumerable items, String parameter)
		{
			this.Items = items;
			this.Parameter = parameter;
		}

		public Type ElementType { get { return GetElementType(this.Items); } }

		public static Type GetElementType(IEnumerable enumerable)
		{
			Type enumerableType = enumerable.GetType();
			Type elementType = null;
			IEnumerator enumItems;
			Type[] genericArguments;

			if (enumerableType.IsGenericType && (genericArguments = enumerableType.GetGenericArguments()).Length > 0)
				elementType = genericArguments[0];

			if (elementType == null && (enumItems = enumerable.GetEnumerator()).MoveNext() && enumItems.Current != null)
				elementType = enumItems.Current.GetType();

			return elementType;
		}


		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.Items.GetEnumerator();
		}

		public override String ToString()
		{
			return String.Format("{{BindingGroup of {0}}}", ElementType.FullName);
		}
	};

	[MarkupExtensionReturnType(typeof(String))]
	public sealed class IEnumerableKeyExtension : MarkupExtension
	{
		static Type s_genericEnumerable = typeof(IEnumerable<Object>).GetGenericTypeDefinition();

		public Type Type { get; set; }

		public String TypeName { get; set; }

		public IEnumerableKeyExtension() { }
		public IEnumerableKeyExtension(String typeName) { this.TypeName = typeName; }
		public IEnumerableKeyExtension(Type type) { this.Type = type; }

		Type parseType(IServiceProvider serviceProvider)
		{
			if (this.Type != null)
				return this.Type;

			IXamlTypeResolver xamlTypeResolver = serviceProvider.GetService(typeof(IXamlTypeResolver)) as IXamlTypeResolver;
			if (xamlTypeResolver != null)
				return xamlTypeResolver.Resolve(this.TypeName);

			return typeof(IEnumerable<Object>);
		}

		public override Object ProvideValue(IServiceProvider serviceProvider)
		{
			return new DataTemplateKey(s_genericEnumerable.MakeGenericType(parseType(serviceProvider)));
		}
	};

	[MarkupExtensionReturnType(typeof(Type))]
	public sealed class IEnumerableTypeExtension : TypeExtension
	{
		static Type s_genericEnumerable = typeof(IEnumerable<Object>).GetGenericTypeDefinition();

		public IEnumerableTypeExtension() { }

		public IEnumerableTypeExtension(String typeName) : base(typeName) { }

		public IEnumerableTypeExtension(Type type) : base(type) { }

		Type parseType(IServiceProvider serviceProvider)
		{
			if (this.Type != null)
				return this.Type;

			IXamlTypeResolver xamlTypeResolver = serviceProvider.GetService(typeof(IXamlTypeResolver)) as IXamlTypeResolver;
			if (xamlTypeResolver != null)
				return xamlTypeResolver.Resolve(this.TypeName);

			return typeof(IEnumerable<Object>);
		}

		public override Object ProvideValue(IServiceProvider serviceProvider)
		{
			return s_genericEnumerable.MakeGenericType(parseType(serviceProvider));
		}
	};

	/// <summary>
	/// ComplexGroupConverter is a IMultiValueConverter used in MultiBindings
	/// in conjuction with the <see>ComplexGroupDataTemplateSelector</see> to
	/// enable complex data template hierachies
	/// 
	/// &lt;complex:ComplexGroupConverter x:Key="group-converter"/>
	/// &lt;HierarchicalDataTemplate DataType="{x:Type cpn:INet}">
	///     &lt;HierarchicalDataTemplate.ItemsSource>
	///        &lt;MultiBinding Converter="{StaticResource group-converter}">
	///              &lt;Binding Path="Definitions"/>
	///              &lt;Binding Path="Pages"/>
	///        &lt;/MultiBinding>
	///        &lt;/HierarchicalDataTemplate.ItemsSource>
	///        &lt;StackPanel Orientation="Horizontal">
	///            &lt;Image Source="net.png" VerticalAlignment="Center"/>
	///            &lt;TextBlock Text="{Binding Path=Label}"/>
	///        &lt;/StackPanel>
	/// &lt;/HierarchicalDataTemplate>
	/// </summary>
	public sealed class ComplexGroupConverter : IMultiValueConverter
	{
		public Object Convert(Object[] values, Type targetType, Object parameter, CultureInfo culture)
		{
			List<Object> results = new List<Object>();
			String[] parameters;
			if (parameter is String)
			{
				parameters = ((String)parameter).Split(',');
				for (int i = 0; i < parameters.Length; i++)
					parameters[i] = parameters[i].Trim();
			}
			else
				parameters = new String[0];

			int index = 0;
			foreach (Object value in values)
			{
				if (!(value is IEnumerable))
					results.Add(value);
				else if (index < parameters.Length)
					results.Add(new BindingGroup(value as IEnumerable, parameters[index]));
				else
					results.Add(value);
				index++;
			}
			return results;
		}

		public Object[] ConvertBack(Object value, Type[] targetTypes, Object parameter, CultureInfo culture)
		{
			if (!(value is List<Object>))
				throw new NotSupportedException();

			List<Object> objects = value as List<Object>;
			return objects.ToArray();
		}
	};
}
