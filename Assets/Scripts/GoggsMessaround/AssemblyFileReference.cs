/*
 * TKTek, 2023/9/1 4:21:
 * this #if preprocessor command is used for my debugging?
 * Remove this #if preprocessor command if it's useless.
 */
#define GOGGS_SCRIPT_ENGINE
#if GOGGS_SCRIPT_ENGINE && !UNITY_WSA
using System.IO;

namespace GoggsMessaround.CSharpScriptEngine
{
    /*
	 * Created by TKTek, at 2023/8/31 19:25
	 */
    public class AssemblyFileReference : AssemblyReference
    {
        private FileInfo m_fiAssemblyFilePath;
        /// <summary>
        /// The path of the referenced assembly file.
        /// </summary>
        public string AssemblyFilePath => m_fiAssemblyFilePath.FullName;

        /// <summary>
        /// Create a assembly file reference.
        /// </summary>
        /// <exception cref = "FileNotFoundException"/>
        /// <exception cref = "IOException"/>
        /// <param name = "assemblyFilePath">The path of the assembly file will create <see cref = "AssemblyFileReference"/> reference information class</param>
        public AssemblyFileReference(string assemblyFilePath)
        {
            m_fiAssemblyFilePath = new FileInfo(assemblyFilePath);
            if (!m_fiAssemblyFilePath.Exists)
                throw new FileNotFoundException(assemblyFilePath);
            this.m_MdRef = Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(m_fiAssemblyFilePath.FullName);
        }
    }
}
#endif
