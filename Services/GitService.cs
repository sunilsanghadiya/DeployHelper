using LibGit2Sharp;
using System;
using System.Collections.Generic;


public static class GitService
{
    public static List<string> GetFeatureBranches(string repoPath)
    {
        using var repo = new Repository(repoPath);
        return repo.Branches
            .Where(b => b.FriendlyName.StartsWith("feature/") && !b.IsRemote)
            .Select(b => b.FriendlyName)
            .OrderBy(x => x)
            .ToList();
    }

    public static string CreateEnvironmentBranch(string repoPath, string baseBranch, string newBranch, List<string> features, bool push)
    {
        using var repo = new Repository(repoPath);
        var signature = repo.Config.BuildSignature(DateTimeOffset.Now);

        // Checkout base branch
        Commands.Checkout(repo, repo.Branches[baseBranch] ?? repo.CreateBranch(baseBranch, repo.Branches[$"origin/{baseBranch}"].Tip));

        // Create new env branch
        var envBranch = repo.CreateBranch(newBranch);
        Commands.Checkout(repo, envBranch);

        foreach (var feature in features)
        {
            Console.WriteLine($"Merging {feature}...");
            var featureBranch = repo.Branches[feature] ?? repo.Branches[$"origin/{feature}"];
            if (featureBranch == null)
            {
                Console.WriteLine($"Branch not found: {feature}");
                continue;
            }

            try
            {
                var result = repo.Merge(featureBranch, signature, new MergeOptions { FastForwardStrategy = FastForwardStrategy.Default });
                if (result.Status == MergeStatus.Conflicts)
                    Console.WriteLine($"Merge conflict in {feature}. Resolve manually before pushing.");
                else
                    Console.WriteLine($"Merged {feature}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to merge {feature}: {ex.Message}");
            }
        }

        if (push)
        {
            var remote = repo.Network.Remotes["origin"];
            repo.Network.Push(remote, $"refs/heads/{newBranch}");
            Console.WriteLine($"Pushed {newBranch} to origin");
        }

        return newBranch;
    }
}
