using System.CommandLine;
using DeployHelper.Commands;

var root = new RootCommand("Welcome to Deploy Helper CLI - Automate feature merges into env branches");

// ----- LIST COMMAND -----
var list = new Command("list", "List all feature branches");
list.SetHandler(() =>
{
    new ListBranchesCommand().Execute();
});
root.AddCommand(list);

// ----- CREATE COMMAND -----
var envOption = new Option<string>("--env", "Target environment (e.g., uat, qa, prod)") { IsRequired = true };
var baseOption = new Option<string>("--base", "Base branch (e.g., dev, main)") { IsRequired = true };
var branchesOption = new Option<string>("--branches", "Comma-separated feature branch names") { IsRequired = true };
var noPushOption = new Option<bool>("--no-push", description: "Skip pushing branch to origin", getDefaultValue: () => false);

var create = new Command("create", "Create environment-specific branch from selected features")
{
    envOption,
    baseOption,
    branchesOption,
    noPushOption
};

create.SetHandler(
    async (string env, string baseBranch, string branches, bool noPush) =>
    {
        var branchList = branches.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        await new CreateBranchCommand().ExecuteAsync(env, baseBranch, branchList, !noPush);
    },
    envOption, baseOption, branchesOption, noPushOption
);

root.AddCommand(create);

// ----- RUN -----
await root.InvokeAsync(args);
