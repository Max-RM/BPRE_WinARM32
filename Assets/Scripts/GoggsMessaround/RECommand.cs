using System;
using System.Text;

public class RECommand
{
    public string Name;
    public string Description;
    public RECommandArgDef[] ArgDefs; //argument definitions to print for help
    public Action<RECommandArgs> Execute; //called with the specified args when the command is run
    public RECommand(string name, string description, RECommandArgDef[] argDefs, Action<RECommandArgs> execute)
    {
        Name = name;
        Description = description;
        ArgDefs = argDefs;
        Execute = execute;
    }

    /// <summary>
    /// Gets the help string for this command.
    /// </summary>
    /// <returns>The help string.</returns>
    public string GetHelpString()
    {
        StringBuilder sb = new StringBuilder();
        //command name & description
        sb.Append($"Help for {Name}:\n");
        sb.Append($"   {Description}");
        //add all argument definitions
        sb.Append("Arguments:\n");
        foreach (RECommandArgDef argDef in ArgDefs)
        {
            sb.Append($"   {argDef}\n");
        }

        return sb.ToString();
    }
}