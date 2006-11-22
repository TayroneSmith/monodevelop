using System;
using System.Collections;
using System.IO;

using System.Runtime.InteropServices;
using MonoDevelop.Core;

namespace VersionControl.Service.Subversion
{
	public class SvnClient
	{
		IntPtr pool;
		IntPtr ctx;
		static LibApr apr;
		
		object sync = new object();
		bool inProgress = false;
		
		IProgressMonitor updatemonitor;
		string commitmessage = null;
		
		// retain this so the delegates aren't GC'ed
		svn_client_ctx_t ctxstruct;
		
		static SvnClient() {
			apr = LibApr.GetLib ();
		}

		private IntPtr newpool(IntPtr parent) {
			IntPtr p;
			apr.pool_create_ex(out p, parent, IntPtr.Zero, IntPtr.Zero);
			if (p == IntPtr.Zero)
				throw new InvalidOperationException("Could not create an APR pool.");
			return p;
		}
		
		public SvnClient() {
			// Allocate the APR pool and the SVN client context.
			
			pool = newpool(IntPtr.Zero);
		
			if (svn_client_create_context(out ctx, pool) != IntPtr.Zero)
				throw new InvalidOperationException("Could not create a Subversion client context.");
				
			// Set the callbacks on the client context structure.
		
			// This is quite a roudabout way of doing this.  The task
			// is to set the notify field of the unmanaged structure
			// at the address 'ctx' -- with the address of a delegate.
			// There's no way to get an address for the delegate directly,
			// as far as I could figure out, so instead I create a managed
			// structure that mirrors the start of the unmanaged structure
			// I want to modify.  Then I marshal the managed structure
			// *onto* to unmanaged one, overwriting fields in the process.
			// I don't use references to the structure itself in the API
			// calls because it needs to be allocated by SVN.  I hope
			// this doesn't cause any memory leaks.
			ctxstruct = new svn_client_ctx_t();
			ctxstruct.NotifyFunc = new svn_wc_notify_func_t(svn_wc_notify_func_t_impl);
			ctxstruct.LogMsgFunc = new svn_client_get_commit_log_t(svn_client_get_commit_log_impl);
			Marshal.StructureToPtr(ctxstruct, ctx, false);
		}
		
		[StructLayout(LayoutKind.Sequential)]
		private struct svn_client_ctx_t {
			public IntPtr auth_baton;
			public svn_wc_notify_func_t NotifyFunc;
			public IntPtr notify_baton;
			public svn_client_get_commit_log_t LogMsgFunc;
			public IntPtr logmsg_baton;
		}

		~SvnClient() {
			apr.pool_destroy(pool);
		}

		// Wrappers for native interop
		
		public string Version {
			get {
				IntPtr ptr = svn_client_version();
				svn_version_t ver = (svn_version_t)Marshal.PtrToStructure(ptr, typeof(svn_version_t));				
				return ver.major + "." + ver.minor + "." + ver.patch;
			}
		}
		
		public IList List(string pathorurl, bool recurse, Rev revision) {
			if (pathorurl == null) throw new ArgumentException();
			
			IntPtr localpool = newpool(pool);
			ArrayList items = new ArrayList();
			try {
				IntPtr hash;
			
				CheckError(svn_client_ls(out hash, pathorurl, ref revision,
	               recurse ? 1 : 0, ctx, localpool));
	               
	             IntPtr item = apr.hash_first(localpool, hash);
	             while (item != IntPtr.Zero) {
	             	IntPtr nameptr, val;
	             	int namelen;
	             	apr.hash_this(item, out nameptr, out namelen, out val);
	             
	             	string name = Marshal.PtrToStringAnsi(nameptr);
					svn_dirent_t ent = (svn_dirent_t)Marshal.PtrToStructure(val, typeof(svn_dirent_t));				
	             	item = apr.hash_next(item);
	             	
	             	items.Add(new DirEnt(name, ent));
	             }	             
	             
			} finally {
				apr.pool_destroy(localpool);
			}
			return items;
		}

		public IList Status(string path, Rev revision) {
			return Status(path, revision, false, false, false);
		}
		
		public IList Status(string path, Rev revision, bool descendDirs, bool changedItemsOnly, bool remoteStatus) {
			if (path == null) throw new ArgumentException();
		
			ArrayList ret = new ArrayList();
			int result_rev = 0;

			StatusCollector collector = new StatusCollector(ret);

			IntPtr localpool = newpool(pool);
			try {
				CheckError(svn_client_status (ref result_rev, path, ref revision,
					new svn_wc_status_func_t(collector.Func),
					IntPtr.Zero,
					descendDirs ? 1 : 0, 
					changedItemsOnly ? 0 : 1, 
					remoteStatus ? 1 : 0,
					1,
					ctx, localpool));
			} finally {
				apr.pool_destroy(localpool);
			}
				
			return ret;
		}
		
		public IList Log(string path, Rev revisionStart, Rev revisionEnd) {
			if (path == null) throw new ArgumentException();
			
			ArrayList ret = new ArrayList();
			IntPtr localpool = newpool(pool);
			IntPtr strptr = IntPtr.Zero;
			try {
				IntPtr array = apr.array_make(localpool, 0, IntPtr.Size);
				IntPtr first = apr.array_push(array);
				strptr = Marshal.StringToHGlobalAnsi(path);
				Marshal.WriteIntPtr(first, strptr);

				LogCollector collector = new LogCollector(ret);
				
				CheckError(svn_client_log (
					array,
					ref revisionStart, ref revisionEnd,
					1,
		            0,
		        	new svn_log_message_receiver_t(collector.Func),
		            IntPtr.Zero,
		            ctx, localpool));
			} finally {
				if (strptr != IntPtr.Zero)
					Marshal.FreeHGlobal(strptr);
				apr.pool_destroy(localpool);
			}
			return ret;
		}
		
		public string GetPathUrl(string path) {
			if (path == null) throw new ArgumentNullException();
			
			IntPtr ret = IntPtr.Zero;
			IntPtr localpool = newpool(pool);
			try {
				CheckError(svn_client_url_from_path(ref ret, path, localpool));
			} finally {
				apr.pool_destroy(localpool);
			}
			if (ret == IntPtr.Zero) return null;
			return Marshal.PtrToStringAnsi(ret);
		}
		
		public string Cat(string pathorurl, Rev revision) {
			MemoryStream memstream = new MemoryStream();
			Cat(pathorurl, revision, memstream);
			try {
				return System.Text.Encoding.UTF8.GetString(memstream.GetBuffer());
			} catch {
			}
			return System.Text.Encoding.ASCII.GetString(memstream.GetBuffer());
		}

		public void Cat(string pathorurl, Rev revision, Stream stream) {
			if (pathorurl == null) throw new ArgumentException();
			if (stream == null) throw new ArgumentException();
			
			IntPtr localpool = newpool(pool);
			try {
				StreamCollector collector = new StreamCollector(stream);
				IntPtr svnstream = svn_stream_create(IntPtr.Zero, localpool);
				svn_stream_set_write(svnstream, new svn_readwrite_fn_t(collector.Func));
				CheckError(svn_client_cat(svnstream, pathorurl, ref revision, ctx, localpool));
			} finally {
				apr.pool_destroy(localpool);
			}
		}

		public void Update(string path, bool recurse, IProgressMonitor monitor)
		{
			if (path == null || monitor == null)
				throw new ArgumentException();
			
			lock (sync) {
				if (inProgress)
					throw new SubversionException("Another Subversion operation is already in progress.");
				inProgress = true;
			}
			
			updatemonitor = monitor;
			
			int result_rev;
			Rev rev = Rev.Head;
			IntPtr localpool = newpool(pool);
			try {
				CheckError (svn_client_update(out result_rev, path, ref rev, recurse ? 1 : 0, ctx, localpool));
			} finally {
				apr.pool_destroy(localpool);
				updatemonitor = null;
				inProgress = false;
			}
		}
		
		public void Revert (string[] paths, bool recurse, IProgressMonitor monitor)
		{
			if (paths == null || monitor == null)
				throw new ArgumentException();
			
			lock (sync) {
				if (inProgress)
					throw new SubversionException("Another Subversion operation is already in progress.");
				inProgress = true;
			}
			
			updatemonitor = monitor;
			IntPtr localpool = newpool(pool);
			
			// Put each item into an APR array.
			IntPtr array = apr.array_make(localpool, 0, IntPtr.Size);
			foreach (string path in paths) {
				IntPtr item = apr.array_push(array);
				Marshal.WriteIntPtr (item, apr.pstrdup (localpool, path));
			}
			
			try {
				CheckError (svn_client_revert (array, recurse ? 1 : 0, ctx, localpool));
			} finally {
				apr.pool_destroy(localpool);
				updatemonitor = null;
				inProgress = false;
			}
		}
		
		public void Add(string path, bool recurse, IProgressMonitor monitor) {
			if (path == null || monitor == null) throw new ArgumentException();
			
			lock (sync) {
				if (inProgress)
					throw new SubversionException("Another Subversion operation is already in progress.");
				inProgress = true;
			}
			
			updatemonitor = monitor;
			IntPtr localpool = newpool(pool);
			try {
				CheckError(svn_client_add(path, (recurse ? 1 :0), ctx, localpool));
			} finally {
				apr.pool_destroy(localpool);
				updatemonitor = null;
				inProgress = false;
			}
		}
		
		public void Checkout (string url, string path, Rev rev, bool recurse, IProgressMonitor monitor) {
			if (url == null || monitor == null) throw new ArgumentException();
			
			lock (sync) {
				if (inProgress)
					throw new SubversionException("Another Subversion operation is already in progress.");
				inProgress = true;
			}
			
			updatemonitor = monitor;
			Rev revision;
			IntPtr localpool = newpool(pool);
			try {
				CheckError(svn_client_checkout(out revision, url, path, ref rev, (recurse ? 1 :0), ctx, localpool));
			} finally {
				apr.pool_destroy(localpool);
				updatemonitor = null;
				inProgress = false;
			}
		}

		public void Commit(string[] paths, string message, IProgressMonitor monitor) {
			if (paths == null || message == null || monitor == null)
				throw new ArgumentNullException();
		
			lock (sync) {
				if (inProgress) throw new SubversionException("Another Subversion operation is already in progress.");
				inProgress = true;
			}

			updatemonitor = monitor;
			
			IntPtr localpool = newpool(pool);
			try {
				// Put each item into an APR array.
				IntPtr array = apr.array_make(localpool, 0, IntPtr.Size);
				foreach (string path in paths) {
					IntPtr item = apr.array_push(array);
					Marshal.WriteIntPtr(item, apr.pstrdup(localpool, path));
				}

				IntPtr commit_info = IntPtr.Zero;
				
				commitmessage = message;
		
				CheckError(svn_client_commit (
					ref commit_info, array,
					0, ctx, localpool));
			} finally {
				commitmessage = null;
				updatemonitor = null;
				apr.pool_destroy(localpool);
				inProgress = false;
			}
		}
		
		public void Mkdir (string[] paths, string message, IProgressMonitor monitor) 
		{
			if (paths == null || monitor == null)
				throw new ArgumentNullException();
		
			lock (sync) {
				if (inProgress) throw new SubversionException("Another Subversion operation is already in progress.");
				inProgress = true;
			}

			updatemonitor = monitor;
			
			IntPtr localpool = newpool(pool);
			try {
				// Put each item into an APR array.
				IntPtr array = apr.array_make(localpool, paths.Length, IntPtr.Size);
				foreach (string path in paths) {
					IntPtr item = apr.array_push(array);
					Marshal.WriteIntPtr(item, apr.pstrdup(localpool, path));
				}
				
				commitmessage = message;

				IntPtr commit_info = IntPtr.Zero;
				IntPtr pp = svn_client_mkdir2 (ref commit_info, array, ctx, localpool); 
				CheckError(pp);
			} finally {	
				commitmessage = null;
				updatemonitor = null;
				apr.pool_destroy(localpool);
				inProgress = false;
			}
		}
		
		public void Delete(string path, bool force, IProgressMonitor monitor) {
			if (path == null || monitor == null)
				throw new ArgumentNullException();
		
			lock (sync) {
				if (inProgress) throw new SubversionException("Another Subversion operation is already in progress.");
				inProgress = true;
			}

			updatemonitor = monitor;
			
			IntPtr localpool = newpool(pool);
			try {
				// Put each item into an APR array.
				IntPtr array = apr.array_make(localpool, 0, IntPtr.Size);
				//foreach (string path in paths) {
					IntPtr item = apr.array_push(array);
					Marshal.WriteIntPtr(item, apr.pstrdup(localpool, path));
				//}

				int result_rev;
						
				CheckError(svn_client_delete(out result_rev, array, (force ? 1 : 0), ctx, localpool));
			} finally {
				commitmessage = null;
				updatemonitor = null;
				apr.pool_destroy(localpool);
				inProgress = false;
			}
		}
		
		public void Move(string srcPath, string destPath, Rev revision, bool force, IProgressMonitor monitor) {
			if (srcPath == null || destPath == null || monitor == null) throw new ArgumentException();
			
			lock (sync) {
				if (inProgress)
					throw new SubversionException("Another Subversion operation is already in progress.");
				inProgress = true;
			}
			
			updatemonitor = monitor;
			int result_rev;
			IntPtr localpool = newpool(pool);
			try {
				CheckError(svn_client_move(out result_rev, srcPath, ref revision,
											   destPath, (force ? 1 : 0), ctx, localpool));
			} finally {
				apr.pool_destroy(localpool);
				updatemonitor = null;
				inProgress = false;
			}
		}
		
		public string PathDiff (string path1, Rev revision1, string path2, Rev revision2, bool recursive)
		{
			IntPtr outfile = IntPtr.Zero;
			IntPtr errfile = IntPtr.Zero;
			string fout = null;
			string ferr = null;
			IntPtr localpool = newpool(pool);
			
			try {
				IntPtr options = apr.array_make (localpool, 0, IntPtr.Size);
				
				fout = Path.GetTempFileName ();
				ferr = Path.GetTempFileName ();
				int res1 = apr.file_open (ref outfile, fout, APR_WRITE | APR_CREATE | APR_TRUNCATE, APR_OS_DEFAULT, localpool);
				int res2 = apr.file_open (ref errfile, ferr, APR_WRITE | APR_CREATE | APR_TRUNCATE, APR_OS_DEFAULT, localpool);
				
				if (res1==0 && res2==0) {
					CheckError (svn_client_diff (options, path1, ref revision1, path2, ref revision2, (recursive ? 1 : 0), 0, 1, outfile, errfile, ctx, localpool));
					return fout;
				} else {
					throw new Exception ("Could not get diff information");
				}
			} catch {
				try {
					if (outfile != IntPtr.Zero)
						apr.file_close (outfile);
					if (fout != null)
						File.Delete (fout);
					outfile = IntPtr.Zero;
					fout = null;
				} catch {}
				throw;
			} finally {
				try {
					// Cleanup
					apr.pool_destroy (localpool);
					if (outfile != IntPtr.Zero)
						apr.file_close (outfile); 
					if (errfile != IntPtr.Zero)
						apr.file_close (errfile);
					if (ferr != null)
						File.Delete (ferr);
				} catch {}
			}
		}
		
		IntPtr svn_client_get_commit_log_impl(ref IntPtr log_msg,
			ref IntPtr tmp_file, IntPtr commit_items, IntPtr baton,
			IntPtr pool) {
			log_msg = apr.pstrdup(pool, commitmessage);
			tmp_file = IntPtr.Zero;
			return IntPtr.Zero;
		}

		private void CheckError(IntPtr error) {
			if (error == IntPtr.Zero) return;
			svn_error_t error_t = (svn_error_t)Marshal.PtrToStructure(error, typeof(svn_error_t));				
			throw new SubversionException(error_t.message);
		}
		
		void svn_wc_notify_func_t_impl(IntPtr baton, IntPtr path,
			NotifyAction action, NodeKind kind, IntPtr mime_type,
			NotifyState content_state, NotifyState prop_state, long revision) {
				string actiondesc = action.ToString();
				switch (action) {
					case NotifyAction.UpdateAdd: actiondesc = "Added"; break;
					case NotifyAction.UpdateDelete: actiondesc = "Deleted"; break;
					case NotifyAction.UpdateUpdate: actiondesc = "Updating"; break;
					case NotifyAction.UpdateExternal: actiondesc = "External Updated"; break;
					case NotifyAction.UpdateCompleted: actiondesc = "Finished"; break;
					
					case NotifyAction.CommitAdded: actiondesc = "Added"; break;
					case NotifyAction.CommitDeleted: actiondesc = "Deleted"; break;
					case NotifyAction.CommitModified: actiondesc = "Modified"; break;
					case NotifyAction.CommitReplaced: actiondesc = "Replaced"; break;
					case NotifyAction.CommitPostfixTxDelta: actiondesc = "Sending Content"; break;
					/*Add,
					Copy,
					Delete,
					Restore,
					Revert,
					FailedRevert,
					Resolved,
					Skip,
					StatusCompleted,
					StatusExternal,
					BlameRevision*/
				}
			
				if (updatemonitor != null) {
					updatemonitor.Log.WriteLine (actiondesc + " " + Marshal.PtrToStringAnsi (path));
				}
		}
		
		private class StatusCollector {
			ArrayList statuses;
			public StatusCollector(ArrayList statuses) { this.statuses = statuses; }
			public void Func(IntPtr baton, IntPtr path, ref svn_wc_status_t status) {
				string pathstr = Marshal.PtrToStringAnsi(path);
/*				if (status.to_svn_wc_entry_t == IntPtr.Zero)
					return;
*/
				statuses.Add(new StatusEnt(status, pathstr));
			}
  
		}

		private class LogCollector {
			static readonly DateTime Epoch = new DateTime(1970,1,1);

			ArrayList logs;
			public LogCollector(ArrayList logs) { this.logs = logs; }
			public IntPtr Func(IntPtr baton, IntPtr apr_hash_changed_paths, int revision, IntPtr author, IntPtr date, IntPtr message, IntPtr pool) {
				long time;
				svn_time_from_cstring(out time, Marshal.PtrToStringAnsi(date), pool);
				string smessage = "";
				if (message != IntPtr.Zero) smessage = Marshal.PtrToStringAnsi(message).Trim();
			
				ArrayList items = new ArrayList();

				IntPtr item = apr.hash_first(pool, apr_hash_changed_paths);
				while (item != IntPtr.Zero) {
					IntPtr nameptr, val;
					int namelen;
					apr.hash_this(item, out nameptr, out namelen, out val);
	             
					string name = Marshal.PtrToStringAnsi(nameptr);
					svn_log_changed_path_t ch = (svn_log_changed_path_t)Marshal.PtrToStructure(val, typeof(svn_log_changed_path_t));				
					item = apr.hash_next(item);
	             	
					items.Add(new LogEntChangedPath(name, ch));
	             }	             

				logs.Add(new LogEnt(revision, Marshal.PtrToStringAnsi(author), Epoch.AddTicks(time*10), smessage, 
					(LogEntChangedPath[])items.ToArray(typeof(LogEntChangedPath))));
				
				return IntPtr.Zero;
			}
		}
		
		private class StreamCollector {
			Stream buf;
			public StreamCollector(Stream buf) { this.buf = buf; }
			public IntPtr Func(IntPtr baton, IntPtr data, ref int len) {
				Console.WriteLine(len);
				for (int i = 0; i < len; i++)
					buf.WriteByte(Marshal.ReadByte((IntPtr)((int)data+i)));
				return IntPtr.Zero;
			}
		}
		
		public class DirEnt {
			public readonly string Name;
			public readonly bool IsDirectory;
			public readonly long Size;
			public readonly bool HasProps;
			public readonly int CreatedRevision;
			public readonly DateTime Time;
			public readonly string LastAuthor;
			
			static readonly DateTime Epoch = new DateTime(1970,1,1);
			
			internal DirEnt(string name, svn_dirent_t ent) {
				Name = name;
				IsDirectory = (ent.kind == (int)NodeKind.Dir);
				Size = ent.size;
				HasProps = ent.has_props != 0;
				CreatedRevision = ent.created_rev;
				Time = Epoch.AddTicks(ent.time*10);
				LastAuthor = ent.last_author;
			}
			
		}
		
		public class StatusEnt {
			public readonly string LocalFilePath;
			public readonly string Name;
			public readonly int Revision;
  			public readonly string Url;
  			public readonly string Repository;
  			public readonly string RepositoryUuid;
  			public readonly bool IsDirectory;
  			public readonly NodeSchedule Schedule;
  			public readonly bool Copied;
  			public readonly bool Deleted;
  			public readonly bool Absent;
  			public readonly bool Incomplete;
  			public readonly string CopiedFrom;
  			public readonly int CopiedFromRevision;
  			public readonly string ConflictOld;
  			public readonly string ConflictNew;
  			public readonly string ConflictWorking;
  			public readonly string PropertyRejectFile;
  			public readonly DateTime TextTime;
  			public readonly DateTime PropTime;
  			public readonly string Checksum; //(base64 for text-base, or NULL);
  			public readonly int LastCommitRevision;
  			public readonly DateTime LastCommitDate;
  			public readonly string LastCommitAuthor;	
 			
			public readonly VersionStatus TextStatus;
			public readonly VersionStatus PropsStatus;
			public readonly bool Locked;
			public readonly bool Switched;
			public readonly VersionStatus RemoteTextStatus;
			public readonly VersionStatus RemotePropsStatus;
			
			static readonly DateTime Epoch = new DateTime(1970,1,1);

			internal StatusEnt(svn_wc_status_t status, string localpath) {
				LocalFilePath = localpath;
				TextStatus = (VersionStatus)status.svn_wc_status_kind_text;
				PropsStatus = (VersionStatus)status.svn_wc_status_kind_props;
				Locked = status.locked != 0;
				Copied = status.copied != 0;
				Switched = status.switched != 0;
				RemoteTextStatus = (VersionStatus)status.svn_wc_status_kind_text_repo;
				RemotePropsStatus = (VersionStatus)status.svn_wc_status_kind_props_repo;
				
				if (status.to_svn_wc_entry_t == IntPtr.Zero)
					return;
					
				svn_wc_entry_t ent = (svn_wc_entry_t)Marshal.PtrToStructure(status.to_svn_wc_entry_t, typeof(svn_wc_entry_t));
				Name = ent.name;
				Revision = ent.revision;
	  			Url = ent.url;
	  			Repository = ent.repos;
	  			RepositoryUuid = ent.repos_uuid;
	  			IsDirectory = (ent.node_kind == (int)NodeKind.Dir);
	  			Schedule = (NodeSchedule)ent.schedule;
	  			Copied = ent.copied != 0;
	  			Deleted = ent.deleted != 0;
	  			Absent = ent.absent != 0;
	  			Incomplete = ent.incomplete != 0;
	  			CopiedFrom = ent.copied_from;
	  			CopiedFromRevision = ent.copied_rev;
	  			ConflictOld = ent.conflict_old;
	  			ConflictNew = ent.conflict_new;
	  			ConflictWorking = ent.conflict_working;
	  			PropertyRejectFile = ent.property_reject_file;
	  			TextTime = Epoch.AddTicks(ent.text_time*10);
	  			PropTime = Epoch.AddTicks(ent.prop_time*10);
	  			Checksum = ent.checksum;
	  			LastCommitRevision = ent.last_commit_rev;
	  			LastCommitDate = Epoch.AddTicks(ent.last_commit_date*10);
	  			LastCommitAuthor = ent.last_commit_author;	
			}

		}
		
		public class LogEnt {
			public readonly int Revision;
			public readonly string Author;
			public readonly DateTime Time;
			public readonly string Message;
			public readonly LogEntChangedPath[] ChangedPaths;
			
			internal LogEnt(int rev, string author, DateTime time, string msg, LogEntChangedPath[] changes) {
				Revision = rev;
				Author = author;
				Time = time;
				Message = msg;
				ChangedPaths = changes;
			}
		}
		
		public class LogEntChangedPath {
			public readonly string Path;
			public readonly ActionType Action;
			public readonly string CopyFromPath;
			public readonly int CopyFromRevision;
			
			internal LogEntChangedPath(string path, svn_log_changed_path_t info) {
				Path = path;
				CopyFromPath = info.copy_from_path;
				CopyFromRevision = info.copy_from_rev;
				
				switch (info.action) {
				case 'A': Action = ActionType.Add; break;
				case 'D': Action = ActionType.Delete; break;
				case 'R': Action = ActionType.Replace; break;
				default: Action = ActionType.Modify; break; // should be an 'M'
				}
			}
			
			public enum ActionType {
				Add,
				Delete,
				Replace,
				Modify
			}
		}
		
		// Native Interop
		
		
		const int APR_OS_DEFAULT = 0xFFF;
		const int APR_WRITE = 2;
		const int APR_CREATE = 4;
		const int APR_TRUNCATE = 16;

		private const string svnclientlib = "libsvn_client-1.so.0";
		
		private struct svn_error_t {
			public int apr_err;
			public string message;
			public IntPtr svn_error_t_child;
			public IntPtr pool;
		}
		
		private struct svn_version_t {
  			public int major;
  			public int minor;
  			public int patch;
			public string tag;	
  		}
  		
  		enum NodeKind {
  			None,
  			File,
  			Dir,
  			Unknown
  		}

		internal struct svn_dirent_t {
			public int kind;
			public long size;
			public int has_props;
			public int created_rev;
			public long time; // created
			public string last_author;
		}
		
		public enum NodeSchedule {
			Normal,
			Add,
			Delete,
			Replace
		}
		
		public enum VersionStatus {
			EMPTY,
			None,
			Unversioned,
			Normal,
			Added,
			Missing,
			Deleted,
			Replaced,
			Modified,
			Merged,
			Conflicted,
			Ignored,
			Obstructed,
			External,
			Incomplete
		}
		
		internal struct svn_wc_entry_t {
			public string name;
			public int revision;
  			public string url;
  			public string repos;
  			public string repos_uuid;
  			public int node_kind;
  			public int schedule;
  			public int copied;
  			public int deleted;
  			public int absent;
  			public int incomplete;
  			public string copied_from;
  			public int copied_rev;
  			public string conflict_old;
  			public string conflict_new;
  			public string conflict_working;
  			public string property_reject_file;
  			public long text_time; // or zero
  			public long prop_time;
  			public string checksum; //(base64 for text-base, or NULL);
  			public int last_commit_rev;
  			public long last_commit_date;
  			public string last_commit_author;
  		}
  				
		internal struct svn_wc_status_t {
			public IntPtr to_svn_wc_entry_t;
			public int svn_wc_status_kind_text;
			public int svn_wc_status_kind_props;
			public int locked;
			public int copied;
			public int switched;
			public int svn_wc_status_kind_text_repo;
			public int svn_wc_status_kind_props_repo;
		}
		
		public struct Rev {
			public int kind;
			public int number;
			
			public Rev(int kind, int number) {
				this.kind = kind;
				this.number = number;
			}
			
			public static Rev Number(int rev) { return new Rev(1, rev); }
			
			public readonly static Rev Blank = new Rev(0, 0);
			public readonly static Rev First = new Rev(1, 1);
			public readonly static Rev Committed = new Rev(3, 0);
			public readonly static Rev Previous = new Rev(4, 0);
			public readonly static Rev Base = new Rev(5, 0);
			public readonly static Rev Working = new Rev(6, 0);
			public readonly static Rev Head = new Rev(7, 0);
		}
		
		internal struct svn_log_changed_path_t {
			public char action;
			public string copy_from_path;
			public int copy_from_rev;
		}
		
		internal enum NotifyAction {
			Add,
			Copy,
			Delete,
			Restore,
			Revert,
			FailedRevert,
			Resolved,
			Skip,
			UpdateDelete,
			UpdateAdd,
			UpdateUpdate,
			UpdateCompleted,
			UpdateExternal,
			StatusCompleted,
			StatusExternal,
			CommitModified,
			CommitAdded,
			CommitDeleted,
			CommitReplaced,
			CommitPostfixTxDelta,
			BlameRevision
		}
		
		internal enum NotifyState {
			Inapplicable,
			Unknown,
			Unchanged,
			Missing,
			Obstructed,
			Changed,
			Merged,
			Conflicted
		}
		
		delegate void svn_wc_status_func_t(IntPtr baton, IntPtr path,
			ref svn_wc_status_t status);
		
		delegate IntPtr svn_log_message_receiver_t(IntPtr baton,
			IntPtr apr_hash_changed_paths, int revision, IntPtr author,
			IntPtr date, IntPtr message, IntPtr pool);
		
		delegate IntPtr svn_readwrite_fn_t(IntPtr baton, IntPtr data, ref int len);
		
		delegate void svn_wc_notify_func_t(IntPtr baton, IntPtr path,
			NotifyAction action, NodeKind kind, IntPtr mime_type,
			NotifyState content_state, NotifyState prop_state, long revision);
			
		delegate IntPtr svn_client_get_commit_log_t(ref IntPtr log_msg,
			ref IntPtr tmp_file, IntPtr commit_items, IntPtr baton,
			IntPtr pool);

		[DllImport(svnclientlib)] static extern IntPtr svn_client_version();
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_create_context(out IntPtr ctx, IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_ls (
				out IntPtr dirents, string path_or_url, ref Rev revision,
               int recurse, IntPtr ctx, IntPtr pool);

		[DllImport(svnclientlib)] static extern IntPtr svn_client_status (
			ref int result_rev, string path, ref Rev revision,
			svn_wc_status_func_t status_func, IntPtr status_baton,
			int descend, int get_all, int update, int no_ignore,
			IntPtr ctx, IntPtr pool);
			
		[DllImport(svnclientlib)] static extern IntPtr svn_client_log (
			IntPtr apr_array_header_t_targets,
			ref Rev rev_start, ref Rev rev_end,
			int discover_changed_paths,
            int strict_node_history,
            svn_log_message_receiver_t receiver,
            IntPtr receiver_baton,
            IntPtr ctx, IntPtr pool);
            
        [DllImport(svnclientlib)] static extern IntPtr svn_time_from_cstring (
        	out long aprtime, string time, IntPtr pool);
        	
		[DllImport(svnclientlib)] static extern IntPtr svn_client_url_from_path (
			ref IntPtr url, string path_or_url, IntPtr pool);

		[DllImport(svnclientlib)] static extern IntPtr svn_client_cat (
			IntPtr stream, string path_or_url,
			ref Rev revision,
			IntPtr ctx, IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_stream_create (
			IntPtr baton, IntPtr pool);
		
		//[DllImport(svnclientlib)] static extern IntPtr svn_stream_set_read (
		//	IntPtr stream, svn_readwrite_fn_t reader);

		[DllImport(svnclientlib)] static extern IntPtr svn_stream_set_write (
			IntPtr stream, svn_readwrite_fn_t writer);
			
		[DllImport(svnclientlib)] static extern IntPtr svn_client_update (
			out int result_rev,
			string path, ref Rev revision,
			int recurse, IntPtr ctx, IntPtr pool);
			
		[DllImport(svnclientlib)] static extern IntPtr svn_client_delete (
			out int result_rev, IntPtr apr_array_header_t_targets, 
			int force, IntPtr ctx, IntPtr pool);
			
		[DllImport(svnclientlib)] static extern IntPtr svn_client_add (
			string path, int recurse, IntPtr ctx, IntPtr pool);
			
		[DllImport(svnclientlib)] static extern IntPtr svn_client_commit (
			ref IntPtr svn_client_commit_info_t_commit_info,
			IntPtr apr_array_header_t_targets, int nonrecursive,
			IntPtr ctx, IntPtr pool);
			
		[DllImport(svnclientlib)] static extern IntPtr svn_client_revert (
			IntPtr apr_array_header_t_targets, int recursive,
			IntPtr ctx, IntPtr pool);
			
		[DllImport(svnclientlib)] static extern IntPtr svn_client_move(
			out int result_rev, string srcPath, ref Rev rev,
			string destPath, int force, IntPtr ctx, IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_checkout(
			out Rev revision, string url, string path, ref Rev rev, 
			int recurse, IntPtr ctx, IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_mkdir2(
			ref IntPtr commit_info, IntPtr apr_array_paths, IntPtr ctx, IntPtr pool);
			
		[DllImport(svnclientlib)]
		static extern IntPtr svn_client_diff (
			IntPtr diff_options,
			string path1,
			ref Rev revision1,
			string path2,
			ref Rev revision2,
			int recurse,
			int ignore_ancestry,
			int no_diff_deleted,
			IntPtr outfile,
			IntPtr errfile,
			IntPtr ctx,
			IntPtr pool
		);
	
	}
}
