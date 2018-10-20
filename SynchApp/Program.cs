using SynchLibrary;
using NDesk.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace SynchApp
{
	class Program
	{
		/// <summary>
		/// Main entry point for the SynchApp program
		/// Parse the command line arguements, perform some basic validation and start the synchronisation process
		/// </summary>
		/// <param name="args"></param>
		static void Main( string[] args )
		{
			try
			{
				// Parameter set to pass to the Sync library
				InputParams parameters = new InputParams();

				OptionSet options = new OptionSet()
					.Add( "a|analyse", "analysis only, no synchronisation", a => parameters.AnalyseOnly = true )
					.Add( "xh|exclude hidden", "exclude hidden files and directories", xh => parameters.ExcludeHidden = true )
					.Add( "xi|exclude identical", "exclude identical files from the report", xi => parameters.ExcludeIdenticalFiles = true )
					.Add( "d|delete", "delete files and directories in destination which do not appear in source", d => parameters.DeleteFromDest = true )
					.Add( "xf|exclude files=", "exclude files from source that match any of the filespecs", 
						xf => parameters.ExcludeFiles = FormRegexListfromString( xf ) )
					.Add( "xd|exclude directories=", "exclude directories from source that match any of the filespecs", 
						xd => parameters.ExcludeDirs = FormRegexListfromString( xd ) )
					.Add( "if|include files=", "only include files from source that match one of the filespecs", 
						inf => parameters.IncludeFiles = FormRegexListfromString( inf ) )
					.Add( "id|include directories=", "include directories from source that match one of the filespecs", 
						ind => parameters.IncludeDirs = FormRegexListfromString( ind ) )
					.Add( "ndf|exclude delete files=", "exclude files from deletion that match any of the filespecs", 
						ndf => parameters.DeleteExcludeFiles = FormRegexListfromString( ndf ) )
					.Add( "ndd|exclude delete directories=", "Exclude directories from deletion that match any of the filespecs", 
						ndd => parameters.DeleteExcludeDirs = FormRegexListfromString( ndd ) );

				List<string> directories = options.Parse( args );

				if ( directories.Count == 2 )
				{
					parameters.SourceDirectory = new DirectoryInfo( directories[ 0 ] ).FullName;
					parameters.DestinationDirectory = new DirectoryInfo( directories[ 1 ] ).FullName;

					// Check that at least the source directory exists and that the directories don't overlap in some way
					if ( Directory.Exists( parameters.SourceDirectory ) == true )
					{
						string fullSrcDir = Path.GetFullPath( parameters.SourceDirectory );
						string fullDestDir = Path.GetFullPath( parameters.DestinationDirectory );
						if ( ( parameters.DestinationDirectory.StartsWith( fullSrcDir ) == false ) &&
							( parameters.SourceDirectory.StartsWith( fullDestDir ) == false ) )
						{
							// Check that both includes and excludes have not been defined
							if ( ( ( parameters.IncludeFiles == null ) || ( parameters.ExcludeFiles == null ) ) &&
								 ( ( parameters.IncludeDirs == null ) || ( parameters.ExcludeDirs == null ) ) )
							{
								// Deletion excludions should only be defined if destination deletion has been specified
								if ( ( parameters.DeleteFromDest == true ) ||
									( ( parameters.DeleteExcludeFiles == null ) && ( parameters.DeleteExcludeDirs == null ) ) )
								{
									Console.WriteLine( "Synchronising source '{0}' and destination '{1}'", fullSrcDir, fullDestDir );

									Sync synchroniseFiles = new Sync( parameters );
									synchroniseFiles.Log = LogResult;
									synchroniseFiles.Start();
								}
								else
								{
									Console.WriteLine( "Error: exclude-from-deletion options (-ndf and -ndd) require deletion (-d) enabled." );
									DisplayHelp( options );
								}
							}
							else
							{
								Console.WriteLine( "Error: cannot include and exclude items at the same time." );
								DisplayHelp( options );
							}
						}
						else
						{
							Console.WriteLine( "Error: source directory {0} and destination directory {1} cannot contain each other", fullSrcDir, fullDestDir );
							DisplayHelp( options );
						}
					}
					else
					{
						Console.WriteLine( "Error: source directory {0} does not exist", parameters.SourceDirectory );
						DisplayHelp( options );
					}
				}
				else
				{
					DisplayHelp( options );
				}
			}
			catch ( OptionException oException )
			{
				Console.Write( "SynchApp: " );
				Console.WriteLine( oException.Message );
				Console.WriteLine( "Try 'SynchApp --help' for more information" );
			}
			catch ( NotSupportedException nsException )
			{
				Console.Write( "SynchApp: " );
				Console.WriteLine( nsException.Message );
				Console.WriteLine( "Try 'SynchApp --help' for more information" );
			}
		}

		static void DisplayHelp( OptionSet options )
		{
			Console.WriteLine( "Usage 'SynchApp source destination [OPTIONS]+'" );
			Console.WriteLine( "Synchronise the destination directory with the source directory" );
			Console.WriteLine( "Options: " );

			Console.WriteLine( "" );
			Console.WriteLine( "Include/exclude files options (-if and -xf) may not be combined." );
			Console.WriteLine( "Include/exclude directories options (-id and -xd) may not be combined." );
			Console.WriteLine( "Exclude-from-deletion options (-ndf and -ndd) require deletion (-d) enabled." );

			options.WriteOptionDescriptions( Console.Out );
		}

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
							break;
						}

						case SyncResult.ReasonType.Length:
						{
							Console.WriteLine( string.Format( "'{0}' different lengths", result.Message ) );
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
				}
			}
		}

		/// <summary>
		/// Form an array of Regex expressions from a comma delimited string
		/// </summary>
		/// <param name="commaDelimitedString"></param>
		/// <returns></returns>
		private static Regex[] FormRegexListfromString( string commaDelimitedString )
		{
			List<Regex> result = new List<Regex>();

			// Form a Regex object for each string
			foreach ( string regexString in commaDelimitedString.Split( ',', ' ' ) )
			{
				if ( regexString.Length > 0 )
				{
					result.Add( new Regex( regexString ) );
				}
			}

			return result.ToArray();
		}

	}
}
