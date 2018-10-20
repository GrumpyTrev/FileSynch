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
		/// Should delete files/directories from dest than are not present in source
		/// </summary>
		public bool DeleteFromDest { get; set; } = false;

		/// <summary>
		/// List of filespecs to exclude
		/// </summary>
		public Regex[] ExcludeFiles { get; set; } = null;

		/// <summary>
		/// List of directory specs to exclude
		/// </summary>
		public Regex[] ExcludeDirs { get; set; } = null;

		/// <summary>
		/// List of filespecs to include 
		/// </summary>
		public Regex[] IncludeFiles { get; set; } = null;

		/// <summary>
		/// List of directory specs to include
		/// </summary>
		public Regex[] IncludeDirs { get; set; } = null;

		/// <summary>
		/// List of filespecs NOT to delete from dest
		/// </summary>
		public Regex[] DeleteExcludeFiles { get; set; } = null;

		/// <summary>
		/// List of directory specs NOT to delete from dest
		/// </summary>
		public Regex[] DeleteExcludeDirs { get; set; } = null;

        public bool AreSourceFilesFiltered
        {
            get
            {
                return ( ExcludeHidden == true ) || (IncludeFiles != null) || (ExcludeFiles != null) ||
                    (IncludeDirs != null) || (ExcludeDirs != null);
            }
        }
    }
}
