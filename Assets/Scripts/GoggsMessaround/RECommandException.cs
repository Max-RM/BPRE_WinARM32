using System;

/// <summary>
/// Exception thrown when issues occur while parsing commands.
/// </summary>
[Serializable]
public class RECommandException : Exception
{
    public RECommandException()
    {
    }

    public RECommandException(string message) : base(message)
    {
    }

    public RECommandException(string message, Exception inner) : base(message, inner)
    {
    }

    protected RECommandException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context)
    {
    }
}