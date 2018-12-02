using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynchApp
{
	class Logging: TextWriter
	{
		/// <summary>
		/// Create a new logging object.
		/// Delete any log and error files older than the specified number of days.
		/// Create new log and error files based on the current date and a numeric value to provide uniqueness
		/// </summary>
		/// <param name="daysToKeep"></param>
		public Logging( int daysToKeep )
		{
			string logFileName = string.Format( "Diff{0}.txt", DateTime.Today.ToString( "yyyyMMdd") );
			logStream = new StreamWriter( logFileName, true );
		}

		/// <summary>
		/// Close the underlying stream
		/// </summary>
		public override void Close()
		{
			logStream.Close();
		}

		/// <summary>
		/// Flush the underlying stream
		/// </summary>
		public override void Flush()
		{
			logStream.Flush();
		}

		/// <summary>
		/// Output a string to the log file
		/// </summary>
		/// <param name="value"></param>
		public override void Write( string value )
		{
			logStream.Write( value );
		}

		/// <summary>
		/// Output a string and linefeed to the log file
		/// </summary>
		/// <param name="value"></param>
		public override void WriteLine( string value )
		{
			logStream.WriteLine( value );
			logStream.Flush();
		}

		/// <summary>
		/// The character encoding in which the output is written.
		/// </summary>
		public override Encoding Encoding
		{
			get
			{
				return Encoding.ASCII;
			}
		}

		/// <summary>
		/// The StreamWriter used to log 
		/// </summary>
		private StreamWriter logStream = null;

	}
}
