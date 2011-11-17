/*
 *  Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a copy
 *  of this software and associated documentation files (the "Software"), to deal
 *  in the Software without restriction, including without limitation the rights
 *  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 *  copies of the Software, and to permit persons to whom the Software is
 *  furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 *  THE SOFTWARE.
 *
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading;
using System.Linq;
using System.Xml.Linq;
using fCraft.UpdateInstaller.Properties;


namespace fCraft.UpdateInstaller {
    static class Program {
        const string ConfigFileNameDefault = "config.xml",
                     BackupFileNameFormat = "800craftData_{0:yyyyMMdd'_'HH'-'mm'-'ss}_BeforeUpdate.zip";

        static readonly string[] FilesToBackup = new[]{
            "PlayerDB.txt",
            "config.xml",
            "ipbans.txt",
            "worlds.xml"
        };

        static readonly string[] LegacyFiles = new[]{
            "fCraftConsole.exe",
            "fCraftUI.exe",
            "ConfigTool.exe",
            "fCraftWinService.exe"
        };


        static int Main( string[] args ) {
            string restartTarget = null;
            string configFileName = ConfigFileNameDefault;

            // Set path
            string defaultPath = Path.GetFullPath( Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location ) );
            Directory.SetCurrentDirectory( defaultPath );

            // Parse command-line arguments
            List<string> argsList = new List<string>();
            foreach( string arg in args ) {
                Console.WriteLine( arg );
                if( arg.StartsWith( "--path=", StringComparison.OrdinalIgnoreCase ) ) {
                    Directory.SetCurrentDirectory( arg.Substring( arg.IndexOf( '=' ) + 1 ).TrimQuotes() );
                    argsList.Add( arg );
                } else if( arg.StartsWith( "--config=", StringComparison.OrdinalIgnoreCase ) ) {
                    configFileName = arg.Substring( arg.IndexOf( '=' ) + 1 ).TrimQuotes();
                    argsList.Add( arg );
                } else if( arg.StartsWith( "--restart=", StringComparison.OrdinalIgnoreCase ) ) {
                    restartTarget = arg.Substring( arg.IndexOf( '=' ) + 1 ).TrimQuotes();
                } else if( arg != "&" ) {
                    argsList.Add( arg );
                }
            }

            // Parse update settings
            string runBefore = null,
                   runAfter = null;
            bool doBackup = true;

            try {
                if( File.Exists( configFileName ) ) {
                    XDocument doc = XDocument.Load( configFileName );
                    if( doc.Root != null ) {
                        XElement elRunBefore = doc.Root.Element( "RunBeforeUpdate" );
                        if( elRunBefore != null ) runBefore = elRunBefore.Value;
                        XElement elRunAfter = doc.Root.Element( "RunAfterUpdate" );
                        if( elRunAfter != null ) runAfter = elRunAfter.Value;
                        XElement elDoBackup = doc.Root.Element( "BackupBeforeUpdate" );
                        if( elDoBackup != null && !String.IsNullOrEmpty( elDoBackup.Value ) ) {
                            if( !Boolean.TryParse( elDoBackup.Value, out doBackup ) ) {
                                doBackup = true;
                            }
                        }
                    }
                }
            } catch( Exception ex ) {
                Console.Error.WriteLine( "Error reading fCraft config: {0}", ex );
            }

            // Backup data files (if requested)
            if( doBackup ) DoBackup();

            // Run pre-update script (if any)
            if( !String.IsNullOrEmpty( runBefore ) ) {
                Console.WriteLine( "Executing pre-update script..." );
                try {
                    Process preUpdateProcess = Process.Start( runBefore, "" );
                    if( preUpdateProcess != null ) preUpdateProcess.WaitForExit();
                } catch( Exception ex ) {
                    Console.Error.WriteLine( "Failed to run pre-update process, aborting update application: {0}", ex );
                    return (int)ReturnCodes.FailedToRunPreUpdateCommand;
                }
            }


            // Apply the update
            using( MemoryStream ms = new MemoryStream( Resources.Payload ) ) {
                using( ZipStorer zs = ZipStorer.Open( ms, FileAccess.Read ) ) {

                    var allFiles = zs.ReadCentralDir().Select( entry => entry.FilenameInZip ).Union( LegacyFiles );

                    // ensure that fcraft files are writable
                    bool allPassed;
                    do {
                        allPassed = true;
                        foreach( var fileName in allFiles ) {
                            try {
                                FileInfo fi = new FileInfo( fileName );
                                if( !fi.Exists ) continue;
                                using( fi.OpenWrite() ) { }

                            } catch( Exception ex ) {
                                if( ex is IOException ) {
                                    Console.WriteLine( "Waiting for 800craft-related applications to close..." );
                                } else {
                                    Console.Error.WriteLine( "ERROR: could not write to {0}: {1} - {2}",
                                                             fileName, ex.GetType().Name, ex.Message );
                                    Console.WriteLine();
                                }
                                allPassed = false;
                                Thread.Sleep( 1000 );
                                break;
                            }
                        }
                    } while( !allPassed );

                    // delete legacy files
                    foreach( var legacyFile in LegacyFiles ) {
                        try {
                            File.Delete( legacyFile );
                        } catch( Exception ex ) {
                            Console.Error.WriteLine( "    ERROR: {0} {1}", ex.GetType().Name, ex.Message );
                        }
                    }

                    // extract files
                    foreach( var entry in zs.ReadCentralDir() ) {
                        Console.WriteLine( "Extracting {0}", entry.FilenameInZip );
                        try {
                            using( FileStream fs = File.Create( entry.FilenameInZip ) ) {
                                zs.ExtractFile( entry, fs );
                            }
                        } catch( Exception ex ) {
                            Console.Error.WriteLine( "    ERROR: {0} {1}", ex.GetType().Name, ex.Message );
                        }
                    }
                }
            }

            // Run post-update script
            if( !String.IsNullOrEmpty( runAfter ) ) {
                Console.WriteLine( "Executing post-update script..." );
                try {
                    Process postUpdateProcess = Process.Start( runAfter, "" );
                    if( postUpdateProcess != null ) postUpdateProcess.WaitForExit();
                } catch( Exception ex ) {
                    Console.Error.WriteLine( "Failed to run post-update process, aborting restart: {0}", ex );
                    return (int)ReturnCodes.FailedToRunPostUpdateCommand;
                }
            }

            Console.WriteLine( "800craft update complete." );

            // Restart fCraft (if requested)
            if( restartTarget != null ) {
                if(restartTarget=="fCraftConsole.exe"){
                    restartTarget = "ServerCLI.exe";
                }else if( restartTarget=="fCraftUI.exe"){
                    restartTarget = "ServerGUI.exe";
                }

                if( !File.Exists( restartTarget ) ) {
                    Console.Error.WriteLine( "Restart target not found, quitting: {0}", restartTarget );
                    return (int)ReturnCodes.RestartTargetNotFound;
                }
                string argString = String.Join( " ", argsList.ToArray() );
                Console.WriteLine( "Starting: {0} {1}", restartTarget, argString );
                switch( Environment.OSVersion.Platform ) {
                    case PlatformID.MacOSX:
                    case PlatformID.Unix:
                        Process.Start( "mono", "\"" + restartTarget + "\" " + argString + " &" );
                        break;
                    default:
                        Process.Start( restartTarget, argString );
                        break;
                }
            }

            return (int)ReturnCodes.Ok;
        }


        static void DoBackup() {
            string backupFileName = String.Format( BackupFileNameFormat, DateTime.Now ); // localized
            using( FileStream fs = File.Create( backupFileName ) ) {
                using( ZipStorer backupZip = ZipStorer.Create( fs, "" ) ) {
                    foreach( string dataFileName in FilesToBackup ) {
                        if( File.Exists( dataFileName ) ) {
                            backupZip.AddFile( ZipStorer.Compression.Deflate, dataFileName, dataFileName, "" );
                        }
                    }
                }
            }
        }


        static string TrimQuotes( this string str ) {
            if( str.StartsWith( "\"" ) && str.EndsWith( "\"" ) ) {
                return str.Substring( 1, str.Length - 2 );
            } else {
                return str;
            }
        }
    }

    enum ReturnCodes {
        Ok = 0,
        FailedToRunPreUpdateCommand = 1,
        FailedToRunPostUpdateCommand = 2,
        RestartTargetNotFound = 3
    }
}