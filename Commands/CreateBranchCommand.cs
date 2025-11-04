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
            using var repo = new Repository(Environment.CurrentDirectory);

            Console.WriteLine();
            Console.WriteLine($"Creating new environment branch for: {env}");
            Console.WriteLine($"Base branch: {baseBranch}");
            Console.WriteLine($"Merging feature branches: {string.Join(", ", featureBranches)}");

            //fetch latest refs first
            try
            {
                LibGit2Sharp.Commands.Fetch(repo, "origin", Array.Empty<string>(), new FetchOptions(), null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: fetch failed: {ex.Message}");
            }

            //ensure base branch exists (local or remote)
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
                Console.WriteLine($"Base branch '{baseBranch}' not found locally or on origin.");
                return;
            }

            //checkout base branch
            LibGit2Sharp.Commands.Checkout(repo, baseLocal);
            Console.WriteLine($"Checked out base branch: {baseBranch}");

            //Pull latest for base (fetch + merge origin/baseBranch)
            try
            {
                PullLatest(repo, baseBranch);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: pull latest for base failed: {ex.Message}");
            }

            var timestamp = DateTime.Now.ToString("ddMMyyyy_HHmm");
            var user = Environment.UserName.Split('\\').Last(); // keep username clean
            var newBranchName = $"{env}_feature_{user}_{timestamp}";

            //create and checkout new branch
            var newBranch = repo.CreateBranch(newBranchName);
            LibGit2Sharp.Commands.Checkout(repo, newBranch);
            Console.WriteLine($"Created and switched to new branch: {newBranchName}");

            //merge each feature branch (ensure pulled latest first)
            var signature = new Signature("DeployHelper", "deploy@local", DateTimeOffset.Now);

            foreach (var feature in featureBranches)
            {
                //attempt to find local branch; if not, try remote and create local tracking branch
                var featureLocal = repo.Branches.FirstOrDefault(b => b.FriendlyName == feature);
                if (featureLocal == null)
                {
                    var remoteFeature = repo.Branches.FirstOrDefault(b => b.FriendlyName == $"origin/{feature}");
                    if (remoteFeature != null)
                    {
                        featureLocal = repo.CreateBranch(feature, remoteFeature.Tip);
                    }
                }

                if (featureLocal == null)
                {
                    Console.WriteLine($"Feature branch not found: {feature}");
                    continue;
                }

                //fetch & pull latest for feature
                try
                {
                    LibGit2Sharp.Commands.Fetch(repo, "origin", new[] { feature }, new FetchOptions(), null);
                    PullLatest(repo, feature);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: fetch/pull for {feature} failed: {ex.Message}");
                }

                Console.WriteLine($"Merging branch: {feature}");
                var mergeResult = repo.Merge(featureLocal, signature, new MergeOptions());
                if (mergeResult.Status == MergeStatus.Conflicts)
                {
                    Console.WriteLine($"Conflicts detected while merging {feature}. Resolve manually and re-run or abort.");
                    return;
                }
                Console.WriteLine($"Merged branch: {feature}");
            }

            //push branch if requested (use git CLI or LibGit2Sharp with credentials)
            if (pushToOrigin)
            {
                try
                {
                    var remote = repo.Network.Remotes["origin"];
                    var pushRef = $"refs/heads/{newBranchName}";
                    repo.Network.Push(remote, pushRef, new PushOptions());
                    Console.WriteLine($"Pushed branch to origin: {newBranchName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Push failed: {ex.Message}");
                    Console.WriteLine("You can push manually if needed.");
                }
            }
            else
            {
                Console.WriteLine($"Branch created locally: {newBranchName}");
            }

            await Task.CompletedTask;
        }

        private static void PullLatest(Repository repo, string branchName)
        {
            //fetch done before calling this method
            var remoteBranch = repo.Branches.FirstOrDefault(b => b.FriendlyName == $"origin/{branchName}");
            if (remoteBranch == null)
                return;

            //ensure local branch exists
            var local = repo.Branches.FirstOrDefault(b => b.FriendlyName == branchName);
            if (local == null)
            {
                local = repo.CreateBranch(branchName, remoteBranch.Tip);
            }

            //merge origin/branch into local branch
            var signature = new Signature("DeployHelper", "deploy@local", DateTimeOffset.Now);
            var result = repo.Merge(remoteBranch, signature, new MergeOptions());
        }
    }
}
