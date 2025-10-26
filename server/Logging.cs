using System.Diagnostics;
using System.Reflection;

namespace TScriptLanguageServer;

public class Logging
{
    public enum Level
    {
        Debug,
        Info,
        Error,
        Warning
    }


    public static StreamWriter? OutputStream = null;
    public static bool DebugMessagesEnabled = false;

    public static void Log(string message, Level level)
    {
        if (OutputStream == null || (level == Level.Debug && !DebugMessagesEnabled))
            return;

        OutputStream.WriteLine($"{GetLevelString(level)}{message} [{GetSignature(3)} -> {GetSignature(4)}]");
        OutputStream.Flush();
    }

    private static string GetCleanMethodName(MethodBase method)
    {
        string fullMethodName = method.Name;
        int lastDotIndex = fullMethodName.LastIndexOf('.');
        if (lastDotIndex != -1)
            return fullMethodName[(lastDotIndex + 1)..];
        return fullMethodName;
    }

    private static string GetMethodSignature(MethodBase method)
    {
        string returnType = (method is MethodInfo info) ? info.ReturnType.Name : "void";
        var parameters = method.GetParameters();
        string paramList = string.Join(", ", parameters.Select(p => $"{p.ParameterType.Name} {p.Name}"));
        return $"{GetCleanMethodName(method)}({paramList}) : {returnType}";
    }

    private static string GetSignature(int stackIndex)
    {
        StackTrace stackTrace = new();
        StackFrame? callerFrame = stackTrace.GetFrame(stackIndex);
        if (callerFrame == null)
            return "Unknown.Unknown(): Unknown";

        MethodBase? callerMethod = callerFrame.GetMethod() ?? MethodBase.GetCurrentMethod();
        if (callerMethod == null)
            return "Unknown.Unknown(): Unknown";

        string className = callerMethod.ReflectedType?.Name ?? "UnknownClass";
        string methodSignature = GetMethodSignature(callerMethod);

        return $"{className}.{methodSignature}";
    }

    public static void LogDebug(string message) => Log(message, Level.Debug);
    public static void LogInfo(string message) => Log(message, Level.Info);
    public static void LogError(string message) => Log(message, Level.Error);
    public static void LogWarning(string message) => Log(message, Level.Warning);

    private static string GetLevelString(Level level)
    {
        if (level == Level.Debug)
            return "[DEBUG]\t\t";

        if (level == Level.Info)
            return "[INFO]\t\t";

        if (level == Level.Error)
            return "[ERROR]\t\t";

        if (level == Level.Warning)
            return "[WARNING]\t";

        return "[UNKNOWN]\t";
    }
}
