using SynchLibrary;

namespace SynchApp
{
	/// <summary>
	/// Parameters specific to controlling the running of the SynchLibrary, not for the SynchLibrary itself
	/// </summary>
	class ProgramParams : InputParams
	{
		/// <summary>
		/// If true perform an analysis of the differences and then only synchronise if the number of
		/// directories and files to sychronise is below the specified limits
		/// </summary>
		public bool AnalyseFirst { get; set; } = false;

		/// <summary>
		/// Should all the files and directory specs be treated as regex (true) or simple strings (false)
		/// </summary>
		public bool UseRegex { get; set; } = false;

		/// <summary>
		/// The descriptive name of this backup job
		/// </summary>
		public string Name { get; set; } = "";
	}
}
