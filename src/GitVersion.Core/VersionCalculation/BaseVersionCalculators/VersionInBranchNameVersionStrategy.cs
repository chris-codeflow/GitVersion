using System;
using System.Collections.Generic;
using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Extensions;

namespace GitVersion.VersionCalculation
{
    /// <summary>
    /// Version is extracted from the name of the branch.
    /// BaseVersionSource is the commit where the branch was branched from its parent.
    /// Does not increment.
    /// </summary>
    public class VersionInBranchNameVersionStrategy : VersionStrategyBase
    {
        private IRepositoryStore repositoryStore;

        public VersionInBranchNameVersionStrategy(IRepositoryStore repositoryStore, Lazy<GitVersionContext> versionContext) : base(versionContext)
        {
            this.repositoryStore = repositoryStore ?? throw new ArgumentNullException(nameof(repositoryStore));
        }

        public override IEnumerable<BaseVersion> GetVersions()
        {
            var currentBranch = Context.CurrentBranch;
            var tagPrefixRegex = Context.Configuration.GitTagPrefix;
            return GetVersions(tagPrefixRegex, currentBranch);
        }

        internal IEnumerable<BaseVersion> GetVersions(string tagPrefixRegex, IBranch currentBranch)
        {
            if (!Context.FullConfiguration.IsReleaseBranch(NameWithoutOrigin(currentBranch)))
            {
                yield break;
            }

            var branchName = currentBranch.Name.Friendly;
            var versionInBranch = GetVersionInBranch(branchName, tagPrefixRegex);
            if (versionInBranch != null)
            {
                var commitBranchWasBranchedFrom = repositoryStore.FindCommitBranchWasBranchedFrom(currentBranch, Context.FullConfiguration);
                var branchNameOverride = branchName.RegexReplace("[-/]" + versionInBranch.Item1, string.Empty);
                yield return new BaseVersion("Version in branch name", false, versionInBranch.Item2, commitBranchWasBranchedFrom.Commit, branchNameOverride);
            }
        }

        private static Tuple<string, SemanticVersion> GetVersionInBranch(string branchName, string tagPrefixRegex)
        {
            var branchParts = branchName.Split('/', '-');
            foreach (var part in branchParts)
            {
                if (SemanticVersion.TryParse(part, tagPrefixRegex, out var semanticVersion))
                {
                    return Tuple.Create(part, semanticVersion);
                }
            }

            return null;
        }

        private static string NameWithoutOrigin(IBranch branch)
        {
            return branch.IsRemote && branch.Name.Friendly.StartsWith("origin/")
                ? branch.Name.Friendly.Substring("origin/".Length)
                : branch.Name.Friendly;
        }
    }
}
