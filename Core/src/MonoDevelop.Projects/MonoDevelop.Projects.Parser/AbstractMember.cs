// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="?" email="?"/>
//     <version value="$version"/>
// </file>

using System;

namespace MonoDevelop.Projects.Parser
{
	[Serializable]
	public abstract class AbstractMember : AbstractNamedEntity, IMember
	{
		protected IClass declaringType;
		protected IReturnType returnType;
		protected IRegion          region;
		
		public virtual IRegion Region {
			get {
				return region;
			}
		}
		
		public IClass DeclaringType {
			get {
				return declaringType;
			}
			set {
				declaringType = value;
			}
		}
		
		public IReturnType ReturnType {
			get {
				return returnType;
			}
			set {
				returnType = value;
			}
		}
	}
}
