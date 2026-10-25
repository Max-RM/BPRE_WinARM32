namespace GoggsMessaround.CSharpScriptEngine
{
    /*
	 * Created by TKTek, at 2023/9/1 0:25
	 */
    [System.Serializable]
    public class ScriptCompilingException : System.Exception
    {
        public ScriptCompilingException()
        {
        }

        public ScriptCompilingException(string message) : base(message)
        {
        }

        public ScriptCompilingException(string message, System.Exception inner) : base(message, inner)
        {
        }

        protected ScriptCompilingException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context)
        {
        }
    }
}