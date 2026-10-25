/*
 * TKTek, 2023/9/1 4:21:
 * this #if preprocessor command is used for my debugging?
 * Remove this #if preprocessor command if it's useless.
 */
#define GOGGS_SCRIPT_ENGINE
#if GOGGS_SCRIPT_ENGINE && !UNITY_WSA
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace GoggsMessaround.CSharpScriptEngine
{
    /*
	 * Created by TKTek, at 2023/8/31 19:21
	 */
    /// <summary>
    /// Assembly reference information class with more information about assembly?
    /// </summary>
    public abstract class AssemblyReference
    {
        protected MetadataReference m_MdRef;
        /// <summary>
        /// Assembly Metadata Reference.
        /// </summary>
        public MetadataReference MetadataReference => m_MdRef;
    }
}
#endif
