using System;
using System.Collections.Generic;
using System.Text;

namespace SharpPeg.Runner
{
    public interface IRunnerFactory
    {
        IRunner New();
    }
}
