using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace tester
{
    interface DebuggerInterface
    {
        int attachProcess(Process process);
        int getCountFunctionCalls();
        int setHook(Func<float, int, int, int> func);
        int Stop();
    }
}
