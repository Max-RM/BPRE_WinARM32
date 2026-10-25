/*
 * TKTek, 2023/9/1 4:21:
 * this #if preprocessor command is used for my debugging?
 * Remove this #if preprocessor command if it's useless.
 */
#define GOGGS_SCRIPT_ENGINE
#if GOGGS_SCRIPT_ENGINE && !UNITY_WSA
namespace GoggsMessaround.CSharpScriptEngine
{
    /*
	 * Created by TKTek, at 2023/8/31 19:31
	 */
    public class AssemblyBinaryReference : AssemblyReference
    {
        private byte[] m_AssemblyBinary;
        /// <summary>
        /// <para>Returns a the assembly's binary data</para>
        /// <para>DON'T MODIFY THE RETURNED BYTE ARRAY</para>
        /// </summary>
         //bruh should we give a copy of the assembly binary data - not a reference to the assembly binary data's byte array in this class?
        public byte[] AssemblyBinary => m_AssemblyBinary;

        public AssemblyBinaryReference(byte[] assemblyBinary)
        {
            m_AssemblyBinary = assemblyBinary;
            m_MdRef = Microsoft.CodeAnalysis.MetadataReference.CreateFromImage(m_AssemblyBinary);
        }
    }
}
#endif
