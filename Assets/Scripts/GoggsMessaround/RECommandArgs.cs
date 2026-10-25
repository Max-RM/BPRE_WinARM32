using System;
using System.Text;

/// <summary>
/// Helper class that holds and wraps arguments passed to a command.
/// </summary>
public class RECommandArgs
{
    private string[] args; //backing string[] of args passed
    /// <summary>
    /// Creates a new RECommandArgs object, pulling args from the specified array.
    /// </summary>
    /// <param name = "args">Array of string arguments.</param>
    public RECommandArgs(string[] args)
    {
        this.args = args;
    }

    public string GetRawString()
    {
        StringBuilder sb = new StringBuilder();
        foreach (string argString in args)
        {
            sb.Append(argString);
        }

        return sb.ToString();
    }

    public string GetString(int at)
    {
        return args[at];
    }

    public string GetLowNormString(int at)
    {
        return args[at].Normalize().ToLower();
    }

    public int GetInt(int at)
    {
        if (!Int32.TryParse(args[at], out int value))
            throw new RECommandException($"Failed to parse int at {at}! Args: {GetArgsStr(args)}");
        return value;
    }

    public bool HasValue(int at)
    {
        return this.args.Length - 1 >= at;
    }

    public float GetFloat(int at)
    {
        if (!Single.TryParse(args[at], out float value))
            throw new RECommandException($"Failed to parse float at {at}! Args: {GetArgsStr(args)}");
        return value;
    }

    public uint GetUInt(int at)
    {
        if (!UInt32.TryParse(args[at], out uint value))
            throw new RECommandException($"Failed to parse unsigned int at {at}! Args: {GetArgsStr(args)}");
        return value;
    }

    public long GetLong(int at)
    {
        if (!Int64.TryParse(args[at], out long value))
            throw new RECommandException($"Failed to parse long at {at}! Args: {GetArgsStr(args)}");
        return value;
    }

    public ulong GetULong(int at)
    {
        if (!UInt64.TryParse(args[at], out ulong value))
            throw new RECommandException($"Failed to parse unsigned long at {at}! Args: {GetArgsStr(args)}");
        return value;
    }

    public T GetEnum<T>(int at)
        where T : Enum
    {
        //edit: fix typo
        //we assume the enum underlying type is int here, this shouldn't be *too* bad...
        if (!Int32.TryParse(args[at], out int intVal))
            throw new RECommandException($"Failed to parse enum int at {at}! Args: {GetArgsStr(args)}");
        //throw if the int isn't valid for that enum
        if (!Enum.IsDefined(typeof(T), intVal))
            throw new RECommandException($"{intVal} not valid enum value for {typeof(T).Name}! Args: {GetArgsStr(args)}");
        //return Unsafe.As<int, T>(ref intVal);
        //prefer one above, but not sure if unity supports Unsafe class (i hate boxing!)
        return (T)(object)intVal;
    }

    public bool GetBool(int at)
    {
        return bool.TryParse(args[at], out _);
    }

    public T GetAs<T>(int at)
    {
        return (T)Convert.ChangeType(args[at], typeof(T));
    }

    //private unsafe static T Int2Enum<T>(int val) where T : Enum
    //{
    //    return *(T*)&val;
    //}
    //get string for arg [] to print
    private static string GetArgsStr(string[] args)
    {
        StringBuilder sb = new StringBuilder();
        Array.ForEach<string>(args, (arg) =>
        {
            sb.Append(arg);
            sb.Append(", ");
        });
        string result = sb.ToString();
        //remove last ", " if there is one
        if (result.EndsWith(", "))
            result = result.Remove(result.Length - 2); //2 is obviously length of ", "
        return result;
    }
}