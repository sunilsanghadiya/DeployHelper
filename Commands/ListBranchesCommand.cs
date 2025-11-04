using DeployHelper.Services;
using System;
using System.IO;
using LibGit2Sharp;

namespace DeployHelper.Commands;

public class ListBranchesCommand
{
    public void Execute()
    {
        using var repo = new Repository(Environment.CurrentDirectory);

        var excludedBaseBranches = new[] { "main", "master", "dev", "develop", "qa", "uat", "prod", "production" };

        var branches = repo.Branches
            .Where(b =>
                !b.IsRemote &&
                !excludedBaseBranches.Contains(b.FriendlyName, StringComparer.OrdinalIgnoreCase) &&
                !b.FriendlyName.Contains("origin/") &&
                !b.FriendlyName.StartsWith("refs/", StringComparison.OrdinalIgnoreCase)
            )
            .Select(b => b.FriendlyName)
            .OrderBy(b => b)
            .ToList();

        if (branches.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("No local feature-like branches found :( ");
            Console.ResetColor();
            return;
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Found {branches.Count} potential feature branches:\n");
        Console.ResetColor();

        foreach (var branch in branches)
        {
            Console.WriteLine($" - {branch}");
        }

        Console.WriteLine();
    }
}
