// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using JetBrains.Annotations;


namespace fCraft {
    /// <summary> Contains fCraft path settings, and some filesystem-related utilities. </summary>
    public static class Paths {

        static readonly string[] ProtectedFiles;

        internal static readonly string[] DataFilesToBackup;

        static Paths() {
            string assemblyDir = Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location );
            if( assemblyDir != null ) {
                WorkingPathDefault = Path.GetFullPath( assemblyDir );
            } else {
                WorkingPathDefault = Path.GetPathRoot( assemblyDir );
            }

            WorkingPath = WorkingPathDefault;
            MapPath = MapPathDefault;
            LogPath = LogPathDefault;
            ConfigFileName = ConfigFileNameDefault;

            ProtectedFiles = new[]{
                "ConfigGUI.exe",
                "ConfigCLI.exe",
                "fCraft.dll",
                "fCraftGUI.dll",
                "ServerCLI.exe",
                "ServerGUI.exe",
                "ServerWinService.exe",
                UpdaterFileName,
                ConfigFileNameDefault,
                PlayerDBFileName,
                IPBanListFileName,
                RulesFileName,
                AnnouncementsFileName,
                GreetingFileName,
                HeartbeatDataFileName,
                WorldListFileName,

                BasscannonFileName,
                 ReqFileName,
                AutoRankFileName
            };

            DataFilesToBackup = new[]{
                PlayerDBFileName,
                IPBanListFileName,
                WorldListFileName,
                ConfigFileName
            };
        }


        #region Paths & Properties

        public static bool IgnoreMapPathConfigKey { get; internal set; }

        public const string MapPathDefault = "maps",
                            LogPathDefault = "logs",
                            ConfigFileNameDefault = "config.xml";

        public static readonly string WorkingPathDefault;

        /// <summary> Path to save maps to (default: .\maps)
        /// Can be overridden at startup via command-line argument "--mappath=",
        /// or via "MapPath" ConfigKey </summary>
        public static string MapPath { get; set; }

        /// <summary> Working path (default: whatever directory fCraft.dll is located in)
        /// Can be overridden at startup via command line argument "--path=" </summary>
        public static string WorkingPath { get; set; }

        /// <summary> Path to save logs to (default: .\logs)
        /// Can be overridden at startup via command-line argument "--logpath=" </summary>
        public static string LogPath { get; set; }

        /// <summary> Path to load/save config to/from (default: .\config.xml)
        /// Can be overridden at startup via command-line argument "--config=" </summary>
        public static string ConfigFileName { get; set; }


        public const string PlayerDBFileName = "PlayerDB.txt";

        public const string PortalDBFileName = "PortalDB.txt";
        public const string HbDataFileName = "HeartbeatSaver.txt";

        public const string IPBanListFileName = "ipbans.txt";

        public const string GreetingFileName = "greeting.txt";

        public const string AnnouncementsFileName = "announcements.txt";

        public const string RulesFileName = "rules.txt";

        public const string RulesDirectory = "rules";

        public const string HeartbeatDataFileName = "heartbeatdata.txt";

        public const string UpdaterFileName = "UpdateInstaller.exe";

        public const string WorldListFileName = "worlds.xml";

        public const string AutoRankFileName = "autorank.xml";

        public const string BlockDBDirectory = "blockdb";
        public const string ReqDirectory = "req";
        public const string ReqFileName = "requirements.txt";
        public const string BasscannonFileName = "basscannon.txt";


        public static string BlockDBPath {
            get { return Path.Combine( WorkingPath, BlockDBDirectory ); }
        }
        public static string ReqPath
        {
            get { return Path.Combine(WorkingPath, ReqDirectory); }
        }

        public static string RulesPath {
            get { return Path.Combine( WorkingPath, RulesDirectory ); }
        }

        /// <summary> Path where map backups are stored </summary>
        public static string BackupPath {
            get {
                return Path.Combine( MapPath, "backups" );
            }
        }

        #endregion


        #region Utility Methods

        public static void MoveOrReplace( [NotNull] string source, [NotNull] string destination ) {
            if( source == null ) throw new ArgumentNullException( "source" );
            if( destination == null ) throw new ArgumentNullException( "destination" );
            if( File.Exists( destination ) ) {
                if( Path.GetPathRoot( Path.GetFullPath( source ) ) == Path.GetPathRoot( Path.GetFullPath( destination ) ) ) {
                    File.Replace( source, destination, null, true );
                } else {
                    File.Delete( destination );
                    File.Move( source, destination );
                }
            } else {
                File.Move( source, destination );
            }
        }


        /// <summary> Makes sure that the path format is valid, that it exists, that it is accessible and writeable. </summary>
        /// <param name="pathLabel"> Name of the path that's being tested (e.g. "map path"). Used for logging. </param>
        /// <param name="path"> Full or partial path. </param>
        /// <param name="checkForWriteAccess"> If set, tries to write to the given directory. </param>
        /// <returns> Full path of the directory (on success) or null (on failure). </returns>
        public static bool TestDirectory( [NotNull] string pathLabel, [NotNull] string path, bool checkForWriteAccess ) {
            if( pathLabel == null ) throw new ArgumentNullException( "pathLabel" );
            if( path == null ) throw new ArgumentNullException( "path" );
            try {
                if( !Directory.Exists( path ) ) {
                    Directory.CreateDirectory( path );
                }
                DirectoryInfo info = new DirectoryInfo( path );
                if( checkForWriteAccess ) {
                    string randomFileName = Path.Combine( info.FullName, "fCraft_write_test_" + Guid.NewGuid() );
                    using( File.Create( randomFileName ) ) { }
                    File.Delete( randomFileName );
                }
                return true;

            } catch( Exception ex ) {
                if( ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException ) {
                    Logger.Log( LogType.Error,
                                "Paths.TestDirectory: Specified path for {0} is invalid or incorrectly formatted ({1}: {2}).",
                                pathLabel, ex.GetType().Name, ex.Message );
                } else if( ex is SecurityException || ex is UnauthorizedAccessException ) {
                    Logger.Log( LogType.Error,
                                "Paths.TestDirectory: Cannot create or write to file/path for {0}, please check permissions ({1}: {2}).",
                                pathLabel, ex.GetType().Name, ex.Message );
                } else if( ex is DirectoryNotFoundException ) {
                    Logger.Log( LogType.Error,
                                "Paths.TestDirectory: Drive/volume for {0} does not exist or is not mounted ({1}: {2}).",
                                pathLabel, ex.GetType().Name, ex.Message );
                } else if( ex is IOException ) {
                    Logger.Log( LogType.Error,
                                "Paths.TestDirectory: Specified directory for {0} is not readable/writable ({1}: {2}).",
                                pathLabel, ex.GetType().Name, ex.Message );
                } else {
                    throw;
                }
            }
            return false;
        }


        /// <summary> Makes sure that the path format is valid, and optionally whether it is readable/writeable. </summary>
        /// <param name="fileLabel"> Name of the path that's being tested (e.g. "map path"). Used for logging. </param>
        /// <param name="filename"> Full or partial path of the file. </param>
        /// <param name="createIfDoesNotExist"> If target file is missing and this option is OFF, TestFile returns true.
        /// If target file is missing and this option is ON, TestFile tries to create
        /// a file and returns whether it succeeded. </param>
        /// <param name="neededAccess"> If file is present, type of access to test. </param>
        /// <returns> Whether target file passed all tests. </returns>
        public static bool TestFile( [NotNull] string fileLabel, [NotNull] string filename,
                                     bool createIfDoesNotExist, FileAccess neededAccess ) {
            if( fileLabel == null ) throw new ArgumentNullException( "fileLabel" );
            if( filename == null ) throw new ArgumentNullException( "filename" );
            try {
                new FileInfo( filename );
                if( File.Exists( filename ) ) {
                    if( (neededAccess & FileAccess.Read) == FileAccess.Read ) {
                        using( File.OpenRead( filename ) ) { }
                    }
                    if( (neededAccess & FileAccess.Write) == FileAccess.Write ) {
                        using( File.OpenWrite( filename ) ) { }
                    }
                } else if( createIfDoesNotExist ) {
                    using( File.Create( filename ) ) { }
                }
                return true;

            } catch( Exception ex ) {
                if( ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException ) {
                    Logger.Log( LogType.Error,
                                "Paths.TestFile: Specified path for {0} is invalid or incorrectly formatted ({1}: {2}).",
                                fileLabel, ex.GetType().Name, ex.Message );
                } else if( ex is SecurityException || ex is UnauthorizedAccessException ) {
                    Logger.Log( LogType.Error,
                                "Paths.TestFile: Cannot create or write to {0}, please check permissions ({1}: {2}).",
                                fileLabel, ex.GetType().Name, ex.Message );
                } else if( ex is DirectoryNotFoundException ) {
                    Logger.Log( LogType.Error,
                                "Paths.TestFile: Drive/volume for {0} does not exist or is not mounted ({1}: {2}).",
                                fileLabel, ex.GetType().Name, ex.Message );
                } else if( ex is IOException ) {
                    Logger.Log( LogType.Error,
                                "Paths.TestFile: Specified file for {0} is not readable/writable ({1}: {2}).",
                                fileLabel, ex.GetType().Name, ex.Message );
                } else {
                    throw;
                }
            }
            return false;
        }


        public static bool IsDefaultMapPath( [CanBeNull] string path ) {
            return String.IsNullOrEmpty( path ) || Compare( MapPathDefault, path );
        }


        /// <summary>Returns true if paths or filenames reference the same location (accounts for all the filesystem quirks).</summary>
        public static bool Compare( [NotNull] string p1, [NotNull] string p2 ) {
            if( p1 == null ) throw new ArgumentNullException( "p1" );
            if( p2 == null ) throw new ArgumentNullException( "p2" );
            return Compare( p1, p2, MonoCompat.IsCaseSensitive );
        }


        /// <summary>Returns true if paths or filenames reference the same location (accounts for all the filesystem quirks).</summary>
        public static bool Compare( [NotNull] string p1, [NotNull] string p2, bool caseSensitive ) {
            if( p1 == null ) throw new ArgumentNullException( "p1" );
            if( p2 == null ) throw new ArgumentNullException( "p2" );
            StringComparison sc = (caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
            return String.Equals( Path.GetFullPath( p1 ).TrimEnd( Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar ),
                                  Path.GetFullPath( p2 ).TrimEnd( Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar ),
                                  sc );
        }


        public static bool IsValidPath( string path ) {
            try {
                new FileInfo( path );
                return true;
            } catch( ArgumentException ) {
            } catch( PathTooLongException ) {
            } catch( NotSupportedException ) {
            }
            return false;
        }


        /// <summary> Checks whether childPath is inside parentPath </summary>
        /// <param name="parentPath">Path that is supposed to contain childPath</param>
        /// <param name="childPath">Path that is supposed to be contained within parentPath</param>
        /// <returns>true if childPath is contained within parentPath</returns>
        public static bool Contains( [NotNull] string parentPath, [NotNull] string childPath ) {
            if( parentPath == null ) throw new ArgumentNullException( "parentPath" );
            if( childPath == null ) throw new ArgumentNullException( "childPath" );
            return Contains( parentPath, childPath, MonoCompat.IsCaseSensitive );
        }


        /// <summary> Checks whether childPath is inside parentPath </summary>
        /// <param name="parentPath"> Path that is supposed to contain childPath </param>
        /// <param name="childPath"> Path that is supposed to be contained within parentPath </param>
        /// <param name="caseSensitive"> Whether check should be case-sensitive or case-insensitive. </param>
        /// <returns> true if childPath is contained within parentPath </returns>
        public static bool Contains( [NotNull] string parentPath, [NotNull] string childPath, bool caseSensitive ) {
            if( parentPath == null ) throw new ArgumentNullException( "parentPath" );
            if( childPath == null ) throw new ArgumentNullException( "childPath" );
            string fullParentPath = Path.GetFullPath( parentPath ).TrimEnd( Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar );
            string fullChildPath = Path.GetFullPath( childPath ).TrimEnd( Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar );
            StringComparison sc = (caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
            return fullChildPath.StartsWith( fullParentPath, sc );
        }


        /// <summary> Checks whether the file exists in a specified way (case-sensitive or case-insensitive) </summary>
        /// <param name="fileName"> filename in question </param>
        /// <param name="caseSensitive"> Whether check should be case-sensitive or case-insensitive. </param>
        /// <returns> true if file exists, otherwise false </returns>
        public static bool FileExists( [NotNull] string fileName, bool caseSensitive ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            if( caseSensitive == MonoCompat.IsCaseSensitive ) {
                return File.Exists( fileName );
            } else {
                return new FileInfo( fileName ).Exists( caseSensitive );
            }
        }


        /// <summary>Checks whether the file exists in a specified way (case-sensitive or case-insensitive)</summary>
        /// <param name="fileInfo">FileInfo object in question</param>
        /// <param name="caseSensitive">Whether check should be case-sensitive or case-insensitive.</param>
        /// <returns>true if file exists, otherwise false</returns>
        public static bool Exists( [NotNull] this FileInfo fileInfo, bool caseSensitive ) {
            if( fileInfo == null ) throw new ArgumentNullException( "fileInfo" );
            if( caseSensitive == MonoCompat.IsCaseSensitive ) {
                return fileInfo.Exists;
            } else {
                DirectoryInfo parentDir = fileInfo.Directory;
                StringComparison sc = (caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
                return parentDir.GetFiles( "*", SearchOption.TopDirectoryOnly )
                                .Any( file => file.Name.Equals( fileInfo.Name, sc ) );
            }
        }


        /// <summary> Allows making changes to filename capitalization on case-insensitive filesystems. </summary>
        /// <param name="originalFullFileName"> Full path to the original filename </param>
        /// <param name="newFileName"> New file name (do not include the full path) </param>
        public static void ForceRename( [NotNull] string originalFullFileName, [NotNull] string newFileName ) {
            if( originalFullFileName == null ) throw new ArgumentNullException( "originalFullFileName" );
            if( newFileName == null ) throw new ArgumentNullException( "newFileName" );
            FileInfo originalFile = new FileInfo( originalFullFileName );
            if( originalFile.Name == newFileName ) return;
            FileInfo newFile = new FileInfo( Path.Combine( originalFile.DirectoryName, newFileName ) );
            string tempFileName = originalFile.FullName + Guid.NewGuid();
            MoveOrReplace( originalFile.FullName, tempFileName );
            MoveOrReplace( tempFileName, newFile.FullName );
        }


        /// <summary> Find files that match the name in a case-insensitive way. </summary>
        /// <param name="fullFileName"> Case-insensitive filename to look for. </param>
        /// <returns> Array of matches. Empty array if no files matches. </returns>
        public static FileInfo[] FindFiles( [NotNull] string fullFileName ) {
            if( fullFileName == null ) throw new ArgumentNullException( "fullFileName" );
            FileInfo fi = new FileInfo( fullFileName );
            DirectoryInfo parentDir = fi.Directory;
            return parentDir.GetFiles( "*", SearchOption.TopDirectoryOnly )
                            .Where( file => file.Name.Equals( fi.Name, StringComparison.OrdinalIgnoreCase ) )
                            .ToArray();
        }


        public static bool IsProtectedFileName( [NotNull] string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            return ProtectedFiles.Any( t => Compare( t, fileName ) );
        }

        #endregion
    }
}