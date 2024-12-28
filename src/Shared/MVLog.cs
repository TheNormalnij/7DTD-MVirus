﻿using System;

namespace MVirus.Shared
{
    public class MVLog
    {
        private static string PREFIX = "[MVirus] ";

        public static void Out(string message)
        {
            Log.Out(PREFIX + message);
        }

        public static void Warning(string message)
        {
            Log.Warning(PREFIX + message);
        }

        public static void Error(string message)
        {
            Log.Error(PREFIX + message);
        }

        public static void Exception(Exception ex)
        {
            Log.Exception(ex);
            Error(ex.StackTrace.ToString());
        }

        public static void Out(string _format, params object[] _values)
        {
            Out(string.Format(_format, _values));
        }

        public static void Warning(string _format, params object[] _values)
        {
            Warning(string.Format(_format, _values));
        }

        public static void Error(string _format, params object[] _values)
        {
            Error(string.Format(_format, _values));
        }
    }
}