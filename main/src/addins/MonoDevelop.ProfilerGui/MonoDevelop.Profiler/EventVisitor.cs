// 
// EventVisitor.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;

namespace MonoDevelop.Profiler
{
	public abstract class EventVisitor
	{
		public abstract Buffer CurrentBuffer {
			get;
			set;
		}
					
		public virtual object Visit (AllocEvent allocEvent)
		{
			return null;
		}
		
		public virtual object Visit (ResizeGcEvent resizeGcEvent)
		{
			return null;
		}
		
		public virtual object Visit (GcEvent gcEvent)
		{
			return null;
		}
		
		public virtual object Visit (MoveGcEvent moveGcEvent)
		{
			return null;
		}
		
		public virtual object Visit (HandleCreatedGcEvent handleCreatedGcEvent)
		{
			return null;
		}
		
		public virtual object Visit (HandleDestroyedGcEvent handleDestroyedGcEvent)
		{
			return null;
		}
		
		public virtual object Visit (MetaDataClassEvent metaDataClassEvent)
		{
			return null;
		}
		
		public virtual object Visit (MetaDataAssemblyEvent metaDataAssemblyEvent	)
		{
			return null;
		}
		
		public virtual object Visit (MetaDataDomainEvent metaDataDomainEvent)
		{
			return null;
		}
		
		public virtual object Visit (MetaDataImageEvent metaDataImageEvent)
		{
			return null;
		}
		
		public virtual object Visit (MetaDataThreadEvent metaDataThreadEvent)
		{
			return null;
		}
		
		public virtual object Visit (MethodLeaveEvent methodLeaveEvent)
		{
			return null;
		}
		
		public virtual object Visit (MethodEnterEvent methodEnterEvent)
		{
			return null;
		}
		
		public virtual object Visit (MethodExcLeaveEvent methodExcLeaveEvent)
		{
			return null;
		}
		
		public virtual object Visit (MethodJitEvent methodJitEvent)
		{
			return null;
		}
		
		public virtual object Visit (ExceptionClauseEvent exceptionClauseEvent)
		{
			return null;
		}

		public virtual object Visit (ExceptionThrowEvent exceptionThrowEvent)
		{
			return null;
		}

		public virtual object Visit (MonitiorEvent monitiorEvent)
		{
			return null;
		}

		public virtual object Visit (HeapStartEvent heapStartEvent)
		{
			return null;
		}

		public virtual object Visit (HeapEndEvent heapEndEvent)
		{
			return null;
		}

		public virtual object Visit (HeapObjectEvent heapObjectEvent)
		{
			return null;
		}

		public virtual object Visit (HeapRootEvent heapRootEvent)
		{
			return null;
		}
		
		public virtual object Visit (SampleUSymEvent sampleUSymEvent)
		{
			return null;
		}
		
		public virtual object Visit (SampleHitEvent sampleHitEvent)
		{
			return null;
		}
		
		public virtual object Visit (SampleUBinEvent sampleUBinEvent)
		{
			return null;
		}
	}
}
