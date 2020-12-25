﻿using System.Collections.Generic;

namespace ACME.PRJ.CodebaseCommons
{
	/// <summary>
	/// Инфо о ветке
	/// </summary>
	public class BranchInfo : InfoBase
	{
		public string LatestCommitId { get; set; }

		public string LatestChangesetId { get; set; }

		public bool IsDefault { get; set; }

		public RepoInfo Repo { get; set; }
		
		/// <summary>
		/// Autogenerated
		/// </summary>
		public override bool Equals(object obj)
		{
			var info = obj as BranchInfo;
			return info != null &&
				   IsDefault == info.IsDefault &&
				   base.Equals(obj);
		}
		
		/// <summary>
		/// Autogenerated
		/// </summary>
		public override int GetHashCode()
		{
			var hashCode = -1042057239;
			hashCode = hashCode * -1521134295 + base.GetHashCode();
			hashCode = hashCode * -1521134295 + IsDefault.GetHashCode();
			return hashCode;
		}

		public override bool IsValid()
		{
			return (Repo?.IsValid() == true)
				&& base.IsValid();
		}
	}
}