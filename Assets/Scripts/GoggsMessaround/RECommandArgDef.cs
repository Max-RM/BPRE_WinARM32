/// <summary>
/// Argument definition for a command, not actually used while parsing, just to provide help to the user.
/// </summary>
public class RECommandArgDef
{
    public string Name;
    public string Type;
    public string Description;
    public RECommandArgDef(string name, string type, string description)
    {
        Name = name;
        Type = type;
        Description = description;
    }

    //overwrite to str so we can use it easily
    //example output:
    //Page:int
    //   Page to get help from.
    public override string ToString()
    {
        return $"({Name}:{Type})=>[{Description}]";
    }
}