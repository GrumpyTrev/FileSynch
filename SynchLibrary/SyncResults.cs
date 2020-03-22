using System;
using System.Collections.Generic;

namespace SynchLibrary
{
	/// <summary>
	/// The SyncResults class contains a number of SyncResult entries gathered during the synchronisation process
	/// </summary>
	public class SyncResults
    {
		/// <summary>
		/// Add a simple trace message to the collection
		/// </summary>
		/// <param name="traceMessage"></param>
		/// <returns></returns>
		public SyncResult AddResult( string traceMessage )
		{
			SyncResult result = new SyncResult( traceMessage );
			Results.Add( result );
			return result;
		}

		/// <summary>
		/// Add a fully specified SyncResult to the collection
		/// </summary>
		/// <param name="type"></param>
		/// <param name="reason"></param>
		/// <param name="container"></param>
		/// <param name="itemName"></param>
		/// <param name="containerName"></param>
		/// <returns></returns>
		public SyncResult AddResult( SyncResult.ItemType type, SyncResult.ReasonType reason, SyncResult.ContainerType container, string itemName, string containerName )
		{
			SyncResult result = new SyncResult( type, reason, container, itemName, containerName );
			Results.Add( result );
			return result;
		}

		/// <summary>
		/// Collection of SyncResult objects
		/// </summary>
		public List<SyncResult> Results { get; } = new List<SyncResult>();
	}

	public class SyncResult
	{
		/// <summary>
		/// The type of the item being synchronised
		/// </summary>
		public enum ItemType
		{
			File,
			Directory,
			Trace
		}

		/// <summary>
		/// The reason for this result
		/// </summary>
		public enum ReasonType
		{
			OnlyIn,
			Length,
			ModifiedTime,
			Identical
		}

		/// <summary>
		/// Is this result associated with the source or destination
		/// </summary>
		public enum ContainerType
		{
			Source,
			Destination,
			NA
		}

		/// <summary>
		/// Create a simple trace message
		/// </summary>
		/// <param name="traceMessage"></param>
		public SyncResult( string traceMessage )
		{
			Item = ItemType.Trace;
			Message = traceMessage;
		}

		/// <summary>
		/// Create a fully specified result
		/// </summary>
		/// <param name="type"></param>
		/// <param name="reason"></param>
		/// <param name="container"></param>
		/// <param name="itemName"></param>
		/// <param name="containerName"></param>
		public SyncResult( SyncResult.ItemType type, SyncResult.ReasonType reason, SyncResult.ContainerType container, string itemName, string containerName )
		{
			Item = type;
			Reason = reason;
			Context = container;
			Message = itemName;
			Container = containerName;
		}

		/// <summary>
		/// The type of item (file, directory or general trace message) that this result is for
		/// </summary>
		public ItemType Item { get; set; }

		/// <summary>
		/// The reason for the result
		/// </summary>
		public ReasonType Reason { get; set; }

		/// <summary>
		/// The context of the result
		/// </summary>
		public ContainerType Context { get; set; }

		/// <summary>
		/// The text message of the result
		/// </summary>
		public string Message { get; set; }

		/// <summary>
		/// The name of the container (directory)
		/// </summary>
		public string Container { get; set; }
	}
}
