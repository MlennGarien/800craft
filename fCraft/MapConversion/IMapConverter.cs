// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;

namespace fCraft.MapConversion {
    public interface IMapConverter {
        /// <summary> Returns name(s) of the server(s) that uses this format. </summary>
        [NotNull]
        string ServerName { get; }
        

        /// <summary> Returns the map storage type (file-based or directory-based). </summary>
        MapStorageType StorageType { get; }


        /// <summary> Returns the format name. </summary>
        MapFormat Format { get; }


        /// <summary> Returns true if the filename (or directory name) matches this format's expectations. </summary>
        bool ClaimsName( [NotNull] string path );


        /// <summary> Allows validating the map format while using minimal resources. </summary>
        /// <returns> Returns true if specified file/directory is valid for this format. </returns>
        bool Claims( [NotNull] string path );


        /// <summary> Attempts to load map dimensions from specified location. </summary>
        /// <returns> Map object on success, or null on failure. </returns>
        Map LoadHeader( [NotNull] string path );


        /// <summary> Fully loads map from specified location. </summary>
        /// <returns> Map object on success, or null on failure. </returns>
        Map Load( [NotNull] string path );


        /// <summary> Saves given map at the given location. </summary>
        /// <returns> true if saving succeeded. </returns>
        bool Save( [NotNull] Map mapToSave, [NotNull] string path );
    }

	public interface IMapConverterEx : IMapConverter
	{
		//must return this to enable addin multiple extensions in one line: converter.AddExtension(e1).AddExtension(e2)
		IMapConverterEx AddExtension(IConverterExtension ex);
		void WriteMetadataEntry(string group, string key, string value, BinaryWriter writer);
	}

	public interface IConverterExtension
	{
		/// <summary>
		/// Returns groups procesed by this extension
		/// </summary>
		/// <returns></returns>
		IEnumerable<string> AcceptedGroups { get; }
		/// <summary>
		/// Serializes the extended data as metadata and returns the number of written keys
		/// </summary>
		/// <returns>
		/// The number of written keys
		/// </returns>
		int Serialize(Map map, Stream stream, IMapConverterEx converter);
		/// <summary>
		/// Instantiates the extended data for the given map from the group, key, and value
		/// </summary>
		void Deserialize(string group, string key, string value, Map map);
	}
}
