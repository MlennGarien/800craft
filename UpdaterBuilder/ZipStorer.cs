// ZipStorer, by Jaime Olivares
// Website: zipstorer.codeplex.com
// Version: 2.35 (March 14, 2010)
using System.Collections.Generic;
using System.Text;

namespace System.IO.Compression {
    /// <summary>
    /// Unique class for compression/decompression file. Represents a Zip file.
    /// </summary>
    public sealed class ZipStorer : IDisposable {
        /// <summary>
        /// Compression method enumeration
        /// </summary>
        public enum Compression : ushort {
            /// <summary>Uncompressed storage</summary> 
            Store = 0,
            /// <summary>Deflate compression method</summary>
            Deflate = 8
        }

        /// <summary>
        /// Represents an entry in Zip file directory
        /// </summary>
        public struct ZipFileEntry {
            /// <summary>Compression method</summary>
            public Compression Method;
            /// <summary>Full path and filename as stored in Zip</summary>
            public string FilenameInZip;
            /// <summary>Original file size</summary>
            public uint FileSize;
            /// <summary>Compressed file size</summary>
            public uint CompressedSize;
            /// <summary>Offset of header information inside Zip storage</summary>
            public uint HeaderOffset;
            /// <summary>Offset of file inside Zip storage</summary>
            public uint FileOffset;
            /// <summary>Size of header information</summary>
            public uint HeaderSize;
            /// <summary>32-bit checksum of entire file</summary>
            public uint Crc32;
            /// <summary>Last modification time of file</summary>
            public DateTime ModifyTime;
            /// <summary>User comment for file</summary>
            public string Comment;
            /// <summary>True if UTF8 encoding for filename and comments, false if default (CP 437)</summary>
            public bool EncodeUTF8;

            /// <summary>Overriden method</summary>
            /// <returns>Filename in Zip</returns>
            public override string ToString() {
                return FilenameInZip;
            }
        }

        #region Public fields
        /// <summary>True if UTF8 encoding for filename and comments, false if default (CP 437)</summary>
        public bool EncodeUTF8;
        /// <summary>Force deflate algotithm even if it inflates the stored file. Off by default.</summary>
        public bool ForceDeflating;
        #endregion

        #region Private fields
        // List of files to store
        private readonly List<ZipFileEntry> files = new List<ZipFileEntry>();
        // Filename of storage file
        private string fileName;
        // Stream object of storage file
        private Stream zipFileStream;
        // General comment
        private string comment = "";
        // Central dir image
        private byte[] centralDirImage;
        // Existing files in zip
        private ushort existingFiles;
        // File access for Open method
        private FileAccess access;
        // Static CRC32 Table
        private readonly static UInt32[] CrcTable;
        // Default filename encoder
        private readonly static Encoding DefaultEncoding = Encoding.GetEncoding( 437 );
        #endregion

        #region Public methods
        // Static constructor. Just invoked once in order to create the CRC32 lookup table.
        static ZipStorer() {
            // Generate CRC32 table
            CrcTable = new UInt32[256];
            for( int i = 0; i < CrcTable.Length; i++ ) {
                UInt32 c = (UInt32)i;
                for( int j = 0; j < 8; j++ ) {
                    if( ( c & 1 ) != 0 )
                        c = 3988292384 ^ ( c >> 1 );
                    else
                        c >>= 1;
                }
                CrcTable[i] = c;
            }
        }
        /// <summary>
        /// Method to create a new storage file
        /// </summary>
        /// <param name="filename">Full path of Zip file to create</param>
        /// <param name="fileComment">General comment for Zip file</param>
        /// <returns>A valid ZipStorer object</returns>
        public static ZipStorer Create( string filename, string fileComment ) {
            Stream stream = new FileStream( filename, FileMode.Create, FileAccess.ReadWrite );

            ZipStorer zip = Create( stream, fileComment );
            zip.comment = fileComment;
            zip.fileName = filename;

            return zip;
        }
        /// <summary>
        /// Method to create a new zip storage in a stream
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="fileComment"></param>
        /// <returns>A valid ZipStorer object</returns>
        public static ZipStorer Create( Stream stream, string fileComment ) {
            ZipStorer zip = new ZipStorer { comment = fileComment, zipFileStream = stream, access = FileAccess.Write };

            return zip;
        }


        /// <summary>
        /// Method to open an existing storage file
        /// </summary>
        /// <param name="filename">Full path of Zip file to open</param>
        /// <param name="fileAccess">File access mode as used in FileStream constructor</param>
        /// <returns>A valid ZipStorer object</returns>
        public static ZipStorer Open( string filename, FileAccess fileAccess ) {
            Stream stream = new FileStream( filename, FileMode.Open, fileAccess == FileAccess.Read ? FileAccess.Read : FileAccess.ReadWrite );

            ZipStorer zip = Open( stream, fileAccess );
            zip.fileName = filename;

            return zip;
        }
        /// <summary>
        /// Method to open an existing storage from stream
        /// </summary>
        /// <param name="stream">Already opened stream with zip contents</param>
        /// <param name="fileFileAccess">File access mode for stream operations</param>
        /// <returns>A valid ZipStorer object</returns>
        public static ZipStorer Open( Stream stream, FileAccess fileFileAccess ) {
            if( !stream.CanSeek && fileFileAccess != FileAccess.Read )
                throw new InvalidOperationException( "Stream cannot seek" );

            ZipStorer zip = new ZipStorer { zipFileStream = stream, access = fileFileAccess };

            if( zip.ReadFileInfo() )
                return zip;

            throw new InvalidDataException();
        }
        /// <summary>
        /// Add full contents of a file into the Zip storage
        /// </summary>
        /// <param name="method">Compression method</param>
        /// <param name="pathname">Full path of file to add to Zip storage</param>
        /// <param name="filenameInZip">Filename and path as desired in Zip directory</param>
        /// <param name="fileComment">Comment for stored file</param>        
        public void AddFile( Compression method, string pathname, string filenameInZip, string fileComment ) {
            if( access == FileAccess.Read )
                throw new InvalidOperationException( "Writing is not alowed" );

            FileStream stream = new FileStream( pathname, FileMode.Open, FileAccess.Read );
            AddStream( method, filenameInZip, stream, File.GetLastWriteTime( pathname ), fileComment );
            stream.Close();
        }
        /// <summary>
        /// Add full contents of a stream into the Zip storage
        /// </summary>
        /// <param name="method">Compression method</param>
        /// <param name="filenameInZip">Filename and path as desired in Zip directory</param>
        /// <param name="source">Stream object containing the data to store in Zip</param>
        /// <param name="modTime">Modification time of the data to store</param>
        /// <param name="fileComment">Comment for stored file</param>
        public void AddStream( Compression method, string filenameInZip, Stream source, DateTime modTime, string fileComment ) {
            if( access == FileAccess.Read )
                throw new InvalidOperationException( "Writing is not alowed" );

            /*long offset;
            if( Files.Count == 0 )
                offset = 0;
            else {
                ZipFileEntry last = Files[Files.Count - 1];
                offset = last.HeaderOffset + last.HeaderSize;
            }*/

            // Prepare the fileinfo
            ZipFileEntry zfe = new ZipFileEntry {
                Method = method,
                EncodeUTF8 = EncodeUTF8,
                FilenameInZip = NormalizedFilename( filenameInZip ),
                Comment = ( fileComment ?? "" ),
                Crc32 = 0,
                HeaderOffset = (uint)zipFileStream.Position,
                ModifyTime = modTime
            };

            // Even though we write the header now, it will have to be rewritten, since we don't know compressed size or crc.

            // Write local header
            WriteLocalHeader( ref zfe );
            zfe.FileOffset = (uint)zipFileStream.Position;

            // Write file to zip (store)
            Store( ref zfe, source );
            source.Close();

            UpdateCrcAndSizes( ref zfe );

            files.Add( zfe );
        }
        /// <summary>
        /// Updates central directory (if pertinent) and close the Zip storage
        /// </summary>
        /// <remarks>This is a required step, unless automatic dispose is used</remarks>
        public void Close() {
            if( access != FileAccess.Read ) {
                uint centralOffset = (uint)zipFileStream.Position;
                uint centralSize = 0;

                if( centralDirImage != null )
                    zipFileStream.Write( centralDirImage, 0, centralDirImage.Length );

                for( int i = 0; i < files.Count; i++ ) {
                    long pos = zipFileStream.Position;
                    WriteCentralDirRecord( files[i] );
                    centralSize += (uint)( zipFileStream.Position - pos );
                }

                if( centralDirImage != null )
                    WriteEndRecord( centralSize + (uint)centralDirImage.Length, centralOffset );
                else
                    WriteEndRecord( centralSize, centralOffset );
            }

            if( zipFileStream == null ) return;

            zipFileStream.Flush();
            zipFileStream.Dispose();
            zipFileStream = null;
        }
        /// <summary>
        /// Read all the file records in the central directory 
        /// </summary>
        /// <returns>List of all entries in directory</returns>
        public List<ZipFileEntry> ReadCentralDir() {
            if( centralDirImage == null )
                throw new InvalidOperationException( "Central directory currently does not exist" );

            List<ZipFileEntry> result = new List<ZipFileEntry>();

            for( int pointer = 0; pointer < centralDirImage.Length; ) {
                uint signature = BitConverter.ToUInt32( centralDirImage, pointer );
                if( signature != 0x02014b50 )
                    break;

                bool encodeUTF8 = ( BitConverter.ToUInt16( centralDirImage, pointer + 8 ) & 0x0800 ) != 0;
                ushort method = BitConverter.ToUInt16( centralDirImage, pointer + 10 );
                uint modifyTime = BitConverter.ToUInt32( centralDirImage, pointer + 12 );
                uint crc32 = BitConverter.ToUInt32( centralDirImage, pointer + 16 );
                uint comprSize = BitConverter.ToUInt32( centralDirImage, pointer + 20 );
                uint fileSize = BitConverter.ToUInt32( centralDirImage, pointer + 24 );
                ushort filenameSize = BitConverter.ToUInt16( centralDirImage, pointer + 28 );
                ushort extraSize = BitConverter.ToUInt16( centralDirImage, pointer + 30 );
                ushort commentSize = BitConverter.ToUInt16( centralDirImage, pointer + 32 );
                uint headerOffset = BitConverter.ToUInt32( centralDirImage, pointer + 42 );
                uint headerSize = (uint)( 46 + filenameSize + extraSize + commentSize );

                Encoding encoder = encodeUTF8 ? Encoding.UTF8 : DefaultEncoding;

                ZipFileEntry zfe = new ZipFileEntry {
                    Method = (Compression)method,
                    FilenameInZip =
                        encoder.GetString( centralDirImage, pointer + 46,
                                           filenameSize ),
                    FileOffset = GetFileOffset( headerOffset ),
                    FileSize = fileSize,
                    CompressedSize = comprSize,
                    HeaderOffset = headerOffset,
                    HeaderSize = headerSize,
                    Crc32 = crc32,
                    ModifyTime = DosTimeToDateTime( modifyTime )
                };
                if( commentSize > 0 )
                    zfe.Comment = encoder.GetString( centralDirImage, pointer + 46 + filenameSize + extraSize, commentSize );

                result.Add( zfe );
                pointer += ( 46 + filenameSize + extraSize + commentSize );
            }

            return result;
        }
        /// <summary>
        /// Copy the contents of a stored file into a physical file
        /// </summary>
        /// <param name="zfe">Entry information of file to extract</param>
        /// <param name="filename">Name of file to store uncompressed data</param>
        /// <returns>True if success, false if not.</returns>
        /// <remarks>Unique compression methods are Store and Deflate</remarks>
        public bool ExtractFile( ZipFileEntry zfe, string filename ) {
            // Make sure the parent directory exist
            string path = Path.GetDirectoryName( filename );

            if( !Directory.Exists( path ) )
                Directory.CreateDirectory( path );
            // Check it is directory. If so, do nothing
            if( Directory.Exists( filename ) )
                return true;

            Stream output = new FileStream( filename, FileMode.Create, FileAccess.Write );
            bool result = ExtractFile( zfe, output );
            if( result )
                output.Close();

            File.SetCreationTime( filename, zfe.ModifyTime );
            File.SetLastWriteTime( filename, zfe.ModifyTime );

            return result;
        }
        /// <summary>
        /// Copy the contents of a stored file into an opened stream
        /// </summary>
        /// <param name="zfe">Entry information of file to extract</param>
        /// <param name="stream">Stream to store the uncompressed data</param>
        /// <returns>True if success, false if not.</returns>
        /// <remarks>Unique compression methods are Store and Deflate</remarks>
        public bool ExtractFile( ZipFileEntry zfe, Stream stream ) {
            if( !stream.CanWrite )
                throw new InvalidOperationException( "Stream cannot be written" );

            // check signature
            byte[] signature = new byte[4];
            zipFileStream.Seek( zfe.HeaderOffset, SeekOrigin.Begin );
            zipFileStream.Read( signature, 0, 4 );
            if( BitConverter.ToUInt32( signature, 0 ) != 0x04034b50 )
                return false;

            // Select input stream for inflating or just reading
            Stream inStream;
            if( zfe.Method == Compression.Store ) {
                inStream = zipFileStream;
            } else if( zfe.Method == Compression.Deflate ) {
                inStream = new DeflateStream( zipFileStream, CompressionMode.Decompress, true );
            } else {
                return false;
            }

            // Buffered copy
            byte[] buffer = new byte[16384];
            zipFileStream.Seek( zfe.FileOffset, SeekOrigin.Begin );
            uint bytesPending = zfe.FileSize;
            while( bytesPending > 0 ) {
                int bytesRead = inStream.Read( buffer, 0, (int)Math.Min( bytesPending, buffer.Length ) );
                stream.Write( buffer, 0, bytesRead );
                bytesPending -= (uint)bytesRead;
            }
            stream.Flush();

            if( zfe.Method == Compression.Deflate )
                inStream.Dispose();
            return true;
        }
        /// <summary>
        /// Removes one of many files in storage. It creates a new Zip file.
        /// </summary>
        /// <param name="zip">Reference to the current Zip object</param>
        /// <param name="zfes">List of Entries to remove from storage</param>
        /// <returns>True if success, false if not</returns>
        /// <remarks>This method only works for storage of type FileStream</remarks>
        public static bool RemoveEntries( ref ZipStorer zip, List<ZipFileEntry> zfes ) {
            if( !( zip.zipFileStream is FileStream ) )
                throw new InvalidOperationException( "RemoveEntries is allowed just over streams of type FileStream" );


            //Get full list of entries
            List<ZipFileEntry> fullList = zip.ReadCentralDir();

            //In order to delete we need to create a copy of the zip file excluding the selected items
            string tempZipName = Path.GetTempFileName();
            string tempEntryName = Path.GetTempFileName();

            try {
                ZipStorer tempZip = Create( tempZipName, string.Empty );

                foreach( ZipFileEntry zfe in fullList ) {
                    if( !zfes.Contains( zfe ) ) {
                        if( zip.ExtractFile( zfe, tempEntryName ) ) {
                            tempZip.AddFile( zfe.Method, tempEntryName, zfe.FilenameInZip, zfe.Comment );
                        }
                    }
                }
                zip.Close();
                tempZip.Close();

                if( File.Exists( zip.fileName ) ) {
                    File.Replace( tempZipName, zip.fileName, null, true );
                } else {
                    File.Move( tempZipName, zip.fileName );
                }

                zip = Open( zip.fileName, zip.access );
            } catch {
                return false;
            } finally {
                if( File.Exists( tempZipName ) )
                    File.Delete( tempZipName );
                if( File.Exists( tempEntryName ) )
                    File.Delete( tempEntryName );
            }
            return true;
        }
        #endregion

        #region Private methods
        // Calculate the file offset by reading the corresponding local header
        private uint GetFileOffset( uint headerOffset ) {
            byte[] buffer = new byte[2];

            zipFileStream.Seek( headerOffset + 26, SeekOrigin.Begin );
            zipFileStream.Read( buffer, 0, 2 );
            ushort filenameSize = BitConverter.ToUInt16( buffer, 0 );
            zipFileStream.Read( buffer, 0, 2 );
            ushort extraSize = BitConverter.ToUInt16( buffer, 0 );

            return (uint)( 30 + filenameSize + extraSize + headerOffset );
        }
        /* Local file header:
            local file header signature     4 bytes  (0x04034b50)
            version needed to extract       2 bytes
            general purpose bit flag        2 bytes
            compression method              2 bytes
            last mod file time              2 bytes
            last mod file date              2 bytes
            crc-32                          4 bytes
            compressed size                 4 bytes
            uncompressed size               4 bytes
            filename length                 2 bytes
            extra field length              2 bytes

            filename (variable size)
            extra field (variable size)
        */
        private void WriteLocalHeader( ref ZipFileEntry zfe ) {
            long pos = zipFileStream.Position;
            Encoding encoder = zfe.EncodeUTF8 ? Encoding.UTF8 : DefaultEncoding;
            byte[] encodedFilename = encoder.GetBytes( zfe.FilenameInZip );

            zipFileStream.Write( new byte[] { 80, 75, 3, 4, 20, 0 }, 0, 6 ); // No extra header
            zipFileStream.Write( BitConverter.GetBytes( (ushort)( zfe.EncodeUTF8 ? 0x0800 : 0 ) ), 0, 2 ); // filename and comment encoding 
            zipFileStream.Write( BitConverter.GetBytes( (ushort)zfe.Method ), 0, 2 );  // zipping method
            zipFileStream.Write( BitConverter.GetBytes( DateTimeToDosTime( zfe.ModifyTime ) ), 0, 4 ); // zipping date and time
            zipFileStream.Write( new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, 0, 12 ); // unused CRC, un/compressed size, updated later
            zipFileStream.Write( BitConverter.GetBytes( (ushort)encodedFilename.Length ), 0, 2 ); // filename length
            zipFileStream.Write( BitConverter.GetBytes( (ushort)0 ), 0, 2 ); // extra length

            zipFileStream.Write( encodedFilename, 0, encodedFilename.Length );
            zfe.HeaderSize = (uint)( zipFileStream.Position - pos );
        }
        /* Central directory's File header:
            central file header signature   4 bytes  (0x02014b50)
            version made by                 2 bytes
            version needed to extract       2 bytes
            general purpose bit flag        2 bytes
            compression method              2 bytes
            last mod file time              2 bytes
            last mod file date              2 bytes
            crc-32                          4 bytes
            compressed size                 4 bytes
            uncompressed size               4 bytes
            filename length                 2 bytes
            extra field length              2 bytes
            file comment length             2 bytes
            disk number start               2 bytes
            internal file attributes        2 bytes
            external file attributes        4 bytes
            relative offset of local header 4 bytes

            filename (variable size)
            extra field (variable size)
            file comment (variable size)
        */
        private void WriteCentralDirRecord( ZipFileEntry zfe ) {
            Encoding encoder = zfe.EncodeUTF8 ? Encoding.UTF8 : DefaultEncoding;
            byte[] encodedFilename = encoder.GetBytes( zfe.FilenameInZip );
            byte[] encodedComment = encoder.GetBytes( zfe.Comment );

            zipFileStream.Write( new byte[] { 80, 75, 1, 2, 23, 0xB, 20, 0 }, 0, 8 );
            zipFileStream.Write( BitConverter.GetBytes( (ushort)( zfe.EncodeUTF8 ? 0x0800 : 0 ) ), 0, 2 ); // filename and comment encoding 
            zipFileStream.Write( BitConverter.GetBytes( (ushort)zfe.Method ), 0, 2 );  // zipping method
            zipFileStream.Write( BitConverter.GetBytes( DateTimeToDosTime( zfe.ModifyTime ) ), 0, 4 );  // zipping date and time
            zipFileStream.Write( BitConverter.GetBytes( zfe.Crc32 ), 0, 4 ); // file CRC
            zipFileStream.Write( BitConverter.GetBytes( zfe.CompressedSize ), 0, 4 ); // compressed file size
            zipFileStream.Write( BitConverter.GetBytes( zfe.FileSize ), 0, 4 ); // uncompressed file size
            zipFileStream.Write( BitConverter.GetBytes( (ushort)encodedFilename.Length ), 0, 2 ); // Filename in zip
            zipFileStream.Write( BitConverter.GetBytes( (ushort)0 ), 0, 2 ); // extra length
            zipFileStream.Write( BitConverter.GetBytes( (ushort)encodedComment.Length ), 0, 2 );

            zipFileStream.Write( BitConverter.GetBytes( (ushort)0 ), 0, 2 ); // disk=0
            zipFileStream.Write( BitConverter.GetBytes( (ushort)0 ), 0, 2 ); // file type: binary
            zipFileStream.Write( BitConverter.GetBytes( (ushort)0 ), 0, 2 ); // Internal file attributes
            zipFileStream.Write( BitConverter.GetBytes( (ushort)0x8100 ), 0, 2 ); // External file attributes (normal/readable)
            zipFileStream.Write( BitConverter.GetBytes( zfe.HeaderOffset ), 0, 4 );  // Offset of header

            zipFileStream.Write( encodedFilename, 0, encodedFilename.Length );
            zipFileStream.Write( encodedComment, 0, encodedComment.Length );
        }
        /* End of central dir record:
            end of central dir signature    4 bytes  (0x06054b50)
            number of this disk             2 bytes
            number of the disk with the
            start of the central directory  2 bytes
            total number of entries in
            the central dir on this disk    2 bytes
            total number of entries in
            the central dir                 2 bytes
            size of the central directory   4 bytes
            offset of start of central
            directory with respect to
            the starting disk number        4 bytes
            zipfile comment length          2 bytes
            zipfile comment (variable size)
        */
        private void WriteEndRecord( uint size, uint offset ) {
            Encoding encoder = EncodeUTF8 ? Encoding.UTF8 : DefaultEncoding;
            byte[] encodedComment = encoder.GetBytes( comment );

            zipFileStream.Write( new byte[] { 80, 75, 5, 6, 0, 0, 0, 0 }, 0, 8 );
            zipFileStream.Write( BitConverter.GetBytes( (ushort)files.Count + existingFiles ), 0, 2 );
            zipFileStream.Write( BitConverter.GetBytes( (ushort)files.Count + existingFiles ), 0, 2 );
            zipFileStream.Write( BitConverter.GetBytes( size ), 0, 4 );
            zipFileStream.Write( BitConverter.GetBytes( offset ), 0, 4 );
            zipFileStream.Write( BitConverter.GetBytes( (ushort)encodedComment.Length ), 0, 2 );
            zipFileStream.Write( encodedComment, 0, encodedComment.Length );
        }
        // Copies all source file into storage file
        private void Store( ref ZipFileEntry zfe, Stream source ) {
            byte[] buffer = new byte[16384];
            int bytesRead;
            uint totalRead = 0;
            Stream outStream;

            long posStart = zipFileStream.Position;
            long sourceStart = source.Position;

            if( zfe.Method == Compression.Store )
                outStream = zipFileStream;
            else
                outStream = new DeflateStream( zipFileStream, CompressionMode.Compress, true );

            zfe.Crc32 = 0 ^ 0xffffffff;

            do {
                bytesRead = source.Read( buffer, 0, buffer.Length );
                totalRead += (uint)bytesRead;
                if( bytesRead > 0 ) {
                    outStream.Write( buffer, 0, bytesRead );

                    for( uint i = 0; i < bytesRead; i++ ) {
                        zfe.Crc32 = CrcTable[( zfe.Crc32 ^ buffer[i] ) & 0xFF] ^ ( zfe.Crc32 >> 8 );
                    }
                }
            } while( bytesRead == buffer.Length );

            if( totalRead > 0 )
                outStream.Flush(); // fix for "Internal error Flush" under Mono

            if( zfe.Method == Compression.Deflate )
                outStream.Dispose();

            zfe.Crc32 ^= 0xffffffff;
            zfe.FileSize = totalRead;
            zfe.CompressedSize = (uint)( zipFileStream.Position - posStart );

            // Verify for real compression
            if( zfe.Method == Compression.Deflate && !ForceDeflating && source.CanSeek && zfe.CompressedSize > zfe.FileSize ) {
                // Start operation again with Store algorithm
                zfe.Method = Compression.Store;
                zipFileStream.Position = posStart;
                zipFileStream.SetLength( posStart );
                source.Position = sourceStart;
                Store( ref zfe, source );
            }
        }
        /* DOS Date and time:
            MS-DOS date. The date is a packed value with the following format. Bits Description 
                0-4 Day of the month (1–31) 
                5-8 Month (1 = January, 2 = February, and so on) 
                9-15 Year offset from 1980 (add 1980 to get actual year) 
            MS-DOS time. The time is a packed value with the following format. Bits Description 
                0-4 Second divided by 2 
                5-10 Minute (0–59) 
                11-15 Hour (0–23 on a 24-hour clock) 
        */
        private static uint DateTimeToDosTime( DateTime dt ) {
            return (uint)(
                ( dt.Second / 2 ) | ( dt.Minute << 5 ) | ( dt.Hour << 11 ) |
                ( dt.Day << 16 ) | ( dt.Month << 21 ) | ( ( dt.Year - 1980 ) << 25 ) );
        }
        private static DateTime DosTimeToDateTime( uint dt ) {
            return new DateTime(
                (int)( dt >> 25 ) + 1980,
                (int)( dt >> 21 ) & 15,
                (int)( dt >> 16 ) & 31,
                (int)( dt >> 11 ) & 31,
                (int)( dt >> 5 ) & 63,
                (int)( dt & 31 ) * 2 );
        }

        /* CRC32 algorithm
          The 'magic number' for the CRC is 0xdebb20e3.  
          The proper CRC pre and post conditioning
          is used, meaning that the CRC register is
          pre-conditioned with all ones (a starting value
          of 0xffffffff) and the value is post-conditioned by
          taking the one's complement of the CRC residual.
          If bit 3 of the general purpose flag is set, this
          field is set to zero in the local header and the correct
          value is put in the data descriptor and in the central
          directory.
        */
        private void UpdateCrcAndSizes( ref ZipFileEntry zfe ) {
            long lastPos = zipFileStream.Position;  // remember position

            zipFileStream.Position = zfe.HeaderOffset + 8;
            zipFileStream.Write( BitConverter.GetBytes( (ushort)zfe.Method ), 0, 2 );  // zipping method

            zipFileStream.Position = zfe.HeaderOffset + 14;
            zipFileStream.Write( BitConverter.GetBytes( zfe.Crc32 ), 0, 4 );  // Update CRC
            zipFileStream.Write( BitConverter.GetBytes( zfe.CompressedSize ), 0, 4 );  // Compressed size
            zipFileStream.Write( BitConverter.GetBytes( zfe.FileSize ), 0, 4 );  // Uncompressed size

            zipFileStream.Position = lastPos;  // restore position
        }
        // Replaces backslashes with slashes to store in zip header
        private static string NormalizedFilename( string filename ) {
            string normalizedFilename = filename.Replace( '\\', '/' );

            int pos = normalizedFilename.IndexOf( ':' );
            if( pos >= 0 )
                normalizedFilename = normalizedFilename.Remove( 0, pos + 1 );

            return normalizedFilename.Trim( '/' );
        }
        // Reads the end-of-central-directory record
        private bool ReadFileInfo() {
            if( zipFileStream.Length < 22 )
                return false;

            try {
                zipFileStream.Seek( -17, SeekOrigin.End );
                BinaryReader br = new BinaryReader( zipFileStream );
                do {
                    zipFileStream.Seek( -5, SeekOrigin.Current );
                    UInt32 sig = br.ReadUInt32();
                    if( sig != 0x06054b50 ) continue;

                    zipFileStream.Seek( 6, SeekOrigin.Current );

                    UInt16 entries = br.ReadUInt16();
                    Int32 centralSize = br.ReadInt32();
                    UInt32 centralDirOffset = br.ReadUInt32();
                    UInt16 commentSize = br.ReadUInt16();

                    // check if comment field is the very last data in file
                    if( zipFileStream.Position + commentSize != zipFileStream.Length )
                        return false;

                    // Copy entire central directory to a memory buffer
                    existingFiles = entries;
                    centralDirImage = new byte[centralSize];
                    zipFileStream.Seek( centralDirOffset, SeekOrigin.Begin );
                    zipFileStream.Read( centralDirImage, 0, centralSize );

                    // Leave the pointer at the begining of central dir, to append new files
                    zipFileStream.Seek( centralDirOffset, SeekOrigin.Begin );
                    return true;
                } while( zipFileStream.Position > 0 );
            } catch { }

            return false;
        }
        #endregion

        #region IDisposable Members
        /// <summary>
        /// Closes the Zip file stream
        /// </summary>
        public void Dispose() {
            Close();
        }
        #endregion
    }
}