using System;
using DeployHelper.Services;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DeployHelper.Commands;

public class CreateBranchCommand
{
    public async Task ExecuteAsync(string env, string baseBranch, List<string> features, bool push)
    {
        var repoPath = Directory.GetCurrentDirectory();
        var user = Environment.UserName;
        var timestamp = DateTime.Now.ToString("yyyyMMddHHmm");
        var newBranchName = $"{env.ToLower()}_feature_{user}_{timestamp}";

        Console.WriteLine($"\n Creating branch: {newBranchName}");
        Console.WriteLine($"Base branch: {baseBranch}");
        Console.WriteLine($"Merging features: {string.Join(", ", features)}");

        try
        {
            var createdBranch = GitService.CreateEnvironmentBranch(repoPath, baseBranch, newBranchName, features, push);

            // Log deployment info
            var record = new DeploymentRecord
            {
                Environment = env,
                BaseBranch = baseBranch,
                Branches = features,
                NewBranch = newBranchName,
                CreatedBy = user,
                Timestamp = DateTime.Now
            };

            DeploymentLogger.Log(record);
            Console.WriteLine($"Created {(push ? "and pushed" : "")} branch {createdBranch}");

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
