using System;
using System.CommandLine;
using System.Linq;
using DeployHelper.Commands;

var root = new RootCommand("Welcome to Deploy Helper CLI - Automate feature merges and development workflows");

//LIST COMMAND
var list = new Command("list", "List all feature branches");
list.SetHandler(() =>
{
    new ListBranchesCommand().Execute();
});
root.AddCommand(list);

//CREATE COMMAND
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

//OWNER COMMAND
var branchArgument = new Argument<string>("branch", "Branch name to analyze");

var owner = new Command("owner", "Show who created and contributed to a branch")
{
    branchArgument
};

owner.SetHandler((string branch) =>
{
    new OwnerCommand().Execute(branch);
}, branchArgument);

root.AddCommand(owner);

//WHERE COMMAND
var whereArgument = new Argument<string>("feature-branch", "Feature branch to track across environments");

var where = new Command("where", "Check which environments contain a feature branch")
{
    whereArgument
};

where.SetHandler((string featureBranch) =>
{
    new WhereCommand().Execute(featureBranch);
}, whereArgument);

root.AddCommand(where);

var doctor = new Command("doctor", "Run repository health check");

doctor.SetHandler(() =>
{
    new DoctorCommand().Execute();
});

root.AddCommand(doctor);

var queryArgument = new Argument<string>("query", "Search query");
var codeOption = new Option<bool>("--code", "Search in code changes instead of commit messages");
var authorOption = new Option<string>("--author", "Filter by commit author");
var sinceOption = new Option<string>("--since", "Filter by date (e.g., '7d', '2024-01-01')");
var branchOption = new Option<string>("--branch", "Search in specific branch only");
var caseSensitiveOption = new Option<bool>("--case-sensitive", "Case-sensitive search");
var regexOption = new Option<bool>("--regex", "Use regex pattern");
var limitOption = new Option<int>("--limit", "Maximum number of results to show") { };

var search = new Command("search", "Search commits or code changes")
{
    queryArgument,
    codeOption,
    authorOption,
    sinceOption,
    branchOption,
    caseSensitiveOption,
    regexOption,
    limitOption
};

search.SetHandler((string query, bool code, string author, string since, string branch, bool caseSensitive, bool regex, int limit) =>
    {
        var options = new SearchOptions
        {
            SearchCode = code,
            Author = author,
            BranchName = branch,
            CaseSensitive = caseSensitive,
            UseRegex = regex,
            Limit = limit > 0 ? limit : null
        };

        if (!string.IsNullOrEmpty(since))
        {
            if (since.EndsWith("d"))
            {
                if (int.TryParse(since.TrimEnd('d'), out var days))
                {
                    options.Since = DateTimeOffset.Now.AddDays(-days);
                }
            }
            else if (DateTime.TryParse(since, out var date))
            {
                options.Since = new DateTimeOffset(date);
            }
        }

        new SearchCommand().Execute(query, options);
    },
    queryArgument, codeOption, authorOption, sinceOption, branchOption, caseSensitiveOption, regexOption, limitOption
);

root.AddCommand(search);

await root.InvokeAsync(args);