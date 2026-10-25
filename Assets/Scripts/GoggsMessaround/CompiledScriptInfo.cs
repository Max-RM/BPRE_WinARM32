/*
* Created by TKTek, at 2023/8/31 20:08
* may need to add more information fields in future?
*/
/*
 * TKTek, 2023/9/1 4:21:
 * this #if preprocessor command is used for my debugging?
 * Remove this #if preprocessor command if it's useless.
 */
#define GOGGS_SCRIPT_ENGINE
#if GOGGS_SCRIPT_ENGINE && !UNITY_WSA
using System;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace GoggsMessaround.CSharpScriptEngine
{
    public class CompiledScriptInfo
    {
        /// <summary>
        /// The script assembly of the compiled script
        /// </summary>
        public Assembly m_Assembly = null;
        /// <summary>
        /// <para>The compiled script's assembly bytes</para>
        /// <para>DON'T MODIFY THE DATA</para>
        /// </summary>
        public byte[] m_AssemblyBinaryData = null;
        /// <summary>
        /// <para>The compilation error occurred at the script compilation process</para>
        /// <para>If this field's value's null, the script compilation process is success</para>
        /// <para>If this field's value isn't null, the script compilation process is failed and the <see cref = "CompiledScriptInfo.m_Assembly"/>, <see cref = "CompiledScriptInfo.m_AssemblyBinaryData"/> field's value will be null</para>
        /// </summary>
        public ScriptCompilingException m_CompilationException = null;
        public IMethodSymbol m_EntryPoint = null;
        public MethodInfo m_ScriptEntryPointMethod = null;
        public Func<object> m_ScriptEntryPointMethodDelegate = null;
        public CompiledScriptInfo(Assembly assembly, byte[] assemblyBinaryData, IMethodSymbol entryPoint = null, MethodInfo scriptEntryMethod = null, Func<object> scriptEntryMethodDelegate = null)
        {
            this.m_Assembly = assembly;
            this.m_AssemblyBinaryData = assemblyBinaryData;
            this.m_EntryPoint = entryPoint;
            this.m_ScriptEntryPointMethodDelegate = scriptEntryMethodDelegate;
            this.m_ScriptEntryPointMethod = scriptEntryMethod;
        }

        public CompiledScriptInfo(ScriptCompilingException ex)
        {
            this.m_CompilationException = ex;
        }
    }
}
#endif
