using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LibGit2Sharp;

namespace DeployHelper.Commands
{
    public class CreateBranchCommand
    {
        public async Task ExecuteAsync(string env, string baseBranch, List<string> featureBranches, bool pushToOrigin)
        {
            string originalBranch = null;

            try
            {
                using var repo = new Repository(Environment.CurrentDirectory);

                Console.WriteLine();
                Console.WriteLine($"Creating new environment branch for: {env}");
                Console.WriteLine($"Base branch: {baseBranch}");
                Console.WriteLine($"Merging feature branches: {string.Join(", ", featureBranches)}");

                // record current branch to restore later
                originalBranch = repo.Head.FriendlyName;

                // fetch latest refs first
                try
                {
                    LibGit2Sharp.Commands.Fetch(repo, "origin", Array.Empty<string>(), new FetchOptions(), null);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: fetch failed: {ex.Message}");
                }

                // ensure base branch exists (local or remote)
                Branch baseLocal = repo.Branches.FirstOrDefault(b => b.FriendlyName == baseBranch);
                if (baseLocal == null)
                {
                    var remoteRef = repo.Branches.FirstOrDefault(b => b.FriendlyName == $"origin/{baseBranch}");
                    if (remoteRef != null)
                    {
                        baseLocal = repo.CreateBranch(baseBranch, remoteRef.Tip);
                    }
                }

                if (baseLocal == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Base branch '{baseBranch}' not found locally or on origin.");
                    Console.ResetColor();
                    return;
                }

                // checkout base branch
                LibGit2Sharp.Commands.Checkout(repo, baseLocal);
                Console.WriteLine($"Checked out base branch: {baseBranch}");

                // pull latest base
                try
                {
                    PullLatest(repo, baseBranch);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: pull latest for base failed: {ex.Message}");
                }

                // generate safe branch name
                var timestamp = DateTime.Now.ToString("ddMMyyyy_HHmm");
                var user = Environment.UserName.Split('\\').Last();
                var newBranchName = $"{env}_feature_{user}_{timestamp}";

                // create and checkout new branch
                var newBranch = repo.CreateBranch(newBranchName);
                LibGit2Sharp.Commands.Checkout(repo, newBranch);
                Console.WriteLine($"Created and switched to new branch: {newBranchName}");

                var signature = new Signature("DeployHelper", "deploy@local", DateTimeOffset.Now);

                foreach (var feature in featureBranches)
                {
                    try
                    {
                        Console.WriteLine($"Merging branch: {feature}");

                        var featureLocal = repo.Branches.FirstOrDefault(b => b.FriendlyName == feature)
                                           ?? repo.Branches.FirstOrDefault(b => b.FriendlyName == $"origin/{feature}");

                        if (featureLocal == null)
                        {
                            Console.WriteLine($"Feature branch not found: {feature}");
                            continue;
                        }

                        // ensure latest
                        LibGit2Sharp.Commands.Fetch(repo, "origin", new[] { feature }, new FetchOptions(), null);

                        var mergeResult = repo.Merge(featureLocal, signature, new MergeOptions());

                        if (mergeResult.Status == MergeStatus.Conflicts)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Merge conflict detected in {feature}. Resolve manually and retry.");
                            Console.ResetColor();
                            return;
                        }

                        Console.WriteLine($"Merged branch: {feature}");
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"Warning: Failed to merge {feature}: {ex.Message}");
                        Console.ResetColor();
                    }
                }

                // push if requested
                if (pushToOrigin)
                {
                    try
                    {
                        var remote = repo.Network.Remotes["origin"];
                        repo.Network.Push(remote, $"refs/heads/{newBranchName}", new PushOptions());
                        Console.WriteLine($"Pushed branch to origin: {newBranchName}");
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"Warning: push failed: {ex.Message}");
                        Console.WriteLine("You can push manually if needed.");
                        Console.ResetColor();
                    }
                }
                else
                {
                    Console.WriteLine($"Branch created locally: {newBranchName}");
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error during branch creation: {ex.Message}");
                Console.ResetColor();
            }
            finally
            {
                try
                {
                    using var repo = new Repository(Environment.CurrentDirectory);
                    if (!string.IsNullOrEmpty(originalBranch) && repo.Head.FriendlyName != originalBranch)
                    {
                        LibGit2Sharp.Commands.Checkout(repo, originalBranch);
                        Console.WriteLine($"Restored to original branch: {originalBranch}");
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Warning: could not restore original branch: {ex.Message}");
                    Console.ResetColor();
                }
            }

            await Task.CompletedTask;
        }

        private static void PullLatest(Repository repo, string branchName)
        {
            var remoteBranch = repo.Branches.FirstOrDefault(b => b.FriendlyName == $"origin/{branchName}");
            if (remoteBranch == null) return;

            var local = repo.Branches.FirstOrDefault(b => b.FriendlyName == branchName)
                        ?? repo.CreateBranch(branchName, remoteBranch.Tip);

            var signature = new Signature("DeployHelper", "deploy@local", DateTimeOffset.Now);
            repo.Merge(remoteBranch, signature, new MergeOptions());
        }
    }
}
