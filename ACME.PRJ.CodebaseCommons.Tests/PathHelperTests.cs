using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;

namespace ACME.PRJ.CodebaseCommons.Tests
{
	[TestClass]
	class PathHelperTests
	{
		[TestMethod]
		[TestCase(@"/folder1/folder2/", '/', ExpectedResult = @"folder1/folder2")]
		[TestCase(@"folder1/folder2/", '/', ExpectedResult = @"folder1/folder2")]
		[TestCase(@"/folder1/folder2", '/', ExpectedResult = @"folder1/folder2")]
		[TestCase(@"/folder1/folder2/", '\\', ExpectedResult = @"/folder1/folder2/")]
		[TestCase(@"\folder1\folder2/", '\\', ExpectedResult = @"folder1\folder2/")]
		[TestCase(@"/folder1\folder2\", '\\', ExpectedResult = @"/folder1\folder2")]
		public string NormalizePathTest(string path, char catalogSeparator)
		{
			return PathHelper.NormalizePath(path, catalogSeparator);
		}
	}
}
