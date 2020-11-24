using System;

using Verse;

namespace CM_PocketDimension
{
    public static class Logger
    {
        public static bool Enabled = true;
        public static void MessageFormat(object caller, string message, params object[] stuff)
        {
            if (Logger.Enabled)
            {
                message = caller.GetType().ToString() + "." + (new System.Diagnostics.StackTrace()).GetFrame(1).GetMethod().Name + " - " + message;
                Log.Message(String.Format(message, stuff));
            }
        }

        public static void MessageFormat(string message, params object[] stuff)
        {
            if (Logger.Enabled)
            {
                message = (new System.Diagnostics.StackTrace()).GetFrame(1).GetMethod().Name + " - " + message;
                Log.Message(String.Format(message, stuff));
            }
        }
    }
}
