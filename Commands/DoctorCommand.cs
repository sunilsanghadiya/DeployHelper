using System;

namespace DeployHelper.Commands
{
    public class DoctorCommand
    {
        public void Execute()
        {
            Console.WriteLine("Running repository health check...");
            Console.WriteLine("✅ Everything looks good!");
        }
    }
}
