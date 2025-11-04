using LibGit2Sharp;
using System.Linq;
using System.Collections.Generic;

namespace DeployHelper.Helpers
{
    public static class BranchHelper
    {
        public static List<string> GetLocalFeatureBranches(Repository repo, IEnumerable<string> excludedBaseBranches)
        {
            var branches = repo.Branches
                .Where(b =>
                    !b.IsRemote &&
                    !IsExcludedBaseBranch(b.FriendlyName, excludedBaseBranches) &&
                    !IsGitInternalBranch(b.FriendlyName))
                .Select(b => b.FriendlyName)
                .Distinct()
                .OrderBy(b => b)
                .ToList();

            return branches;
        }

        private static bool IsExcludedBaseBranch(string branchName, IEnumerable<string> excludedBaseBranches)
        {
            return excludedBaseBranches.Any(baseBranch => branchName == baseBranch);
        }

        private static bool IsGitInternalBranch(string branchName)
        {
            return branchName.StartsWith("refs/") ||
                   branchName.Contains("HEAD") ||
                   branchName.Contains("MERGE_") ||
                   branchName.Contains("FETCH_HEAD");
        }
    }
}
