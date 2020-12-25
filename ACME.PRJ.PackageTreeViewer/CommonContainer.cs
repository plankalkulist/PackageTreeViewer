using System;
using ACME.PRJ.BitbucketAdapter;
using ACME.PRJ.CodebaseCommons;

namespace ACME.PRJ.PackageTreeViewer
{
	/// <summary>
	/// IoC на минималках
	/// </summary>
	internal static class CommonContainer
	{
		private static ICodebaseService _codebaseService { get; set; }

		public static T Resolve<T>(params object[] ctorParams)
		{
			if (typeof(T) == typeof(ICodebaseService))
			{
				if (_codebaseService == null)
				{
					//var instance = new BitbucketService((string)ctorParams[0]);
                    var instance = new CodebaseServiceStub();
                    _codebaseService = instance;
					return (T)_codebaseService;
				}
				else
				{
					throw new NotImplementedException();
				}
			}
			else
			{
				throw new NotImplementedException();
			}
		}

		public static T Resolve<T>()
		{
			if (typeof(T) == typeof(ICodebaseService))
			{
				if (_codebaseService == null)
				{
					throw new NotImplementedException();
				}
				else
				{
					return (T)_codebaseService;
				}
			}
			else
			{
				throw new NotImplementedException();
			}
		}
	}
}
