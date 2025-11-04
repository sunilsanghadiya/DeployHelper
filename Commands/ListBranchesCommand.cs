using DeployHelper.Services;
using System;
using System.IO;

namespace DeployHelper.Commands;

public class ListBranchesCommand
{
    public void Execute()
    {
        var repoPath = Directory.GetCurrentDirectory();
        var branches = GitService.GetFeatureBranches(repoPath);

        if (branches.Count == 0)
        {
            Console.WriteLine("No feature branches found :(");
            return;
        }

        Console.WriteLine("Available feature branches:");
        for (int i = 0; i < branches.Count; i++)
            Console.WriteLine($"{i + 1}. {branches[i]}");
    }
}
