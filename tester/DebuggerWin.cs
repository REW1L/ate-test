using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;

namespace tester
{
#if _WIN32
    class DebuggerWin : DebuggerInterface
    {
        private static List<ModuleInfo> moduleInfoList = new List<ModuleInfo>();
        private int timeout = -1;
        bool continueDebug;
        Func<float, int, int, int> onFrame;
        struct ModuleInfo
        {
            public string fileName;
            public IntPtr baseAddress;
        }
        public DebuggerWin(int timeout)
        {
            this.onFrame = null;
            this.timeout = timeout;
        }
        int DebuggerInterface.Stop()
        {
            continueDebug = false;
            return 0;
        }
        int DebuggerInterface.setHook(Func<float, int, int, int> func)
        {
            this.onFrame = func;
            return 0;
        }
        private static IntPtr GetIntPtrFromByteArray(byte[] byteArray)
        {
            GCHandle pinnedArray = GCHandle.Alloc(byteArray, GCHandleType.Pinned);
            IntPtr intPtr = pinnedArray.AddrOfPinnedObject();
            pinnedArray.Free();
            return intPtr;
        }
        int DebuggerInterface.attachProcess(Process process)
        {
            uint pid = (uint)process.Id;
            if (!PInvokes.DebugActiveProcess(pid))
            {
                Console.WriteLine(String.Format("Cannot debug the process with PID: {0}. Last Error 0x{1:x}", pid.ToString(), (int)PInvokes.GetLastError()));
                return -1;
            }
            else
            {
                Console.WriteLine("Process is now debugged");
            }
            IntPtr debugEventPtr = Marshal.AllocHGlobal(188);
            int a = 0;
            bool bb;
            IntPtr hProcess = IntPtr.Zero;
            IntPtr hThread = IntPtr.Zero;
            continueDebug = true;
            UInt32 dwContinueDebugEvent = PInvokes.DBG_CONTINUE;
            PInvokes.DEBUG_EVENT DebugEvent;
            IntPtr drawRectFunctionAddr;
            byte[] breakpoint = { 0xCC };
            IntPtr bytesWritten = IntPtr.Zero;
            Stopwatch sw = null;
            float angle = 0;
            long delay = 0;
            int fullRotationAmount = 0;
            PInvokes.CREATE_PROCESS_DEBUG_INFO CreateProcessDebugInfo;
            Stopwatch timer = null;
            while (continueDebug)
            {
                bb = PInvokes.WaitForDebugEvent(debugEventPtr, 1000);
                DebugEvent = (PInvokes.DEBUG_EVENT)Marshal.PtrToStructure(debugEventPtr, typeof(PInvokes.DEBUG_EVENT));
                IntPtr debugInfoPtr = GetIntPtrFromByteArray(DebugEvent.u);
                if(timeout != -1 && timer != null && timer.ElapsedMilliseconds > timeout)
                {
                    break;
                }
                if (bb)
                {
                    switch(DebugEvent.dwDebugEventCode)
                    {
                        case PInvokes.CREATE_PROCESS_DEBUG_EVENT:
                            Console.WriteLine("CREATE_PROCESS_DEBUG_EVENT");
                            CreateProcessDebugInfo = (PInvokes.CREATE_PROCESS_DEBUG_INFO)Marshal.PtrToStructure(debugInfoPtr,
                                typeof(PInvokes.CREATE_PROCESS_DEBUG_INFO));
                            hProcess = CreateProcessDebugInfo.hProcess;
                            hThread = CreateProcessDebugInfo.hThread;
                            drawRectFunctionAddr = process.MainModule.EntryPointAddress + 0xB7; // cosf
                            PInvokes.WriteProcessMemory(hProcess, drawRectFunctionAddr, breakpoint, 1, out bytesWritten);
                            if (bytesWritten == IntPtr.Zero)
                            {
                                process.Close();
                                process.Dispose();
                                return -1;
                            }
                            timer = Stopwatch.StartNew();
                            break;
                        case PInvokes.CREATE_THREAD_DEBUG_EVENT:
                            Console.WriteLine("CREATE_THREAD_DEBUG_EVENT");
                            break;
                        case PInvokes.EXCEPTION_DEBUG_EVENT:
                            PInvokes.EXCEPTION_DEBUG_INFO ExceptionDebugInfo = (PInvokes.EXCEPTION_DEBUG_INFO)Marshal.PtrToStructure(debugInfoPtr, 
                                typeof(PInvokes.EXCEPTION_DEBUG_INFO));
                            string exceptionDebugStr = String.Format("EXCEPTION_DEBUG_EVENT: Exception Address: 0x{0:x}, Exception code: 0x{1:x}",
                                (ulong)ExceptionDebugInfo.ExceptionRecord.ExceptionAddress, ExceptionDebugInfo.ExceptionRecord.ExceptionCode);
                            Console.WriteLine(exceptionDebugStr);
                            switch (ExceptionDebugInfo.ExceptionRecord.ExceptionCode)
                            {
                                case PInvokes.EXCEPTION_ACCESS_VIOLATION:
                                    Console.WriteLine("EXCEPTION_ACCESS_VIOLATION");
                                    continueDebug = false;

                                    string filePath = Path.GetFullPath(@"/" + string.Format(@"{0}", Guid.NewGuid()));
                                    using (var minidumpFile = new FileStream(filePath + ".dmp", FileMode.CreateNew))
                                    {
                                        PInvokes.MINIDUMP_EXCEPTION_INFORMATION exceptionInformation = new PInvokes.MINIDUMP_EXCEPTION_INFORMATION();
                                        PInvokes.MiniDumpWriteDump( hProcess, pid, minidumpFile.SafeFileHandle.DangerousGetHandle(),
                                            (int)PInvokes.MinidumpType.MiniDumpAll, ref exceptionInformation, IntPtr.Zero, IntPtr.Zero);
                                    }
                                    using (StreamWriter stateFile = new StreamWriter(filePath + ".txt"))
                                    {
                                        stateFile.WriteLine("Frame: {0},Rotations: {1},Delay: {2},Angle: {3}", ++a, delay, angle, fullRotationAmount);
                                    }
                                    break;

                                case PInvokes.EXCEPTION_BREAKPOINT:
                                    Console.WriteLine("EXCEPTION_BREAKPOINT");
                                    PInvokes.CONTEXT64 context = new PInvokes.CONTEXT64();
                                    context.ContextFlags = PInvokes.CONTEXT_FLAGS.CONTEXT_ALL;
                                    IntPtr currentThread = PInvokes.OpenThread(PInvokes.ThreadAccess.GET_CONTEXT, true, (int)DebugEvent.dwThreadId);
                                    if (currentThread != IntPtr.Zero && PInvokes.GetThreadContext(currentThread, ref context))
                                    {
                                        float oldAngle = angle;
                                        angle = BitConverter.Int32BitsToSingle((int)(context.DUMMYUNIONNAME.XmmRegisters[0].High));
                                        if (oldAngle > angle)
                                        {
                                            fullRotationAmount++;
                                        }
                                    }
                                    PInvokes.CloseHandle(currentThread);
                                    if (sw != null)
                                    {
                                        delay = sw.ElapsedMilliseconds;
                                        sw.Restart();
                                    }
                                    else
                                    {
                                        sw = Stopwatch.StartNew();
                                    }
                                    //Console.WriteLine("Frame: {0} Full Rotations: {3} Delta: {1} Angle: {2}", ++a, delay, angle, fullRotationAmount);
                                    if (this.onFrame != null)
                                    {
                                        this.onFrame(angle, (int)delay, a);
                                    }
                                    break;

                                case PInvokes.EXCEPTION_DATATYPE_MISALIGNMENT:
                                    Console.WriteLine("EXCEPTION_DATATYPE_MISALIGNMENT");
                                    break;

                                case PInvokes.EXCEPTION_SINGLE_STEP:
                                    Console.WriteLine("EXCEPTION_SINGLE_STEP");
                                    break;

                                case PInvokes.DBG_CONTROL_C:
                                    Console.WriteLine("DBG_CONTROL_C");
                                    break;
                                case PInvokes.EXCEPTION_ARRAY_BOUNDS_EXCEEDED:
                                    Console.WriteLine("EXCEPTION_ARRAY_BOUNDS_EXCEEDED");
                                    break;
                                case PInvokes.EXCEPTION_INT_DIVIDE_BY_ZERO:
                                    PInvokes.ContinueDebugEvent(DebugEvent.dwProcessId,
                                           DebugEvent.dwThreadId,
                                           PInvokes.DBG_EXCEPTION_NOT_HANDLED);
                                    Console.WriteLine("EXCEPTION_INT_DIVIDE_BY_ZERO");
                                    break;
                                default:
                                    Console.WriteLine("Handle other exceptions.");
                                    break;
                            }
                            break;
                        case PInvokes.EXIT_PROCESS_DEBUG_EVENT:
                            Console.WriteLine("EXIT_PROCESS_DEBUG_EVENT");
                            PInvokes.EXIT_PROCESS_DEBUG_INFO ExitProcessDebugInfo = (PInvokes.EXIT_PROCESS_DEBUG_INFO)Marshal.PtrToStructure(debugInfoPtr, 
                                typeof(PInvokes.EXIT_PROCESS_DEBUG_INFO));
                            Console.WriteLine("EXIT CODE: " + ExitProcessDebugInfo.dwExitCode);
                            continueDebug = false;
                            break;
                        case PInvokes.EXIT_THREAD_DEBUG_EVENT:
                            Console.WriteLine("EXIT_THREAD_DEBUG_EVENT");
                            break;
                        case PInvokes.LOAD_DLL_DEBUG_EVENT:
                            Console.WriteLine("OUTPUT_DEBUG_STRING_EVENT");
                            break;
                        case PInvokes.OUTPUT_DEBUG_STRING_EVENT:
                            Console.WriteLine("OUTPUT_DEBUG_STRING_EVENT");
                            break;
                        case PInvokes.RIP_EVENT:
                            Console.WriteLine("RIP_EVENT");
                            PInvokes.RIP_INFO RipInfo = (PInvokes.RIP_INFO)Marshal.PtrToStructure(debugInfoPtr, typeof(PInvokes.RIP_INFO));
                            break;
                        case PInvokes.UNLOAD_DLL_DEBUG_EVENT:
                            Console.WriteLine("UNLOAD_DLL_DEBUG_EVENT");
                            break;
                        default:
                            Console.WriteLine("DEBUGEVENT: " + DebugEvent.dwDebugEventCode);
                            break;
                    }
                }
                PInvokes.ContinueDebugEvent((uint)DebugEvent.dwProcessId, (uint)DebugEvent.dwThreadId, dwContinueDebugEvent);
            }
            if (debugEventPtr != null)
                Marshal.FreeHGlobal(debugEventPtr);
            if (pid > 0)
            {
                if (!PInvokes.DebugActiveProcessStop(pid))
                {
                    Console.WriteLine("DebugActiveProcessStop failed");
                }
                process.Close();
                process.Dispose();
            }
            return 0;
        }
        int DebuggerInterface.getCountFunctionCalls() {

            return 0;
        }
    }
#endif
}
