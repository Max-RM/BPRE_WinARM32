/*
 * Script engine configure information class, created by TKTek at 2023/8/31
 */
/*
 * TKTek, 2023/8/31 21:42:
 * this #if preprocessor command is used for my debugging?
 * Remove this #if preprocessor command if it's useless.
 */
#define GOGGS_SCRIPT_ENGINE
#if GOGGS_SCRIPT_ENGINE && !UNITY_WSA
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace GoggsMessaround.CSharpScriptEngine
{
    public class ScriptEngineConfig
    {
        public IEnumerable<AssemblyReference> InitialAssemblyReferences;
        public List<AssemblyReference> AssemblyReferences;
        public List<string> Usings;
        public List<string> PreprocessorSymbols;
        public bool fAllowUnsafeBlocks;
        public bool fCheckOverflow;
        public LanguageVersion fCSharpLanguageVersion;
        public SourceCodeKind fCSharpCodeKind;
        public OptimizationLevel fCompilationOptimizationLevel;
        public Platform fCompilationPlatform;
#warning TKTed: I THINK I SHOULD FILL MORE ARGUMENTS HERE!
        public ScriptEngineConfig(IEnumerable<AssemblyReference> initialAssemblyReferences = null, IEnumerable<string> usings = null, IEnumerable<string> preprocessorSymbols = null, bool allowUnsafeBlocks = true, bool checkOverflow = false, LanguageVersion CSharpLanguageVersion = LanguageVersion.Latest, SourceCodeKind codeKind = SourceCodeKind.Script, OptimizationLevel compilationOptimizationLevel = OptimizationLevel.Debug, Platform compilationPlatform = Platform.AnyCpu)
        {
            this.InitialAssemblyReferences = (initialAssemblyReferences == null) ? new List<AssemblyReference>() : new List<AssemblyReference>(initialAssemblyReferences);
            this.AssemblyReferences = (initialAssemblyReferences == null) ? new List<AssemblyReference>() : new List<AssemblyReference>(initialAssemblyReferences);
            this.Usings = (usings == null) ? new List<string>() : new List<string>(usings);
            this.PreprocessorSymbols = (preprocessorSymbols == null) ? new List<string>() : new List<string>(preprocessorSymbols);
            this.fAllowUnsafeBlocks = allowUnsafeBlocks;
            this.fCheckOverflow = checkOverflow;
            this.fCSharpLanguageVersion = CSharpLanguageVersion;
            this.fCSharpCodeKind = codeKind;
            this.fCompilationOptimizationLevel = compilationOptimizationLevel;
            this.fCompilationPlatform = compilationPlatform;
        }
    }
}
#endif
