using System;

namespace DeployHelper.Commands
{
    public class WhereCommand
    {
        public void Execute(string featureBranch)
        {
            Console.WriteLine($"Checking environments that contain feature branch: {featureBranch}");
        }
    }
}
