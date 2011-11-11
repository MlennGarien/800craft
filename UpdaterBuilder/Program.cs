using System.IO;
using System.IO.Compression;

namespace fCraft.UpdateBuilder {
    static class Program {

        static readonly string[] FileList = {
            "ConfigGUI.exe",
            "fCraft.dll",
            "fCraftGUI.dll",
            "ServerCLI.exe",
            "ServerGUI.exe",
            "../../CHANGELOG.txt",
            "../../README.txt"
        };

        const string BinariesFileName = "../../UpdateInstaller/Resources/Payload.zip";


        static void Main() {
            FileInfo binaries = new FileInfo( BinariesFileName );
            if( binaries.Exists ) {
                binaries.Delete();
            }

            using( ZipStorer zs = ZipStorer.Create( binaries.FullName, "" ) ) {
                foreach( string file in FileList ) {
                    FileInfo fi = new FileInfo( file );
                    if( !fi.Exists ) {
                        return; // abort if any of the files do not exist
                    }
                    zs.AddFile( ZipStorer.Compression.Deflate, fi.FullName, fi.Name, "" );
                }
            }
        }
    }
}