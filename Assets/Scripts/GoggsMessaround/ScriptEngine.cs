/*
 * Original code file written by Goggs.
 * Heavy-Modified by TKTek at 2023/9/1
 */
/*
 * TKTek, 2023/8/31 18:14:
 * this #if preprocessor command is used for my debugging?
 * Remove this #if preprocessor command if it's useless.
 */
#define GOGGS_SCRIPT_ENGINE
#if GOGGS_SCRIPT_ENGINE && !UNITY_WSA
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using UnityEngine;

namespace GoggsMessaround.CSharpScriptEngine
{
    public class ScriptEngine
    {
        //TKTek note, at 2023/8/31 19:11 : ExceptionMassager => ExceptionMessenger, plz
        public static string ExceptionMassager { get; set; } = null;

        protected static RandomNumberGenerator randomNumberGenerator = RandomNumberGenerator.Create();
        /*
		 * These variants're created by TKTek at 2023/8/31
		 */
        protected ScriptEngineConfig m_config;
        //#warning NEVER ENLISTED COMPILED SCRIPT INFO INTO THIS POOL.
        //oh ScriptEngine.cs:189
        protected List<CompiledScriptInfo> m_PreviousCompiledScriptPool;
        protected CSharpCompilation m_PreviousCompilation = null;
        protected object[] m_submissionStates = new object[]
        {
            null,
            null
        };
        protected int m_submissionIndex = 0;
        public IEnumerable<CompiledScriptInfo> PrevCompiledScriptPool => this.m_PreviousCompiledScriptPool;
        public ScriptEngineConfig Config => this.m_config;

        //TKTek note, at 2023/8/31 20:07 removed this static constructor..
        /*
		static ScriptEngine()
		{

			//add all currently referenced assemblies so mods have access to them
			Assembly[] referencedAssemblies = AppDomain.CurrentDomain.GetAssemblies();

			foreach (Assembly assembly in referencedAssemblies)
			{
				try
				{
					if (!AddAssemblyFromPath(assembly.Location))
					{
						Debug.LogError($"Assembly {Path.GetFileName(assembly.Location)} failed to load!");
					}
				}
				catch (NotSupportedException e)
				{
					Debug.Log("Assembly location not supported, probably loaded from memory\n" + e);
				}
			}
			//Goggs: should be for windows platform
			//Debug.LogError(Assembly.GetCallingAssembly().GetName());
		}
		*/
        //A constructor written by TKTek
        public ScriptEngine(ScriptEngineConfig config)
        {
            this.m_config = config;
            this.m_PreviousCompiledScriptPool = new List<CompiledScriptInfo>();
        }

        public SyntaxTree Parse(string code) => SyntaxFactory.ParseSyntaxTree(text: code, options: new CSharpParseOptions(languageVersion: this.m_config.fCSharpLanguageVersion, kind: this.m_config.fCSharpCodeKind, preprocessorSymbols: this.m_config.PreprocessorSymbols));
        /// <summary>
        /// Compile script.
        /// </summary>
        /// <param name = "code">Literal code to be compiled</param>
        /// <returns></returns>
        public CompiledScriptInfo Compile(string code) => Compile(Parse(code));
        /// <summary>
        /// Compile script.
        /// </summary>
        /// <remarks>TKTek 2023/9/1 4:33 note: also loads the script assembly to the caller's .NET AppDomain!</remarks>
        /// <param name = "stxTree"></param>
        /// <returns></returns>
         //actual compilation happens here
        public CompiledScriptInfo Compile(SyntaxTree stxTree)
        {
            string l_ScriptAssemblyName = $"BPLE_ScriptAssembly-{Guid.NewGuid()}";
            string l_ScriptClassName = $"BPLE_ScriptClass-{Guid.NewGuid()}";
            Debug.Log($"Script compilation will be performed, *timestamp* {DateTime.Now}, *script assembly name* {l_ScriptClassName}");
            CSharpCompilation l_Compilation = CSharpCompilation.CreateScriptCompilation(assemblyName: l_ScriptAssemblyName, syntaxTree: stxTree, references: GetMdReferences(), options: new CSharpCompilationOptions(outputKind: OutputKind.DynamicallyLinkedLibrary, //build a dll...? (goggs'note)
 //TKTek note at 2023/8/31 19:52 - urge, I think it's just compiling a dotnet class library? 
            optimizationLevel: this.m_config.fCompilationOptimizationLevel, //optimize the code, we may change this to debug later if it's too slow
 //TKTek note at 2023/8/31 21:57 - oh it will be configurable through config file?
            checkOverflow: this.m_config.fCheckOverflow, allowUnsafe: this.m_config.fAllowUnsafeBlocks, platform: this.m_config.fCompilationPlatform, usings: this.m_config.Usings, scriptClassName: l_ScriptClassName), previousScriptCompilation: this.m_PreviousCompilation);
            EmitResult compilationResult = null;
            //the compiled dll is outputted to this byte array
            byte[] l_compiledScriptAsmBinDat = null;
            string errorMessage = null;
            using (MemoryStream ms = new MemoryStream())
            {
                //woah we actually compile it here
                compilationResult = l_Compilation.Emit(ms);
                l_compiledScriptAsmBinDat = ms.ToArray();
            }

            //error handling
            if (!compilationResult.Success)
            {
                StringBuilder errMessageBuilder = new StringBuilder($"Error compiling code for script assembly {l_ScriptAssemblyName}!\n");
                foreach (Diagnostic diagnostic in compilationResult.Diagnostics)
                {
                    errMessageBuilder.AppendLine(diagnostic.ToString());
                }

                errorMessage = errMessageBuilder.ToString();
                Debug.LogError(errorMessage);
                ExceptionMassager = errorMessage;
                return new CompiledScriptInfo(new ScriptCompilingException(errorMessage));
            }

#if TKTMOD
			if (INUserSettings.Instance.ScriptEngineSettings.DumpScriptAssemblyToFile)
			{
				WriteCompiledScriptAssemblyDumpToDisk(l_compiledScriptAsmBinDat, l_ScriptAssemblyName);
			}
#endif
            this.m_submissionIndex++;
            //load the script assembly ^-^
            Assembly l_CompiledScriptAssembly = Assembly.Load(l_compiledScriptAsmBinDat);
            //the entry point method symbol of the script assembly
            IMethodSymbol l_EntryPointMethodSymbol = l_Compilation.GetEntryPoint(cancellationToken: System.Threading.CancellationToken.None);
            bool l_fScriptClassIsInNamespace = string.IsNullOrWhiteSpace(l_EntryPointMethodSymbol.ContainingNamespace.MetadataName);
            Type l_ScriptClassType = l_CompiledScriptAssembly.GetType($"{(l_fScriptClassIsInNamespace ? string.Empty : l_EntryPointMethodSymbol.ContainingNamespace.MetadataName + ".")}{l_EntryPointMethodSymbol.ContainingType.MetadataName}");
            MethodInfo l_ScriptAssemblyEntryPointMethod = l_ScriptClassType.GetMethod(l_EntryPointMethodSymbol.MetadataName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (this.m_submissionIndex >= this.m_submissionStates.Length)
            {
                Array.Resize(ref this.m_submissionStates, Math.Max(this.m_submissionIndex, this.m_submissionStates.Length * 2));
            }

            CompiledScriptInfo l_Result = new CompiledScriptInfo(assembly: l_CompiledScriptAssembly, assemblyBinaryData: l_compiledScriptAsmBinDat, entryPoint: l_EntryPointMethodSymbol, scriptEntryMethod: l_ScriptAssemblyEntryPointMethod, scriptEntryMethodDelegate: () =>
            {
                Func<object[], Task<object>> l_submission = (Func<object[], Task<object>>)l_ScriptAssemblyEntryPointMethod.CreateDelegate(typeof(Func<object[], Task<object>>));
                return l_submission(this.m_submissionStates).GetAwaiter().GetResult();
            });
            //Enlist the compiled script's information into the compiled script pool.
            m_PreviousCompiledScriptPool.Add(l_Result);
            Debug.Log("WOW HE WROTE A SUCCESSFUL CODE");
            //and finally, we return the compiled script's information.
            return l_Result;
            //----------------------------------------------------------------
            //get metadata references..
            IEnumerable<MetadataReference> GetMdReferences() =>
                from AssemblyReference lit_AsmRef in this.m_config.AssemblyReferences
                select lit_AsmRef.MetadataReference;
            void WriteCompiledScriptAssemblyDumpToDisk(byte[] array, string assemblyName)
            {
                string l_DumpFileDir = Path.Combine(UnityEngine.Application.persistentDataPath, $"/CompiledScriptAssembliesDump/");
                string l_DumpFilePath = Path.Combine(l_DumpFileDir, $"{assemblyName}.dll");
                if (!Directory.Exists(l_DumpFileDir))
                    Directory.CreateDirectory(l_DumpFileDir);
                if (!File.Exists(l_DumpFilePath))
                    File.Create(l_DumpFilePath).Dispose();
                else
                    File.Delete(l_DumpFilePath);
                File.WriteAllBytes(l_DumpFilePath, array);
            }
        }

        //returns true on success, false on failure
        private bool AddAssemblyReferenceFromPath(string path)
        {
            //ignore empty paths
            if (string.IsNullOrEmpty(path))
                return false;
            string fullPath = Path.GetFullPath(path);
            if (!File.Exists(fullPath))
                return false;
            AssemblyFileReference l_AsmFileRef = new AssemblyFileReference(fullPath);
            //ignore already referenced libraries
            //TKTek note, at 2023/8/31 20:11 - bruh this is not a good way to checking the assembly will adding reference to the reference list is already contained in the reference list, have to do the "TEST".
            if (this.m_config.AssemblyReferences.Any(r => r.MetadataReference.Display == l_AsmFileRef.MetadataReference.Display))
                return true;
            try
            {
                this.m_config.AssemblyReferences.Add(l_AsmFileRef);
            }
            catch
            {
                return false;
            }

            return true;
        }

        //TKTek 2023/9/1 4:39 note: just remove this method please!
#if false
		public Func<TInput, TOutput>? CompileAsFunc<TInput, TOutput>(string code, string inputTypeName)
		{
			string randomClassName = RandomString(42, "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ");
			string wrappedCode = $"using System;\nusing System.Collections.Generic;\nusing System.IO;\nusing System.Linq;\nusing System.Reflection;\nusing System.Text;\nusing UnityEngine;\n" +
				"public class " + randomClassName + " : MonoBehaviour\n{public " + typeof(TOutput) + " Main(" + typeof(TInput) + " " + inputTypeName + ")\n{return " + code + ";\n}}";
			Assembly? nullableAssembly = this.Compile(wrappedCode/*, $"eval{randomClassName}"*/)?.m_Assembly;
			if (nullableAssembly == null)
			{
				return null;
			}
			else
			{
				object assemblyInstance = nullableAssembly.CreateInstance(randomClassName)!;
				MethodInfo methodInfo = nullableAssembly.GetType(randomClassName).GetMethod("Main")!;
				return delegate (TInput type)
				{
					TOutput returnValue = (TOutput)methodInfo.Invoke(assemblyInstance, new object[] { type });
					return returnValue;
				};
			}
		}
#endif
        public static ScriptEngineConfig GetDefaultConfig()
        {
            List<AssemblyReference> assemblyReferences = new List<AssemblyReference>();
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly EEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEE in assemblies)
            {
                try
                {
                    assemblyReferences.Add(new AssemblyFileReference(EEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEE.Location));
                }
                catch (Exception e)
                {
                //assemblyReferences.Add(new AssemblyBinaryReference(EEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEE.));
                }
            }

            return new ScriptEngineConfig(initialAssemblyReferences: assemblyReferences, usings: new[] { "System", "System.Collections", "System.Collections.Generic", "UnityEngine" });
        }

#region Goggs' utillity methods
        public static string RandomString(int length, string allowedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789")
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), "length cannot be less than zero.");
            }

            if (string.IsNullOrEmpty(allowedChars))
            {
                throw new ArgumentException("allowedChars may not be empty.");
            }

            var allowedCharSet = new HashSet<char>(allowedChars).ToArray();
            if (256 < allowedCharSet.Length)
            {
                throw new ArgumentException($"allowedChars may contain no more than 256 characters.");
            }

            var result = new StringBuilder();
            var buf = new byte[128];
            while (result.Length < length)
            {
                randomNumberGenerator.GetBytes(buf);
                var i = 0;
                while (i < buf.Length && result.Length < length)
                {
                    var outOfRangeStart = 256 - 256 % allowedCharSet.Length;
                    if (outOfRangeStart > buf[i])
                    {
                        result.Append(allowedCharSet[buf[i] % allowedCharSet.Length]);
                    }

                    i++;
                }
            }

            var result2 = result.ToString();
            return result2;
        }

        public string WarpCodeSnippet(string snippet, string randomClassName = "Start")
        {
            string[] lines = snippet.Split('\n');
            string usings = "";
            string regularCode = "";
            foreach (string line in lines)
            {
                if (line.Contains("using"))
                {
                    usings += line + '\n';
                }
                else
                {
                    regularCode += line + '\n';
                }
            }

            return @"using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
" + usings + @"public class " + randomClassName + @" : MonoBehaviour
{
    public static object Main()
    {
        " + regularCode + @"
        return ""No return statement!"";
    }
}
";
        }
#endregion
    }
}
#endif
