namespace GoggsMessaround.CSharpScriptEngine
{
    /*
	 * Created by TKTek, at 2023/9/1 0:25
	 */
    [System.Serializable]
    public class ScriptExecutionException : System.Exception
    {
        public ScriptExecutionException()
        {
        }

        public ScriptExecutionException(string message) : base(message)
        {
        }

        public ScriptExecutionException(string message, System.Exception inner) : base(message, inner)
        {
        }

        protected ScriptExecutionException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context)
        {
        }
    }
}