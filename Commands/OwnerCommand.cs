using System;
using LibGit2Sharp;

namespace DeployHelper.Commands
{
    public class OwnerCommand
    {
        public void Execute(string branchName)
        {
            try
            {
                using var repo = new Repository(Environment.CurrentDirectory);

                Console.WriteLine();
                Console.WriteLine($"Analyzing branch: {branchName}");
                Console.WriteLine();

                // Find the branch (local or remote)
                var branch = repo.Branches.FirstOrDefault(b => b.FriendlyName == branchName)
                            ?? repo.Branches.FirstOrDefault(b => b.FriendlyName == $"origin/{branchName}");

                if (branch == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Branch '{branchName}' not found.");
                    Console.ResetColor();
                    return;
                }

                var commits = branch.Commits.ToList();

                if (!commits.Any())
                {
                    Console.WriteLine("No commits found in this branch.");
                    return;
                }

                // Get creator (first commit author)
                var firstCommit = commits.Last();
                var creator = firstCommit.Author;

                // Get last contributor
                var lastCommit = commits.First();
                var lastContributor = lastCommit.Author;

                // Count commits per author
                var contributorStats = commits
                    .GroupBy(c => new { c.Author.Name, c.Author.Email })
                    .Select(g => new
                    {
                        Name = g.Key.Name,
                        Email = g.Key.Email,
                        CommitCount = g.Count(),
                        Percentage = (g.Count() * 100.0) / commits.Count
                    })
                    .OrderByDescending(x => x.CommitCount)
                    .ToList();

                // Calculate branch age
                var branchAge = DateTime.Now - firstCommit.Author.When.DateTime;

                // Display results
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"Branch: {branch.FriendlyName}");
                Console.ResetColor();
                Console.WriteLine();

                Console.WriteLine($"Owner (Creator): {creator.Name} <{creator.Email}>");
                Console.WriteLine($"Created: {firstCommit.Author.When:yyyy-MM-dd HH:mm}");
                Console.WriteLine();

                Console.WriteLine($"Last Contributor: {lastContributor.Name} <{lastContributor.Email}>");
                Console.WriteLine($"Last Activity: {lastCommit.Author.When:yyyy-MM-dd HH:mm}");
                Console.WriteLine();

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Contributors:");
                Console.ResetColor();

                foreach (var contributor in contributorStats)
                {
                    var bar = new string('█', (int)(contributor.Percentage / 5));
                    Console.WriteLine($"  {contributor.Name,-25} - {contributor.CommitCount,3} commits ({contributor.Percentage,5:F1}%) {bar}");
                }

                Console.WriteLine();
                Console.WriteLine($"Total commits: {commits.Count}");
                Console.WriteLine($"Branch age: {branchAge.Days} days");

                // Additional useful info
                var uniqueContributors = contributorStats.Count;
                Console.WriteLine($"Unique contributors: {uniqueContributors}");

                if (branch.IsTracking)
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"✓ Tracking: {branch.TrackedBranch?.FriendlyName}");
                    Console.ResetColor();
                }
                else
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("⚠ Not tracking any remote branch");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: {ex.Message}");
                Console.ResetColor();
            }
        }
    }
}