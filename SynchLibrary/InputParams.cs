using System;
using System.Text.RegularExpressions;

namespace SynchLibrary
{
    public class InputParams
    {
		/// <summary>
		/// The source directory for the synch
		/// </summary>
		public string SourceDirectory { get; set; }

		/// <summary>
		/// The destination direectory for the synch
		/// </summary>
		public string DestinationDirectory { get; set; }

		/// <summary>
		/// If true an analysis of the differences will only be performed, no synchronistion
		/// </summary>
		public bool AnalyseOnly { get; set; } = false;

		/// <summary>
		/// Should exclude hidden files/directories in source
		/// </summary>
		public bool ExcludeHidden { get; set; } = false;

		/// <summary>
		/// Should identical files be excluded from the report
		/// </summary>
		public bool ExcludeIdenticalFiles { get; set; } = false;

		/// <summary>
		/// Should delete files from dest than are not present in source
		/// </summary>
		public bool DeleteFilesFromDest { get; set; } = false;

		/// <summary>
		/// Should delete directories from dest than are not present in source
		/// </summary>
		public bool DeleteDirsFromDest { get; set; } = false;

		/// <summary>
		/// List of filespecs to exclude
		/// </summary>
		public DirectoryFilter[] ExcludeFiles { get; set; } = null;

		/// <summary>
		/// List of directory specs to exclude
		/// </summary>
		public DirectoryFilter[] ExcludeDirs { get; set; } = null;

		/// <summary>
		/// List of filespecs to include 
		/// </summary>
		public DirectoryFilter[] IncludeFiles { get; set; } = null;

		/// <summary>
		/// List of directory specs to include
		/// </summary>
		public DirectoryFilter[] IncludeDirs { get; set; } = null;

		/// <summary>
		/// List of filespecs NOT to delete from dest
		/// </summary>
		public DirectoryFilter[] DeleteExcludeFiles { get; set; } = null;

		/// <summary>
		/// List of directory specs NOT to delete from dest
		/// </summary>
		public DirectoryFilter[] DeleteExcludeDirs { get; set; } = null;

		/// <summary>
		/// If AnalyseFirst is true then this specifies the maximum number of directories( source or destination) that 
		/// will be synchronised.  If this limit is exceeded then no synchronisation will take place
		/// </summary>
		public uint DirectorySynchLimit = 0;

		/// <summary>
		/// If AnalyseFirst is true then this specifies the maximum number of files( source or destination) that 
		/// will be synchronised.  If this limit is exceeded then no synchronisation will take place
		/// </summary>
		public uint FileSynchLimit = 0;

		/// <summary>
		/// Combined property for any filtering
		/// </summary>
		public bool AreSourceFilesFiltered => ( ExcludeHidden == true ) || ( IncludeFiles != null ) || ( ExcludeFiles != null ) ||
					( IncludeDirs != null ) || ( ExcludeDirs != null );

		/// <summary>
		/// Any filtering to be applied to source directories
		/// </summary>
		public bool AreSourceDirectoriesFiltered => ( IncludeDirs != null ) || ( ExcludeDirs != null );
	}

	/// <summary>
	/// Speciifcation for a directory filter used to either include or exclude directories
	/// </summary>
	public class DirectoryFilter
	{
		/// <summary>
		/// Regular Expression to be matched
		/// </summary>
		public Regex Name { get; set; }

		/// <summary>
		/// Should the full path of the directory be used
		/// </summary>
		public bool FullPath { get; set; } = false;

		/// <summary>
		/// Should this filter only be applied to the top level directories only
		/// </summary>
		public bool TopLevelOnly { get; set; } = true;
	}

}
