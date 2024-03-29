﻿using Security;
using System;

namespace UpdatesClient.Modules.SelfUpdater
{
    internal static class Security
    {
        internal static string UID;

        internal static bool CheckEnvironment()
        {
            UID = Hashing.GetMD5FromText(SystemFunctions.GetHWID());
            AesEncoder.Init();
#if (DEBUG)

#elif (BETA)
            if (!CheckInjection()) return false;
#else
            if (!CheckInjection()) return false;
#endif
            return true;
        }

        private static bool CheckInjection()
        {
            if (WinFunctions.GetModuleHandle("SbieDll.dll") != IntPtr.Zero) return false;
            return true;
        }
    }
}
