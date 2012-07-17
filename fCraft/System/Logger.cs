// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Reflection;
using System.Text;
using fCraft.Events;
using JetBrains.Annotations;
#if DEBUG_EVENTS
using System.Reflection.Emit;
#endif

namespace fCraft
{
    /// <summary> Central logging class. Logs to file, relays messages to the frontend, submits crash reports. </summary>
    public static class Logger
    {
        static readonly object LogLock = new object();
        public static bool Enabled { get; set; }
        public static readonly bool[] ConsoleOptions;
        public static readonly bool[] LogFileOptions;

        const string DefaultLogFileName = "800Craft.log",
                     LongDateFormat = "yyyy'-'MM'-'dd'_'HH'-'mm'-'ss",
                     ShortDateFormat = "yyyy'-'MM'-'dd";
        static readonly Uri CrashReportUri = new Uri("http://au70.net/crashreport.php");
        public static LogSplittingType SplittingType = LogSplittingType.OneFile;

        static readonly string SessionStart = DateTime.Now.ToString(LongDateFormat); // localized
        static readonly Queue<string> RecentMessages = new Queue<string>();
        const int MaxRecentMessages = 25;

        public static string CurrentLogFileName
        {
            get
            {
                switch (SplittingType)
                {
                    case LogSplittingType.SplitBySession:
                        return SessionStart + ".log";
                    case LogSplittingType.SplitByDay:
                        return DateTime.Now.ToString(ShortDateFormat) + ".log"; // localized
                    default:
                        return DefaultLogFileName;
                }
            }
        }


        static Logger()
        {
            Enabled = true;
            int typeCount = Enum.GetNames(typeof(LogType)).Length;
            ConsoleOptions = new bool[typeCount];
            LogFileOptions = new bool[typeCount];
            for (int i = 0; i < typeCount; i++)
            {
                ConsoleOptions[i] = true;
                LogFileOptions[i] = true;
            }
        }


        internal static void MarkLogStart()
        {
            // Mark start of logging
            Log(LogType.SystemActivity, "------ Log Starts {0} ({1}) ------",
                 DateTime.Now.ToLongDateString(), DateTime.Now.ToShortDateString()); // localized
        }

        public static void LogToConsole([NotNull] string message)
        {
            if (message == null) throw new ArgumentNullException("message");
            if (message.Contains('\n'))
            {
                foreach (string line in message.Split('\n'))
                {
                    LogToConsole(line);
                }
                return;
            }
            string processedMessage = "# ";
            for (int i = 0; i < message.Length; i++)
            {
                if (message[i] == '&') i++;
                else processedMessage += message[i];
            }
            Log(LogType.ConsoleOutput, processedMessage);
        }


        [DebuggerStepThrough]
        [StringFormatMethod("message")]
        public static void Log(LogType type, [NotNull] string message, [NotNull] params object[] values)
        {
            if (message == null) throw new ArgumentNullException("message");
            if (values == null) throw new ArgumentNullException("values");
            Log(type, String.Format(message, values));
        }


        [DebuggerStepThrough]
        public static void Log(LogType type, [NotNull] string message)
        {
            if (message == null) throw new ArgumentNullException("message");
            if (!Enabled) return;
            string line = DateTime.Now.ToLongTimeString() + " > " + GetPrefix(type) + message; // localized

            lock (LogLock)
            {
                RaiseLoggedEvent(message, line, type);

                RecentMessages.Enqueue(line);
                while (RecentMessages.Count > MaxRecentMessages)
                {
                    RecentMessages.Dequeue();
                }

                if (LogFileOptions[(int)type])
                {
                    try
                    {
                        File.AppendAllText(Path.Combine(Paths.LogPath, CurrentLogFileName), line + Environment.NewLine);
                    }
                    catch (Exception ex)
                    {
                        string errorMessage = "Logger.Log: " + ex.Message;
                        RaiseLoggedEvent(errorMessage,
                                          DateTime.Now.ToLongTimeString() + " > " + GetPrefix(LogType.Error) + errorMessage, // localized
                                          LogType.Error);
                    }
                }
            }
        }


        [DebuggerStepThrough]
        public static string GetPrefix(LogType level)
        {
            switch (level)
            {
                case LogType.SeriousError:
                case LogType.Error:
                    return "ERROR: ";
                case LogType.Warning:
                    return "Warning: ";
                case LogType.IRC:
                    return "IRC: ";
                default:
                    return String.Empty;
            }
        }


        #region Crash Handling

        static readonly object CrashReportLock = new object(); // mutex to prevent simultaneous reports (messes up the timers/requests)
        static DateTime lastCrashReport = DateTime.MinValue;
        const int MinCrashReportInterval = 61; // minimum interval between submitting crash reports, in seconds


        public static void LogAndReportCrash([CanBeNull] string message, [CanBeNull] string assembly,
                                             [CanBeNull] Exception exception, bool shutdownImminent)
        {
            if (message == null) message = "(null)";
            if (assembly == null) assembly = "(null)";
            if (exception == null) exception = new Exception("(null)");

            Log(LogType.SeriousError, "{0}: {1}", message, exception);

            bool submitCrashReport = ConfigKey.SubmitCrashReports.Enabled();
            bool isCommon = CheckForCommonErrors(exception);

            // ReSharper disable EmptyGeneralCatchClause
            try
            {
                var eventArgs = new CrashedEventArgs(message,
                                                      assembly,
                                                      exception,
                                                      submitCrashReport && !isCommon,
                                                      isCommon,
                                                      shutdownImminent);
                RaiseCrashedEvent(eventArgs);
                isCommon = eventArgs.IsCommonProblem;
            }
            catch { }
            // ReSharper restore EmptyGeneralCatchClause

            if (!submitCrashReport || isCommon)
            {
                return;
            }

            lock (CrashReportLock)
            {
                if (DateTime.UtcNow.Subtract(lastCrashReport).TotalSeconds < MinCrashReportInterval)
                {
                    Log(LogType.Warning, "Logger.SubmitCrashReport: Could not submit crash report, reports too frequent.");
                    return;
                }
                lastCrashReport = DateTime.UtcNow;

                try
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("version=").Append(Uri.EscapeDataString(Updater.CurrentRelease.VersionString));
                    sb.Append("&message=").Append(Uri.EscapeDataString(message));
                    sb.Append("&assembly=").Append(Uri.EscapeDataString(assembly));
                    sb.Append("&runtime=");
                    if (MonoCompat.IsMono)
                    {
                        sb.Append(Uri.EscapeDataString("Mono " + MonoCompat.MonoVersionString));
                    }
                    else
                    {
                        sb.Append(Uri.EscapeDataString("CLR " + Environment.Version));
                    }
                    sb.Append("&os=").Append(Environment.OSVersion.Platform + " / " + Environment.OSVersion.VersionString);

                    if (exception is TargetInvocationException)
                    {
                        exception = (exception).InnerException;
                    }
                    else if (exception is TypeInitializationException)
                    {
                        exception = (exception).InnerException;
                    }
                    sb.Append("&exceptiontype=").Append(Uri.EscapeDataString(exception.GetType().ToString()));
                    sb.Append("&exceptionmessage=").Append(Uri.EscapeDataString(exception.Message));
                    sb.Append("&exceptionstacktrace=").Append(Uri.EscapeDataString(exception.StackTrace));

                    if (File.Exists(Paths.ConfigFileName))
                    {
                        sb.Append("&config=").Append(Uri.EscapeDataString(File.ReadAllText(Paths.ConfigFileName)));
                    }
                    else
                    {
                        sb.Append("&config=");
                    }

                    string[] lastFewLines;
                    lock (LogLock)
                    {
                        lastFewLines = RecentMessages.ToArray();
                    }
                    sb.Append("&log=").Append(Uri.EscapeDataString(String.Join(Environment.NewLine, lastFewLines)));

                    byte[] formData = Encoding.UTF8.GetBytes(sb.ToString());

                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(CrashReportUri);
                    request.Method = "POST";
                    request.Timeout = 15000; // 15s timeout
                    request.ContentType = "application/x-www-form-urlencoded";
                    request.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);
                    request.ContentLength = formData.Length;
                    request.UserAgent = Updater.UserAgent;

                    using (Stream requestStream = request.GetRequestStream())
                    {
                        requestStream.Write(formData, 0, formData.Length);
                        requestStream.Flush();
                    }

                    string responseString;
                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    {
                        using (Stream responseStream = response.GetResponseStream())
                        {
                            // ReSharper disable AssignNullToNotNullAttribute
                            using (StreamReader reader = new StreamReader(responseStream))
                            {
                                // ReSharper restore AssignNullToNotNullAttribute
                                responseString = reader.ReadLine();
                            }
                        }
                    }
                    request.Abort();

                    if (responseString != null && responseString.StartsWith("ERROR"))
                    {
                        Log(LogType.Error, "Crash report could not be processed by au70.net.");
                    }
                    else
                    {
                        int referenceNumber;
                        if (responseString != null && Int32.TryParse(responseString, out referenceNumber))
                        {
                            Log(LogType.SystemActivity, "Crash report submitted (Reference #{0})", referenceNumber);
                        }
                        else
                        {
                            Log(LogType.SystemActivity, "Crash report submitted.");
                        }
                    }


                }
                catch (Exception ex)
                {
                    Log(LogType.Warning, "Logger.SubmitCrashReport: {0}", ex.Message);
                }
            }
        }


        // Called by the Logger in case of serious errors to print troubleshooting advice.
        // Returns true if this type of error is common, and crash report should NOT be submitted.
        public static bool CheckForCommonErrors([CanBeNull] Exception ex)
        {
            if (ex == null) throw new ArgumentNullException("ex");
            string message = null;
            try
            {
                if (ex is FileNotFoundException && ex.Message.Contains("Version=3.5"))
                {
                    message = "Your crash was likely caused by using a wrong version of .NET or Mono runtime. " +
                              "Please update to Microsoft .NET Framework 3.5 (Windows) OR Mono 2.6.4+ (Linux, Unix, Mac OS X).";
                    return true;

                }
                else if (ex.Message.Contains("libMonoPosixHelper") ||
                         ex is EntryPointNotFoundException && ex.Message.Contains("CreateZStream"))
                {
                    message = "800Craft could not locate Mono's compression functionality. " +
                              "Please make sure that you have zlib (sometimes called \"libz\" or just \"z\") installed. " +
                              "Some versions of Mono may also require \"libmono-posix-2.0-cil\" package to be installed.";
                    return true;

                }
                else if (ex is MissingMemberException || ex is TypeLoadException)
                {
                    message = "Something is incompatible with the current revision of 800Craft. " +
                              "If you installed third-party modifications, " +
                              "make sure to use the correct revision (as specified by mod developers). " +
                              "If your own modifications stopped working, your may need to make some updates.";
                    return true;

                }
                else if (ex is UnauthorizedAccessException)
                {
                    message = "800Craft was blocked from accessing a file or resource. " +
                              "Make sure that correct permissions are set for the 800Craft files, folders, and processes.";
                    return true;

                }
                else if (ex is OutOfMemoryException)
                {
                    message = "800Craft ran out of memory. Make sure there is enough RAM to run.";
                    return true;

                }
                else if (ex is SystemException && ex.Message == "Can't find current process")
                {
                    // Ignore Mono-specific bug in MonitorProcessorUsage()
                    return true;

                }
                else if (ex is InvalidOperationException && ex.StackTrace.Contains("MD5CryptoServiceProvider"))
                {
                    message = "Some Windows settings are preventing 800Craft from doing player name verification. " +
                              "See http://support.microsoft.com/kb/811833";
                    return true;

                }
                else if (ex.StackTrace.Contains("__Error.WinIOError"))
                {
                    message = "A filesystem-related error has occured. Make sure that only one instance of 800Craft is running, " +
                              "and that no other processes are using server's files or directories.";
                    return true;

                }
                else if (ex.Message.Contains("UNSTABLE"))
                {
                    return true;

                }
                else
                {
                    return false;
                }
            }
            finally
            {
                if (message != null)
                {
                    Log(LogType.Warning, message);
                }
            }
        }

        #endregion


        #region Event Tracing
#if DEBUG_EVENTS

        // list of events in this assembly
        static readonly Dictionary<int, EventInfo> eventsMap = new Dictionary<int, EventInfo>();


        static List<string> eventWhitelist = new List<string>();
        static List<string> eventBlacklist = new List<string>();
        const string TraceWhitelistFile = "traceonly.txt",
                     TraceBlacklistFile = "notrace.txt";
        static bool useEventWhitelist, useEventBlacklist;

        static void LoadTracingSettings() {
            if( File.Exists( TraceWhitelistFile ) ) {
                useEventWhitelist = true;
                eventWhitelist.AddRange( File.ReadAllLines( TraceWhitelistFile ) );
            } else if( File.Exists( TraceBlacklistFile ) ) {
                useEventBlacklist = true;
                eventBlacklist.AddRange( File.ReadAllLines( TraceBlacklistFile ) );
            }
        }


        // adds hooks to all compliant events in current assembly
        internal static void PrepareEventTracing() {

            LoadTracingSettings();

            // create a dynamic type to hold our handler methods
            AppDomain myDomain = AppDomain.CurrentDomain;
            var asmName = new AssemblyName( "fCraftEventTracing" );
            AssemblyBuilder myAsmBuilder = myDomain.DefineDynamicAssembly( asmName, AssemblyBuilderAccess.RunAndSave );
            ModuleBuilder myModule = myAsmBuilder.DefineDynamicModule( "DynamicHandlersModule" );
            TypeBuilder typeBuilder = myModule.DefineType( "EventHandlersContainer", TypeAttributes.Public );

            int eventIndex = 0;
            Assembly asm = Assembly.GetExecutingAssembly();
            List<EventInfo> eventList = new List<EventInfo>();

            // find all events in current assembly, and create a handler for each one
            foreach( Type type in asm.GetTypes() ) {
                foreach( EventInfo eventInfo in type.GetEvents() ) {
                    // Skip non-static events
                    if( (eventInfo.GetAddMethod().Attributes & MethodAttributes.Static) != MethodAttributes.Static ) {
                        continue;
                    }
                    if( eventInfo.EventHandlerType.FullName.StartsWith( typeof( EventHandler<> ).FullName ) ||
                        eventInfo.EventHandlerType.FullName.StartsWith( typeof( EventHandler ).FullName ) ) {

                        if( useEventWhitelist && !eventWhitelist.Contains( type.Name + "." + eventInfo.Name, StringComparer.OrdinalIgnoreCase ) ||
                            useEventBlacklist && eventBlacklist.Contains( type.Name + "." + eventInfo.Name, StringComparer.OrdinalIgnoreCase ) ) continue;

                        MethodInfo method = eventInfo.EventHandlerType.GetMethod( "Invoke" );
                        var parameterTypes = method.GetParameters().Select( info => info.ParameterType ).ToArray();
                        AddEventHook( typeBuilder, parameterTypes, method.ReturnType, eventIndex );
                        eventList.Add( eventInfo );
                        eventsMap.Add( eventIndex, eventInfo );
                        eventIndex++;
                    }
                }
            }

            // hook up the handlers
            Type handlerType = typeBuilder.CreateType();
            for( int i = 0; i < eventList.Count; i++ ) {
                MethodInfo notifier = handlerType.GetMethod( "EventHook" + i );
                var handlerDelegate = Delegate.CreateDelegate( eventList[i].EventHandlerType, notifier );
                try {
                    eventList[i].AddEventHandler( null, handlerDelegate );
                } catch( TargetException ) {
                    // There's no way to tell if an event is static until you
                    // try adding a handler with target=null.
                    // If it wasn't static, TargetException is thrown
                }
            }
        }


        // create a static handler method that matches the given signature, and calls EventTraceNotifier
        static void AddEventHook( TypeBuilder typeBuilder, Type[] methodParams, Type returnType, int eventIndex ) {
            string methodName = "EventHook" + eventIndex;
            MethodBuilder methodBuilder = typeBuilder.DefineMethod( methodName,
                                                                    MethodAttributes.Public | MethodAttributes.Static,
                                                                    returnType,
                                                                    methodParams );

            ILGenerator generator = methodBuilder.GetILGenerator();
            generator.Emit( OpCodes.Ldc_I4, eventIndex );
            generator.Emit( OpCodes.Ldarg_1 );
            MethodInfo min = typeof( Logger ).GetMethod( "EventTraceNotifier" );
            generator.EmitCall( OpCodes.Call, min, null );
            generator.Emit( OpCodes.Ret );
        }


        // Invoked when events fire
        public static void EventTraceNotifier( int eventIndex, EventArgs e ) {
            if( (e is LogEventArgs) && ((LogEventArgs)e).MessageType == LogType.Trace ) return;
            var eventInfo = eventsMap[eventIndex];

            StringBuilder sb = new StringBuilder();
            bool first = true;
            foreach( var prop in e.GetType().GetProperties() ) {
                if( !first ) sb.Append( ", " );
                if( prop.Name != prop.PropertyType.Name ) {
                    sb.Append( prop.Name ).Append( '=' );
                }
                object val = prop.GetValue( e, null );
                if( val == null ) {
                    sb.Append( "null" );
                } else if( val is string ) {
                    sb.AppendFormat( "\"{0}\"", val );
                } else {
                    sb.Append( val );
                }
                first = false;
            }

            Log( LogType.Trace,
                 "TraceEvent: {0}.{1}( {2} )",
                 eventInfo.DeclaringType.Name, eventInfo.Name, sb.ToString() );

        }

#endif
        #endregion


        #region Events

        /// <summary> Occurs after a message has been logged. </summary>
        public static event EventHandler<LogEventArgs> Logged;


        /// <summary> Occurs when the server "crashes" (has an unhandled exception).
        /// Note that such occurences will not always cause shutdowns - check ShutdownImminent property.
        /// Reporting of the crash may be suppressed. </summary>
        public static event EventHandler<CrashedEventArgs> Crashed;


        [DebuggerStepThrough]
        static void RaiseLoggedEvent([NotNull] string rawMessage, [NotNull] string line, LogType logType)
        {
            if (rawMessage == null) throw new ArgumentNullException("rawMessage");
            if (line == null) throw new ArgumentNullException("line");
            var h = Logged;
            if (h != null) h(null, new LogEventArgs(rawMessage,
                                                      line,
                                                      logType,
                                                      LogFileOptions[(int)logType],
                                                      ConsoleOptions[(int)logType]));
        }


        static void RaiseCrashedEvent(CrashedEventArgs e)
        {
            var h = Crashed;
            if (h != null) h(null, e);
        }

        #endregion
    }


    #region Enums

    /// <summary> Category of a log event. </summary>
    public enum LogType
    {
        /// <summary> System activity (loading/saving of data, shutdown and startup events, etc). </summary>
        SystemActivity,

        ChangedWorld,

        /// <summary> Warnings (missing files, config discrepancies, minor recoverable errors, etc). </summary>
        Warning,

        /// <summary> Recoverable errors (loading/saving problems, connection problems, etc). </summary>
        Error,

        /// <summary> Critical non-recoverable errors and crashes. </summary>
        SeriousError,

        /// <summary> Routine user activity (command results, kicks, bans, etc). </summary>
        UserActivity,

        /// <summary> Raw commands entered by the player. </summary>
        UserCommand,

        /// <summary> Permission and hack related activity (name verification failures, banned players logging in, detected hacks, etc). </summary>
        SuspiciousActivity,

        /// <summary> Normal (white) chat written by the players. </summary>
        GlobalChat,

        /// <summary> Private messages exchanged by players. </summary>
        PrivateChat,

        /// <summary> Rank chat messages. </summary>
        RankChat,

        /// <summary> Messages and commands entered from console. </summary>
        ConsoleInput,

        /// <summary> Messages printed to the console (typically as the result of commands called from console). </summary>
        ConsoleOutput,

        /// <summary> Messages related to IRC activity.
        /// Does not include all messages relayed to/from IRC channels. </summary>
        IRC,

        /// <summary> Information useful for debugging (error details, routine events, system information). </summary>
        Debug,

        /// <summary> Special-purpose messages related to event tracing (never logged). </summary>
        Trace
    }


    /// <summary> Log splitting type. </summary>
    public enum LogSplittingType
    {
        /// <summary> All logs are written to one file. </summary>
        OneFile,

        /// <summary> A new timestamped logfile is made every time the server is started. </summary>
        SplitBySession,

        /// <summary> A new timestamped logfile is created every 24 hours. </summary>
        SplitByDay
    }

    #endregion
}


namespace fCraft.Events
{
    public sealed class LogEventArgs : EventArgs
    {
        [DebuggerStepThrough]
        internal LogEventArgs(string rawMessage, string message, LogType messageType, bool writeToFile, bool writeToConsole)
        {
            RawMessage = rawMessage;
            Message = message;
            MessageType = messageType;
            WriteToFile = writeToFile;
            WriteToConsole = writeToConsole;
        }
        public string RawMessage { get; private set; }
        public string Message { get; private set; }
        public LogType MessageType { get; private set; }
        public bool WriteToFile { get; private set; }
        public bool WriteToConsole { get; private set; }
    }


    public sealed class CrashedEventArgs : EventArgs
    {
        internal CrashedEventArgs(string message, string location, Exception exception, bool submitCrashReport, bool isCommonProblem, bool shutdownImminent)
        {
            Message = message;
            Location = location;
            Exception = exception;
            SubmitCrashReport = submitCrashReport;
            IsCommonProblem = isCommonProblem;
            ShutdownImminent = shutdownImminent;
        }
        public string Message { get; private set; }
        public string Location { get; private set; }
        public Exception Exception { get; private set; }
        public bool SubmitCrashReport { get; set; }
        public bool IsCommonProblem { get; private set; }
        public bool ShutdownImminent { get; private set; }
    }
}