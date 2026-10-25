using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>
/// Handles commands.
/// </summary>
public class RECommandHandler : MonoBehaviour
{
    private Dictionary<string, RECommand> commands = new Dictionary<string, RECommand>(); //command name (lowercase!) -> command class
    private Dictionary<string, BasePart> BasePartPool = new Dictionary<string, BasePart>();
    public void AddPartDefinition(string varName, string literal)
    {
        PartDefinition partDefinition = new PartDefinition(literal);
        BasePartPool.Add(varName, partDefinition.GetBasePart());
    }

    public bool TryAddPartDefinition(string varName, string literal)
    {
        try
        {
            PartDefinition partDefinition = new PartDefinition(literal);
            BasePartPool.Add(varName, partDefinition.GetBasePart());
            return true;
        }
        catch (Exception e)
        {
            return false;
        }
    }

    public BasePart GetPartDefinition(string varName)
    {
        return BasePartPool[varName];
    }

    public RECommandHandler()
    {
        //we register our help command here since it's special-ish
        RegisterCommand("help", "Provides help for a command.", new RECommandArgDef[] { new RECommandArgDef("Name", "String", "Name of the command to get help for.") }, (args) =>
        {
            if (!commands.TryGetValue(args.GetString(0).ToLower().Trim(), out RECommand command)) //we lower & trim since we ignore case (and trailing spaces etc)
                throw new RECommandException($"Command {args.GetString(0)} not found!");
            Console.WriteLine(command.GetHelpString());
        });
    }

    /// <summary>
    /// Registers a command to this handler.
    /// </summary>
    /// <param name = "name">Name of the command. (lowercase!)</param>
    /// <param name = "description">Description of the command.</param>
    /// <param name = "args">Arguments for the command (to print when the user asks for help)</param>
    /// <param name = "execute">Method to execute the command.</param>
    public void RegisterCommand(string name, string description, RECommandArgDef[] args, Action<RECommandArgs> execute)
    {
        commands.Add(name, new RECommand(name, description, args, execute));
    }

    /// <summary>
    /// Removes a command.
    /// </summary>
    /// <param name = "name">Name of the command to remove.</param>
    public void RemoveCommand(string name)
    {
        commands.Remove(name);
    }

    /// <summary>
    /// Runs a command previously registered.
    /// </summary>
    /// <param name = "text">Raw text to invoke the command from (e.g. "/gamerule x y")</param>
    /// <exception cref = "RECommandException"></exception>
    public void RunCommand(string text)
    {
        char firstChar = text.Normalize()[0];
        switch (firstChar)
        {
            case '/':
                break;
            case '#':
                HandleMultiLineCommand(text);
                return;
            default:
                Console.Write($"<{GameRules.GetGameRuleString("playername")}>: {text}\n");
                return;
        }

        string[] textParts = text.Normalize().TrimStart('/').Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); //Dart: maybe use "　" (U+3000) instead of " " for chinese? //Goggs: i guess i'll just use string.Normalize(). also we don't use U+3000
        if (!commands.TryGetValue(textParts[0].ToLower().Trim(), out RECommand command)) //we lower & trim since we ignore case (and trailing spaces etc)
        {
            Console.WriteLine($"Command {textParts[0]} not found!");
            throw new RECommandException($"Command {textParts[0]} not found!");
        }

        switch (command.Name)
        {
            case "executefile":
            {
                HandleFile(textParts[1]);
                return;
            }

            case "stop":
            {
                StopAllCoroutines();
                break;
            }
        }

        command.Execute(new RECommandArgs(textParts.Skip(1).ToArray())); //skip first part since that's the command name
    }

    public void HandleFile(string path)
    {
        string src = File.ReadAllText(path);
        src = src.Normalize().ToLower().Replace("#multiline", "").Replace("\r", "").Trim();
        bool isAutoAnimated = src.Contains("#animate");
        double autoAnimateTime = isAutoAnimated ? 0.01 : 0;
        string[] multiCommands = src.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        int executionIndex = 0;
        bool isExecuting = true;
        bool canExecuteNext = true;
        int waitUntil = 0;
        src = src.Replace("#animate", "");
        StartCoroutine(HandleFile(multiCommands, isAutoAnimated, autoAnimateTime));
    }

    public void HandleMultiLineCommand(string text)
    {
        text = text.Normalize().ToLower().Replace("#multiline", "").Replace("\r", "").Trim();
        isAutoAnimated = text.Contains("#animate");
        text = text.Replace("#animate", "");
        autoAnimateTime = isAutoAnimated ? 0.01 : 0;
        multiCommands = text.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        executionIndex = 0;
        isExecuting = true;
        canExecuteNext = true;
        waitUntil = 0;
    }

    public string[] multiCommands;
    public int executionIndex;
    public bool canExecuteNext;
    private bool isExecuting = false;
    private float waitUntil;
    public bool isAutoAnimated;
    public double autoAnimateTime;
    public void ExecuteNext()
    {
        string c = multiCommands[executionIndex];
        string[] textParts = c.TrimStart('/').Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); //Dart: maybe use "　" (U+3000) instead of " " for chinese? //Goggs: i guess i'll just use string.Normalize(). also we don't use U+3000
        if (!commands.TryGetValue(textParts[0].ToLower().Trim(), out RECommand command)) //we lower & trim since we ignore case (and trailing spaces etc)
        {
            Console.WriteLine($"Line {executionIndex + 1} : Command {textParts[0]} not found!");
            throw new RECommandException($"Line {executionIndex + 1} : Command {textParts[0]} not found!");
        }

        switch (command.Name)
        {
            case "delay":
            {
                float.TryParse(textParts[1], out float f);
                canExecuteNext = false;
                waitUntil = Time.realtimeSinceStartup + f;
                break;
            }

            case "frameinterval":
            {
                float.TryParse(textParts[1], out float f);
                autoAnimateTime = f;
                break;
            }

            case "executefile":
            {
                HandleFile(textParts[1]);
                break;
            }

            case "stop":
            {
                StopAllCoroutines();
                break;
            }
        }

        command.Execute(new RECommandArgs(textParts.Skip(1).ToArray()));
        executionIndex++;
    }

    private IEnumerator HandleFile(string[] multiCommands, bool isAutoAnimated = false, double autoAnimateTime = 0.01, /*just don't touch these two->>*/ bool canExecuteNext = true, bool isExecuting = true)
    {
        int executionIndex = 0;
        float waitUntil = 0;
        for (;;)
        {
            if (!isExecuting)
                break;
            switch (canExecuteNext)
            {
                case true:
                {
                    InternalExecuteNext();
                    if (executionIndex == multiCommands.Length)
                    {
                        isExecuting = false;
                        continue;
                    }

                    //----------------------------------------//
                    if (isAutoAnimated)
                    {
                        canExecuteNext = false;
                        waitUntil = Time.realtimeSinceStartup + (float)autoAnimateTime;
                    }

                    break;
                }

                case false:
                {
                    if (Time.realtimeSinceStartup >= waitUntil)
                    {
                        canExecuteNext = true;
                    }

                    yield return new WaitForRealSeconds((float)autoAnimateTime);
                    break;
                }
            }
        }

        yield return null;
        void InternalExecuteNext()
        {
            string c = multiCommands[executionIndex];
            string[] textParts = c.TrimStart('/').Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); //Dart: maybe use "　" (U+3000) instead of " " for chinese? //Goggs: i guess i'll just use string.Normalize(). also we don't use U+3000
            if (!commands.TryGetValue(textParts[0].ToLower().Trim(), out RECommand command)) //we lower & trim since we ignore case (and trailing spaces etc)
            {
                Console.WriteLine($"Line {executionIndex + 1} : Command {textParts[0]} not found!");
                throw new RECommandException($"Line {executionIndex + 1} : Command {textParts[0]} not found!");
            }

            switch (command.Name)
            {
                case "delay":
                {
                    float.TryParse(textParts[1], out float f);
                    canExecuteNext = false;
                    waitUntil = Time.realtimeSinceStartup + f;
                    break;
                }

                case "frameinterval":
                {
                    float.TryParse(textParts[1], out float f);
                    autoAnimateTime = f;
                    break;
                }

                case "executefile":
                {
                    HandleFile(textParts[1]);
                    break;
                }

                case "stop":
                {
                    StopAllCoroutines();
                    break;
                }
            }

            command.Execute(new RECommandArgs(textParts.Skip(1).ToArray()));
            executionIndex++;
        }
    }

    public void Update()
    {
        if (!isExecuting)
            return;
        switch (canExecuteNext)
        {
            case true:
            {
                ExecuteNext();
                if (executionIndex == multiCommands.Length)
                {
                    isExecuting = false;
                }

                //----------------------------------------//
                if (isAutoAnimated)
                {
                    canExecuteNext = false;
                    waitUntil = Time.realtimeSinceStartup + (float)autoAnimateTime;
                }

                break;
            }

            case false:
            {
                if (Time.realtimeSinceStartup >= waitUntil)
                {
                    canExecuteNext = true;
                }

                return;
            }
        }
    }
}