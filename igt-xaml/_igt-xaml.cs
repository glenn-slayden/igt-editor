using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xaml;


namespace xie
{
	public static class IgtXaml
	{
		public static readonly XamlSchemaContext ctx;

		static IgtXaml()
		{
			ctx = new XamlSchemaContext(new XamlSchemaContextSettings
			{
				SupportMarkupExtensionsWithDuplicateArity = true,
			});
		}
	};
}
