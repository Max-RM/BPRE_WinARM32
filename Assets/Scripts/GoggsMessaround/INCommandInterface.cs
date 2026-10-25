#if !UNITY_WSA
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using GoggsMessaround.CSharpScriptEngine;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class INCommandInterface : MonoBehaviour
{
    private ScriptEngine scriptEngine;
    public Text ConsoleInput;
    public Text ConsoleOutput;
    [FormerlySerializedAs("Enter")]
    public UnityEngine.UI.Button EnterButton;
    public UnityEngine.UI.Button ClearButton;
    public string randomClassName = ScriptEngine.RandomString(13, "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ");
    private int index = 0;
    private void Start()
    {
        scriptEngine = new ScriptEngine(ScriptEngine.GetDefaultConfig());
        EnterButton.onClick.AddListener(ExecuteCode);
        ClearButton.onClick.AddListener(ClearConsole);
    /*//testing
        var func = ScriptEngine.CompileAsFunc<double,double>("1d/Math.Sqrt(x)","x");
        string outut = "";
        int i = 1;
        while(i < 25)
        {
            outut+=func(i).ToString()+"\n";
            i++;
        }

        ConsoleOutput.text = outut;
        ConsoleOutput.text += func(func(func(func(func(func(func(func(func(func(func(func(func(145)))))))))))));
        */
    }

    private void ClearConsole()
    {
        ConsoleOutput.text = "";
    }

    private void ExecuteCode()
    {
        randomClassName = ScriptEngine.RandomString(13, "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ");
        string code = scriptEngine.WarpCodeSnippet(ConsoleInput.text, randomClassName);
        CompiledScriptInfo info = scriptEngine.Compile(code);
        Assembly nullableAssembly = info.m_Assembly;
        if (nullableAssembly != null)
        {
            object assemblyInstance = nullableAssembly.CreateInstance(randomClassName);
            //object ret = info.m_ScriptEntryPointMethodDelegate?.Invoke() ?? throw new InvalidOperationException();
            try
            {
                object returnValue = info.m_ScriptEntryPointMethodDelegate(); //null ref
                ConsoleOutput.text = returnValue.ToString();
            }
            catch (Exception e)
            {
                ConsoleOutput.text += "\n" + e.Message + e.StackTrace;
            }

            //INSettings.SetValue(INFeature.TerrainScale,whatever goes in here);
            Debug.Log("WOW HE WROTE A SUCCESSFUL CODE");
        }
        else
        {
            ConsoleOutput.text += ScriptEngine.ExceptionMassager;
        }

        index++;
    }
}
#endif