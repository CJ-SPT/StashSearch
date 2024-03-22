using System;

namespace StashSearch.Utils
{
    public static class ReflectionHelper
    {
        public static bool IsOfTypeOrDerivedFrom(object obj, Type targetType)
        {
            Type objType = obj.GetType();

            // Check if the object is exactly of targetType
            if (objType == targetType)
                return true;

            // Check if targetType is in the inheritance hierarchy of objType
            Type parentType = objType.BaseType;
            while (parentType != null)
            {
                if (parentType == targetType)
                    return true;
                parentType = parentType.BaseType;
            }

            return false;
        }
    }
}
