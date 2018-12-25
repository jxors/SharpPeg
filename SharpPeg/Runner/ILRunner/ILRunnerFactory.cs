using SharpPeg.Common;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace SharpPeg.Runner.ILRunner
{
    public class ILRunnerFactory : IRunnerFactory
    {
        private readonly Type type;
        private readonly IReadOnlyList<Method> methods;

        internal ILRunnerFactory(Type type, IReadOnlyList<Method> methods)
        {
            this.type = type;
            this.methods = methods;
        }

        public IRunner New()
        {
            return (IRunner)Activator.CreateInstance(type, new object[] { methods });
        }
    }
}
