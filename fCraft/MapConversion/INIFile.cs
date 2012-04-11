// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;

namespace fCraft.MapConversion {
    /// <summary> INI parser used by MapMyne. </summary>
    sealed class INIFile {
        const string Separator = "=";
        readonly Dictionary<string, Dictionary<string, string>> contents = new Dictionary<string, Dictionary<string, string>>();

        public string this[[NotNull] string section, [NotNull] string key] {
            get {
                if( section == null ) throw new ArgumentNullException( "section" );
                if( key == null ) throw new ArgumentNullException( "key" );
                return contents[section][key];
            }
            set {
                if( section == null ) throw new ArgumentNullException( "section" );
                if( key == null ) throw new ArgumentNullException( "key" );
                if( value == null ) throw new ArgumentNullException( "value" );
                if( !contents.ContainsKey( section ) ) {
                    contents[section] = new Dictionary<string, string>();
                }
                contents[section][key] = value;
            }
        }

        public INIFile( [NotNull] Stream fileStream ) {
            if( fileStream == null ) throw new ArgumentNullException( "fileStream" );
            StreamReader reader = new StreamReader( fileStream );
            Dictionary<string, string> section = null;
            while( true ) {
                string line = reader.ReadLine();
                if( line == null ) break;

                line = line.Trim();
                if( line.StartsWith( "#" ) ) continue;
                if( line.StartsWith( "[" ) ) {
                    string sectionName = line.Substring( 1, line.IndexOf( ']' ) - 1 ).Trim().ToLower();
                    section = new Dictionary<string, string>();
                    contents.Add( sectionName, section );
                } else if( line.Contains( Separator ) && section != null ) {
                    string keyName = line.Substring( 0, line.IndexOf( Separator ) ).TrimEnd().ToLower();
                    string valueName = line.Substring( line.IndexOf( Separator ) + 1 ).TrimStart();
                    section.Add( keyName, valueName );
                }
            }
        }


        public bool ContainsSection( [NotNull] string section ) {
            if( section == null ) throw new ArgumentNullException( "section" );
            return contents.ContainsKey( section.ToLower() );
        }

        public bool Contains( [NotNull] string section, [NotNull] params string[] keys ) {
            if( section == null ) throw new ArgumentNullException( "section" );
            if( keys == null ) throw new ArgumentNullException( "keys" );
            if( contents.ContainsKey( section.ToLower() ) ) {
                return keys.All( key => contents[section.ToLower()].ContainsKey( key.ToLower() ) );
            } else {
                return false;
            }
        }

        public bool IsEmpty {
            get {
                return (contents.Count == 0);
            }
        }
    }
}
