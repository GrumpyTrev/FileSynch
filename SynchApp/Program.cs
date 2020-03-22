using SynchLibrary;
using NDesk.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace SynchApp
{
	class Program
	{
		/// <summary>
		/// Main entry point for the SynchApp program
		/// Parse the command line arguements, perform some basic validation and start the synchronisation process
		/// </summary>
		/// <param name="args"></param>
		static int Main( string[] args )
		{
			ExitCode success = ExitCode.Success;
			int errorCode = ( int )success;
			bool commandLineJob = true;

			try
			{
				// Collection of InputParams one per backup job
				List<ProgramParams> backupJobs = new List<ProgramParams>();

				// Assume that the options for a single backup is specified on the command line and only a single InputParams object
				// is required
				ProgramParams commandParameters = new ProgramParams();
				OptionSet options = ConfigureOptions( commandParameters );

				// Parse the options and the left over directories
				List<string> directories = options.Parse( args );

				// If two unmatched strings have been specified then assume that the command line includes the source and destination
				// and that the InputParams will be set by the OptionSet
				if ( directories.Count == 2 )
				{
					commandParameters.SourceDirectory = new DirectoryInfo( directories[ 0 ] ).FullName;
					commandParameters.DestinationDirectory = new DirectoryInfo( directories[ 1 ] ).FullName;

					backupJobs.Add( commandParameters );
				}
				else if ( directories.Count == 1 )
				{
					// Assume that if a single file has been specified then it is an xml configuration file
					commandLineJob = false;

					if ( GetOptionsFromXmlFile( backupJobs, directories[ 0 ] ) == false )
					{
						DisplayHelp( options );
						success = ExitCode.XmlError;
					}
				}
				else
				{
					DisplayHelp( options );
					success = ExitCode.DirectoryError;
				}

				if ( success == ExitCode.Success )
				{
					// If this is a command line job then perform it and return its success code
					if ( commandLineJob == true )
					{
						success = PerformBackup( backupJobs[ 0 ], options );

						// Include destination file and directory deletions in the error code if successfull
						if ( success == ExitCode.Success )
						{
							errorCode += ( int )( ( ( UnmatchedDestinationFile > 255 ? 255 : UnmatchedDestinationFile ) & 255 ) << 8 );
							errorCode += ( int )( ( ( UnmatchedDestinationDirectory > 255 ? 255 : UnmatchedDestinationDirectory ) & 255 ) << 16 );
						}
					}
					else
					{
						// Set up alternative console logging
						Console.SetOut( new Logging( 30 ) );

						// Process each backup job until
						foreach ( ProgramParams job in backupJobs )
						{
							PerformBackup( job, options );
						}
					}
				}
			}
			catch ( OptionException oException )
			{
				Console.Write( "SynchApp: " );
				Console.WriteLine( oException.Message );
				Console.WriteLine( "Try 'SynchApp --help' for more information" );
				success = ExitCode.DirectoryError;
			}
			catch ( NotSupportedException nsException )
			{
				Console.Write( "SynchApp: " );
				Console.WriteLine( nsException.Message );
				Console.WriteLine( "Try 'SynchApp --help' for more information" );
				success = ExitCode.DirectoryError;
			}

			// Include the success code in the erorr code
			errorCode += ( int )success;

			return errorCode;
		}

		/// <summary>
		/// Carry out a backup job as defined by options in the ProgramParams
		/// </summary>
		/// <param name="parameters"></param>
		/// <param name="options"></param>
		/// <returns></returns>
		private static ExitCode PerformBackup( ProgramParams parameters, OptionSet options )
		{
			ExitCode success = ExitCode.Success;

			ResetLocalCounts();

			// Check that at least the source directory exists and that the directories don't overlap in some way
			if ( Directory.Exists( parameters.SourceDirectory ) == true )
			{
				string fullSrcDir = Path.GetFullPath( parameters.SourceDirectory );
				string fullDestDir = Path.GetFullPath( parameters.DestinationDirectory );
				if ( ( parameters.DestinationDirectory.StartsWith( fullSrcDir ) == false ) &&
					( parameters.SourceDirectory.StartsWith( fullDestDir ) == false ) )
				{
					if ( ValidateOptions( options, parameters ) == true )
					{
						Console.WriteLine( "==================================================" );
						Console.WriteLine( "Job started : {0}", DateTime.Now.TimeOfDay );

						if ( parameters.Name.Length > 0 )
						{
							Console.WriteLine( parameters.Name );
						}

						Console.WriteLine( "Synchronising source '{0}' and destination '{1}'", fullSrcDir, fullDestDir );
						Console.WriteLine( "==================================================" );

						Sync synchroniseFiles = new Sync( parameters ) { Log = LogResult };

						if ( parameters.AnalyseFirst == true )
						{
							// Analyse only
							Console.WriteLine( "Analysing..." );
							parameters.AnalyseOnly = true;
							synchroniseFiles.Start();

							if ( ( ( parameters.DirectorySynchLimit > 0 ) && ( DirectoriesMissing > parameters.DirectorySynchLimit ) ) ||
								 ( ( parameters.FileSynchLimit > 0 ) && ( FilesMissing > parameters.FileSynchLimit ) ) )
							{
								Console.WriteLine( "+++++Synchronisation limits exceeded, no synchronisation performed {0} directories {1} files",
									DirectoriesMissing, FilesMissing );
								success = ExitCode.LimitsReached;
							}
							else
							{
								// Check if any synchronisation is required
								if ( ( DirectoriesMissing > 0 ) || ( FilesMissing > 0 ) || ( FilesChanged > 0 ) )
								{
									// Reset parameters for second pass below
									parameters.AnalyseOnly = false;
									parameters.AnalyseFirst = false;
									parameters.FileSynchLimit = 0;
									parameters.DirectorySynchLimit = 0;

									Console.WriteLine( "Synchronising..." );
								}
								else
								{
									Console.WriteLine( "No synchronisation required" );
								}
							}
						}

						if ( parameters.AnalyseFirst == false )
						{
							synchroniseFiles.Start();

							if ( ( DirectoriesMissing == 0 ) && ( FilesMissing == 0 ) && ( FilesChanged == 0 ) )
							{
								Console.WriteLine( "No synchronisation required" );
							}
						}

						Console.WriteLine( "==================================================" );
						Console.WriteLine( "Job finished : {0}", DateTime.Now.TimeOfDay );
						Console.WriteLine( "==================================================" );
						Console.WriteLine( "" );
						Console.WriteLine( "" );
					}
					else
					{
						success = ExitCode.OptionError;
					}
				}
				else
				{
					Console.WriteLine( "Error: source directory {0} and destination directory {1} cannot contain each other", fullSrcDir, fullDestDir );
					DisplayHelp( options );
					success = ExitCode.DirectoryError;
				}
			}
			else
			{
				Console.WriteLine( "Error: source directory {0} does not exist", parameters.SourceDirectory );
				DisplayHelp( options );
				success = ExitCode.DirectoryError;
			}

			return success;
		}

		/// <summary>
		/// Deserialise an xml configuration file into a set of backup jobs
		/// </summary>
		/// <param name="backups"></param>
		/// <param name="configFile"></param>
		/// <returns></returns>
		private static bool GetOptionsFromXmlFile( List<ProgramParams> backups, string configFile )
		{
			bool success = true;

			try
			{
				// Deserialise the file contents into a Configuration object
				Configuration xmlConfig = ( Configuration )new XmlSerializer( typeof( Configuration ) ).Deserialize( new FileStream( configFile, FileMode.Open ) );

				// Now copy fields from the Configuration object to the parameters, one InputParams per backup job
				foreach ( Backup backup in xmlConfig.Backups )
				{
					ProgramParams parameters = new ProgramParams();

					// Common options
					parameters.AnalyseOnly = xmlConfig.Options.analyseOnly;
					parameters.ExcludeHidden = xmlConfig.Options.excludeHidden;
					parameters.ExcludeIdenticalFiles = xmlConfig.Options.excludeIdentical;
					parameters.DeleteDirsFromDest = xmlConfig.Options.deleteDirectories;
					parameters.DeleteFilesFromDest = xmlConfig.Options.deleteFiles;
					parameters.UseRegex = xmlConfig.Options.useRegex;

					// Specific to this backup
					parameters.Name = backup.name;
					parameters.SourceDirectory = new DirectoryInfo( backup.source ).FullName;
					parameters.DestinationDirectory = new DirectoryInfo( backup.destination ).FullName;

					if ( backup.directoryExcludes.directoryFilter != null )
					{
						parameters.ExcludeDirs = RegexListFromStringList( backup.directoryExcludes.directoryFilter, parameters.UseRegex );
					}

					if ( backup.directoryIncludes.directoryFilter != null )
					{
						parameters.IncludeDirs = RegexListFromStringList( backup.directoryIncludes.directoryFilter, parameters.UseRegex );
					}

					backups.Add( parameters );
				}
			}
			catch ( FileNotFoundException nfException )
			{
				Console.Write( "SynchApp: " );
				Console.WriteLine( nfException.Message );
				success = false;
			}
			catch ( InvalidOperationException ioException )
			{
				Console.Write( "SynchApp: " );
				Console.WriteLine( ioException.Message );
				if ( ioException.InnerException != null )
				{
					Console.WriteLine( ioException.InnerException.Message );
				}

				success = false;
			}

			return success;
		}

		/// <summary>
		/// Validate option combinations
		/// </summary>
		/// <param name="options"></param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		private static bool ValidateOptions( OptionSet options, ProgramParams parameters )
		{
			bool success = false;

			// Check that both includes and excludes have not been defined
			if ( ( ( parameters.IncludeFiles == null ) || ( parameters.ExcludeFiles == null ) ) &&
				 ( ( parameters.IncludeDirs == null ) || ( parameters.ExcludeDirs == null ) ) )
			{
				// Deletion exclusions should only be defined if destination deletion has been specified
				if ( ( ( parameters.DeleteFilesFromDest == true ) || ( parameters.DeleteExcludeFiles == null ) ) && 
					 ( ( parameters.DeleteDirsFromDest == true ) || ( parameters.DeleteExcludeDirs == null ) ) )
				{
					// If the Analyse First option has been set then one of the limits should also be specified
					if ( ( parameters.AnalyseFirst == false ) ||
						 ( ( parameters.AnalyseOnly == false ) && ( ( parameters.DirectorySynchLimit + parameters.FileSynchLimit ) > 0 ) ) )
					{
						success = true;
					}
					else
					{
						if ( parameters.AnalyseOnly == true )
						{
							Console.WriteLine( "Error: analyse first and analyse only options cannot both be set." );
							DisplayHelp( options );
						}
						else
						{
							Console.WriteLine( "Error: analyse first option requries a limit ( -ld or -lf )." );
							DisplayHelp( options );
						}
					}
				}
				else
				{
					Console.WriteLine( "Error: exclude-from-deletion options (-ndf and -ndd) require deletion (-df or -dd) enabled." );
					DisplayHelp( options );
				}
			}
			else
			{
				Console.WriteLine( "Error: cannot include and exclude items at the same time." );
				DisplayHelp( options );
			}

			return success;
		}

		/// <summary>
		/// Configure the OptionsSet object used to parse command line parameters
		/// </summary>
		/// <param name="parameters"></param>
		/// <returns></returns>
		private static OptionSet ConfigureOptions( ProgramParams parameters )
		{
			return new OptionSet()
						   .Add( "a|analyse", "analysis only, no synchronisation", a => parameters.AnalyseOnly = true )
						   .Add( "af|analyse first", "analysis only first and only synch if within limits defined in -ld and -lf options",
							   af => parameters.AnalyseFirst = true )
						   .Add( "xh|exclude hidden", "exclude hidden files and directories", xh => parameters.ExcludeHidden = true )
						   .Add( "xi|exclude identical", "exclude identical files from the report", xi => parameters.ExcludeIdenticalFiles = true )
						   .Add( "df|delete files", "delete files in destination which do not appear in source", df => parameters.DeleteFilesFromDest = true )
						   .Add( "dd|delete dirs", "delete directories in destination which do not appear in source", dd => parameters.DeleteDirsFromDest = true )
						   .Add( "xf|exclude files=", "exclude files from source that match any of the filespecs",
							   xf => parameters.ExcludeFiles = FormRegexListfromString( xf, parameters.UseRegex ) )
						   .Add( "xd|exclude directories=", "exclude directories from source that match any of the filespecs",
							   xd => parameters.ExcludeDirs = FormRegexListfromString( xd, parameters.UseRegex ) )
						   .Add( "if|include files=", "only include files from source that match one of the filespecs",
							   inf => parameters.IncludeFiles = FormRegexListfromString( inf, parameters.UseRegex ) )
						   .Add( "id|include directories=", "include directories from source that match one of the filespecs",
							   ind => parameters.IncludeDirs = FormRegexListfromString( ind, parameters.UseRegex ) )
						   .Add( "ndf|exclude delete files=", "exclude files from deletion that match any of the filespecs",
							   ndf => parameters.DeleteExcludeFiles = FormRegexListfromString( ndf, parameters.UseRegex ) )
						   .Add( "ndd|exclude delete directories=", "Exclude directories from deletion that match any of the filespecs",
							   ndd => parameters.DeleteExcludeDirs = FormRegexListfromString( ndd, parameters.UseRegex ) )
						   .Add( "ld|limit directory synch=", "limit on the number of directories that will be synchronised if the --af option is set",
							   ( uint ld ) => parameters.DirectorySynchLimit = ld )
						   .Add( "lf|limit file synch=", "limit on the number of files that will be synchronised if the --af option is set",
							   ( uint lf ) => parameters.FileSynchLimit = lf );
		}

		/// <summary>
		/// Display the options specified in the OptionSet and any multiple option requirements
		/// </summary>
		/// <param name="options"></param>
		private static void DisplayHelp( OptionSet options )
		{
			Console.WriteLine( "Usage 'SynchApp source destination [OPTIONS]+'" );
			Console.WriteLine( "Usage 'SynchApp config'" );
			Console.WriteLine( "Synchronise the destination directory with the source directory" );
			Console.WriteLine( "Directories and options can be either specified on the command line or in an XML configuration file" );
			Console.WriteLine( "Options: " );

			Console.WriteLine( "" );
			options.WriteOptionDescriptions( Console.Out );
			Console.WriteLine( "" );
			Console.WriteLine( "Include/exclude files options (-if and -xf) may not be combined." );
			Console.WriteLine( "Include/exclude directories options (-id and -xd) may not be combined." );
			Console.WriteLine( "Exclude-from-deletion options (-ndf and -ndd) require deletion (-d) enabled." );
			Console.WriteLine( "" );
			Console.WriteLine( "If the Analyse First (-af) option is set then at least on of the limits ( -ld or -lf ) must be specified" );
		}

		/// <summary>
		/// Log the results to the console and keep track of the number of directories and files that require synchronisation
		/// </summary>
		/// <param name="result"></param>
		private static void LogResult( SyncResult result )
		{
			if ( result.Item == SyncResult.ItemType.Trace )
			{
				Console.WriteLine( result.Message );
			}
			else if ( result.Item == SyncResult.ItemType.File )
			{
				if ( result.Reason == SyncResult.ReasonType.OnlyIn )
				{
					Console.WriteLine( string.Format( "'{0}' only in '{1}'", result.Message, result.Container ) );
					FilesMissing++;

					if ( result.Context == SyncResult.ContainerType.Destination )
					{
						UnmatchedDestinationFile++;
					}
				}
				else
				{
					switch ( result.Reason )
					{
						case SyncResult.ReasonType.Identical:
						{
							Console.WriteLine( string.Format( "'{0}' identical", result.Message ) );
							break;
						}

						case SyncResult.ReasonType.ModifiedTime:
						{
							Console.WriteLine( string.Format( "'{0}' different modified times", result.Message ) );
							FilesChanged++;
							break;
						}

						case SyncResult.ReasonType.Length:
						{
							Console.WriteLine( string.Format( "'{0}' different lengths", result.Message ) );
							FilesChanged++;
							break;
						}
					}
				}
			}
			else
			{
				if ( result.Reason == SyncResult.ReasonType.OnlyIn )
				{
					Console.WriteLine( string.Format( "'{0}' only in '{1}'", result.Message, result.Container ) );
					DirectoriesMissing++;

					if ( result.Context == SyncResult.ContainerType.Destination )
					{
						UnmatchedDestinationDirectory++;
					}
				}
			}
		}

		/// <summary>
		/// Form an array of Regex expressions from a comma delimited string
		/// </summary>
		/// <param name="commaDelimitedString"></param>
		/// <param name="useRegex"></param>
		/// <returns></returns>
		private static DirectoryFilter[] FormRegexListfromString( string commaDelimitedString, bool useRegex )
		{
			List<DirectoryFilter> directoryFilters = new List<DirectoryFilter>();

			// Form a Regex object for each string
			foreach ( string regexString in commaDelimitedString.Split( ',', ' ' ) )
			{
				if ( regexString.Length > 0 )
				{
					DirectoryFilter newFilter = new DirectoryFilter();

					// If simple strings are being used (useRegex = false) then specify that the entire string should be matched
					if ( useRegex == true )
					{
						newFilter.Name = new Regex( regexString );
					}
					else
					{
						newFilter.Name = new Regex( '^' + regexString + '$' );
					}

					newFilter.FullPath = false;
					newFilter.TopLevelOnly = true;
					directoryFilters.Add( newFilter );
				}
			}

			return directoryFilters.ToArray();
		}

		/// <summary>
		/// Form an array of Regex expressions from an array of strings
		/// </summary>
		/// <param name="strings"></param>
		/// <param name="useRegex"></param>
		/// <returns></returns>
		private static DirectoryFilter[] RegexListFromStringList( directoryFilter[] filters, bool useRegex )
		{
			List<DirectoryFilter> directoryFilters = new List<DirectoryFilter>();

			foreach ( directoryFilter filter in filters )
			{
				DirectoryFilter newFilter = new DirectoryFilter();

				// If simple strings are being used (useRegex = false) then specify that the entire string should be matched
				if ( useRegex == true )
				{
					newFilter.Name = new Regex( filter.Value );
				}
				else
				{
					newFilter.Name = new Regex( '^' + filter.Value + '$' );
				}

				newFilter.FullPath = filter.fullPath;
				newFilter.TopLevelOnly = filter.topLevelOnly;
				directoryFilters.Add( newFilter );
			}

			return directoryFilters.ToArray();
		}

		/// <summary>
		/// Reset the event counters
		/// </summary>
		private static void ResetLocalCounts()
		{
			DirectoriesMissing = 0;
			FilesMissing = 0;
			FilesChanged = 0;
			UnmatchedDestinationFile = 0;
			UnmatchedDestinationDirectory = 0;
		}

		/// <summary>
		/// Keep track of the number of directories or files that have been or require synchronising
		/// </summary>
		private static uint DirectoriesMissing = 0;
		private static uint FilesMissing = 0;
		private static uint FilesChanged = 0;
		private static uint UnmatchedDestinationFile = 0;
		private static uint UnmatchedDestinationDirectory = 0;

		/// <summary>
		/// Enumeration of the exit code for the program
		/// </summary>
		private enum ExitCode: int
		{
			Success = 0,
			OptionError = 1,
			DirectoryError = 2,
			LimitsReached = 3,
			XmlError = 4
		}
	}
}
