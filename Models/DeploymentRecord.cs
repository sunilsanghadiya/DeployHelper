using System;
using System.Collections.Generic;

public class DeploymentRecord
{
    public string Environment { get; set; } = "";
    public string BaseBranch { get; set; } = "";
    public List<string> Branches { get; set; } = new();
    public string NewBranch { get; set; } = "";
    public string CreatedBy { get; set; } = "";
    public DateTime Timestamp { get; set; }
}