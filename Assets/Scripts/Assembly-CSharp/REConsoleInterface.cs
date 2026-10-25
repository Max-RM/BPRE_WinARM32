#if !UNITY_WSA
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Linq;
using UnityEngine;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System.Text;

public class REConsoleInterface : MonoBehaviour
{
    [SerializeField]
    private GameObject m_content;
    [SerializeField]
    private UnityEngine.UI.InputField m_consoleCode;
    [SerializeField]
    private UnityEngine.UI.InputField m_consoleOutput;
    [SerializeField]
    private UnityEngine.UI.Button m_compileButton;
    [SerializeField]
    private UnityEngine.UI.Button m_runButton;
    [SerializeField]
    private string m_assemblyName;
    [SerializeField]
    private string m_className;
    [SerializeField]
    private string m_methodName;
    public string AssemblyName => m_assemblyName;
    public string ClassName => m_className;
    public string MethodName => m_methodName;
    public bool IsChanged { get; private set; }
    public static REConsoleInterface Instance { get; private set; }

    private TextWriter m_consoleOut;
    private StringBuilder m_logBuilder;
    private Assembly m_programAssembly;
    private Type m_programType;
    private MethodInfo m_programMethod;
    private MonoBehaviour m_programInstance;
    private GameObject m_programObject;
    private void Awake()
    {
        Instance = this;
        m_consoleCode.text = INLocalization.Instance.GetText("ConsoleInterface_TextArea");
        m_compileButton.onClick.AddListener(Compile);
        m_runButton.onClick.AddListener(Run);
        m_logBuilder = new StringBuilder();
    }

    private void Compile()
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(m_consoleCode.text);
        List<MetadataReference> m_metadatareferences = new List<MetadataReference>();
#if PLATFORM_ANDROID
		Console.WriteLine("Can't Find MetadataReferences on Android!!!!!!!!!!!!!! Roslyn won't work");
#endif
        Console.WriteLine("Compiling...");
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies().Where(assembly => !assembly.IsDynamic))
        {
            if (assembly.Location.Length != 0)
            {
                try
                {
                    m_metadatareferences.Add(MetadataReference.CreateFromFile(assembly.Location));
                }
                catch (Exception exception)
                {
                    Debug.Log(INLocalization.Instance.GetText("ConsoleInterface_PreCompileErrorMetadataReference") + exception.Message);
                }
            }
        }

        CSharpCompilation compilation = CSharpCompilation.Create(assemblyName: m_assemblyName, syntaxTrees: new[] { syntaxTree }, references: m_metadatareferences, options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.Default).WithAllowUnsafe(true).WithOptimizationLevel(OptimizationLevel.Debug));
        MemoryStream memoryStream = new MemoryStream();
        EmitResult result = compilation.Emit(memoryStream);
        if (!result.Success)
        {
            m_consoleOutput.text = INLocalization.Instance.GetText("ConsoleInterface_CompileFailed");
            foreach (Diagnostic diagnostic in
                from d in result.Diagnostics
                where d.Severity == DiagnosticSeverity.Error
                select d)
            {
                m_consoleOutput.text += '\n' + diagnostic.ValueToString();
            }

            return;
        }

        memoryStream.Seek(0, SeekOrigin.Begin);
        byte[] m_assemblyData = memoryStream.ToArray();
        memoryStream.Close();
        m_programAssembly = Assembly.Load(m_assemblyData);
        m_programType = m_programAssembly.GetType(m_className);
        if (m_programType == null)
        {
            m_consoleOutput.text = INLocalization.Instance.GetText("ConsoleInterface_PostCompileErrorGetType");
            return;
        }

        if (m_programInstance != null)
        {
            Destroy(m_programInstance);
        }

        m_programInstance = gameObject.AddComponent(m_programType) as MonoBehaviour;
        if (m_programInstance == null)
        {
            m_consoleOutput.text = INLocalization.Instance.GetText("ConsoleInterface_PostCompileErrorInitType");
            return;
        }

        m_programMethod = m_programType.GetMethod(m_methodName);
        if (m_programMethod == null)
        {
            m_consoleOutput.text = INLocalization.Instance.GetText("ConsoleInterface_PostCompileErrorGetMethod");
            return;
        }

        m_consoleOutput.text = INLocalization.Instance.GetText("ConsoleInterface_CompileSuccess");
    }

    private void Run()
    {
        m_consoleOut = Console.Out;
        m_logBuilder.Clear();
        Console.SetOut(new StringWriter(m_logBuilder));
        Application.logMessageReceived += DebugLogReader;
        if (m_programAssembly == null || m_programType == null || m_programMethod == null || m_programInstance == null)
        {
            m_consoleOutput.text = INLocalization.Instance.GetText("ConsoleInterface_InvalidCompile");
            goto end;
        }

        m_programObject = new GameObject();
        object returnValue = null;
        try
        {
            returnValue = m_programMethod.Invoke(m_programObject, new object[] { });
        }
        catch (Exception exception)
        {
            m_consoleOutput.text = INLocalization.Instance.GetText("ConsoleInterface_RuntimeException") + exception.Message + "\nNote: It may not be possible to get the actual exception that occurred";
            goto end;
        }

        m_consoleOutput.text = m_logBuilder.ToString();
        try
        {
            m_consoleOutput.text += INLocalization.Instance.GetText("ConsoleInterface_ReturnValue") + returnValue;
        }
        catch (Exception exception)
        {
            m_consoleOutput.text += INLocalization.Instance.GetText("ConsoleInterface_RuntimeErrorReturnConvert") + exception.Message;
        }

        end:
            if (m_programObject != null)
            {
                Destroy(m_programObject);
            }

        Application.logMessageReceived -= DebugLogReader;
        Console.SetOut(m_consoleOut);
    }

    private void DebugLogReader(string logMessage, string stackTrace, LogType logType)
    {
        m_logBuilder.AppendLine(logMessage);
    }
}
#endif