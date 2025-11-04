using System;
using System.Linq;
using LibGit2Sharp;

namespace DeployHelper.Commands
{
    public class ListBranchesCommand
    {
        public void Execute()
        {
            using var repo = new Repository(Environment.CurrentDirectory);

            Console.WriteLine($"Repository: {repo.Info.WorkingDirectory}");

            // Fetch remote refs to show up-to-date remote branches
            try
            {
                LibGit2Sharp.Commands.Fetch(repo, "origin", Array.Empty<string>(), new FetchOptions(), null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: fetch failed: {ex.Message}");
            }

            var excludedBaseBranches = new[] { "main", "master", "dev", "qa", "uat", "production" };

            //combine local and remote branch names
            var allBranches = repo.Branches
                .Where(b =>
                    //include both local and remote but exclude internal refs and excluded base names
                    !IsGitInternalBranch(b.FriendlyName) &&
                    !excludedBaseBranches.Contains(b.FriendlyName)
                )
                .Select(b => b.FriendlyName)
                .Distinct()
                .OrderBy(b => b)
                .ToList();

            if (!allBranches.Any())
            {
                Console.WriteLine("No feature branches found.");
                return;
            }

            Console.WriteLine("Available branches:");
            foreach (var b in allBranches)
                Console.WriteLine($"- {b}");
        }

        private static bool IsGitInternalBranch(string branchName)
        {
            return branchName.StartsWith("refs/") ||
                   branchName.Contains("HEAD") ||
                   branchName.Contains("FETCH_HEAD") ||
                   branchName.Contains("MERGE_");
        }
    }
}
