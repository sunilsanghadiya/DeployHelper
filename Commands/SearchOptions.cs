using System;

namespace DeployHelper.Commands
{
    public class SearchOptions
    {
        public bool SearchCode { get; set; }
        public string? Author { get; set; }
        public string? BranchName { get; set; }
        public bool CaseSensitive { get; set; }
        public bool UseRegex { get; set; }
        public int? Limit { get; set; }
        public DateTimeOffset? Since { get; set; }
    }
}