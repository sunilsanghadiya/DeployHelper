using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LibGit2Sharp;
using DeployHelper.Helpers;

namespace DeployHelper.Commands
{
    public class CreateBranchCommand
    {
        private readonly string[] _excludedBaseBranches = { "main", "master", "dev", "qa", "uat", "production" };

        public async Task ExecuteAsync(string env, string baseBranch, List<string> featureBranches, bool pushToOrigin)
        {
            //validation
            if (featureBranches == null || !featureBranches.Any())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: No feature branches provided.");
                Console.ResetColor();
                return;
            }

            Repository repo = null;
            string originalBranch = null;
            var failedBranches = new List<string>();

            try
            {
                repo = new Repository(Environment.CurrentDirectory);

                Console.WriteLine();
                Console.WriteLine($"Creating new environment branch for: {env}");
                Console.WriteLine($"Base branch: {baseBranch}");
                Console.WriteLine($"Merging feature branches: {string.Join(", ", featureBranches)}");

                //Record current branch to restore later
                originalBranch = repo.Head.FriendlyName;

                // Fetch latest refs once at the start
                Console.WriteLine("Fetching latest from origin...");
                if (!FetchFromOrigin(repo))
                {
                    Console.WriteLine("Warning: Proceeding with local refs only.");
                }

                // Ensure base branch exists and checkout
                var baseLocal = EnsureBaseBranch(repo, baseBranch);
                if (baseLocal == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Base branch '{baseBranch}' not found locally or on origin.");
                    Console.ResetColor();
                    return;
                }

                LibGit2Sharp.Commands.Checkout(repo, baseLocal);
                Console.WriteLine($"Checked out base branch: {baseBranch}");

                // Pull latest base
                if (!PullLatest(repo, baseBranch))
                {
                    Console.WriteLine("Warning: Could not pull latest base branch.");
                }

                // Generate safe branch name and check for conflicts
                var newBranchName = GenerateBranchName(env);
                if (repo.Branches[newBranchName] != null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error: Branch '{newBranchName}' already exists.");
                    Console.ResetColor();
                    return;
                }

                // Create and checkout new branch
                var newBranch = repo.CreateBranch(newBranchName);
                LibGit2Sharp.Commands.Checkout(repo, newBranch);
                Console.WriteLine($"Created and switched to new branch: {newBranchName}");

                // Get signature from git config or use default
                var signature = repo.Config.BuildSignature(DateTimeOffset.Now)
                                ?? new Signature("DeployHelper", "deploy@local", DateTimeOffset.Now);

                // Merge feature branches
                foreach (var feature in featureBranches)
                {
                    if (!MergeFeatureBranch(repo, feature, signature))
                    {
                        failedBranches.Add(feature);
                    }
                }

                // Report failures
                if (failedBranches.Any())
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"\nFailed to merge {failedBranches.Count} branch(es):");
                    foreach (var failed in failedBranches)
                    {
                        Console.WriteLine($"  - {failed}");
                    }
                    Console.ResetColor();
                }

                // Push if requested
                if (pushToOrigin)
                {
                    PushBranch(repo, newBranchName);
                }
                else
                {
                    Console.WriteLine($"\nBranch created locally: {newBranchName}");
                    Console.WriteLine($"To push later, run: git push -u origin {newBranchName}");
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nError during branch creation: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                Console.ResetColor();
            }
            finally
            {
                // Restore original branch
                if (repo != null && !string.IsNullOrEmpty(originalBranch))
                {
                    try
                    {
                        if (repo.Head.FriendlyName != originalBranch)
                        {
                            var originalBranchRef = repo.Branches[originalBranch];
                            if (originalBranchRef != null)
                            {
                                LibGit2Sharp.Commands.Checkout(repo, originalBranchRef);
                                Console.WriteLine($"\nRestored to original branch: {originalBranch}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"Warning: could not restore original branch: {ex.Message}");
                        Console.ResetColor();
                    }
                }

                repo?.Dispose();
            }

            await Task.CompletedTask;
        }

        private bool FetchFromOrigin(Repository repo)
        {
            try
            {
                LibGit2Sharp.Commands.Fetch(
                    repo,
                    "origin",
                    Array.Empty<string>(),
                    new FetchOptions
                    {
                        CredentialsProvider = (_url, _user, _cred) => new DefaultCredentials()
                    },
                    null
                );
                return true;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Fetch failed: {ex.Message}");
                Console.ResetColor();
                return false;
            }
        }

        private Branch EnsureBaseBranch(Repository repo, string baseBranch)
        {
            var baseLocal = repo.Branches.FirstOrDefault(b => b.FriendlyName == baseBranch);

            if (baseLocal == null)
            {
                var remoteRef = repo.Branches.FirstOrDefault(b => b.FriendlyName == $"origin/{baseBranch}");
                if (remoteRef != null)
                {
                    baseLocal = repo.CreateBranch(baseBranch, remoteRef.Tip);
                    Console.WriteLine($"Created local tracking branch for origin/{baseBranch}");
                }
            }

            return baseLocal;
        }

        private bool PullLatest(Repository repo, string branchName)
        {
            try
            {
                var remoteBranch = repo.Branches.FirstOrDefault(b => b.FriendlyName == $"origin/{branchName}");
                if (remoteBranch == null) return false;

                var signature = repo.Config.BuildSignature(DateTimeOffset.Now)
                                ?? new Signature("DeployHelper", "deploy@local", DateTimeOffset.Now);

                var mergeResult = repo.Merge(remoteBranch, signature, new MergeOptions
                {
                    FastForwardStrategy = FastForwardStrategy.Default
                });

                return mergeResult.Status != MergeStatus.Conflicts;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Pull failed: {ex.Message}");
                return false;
            }
        }

        private bool MergeFeatureBranch(Repository repo, string feature, Signature signature)
        {
            try
            {
                Console.WriteLine($"Merging branch: {feature}");

                var featureLocal = repo.Branches.FirstOrDefault(b => b.FriendlyName == feature)
                                   ?? repo.Branches.FirstOrDefault(b => b.FriendlyName == $"origin/{feature}");

                if (featureLocal == null)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"  Branch not found: {feature}");
                    Console.ResetColor();
                    return false;
                }

                var mergeResult = repo.Merge(featureLocal, signature, new MergeOptions
                {
                    FastForwardStrategy = FastForwardStrategy.Default,
                    FileConflictStrategy = CheckoutFileConflictStrategy.Merge
                });

                if (mergeResult.Status == MergeStatus.Conflicts)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  CONFLICT: Merge conflict detected in {feature}.");
                    Console.WriteLine($"  Please resolve manually and retry.");
                    Console.ResetColor();
                    return false;
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($" Merged: {feature}");
                Console.ResetColor();
                return true;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"   Failed to merge {feature}: {ex.Message}");
                Console.ResetColor();
                return false;
            }
        }

        private void PushBranch(Repository repo, string branchName)
        {
            try
            {
                Console.WriteLine($"\nPushing branch to origin...");
                var remote = repo.Network.Remotes["origin"];
                repo.Network.Push(remote, $"refs/heads/{branchName}", new PushOptions
                {
                    CredentialsProvider = (_url, _user, _cred) => new DefaultCredentials()
                });

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($" Successfully pushed branch to origin: {branchName}");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Push failed: {ex.Message}");
                Console.WriteLine($"You can push manually with: git push -u origin {branchName}");
                Console.ResetColor();
            }
        }

        private string GenerateBranchName(string env)
        {
            var timestamp = DateTime.Now.ToString("ddMMyyyy-HHmm");
            var user = Environment.UserName.Split('\\').Last().ToLowerInvariant();
            return $"{env}/{user}/{timestamp}";
        }
    }
}