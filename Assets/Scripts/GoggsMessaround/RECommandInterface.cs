using System;
using System.IO;
using UnityEngine;
using System.Text;
using Unity.Mathematics;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

public class RECommandInterface : MonoBehaviour
{
    [SerializeField]
    private GameObject m_content;
    [SerializeField]
    private UnityEngine.UI.Text m_commandOutput;
    [SerializeField]
    private UnityEngine.UI.InputField m_commandInput;
    [SerializeField]
    private UnityEngine.UI.Button m_executeButton;
    [FormerlySerializedAs("m_runButton")]
    [SerializeField]
    private UnityEngine.UI.Button m_helpButton;
    [SerializeField]
    public RECommandHandler ReCommandHandler;
    [SerializeField]
    public GameObject ExecutorDummy;
    [SerializeField]
    public UnityEngine.UI.Button OpenExtraMenu;
    [SerializeField]
    public UnityEngine.UI.Button CloseExtraMenu;
    public bool IsChanged { get; private set; }
    public static RECommandInterface Instance { get; private set; }

    private TextWriter m_commandOut;
    private StringBuilder m_logBuilder;
    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(this);
        ExecutorDummy = INAppInterface.Instance.ExecutorDummy;
        m_executeButton.onClick.AddListener(Execute);
        m_helpButton.onClick.AddListener(Help);
        m_logBuilder = new StringBuilder();
        ReCommandHandler = ExecutorDummy.GetComponent<RECommandHandler>();
        ReCommandHandler.RegisterCommand("gamerule", "Gets or Sets a gamerule", new[] { new RECommandArgDef("Mode", "String", "Select mode (Get or Set)"), new RECommandArgDef("Gamerule", "String", "Select a gamerule"), new RECommandArgDef("Value", "String", "(only for set mode)The value to be set") }, (args) =>
        {
            //we're starting from 0 since we skip the first part in execution
            string mode = args.GetLowNormString(0);
            string gamerule = args.GetLowNormString(1);
            if (mode == "get")
            {
                Console.WriteLine($"Value of gamerule {args.GetString(1)} is {GameRules.GetGameRuleString(gamerule)}.");
            }
            else if (mode == "set")
            {
                Console.WriteLine($"Set gamerule {args.GetString(1)} to {args.GetString(2)}.");
                GameRules.SetGameRule(gamerule, args.GetLowNormString(2));
            }
        });
        ReCommandHandler.RegisterCommand("shift", "Shifts your contraption on grid", new[] { new RECommandArgDef("X-Shift", "Integer", "Shift amount on X-Axis"), new RECommandArgDef("Y-Shift", "Integer", "Shift amount on Y-Axis") }, (args) =>
        {
            int dx = args.GetInt(0);
            int dy = args.GetInt(1);
            if (BPManual.Contraption)
            {
                BPManual.Contraption.MoveOnGrid(dx, dy);
                Console.WriteLine(INLocalization.Instance.GetText("CommandInterface_Shift"), dx, dy);
            }
            else
            {
                Console.WriteLine(INLocalization.Instance.GetText("CommandInterface_ContraptionNotFound"));
            }
        });
        ReCommandHandler.RegisterCommand("setpart", "sets a new part at certain location", new[] { new RECommandArgDef("x", "Float", "x Coordinate of new part"), new RECommandArgDef("y", "Float", "y Coordinate of new part"), new RECommandArgDef("type", "Integer", "part type of new part (-1 for removal)"), new RECommandArgDef("custom index", "Integer", "skin value of new part"), new RECommandArgDef("grid rotation", "Integer[..7]", "grid rotation value of new part"), new RECommandArgDef("is it flipped", "Boolean as Integer", "determine if new part is flipped"), }, (args) =>
        {
            if (Contraption.Instance && WPFMonoBehaviour.levelManager && WPFMonoBehaviour.levelManager.ConstructionUI)
            {
                if ((((WPFMonoBehaviour.levelManager.gameState == LevelManager.GameState.Running) || (WPFMonoBehaviour.levelManager.gameState == LevelManager.GameState.PausedWhileRunning)) || (WPFMonoBehaviour.levelManager.gameState == LevelManager.GameState.PreviewWhileRunning)))
                { /*
                        ContraptionDataset.ContraptionDatasetUnit unit = new ContraptionDataset.ContraptionDatasetUnit
                        {
                            x = args.GetInt(0),
                            y = args.GetInt(1),
                            partType = args.GetInt(2),
                            customPartIndex = args.GetInt(3),
                            rot = args.GetInt(4),
                            flipped = args.GetBool(5)
                        };
                        BasePart customPart = WPFMonoBehaviour.gameData.GetCustomPart(((SortedPartType)args.GetInt(2)).ToPartType(),
                            unit.customPartIndex);
                    
                        if (customPart != null)
                        {
                            Contraption.Instance.Parts.Add(customPart);
                            customPart.contraption = Contraption.Instance;
                            customPart.enabled = true;
                            customPart.Initialize();
                            customPart.PostInitialize();
                            GameObject obj = Object.Instantiate(customPart.gameObject);
                            obj.SetActive(value: true);
                            customPart.EnsureRigidbody();
                        }
                        Console.WriteLine(INLocalization.Instance.GetText("CommandInterface_SetPart"), args.GetInt(0),
                            args.GetInt(1), args.GetInt(2), args.GetInt(3), args.GetInt(4), args.GetBool(5));*/
                    Console.WriteLine(INLocalization.Instance.GetText("CommandInterface_SetPartFailed"));
                    return;
                }
                else if ((((WPFMonoBehaviour.levelManager.gameState == LevelManager.GameState.Building) || (WPFMonoBehaviour.levelManager.gameState == LevelManager.GameState.PausedWhileBuilding)) || (WPFMonoBehaviour.levelManager.gameState == LevelManager.GameState.PreviewWhileBuilding)))
                {
                    float rx = args.GetFloat(0);
                    float ry = args.GetFloat(1);
                    int x = (int)math.floor(rx);
                    int y = (int)math.floor(ry);
                    float offsetX = rx - x;
                    float offsetY = ry - y;
                    if (offsetX > 0.5f)
                    {
                        offsetX -= 1;
                        x++;
                    }

                    if (offsetX < -0.5f)
                    {
                        offsetX = 1 - offsetX;
                        x--;
                    }

                    if (offsetY > 0.5f)
                    {
                        offsetY -= 1;
                        y++;
                    }

                    if (offsetY < -0.5f)
                    {
                        offsetY = 1 - offsetY;
                        y--;
                    }

                    if (args.GetInt(2) <= -1)
                    {
                        Contraption.Instance.RemovePartAt(x, y);
                        return;
                    }

                    int intPartType = args.GetInt(2);
                    int customPartIndex = args.GetInt(3);
                    int rotation = args.GetInt(4);
                    bool isFlipped = args.GetBool(5);
                    BasePart.PartType partType = ((SortedPartType)intPartType).ToPartType();
                    Contraption.Instance.DataSet.AddPart(x, y, intPartType, customPartIndex, rotation, isFlipped, out ContraptionDataset.ContraptionDatasetUnit unit, offsetX, offsetY);
                    ConstructionUI.PartDesc partDesc = WPFMonoBehaviour.levelManager.ConstructionUI.FindPartDesc(partType);
                    BasePart customPart = WPFMonoBehaviour.gameData.GetCustomPart(partType, unit.customPartIndex);
                    if (customPart != null)
                    {
                        WPFMonoBehaviour.levelManager.BuildPart(unit, customPart);
                    }

                    partDesc.useCount++;
                    Console.WriteLine(INLocalization.Instance.GetText("CommandInterface_SetPart"), x, y, intPartType, customPartIndex, rotation, isFlipped);
                }
            }
            else
            {
                Console.WriteLine(INLocalization.Instance.GetText("CommandInterface_ContraptionNotFound"));
            }
        });
        ReCommandHandler.RegisterCommand("#multiline", "execute following lines of code line-by-line", new[] { new RECommandArgDef("commands", "Command[]", "Multiple Lines of Command") }, (args) =>
        {
            ReCommandHandler.HandleMultiLineCommand(args.GetRawString());
        });
        ReCommandHandler.RegisterCommand("delay", "delay for a Time Span (seconds), only used with #multiline", new[] { new RECommandArgDef("delay amount", "Float", "How long to delay in seconds") }, (args) =>
        {
        });
        ReCommandHandler.RegisterCommand("executefile", "execute a command from a file, not multi thread", new[] { new RECommandArgDef("path", "String", "The absolute path of the file") }, (args) =>
        {
        });
        ReCommandHandler.RegisterCommand("stop", "stop all executions", new RECommandArgDef[] { }, (args) =>
        {
        });
        ReCommandHandler.RegisterCommand("frameinterval", "delay for a Time Span for next frame, only used with #multiline and #animate", new[] { new RECommandArgDef("delay amount", "Float", "How long to delay in seconds") }, (args) =>
        {
        });
        ReCommandHandler.RegisterCommand("fill", "fills an rectangular area with a new part", new[] { new RECommandArgDef("x1", "Integer", "x Coordinate of rectangle vertex1"), new RECommandArgDef("y1", "Integer", "y Coordinate of rectangle vertex1"), new RECommandArgDef("x2", "Integer", "x Coordinate of rectangle vertex2"), new RECommandArgDef("y2", "Integer", "y Coordinate of rectangle vertex2"), new RECommandArgDef("type", "Integer", "part type of new part"), new RECommandArgDef("custom index", "Integer", "skin value of new part"), new RECommandArgDef("grid rotation", "Integer[..7]", "grid rotation value of new part"), new RECommandArgDef("is it flipped", "Boolean as Integer", "determine if new part is flipped") }, (args) =>
        {
            if (Contraption.Instance && WPFMonoBehaviour.levelManager && WPFMonoBehaviour.levelManager.ConstructionUI)
            {
                int y;
                if (!((((WPFMonoBehaviour.levelManager.gameState == LevelManager.GameState.Building) || (WPFMonoBehaviour.levelManager.gameState == LevelManager.GameState.PausedWhileBuilding)) || (WPFMonoBehaviour.levelManager.gameState == LevelManager.GameState.PreviewWhileBuilding))))
                {
                    Console.WriteLine(INLocalization.Instance.GetText("CommandInterface_SetPartFailed"));
                    return;
                }

                int intPartType = args.GetInt(4);
                int x1 = args.GetInt(0);
                int x2 = args.GetInt(2);
                int y1 = args.GetInt(1);
                int y2 = args.GetInt(3);
                int customPartIndex = args.GetInt(5);
                int rotation = args.GetInt(6);
                bool isFlipped = args.GetBool(7);
                BasePart.PartType partType = ((SortedPartType)intPartType).ToPartType();
                ConstructionUI.PartDesc partDesc = WPFMonoBehaviour.levelManager.ConstructionUI.FindPartDesc(((SortedPartType)intPartType).ToPartType());
                partDesc.useCount += (Math.Max(x1, x2) - Math.Min(x1, x2)) * (Math.Max(y1, y2) - Math.Min(y1, y2));
                //artistic code editing
                ContraptionDataset.ContraptionDatasetUnit unit = new ContraptionDataset.ContraptionDatasetUnit
                {
                    partType = intPartType,
                    customPartIndex = customPartIndex,
                    rot = rotation,
                    flipped = isFlipped
                };
                for (int x = Math.Min(x1, x2); x <= Math.Max(x1, x2); x++)
                {
                    for (y = Math.Min(y1, y2); y <= Math.Max(y1, y2); y++)
                    {
                        unit.x = x;
                        unit.y = y;
                        Contraption.Instance.DataSet.AddPart(x, y, x2, y2, intPartType, isFlipped, out ContraptionDataset.ContraptionDatasetUnit _, 0, 0);
                        BasePart customPart = WPFMonoBehaviour.gameData.GetCustomPart(partType, unit.customPartIndex);
                        if (customPart != null)
                        {
                            WPFMonoBehaviour.levelManager.BuildPart(unit, customPart);
                        }
                    }
                }

                Console.WriteLine(INLocalization.Instance.GetText("CommandInterface_FillPart"), x1, y1, x2, y2, intPartType, customPartIndex, rotation, isFlipped);
            }
            else
            {
                Console.WriteLine(INLocalization.Instance.GetText("CommandInterface_ContraptionNotFound"));
            }
        });
        ReCommandHandler.RegisterCommand("clear", "Clears command display", Array.Empty<RECommandArgDef>(), (args) =>
        {
            Clear();
        });
        ReCommandHandler.RegisterCommand("storage", "Storage a Part in memory", new RECommandArgDef[] { new RECommandArgDef("multiline part definition/多行部件定义", "Part Definition", "Part describer"), new RECommandArgDef("___OR USE OLDER PART DEFINITION BELOW___", "Alternative Argument Set", "Do not use both"), new RECommandArgDef("x", "Float", "x Coordinate of new part"), new RECommandArgDef("y", "Float", "y Coordinate of new part"), new RECommandArgDef("type", "Integer", "part type of new part"), new RECommandArgDef("custom index", "Integer", "skin value of new part"), new RECommandArgDef("grid rotation", "Integer[..7]", "grid rotation value of new part"), new RECommandArgDef("is it flipped", "Boolean as Integer", "determine if new part is flipped") }, (args) =>
        {
            string varName = args.GetString(0);
            string data = args.GetRawString().Substring(varName.Length);
            PartDefinition partDefinition = new PartDefinition();
            bool success = ReCommandHandler.TryAddPartDefinition(varName, data);
            if (!success)
            {
                float rx = args.GetFloat(0);
                float ry = args.GetFloat(1);
                int intPartType = args.GetInt(2);
                int customPartIndex = args.GetInt(3);
                int rotation = args.GetInt(4);
                bool isFlipped = args.GetBool(5);
                BasePart.PartType partType = ((SortedPartType)intPartType).ToPartType();
                BasePart basePart = partDefinition.GetBasePart(rx, ry, customPartIndex, partType, isFlipped, rotation);
            }
        });
    }

    private void Execute()
    {
        if (GameRules.ShowCommandLog)
        {
            m_commandOut = Console.Out;
            m_logBuilder.Clear();
            Console.SetOut(new StringWriter(m_logBuilder));
        }

        Application.logMessageReceived += DebugLogReader;
        try
        {
            ReCommandHandler.RunCommand(m_commandInput.text);
        }
        catch (RECommandException e)
        {
            Console.WriteLine(e.Message);
        }
        catch (Exception e)
        {
        // ignored
        }

        if (!GameRules.ShowCommandLog)
            return;
        Application.logMessageReceived -= DebugLogReader;
        m_commandOutput.text += m_logBuilder.ToString();
        Console.SetOut(m_commandOut);
    }

    private void Help()
    {
        m_commandOut = Console.Out;
        m_logBuilder.Clear();
        Console.SetOut(new StringWriter(m_logBuilder));
        Console.WriteLine(BPManual.GetHelpPage("0"));
        m_commandOutput.text += m_logBuilder.ToString();
        Console.SetOut(m_commandOut);
    }

    private void Clear()
    {
        m_commandOutput.text = "";
    }

    private void DebugLogReader(string logMessage, string stackTrace, LogType logType)
    {
        m_logBuilder.AppendLine(logMessage);
    }
}/*
#multiline
#animate
/frameinterval 0.001
/delay 3
/setpart 0 0 6 42 0 0
/setpart 0 1 6 42 0 0
/setpart 0 2 6 42 0 0
/setpart 0 3 6 42 0 0
/setpart 0 4 6 42 0 0
/setpart 0 5 6 42 0 0
/setpart 1 0 6 42 0 0
/setpart 1 1 6 42 0 0
/setpart 1 2 6 42 0 0
/setpart 1 3 6 42 0 0
/setpart 1 4 6 42 0 0
/setpart 1 5 6 42 0 0
/setpart 2 0 6 42 0 0
/setpart 2 1 6 42 0 0
/setpart 2 2 6 42 0 0
/setpart 2 3 6 42 0 0
/setpart 2 4 6 42 0 0
/setpart 2 5 6 42 0 0
/setpart 3 0 6 42 0 0
/setpart 3 1 6 42 0 0
/setpart 3 2 6 42 0 0
/setpart 3 3 6 42 0 0
/setpart 3 4 6 42 0 0
/setpart 3 5 6 42 0 0
/setpart 4 0 6 42 0 0
/setpart 4 1 6 42 0 0
/setpart 4 2 6 42 0 0
/setpart 4 3 6 42 0 0
/setpart 4 4 6 42 0 0
/setpart 4 5 6 42 0 0
/setpart 5 0 6 42 0 0
/setpart 5 1 6 42 0 0
/setpart 5 2 6 42 0 0
/setpart 5 3 6 42 0 0
/setpart 5 4 6 42 0 0
/setpart 5 5 6 42 0 0
/setpart 6 0 6 42 0 0
/setpart 6 1 6 42 0 0
/setpart 6 2 6 42 0 0
/setpart 6 3 6 42 0 0
/setpart 6 4 6 42 0 0
/setpart 6 5 6 42 0 0
*/
