using UnityEngine;

namespace MentalTools
{
    public class MentalHelper
    {
        public static T EnsureComponent<T>(GameObject go)
            where T : Component
        {
            T component = go.GetComponent<T>();
            if ((Object)component == (Object)null)
            {
                return go.AddComponent<T>();
            }

            return component;
        }
    }
}