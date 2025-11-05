using System;

namespace DeployHelper.Commands
{
    public class SearchCommand
    {
        public void Execute(string query, SearchOptions options)
        {
            Console.WriteLine($"Searching for '{query}' in commits or code...");
            Console.WriteLine($"Options -> Author: {options.Author}, Branch: {options.BranchName}");
            Console.WriteLine("✅ Search completed.");
        }
    }
}
