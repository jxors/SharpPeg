using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace SharpPeg.Runner.ILRunner
{
    static class ReflectionExts
    {
        public static PropertyInfo GetProperty(this Type type, string name)
        {
            return type.GetTypeInfo().GetProperty(name);
        }

        public static ConstructorInfo GetConstructor(this Type type, Type[] types)
        {
            return type.GetTypeInfo().GetConstructor(types);
        }

        public static MethodInfo GetMethod(this Type type, string name)
        {
            return type.GetTypeInfo().GetMethod(name);
        }

        public static MethodInfo GetMethod(this Type type, string name, BindingFlags bf)
        {
            return type.GetTypeInfo().GetMethod(name, bf);
        }

        public static FieldInfo GetField(this Type type, string name)
        {
            return type.GetTypeInfo().GetField(name);
        }
    }
}
