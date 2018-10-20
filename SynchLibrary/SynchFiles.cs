using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace SynchLibrary
{
	/// <summary>
	/// Folders and files synchronization
	/// </summary>
	public class Sync
    {
        /// <summary>
        /// Initializes a new instance of Synchonizer object.
        /// </summary>
        public Sync( InputParams options )
        {
			Configuration = options;
		}

        /// <summary>
        /// Set this property to log the synchronization progress by this class to the given delegate. 
        /// For example, to log to the console, set this property to Console.Write.
        /// </summary>
        public virtual Action< SyncResult > Log { get; set; }

        /// <summary>
        /// Get or set all synronization parameters
        /// </summary>
        public InputParams Configuration { get; }

        /// <summary>
        /// Performs one-way synchronization from source directory tree to destination directory tree
        /// </summary>
        public SyncResults Start()
        {
            results = new SyncResults();

            // Recursively process directories
            ProcessDirectory( Configuration.SourceDirectory, Configuration.DestinationDirectory );

            return results;
        }

		/// <summary>
		/// Recursively performs one-way synchronization from a single source to destination directory
		/// </summary>
		/// <param name="sourceDirectory"></param>
		/// <param name="destinationDirectory"></param>
		private bool ProcessDirectory( string sourceDirectory, string destinationDirectory )
        {
			bool success = false;

            DirectoryInfo destinationInfo = new DirectoryInfo( destinationDirectory );

			// Create destination directory if it doesn't exist
			if ( CreateDestinationDirectoryIfItDoesntExist( destinationInfo ) == true )
			{
				DirectoryInfo sourceInfo = new DirectoryInfo( sourceDirectory );

				// If analysing the destination directory may not have been created, so check again here
				destinationInfo = new DirectoryInfo( destinationDirectory );
				if ( destinationInfo.Exists == true )
				{
					if ( SynchroniseFiles( sourceInfo, destinationInfo ) == true )
					{
						success = SynchroniseSubDirectories( sourceInfo, destinationInfo );
					}
				}
				else
				{
					success = true;
				}
			}

			return success;
        }

		/// <summary>
		/// Create the specified destination directory if it doesn't already exist
		/// </summary>
		/// <param name="directory"></param>
		/// <returns>True if directory exists or was successfully created, false otherwise</returns>
		private bool CreateDestinationDirectoryIfItDoesntExist( DirectoryInfo directory )
		{
			bool success = true;

			if ( directory.Exists == false )
			{
				TraceItem( SyncResult.ItemType.Directory, SyncResult.ReasonType.OnlyIn, FileDisplayName( directory.FullName, Configuration.DestinationDirectory ), 
					Configuration.SourceDirectory );

				if ( Configuration.AnalyseOnly == false )
				{
					try
					{
						// Create the directory
						Trace( "Creating directory: {0}", directory );
						directory.Create();
					}
					catch ( IOException exception )
					{
						Trace( "Error: failed to create directory {0}. {1}", directory, exception.Message );
						success = false;
					}
				}
			}

			return success;
		}

		/// <summary>
		/// Synchonise files between the source and destination directories
		/// </summary>
		/// <param name="sourceDirectory"></param>
		/// <param name="destinationDirectory"></param>
		/// <returns></returns>
		private bool SynchroniseFiles( DirectoryInfo sourceDirectory, DirectoryInfo destinationDirectory )
		{
			bool success = true;

			// Get list of files from source directory
			List<FileInfo> sourceFiles = GetFiles( sourceDirectory, Configuration );

			// Get list of files in destination directory
			List<FileInfo> destinationFiles = GetFiles( destinationDirectory, null );

			// Make sure there was no problem accessing the files
			if ( ( sourceFiles != null ) && ( destinationFiles != null ) )
			{
				// Form destination file dictionary for quick lookup                
				Dictionary<string, FileInfo> destinationFilesDictionary = new Dictionary<string, FileInfo>();
				foreach ( FileInfo destinationFile in destinationFiles )
				{
					destinationFilesDictionary.Add( destinationFile.Name, destinationFile );
				}

				// Compare files in the source directory against the destination directory
				for ( int fileIndex = 0; ( fileIndex < sourceFiles.Count ) && ( success == true ); ++fileIndex )
				{
					success = SynchroniseFile( sourceFiles[ fileIndex ], destinationDirectory, destinationFilesDictionary );
				}

				// Delete extra files in destination directory if specified
				if ( Configuration.DeleteFromDest == true )
				{
					// Form source file dictionary for quick lookup
					Dictionary<string, FileInfo> sourceFilesDictionary = new Dictionary<string, FileInfo>();
					foreach ( FileInfo sourceFile in sourceFiles )
					{
						sourceFilesDictionary.Add( sourceFile.Name, sourceFile );
					}

					for ( int fileIndex = 0; ( fileIndex < destinationFiles.Count ) && ( success == true ); ++fileIndex )
					{
						FileInfo destinationFile = destinationFiles[ fileIndex ];

						if ( sourceFilesDictionary.ContainsKey( destinationFile.Name ) == false )
						{
							success = DeleteUnmatchedFile( destinationFile );
						}
					}
				}
			}
			else
			{
				success = false;
			}

			return success;
		}

		/// <summary>
		/// Synchronise a source file in the destination directory.
		/// If the source and destination files do not match, or the destination file does not exist, then copy the source file to the destination
		/// directory
		/// </summary>
		/// <param name="sourceFile">The file to synchronise</param>
		/// <param name="destinationDirectory">The destination directory</param>
		/// <param name="destinationFiles">All the existing files in the destination directory</param>
		/// <returns></returns>
		private bool SynchroniseFile( FileInfo sourceFile, DirectoryInfo destinationDirectory, Dictionary<string, FileInfo> destinationFiles )
		{
			bool success = true;
			bool filesMatch = false;
			FileInfo destinationFile = null;

			// Look up in hash table to see if file exists in destination
			if ( destinationFiles.ContainsKey( sourceFile.Name ) == true )
			{
				// Check if the file lengths and write times match
				destinationFile = destinationFiles[ sourceFile.Name ];
				if ( sourceFile.LastWriteTime == destinationFile.LastWriteTime )
				{
					if ( sourceFile.Length == destinationFile.Length )
					{
						if ( Configuration.ExcludeIdenticalFiles == false )
						{
							TraceItem( SyncResult.ItemType.File, SyncResult.ReasonType.Identical, FileDisplayName( sourceFile.FullName, Configuration.SourceDirectory ),
								Configuration.SourceDirectory );
						}

						filesMatch = true;
					}
					else
					{
						TraceItem( SyncResult.ItemType.File, SyncResult.ReasonType.Length, FileDisplayName( sourceFile.FullName, Configuration.SourceDirectory ),
							Configuration.SourceDirectory );
					}
				}
				else
				{
					TraceItem( SyncResult.ItemType.File, SyncResult.ReasonType.ModifiedTime, FileDisplayName( sourceFile.FullName, Configuration.SourceDirectory ),
							Configuration.SourceDirectory );
				}
			}
			else
			{
				TraceItem( SyncResult.ItemType.File, SyncResult.ReasonType.OnlyIn, FileDisplayName( sourceFile.FullName, Configuration.SourceDirectory ),
							Configuration.SourceDirectory );
			}

			if ( Configuration.AnalyseOnly == false )
			{
				// If the file doesn't exist or is different, copy the source file to destination
				if ( filesMatch == false )
				{
					string destPath = Path.Combine( destinationDirectory.FullName, sourceFile.Name );

					// Make sure destination is not read-only
					if ( ( destinationFile != null ) && ( destinationFile.IsReadOnly == true ) )
					{
						destinationFile.IsReadOnly = false;
					}

					try
					{
						// Copy the file
						Trace( "Copying: {0} -> {1}", sourceFile.FullName, Path.GetFullPath( destPath ) );

						sourceFile.CopyTo( destPath, true );

						// Set attributes appropriately
						File.SetAttributes( destPath, sourceFile.Attributes );
					}
					catch ( Exception ex )
					{
						Trace( "Error: failed to copy file from {0} to {1}. {2}", sourceFile.FullName, destPath, ex.Message );
						success = false;
					}
				}
			}

			return success;
		}

		/// <summary>
		/// Delete the specified file that has not been matched in the destination.
		/// </summary>
		/// <param name="fileToDelete"></param>
		/// <returns></returns>
		private bool DeleteUnmatchedFile( FileInfo fileToDelete )
		{
			bool success = true;

			// If this file is specified in exclude-from-deletion list, don't delete it
			if ( ShouldExclude( Configuration.DeleteExcludeFiles, null, fileToDelete.Name ) == false )
			{
				TraceItem( SyncResult.ItemType.File, SyncResult.ReasonType.OnlyIn, FileDisplayName( fileToDelete.FullName, Configuration.DestinationDirectory ),
					Configuration.DestinationDirectory );

				if ( Configuration.AnalyseOnly == false )
				{
					try
					{
						// Delete the file
						Trace( "Deleting: {0} ", fileToDelete.FullName );

						fileToDelete.IsReadOnly = false;
						fileToDelete.Delete();
					}
					catch ( Exception ex )
					{
						Trace( "Error: failed to delete file from {0}. {1}", fileToDelete.FullName, ex.Message );
						success = false;
					}
				}
			}
			return success;
		}

		/// <summary>
		/// Synchronise all of the subdirectories in the source directory with the destination directory and then delete any extra destination subdirectories 
		/// </summary>
		/// <param name="sourceDirectory"></param>
		/// <param name="destinationDirectory"></param>
		/// <returns></returns>
		private bool SynchroniseSubDirectories( DirectoryInfo sourceDirectory, DirectoryInfo destinationDirectory )
		{
			bool success = true;

			// Get list of selected subdirectories in source directory
			List<DirectoryInfo> sourceSubDirectories = GetDirectories( sourceDirectory, Configuration );

			// Get list of subdirectories in destination directory
			List<DirectoryInfo> destinationSubDirectories = GetDirectories( destinationDirectory, Configuration );

			// Any problems accessing the subdirectories
			if ( ( sourceSubDirectories != null ) && ( destinationSubDirectories != null ) )
			{
				// Add selected source subdirectories to dictionary, and recursively process them
				Dictionary<string, DirectoryInfo> sourceDirectoriesDictionary = new Dictionary<string, DirectoryInfo>();

				for ( int directoryIndex = 0; ( directoryIndex < sourceSubDirectories.Count ) && ( success == true ); ++directoryIndex )
				{
					DirectoryInfo sourceSubDirectory = sourceSubDirectories[ directoryIndex ];

					sourceDirectoriesDictionary.Add( sourceSubDirectory.Name, sourceSubDirectory );

					// Recurse into this directory
					success = ProcessDirectory( sourceSubDirectory.FullName, Path.Combine( destinationDirectory.FullName, sourceSubDirectory.Name ) );
				}

				// Delete extra directories in destination if specified
				foreach ( DirectoryInfo destinationSubDirectory in destinationSubDirectories )
				{
					// does this destination subdirectory exist in the source subdirs?
					if ( sourceDirectoriesDictionary.ContainsKey( destinationSubDirectory.Name ) == false )
					{
						// if this directory is specified in exclude-from-deletion list, don't delete it
						if ( ShouldExclude( Configuration.DeleteExcludeDirs, null, destinationSubDirectory.Name ) == false )
						{
							TraceItem( SyncResult.ItemType.Directory, SyncResult.ReasonType.OnlyIn, 
								FileDisplayName( destinationSubDirectory.FullName, Configuration.DestinationDirectory ), Configuration.DestinationDirectory );

							if ( ( Configuration.AnalyseOnly == false ) && ( Configuration.DeleteFromDest == true ) )
							{
								try
								{
									Trace( "Deleting directory: {0} ", destinationSubDirectory.FullName );

									// Delete directory
									ForceDeleteDirectory( destinationSubDirectory );
								}
								catch ( Exception ex )
								{
									Trace( "Error: failed to delete directory {0}. {1}", destinationSubDirectory.FullName, ex.Message );
									success = false;
								}
							}
						}
					}
				}
			}
			else
			{
				success = false;
			}

			return success;
		}

		/// <summary>
		/// Delete the directory and all of its contents irrespective of any read-only attributes
		/// </summary>
		/// <param name="directory"></param>
		private void ForceDeleteDirectory( DirectoryInfo directory )
		{
			directory.Attributes = FileAttributes.Normal;

			foreach ( FileSystemInfo info in directory.GetFileSystemInfos( "*", SearchOption.AllDirectories ) )
			{
				info.Attributes = FileAttributes.Normal;
			}

			directory.Delete( true );
		}

		/// <summary>
		/// Gets list of files in specified directory, optionally filtered by specified input parameters
		/// </summary>
		/// <param name="directoryInfo"></param>
		private List<FileInfo> GetFiles( DirectoryInfo directoryInfo, InputParams filesConfiguration )
		{
			List<FileInfo> returnList = null;

			try
			{
				// Get all files in this directory
				List<FileInfo> fileList = new List<FileInfo>( directoryInfo.GetFiles() );

				// Do we need to do any filtering?
				if ( ( filesConfiguration != null ) && ( filesConfiguration.AreSourceFilesFiltered == true ) )
				{
					// Copy all included files to the return list
					returnList = new List<FileInfo>();

					foreach ( FileInfo fileInfo in fileList )
					{
						// Only copy files that are not explicitly excluded or excluded due to being hidden and the exclude hidden option is set
						if ( ( ( filesConfiguration.ExcludeHidden == false ) || ( ( fileInfo.Attributes & FileAttributes.Hidden ) == 0 ) ) &&
							( ShouldExclude( filesConfiguration.ExcludeFiles, filesConfiguration.IncludeFiles, fileInfo.Name ) == false ) )
						{
							returnList.Add( fileInfo );
						}
					}
				}
				else
				{
					// Return all the files
					returnList = fileList;
				}
			}
			catch ( UnauthorizedAccessException exception )
			{
				Trace( "Error: failed to get files for directory {0}. {1}", directoryInfo, exception.Message );
			}

			return returnList;
		}

		/// <summary>
		/// Gets list of subdirectories of specified directory, optionally filtered by specified input parameters
		/// </summary>
		/// <param name="directoryInfo"></param>
		private List<DirectoryInfo> GetDirectories( DirectoryInfo directoryInfo, InputParams filesConfiguration )
		{
			List<DirectoryInfo> returnList = null;

			try
			{
				// Get all directories
				List<DirectoryInfo> directoryList = new List<DirectoryInfo>( directoryInfo.GetDirectories() );

				// do we need to do any filtering?
				bool needFilter = ( filesConfiguration != null ) && ( filesConfiguration.AreSourceFilesFiltered == true );
				if ( needFilter == true )
				{
					returnList = new List<DirectoryInfo>();

					foreach ( DirectoryInfo subdirInfo in directoryList )
					{
						// filter out directories based on hiddenness and exclude/include filespecs
						if ( ( ( filesConfiguration.ExcludeHidden == false ) || ( ( subdirInfo.Attributes & FileAttributes.Hidden ) == 0 ) ) &&
							( ShouldExclude( filesConfiguration.ExcludeDirs, filesConfiguration.IncludeDirs, subdirInfo.Name ) == false ) )
						{
							returnList.Add( subdirInfo );
						}
					}
				}
				else
				{
					// Return all the directories
					returnList = directoryList;
				}
			}
			catch ( Exception generalException )
			{
				Trace( "Error: failed to get sub-directories for directory {0}. {1}", directoryInfo, generalException.Message );
			}

			return returnList;
		}

		/// <summary>
		/// For a given include and exclude list of regex's and a name to match, determines if the
		/// named item should be excluded
		/// </summary>
		/// <param name="excludeList"></param>
		/// <param name="includeList"></param>
		/// <param name="name"></param>
		private bool ShouldExclude( Regex[] excludeList, Regex[] includeList, string name )
		{
			bool exclude = false;

			if ( excludeList != null )
			{
				// Check against regex's in our exclude list
				for ( int index = 0; ( ( index < excludeList.Length ) && ( exclude == false ) ); ++index )
				{
					// If the name matches an entry in the exclude list, we SHOULD exclude it
					exclude = excludeList[ index ].Match( name ).Success;
				}
			}
			else if ( includeList != null )
			{
				// Assume excluded
				exclude = true;

				for ( int index = 0; ( ( index < includeList.Length ) && ( exclude == true ) ); ++index )
				{
					exclude = ( includeList[ index ].Match( name ).Success == false );
				}
			}

			return exclude;
		}

        /// <summary>
        /// Trace message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        private void Trace( string message, params object[] args )
        {
            if ( Log != null)
			{
                Log.Invoke( results.AddResult( String.Format( message, args ) ) );
			}
        }

		private void TraceItem( SyncResult.ItemType type, SyncResult.ReasonType reason, string itemName )
		{
			TraceItem( type, reason, itemName, null );
		}

		private void TraceItem( SyncResult.ItemType type, SyncResult.ReasonType reason, string itemName, string containerName )
		{
			if ( Log != null )
			{
				Log.Invoke( results.AddResult( type, reason, itemName, containerName ) );
			}
		}

		/// <summary>
		/// Remove the root directory from the front of the filename
		/// </summary>
		/// <param name="fullName"></param>
		/// <param name="rootDirectory"></param>
		/// <returns></returns>
		private static string FileDisplayName( string fullName, string rootDirectory )
		{
			string displayName = fullName;

			if ( fullName.StartsWith( rootDirectory ) == true )
			{
				displayName = fullName.Substring( rootDirectory.Length );
			}

			return displayName;
		}

		private SyncResults results = null;
    }
}