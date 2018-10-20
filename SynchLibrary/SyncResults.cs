using System;
using System.Collections.Generic;

namespace SynchLibrary
{
    public class SyncResults
    {
		public List< SyncResult> Results{ get; } = new List<SyncResult>();

		public SyncResult AddResult( string traceMessage )
		{
			SyncResult result = new SyncResult( traceMessage );
			Results.Add( result );
			return result;
		}

		public SyncResult AddResult( SyncResult.ItemType type,SyncResult.ReasonType reason, string itemName, string containerName )
		{
			SyncResult result = new SyncResult( type,reason, itemName, containerName );
			Results.Add( result );
			return result;
		}
	}

	public class SyncResult
	{
		public enum ItemType
		{
			File,
			Directory,
			Trace
		}

		public enum ReasonType
		{
			OnlyIn,
			Length,
			ModifiedTime,
			Identical
		}

		public SyncResult( string traceMessage )
		{
			Item = ItemType.Trace;
			Message = traceMessage;
		}

		public SyncResult( SyncResult.ItemType type, SyncResult.ReasonType reason, string itemName, string containerName )
		{
			Item = type;
			Reason = reason;
			Message = itemName;
			Container = containerName;
		}

		/// <summary>
		/// The type of item (file, directory or general trace message) that this result is for
		/// </summary>
		public ItemType Item { get; set; }

		public ReasonType Reason { get; set; }

		public string Message { get; set; }

		public string Container { get; set; }
	}
}
