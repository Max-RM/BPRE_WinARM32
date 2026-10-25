using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    protected static T instance;
    public static T Instance
    {
        get
        {
            if ((Object)instance == (Object)null)
            {
                instance = Object.FindObjectOfType<T>();
            }

            return instance;
        }
    }

    public static bool IsInstantiated()
    {
        return (Object)instance != (Object)null;
    }

    protected void SetAsPersistant()
    {
        instance = this as T;
        Object.DontDestroyOnLoad(this);
    }
}