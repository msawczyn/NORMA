#region Common Public License Copyright Notice
/**************************************************************************\
* Natural Object-Role Modeling Architect for Visual Studio                 *
*                                                                          *
* Copyright � Neumont University. All rights reserved.                     *
* Copyright � ORM Solutions, LLC. All rights reserved.                     *
*                                                                          *
* The use and distribution terms for this software are covered by the      *
* Common Public License 1.0 (http://opensource.org/licenses/cpl) which     *
* can be found in the file CPL.txt at the root of this distribution.       *
* By using this software in any fashion, you are agreeing to be bound by   *
* the terms of this license.                                               *
*                                                                          *
* You must not remove this notice, or any other, from this software.       *
\**************************************************************************/
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using Microsoft.VisualStudio.Modeling;
using ORMSolutions.ORMArchitect.Framework;
using System.Text;

namespace ORMSolutions.ORMArchitect.Core.ObjectModel
{
	#region IFactConstraint interface
	/// <summary>
	/// A constraint is defined such that it can have
	/// roles that span multiple fact types. The core
	/// model makes it difficult to determine which roles
	/// on a fact are used by a given constraint. FactConstraint
	/// and InternalFactConstraint relationships are generated
	/// automatically, but these have significantly different
	/// mechanisms for getting from the fact type to the constraint
	/// and its roles. The IFactConstraint interface is defined to
	/// smooth over this difference.
	/// </summary>
	public interface IFactConstraint
	{
		/// <summary>
		/// Get the constraint instance bound
		/// to the context fact
		/// </summary>
		IConstraint Constraint { get;}
		/// <summary>
		/// Get the roles associated with both the
		/// constraint and the fact.
		/// </summary>
		IList<Role> RoleCollection { get;}
		/// <summary>
		/// Get the fact type instance associated
		/// with this constraint. All roles in RoleCollection
		/// will be parented to this fact.
		/// </summary>
		FactType FactType { get;}
	}
	#endregion // IFactConstraint interface
	public partial class FactType : INamedElementDictionaryChild, INamedElementDictionaryRemoteChild, INamedElementDictionaryParentNode, IModelErrorOwner, IHasIndirectModelErrorOwner, IModelErrorDisplayContext, IVerbalizeCustomChildren, IHierarchyContextEnabled, IVerbalizeFilterChildrenByRole
	{
		#region Public token values
		/// <summary>
		/// A key to set in the top-level transaction context to indicate the role that
		/// a newly added role should be added after.
		/// </summary>
		public static readonly object InsertAfterRoleKey = new object();
		/// <summary>
		/// A key to set in the top-level transaction context to indicate the role that
		/// a newly added role should be added before.
		/// </summary>
		public static readonly object InsertBeforeRoleKey = new object();
		#endregion // Public token values
		#region ReadingOrder acquisition
		/// <summary>
		/// Gets a reading order, first by trying to find it, if one doesn't exist
		/// it will then create a new ReadingOrder. It operates under the assumption
		/// that a transaction has already been started.
		/// </summary>
		public ReadingOrder EnsureReadingOrder(IList<RoleBase> roleOrder)
		{
			ReadingOrder retVal = FindMatchingReadingOrder(roleOrder);
			if (retVal == null)
			{
				retVal = CreateReadingOrder(roleOrder);
			}
			return retVal;
		}

		/// <summary>
		/// Looks for a ReadingOrder that has the roles in the same order
		/// as the currently selected role order.
		/// </summary>
		/// <param name="roleOrder">An IList of <see cref="RoleBase"/> elements. </param>
		/// <returns>The reading order if found, null if it was not.</returns>
		/// <remarks>The requested roleOrder may have fewer roles than the FactType and
		/// may contain trailing null elements in the list. This allows a request
		/// for a partial reading order match.</remarks>
		public ReadingOrder FindMatchingReadingOrder(IList<RoleBase> roleOrder)
		{
			return FindMatchingReadingOrder(ReadingOrderCollection, roleOrder);
		}
		/// <summary>
		/// Looks for a <see cref="ReadingOrder"/> that has the roles in the same order
		/// as the currently selected role order.
		/// </summary>
		/// <param name="readingOrders">An IList of <see cref="ReadingOrder"/> elements. </param>
		/// <param name="roleOrder">An IList of <see cref="RoleBase"/> elements. </param>
		/// <returns>The reading order if found, null if it was not.</returns>
		/// <remarks>The requested roleOrder may have fewer roles than the FactType and
		/// may contain trailing null elements in the list. This allows a request
		/// for a partial reading order match.</remarks>
		public static ReadingOrder FindMatchingReadingOrder(IList<ReadingOrder> readingOrders, IList<RoleBase> roleOrder)
		{
			ReadingOrder retval = null;
			int roleOrderCount = roleOrder.Count;
			foreach (ReadingOrder order in readingOrders)
			{
				LinkedElementCollection<RoleBase> roles = order.RoleCollection;
				if (roles.Count >= roleOrderCount)
				{
					bool match = true;
					for (int i = 0; i < roleOrderCount; ++i)
					{
						RoleBase expectedRole = roleOrder[i];
						if (expectedRole == null)
						{
							if (i == 0)
							{
								// Nothing to match
								return null;
							}
							break;
						}
						if (roles[i] != expectedRole)
						{
							match = false;
							break;
						}
					}
					if (match)
					{
						retval = order;
						break;
					}
				}
			}
			return retval;
		}
		/// <summary>
		/// Creates a new ReadingOrder with the same role sequence as the currently selected one.
		/// A transaction should have been pushed before calling this method. It operates under
		/// the assumption that a transaction has already been started.
		/// </summary>
		/// <returns>Should always return a value unless there was an error creating the ReadingOrder</returns>
		private ReadingOrder CreateReadingOrder(IList<RoleBase> roleOrder)
		{
			ReadingOrder retval = null;
			if (roleOrder.Count > 0)
			{
				retval = new ReadingOrder(Partition);
				LinkedElementCollection<RoleBase> readingRoles = retval.RoleCollection;
				Role unaryRole;
				if (null != (unaryRole = UnaryRole))
				{
					readingRoles.Add(unaryRole);
				}
				else
				{
					int numRoles = roleOrder.Count;
					for (int i = 0; i < numRoles; ++i)
					{
						readingRoles.Add(roleOrder[i]);
					}
				}
				ReadingOrderCollection.Add(retval);
			}
			return retval;
		}
		#endregion
		#region FactType Specific
		/// <summary>
		/// Get a collection of all constraints associated with this fact,
		/// along with the roles on this fact that are used by each fact
		/// constraint.
		/// </summary>
		/// <value></value>
		public ICollection<IFactConstraint> FactConstraintCollection
		{
			get
			{
				return new FactConstraintCollectionImpl(this);
			}
		}
		/// <summary>
		/// Get an enumeration of constraints of the given type
		/// </summary>
		/// <param name="filterType">The type of constraint to return</param>
		/// <returns>IEnumerable</returns>
		public IEnumerable<SetConstraint> GetInternalConstraints(ConstraintType filterType)
		{
			switch (filterType)
			{
				case ConstraintType.InternalUniqueness:
				case ConstraintType.SimpleMandatory:
					IList constraints = SetConstraintCollection;
					int constraintCount = constraints.Count;
					for (int i = 0; i < constraintCount; ++i)
					{
						IConstraint ic = (IConstraint)constraints[i];
						if (ic.ConstraintType == filterType)
						{
							yield return (SetConstraint)ic;
						}
					}
					break;
			}
		}
		/// <summary>
		/// Get an enumeration of constraints of the given type using generics
		/// </summary>
		/// <typeparam name="T">An internal constraint type</typeparam>
		/// <returns>IEnumerable</returns>
		public IEnumerable<T> GetInternalConstraints<T>() where T : SetConstraint, IConstraint
		{
			LinkedElementCollection<SetConstraint> constraints = SetConstraintCollection;
			int constraintCount = constraints.Count;
			for (int i = 0; i < constraintCount; ++i)
			{
				T ic = constraints[i] as T;
				if (ic != null && ic.ConstraintIsInternal)
				{
					yield return ic;
				}
			}
		}
		/// <summary>
		/// Get the number of internal constraints of the specified constraint type
		/// </summary>
		/// <param name="filterType">The type of constraint to count</param>
		/// <returns>int</returns>
		public int GetInternalConstraintsCount(ConstraintType filterType)
		{
			int retVal = 0;
			// Count the enumerator without foreach to satisfy FxCop
			IEnumerator<SetConstraint> ienum = GetInternalConstraints(filterType).GetEnumerator();
			while (ienum.MoveNext())
			{
				++retVal;
			}
			return retVal;
		}
		/// <summary>
		/// Return the Objectification relationship that
		/// attaches this fact to its nesting type
		/// </summary>
		public Objectification Objectification
		{
			get
			{
				return Objectification.GetLinkToNestingType(this);
			}
		}
		/// <summary>
		/// Return roles in the order of the first reading order if available, or the
		/// <see cref="RoleCollection"/> if readings are not specified.
		/// </summary>
		public IList<RoleBase> OrderedRoleCollection
		{
			get
			{
				// Note that this needs to be kept in sync with the InitializeDefaultFactRoles template
				// in VerbalizationGenerator.xslt, which generates an inline form of this property
				LinkedElementCollection<ReadingOrder> orders = ReadingOrderCollection;
				if (orders.Count != 0)
				{
					return orders[0].RoleCollection;
				}
				else
				{
					LinkedElementCollection<RoleBase> roles = RoleCollection;
					int? unaryRoleIndex = GetUnaryRoleIndex(roles);
					return (unaryRoleIndex.HasValue) ?
						(IList<RoleBase>)new RoleBase[] { roles[unaryRoleIndex.Value] } :
						roles;
				}
			}
		}
		#endregion // FactType Specific
		#region FactConstraintCollection implementation
		private sealed class FactConstraintCollectionImpl : ICollection<IFactConstraint>
		{
			#region Member Variables
			private readonly ReadOnlyCollection<ElementLink>[] myLists;
			#endregion // Member Variables
			#region Constructors
			/// <summary>
			/// Create a FactConstraint collection for the given fact type. Fact constraints
			/// come from multiple links, this puts them all together.
			/// </summary>
			/// <param name="factType">The parent fact type</param>
			public FactConstraintCollectionImpl(FactType factType)
			{
				myLists = new ReadOnlyCollection<ElementLink>[]{
					DomainRoleInfo.GetElementLinks<ElementLink>(factType, FactSetConstraint.FactTypeDomainRoleId),
					DomainRoleInfo.GetElementLinks<ElementLink>(factType, FactSetComparisonConstraint.FactTypeDomainRoleId)};
			}
			#endregion // Constructors
			#region ICollection<IFactConstraint> Implementation
			bool ICollection<IFactConstraint>.Contains(IFactConstraint item)
			{
				ElementLink link = item as ElementLink;
				if (link != null)
				{
					ReadOnlyCollection<ElementLink>[] lists = myLists;
					for (int i = 0; i < lists.Length; ++i)
					{
						if (lists[i].Contains(link))
						{
							return true;
						}
					}
				}
				return false;
			}
			void ICollection<IFactConstraint>.CopyTo(IFactConstraint[] array, int arrayIndex)
			{
				ReadOnlyCollection<ElementLink>[] lists = myLists;
				int prevTotal = 0;
				for (int i = 0; i < lists.Length; ++i)
				{
					ReadOnlyCollection<ElementLink> curList = lists[i];
					int curTotal = curList.Count;
					if (curTotal != 0)
					{
						((IList)curList).CopyTo(array, prevTotal);
						prevTotal += curTotal;
					}
				}
			}
			int ICollection<IFactConstraint>.Count
			{
				get
				{
					ReadOnlyCollection<ElementLink>[] lists = myLists;
					int total = 0;
					for (int i = 0; i < lists.Length; ++i)
					{
						total += lists[i].Count;
					}
					return total;
				}
			}
			bool ICollection<IFactConstraint>.IsReadOnly
			{
				get { return true; }
			}
			bool ICollection<IFactConstraint>.Remove(IFactConstraint item)
			{
				// Not supported for read-only
				throw new NotSupportedException();
			}
			void ICollection<IFactConstraint>.Add(IFactConstraint item)
			{
				// Not supported for read-only
				throw new NotSupportedException();
			}
			void ICollection<IFactConstraint>.Clear()
			{
				// Not supported for read-only
				throw new NotSupportedException();
			}
			#endregion // ICollection<IFactConstraint> Implementation
			#region IEnumerable<IFactConstraint> Implementation
			IEnumerator<IFactConstraint> IEnumerable<IFactConstraint>.GetEnumerator()
			{
				ReadOnlyCollection<ElementLink>[] lists = myLists;
				for (int i = 0; i < lists.Length; ++i)
				{
					ReadOnlyCollection<ElementLink> curList = lists[i];
					int curTotal = curList.Count;
					for (int j = 0; j < curTotal; ++j)
					{
						yield return (IFactConstraint)curList[j];
					}
				}
			}
			IEnumerator IEnumerable.GetEnumerator()
			{
				return (this as IEnumerable<IFactConstraint>).GetEnumerator();
			}
			#endregion // IEnumerable<IFactConstraint> Implementation
		}
		#endregion // FactConstraintCollection implementation
		#region Customize property display
		/// <summary>
		/// Return a simple name instead of a name decorated with the type (the
		/// default for a ModelElement). This is the easiest way to display
		/// clean names in the property grid when we reference properties.
		/// </summary>
		public override string ToString()
		{
			return Name;
		}
		#endregion // Customize property display
		#region MergeContext functions
		/// <summary>
		/// Support adding root elements and constraints directly to the design surface
		/// </summary>
		/// <param name="rootElement">The element to add.</param>
		/// <param name="elementGroupPrototype">The object representing the serialized data being added to this <see cref="FactType"/>.</param>
		/// <returns><see langword="true"/> if addition is allowed; otherwise, <see langword="false"/>.</returns>
		protected override bool CanMerge(ProtoElementBase rootElement, ElementGroupPrototype elementGroupPrototype)
		{
			if (rootElement != null)
			{
				DomainClassInfo classInfo = Store.DomainDataDirectory.FindDomainClass(rootElement.DomainClassId);
				if (classInfo.IsDerivedFrom(UniquenessConstraint.DomainClassId))
				{
					return ORMModel.InternalUniquenessConstraintUserDataKey.Equals(elementGroupPrototype.UserData as string) && this.ImpliedByObjectification == null;
				}
			}
			return false;
		}

		/// <summary>
		/// Attach a deserialized InternalUniquenessConstraint to this FactType.
		/// Called after prototypes for these items are dropped onto the diagram
		/// from the toolbox.
		/// </summary>
		/// <param name="sourceElement">The element being added</param>
		/// <param name="elementGroup">The element describing all of the created elements</param>
		protected override void MergeRelate(ModelElement sourceElement, ElementGroup elementGroup)
		{
			base.MergeRelate(sourceElement, elementGroup);
			UniquenessConstraint internalConstraint;
			if (null != (internalConstraint = sourceElement as UniquenessConstraint))
			{
				if (internalConstraint.IsInternal)
				{
					internalConstraint.FactTypeCollection.Add(this);
				}
			}
		}
		#endregion // MergeContext functions
		#region CustomStorage handlers
		private string GetDerivationNoteDisplayValue()
		{
			FactTypeDerivationRule derivationRule;
			DerivationNote derivationNote;
			return (null != (derivationRule = DerivationRule as FactTypeDerivationRule) && null != (derivationNote = derivationRule.DerivationNote)) ? derivationNote.Body : String.Empty;
		}
		private void SetDerivationNoteDisplayValue(string newValue)
		{
			Partition partition = Partition;
			if (!Store.InUndoRedoOrRollback)
			{
				FactTypeDerivationRule derivationRule;
				DerivationNote derivationNote;
				if (null != (derivationRule = DerivationRule as FactTypeDerivationRule))
				{
					derivationNote = derivationRule.DerivationNote;
					if (derivationNote == null && string.IsNullOrEmpty(newValue))
					{
						return;
					}
				}
				else if (string.IsNullOrEmpty(newValue))
				{
					return; // Don't create a new rule for an empty note body
				}
				else
				{
					new FactTypeHasDerivationRule(
						this,
						derivationRule = new FactTypeDerivationRule(
							partition,
							new PropertyAssignment(FactTypeDerivationRule.ExternalDerivationDomainPropertyId, true)));
					derivationNote = null;
				}
				if (derivationNote == null)
				{
					new FactTypeDerivationRuleHasDerivationNote(
						derivationRule,
						new DerivationNote(
							partition,
							new PropertyAssignment(DerivationNote.BodyDomainPropertyId, newValue)));
				}
				else
				{
					derivationNote.Body = newValue;
				}
			}
		}
		private DerivationExpressionStorageType GetDerivationStorageDisplayValue()
		{
			FactTypeDerivationRule rule;
			if (null != (rule = DerivationRule as FactTypeDerivationRule))
			{
				switch (rule.DerivationCompleteness)
				{
					case DerivationCompleteness.FullyDerived:
						return (rule.DerivationStorage == DerivationStorage.Stored) ? DerivationExpressionStorageType.DerivedAndStored : DerivationExpressionStorageType.Derived;
					case DerivationCompleteness.PartiallyDerived:
						return (rule.DerivationStorage == DerivationStorage.Stored) ? DerivationExpressionStorageType.PartiallyDerivedAndStored : DerivationExpressionStorageType.PartiallyDerived;
				}
			}
			return DerivationExpressionStorageType.Derived;
		}
		private void SetDerivationStorageDisplayValue(DerivationExpressionStorageType newValue)
		{
			if (!Store.InUndoRedoOrRollback)
			{
				FactTypeDerivationRule rule;
				if (null != (rule = DerivationRule as FactTypeDerivationRule))
				{
					DerivationCompleteness completeness = DerivationCompleteness.FullyDerived;
					DerivationStorage storage = DerivationStorage.NotStored;
					switch (newValue)
					{
						//case DerivationExpressionStorageType.Derived:
						case DerivationExpressionStorageType.DerivedAndStored:
							storage = DerivationStorage.Stored;
							break;
						case DerivationExpressionStorageType.PartiallyDerived:
							completeness = DerivationCompleteness.PartiallyDerived;
							break;
						case DerivationExpressionStorageType.PartiallyDerivedAndStored:
							completeness = DerivationCompleteness.PartiallyDerived;
							storage = DerivationStorage.Stored;
							break;
					}
					rule.DerivationCompleteness = completeness;
					rule.DerivationStorage = storage;
				}
			}
		}
		private string GetDefinitionTextValue()
		{
			Definition currentDefinition = Definition;
			return (currentDefinition != null) ? currentDefinition.Text : String.Empty;
		}
		private void SetDefinitionTextValue(string newValue)
		{
			if (!Store.InUndoRedoOrRollback)
			{
				Definition definition = Definition;
				if (definition != null)
				{
					definition.Text = newValue;
				}
				else if (!string.IsNullOrEmpty(newValue))
				{
					Definition = new Definition(Partition, new PropertyAssignment(Definition.TextDomainPropertyId, newValue));
				}
			}
		}
		private string GetNoteTextValue()
		{
			Note currentNote = Note;
			return (currentNote != null) ? currentNote.Text : String.Empty;
		}
		private void SetNoteTextValue(string newValue)
		{
			if (!Store.InUndoRedoOrRollback)
			{
				Note note = Note;
				if (note != null)
				{
					note.Text = newValue;
				}
				else if (!string.IsNullOrEmpty(newValue))
				{
					Note = new Note(Partition, new PropertyAssignment(Note.TextDomainPropertyId, newValue));
				}
			}
		}
		private string GetNameValue()
		{
			return GetGeneratedNameValue(true);
		}
		private void SetNameValue(string newValue)
		{
			// Handled by FactTypeNameChangeRule to verify that this
			// is set directly instead instead of by a rule.
		}
		private static RuntimeMethodHandle myNameSetValueMethodHandle;
		private static RuntimeMethodHandle NameSetValueMethodHandle
		{
			get
			{
				RuntimeMethodHandle retVal = myNameSetValueMethodHandle;
				if (retVal.Value == IntPtr.Zero)
				{
					myNameSetValueMethodHandle = retVal = typeof(NamePropertyHandler).GetMethod("SetValue").MethodHandle;
				}
				return retVal;
			}
		}
		private static RuntimeMethodHandle myGeneratedNameSetValueMethodHandle;
		private static RuntimeMethodHandle GeneratedNameSetValueMethodHandle
		{
			get
			{
				RuntimeMethodHandle retVal = myGeneratedNameSetValueMethodHandle;
				if (retVal.Value == IntPtr.Zero)
				{
					myGeneratedNameSetValueMethodHandle = retVal = typeof(GeneratedNamePropertyHandler).GetMethod("SetValue").MethodHandle;
				}
				return retVal;
			}
		}
		private string GetGeneratedNameValue()
		{
			return GetGeneratedNameValue(false);
		}
		private void SetGeneratedNameValue(string newValue)
		{
			Debug.Assert(Store.InUndoRedoOrRollback || (Store.TransactionActive && Store.TransactionManager.CurrentTransaction.TopLevelTransaction.Context.ContextInfo.ContainsKey(ElementGroupPrototype.CreatingKey)), "Call GeneratedNamePropertyHandler.SetGeneratedName directly to modify myGeneratedName field.");
			if (Store.InUndoRedoOrRollback)
			{
				// We only set this in undo/redo scenarios so that the initial
				// change on a writable property comes indirectly from the objectifying
				// type changing its name.
				myGeneratedName = newValue;
			}
		}
		private string GetGeneratedNameValue(bool forGetNameValue)
		{
			ObjectType nestingType;
			FactTypeDerivationRule derivationRule;
			Store store = Utility.ValidateStore(Store);
			if (store != null &&
				// This is a very tricky operation, resulting in the unconventional
				// stack frame check. During an Undo/Redo, when 'GetValue' is called
				// from 'SetValue' then there should be no side effects. All of these
				// SetValue calls are made during the store-internal Undo and Redo
				// methods on the ChangeElementCommand class. However, other requests
				// will also naturally occur during the 'NotifyObservers' phase of the
				// command playback sequence. SetValue cannot be called during this event
				// notification phase, which needs to be treated as a normal request
				// with on-demand generation of the name.
				(store.InUndoRedoOrRollback && (new StackFrame(3, false).GetMethod().MethodHandle == (forGetNameValue ? NameSetValueMethodHandle : GeneratedNameSetValueMethodHandle))))
			{
				return myGeneratedName;
			}
			else if (null != (nestingType = NestingType))
			{
				return nestingType.Name;
			}
			else if (null != (derivationRule = DerivationRule as FactTypeDerivationRule) && derivationRule.DerivationCompleteness == DerivationCompleteness.FullyDerived && !derivationRule.ExternalDerivation)
			{
				return derivationRule.Name;
			}
			else if (!store.TransactionManager.InTransaction)
			{
				string generatedName = myGeneratedName;
				return String.IsNullOrEmpty(generatedName) ? myGeneratedName = GenerateName(false) : generatedName;
			}
			else
			{
				string generatedName = myGeneratedName;
				if (string.IsNullOrEmpty(generatedName) && !IsDeleting && !IsDeleted)
				{
					generatedName = GenerateName(false);
					if (!string.IsNullOrEmpty(generatedName))
					{
						GeneratedNamePropertyHandler.SetGeneratedName(this, "", generatedName);
					}
				}
				return generatedName;
			}
		}
		private void OnFactTypeNameChanged()
		{
			TransactionManager tmgr = Store.TransactionManager;
			if (tmgr.InTransaction)
			{
				NameChanged = tmgr.CurrentTransaction.SequenceNumber;
			}
		}
		private long GetNameChangedValue()
		{
			TransactionManager tmgr = Store.TransactionManager;
			if (tmgr.InTransaction)
			{
				// Subtract 1 so that we get a difference in the transaction log
				return unchecked(tmgr.CurrentTransaction.SequenceNumber - 1);
			}
			else
			{
				return 0L;
			}
		}
		private void SetNameChangedValue(long newValue)
		{
			// Nothing to do, we're just trying to create a transaction log entry
		}
		#endregion // CustomStorage handlers
		#region INamedElementDictionaryChild implementation
		void INamedElementDictionaryChild.GetRoleGuids(out Guid parentDomainRoleId, out Guid childDomainRoleId)
		{
			GetRoleGuids(out parentDomainRoleId, out childDomainRoleId);
		}
		/// <summary>
		/// Implementation of INamedElementDictionaryChild.GetRoleGuids. Identifies
		/// this child as participating in the 'ModelHasFactType' naming set.
		/// </summary>
		/// <param name="parentDomainRoleId">Guid</param>
		/// <param name="childDomainRoleId">Guid</param>
		protected static void GetRoleGuids(out Guid parentDomainRoleId, out Guid childDomainRoleId)
		{
			parentDomainRoleId = ModelHasFactType.ModelDomainRoleId;
			childDomainRoleId = ModelHasFactType.FactTypeDomainRoleId;
		}
		#endregion // INamedElementDictionaryChild implementation
		#region INamedElementDictionaryRemoteChild implementation
		private static readonly Guid[] myRemoteNamedElementDictionaryChildRoles = new Guid[] { FactTypeHasRole.FactTypeDomainRoleId, FactTypeHasReadingOrder.FactTypeDomainRoleId };
		/// <summary>
		/// Implements <see cref="INamedElementDictionaryRemoteChild.GetNamedElementDictionaryChildRoles"/>. Identifies
		/// this as a remote parent for the 'ModelHasConstraint' naming set and the duplicate reading set of names.
		/// </summary>
		/// <returns>Guid for the FactTypeHasRole.FactType and the FactTypeHasReadingOrder.FactType role</returns>
		protected static Guid[] GetNamedElementDictionaryChildRoles()
		{
			return myRemoteNamedElementDictionaryChildRoles;
		}
		Guid[] INamedElementDictionaryRemoteChild.GetNamedElementDictionaryChildRoles()
		{
			return GetNamedElementDictionaryChildRoles();
		}
		/// <summary>
		/// Implements <see cref="INamedElementDictionaryRemoteChild.NamedElementDictionaryParentRole"/>
		/// </summary>
		protected static Guid NamedElementDictionaryParentRole
		{
			get
			{
				return ModelHasFactType.FactTypeDomainRoleId;
			}
		}
		Guid INamedElementDictionaryRemoteChild.NamedElementDictionaryParentRole
		{
			get
			{
				return NamedElementDictionaryParentRole;
			}
		}
		#endregion // INamedElementDictionaryRemoteChild implementation
		#region FactTypeNameChangeRule
		/// <summary>
		/// ChangeRule: typeof(FactType)
		/// </summary>
		private static void FactTypeNameChangeRule(ElementPropertyChangedEventArgs e)
		{
			if (e.DomainProperty.Id == FactType.NameDomainPropertyId)
			{
				Debug.Assert(e.ChangeSource == ChangeSource.Normal, "The FactType.Name property should not be set directly from a rule. FactType and its nested class should set the GeneratedName property instead.");
				if (e.ChangeSource == ChangeSource.Normal) // Ignore changes from rules and other sources
				{
					FactType factType = (FactType)e.ModelElement;
					ObjectType objectifyingType;
					FactTypeDerivationRule derivationRule;
					if (null != (objectifyingType = factType.NestingType))
					{
						objectifyingType.Name = (string)e.NewValue;
					}
					else if (null != (derivationRule = factType.DerivationRule as FactTypeDerivationRule) &&
						derivationRule.DerivationCompleteness == DerivationCompleteness.FullyDerived &&
						!derivationRule.ExternalDerivation)
					{
						derivationRule.Name = (string)e.NewValue;
					}
				}
			}
		}
		#endregion // FactTypeNameChangeRule
		#region IModelErrorOwner Implementation
		/// <summary>
		/// Returns the error associated with the fact.
		/// </summary>
		protected new IEnumerable<ModelErrorUsage> GetErrorCollection(ModelErrorUses filter)
		{
			ModelErrorUses startFilter = filter;
			if (filter == 0)
			{
				filter = (ModelErrorUses)(-1);
			}
			if (0 != (filter & (ModelErrorUses.BlockVerbalization | ModelErrorUses.DisplayPrimary)))
			{
				FactTypeRequiresReadingError noReadingError = this.ReadingRequiredError;
				if (noReadingError != null)
				{
					yield return new ModelErrorUsage(noReadingError, ModelErrorUses.BlockVerbalization);
				}
			}

			if (0 != (filter & (ModelErrorUses.Verbalize | ModelErrorUses.DisplayPrimary)))
			{
				FactTypeRequiresInternalUniquenessConstraintError noUniquenessError = this.InternalUniquenessConstraintRequiredError;
				if (noUniquenessError != null)
				{
					yield return noUniquenessError;
				}
				// Any duplicate constraint will trigger this. It is possible for multiple duplicated
				// constraints to exist on the fact type, but only one error is shown per fact type.
				ImpliedInternalUniquenessConstraintError dupConstraint = this.ImpliedInternalUniquenessConstraintError;
				if (dupConstraint != null)
				{
					yield return dupConstraint;
				}
			}

			if (0 == (filter & (ModelErrorUses.Verbalize | ModelErrorUses.BlockVerbalization | ModelErrorUses.DisplayPrimary)) || filter == (ModelErrorUses)(-1))
			{
				// NMinusOneError is parented off InternalConstraint. The constraint has a name,
				// but the name is often arbitrary. Including the fact name as well makes the
				// error message much more meaningful.
				foreach (UniquenessConstraint ic in GetInternalConstraints<UniquenessConstraint>())
				{
					NMinusOneError nMinusOneError = ic.NMinusOneError;
					if (nMinusOneError != null)
					{
						yield return new ModelErrorUsage(nMinusOneError, ModelErrorUses.None);
					}
				}
			}
			LinkedElementCollection<RoleBase> roles = null;
			if (filter == (ModelErrorUses)(-1))
			{
				// Show the fact type as an owner of the role errors as well
				// so the fact can be accurately named in the error text. However,
				// we do not validate this error on the fact type, it is done on the role.
				foreach (RoleBase roleBase in (roles = RoleCollection))
				{
					Role role = roleBase as Role;
					if (role != null)
					{
						foreach (ModelErrorUsage roleError in (role as IModelErrorOwner).GetErrorCollection(startFilter))
						{
							yield return new ModelErrorUsage(roleError, ModelErrorUses.None);
						}
						IModelErrorOwner valueErrors = role.ValueConstraint as IModelErrorOwner;
						if (valueErrors != null)
						{
							foreach (ModelErrorUsage valueError in valueErrors.GetErrorCollection(startFilter))
							{
								yield return new ModelErrorUsage(valueError, ModelErrorUses.None);
							}
						}
					}
				}
			}
			if (0 != (filter & (ModelErrorUses.BlockVerbalization | ModelErrorUses.Verbalize | ModelErrorUses.DisplayPrimary)))
			{
				foreach (ReadingOrder readingOrder in ReadingOrderCollection)
				{
					foreach (Reading reading in readingOrder.ReadingCollection)
					{
						foreach (ModelErrorUsage readingErrorUsage in (reading as IModelErrorOwner).GetErrorCollection(startFilter))
						{
							if (0 != (readingErrorUsage.UseFor & filter))
							{
								yield return readingErrorUsage;
							}
						}
					}
				}
			}
			if (filter == ModelErrorUses.DisplayPrimary || filter == ModelErrorUses.Verbalize)
			{
				// If we're objectified, list primary errors from the objectifying type
				// here as well. Note that we should verbalize anything we list in our
				// validation errors
				Objectification objectification;
				if (null != (objectification = Objectification))
				{
					// Always ask for 'DisplayPrimary', even if we're verbalizing
					// None of these should list as blocking verbalization here, even if they're blocking on the nesting
					foreach (ModelError nestingError in (objectification.NestingType as IModelErrorOwner).GetErrorCollection(ModelErrorUses.DisplayPrimary))
					{
						yield return new ModelErrorUsage(nestingError, ModelErrorUses.Verbalize | ModelErrorUses.DisplayPrimary);
					}
					foreach (FactType linkFactType in objectification.ImpliedFactTypeCollection)
					{
						foreach (ModelError nestingError in (linkFactType as IModelErrorOwner).GetErrorCollection(ModelErrorUses.DisplayPrimary))
						{
							yield return new ModelErrorUsage(nestingError, ModelErrorUses.Verbalize | ModelErrorUses.DisplayPrimary);
						}
					}
				}
			}

			if (0 != (filter & (ModelErrorUses.DisplayPrimary | ModelErrorUses.Verbalize)))
			{
				LinkedElementCollection<FactTypeInstance> factTypeInstances = this.FactTypeInstanceCollection;
				int factTypeInstanceCount = factTypeInstances.Count;
				for (int i = 0; i < factTypeInstanceCount; ++i)
				{
					FactTypeInstance factTypeInstance = factTypeInstances[i];
					foreach (ModelErrorUsage usage in (factTypeInstance as IModelErrorOwner).GetErrorCollection(startFilter))
					{
						yield return usage;
					}
				}

				RoleProjectedDerivationRule derivationRule = DerivationRule;
				if (derivationRule != null)
				{
					foreach (ModelErrorUsage derivationError in ((IModelErrorOwner)derivationRule).GetErrorCollection(startFilter))
					{
						yield return derivationError;
					}
				}
			}

			// Get errors off the base
			foreach (ModelErrorUsage baseError in base.GetErrorCollection(startFilter))
			{
				yield return baseError;
			}
		}
		IEnumerable<ModelErrorUsage> IModelErrorOwner.GetErrorCollection(ModelErrorUses filter)
		{
			return GetErrorCollection(filter);
		}

		/// <summary>
		/// Implements IModelErrorOwner.ValidateErrors
		/// </summary>
		protected new void ValidateErrors(INotifyElementAdded notifyAdded)
		{
			// Calls added here need corresponding delayed calls in DelayValidateErrors
			ValidateRequiresReading(notifyAdded);
			ValidateRequiresInternalUniqueness(notifyAdded);
			ValidateImpliedInternalUniqueness(notifyAdded);
		}
		void IModelErrorOwner.ValidateErrors(INotifyElementAdded notifyAdded)
		{
			ValidateErrors(notifyAdded);
		}
		/// <summary>
		/// Implements IModelErrorOwner.DelayValidateErrors
		/// </summary>
		protected new void DelayValidateErrors()
		{
			FrameworkDomainModel.DelayValidateElement(this, DelayValidateFactTypeRequiresReadingError);
			FrameworkDomainModel.DelayValidateElement(this, DelayValidateFactTypeRequiresInternalUniquenessConstraintError);
			FrameworkDomainModel.DelayValidateElement(this, DelayValidateImpliedInternalUniquenessConstraintError);
		}
		void IModelErrorOwner.DelayValidateErrors()
		{
			DelayValidateErrors();
		}
		#endregion // IModelErrorOwner Implementation
		#region IHasIndirectModelErrorOwner Implementation
		private static Guid[] myIndirectModelErrorOwnerLinkRoles;
		/// <summary>
		/// Implements <see cref="IHasIndirectModelErrorOwner.GetIndirectModelErrorOwnerLinkRoles"/>
		/// </summary>
		protected static Guid[] GetIndirectModelErrorOwnerLinkRoles()
		{
			// Creating a static readonly guid array is causing static field initialization
			// ordering issues with the partial classes. Defer initialization.
			Guid[] linkRoles = myIndirectModelErrorOwnerLinkRoles;
			if (linkRoles == null)
			{
				// Show link fact type reading errors on the owning fact type.
				// This passes onto the Objectification class, which maps towards
				// the fact type.
				myIndirectModelErrorOwnerLinkRoles = linkRoles = new Guid[] {
					ObjectificationImpliesFactType.ImpliedFactTypeDomainRoleId };
				// Note that this method is hidden by SubQuery.GetIndirectModelErrorOwnerLinkRoles
				// because a subquery is never an implied fact type. Check other owner list
				// if this one changes.
			}
			return linkRoles;
		}
		Guid[] IHasIndirectModelErrorOwner.GetIndirectModelErrorOwnerLinkRoles()
		{
			return GetIndirectModelErrorOwnerLinkRoles();
		}
		#endregion // IHasIndirectModelErrorOwner Implementation
		#region IModelErrorDisplayContext Implementation
		/// <summary>
		/// Implements <see cref="IModelErrorDisplayContext.ErrorDisplayContext"/>
		/// </summary>
		protected string ErrorDisplayContext
		{
			get
			{
				ORMModel model = ResolvedModel;
				return string.Format(CultureInfo.CurrentCulture, ResourceStrings.ModelErrorDisplayContextFactType, Name, model != null ? model.Name : "");
			}
		}
		string IModelErrorDisplayContext.ErrorDisplayContext
		{
			get
			{
				return ErrorDisplayContext;
			}
		}
		#endregion // IModelErrorDisplayContext Implementation
		#region Virtual methods for implicit reading support
		/// <summary>
		/// Allow fact types to have implicit readings without using the ReadingCollection
		/// </summary>
		public virtual bool HasImplicitReadings
		{
			get
			{
				return false;
			}
		}
		/// <summary>
		/// Generate the implicit name for this FactType. Called only
		/// if the <see cref="HasImplicitReadings"/> property returns
		/// <see langword="true"/>
		/// </summary>
		/// <returns>An empty string</returns>
		protected virtual string GenerateImplicitName()
		{
			Debug.Fail("GenerateImplicitName must be overridden if HasImplicitReadings is overridden to return true.");
			return "";
		}
		/// <summary>
		/// Generate an implicit reading for the specified lead role. Called only
		/// if the <see cref="HasImplicitReadings"/> property returns <see langword="true"/>
		/// </summary>
		/// <param name="leadRole">The role that should begin the reading</param>
		/// <returns><see cref="IReading"/></returns>
		protected virtual IReading GetImplicitReading(RoleBase leadRole)
		{
			Debug.Fail("GetImplicitReading must be overridden if HasImplicitReadings is overridden to return true.");
			return null;
		}
		/// <summary>
		/// Generate the default implicit reading for this FactType. Called only
		/// if the <see cref="HasImplicitReadings"/> property returns <see langword="true"/>
		/// </summary>
		/// <returns><see cref="IReading"/></returns>
		protected virtual IReading GetDefaultImplicitReading()
		{
			Debug.Fail("GetDefaultImplicitReading must be overridden if HasImplicitReadings is overridden to return true.");
			return null;
		}
		#region ImplicitReading class
		/// <summary>
		/// A helper class used to add implicit readings. Can be used to
		/// implement <see cref="GetImplicitReading"/>.
		/// </summary>
		protected sealed class ImplicitReading : IReading
		{
			#region Member Variables
			private string myReadingText;
			private IList<RoleBase> myRoleCollection;
			#endregion // Member Variables
			#region Constructors
			/// <summary>
			/// Create a new implicit reading
			/// </summary>
			/// <param name="readingText">The reading format string</param>
			/// <param name="roleOrder">The role order to use.</param>
			public ImplicitReading(string readingText, IList<RoleBase> roleOrder)
			{
				myRoleCollection = roleOrder;
				myReadingText = readingText;
			}
			#endregion // Constructors
			#region IReading Implementation
			string IReading.Text
			{
				get
				{
					return myReadingText;
				}
			}

			IList<RoleBase> IReading.RoleCollection
			{
				get
				{
					return myRoleCollection;
				}
			}
			bool IReading.IsEditable
			{
				get
				{
					return false;
				}
			}
			bool IReading.IsDefault
			{
				get
				{
					return true;
				}
			}
			#endregion //IReading Implementation
		}
		#endregion // ImplicitReading class
		#endregion // Virtual methods for implicit reading support
		#region Validation Methods
		/// <summary>
		/// Validator callback for FactTypeRequiresReadingError
		/// </summary>
		private static void DelayValidateFactTypeRequiresReadingError(ModelElement element)
		{
			(element as FactType).ValidateRequiresReading(null);
		}
		private void ValidateRequiresReading(INotifyElementAdded notifyAdded)
		{
			if (!IsDeleted)
			{
				// UNDONE: On the next format change these errors should not
				// be allowed to load or the readings to be created, so we don't have
				// to look for readings or the reading required error if HasImplicitReadings is true.
				bool hasError = !HasImplicitReadings && !(this is QueryBase);
				if (hasError)
				{
					LinkedElementCollection<ReadingOrder> readingOrders = ReadingOrderCollection;
					if (readingOrders.Count > 0)
					{
						foreach (ReadingOrder order in readingOrders)
						{
							if (order.ReadingCollection.Count > 0)
							{
								hasError = false;
								break;
							}
						}
					}
				}

				FactTypeRequiresReadingError noReadingError = ReadingRequiredError;
				if (hasError)
				{
					if (noReadingError == null)
					{
						noReadingError = new FactTypeRequiresReadingError(Partition);
						noReadingError.FactType = this;
						noReadingError.Model = ResolvedModel;
						noReadingError.GenerateErrorText();
						if (notifyAdded != null)
						{
							notifyAdded.ElementAdded(noReadingError, true);
						}
					}
				}
				else
				{
					if (noReadingError != null)
					{
						noReadingError.Delete();
					}
				}
			}
		}
		/// <summary>
		/// Validator callback for FactTypeRequiresInternalUniquenessConstraintError
		/// </summary>
		private static void DelayValidateFactTypeRequiresInternalUniquenessConstraintError(ModelElement element)
		{
			(element as FactType).ValidateRequiresInternalUniqueness(null);
		}
		private void ValidateRequiresInternalUniqueness(INotifyElementAdded notifyAdded)
		{
			ORMModel model;
			IHasAlternateOwner<FactType> toAlternateOwner;
			IAlternateElementOwner<FactType> alternateOwner;
			if (!IsDeleted &&
				!(this is QueryBase) &&
				(null == (toAlternateOwner = this as IHasAlternateOwner<FactType>) ||
				null == (alternateOwner = toAlternateOwner.AlternateOwner) ||
				alternateOwner.ValidateErrorFor(this, typeof(FactTypeRequiresInternalUniquenessConstraintError))) &&
				(null != (model = ResolvedModel)))
			{
				FactTypeDerivationRule derivationRule;
				bool hasError = RoleCollection.Count > 1 &&
					(null == (derivationRule = DerivationRule as FactTypeDerivationRule) || derivationRule.DerivationCompleteness != DerivationCompleteness.FullyDerived || derivationRule.ExternalDerivation);

				if (hasError)
				{
					foreach (UniquenessConstraint iuc in GetInternalConstraints<UniquenessConstraint>())
					{
						if (iuc.Modality == ConstraintModality.Alethic && iuc.RoleCollection.Count != 0)
						{
							hasError = false;
							break;
						}
					}
				}

				FactTypeRequiresInternalUniquenessConstraintError noUniquenessError = InternalUniquenessConstraintRequiredError;

				if (hasError)
				{
					if (noUniquenessError == null)
					{
						noUniquenessError = new FactTypeRequiresInternalUniquenessConstraintError(Partition);
						noUniquenessError.FactType = this;
						noUniquenessError.Model = model;
						noUniquenessError.GenerateErrorText();
						if (notifyAdded != null)
						{
							notifyAdded.ElementAdded(noUniquenessError, true);
						}
					}
				}
				else if (noUniquenessError != null)
				{
					noUniquenessError.Delete();
				}
			}
		}
		/// <summary>
		/// Validator callback for ImpliedInternalUniquenessConstraintError
		/// </summary>
		private static void DelayValidateImpliedInternalUniquenessConstraintError(ModelElement element)
		{
			(element as FactType).ValidateImpliedInternalUniqueness(null);
		}
		private void ValidateImpliedInternalUniqueness(INotifyElementAdded notifyAdded)
		{

			ORMModel model;
			IHasAlternateOwner<FactType> toAlternateOwner;
			IAlternateElementOwner<FactType> alternateOwner;
			if (!IsDeleted &&
				(null == (toAlternateOwner = this as IHasAlternateOwner<FactType>) ||
				null == (alternateOwner = toAlternateOwner.AlternateOwner) ||
				alternateOwner.ValidateErrorFor(this, typeof(ImpliedInternalUniquenessConstraintError))) &&
				(null != (model = ResolvedModel)))
			{
				LinkedElementCollection<RoleBase> factRoles = RoleCollection;
				bool hasError = false;
				int iucCount = GetInternalConstraintsCount(ConstraintType.InternalUniqueness);
				if (iucCount != 0)
				{
					uint[] roleBits = new uint[iucCount];
					const uint deonticBit = 1U << 31;
					int index = 0;
					foreach (UniquenessConstraint ic in GetInternalConstraints<UniquenessConstraint>())
					{
						uint bits = 0;
						LinkedElementCollection<Role> constraintRoles = ic.RoleCollection;
						int roleCount = constraintRoles.Count;
						for (int i = 0; i < roleCount; ++i)
						{
							bits |= 1U << factRoles.IndexOf(constraintRoles[i]);
						}
						if (bits != 0 && ic.Modality == ConstraintModality.Deontic)
						{
							bits |= deonticBit;
						}
						roleBits[index] = bits;
						++index;
					}
					int rbLength = roleBits.Length;
					for (int i = 0; !hasError && i < rbLength - 1; ++i)
					{
						for (int j = i + 1; j < rbLength; ++j)
						{
							uint left = roleBits[i];
							uint right = roleBits[j];
							if (left != 0 && right != 0)
							{
								if (0 != ((left ^ right) & deonticBit))
								{
									// Modality is different. The alethic constraint
									// should not be a subset of the deontic constraint,
									// but there are no restrictions the other way around.
									if ((left & right) == ((0 == (left & deonticBit)) ? left : right))
									{
										hasError = true;
										break;
									}
								}
								else
								{
									uint compare = left & right;
									if ((compare == left) || (compare == right))
									{
										hasError = true;
										break;
									}
								}
							}
						}
					}
				}
				ImpliedInternalUniquenessConstraintError impConstraint = ImpliedInternalUniquenessConstraintError;
				if (hasError)
				{
					if (impConstraint == null)
					{
						impConstraint = new ImpliedInternalUniquenessConstraintError(Partition);
						impConstraint.FactType = this;
						impConstraint.Model = model;
						impConstraint.GenerateErrorText();
						if (notifyAdded != null)
						{
							notifyAdded.ElementAdded(impConstraint);
						}
					}
				}
				else if (impConstraint != null)
				{
					impConstraint.Delete();
				}
			}
		}
		#endregion // Validation Methods
		#region Automatic Name Generation
		#region Deserialization Fixup
		/// <summary>
		/// Return a deserialization fixup listener. The listener
		/// synchronizes the initial name settings with any objectifying fact.
		/// </summary>
		public static IDeserializationFixupListener ObjectifyingNameFixupListener
		{
			get
			{
				return new GeneratedNameObjectificationFixupListener();
			}
		}
		/// <summary>
		/// Fixup listener implementation. Properly initializes the myGeneratedName field
		/// </summary>
		private sealed class GeneratedNameObjectificationFixupListener : DeserializationFixupListener<Objectification>
		{
			/// <summary>
			/// GeneratedNameObjectificationFixupListener constructor
			/// </summary>
			public GeneratedNameObjectificationFixupListener()
				: base((int)ORMDeserializationFixupPhase.SynchronizeStoredElementNames)
			{
			}
			/// <summary>
			/// Process objectification elements
			/// </summary>
			/// <param name="element">An Objectification element</param>
			/// <param name="store">The context store</param>
			/// <param name="notifyAdded">The listener to notify if elements are added during fixup</param>
			protected sealed override void ProcessElement(Objectification element, Store store, INotifyElementAdded notifyAdded)
			{
				SynchronizeGeneratedName(element, true);
			}
			/// <summary>
			/// Share the name synchronization for use during element merge.
			/// </summary>
			public static void SynchronizeGeneratedName(Objectification objectification)
			{
				SynchronizeGeneratedName(objectification, false);
			}
			/// <summary>
			/// Make sure the generated name is stored on load if it matches
			/// the object type name so that these names remain synchronized.
			/// </summary>
			/// <param name="objectification">The objectification to synchronize</param>
			/// <param name="forFixup">True if we should check the name generated by the
			/// original name generation algorithm.</param>
			private static void SynchronizeGeneratedName(Objectification objectification, bool forFixup)
			{
				if (!objectification.IsDeleted)
				{
					FactType factType = objectification.NestedFactType;
					ObjectType objectType = objectification.NestingType;
					string objectTypeName = objectType.Name;
					string generatedName = factType.GenerateName(false);
					if (generatedName == objectTypeName)
					{
						factType.myGeneratedName = generatedName;
					}
					else if (forFixup &&
						objectTypeName == factType.GenerateName(true))
					{
						factType.myGeneratedName = generatedName;
						objectType.Name = generatedName;
						FactTypeDerivationRule derivationRule;
						if (null != (derivationRule = factType.DerivationRule as FactTypeDerivationRule) &&
							derivationRule.DerivationCompleteness == DerivationCompleteness.FullyDerived &&
							!derivationRule.ExternalDerivation)
						{
							// In this case, we basically have two sources for the name and
							// keep them synchronized to the object type name.
							derivationRule.Name = generatedName;
						}
					}
				}
			}
		}
		/// <summary>
		/// Return a deserialization fixup listener. The listener
		/// synchronizes the initial name settings a fully derived
		/// derivation rule on a non-objectified fact type.
		/// </summary>
		public static IDeserializationFixupListener DerivationNameFixupListener
		{
			get
			{
				return new GeneratedNameDerivationFixupListener();
			}
		}
		/// <summary>
		/// Fixup listener implementation. Properly initializes the myGeneratedName field
		/// </summary>
		private sealed class GeneratedNameDerivationFixupListener : DeserializationFixupListener<FactTypeDerivationRule>
		{
			/// <summary>
			/// GeneratedNameDerivationFixupListener constructor
			/// </summary>
			public GeneratedNameDerivationFixupListener()
				: base((int)ORMDeserializationFixupPhase.SynchronizeStoredElementNames)
			{
			}
			/// <summary>
			/// Process derivation elements
			/// </summary>
			/// <param name="element">A <see cref="FactTypeDerivationRule"/> element</param>
			/// <param name="store">The context store</param>
			/// <param name="notifyAdded">The listener to notify if elements are added during fixup</param>
			protected sealed override void ProcessElement(FactTypeDerivationRule element, Store store, INotifyElementAdded notifyAdded)
			{
				SynchronizeGeneratedName(element, true);
			}
			/// <summary>
			/// Share the name synchronization for use during element merge.
			/// </summary>
			public static void SynchronizeGeneratedName(FactTypeDerivationRule derivationRule)
			{
				SynchronizeGeneratedName(derivationRule, false);
			}
			/// <summary>
			/// Make sure the generated name is stored on load if it matches
			/// the object type name so that these names remain synchronized.
			/// </summary>
			/// <param name="derivationRule">The derivation rule to synchronize</param>
			/// <param name="forFixup">True if we should check the name generated by the
			/// original name generation algorithm.</param>
			public static void SynchronizeGeneratedName(FactTypeDerivationRule derivationRule, bool forFixup)
			{
				if (!derivationRule.IsDeleted)
				{
					if (derivationRule.DerivationCompleteness == DerivationCompleteness.FullyDerived &&
						!derivationRule.ExternalDerivation)
					{
						FactType factType = derivationRule.FactType;
						ObjectType objectifyingType = factType.NestingType;
						if (objectifyingType != null)
						{
							// If an objectification is present, then let the other
							// fixup listener do the name generation.
							derivationRule.Name = objectifyingType.Name;
						}
						else
						{
							string generatedName = factType.GenerateName(false);
							string currentName = derivationRule.Name;
							if (string.IsNullOrEmpty(currentName))
							{
								factType.myGeneratedName = generatedName;
								derivationRule.Name = generatedName;
							}
							else if (generatedName == currentName)
							{
								factType.myGeneratedName = generatedName;
							}
							else if (forFixup &&
								currentName == factType.GenerateName(true))
							{
								factType.myGeneratedName = generatedName;
								derivationRule.Name = generatedName;
							}
						}
					}
					else
					{
						derivationRule.Name = "";
					}
				}
			}
		}
		#endregion // Deserialization Fixup
		/// <summary>
		/// Schedule delayed validation for a name part change.
		/// </summary>
		private void DelayValidateNamePartChanged()
		{
			FrameworkDomainModel.DelayValidateElement(
				this,
				Store.TransactionManager.CurrentTransaction.TopLevelTransaction.Context.ContextInfo.ContainsKey(ORMModel.BlockDuplicateReadingSignaturesKey) ?
					(ElementValidation)DelayValidateFactTypeNamePartChangedBlockDuplicateNames :
					DelayValidateFactTypeNamePartChanged);
		}
		[DelayValidateReplaces("DelayValidateFactTypeNamePartChanged")]
		private static void DelayValidateFactTypeNamePartChangedBlockDuplicateNames(ModelElement element)
		{
			if (!element.IsDeleted)
			{
				Dictionary<object, object> contextInfo = element.Store.TransactionManager.CurrentTransaction.TopLevelTransaction.Context.ContextInfo;
				object duplicateSignaturesKey = ORMModel.BlockDuplicateReadingSignaturesKey;
				bool removeDuplicateSignaturesKey = false;
				try
				{
					if (!contextInfo.ContainsKey(duplicateSignaturesKey))
					{
						contextInfo[duplicateSignaturesKey] = null;
						removeDuplicateSignaturesKey = true;
					}
					((FactType)element).ValidateFactTypeNamePartChanged();
				}
				finally
				{
					if (removeDuplicateSignaturesKey)
					{
						contextInfo.Remove(duplicateSignaturesKey);
					}
				}
			}
		}
		private static void DelayValidateFactTypeNamePartChanged(ModelElement element)
		{
			if (!element.IsDeleted)
			{
				Dictionary<object, object> contextInfo = element.Store.TransactionManager.CurrentTransaction.TopLevelTransaction.Context.ContextInfo;
				object duplicateSignaturesKey = ORMModel.BlockDuplicateReadingSignaturesKey;
				bool addDuplicateSignaturesKey = false;
				try
				{
					if (contextInfo.ContainsKey(duplicateSignaturesKey))
					{
						contextInfo.Remove(duplicateSignaturesKey);
						addDuplicateSignaturesKey = true;
					}
					((FactType)element).ValidateFactTypeNamePartChanged();
				}
				finally
				{
					if (addDuplicateSignaturesKey)
					{
						contextInfo[duplicateSignaturesKey] = null;
					}
				}
			}
		}
		private void ValidateFactTypeNamePartChanged()
		{
			string oldGeneratedName = myGeneratedName;
			bool haveNewName = false;
			string newGeneratedName = null;
			bool renameValidationErrors = true;

			// See if the objectifying type or derivation rule
			// uses the old automatic name. If it does, then
			// update the automatic name to the the new name.
			ObjectType objectifyingType;
			FactTypeDerivationRule derivationRule = null;
			if (null != (objectifyingType = NestingType) ||
				(null != (derivationRule = DerivationRule as FactTypeDerivationRule) &&
				derivationRule.DerivationCompleteness == DerivationCompleteness.FullyDerived &&
				!derivationRule.ExternalDerivation))
			{
				newGeneratedName = GenerateName(false);
				haveNewName = true;
				if (newGeneratedName != oldGeneratedName)
				{
					string storedName = objectifyingType != null ? objectifyingType.Name : derivationRule.Name;
					if (storedName == oldGeneratedName)
					{
						Dictionary<object, object> contextInfo = Store.TransactionManager.CurrentTransaction.TopLevelTransaction.Context.ContextInfo;
						object duplicateNamesKey = ORMModel.AllowDuplicateNamesKey;
						bool removeDuplicateNamesKey = false;
						// Blocking reading signature overlap should not block indirect signature
						// changes such as those triggered by changing the name of an objectified type.
						object duplicateSignaturesKey = ORMModel.BlockDuplicateReadingSignaturesKey;
						bool addDuplicateSignaturesKey = false;
						try
						{
							// Force a change in the transaction log so that we can
							// update the generated name as needed
							GeneratedNamePropertyHandler.SetGeneratedName(this, oldGeneratedName, newGeneratedName);
							if (!contextInfo.ContainsKey(duplicateNamesKey))
							{
								contextInfo[duplicateNamesKey] = null;
								removeDuplicateNamesKey = true;
							}
							if (contextInfo.ContainsKey(duplicateSignaturesKey))
							{
								contextInfo.Remove(duplicateSignaturesKey);
								addDuplicateSignaturesKey = true;
							}
							if (objectifyingType != null)
							{
								objectifyingType.Name = newGeneratedName;
							}
							else
							{
								derivationRule.Name = newGeneratedName;
							}
						}
						finally
						{
							if (removeDuplicateNamesKey)
							{
								contextInfo.Remove(duplicateNamesKey);
							}
							if (addDuplicateSignaturesKey)
							{
								contextInfo[duplicateSignaturesKey] = null;
							}
						}
					}
					else
					{
						// Rule updates for this case are handled in ValidateFactTypeNameForObjectTypeNameChangeRule
						// and DerivationRuleChangedRule
						haveNewName = false;
						newGeneratedName = null;
						renameValidationErrors = false;
					}
				}
				else
				{
					newGeneratedName = null;
				}
			}

			if (renameValidationErrors && (!haveNewName || newGeneratedName != null))
			{
				// Now move on to any model errors
				foreach (ModelError error in GetErrorCollection(ModelErrorUses.None))
				{
					if (0 != (error.RegenerateEvents & RegenerateErrorTextEvents.OwnerNameChange))
					{
						if (newGeneratedName == null)
						{
							newGeneratedName = GenerateName(false);
							haveNewName = true;
							if (newGeneratedName == oldGeneratedName)
							{
								newGeneratedName = null;
								break; // Look no further, name did not change
							}
							else
							{
								// Force a change in the transaction log so that we can
								// undo the generated name as needed
								GeneratedNamePropertyHandler.SetGeneratedName(this, oldGeneratedName, newGeneratedName);
							}
						}
						error.GenerateErrorText();
					}
				}
			}
			if (newGeneratedName == null && !haveNewName)
			{
				// Name did not change, but no one cared, add a simple entry to the transaction log
				// Note that we add an entry changing a blank to a blank. If we do not do this, then
				// there is no transaction record, and a name that is generated on demand outside
				// the transaction is not cleared on undo, so it does not get regenerated with
				// the original name.
				GeneratedNamePropertyHandler.ClearGeneratedName(this, !string.IsNullOrEmpty(oldGeneratedName) ? "" : oldGeneratedName);
			}
			OnFactTypeNameChanged();
		}
		partial class GeneratedNamePropertyHandler
		{
			/// <summary>
			/// Clear the generated name modification to the transaction log
			/// without reading the current name, which forces it to regenerated
			/// </summary>
			/// <param name="factType">The <see cref="FactType"/> to modify</param>
			/// <param name="oldGeneratedName">The old generated name to record</param>
			public static void ClearGeneratedName(FactType factType, string oldGeneratedName)
			{
				SetGeneratedName(factType, oldGeneratedName, "");
			}
			/// <summary>
			/// Add a generated name modification to the transaction log
			/// without reading the current name, which forces it to regenerated
			/// </summary>
			/// <param name="factType">The <see cref="FactType"/> to modify</param>
			/// <param name="oldGeneratedName">The old generated name</param>
			/// <param name="newGeneratedName">The new generated name</param>
			public static void SetGeneratedName(FactType factType, string oldGeneratedName, string newGeneratedName)
			{
				factType.myGeneratedName = newGeneratedName;
				Instance.ValueChanged(factType, oldGeneratedName, newGeneratedName);
			}
		}
		/// <summary>
		/// Return the default reading for this <see cref="FactType"/>. Corresponds
		/// to the reading information used to calculate the <see cref="DefaultName"/>
		/// property in the form of an <see cref="IReading"/>, which enables precise
		/// control of generated names.
		/// </summary>
		/// <returns><see cref="IReading"/> or <see langword="null"/> in rare cases</returns>
		public IReading GetDefaultReading()
		{
			// Note that the implementation here should parallel GenerateName
			IReading retVal = null;
			if (!IsDeleted && !IsDeleting)
			{
				if (HasImplicitReadings)
				{
					return GetDefaultImplicitReading();
				}
				LinkedElementCollection<ReadingOrder> readingOrders = ReadingOrderCollection;
				int readingOrdersCount = readingOrders.Count;
				for (int i = 0; i < readingOrdersCount; ++i)
				{
					ReadingOrder order = readingOrders[i];
					LinkedElementCollection<Reading> readings = order.ReadingCollection;
					int readingsCount = readings.Count;
					for (int j = 0; j < readingsCount; ++j)
					{
						Reading reading = readings[j];
						if (reading.TooFewRolesError == null && reading.TooManyRolesError == null)
						{
							// Don't block this for all errors. Just filter out readings with
							// structural errors. Anything else (duplicate signature, user
							// modification required, etc.) should not affect the default naming.
							return reading;
						}
					}
				}
				LinkedElementCollection<RoleBase> roles = RoleCollection;
				int roleCount = roles.Count;
				if (roleCount != 0)
				{
					//used for naming binarized unaries
					int? unaryRoleIndex;
					if (roleCount == 2 &&
						(unaryRoleIndex = GetUnaryRoleIndex(roles)).HasValue)
					{
						return new ImplicitReading(ResourceStrings.ImplicitBooleanValueTypeNoReadingFormatString, new RoleBase[] { roles[unaryRoleIndex.Value] });
					}
					string readingText = null;
					switch (roleCount)
					{
						case 2:
							readingText = "{0}{1}";
							break;
						case 3:
							readingText = "{0}{1}{2}";
							break;
						case 4:
							readingText = "{0}{1}{2}{3}";
							break;
						default:
							StringBuilder sb = new StringBuilder();
							for (int i = 0; i < roleCount; ++i)
							{
								sb.Append('{');
								sb.Append(i.ToString(CultureInfo.InvariantCulture));
								sb.Append('}');
							}
							readingText = sb.ToString();
							break;
					}
					return new ImplicitReading(readingText, roles);
				}
			}
			return retVal;
		}
		private static Dictionary<string, object> myGeneratedNameRolePlayerRenderingOptions;
		private static IDictionary<string, object> GeneratedNameRolePlayerRenderingOptions
		{
			get
			{
				Dictionary<string, object> options;
				if (null == (options = myGeneratedNameRolePlayerRenderingOptions))
				{
					options = new Dictionary<string, object>();
					options[CoreVerbalizationOption.ObjectTypeNameDisplay] = ObjectTypeNameVerbalizationStyle.SeparateCombinedNames;
					options[CoreVerbalizationOption.RemoveObjectTypeNameCharactersOnSeparate] = null;
					options[CoreVerbalizationOption.FixedCaseWordPrefix] = "\x1";
					System.Threading.Interlocked.CompareExchange<Dictionary<string, object>>(ref myGeneratedNameRolePlayerRenderingOptions, options, null);
					options = myGeneratedNameRolePlayerRenderingOptions;
				}
				return options;
			}
		}
		private static Dictionary<string, object> myGeneratedNameFinalRenderingOptions;
		private static IDictionary<string, object> GeneratedNameFinalRenderingOptions
		{
			get
			{
				Dictionary<string, object> options;
				if (null == (options = myGeneratedNameFinalRenderingOptions))
				{
					options = new Dictionary<string, object>();
					options[CoreVerbalizationOption.ObjectTypeNameDisplay] = ObjectTypeNameVerbalizationStyle.CombineNamesLeadWithUpper;
					options[CoreVerbalizationOption.RemoveObjectTypeNameCharactersOnSeparate] = "-.:_";
					options[CoreVerbalizationOption.FixedCaseWordPrefix] = "\x1";
					System.Threading.Interlocked.CompareExchange<Dictionary<string, object>>(ref myGeneratedNameFinalRenderingOptions, options, null);
					options = myGeneratedNameFinalRenderingOptions;
				}
				return options;
			}
		}
		/// <summary>
		/// Helper function to get the current setting for the generated Name property
		/// </summary>
		private string GenerateName(bool originalAlgorithm)
		{
			// Note that the implementation here should parallel GetDefaultReading
			string retVal = "";
			if (!IsDeleted && !IsDeleting)
			{
				if (HasImplicitReadings)
				{
					return GenerateImplicitName();
				}
				QueryBase query;
				if (null != (query = this as QueryBase))
				{
					// We aren't expecting a reading for a query. Use the signature (PARAMS)->{ROLES}
					// as the generated name. We do this inline here as a favor to QueryBase because so
					// many of the components overlap that it isn't worth keeping two sets of rules.
					string structureFormat = ResourceStrings.SubquerySignatureFormat;
					string namedArgFormat = ResourceStrings.SubqueryNamedArgumentFormat;
					string missingTypeName = ResourceStrings.ModelReadingEditorMissingRolePlayerText;
					CultureInfo culture = CultureInfo.CurrentCulture;
					IFormatProvider formatProvider = culture;
					string separator = culture.TextInfo.ListSeparator;
					if (!char.IsWhiteSpace(separator, separator.Length - 1))
					{
						separator += " ";
					}
					string parameters;
					string roles;
					StringBuilder builder = new StringBuilder();
					ObjectType rolePlayer;
					string typeName;
					string fieldName;
					bool first = true;

					// Get parameters
					foreach (QueryParameter parameter in query.ParameterCollection)
					{
						if (first)
						{
							first = false;
						}
						else
						{
							builder.Append(separator);
						}
						rolePlayer = parameter.ParameterType;
						typeName = (rolePlayer != null) ? rolePlayer.Name : missingTypeName;
						builder.Append(string.IsNullOrEmpty(fieldName = parameter.Name) ? typeName : string.Format(formatProvider, namedArgFormat, fieldName, typeName));
					}
					parameters = builder.ToString();

					// Get roles
					builder.Length = 0;
					first = true;
					foreach (RoleBase roleBase in query.RoleCollection)
					{
						if (first)
						{
							first = false;
						}
						else
						{
							builder.Append(separator);
						}
						Role role = roleBase.Role;
						rolePlayer = role.RolePlayer;
						typeName = (rolePlayer != null) ? rolePlayer.Name : missingTypeName;
						builder.Append(string.IsNullOrEmpty(fieldName = role.Name) ? typeName : string.Format(formatProvider, namedArgFormat, fieldName, typeName));
					}
					roles = builder.ToString();

					retVal = string.Format(formatProvider, structureFormat, parameters, roles);
				}
				else
				{
					// Grab the first reading with no errors from the first reading order
					// Note that the first reading in the first reading order is considered
					// to be the default reading order
					LinkedElementCollection<RoleBase> roles = null;
					string formatText = null;
					LinkedElementCollection<ReadingOrder> readingOrders = ReadingOrderCollection;
					int readingOrdersCount = readingOrders.Count;
					for (int i = 0; i < readingOrdersCount && formatText == null; ++i)
					{
						ReadingOrder order = readingOrders[i];
						LinkedElementCollection<Reading> readings = order.ReadingCollection;
						int readingsCount = readings.Count;
						for (int j = 0; j < readingsCount; ++j)
						{
							Reading reading = readings[j];
							if (reading.TooFewRolesError == null && reading.TooManyRolesError == null)
							{
								// Don't block this for all errors. Just filter out readings with
								// structural errors. Anything else (duplicate signature, user
								// modification required, etc.) should not affect the default naming.
								roles = order.RoleCollection;
								formatText = reading.Text;
								break;
							}
						}
					}
					if (roles == null)
					{
						roles = RoleCollection;
					}
					int roleCount = roles.Count;
					if (roleCount != 0)
					{
						//used for naming binarized unaries
						bool applyValueText = false;
						if (roleCount == 2 && GetUnaryRoleIndex(roles).HasValue)
						{
							roleCount = 1;
							applyValueText = readingOrdersCount == 0;
						}
						string[] replacements = new string[roleCount];
						if (!originalAlgorithm)
						{
							for (int k = 0; k < roleCount; ++k)
							{
								ObjectType rolePlayer = roles[k].Role.RolePlayer;
								replacements[k] = (rolePlayer != null) ?
									VerbalizationHelper.NormalizeObjectTypeName(rolePlayer, GeneratedNameRolePlayerRenderingOptions) :
									ResourceStrings.ModelReadingEditorMissingRolePlayerText;
							}
							retVal = VerbalizationHelper.NormalizeObjectTypeName(
								(formatText == null) ?
									string.Join(" ", replacements) :
									string.Format(CultureInfo.InvariantCulture, formatText, replacements),
								GeneratedNameFinalRenderingOptions);
						}
						else
						{
							for (int k = 0; k < roleCount; ++k)
							{
								ObjectType rolePlayer = roles[k].Role.RolePlayer;
								replacements[k] = (rolePlayer != null) ? rolePlayer.Name.Replace('-', ' ') : ResourceStrings.ModelReadingEditorMissingRolePlayerText;
							}
							retVal = (formatText == null) ?
								string.Concat(replacements) :
								string.Format(CultureInfo.InvariantCulture, CultureInfo.InvariantCulture.TextInfo.ToTitleCase(formatText.Replace('-', ' ')), replacements);
							if (!string.IsNullOrEmpty(retVal))
							{
								retVal = retVal.Replace(" ", null);
							}
						}
						if (applyValueText)
						{
							retVal = string.Format(CultureInfo.InvariantCulture, ResourceStrings.ImplicitBooleanValueTypeNoReadingFormatString, retVal);
						}
					}
				}
			}
			return retVal;
		}
		private string myGeneratedName = String.Empty;
		/// <summary>
		/// The auto-generated name for this fact type. Based on the
		/// first reading in the first reading order.
		/// </summary>
		public string DefaultName
		{
			get
			{
				string retVal = myGeneratedName;
				if (string.IsNullOrEmpty(retVal))
				{
					retVal = GenerateName(false);
					if (retVal.Length != 0)
					{
						if (Store.TransactionManager.InTransaction)
						{
							GeneratedNamePropertyHandler.SetGeneratedName(this, "", retVal);
						}
						else
						{
							myGeneratedName = retVal;
						}
					}
				}
				return retVal ?? String.Empty;
			}
		}
		/// <summary>
		/// Override to use our own name handling
		/// </summary>
		protected override void MergeConfigure(ElementGroup elementGroup)
		{
			// Do not forward to the base here. The base calls SetUniqueName,
			// but we don't enforce unique names on the generated FactType name.

			// If the Objectification is set during merge, then we need to
			// make sure the names are in sync.
			ObjectType nestingType = NestingType;
			if (nestingType != null)
			{
				string generatedName = GenerateName(false);
				string nestingTypeName = nestingType.Name;
				if (nestingTypeName.Length == 0 || generatedName == nestingTypeName)
				{
					myGeneratedName = generatedName;
				}
			}
		}
		#endregion // Automatic Name Generation
		#region Model Validation Rules
		/// <summary>
		/// Determine the <see cref="ORMModel"/> for this element.
		/// If the element is owned by an alternate owner, then retrieve
		/// the model through that owner.
		/// </summary>
		public ORMModel ResolvedModel
		{
			get
			{
				IHasAlternateOwner<FactType> toAlternateOwner;
				IAlternateElementOwner<FactType> alternateOwner;
				return (null != (toAlternateOwner = this as IHasAlternateOwner<FactType>) &&
					null != (alternateOwner = toAlternateOwner.AlternateOwner)) ?
						alternateOwner.Model :
						this.Model;
			}
		}
		/// <summary>
		/// RolePlayerChangeRule: typeof(FactTypeHasRole)
		/// Other rules are not set up to handle roles jumping from one FactType to another.
		/// The NORMA UI never attempts this operation.
		/// Throw if any attempt is made to directly modify roles on a FactTypeHasRole
		/// relationship after it has been created.
		/// </summary>
		private static void BlockRoleMigrationRule(RolePlayerChangedEventArgs e)
		{
			throw new InvalidOperationException(ResourceStrings.ModelExceptionFactTypeEnforceNoRoleMigration);
		}
		/// <summary>
		/// AddRule: typeof(FactTypeHasRole)
		/// Internal uniqueness constraints are required for non-unary facts. Requires
		/// validation when roles are added and removed.
		/// </summary>
		private static void FactTypeHasRoleAddRule(ElementAddedEventArgs e)
		{
			FactType factType = (e.ModelElement as FactTypeHasRole).FactType;
			FrameworkDomainModel.DelayValidateElement(factType, DelayValidateFactTypeRequiresInternalUniquenessConstraintError);
			factType.DelayValidateNamePartChanged();
		}
		/// <summary>
		/// DeleteRule: typeof(FactTypeHasRole)
		/// Internal uniqueness constraints are required for non-unary facts. Requires
		/// validation when roles are added and removed.
		/// </summary>
		private static void FactTypeHasRoleDeleteRule(ElementDeletedEventArgs e)
		{
			FactType factType = (e.ModelElement as FactTypeHasRole).FactType;
			if (!factType.IsDeleted)
			{
				FrameworkDomainModel.DelayValidateElement(factType, DelayValidateFactTypeRequiresInternalUniquenessConstraintError);
				factType.DelayValidateNamePartChanged();
			}
		}
		/// <summary>
		/// AddRule: typeof(FactSetConstraint)
		/// Validate the InternalUniquenessConstraintRequired and ImpliedInternalUniquenessConstraintError
		/// </summary>
		private static void InternalConstraintAddRule(ElementAddedEventArgs e)
		{
			FactSetConstraint link = e.ModelElement as FactSetConstraint;
			if (link.SetConstraint.Constraint.ConstraintType == ConstraintType.InternalUniqueness)
			{
				FactType fact = link.FactType;
				FrameworkDomainModel.DelayValidateElement(fact, DelayValidateFactTypeRequiresInternalUniquenessConstraintError);
				FrameworkDomainModel.DelayValidateElement(fact, DelayValidateImpliedInternalUniquenessConstraintError);
			}
		}
		/// <summary>
		/// DeleteRule: typeof(FactSetConstraint)
		/// Validate the InternalUniquenessConstraintRequired and ImpliedInternalUniquenessConstraintError
		/// </summary>
		private static void InternalConstraintDeleteRule(ElementDeletedEventArgs e)
		{
			FactSetConstraint link = e.ModelElement as FactSetConstraint;
			FactType fact = link.FactType;
			if (!fact.IsDeleted &&
				link.SetConstraint.Constraint.ConstraintType == ConstraintType.InternalUniqueness)
			{
				FrameworkDomainModel.DelayValidateElement(fact, DelayValidateFactTypeRequiresInternalUniquenessConstraintError);
				FrameworkDomainModel.DelayValidateElement(fact, DelayValidateImpliedInternalUniquenessConstraintError);
			}
		}
		/// <summary>
		/// ChangeRule: typeof(UniquenessConstraint)
		/// </summary>
		private static void InternalUniquenessConstraintChangeRule(ElementPropertyChangedEventArgs e)
		{
			Guid attributeId = e.DomainProperty.Id;
			if (attributeId == UniquenessConstraint.ModalityDomainPropertyId)
			{
				UniquenessConstraint constraint = e.ModelElement as UniquenessConstraint;
				LinkedElementCollection<FactType> facts;
				if (!constraint.IsDeleted &&
					constraint.IsInternal &&
					1 == (facts = constraint.FactTypeCollection).Count)
				{
					FactType fact = facts[0];
					FrameworkDomainModel.DelayValidateElement(fact, DelayValidateFactTypeRequiresInternalUniquenessConstraintError);
					FrameworkDomainModel.DelayValidateElement(fact, DelayValidateImpliedInternalUniquenessConstraintError);
				}
			}
		}
		/// <summary>
		/// AddRule: typeof(ConstraintRoleSequenceHasRole)
		/// </summary>
		private static void InternalConstraintCollectionHasConstraintAddedRule(ElementAddedEventArgs e)
		{
			ConstraintRoleSequenceHasRole link = e.ModelElement as ConstraintRoleSequenceHasRole;
			UniquenessConstraint constraint = link.ConstraintRoleSequence as UniquenessConstraint;
			LinkedElementCollection<FactType> facts;
			if (constraint != null &&
				constraint.IsInternal &&
				1 == (facts = constraint.FactTypeCollection).Count)
			{
				FactType factType = facts[0];
				if (constraint.Modality == ConstraintModality.Alethic &&
					factType.InternalUniquenessConstraintRequiredError != null)
				{
					// We only need to do this on an add for an existing internal uniqueness with no roles.
					// The required error validator will be triggered for adds/deletes it cares about through
					// other mechanisms. We don't need to run it constantly here.
					FrameworkDomainModel.DelayValidateElement(factType, DelayValidateFactTypeRequiresInternalUniquenessConstraintError);
				}
				FrameworkDomainModel.DelayValidateElement(factType, DelayValidateImpliedInternalUniquenessConstraintError);
			}
		}
		/// <summary>
		/// DeleteRule: typeof(ConstraintRoleSequenceHasRole)
		/// </summary>
		private static void InternalConstraintCollectionHasConstraintDeleteRule(ElementDeletedEventArgs e)
		{
			ConstraintRoleSequenceHasRole link = e.ModelElement as ConstraintRoleSequenceHasRole;
			UniquenessConstraint constraint = link.ConstraintRoleSequence as UniquenessConstraint;
			LinkedElementCollection<FactType> facts;
			FactType fact;
			if (constraint != null &&
				!constraint.IsDeleted &&
				constraint.IsInternal &&
				1 == (facts = constraint.FactTypeCollection).Count &&
				!(fact = facts[0]).IsDeleted)
			{
				FrameworkDomainModel.DelayValidateElement(fact, DelayValidateImpliedInternalUniquenessConstraintError);
			}
		}
		/// <summary>
		/// AddRule: typeof(ModelHasFactType)
		/// Calls the validation of all FactType related errors
		/// </summary>
		private static void FactTypeAddedRule(ElementAddedEventArgs e)
		{
			(e.ModelElement as ModelHasFactType).FactType.DelayValidateErrors();
		}
		/// <summary>
		/// AddRule: typeof(FactTypeHasReadingOrder)
		/// Validates ReadingRequiredError and possibly triggers automatic name generation
		/// </summary>
		private static void FactTypeHasReadingOrderAddRule(ElementAddedEventArgs e)
		{
			FactType factType = (e.ModelElement as FactTypeHasReadingOrder).FactType;
			FrameworkDomainModel.DelayValidateElement(factType, DelayValidateFactTypeRequiresReadingError);
			factType.DelayValidateNamePartChanged();
		}
		/// <summary>
		/// DeleteRule: typeof(FactTypeHasReadingOrder)
		/// Validates ReadingRequiredError and possibly triggers automatic name generation
		/// </summary>
		private static void FactTypeHasReadingOrderDeleteRule(ElementDeletedEventArgs e)
		{
			FactType factType = (e.ModelElement as FactTypeHasReadingOrder).FactType;
			if (!factType.IsDeleted)
			{
				FrameworkDomainModel.DelayValidateElement(factType, DelayValidateFactTypeRequiresReadingError);
				factType.DelayValidateNamePartChanged();
			}
		}
		/// <summary>
		/// AddRule: typeof(ReadingOrderHasReading)
		/// Validates ReadingRequiredError and possibly triggers automatic name generation
		/// </summary>
		private static void ReadingOrderHasReadingAddRule(ElementAddedEventArgs e)
		{
			FactType factType = (e.ModelElement as ReadingOrderHasReading).ReadingOrder.FactType;
			if (factType != null)
			{
				FrameworkDomainModel.DelayValidateElement(factType, DelayValidateFactTypeRequiresReadingError);
				factType.DelayValidateNamePartChanged();
			}
		}
		/// <summary>
		/// DeleteRule: typeof(ReadingOrderHasReading)
		/// Validates ReadingRequiredError and possibly triggers automatic name generation
		/// </summary>
		private static void ReadingOrderHasReadingDeleteRule(ElementDeletedEventArgs e)
		{
			ReadingOrder ord = (e.ModelElement as ReadingOrderHasReading).ReadingOrder;
			FactType factType;
			if (!ord.IsDeleted &&
				null != (factType = ord.FactType) &&
				!factType.IsDeleted)
			{
				FrameworkDomainModel.DelayValidateElement(factType, DelayValidateFactTypeRequiresReadingError);
				factType.DelayValidateNamePartChanged();
			}
		}
		/// <summary>
		/// ChangeRule: typeof(Reading)
		/// </summary>
		private static void ValidateFactTypeNameForReadingChangeRule(ElementPropertyChangedEventArgs e)
		{
			if (e.DomainProperty.Id == Reading.TextDomainPropertyId)
			{
				Reading reading = e.ModelElement as Reading;
				ReadingOrder order;
				FactType factType;
				if (null != reading &&
					!reading.IsDeleted &&
					null != (order = reading.ReadingOrder) &&
					!order.IsDeleted &&
					null != (factType = order.FactType) &&
					!factType.IsDeleted)
				{
					factType.DelayValidateNamePartChanged();
				}
			}
		}
		/// <summary>
		/// RolePlayerPositionChangeRule: typeof(FactTypeHasReadingOrder)
		/// </summary>
		private static void ValidateFactTypeNameForReadingOrderReorderRule(RolePlayerOrderChangedEventArgs e)
		{
			if (e.SourceDomainRole.Id == FactTypeHasReadingOrder.FactTypeDomainRoleId)
			{
				FactType factType = (FactType)e.SourceElement;
				if (!factType.IsDeleted)
				{
					factType.DelayValidateNamePartChanged();
				}
			}
		}
		/// <summary>
		/// RolePlayerPositionChangeRule: typeof(ReadingOrderHasReading)
		/// </summary>
		private static void ValidateFactTypeNameForReadingReorderRule(RolePlayerOrderChangedEventArgs e)
		{
			if (e.SourceDomainRole.Id == ReadingOrderHasReading.ReadingOrderDomainRoleId)
			{
				ReadingOrder order = (ReadingOrder)e.SourceElement;
				FactType factType;
				if (!order.IsDeleted &&
					null != (factType = order.FactType) &&
					!factType.IsDeleted)
				{
					factType.DelayValidateNamePartChanged();
				}
			}
		}
		/// <summary>
		/// AddRule: typeof(ObjectTypePlaysRole)
		/// </summary>
		private static void ValidateFactTypeNameForRolePlayerAddedRule(ElementAddedEventArgs e)
		{
			ProcessValidateFactNameForRolePlayerAdded(e.ModelElement as ObjectTypePlaysRole, null);
		}
		/// <summary>
		/// Rule helper method
		/// </summary>
		private static void ProcessValidateFactNameForRolePlayerAdded(ObjectTypePlaysRole link, Role playedRole)
		{
			if (playedRole == null)
			{
				playedRole = link.PlayedRole;
			}
			FactType factType = playedRole.FactType;
			if (factType != null)
			{
				factType.DelayValidateNamePartChanged();
			}
			RoleProxy proxy;
			if (null != (proxy = playedRole.Proxy) &&
				null != (factType = proxy.FactType))
			{
				factType.DelayValidateNamePartChanged();
			}
		}
		/// <summary>
		/// DeleteRule: typeof(ObjectTypePlaysRole)
		/// </summary>
		private static void ValidateFactTypeNameForRolePlayerDeleteRule(ElementDeletedEventArgs e)
		{
			ProcessValidateFactNameForRolePlayerDelete(e.ModelElement as ObjectTypePlaysRole, null);
		}
		/// <summary>
		/// Rule helper method
		/// </summary>
		private static void ProcessValidateFactNameForRolePlayerDelete(ObjectTypePlaysRole link, Role playedRole)
		{
			if (playedRole == null)
			{
				playedRole = link.PlayedRole;
			}
			FactType factType;
			if (!playedRole.IsDeleted)
			{
				if (null != (factType = playedRole.FactType))
				{
					factType.DelayValidateNamePartChanged();
				}
				RoleProxy proxy;
				if (null != (proxy = playedRole.Proxy) &&
					null != (factType = proxy.FactType))
				{
					factType.DelayValidateNamePartChanged();
				}
			}
		}
		/// <summary>
		/// RolePlayerChangeRule: typeof(ObjectTypePlaysRole)
		/// </summary>
		private static void ValidateFactTypeNameForRolePlayerRolePlayerChangeRule(RolePlayerChangedEventArgs e)
		{
			Guid changedRoleGuid = e.DomainRole.Id;
			Role oldRole = null;
			if (changedRoleGuid == ObjectTypePlaysRole.PlayedRoleDomainRoleId)
			{
				oldRole = (Role)e.OldRolePlayer;
			}
			ObjectTypePlaysRole link = e.ElementLink as ObjectTypePlaysRole;
			ProcessValidateFactNameForRolePlayerDelete(link, oldRole);
			ProcessValidateFactNameForRolePlayerAdded(link, null);
		}
		/// <summary>
		/// ChangeRule: typeof(ObjectType)
		/// </summary>
		private static void ValidateFactTypeNameForObjectTypeNameChangeRule(ElementPropertyChangedEventArgs e)
		{
			Guid attributeId = e.DomainProperty.Id;
			if (attributeId == ObjectType.NameDomainPropertyId)
			{
				ObjectType objectType = (ObjectType)e.ModelElement;
				LinkedElementCollection<Role> playedRoles = objectType.PlayedRoleCollection;
				int playedRolesCount = playedRoles.Count;
				for (int i = 0; i < playedRolesCount; ++i)
				{
					Role role = playedRoles[i];
					FactType factType = role.FactType;
					if (factType != null)
					{
						factType.DelayValidateNamePartChanged();
					}
					RoleProxy proxy;
					if (null != (proxy = role.Proxy) &&
						null != (factType = proxy.FactType))
					{
						factType.DelayValidateNamePartChanged();
					}
				}
				FactType nestedFact = objectType.NestedFactType;
				if (nestedFact != null)
				{
					string newName = (string)e.NewValue;
					if (newName.Length != 0)
					{
						string generatedName = nestedFact.myGeneratedName;
						if (!string.IsNullOrEmpty(generatedName) &&
							(object)newName != (object)generatedName)
						{
							GeneratedNamePropertyHandler.ClearGeneratedName(nestedFact, generatedName);
						}
						nestedFact.RegenerateErrorText();
						nestedFact.OnFactTypeNameChanged();
					}
					FactTypeDerivationRule derivationRule = nestedFact.DerivationRule as FactTypeDerivationRule;
					if (derivationRule != null &&
						derivationRule.DerivationCompleteness == DerivationCompleteness.FullyDerived &&
						!derivationRule.ExternalDerivation)
					{
						derivationRule.Name = newName;
					}
				}
			}
		}
		/// <summary>
		/// ChangeRule: typeof(FactTypeDerivationRule)
		/// Verify fact type name and generated constraint pattenrs
		/// </summary>
		private static void DerivationRuleChangedRule(ElementPropertyChangedEventArgs e)
		{
			Guid attributeId = e.DomainProperty.Id;
			FactTypeDerivationRule derivationRule;
			FactType factType;
			string generatedName;
			if (attributeId == FactTypeDerivationRule.NameDomainPropertyId)
			{
				derivationRule = (FactTypeDerivationRule)e.ModelElement;
				if (derivationRule.DerivationCompleteness == DerivationCompleteness.FullyDerived &&
					!derivationRule.ExternalDerivation &&
					null != (factType = derivationRule.FactType) &&
					null == factType.NestingType)
				{
					string newName = (string)e.NewValue;
					if (newName.Length != 0)
					{
						generatedName = factType.myGeneratedName;
						if (!string.IsNullOrEmpty(generatedName) &&
							(object)newName != (object)generatedName)
						{
							GeneratedNamePropertyHandler.ClearGeneratedName(factType, generatedName);
						}
					}
					else
					{
						derivationRule.Name = factType.DefaultName;
					}
					factType.RegenerateErrorText();
					factType.OnFactTypeNameChanged();
				}
			}
			else if (attributeId == FactTypeDerivationRule.DerivationCompletenessDomainPropertyId ||
				attributeId == FactTypeDerivationRule.ExternalDerivationDomainPropertyId)
			{
				derivationRule = (FactTypeDerivationRule)e.ModelElement;
				if (null != (factType = derivationRule.FactType))
				{
					FrameworkDomainModel.DelayValidateElement(factType, DelayValidateFactTypeRequiresInternalUniquenessConstraintError);
					ObjectType objectifyingType = factType.NestingType;
					if (derivationRule.DerivationCompleteness == DerivationCompleteness.FullyDerived &&
						!derivationRule.ExternalDerivation)
					{
						if (objectifyingType != null)
						{
							derivationRule.Name = objectifyingType.Name;
						}
						else
						{
							generatedName = factType.myGeneratedName;
							if (generatedName == null)
							{
								// Note that there is no notification here, we just
								// need the generated name set so that we can track
								// future changes with the stored value.
								generatedName = factType.GenerateName(false);
								factType.myGeneratedName = generatedName;
							}
							derivationRule.Name = generatedName;
						}
					}
					else
					{
						if (objectifyingType == null)
						{
							// The derivation rule has been controlling the name, revert
							// to the generated name shown for an unobjectified, asserted
							// fact type.
							string currentName = derivationRule.Name;
							generatedName = factType.myGeneratedName;
							bool nameChanged = false;
							if (string.IsNullOrEmpty(generatedName))
							{
								nameChanged = true;
							}
							else if ((object)derivationRule.Name != (object)generatedName)
							{
								GeneratedNamePropertyHandler.ClearGeneratedName(factType, generatedName);
								nameChanged = true;
							}
							if (nameChanged)
							{
								factType.RegenerateErrorText();
								factType.OnFactTypeNameChanged();
							}
						}
						if (!string.IsNullOrEmpty(derivationRule.Name))
						{
							// Prepare to eliminate the old value, but don't toss it yet because
							// the explicit name may be needed to set the name for an implicit
							// objectification created in response to switching to a partially
							// derived fact type.
							FrameworkDomainModel.DelayValidateElement(derivationRule, DelayClearDerivationRuleName);
						}
					}
				}
			}
		}
		[DelayValidatePriority(1)] // Run after Objectification.DelayProcessFactTypeForImpliedObjectification
		private static void DelayClearDerivationRuleName(ModelElement element)
		{
			FactTypeDerivationRule derivationRule;
			if (!element.IsDeleted &&
				((derivationRule = (FactTypeDerivationRule)element).DerivationCompleteness != DerivationCompleteness.FullyDerived ||
				derivationRule.ExternalDerivation) &&
				!string.IsNullOrEmpty(derivationRule.Name))
			{
				derivationRule.Name = "";
			}
		}
		/// <summary>
		/// AddRule: typeof(FactTypeHasDerivationRule)
		/// Set the initial derivation rule name for a fully derived derivation rule and
		/// verify constraint patterns.
		/// </summary>
		private static void DerivationRuleAddedRule(ElementAddedEventArgs e)
		{
			FactTypeHasDerivationRule link = (FactTypeHasDerivationRule)e.ModelElement;
			FactTypeDerivationRule derivationRule;
			if (null != (derivationRule = link.DerivationRule as FactTypeDerivationRule))
			{
				if (derivationRule.DerivationCompleteness == DerivationCompleteness.FullyDerived &&
					!derivationRule.ExternalDerivation)
				{
					FactType factType = link.FactType;
					ObjectType nestingType;
					if (null != (nestingType = factType.NestingType))
					{
						derivationRule.Name = nestingType.Name;
					}
					else if (string.IsNullOrEmpty(derivationRule.Name))
					{
						derivationRule.Name = factType.DefaultName;
					}
					FrameworkDomainModel.DelayValidateElement(factType, DelayValidateFactTypeRequiresInternalUniquenessConstraintError);
				}
				else if (!string.IsNullOrEmpty(derivationRule.Name))
				{
					derivationRule.Name = "";
				}
			}
		}
		/// <summary>
		/// AddRule: typeof(FactTypeDerivationRule), FireTime=LocalCommit, Priority=FrameworkDomainModel.CopyClosureExpansionCompletedRulePriority;
		/// Synchronize the generated fact type name on merge completion
		/// </summary>
		private static void DerivationRuleAddedClosureRule(ElementAddedEventArgs e)
		{
			ModelElement element = e.ModelElement;
			if (!element.IsDeleted &&
				CopyMergeUtility.GetIntegrationPhase(element.Store) == CopyClosureIntegrationPhase.IntegrationComplete)
			{
				GeneratedNameDerivationFixupListener.SynchronizeGeneratedName((FactTypeDerivationRule)element);
			}
		}
		/// <summary>
		/// DeleteRule: typeof(FactTypeHasDerivationRule)
		/// Verify naming and constraint patterns for derivation rule deletion
		/// </summary>
		private static void DerivationRuleDeletedRule(ElementDeletedEventArgs e)
		{
			FactTypeHasDerivationRule link = (FactTypeHasDerivationRule)e.ModelElement;
			FactType factType;
			FactTypeDerivationRule derivationRule;
			string explicitName;
			if (!(factType = link.FactType).IsDeleted &&
				null != (derivationRule = link.DerivationRule as FactTypeDerivationRule) &&
				derivationRule.DerivationCompleteness == DerivationCompleteness.FullyDerived &&
				!derivationRule.ExternalDerivation)
			{
				if (factType.NestingType == null &&
					!string.IsNullOrEmpty(explicitName = derivationRule.Name) &&
					(object)explicitName != (object)factType.myGeneratedName)
				{
					factType.RegenerateErrorText();
					factType.OnFactTypeNameChanged();
				}
				FrameworkDomainModel.DelayValidateElement(factType, DelayValidateFactTypeRequiresInternalUniquenessConstraintError);
			}
		}
		/// <summary>
		/// Rule helper function to regenerate error text
		/// </summary>
		private void RegenerateErrorText()
		{
			foreach (ModelError error in GetErrorCollection(ModelErrorUses.None))
			{
				if (0 != (error.RegenerateEvents & RegenerateErrorTextEvents.OwnerNameChange))
				{
					error.GenerateErrorText();
				}
			}
		}
		/// <summary>
		/// AddRule: typeof(Objectification), FireTime=LocalCommit, Priority=FrameworkDomainModel.CopyClosureExpansionCompletedRulePriority;
		/// Fixup the generated name when closure is complete
		/// </summary>
		private static void ValidateFactTypeNameForObjectificationAddedClosureRule(ElementAddedEventArgs e)
		{
			ModelElement link = e.ModelElement;
			if (!link.IsDeleted &&
				CopyMergeUtility.GetIntegrationPhase(link.Store) == CopyClosureIntegrationPhase.IntegrationComplete)
			{
				GeneratedNameObjectificationFixupListener.SynchronizeGeneratedName((Objectification)link);
			}
		}
		/// <summary>
		/// AddRule: typeof(Objectification), Priority=1;
		/// Update the fact type name when an objectification is added
		/// </summary>
		private static void ValidateFactTypeNameForObjectificationAddedRule(ElementAddedEventArgs e)
		{
			ProcessValidateFactNameForObjectificationAdded(e.ModelElement as Objectification);
		}
		/// <summary>
		/// Rule helper method
		/// </summary>
		private static void ProcessValidateFactNameForObjectificationAdded(Objectification link)
		{
			ObjectType nestingObjectType = link.NestingType;
			if (nestingObjectType.Name.Length != 0)
			{
				FactType nestedFactType = link.NestedFactType;
				nestedFactType.RegenerateErrorText();
				nestedFactType.OnFactTypeNameChanged();
			}
		}
		/// <summary>
		/// DeleteRule: typeof(Objectification), Priority=1;
		/// Update the fact type name when an objectification is deleted
		/// </summary>
		private static void ValidateFactTypeNameForObjectificationDeleteRule(ElementDeletedEventArgs e)
		{
			ProcessValidateFactNameForObjectificationDelete(e.ModelElement as Objectification, null, null);
		}
		/// <summary>
		/// Rule helper method
		/// </summary>
		private static void ProcessValidateFactNameForObjectificationDelete(Objectification link, FactType nestedFactType, ObjectType nestingObjectType)
		{
			if (nestedFactType == null)
			{
				nestedFactType = link.NestedFactType;
			}
			if (nestingObjectType == null)
			{
				nestingObjectType = link.NestingType;
			}
			if (!nestedFactType.IsDeleted && (nestingObjectType.IsDeleted || nestingObjectType.Name.Length != 0))
			{
				nestedFactType.RegenerateErrorText();
				nestedFactType.OnFactTypeNameChanged();
			}
		}
		/// <summary>
		/// RolePlayerChangeRule: typeof(Objectification), Priority=1;
		/// </summary>
		private static void ValidateFactTypeNameForObjectificationRolePlayerChangeRule(RolePlayerChangedEventArgs e)
		{
			FactType oldFactType = null;
			ObjectType oldObjectType = null;
			Objectification link = e.ElementLink as Objectification;
			bool resetObjectTypeName;
			if (e.DomainRole.Id == Objectification.NestingTypeDomainRoleId)
			{
				oldObjectType = (ObjectType)e.OldRolePlayer;
				oldFactType = link.NestedFactType;
				resetObjectTypeName = false; // Leave the old object type name if we grab it
			}
			else
			{
				oldFactType = (FactType)e.OldRolePlayer;
				oldObjectType = link.NestingType;
				resetObjectTypeName = true;
			}
			if (resetObjectTypeName)
			{
				string oldFactTypeName = oldFactType.myGeneratedName;
				resetObjectTypeName = !string.IsNullOrEmpty(oldFactTypeName) && oldFactTypeName == oldObjectType.Name;
			}
			ProcessValidateFactNameForObjectificationDelete(link, oldFactType, oldObjectType);
			ProcessValidateFactNameForObjectificationAdded(link);
			if (resetObjectTypeName)
			{
				link.NestingType.Name = "";
			}
		}
		/// <summary>
		/// RolePlayerPositionChangeRule: typeof(FactTypeHasRole)
		/// </summary>
		private static void FactTypeHasRoleOrderChangedRule(RolePlayerOrderChangedEventArgs e)
		{
			if (e.SourceDomainRole.Id == FactTypeHasRole.FactTypeDomainRoleId)
			{
				((FactType)e.SourceElement).DelayValidateNamePartChanged();
			}
		}
		/// <summary>
		/// ChangeRule: typeof(Role)
		/// Track query signature change
		/// </summary>
		private static void RoleNameChangedRule(ElementPropertyChangedEventArgs e)
		{
			if (e.DomainProperty.Id == Role.NameDomainPropertyId)
			{
				QueryBase queryBase;
				if (null != (queryBase = ((Role)e.ModelElement).FactType as QueryBase))
				{
					queryBase.DelayValidateNamePartChanged();
				}
			}
		}
		/// <summary>
		/// AddRule: typeof(QueryDefinesParameter)
		/// Track query signature change
		/// </summary>
		private static void QueryParameterAddedRule(ElementAddedEventArgs e)
		{
			((QueryDefinesParameter)e.ModelElement).Query.DelayValidateNamePartChanged();
		}
		/// <summary>
		/// ChangeRule: typeof(QueryParameter)
		/// Track query signature change
		/// </summary>
		private static void QueryParameterChangedRule(ElementPropertyChangedEventArgs e)
		{
			QueryBase query;
			if (e.DomainProperty.Id == QueryParameter.NameDomainPropertyId &&
				null != (query = ((QueryParameter)e.ModelElement).Query))
			{
				query.DelayValidateNamePartChanged();
			}
		}
		/// <summary>
		/// DeleteRule: typeof(QueryDefinesParameter)
		/// Track query signature change
		/// </summary>
		private static void QueryParameterDeletedRule(ElementDeletedEventArgs e)
		{
			QueryBase query = ((QueryDefinesParameter)e.ModelElement).Query;
			if (!query.IsDeleted)
			{
				query.DelayValidateNamePartChanged();
			}
		}
		/// <summary>
		/// RolePlayerPositionChangeRule: typeof(QueryDefinesParameter)
		/// Track query signature change
		/// </summary>
		private static void QueryParameterOrderChangedRule(RolePlayerOrderChangedEventArgs e)
		{
			if (e.SourceDomainRole.Id == QueryDefinesParameter.QueryDomainRoleId)
			{
				((QueryBase)e.SourceElement).DelayValidateNamePartChanged();
			}
		}
		/// <summary>
		/// AddRule: typeof(QueryParameterHasParameterType)
		/// Track query signature change
		/// </summary>
		private static void QueryParameterTypeAddedRule(ElementAddedEventArgs e)
		{
			QueryBase query;
			if (null != (query = ((QueryParameterHasParameterType)e.ModelElement).Parameter.Query))
			{
				query.DelayValidateNamePartChanged();
			}
		}
		/// <summary>
		/// DeleteRule: typeof(QueryParameterHasParameterType)
		/// Track query signature change
		/// </summary>
		private static void QueryParameterTypeDeletedRule(ElementDeletedEventArgs e)
		{
			QueryBase query;
			QueryParameter parameter = ((QueryParameterHasParameterType)e.ModelElement).Parameter;
			if (!parameter.IsDeleted &&
				null != (query = parameter.Query))
			{
				query.DelayValidateNamePartChanged();
			}
		}
		/// <summary>
		/// RolePlayerChangeRule: typeof(QueryParameterHasParameterType)
		/// Track query signature change
		/// </summary>
		private static void QueryParameterTypeRolePlayerChangedRule(RolePlayerChangedEventArgs e)
		{
			if (e.DomainRole.Id == QueryParameterHasParameterType.ParameterTypeDomainRoleId)
			{
				QueryBase query;
				if (null != (query = ((QueryParameterHasParameterType)e.ElementLink).Parameter.Query))
				{
					query.DelayValidateNamePartChanged();
				}
			}
		}
		#endregion // Model Validation Rules
		#region AutoFix Methods
		/// <summary>
		/// Remove implied (including duplicate) internal uniqueness constraints. Internal
		/// uniqueness constraint A implies internal uniqueness constraint B if the roles of
		/// A form a subset of the roles of B. Running this method will fix a
		/// FactTypeHasImpliedInternalUniquenessConstraintError on this FactType.
		/// </summary>
		public void RemoveImpliedInternalUniquenessConstraints()
		{
			int iucCount = GetInternalConstraintsCount(ConstraintType.InternalUniqueness);
			if (iucCount == 0)
			{
				return;
			}
			using (Transaction t = Store.TransactionManager.BeginTransaction(ResourceStrings.RemoveImpliedInternalUniquenessConstraintsTransactionName))
			{
				LinkedElementCollection<RoleBase> factRoles = RoleCollection;
				UniquenessConstraint[] iuc = new UniquenessConstraint[iucCount];
				const uint deonticBit = 1U << 31;
				uint[] roleBits = new uint[iucCount];
				int index = 0;
				foreach (UniquenessConstraint ic in GetInternalConstraints<UniquenessConstraint>())
				{
					iuc[index] = ic;
					uint bits = 0;
					LinkedElementCollection<Role> constraintRoles = ic.RoleCollection;
					int roleCount = constraintRoles.Count;
					for (int i = 0; i < roleCount; ++i)
					{
						bits |= 1U << factRoles.IndexOf(constraintRoles[i]);
					}
					if (bits != 0 && ic.Modality == ConstraintModality.Deontic)
					{
						bits |= deonticBit;
					}
					roleBits[index] = bits;
					++index;
				}
				int rbLength = roleBits.Length;
				uint left, right, compare;
				UniquenessConstraint leftIUC;
				UniquenessConstraint rightIUC;
				for (int i = 0; i < rbLength - 1; ++i)
				{
					leftIUC = iuc[i];
					if (leftIUC == null)
					{
						continue;
					}

					// Do duplicates first to simplify processing of implied cases
					left = roleBits[i];
					for (int j = i + 1; j < rbLength; ++j)
					{
						rightIUC = iuc[j];
						if (rightIUC == null)
						{
							continue;
						}
						right = roleBits[j];
						compare = left & right;
						if (0 != ((left ^ right) & deonticBit))
						{
							// Modality is different. The alethic constraint
							// should not be a subset of the deontic constraint,
							// but there are no restrictions the other way around.
							if ((compare == (left & ~deonticBit)) && (compare == (right & ~deonticBit)))
							{
								// Found a duplicate. Remove the deontic one
								if (0 != (left & deonticBit))
								{
									leftIUC.Delete();
									iuc[i] = null;
									left = 0;
									break;
								}
								else
								{
									rightIUC.Delete();
									iuc[j] = null;
								}
							}
						}
						else
						{
							if ((compare == left) && (compare == right))
							{
								// found a duplicate.
								// Remove the one on the right so we can
								// keep processing this element
								rightIUC.Delete();
								iuc[j] = null;
							}
						}
					}
					if (left == 0)
					{
						continue;
					}
					for (int j = i + 1; j < rbLength; ++j)
					{
						rightIUC = iuc[j];
						right = roleBits[j];
						if (rightIUC == null || right == 0)
						{
							continue;
						}

						compare = left & right;
						if (0 != ((left ^ right) & deonticBit))
						{
							// Modality is different. The alethic constraint
							// should not be a subset of the deontic constraint,
							// but there are no restrictions the other way around.
							if (compare == (left & ~deonticBit))
							{
								// left implies right unless left is deontic
								if (0 == (left & deonticBit))
								{
									rightIUC.Delete();
									iuc[j] = null;
								}
							}
							else if (compare == (right & ~deonticBit))
							{
								// right implies left unless right is deontic
								if (0 == (right & deonticBit))
								{
									leftIUC.Delete();
									iuc[i] = null;
									break;
								}
							}
						}
						else
						{
							if (compare == left)
							{
								// left implies right
								rightIUC.Delete();
								iuc[j] = null;
							}
							else if (compare == right)
							{
								// right implies left
								leftIUC.Delete();
								iuc[i] = null;
								break;
							}
						}
					}
				}
				if (t.HasPendingChanges)
				{
					t.Commit();
				}
			}
		}
		#endregion // AutoFix Methods
		#region ImpliedUniqueVerbalizer class
		/// <summary>
		/// Non-generated portions of verbalization helper used to verbalize a
		/// single-role internal uniqueness constraint on a proxy role.
		/// </summary>
		private partial class ImpliedUniqueVerbalizer
		{
			private FactType myFactType;
			private UniquenessConstraint myConstraint;
			public void Initialize(FactType factType, UniquenessConstraint constraint)
			{
				myFactType = factType;
				myConstraint = constraint;
			}
			private void DisposeHelper()
			{
				myFactType = null;
				myConstraint = null;
			}
			private FactType FactType
			{
				get
				{
					return myFactType;
				}
			}
			private LinkedElementCollection<Role> RoleCollection
			{
				get
				{
					return myConstraint.RoleCollection;
				}
			}
			private ConstraintModality Modality
			{
				get
				{
					return myConstraint.Modality;
				}
			}
			private bool IsPreferred
			{
				get
				{
					return myConstraint.IsPreferred;
				}
			}
			#region Equality Overrides
			// Override equality operators so that muliple uses of the verbalization helper
			// for this object with different values does not trigger an 'already verbalized'
			// response for later verbalizations.
			/// <summary>
			/// Standard equality override
			/// </summary>
			public override int GetHashCode()
			{
				return Utility.GetCombinedHashCode(myFactType != null ? myFactType.GetHashCode() : 0, myConstraint != null ? myConstraint.GetHashCode() : 0);
			}
			/// <summary>
			/// Standard equality override
			/// </summary>
			public override bool Equals(object obj)
			{
				ImpliedUniqueVerbalizer other;
				return (null != (other = obj as ImpliedUniqueVerbalizer)) && other.myFactType == myFactType && other.myConstraint == myConstraint;
			}
			#endregion // Equality Overrides
		}
		#endregion // ImpliedUniqueVerbalizer class
		#region ImpliedMandatoryVerbalizer class
		/// <summary>
		/// Non-generated portions of verbalization helper used to verbalize a
		/// simple mandatory constraint on a proxy role.
		/// </summary>
		private partial class ImpliedMandatoryVerbalizer
		{
			private FactType myFactType;
			private MandatoryConstraint myConstraint;
			public void Initialize(FactType factType, MandatoryConstraint constraint)
			{
				myFactType = factType;
				myConstraint = constraint;
			}
			private void DisposeHelper()
			{
				myFactType = null;
				myConstraint = null;
			}
			private FactType FactType
			{
				get
				{
					return myFactType;
				}
			}
			private LinkedElementCollection<Role> RoleCollection
			{
				get
				{
					return myConstraint.RoleCollection;
				}
			}
			private ConstraintModality Modality
			{
				get
				{
					return myConstraint.Modality;
				}
			}
			#region Equality Overrides
			// Override equality operators so that muliple uses of the verbalization helper
			// for this object with different values does not trigger an 'already verbalized'
			// response for later verbalizations.
			/// <summary>
			/// Standard equality override
			/// </summary>
			public override int GetHashCode()
			{
				return Utility.GetCombinedHashCode(myFactType != null ? myFactType.GetHashCode() : 0, myConstraint != null ? myConstraint.GetHashCode() : 0);
			}
			/// <summary>
			/// Standard equality override
			/// </summary>
			public override bool Equals(object obj)
			{
				ImpliedMandatoryVerbalizer other;
				return (null != (other = obj as ImpliedMandatoryVerbalizer)) && other.myFactType == myFactType && other.myConstraint == myConstraint;
			}
			#endregion // Equality Overrides
		}
		#endregion // ImpliedMandatoryVerbalizer class
		#region CombinedMandatoryUniqueVerbalizer class
		/// <summary>
		/// Non-generated portions of verbalization helper used to verbalize a
		/// combined internal uniqueness constraint and simple mandatory constraint.
		/// </summary>
		private partial class CombinedMandatoryUniqueVerbalizer : IModelErrorOwner
		{
			private FactType myFactType;
			private UniquenessConstraint myUniquenessConstraint;
			private MandatoryConstraint myMandatoryConstraint;
			public void Initialize(FactType factType, UniquenessConstraint uniquenessConstraint, MandatoryConstraint mandatoryConstraint)
			{
				myFactType = factType;
				myUniquenessConstraint = uniquenessConstraint;
				myMandatoryConstraint = mandatoryConstraint;
			}
			private void DisposeHelper()
			{
				myFactType = null;
				myUniquenessConstraint = null;
				myMandatoryConstraint = null;
			}
			private FactType FactType
			{
				get
				{
					return myFactType;
				}
			}
			private LinkedElementCollection<Role> RoleCollection
			{
				get
				{
					return myUniquenessConstraint.RoleCollection;
				}
			}
			private ConstraintModality Modality
			{
				get
				{
					return myUniquenessConstraint.Modality;
				}
			}

			#region IModelErrorOwner Implementation
			IEnumerable<ModelErrorUsage> IModelErrorOwner.GetErrorCollection(ModelErrorUses filter)
			{
				// Defer to the errors on the mandatory and uniqueness constraint
				foreach (ModelErrorUsage usage in ((IModelErrorOwner)myMandatoryConstraint).GetErrorCollection(filter))
				{
					yield return usage;
				}
				foreach (ModelErrorUsage usage in ((IModelErrorOwner)myUniquenessConstraint).GetErrorCollection(filter))
				{
					yield return usage;
				}
			}
			void IModelErrorOwner.ValidateErrors(INotifyElementAdded notifyAdded)
			{
			}
			void IModelErrorOwner.DelayValidateErrors()
			{
			}
			#endregion // IModelErrorOwner Implementation
			#region Equality Overrides
			// Override equality operators so that muliple uses of the verbalization helper
			// for this object with different values does not trigger an 'already verbalized'
			// response for later verbalizations.
			/// <summary>
			/// Standard equality override
			/// </summary>
			public override int GetHashCode()
			{
				return Utility.GetCombinedHashCode(myFactType != null ? myFactType.GetHashCode() : 0, myMandatoryConstraint != null ? myMandatoryConstraint.GetHashCode() : 0, myUniquenessConstraint != null ? myUniquenessConstraint.GetHashCode() : 0);
			}
			/// <summary>
			/// Standard equality override
			/// </summary>
			public override bool Equals(object obj)
			{
				CombinedMandatoryUniqueVerbalizer other;
				return (null != (other = obj as CombinedMandatoryUniqueVerbalizer)) && other.myFactType == myFactType && other.myMandatoryConstraint == myMandatoryConstraint && other.myMandatoryConstraint == myMandatoryConstraint;
			}
			#endregion // Equality Overrides
		}
		#endregion // CombinedMandatoryUniqueVerbalizer class
		#region FactTypeInstanceVerbalizer class
		/// <summary>
		/// Non-generated portions of verbalization helper used to verbalize a
		/// combined internal uniqueness constraint and simple mandatory constraint.
		/// </summary>
		private partial class FactTypeInstanceVerbalizer
		{
			private FactType myFactType;
			private FactTypeInstance myInstance;
			private bool myDisplayIdentifier;
			public void Initialize(FactType factType, FactTypeInstance factInstance, bool displayIdentifier)
			{
				myFactType = factType;
				myInstance = factInstance;
				myDisplayIdentifier = displayIdentifier;
			}
			private void DisposeHelper()
			{
				myFactType = null;
				myInstance = null;
			}
			private FactType FactType
			{
				get
				{
					return myFactType;
				}
			}
			private FactTypeInstance Instance
			{
				get
				{
					return myInstance;
				}
			}
			private bool DisplayIdentifier
			{
				get
				{
					return myDisplayIdentifier;
				}
			}
			#region Equality Overrides
			// Override equality operators so that muliple uses of the verbalization helper
			// for this object with different values does not trigger an 'already verbalized'
			// response for later verbalizations.
			/// <summary>
			/// Standard equality override
			/// </summary>
			public override int GetHashCode()
			{
				return Utility.GetCombinedHashCode(myFactType != null ? myFactType.GetHashCode() : 0, myInstance != null ? myInstance.GetHashCode() : 0, myDisplayIdentifier.GetHashCode());
			}
			/// <summary>
			/// Standard equality override
			/// </summary>
			public override bool Equals(object obj)
			{
				FactTypeInstanceVerbalizer other;
				return (null != (other = obj as FactTypeInstanceVerbalizer)) && other.myFactType == myFactType && other.myInstance == myInstance && other.myDisplayIdentifier == myDisplayIdentifier;
			}
			#endregion // Equality Overrides
		}
		#endregion // FactTypeInstanceVerbalizer class
		#region DerivedElementsVerbalizer class
		/// <summary>
		/// Helper class for verbalization elements derived using a fact type.
		/// </summary>
		protected partial class DerivedElementsVerbalizer
		{
			#region DerivedElementComparer class
			private sealed class DerivedElementComparer : IComparer<ORMModelElement>
			{
				public static IComparer<ORMModelElement> Instance = new DerivedElementComparer();
				private DerivedElementComparer() { }

				private static Dictionary<Type, int> myOwnerTypeSortIndex = null;
				private static Dictionary<Type, int> OwnerTypeSortIndex
				{
					get
					{
						Dictionary<Type, int> retVal = myOwnerTypeSortIndex;
						if (retVal == null)
						{
							retVal = new Dictionary<Type, int>();
							retVal[typeof(FactType)] = 1;
							retVal[typeof(SubtypeFact)] = 1;
							retVal[typeof(ObjectType)] = 2;
							retVal[typeof(FrequencyConstraint)] = 3;
							retVal[typeof(RingConstraint)] = 3;
							retVal[typeof(UniquenessConstraint)] = 3;
							retVal[typeof(ValueComparisonConstraint)] = 3;
							retVal[typeof(SetComparisonConstraintRoleSequence)] = 4;
							if (null != System.Threading.Interlocked.CompareExchange<Dictionary<Type, int>>(ref myOwnerTypeSortIndex, retVal, null))
							{
								// Some other thread beat us, abandon our value
								retVal = myOwnerTypeSortIndex;
							}
						}
						return retVal;
					}
				}
				int IComparer<ORMModelElement>.Compare(ORMModelElement left, ORMModelElement right)
				{
					if ((object)left == right)
					{
						return 0;
					}
					Dictionary<Type, int> sortByType = OwnerTypeSortIndex;
					int leftType;
					int rightType;
					if (!sortByType.TryGetValue(left.GetType(), out leftType))
					{
						leftType = 5; // Sanity, should be a dead code path
					}
					if (!sortByType.TryGetValue(right.GetType(), out rightType))
					{
						rightType = 5; // Sanity, should be a dead code path
					}
					if (leftType == rightType)
					{
						int retVal = 0;
						switch (leftType)
						{
							case 1: // Fact type, sort by default reading signature
								{
									IReading leftVirtualReading = ((FactType)left).GetDefaultReading();
									IReading rightVirtualReading = ((FactType)right).GetDefaultReading();
									Reading leftReading = leftVirtualReading as Reading;
									Reading rightReading = rightVirtualReading as Reading;
									retVal = string.Compare(
										leftReading != null ? leftReading.Signature : Reading.GenerateReadingSignature(leftVirtualReading.Text, leftVirtualReading.RoleCollection, false),
										rightReading != null ? rightReading.Signature : Reading.GenerateReadingSignature(rightVirtualReading.Text, rightVirtualReading.RoleCollection, false),
										StringComparison.CurrentCultureIgnoreCase);
								}
								break;
							case 2: // Object type and set constraint, sort by name
							case 3:
								retVal = string.Compare(((ORMNamedElement)left).Name, ((ORMNamedElement)right).Name, StringComparison.CurrentCultureIgnoreCase);
								break;
							case 4: // Comparison constraint sequences.
								{
									SetComparisonConstraintRoleSequence leftSequence = (SetComparisonConstraintRoleSequence)left;
									SetComparisonConstraintRoleSequence rightSequence = (SetComparisonConstraintRoleSequence)right;
									SetComparisonConstraint leftConstraint = leftSequence.ExternalConstraint;
									SetComparisonConstraint rightConstraint = rightSequence.ExternalConstraint;
									if (leftConstraint == rightConstraint)
									{
										LinkedElementCollection<SetComparisonConstraintRoleSequence> orderedSequences = leftConstraint.RoleSequenceCollection;
										retVal = orderedSequences.IndexOf(leftSequence) - orderedSequences.IndexOf(rightSequence);
									}
									else
									{
										retVal = string.Compare(leftConstraint.Name, rightConstraint.Name, StringComparison.CurrentCultureIgnoreCase);
									}
								}
								break;
						}
						// Not much else to do, fallback on id
						return retVal != 0 ? retVal : left.Id.CompareTo(right.Id);
					}
					return leftType - rightType;
				}
			}
			#endregion // DerivedElementComparer class

			private IList<ORMModelElement> myDerivedElements;
			private FactType myFactType;

			/// <summary>
			/// Get a verbalizer instance with the normalized (sorted and filtered for ability to verbalized)
			/// derived elements for a given fact type.
			/// </summary>
			/// <param name="factType">The context fact type</param>
			/// <param name="ownerKind">The kind of elements to retrieve.</param>
			/// <returns>A verbalizer or null.</returns>
			public static DerivedElementsVerbalizer GetNormalizedVerbalizer(FactType factType, RolePathOwnerKind ownerKind)
			{
				ORMModelElement[] derivedElements;
				int derivedElementCount;
				if (0 != (derivedElementCount = (derivedElements = RolePathVerbalizer.GetUsedByRolePathOwners(factType, ownerKind)).Length))
				{
					if (derivedElementCount > 1)
					{
						Array.Sort<ORMModelElement>(derivedElements, DerivedElementComparer.Instance);
					}

					// Make sure everything is verbalizable
					List<ORMModelElement> replacementList = null;
					for (int i = 0; i < derivedElementCount; ++i)
					{
						FactType derivedFactType = derivedElements[i] as FactType;
						if (derivedFactType == null) // These are sorted now, fact types are first and are the only elements that might not be verbalizable, stop looking otherwise
						{
							if (replacementList != null)
							{
								for (int j = i; j < derivedElementCount; ++j)
								{
									replacementList.Add(derivedElements[j]);
								}
							}
						}
						else if (derivedFactType.ReadingRequiredError != null)
						{
							if (replacementList == null)
							{
								replacementList = new List<ORMModelElement>(); // OK if this empty, we'll check at the end
								for (int j = 0; j < i; ++j)
								{
									replacementList.Add(derivedElements[j]);
								}
							}
						}
						else if (replacementList != null)
						{
							replacementList.Add(factType);
						}
					}

					IList<ORMModelElement> normalizedList;
					if (replacementList == null)
					{
						normalizedList = derivedElements;
					}
					else if (replacementList.Count == 0)
					{
						return null; // Nothing left that can be verbalized
					}
					else
					{
						normalizedList = replacementList;
					}
					DerivedElementsVerbalizer retVal = DerivedElementsVerbalizer.GetVerbalizer();
					retVal.Initialize(factType, normalizedList);
					return retVal;
				}
				return null;
			}
			/// <summary>
			/// Initialize this instance
			/// </summary>
			/// <param name="factType">The context fact type</param>
			/// <param name="derivedElements">A non-empty set of derived elements.</param>
			public void Initialize(FactType factType, IList<ORMModelElement> derivedElements)
			{
				myFactType = factType;
				myDerivedElements = derivedElements;
			}
			private void DisposeHelper()
			{
				myFactType = null;
				myDerivedElements = null;
			}
			private IList<ORMModelElement> DerivedElements
			{
				get
				{
					return myDerivedElements;
				}
			}
			#region Equality Overrides
			// Override equality operators so that muliple uses of the verbalization helper
			// for this object with different values does not trigger an 'already verbalized'
			// response for later verbalizations.
			/// <summary>
			/// Standard equality override
			/// </summary>
			public override int GetHashCode()
			{
				// This is initialized with derived elements that are functionally determined
				// by the fact type itself. Just use the fact type for equality.
				return myFactType != null ? myFactType.GetHashCode() : 0;
			}
			/// <summary>
			/// Standard equality override
			/// </summary>
			public override bool Equals(object obj)
			{
				DerivedElementsVerbalizer other;
				return (null != (other = obj as DerivedElementsVerbalizer)) && other.myFactType == myFactType;
			}
			#endregion // Equality Overrides
		}
		#endregion // DerivedElementsVerbalizer class
		#region IVerbalizeFilterChildrenByRole Implementation
		/// <summary>
		/// Implements IVerbalizeFilterChildrenByRole.BlockEmbeddedVerbalization.
		/// Roles are treated as custom children
		/// </summary>
		protected bool BlockEmbeddedVerbalization(DomainRoleInfo embeddingRole)
		{
			return embeddingRole.Id == FactTypeHasRole.FactTypeDomainRoleId;
		}
		bool IVerbalizeFilterChildrenByRole.BlockEmbeddedVerbalization(DomainRoleInfo embeddingRole)
		{
			return BlockEmbeddedVerbalization(embeddingRole);
		}
		#endregion // IVerbalizeFilterChildrenByRole Implementation
		#region IVerbalizeCustomChildren Implementation
		/// <summary>
		/// Implements IVerbalizeCustomChildren.GetCustomChildVerbalizations. Responsible
		/// for internal constraints, combinations of internals, and defaults
		/// </summary>
		protected IEnumerable<CustomChildVerbalizer> GetCustomChildVerbalizations(IVerbalizeFilterChildren filter, IDictionary<string, object> verbalizationOptions, string verbalizationTarget, VerbalizationSign sign)
		{
			if (ReadingRequiredError != null)
			{
				yield break;
			}
			IList<RoleBase> orderedRoles = GetDefaultReading().RoleCollection;
			int readingRoleCount = orderedRoles.Count;
			for (int iRole = 0; iRole < readingRoleCount; ++iRole)
			{
				yield return CustomChildVerbalizer.VerbalizeInstanceWithChildren(orderedRoles[iRole].Role, DeferVerbalizationOptions.None, null);
			}

			LinkedElementCollection<SetConstraint> setConstraints = SetConstraintCollection;
			int setConstraintCount = setConstraints.Count;
			if (setConstraintCount != 0)
			{
				// All internal constraints (and combinations) are non-aggregated, so they
				// are verbalized as custom children.
				bool isNegative = 0 != (sign & VerbalizationSign.Negative);
				bool lookForDefault = (bool)verbalizationOptions[CoreVerbalizationOption.ShowDefaultConstraint];
				bool lookForCombined = !isNegative && (bool)verbalizationOptions[CoreVerbalizationOption.CombineSimpleMandatoryAndUniqueness];

				LinkedElementCollection<RoleBase> factRoles = RoleCollection;
				int? unaryRoleIndex = FactType.GetUnaryRoleIndex(factRoles);
				bool isUnaryFactType = unaryRoleIndex.HasValue;
				if (!isUnaryFactType && 2 == factRoles.Count)
				{
					Role[] roles = new Role[2];
					RoleProxy proxy = null;
					int primaryRoleCount;
					if (ImpliedByObjectification == null)
					{
						roles[0] = (Role)factRoles[0];
						roles[1] = (Role)factRoles[1];
						primaryRoleCount = 2;
					}
					// Find the proxy, and put the opposite role in the 0 slot
					else if (null != (proxy = factRoles[0] as RoleProxy))
					{
						roles[0] = (Role)factRoles[1];
						primaryRoleCount = 1;
					}
					else if (null != (proxy = factRoles[1] as RoleProxy))
					{
						roles[0] = (Role)factRoles[0];
						primaryRoleCount = 1;
					}
					// Fallback case for unary objectification, which does not use a proxy
					else
					{
						roles[0] = (Role)factRoles[0];
						roles[1] = (Role)factRoles[1];
						primaryRoleCount = 2;
					}
					// Array of single role constraints.
					//    Index 1 == Left/Right
					//    Index 2 == Alethic/Deontic
					//    Index 3 == Unique/Mandatory
					SetConstraint[, ,] singleRoleConstraints = new SetConstraint[2, 2, 2];

					// A single-role uniqueness constraint with an implied default
					SetConstraint constraintWithImpliedOppositeDefault = null;

					// Don't run loop at end if we don't have anything to verbalize
					bool haveSingles = false;

					for (int i = 0; i < setConstraintCount; ++i)
					{
						SetConstraint constraint = setConstraints[i];
						IConstraint iConstraint = constraint.Constraint;
						if (iConstraint.ConstraintIsInternal &&
							(filter == null || !filter.FilterChildVerbalizer(constraint, sign).IsBlocked))
						{
							LinkedElementCollection<Role> constraintRoles = constraint.RoleCollection;
							if (constraintRoles.Count == 1)
							{
								// Handle singles specially at the end so that
								// the verbalization order is less dependent on the
								// constraint order in the model
								Role constraintRole = constraintRoles[0];
								for (int iRole = 0; iRole < primaryRoleCount; ++iRole)
								{
									if (constraintRole == roles[iRole])
									{
										int modalityIndex = (constraint.Modality == ConstraintModality.Alethic) ? 0 : 1;
										int constraintIndex = (iConstraint.ConstraintType == ConstraintType.InternalUniqueness) ? 0 : 1;
										if (singleRoleConstraints[iRole, modalityIndex, constraintIndex] == null) // Skip duplicate
										{
											singleRoleConstraints[iRole, modalityIndex, constraintIndex] = constraint;
											haveSingles = true;
											if (lookForDefault && modalityIndex == 0 && constraintIndex == 0) // Alethic Uniqueness constraint
											{
												if (constraintWithImpliedOppositeDefault != null)
												{
													lookForDefault = false;
													constraintWithImpliedOppositeDefault = null;
												}
												else
												{
													constraintWithImpliedOppositeDefault = constraint;
												}
											}
										}
									}
								}
							}
							else
							{
								yield return CustomChildVerbalizer.VerbalizeInstance((IVerbalize)constraint);
							}
						}
					}

					if (proxy != null)
					{
						// Pick up single role internal constraints from the proxy target role and
						// add them to the second column
						LinkedElementCollection<ConstraintRoleSequence> sequences = proxy.TargetRole.ConstraintRoleSequenceCollection;
						int sequenceCount = sequences.Count;
						for (int i = 0; i < sequenceCount; ++i)
						{
							SetConstraint constraint = sequences[i] as SetConstraint;
							IConstraint iConstraint;
							if (constraint != null &&
								(iConstraint = constraint.Constraint).ConstraintIsInternal &&
								constraint.RoleCollection.Count == 1)
							{
								int modalityIndex = (constraint.Modality == ConstraintModality.Alethic) ? 0 : 1;
								int constraintIndex = (iConstraint.ConstraintType == ConstraintType.InternalUniqueness) ? 0 : 1;
								if (singleRoleConstraints[1, modalityIndex, constraintIndex] == null) // Skip duplicate
								{
									singleRoleConstraints[1, modalityIndex, constraintIndex] = constraint;
									haveSingles = true;
									if (lookForDefault && modalityIndex == 0 && constraintIndex == 0) // Alethic Uniqueness constraint
									{
										if (constraintWithImpliedOppositeDefault != null)
										{
											lookForDefault = false;
											constraintWithImpliedOppositeDefault = null;
										}
										else
										{
											constraintWithImpliedOppositeDefault = constraint;
										}
									}
								}
							}
						}
					}

					if (haveSingles)
					{
						// Walk the single role constraints and try to combine them
						// Group by modality/constraintType/role position
						for (int modalityIndex = 0; modalityIndex < 2; ++modalityIndex)
						{
							for (int roleIndex = 0; roleIndex < 2; ++roleIndex)
							{
								UniquenessConstraint uniquenessConstraint = singleRoleConstraints[roleIndex, modalityIndex, 0] as UniquenessConstraint;
								MandatoryConstraint mandatoryConstraint = singleRoleConstraints[roleIndex, modalityIndex, 1] as MandatoryConstraint;
								if (lookForCombined)
								{
									if (uniquenessConstraint != null && mandatoryConstraint != null)
									{
										// Combine verbalizations into one
										CombinedMandatoryUniqueVerbalizer verbalizer = CombinedMandatoryUniqueVerbalizer.GetVerbalizer();
										verbalizer.Initialize(this, uniquenessConstraint, mandatoryConstraint);
										yield return CustomChildVerbalizer.VerbalizeInstance(verbalizer, true);
									}
									else if (uniquenessConstraint != null)
									{
										if (roleIndex == 1 && proxy != null)
										{
											// Make sure the readings come from the implied fact
											ImpliedUniqueVerbalizer verbalizer = ImpliedUniqueVerbalizer.GetVerbalizer();
											verbalizer.Initialize(this, uniquenessConstraint);
											yield return CustomChildVerbalizer.VerbalizeInstance(verbalizer, true);
										}
										else
										{
											yield return CustomChildVerbalizer.VerbalizeInstance(uniquenessConstraint);
										}
									}
									else if (mandatoryConstraint != null)
									{
										if (roleIndex == 1 && proxy != null)
										{
											// Make sure the readings come from the implied fact
											ImpliedMandatoryVerbalizer verbalizer = ImpliedMandatoryVerbalizer.GetVerbalizer();
											verbalizer.Initialize(this, mandatoryConstraint);
											yield return CustomChildVerbalizer.VerbalizeInstance(verbalizer, true);
										}
										else
										{
											yield return CustomChildVerbalizer.VerbalizeInstance(mandatoryConstraint);
										}
									}
								}
								else
								{
									if (uniquenessConstraint != null)
									{
										if (roleIndex == 1 && proxy != null)
										{
											// Make sure the readings come from the implied fact
											ImpliedUniqueVerbalizer verbalizer = ImpliedUniqueVerbalizer.GetVerbalizer();
											verbalizer.Initialize(this, uniquenessConstraint);
											yield return CustomChildVerbalizer.VerbalizeInstance(verbalizer, true);
										}
										else
										{
											yield return CustomChildVerbalizer.VerbalizeInstance(uniquenessConstraint);
										}
									}
									if (mandatoryConstraint != null)
									{
										if (roleIndex == 1 && proxy != null)
										{
											// Make sure the readings come from the implied fact
											ImpliedMandatoryVerbalizer verbalizer = ImpliedMandatoryVerbalizer.GetVerbalizer();
											verbalizer.Initialize(this, mandatoryConstraint);
											yield return CustomChildVerbalizer.VerbalizeInstance(verbalizer, true);
										}
										else
										{
											yield return CustomChildVerbalizer.VerbalizeInstance(mandatoryConstraint);
										}
									}
								}
							}
						}
					}

					if (constraintWithImpliedOppositeDefault != null)
					{
						DefaultBinaryMissingUniquenessVerbalizer verbalizer = DefaultBinaryMissingUniquenessVerbalizer.GetVerbalizer();
						verbalizer.Initialize(this, (UniquenessConstraint)constraintWithImpliedOppositeDefault);
						yield return CustomChildVerbalizer.VerbalizeInstance(verbalizer, true);
					}
				}
				else if (!isUnaryFactType)
				{
					// Easy case, just verbalize all internal constraints as entered
					for (int i = 0; i < setConstraintCount; ++i)
					{
						SetConstraint constraint = setConstraints[i];
						if (constraint.Constraint.ConstraintIsInternal &&
							(filter == null || !filter.FilterChildVerbalizer(constraint, sign).IsBlocked))
						{
							yield return CustomChildVerbalizer.VerbalizeInstance((IVerbalize)constraint);
						}
					}
				}
				else
				{
					// Verbalize the uniqueness constraint on the unary role
					UniquenessConstraint constraint;
					if (null != (constraint = factRoles[unaryRoleIndex.Value].Role.SingleRoleAlethicUniquenessConstraint) &&
						(filter == null || !filter.FilterChildVerbalizer(constraint, sign).IsBlocked))
					{
						yield return CustomChildVerbalizer.VerbalizeInstance((IVerbalize)constraint);
					}
				}
			}

			// Verbalize other single-facttype set constraints
			for (int i = 0; i < setConstraintCount; ++i)
			{
				SetConstraint constraint = setConstraints[i];
				switch (constraint.Constraint.ConstraintType)
				{
					case ConstraintType.Frequency: // UNDONE: Consider collapsing single-role frequency constraints with simple mandatories
					case ConstraintType.Ring:
						if (constraint.FactTypeCollection.Count == 1 &&
							(filter == null || !filter.FilterChildVerbalizer(constraint, sign).IsBlocked))
						{
							yield return CustomChildVerbalizer.VerbalizeInstance((IVerbalize)constraint);
						}
						break;
				}
			}

			// Verbalize instances
			LinkedElementCollection<FactTypeInstance> instances = FactTypeInstanceCollection;
			int instanceCount = instances.Count;
			if (instanceCount != 0)
			{
				ObjectType objectifyingType;
				UniquenessConstraint pid;
				bool displayIdentifier =
					null != (objectifyingType = NestingType) &&
					(null == (pid = objectifyingType.PreferredIdentifier) ||
					!pid.IsObjectifiedPreferredIdentifier);
				yield return CustomChildVerbalizer.VerbalizeInstance(FactTypeInstanceBlockStart.GetVerbalizer(), true);
				for (int i = 0; i < instanceCount; ++i)
				{
					FactTypeInstanceVerbalizer verbalizer = FactTypeInstanceVerbalizer.GetVerbalizer();
					verbalizer.Initialize(this, instances[i], displayIdentifier);
					yield return CustomChildVerbalizer.VerbalizeInstance(verbalizer, true);
				}
				yield return CustomChildVerbalizer.VerbalizeInstance(FactTypeInstanceBlockEnd.GetVerbalizer(), true);
			}

			// Verbalize a derivation note.
			// The derivation rule is verbalized with the fact type. Instead of making the verbalizer walk all derivation
			// rule children, we jump to the only thing we want to verbalize independently.
			FactTypeDerivationRule derivationRule;
			DerivationNote derivationNote;
			if (null != (derivationRule = DerivationRule as FactTypeDerivationRule) &&
				null != (derivationNote = derivationRule.DerivationNote))
			{
				yield return CustomChildVerbalizer.VerbalizeInstance(derivationNote, false);
			}

			DerivedElementsVerbalizer derivedElementsVerbalizer;
			if ((bool)verbalizationOptions[CoreVerbalizationOption.DerivedFromWithFactType] &&
				verbalizationTarget == ORMCoreDomainModel.VerbalizationTargetName &&
				null != (derivedElementsVerbalizer = DerivedElementsVerbalizer.GetNormalizedVerbalizer(this, RolePathOwnerKind.FactTypeDerivation | RolePathOwnerKind.SubtypeDerivation | RolePathOwnerKind.CustomJoinPath)))
			{
				yield return CustomChildVerbalizer.VerbalizeInstance(derivedElementsVerbalizer, true);
			}
		}
		IEnumerable<CustomChildVerbalizer> IVerbalizeCustomChildren.GetCustomChildVerbalizations(IVerbalizeFilterChildren filter, IDictionary<string, object> verbalizationOptions, string verbalizationTarget, VerbalizationSign sign)
		{
			return GetCustomChildVerbalizations(filter, verbalizationOptions, verbalizationTarget, sign);
		}
		#endregion // IVerbalizeCustomChildren Implementation
		#region DefaultBinaryMissingUniquenessVerbalizer
		private partial class DefaultBinaryMissingUniquenessVerbalizer
		{
			private FactType myFactType;
			private UniquenessConstraint myConstraint;
			public void Initialize(FactType factType, UniquenessConstraint constraint)
			{
				myFactType = factType;
				myConstraint = constraint;
			}
			private void DisposeHelper()
			{
				myFactType = null;
				myConstraint = null;
			}
			private FactType FactType
			{
				get
				{
					return myFactType;
				}
			}
			private LinkedElementCollection<Role> RoleCollection
			{
				get
				{
					return myConstraint.RoleCollection;
				}
			}
			private ConstraintModality Modality
			{
				get
				{
					return myConstraint.Modality;
				}
			}
			#region Equality Overrides
			// Override equality operators so that muliple uses of the verbalization helper
			// for this object with different values does not trigger an 'already verbalized'
			// response for later verbalizations.
			/// <summary>
			/// Standard equality override
			/// </summary>
			public override int GetHashCode()
			{
				return Utility.GetCombinedHashCode(myFactType != null ? myFactType.GetHashCode() : 0, myConstraint != null ? myConstraint.GetHashCode() : 0);
			}
			/// <summary>
			/// Standard equality override
			/// </summary>
			public override bool Equals(object obj)
			{
				DefaultBinaryMissingUniquenessVerbalizer other;
				return (null != (other = obj as DefaultBinaryMissingUniquenessVerbalizer)) && other.myFactType == myFactType && other.myConstraint == myConstraint;
			}
			#endregion // Equality Overrides
		}
		#endregion // DefaultBinaryMissingUniquenessVerbalizer
		#region IHierarchyContextEnabled Implementation
		/// <summary>
		/// Gets the contextable object that this instance should resolve to.
		/// </summary>
		/// <value>The forward context. Null if none</value>
		/// <remarks>For example a role should resolve to a fact type since a role is displayed with a fact type</remarks>
		protected static IHierarchyContextEnabled ForwardHierarchyContextTo
		{
			get
			{
				return null;
			}
		}
		/// <summary>
		/// Implements <see cref="IHierarchyContextEnabled.GetForcedHierarchyContextElements"/>.
		/// Returns all role players if minimal elements are called for, otherwise returns all
		/// related FactTypes, Subtypes, and Supertypes of an ObjectifiedFactType, plus the elements
		/// needed for display of those elements.
		/// </summary>
		protected IEnumerable<IHierarchyContextEnabled> GetForcedHierarchyContextElements(bool minimalElements)
		{
			foreach (RoleBase roleBase in RoleCollection)
			{
				ObjectType rolePlayer = roleBase.Role.RolePlayer;
				if (rolePlayer != null && !rolePlayer.IsImplicitBooleanValue)
				{
					yield return rolePlayer;
				}
			}
			if (!minimalElements)
			{
				ObjectType nestingType = NestingType;
				if (nestingType != null)
				{
					// Make sure an objectified FactType picks up its supertypes, subtypes, and
					// facttypes for directly played roles
					foreach (Role role in nestingType.PlayedRoleCollection)
					{
						SubtypeMetaRole subtypeRole;
						SupertypeMetaRole supertypeRole;
						FactType relatedFactType;
						if (null != (subtypeRole = role as SubtypeMetaRole))
						{
							yield return ((SubtypeFact)role.FactType).Supertype;
						}
						else if (null != (supertypeRole = role as SupertypeMetaRole))
						{
							yield return ((SubtypeFact)role.FactType).Subtype;
						}
						else if (null == (relatedFactType = role.FactType).ImpliedByObjectification)
						{
							yield return relatedFactType;
							foreach (RoleBase roleBase in relatedFactType.RoleCollection)
							{
								ObjectType rolePlayer = roleBase.Role.RolePlayer;
								if (rolePlayer != null &&
									rolePlayer != nestingType &&
									!rolePlayer.IsImplicitBooleanValue)
								{
									yield return rolePlayer;
								}
							}
						}
					}
				}
			}
		}
		/// <summary>
		/// Gets the place priority. The place priority specifies the order in which the element will
		/// be placed on the context diagram.
		/// </summary>
		/// <value>The place priority.</value>
		protected HierarchyContextPlacementPriority HierarchyContextPlacementPriority
		{
			get
			{
				if (this.Objectification == null)
				{
					return HierarchyContextPlacementPriority.Medium;
				}
				else
				{
					return HierarchyContextPlacementPriority.Higher;
				}
			}
		}
		/// <summary>
		/// Gets the number of generations to decriment when this object is walked.
		/// </summary>
		/// <value>The number of generations.</value>
		protected int HierarchyContextDecrementCount
		{
			get
			{
				if (NestingType != null)
				{
					return 1;
				}
				return 0;
			}
		}
		/// <summary>
		/// Gets a value indicating whether the path through the diagram should be followed through
		/// this element.
		/// </summary>
		/// <value><c>true</c> to continue walking; otherwise, <c>false</c>.</value>
		protected static bool ContinueWalkingHierarchyContext
		{
			get { return true; }
		}
		int IHierarchyContextEnabled.HierarchyContextDecrementCount
		{
			get { return HierarchyContextDecrementCount; }
		}
		bool IHierarchyContextEnabled.ContinueWalkingHierarchyContext
		{
			get { return ContinueWalkingHierarchyContext; }
		}
		IHierarchyContextEnabled IHierarchyContextEnabled.ForwardHierarchyContextTo
		{
			get { return ForwardHierarchyContextTo; }
		}
		IEnumerable<IHierarchyContextEnabled> IHierarchyContextEnabled.GetForcedHierarchyContextElements(bool minimalElements)
		{
			return GetForcedHierarchyContextElements(minimalElements);
		}
		HierarchyContextPlacementPriority IHierarchyContextEnabled.HierarchyContextPlacementPriority
		{
			get { return HierarchyContextPlacementPriority; }
		}
		ORMModel IHierarchyContextEnabled.Model
		{
			get
			{
				return ResolvedModel;
			}
		}
		bool IHierarchyContextEnabled.HierarchyDisabled
		{
			get
			{
				return HierarchyDisabled;
			}
		}
		/// <summary>
		/// Implements <see cref="IHierarchyContextEnabled.HierarchyDisabled"/>
		/// </summary>
		protected static bool HierarchyDisabled
		{
			get
			{
				return false;
			}
		}
		#endregion // IHierarchyContextEnabled Implementation
		#region UnaryFactTypeFixupListener
		/// <summary>
		/// This fixup listener handles the automatic binarization of unary facts that are stored as single-role facts.
		/// </summary>
		public static IDeserializationFixupListener UnaryFixupListener
		{
			get { return new UnaryFactTypeFixupListener(); }
		}
		private sealed class UnaryFactTypeFixupListener : DeserializationFixupListener<FactType>
		{
			public UnaryFactTypeFixupListener() : base((int)ORMDeserializationFixupPhase.ValidateImplicitStoredElements) { }

			protected override void ProcessElement(FactType element, Store store, INotifyElementAdded notifyAdded)
			{
				// Binarize any fact that is stored as a unary
				if (element.RoleCollection != null && element.RoleCollection.Count == 1)
				{
					UnaryBinarizationUtility.BinarizeUnary(element, notifyAdded);
				}
				else if (element.UnaryRole != null)
				{
					// Validate and fix any binarized unary facts
					UnaryBinarizationUtility.ProcessFactType(element, notifyAdded);
				}
			}
			protected override bool VerifyElementType(ModelElement element)
			{
				return !(element is QueryBase || element is SubtypeFact);
			}
		}
		#endregion
	}
	#region RolePlayer Hierarchy Context Navigation
	partial class ObjectTypePlaysRole : IHierarchyContextLinkFilter
	{
		#region IHierarchyContextLinkFilter Implementation
		/// <summary>
		/// Implements <see cref="IHierarchyContextLinkFilter.ContinueHierachyWalking"/>
		/// Block navigation from an object type to a role in a link fact type.
		/// </summary>
		protected bool ContinueHierachyWalking(DomainRoleInfo fromRoleInfo)
		{
			if (fromRoleInfo.Id == ObjectTypePlaysRole.RolePlayerDomainRoleId)
			{
				FactType factType = this.PlayedRole.FactType;
				return factType != null && factType.ImpliedByObjectification == null;
			}
			return true;
		}
		bool IHierarchyContextLinkFilter.ContinueHierachyWalking(DomainRoleInfo fromRoleInfo)
		{
			return ContinueHierachyWalking(fromRoleInfo);
		}
		#endregion // IHierarchyContextLinkFilter Implementation
	}
	#endregion // RolePlayer Hierarchy Context Navigation
	#region FactType Model Validation Errors
	#region class FactTypeRequiresReadingError
	[ModelErrorDisplayFilter(typeof(FactTypeDefinitionErrorCategory))]
	partial class FactTypeRequiresReadingError
	{
		#region Base overrides
		/// <summary>
		/// Creates error text for when a fact has no readings.
		/// </summary>
		public override void GenerateErrorText()
		{
			IModelErrorDisplayContext context = FactType;
			ErrorText = Utility.UpperCaseFirstLetter(string.Format(CultureInfo.InvariantCulture, ResourceStrings.ModelErrorFactTypeRequiresReadingMessage, context != null ? (context.ErrorDisplayContext ?? "") : ""));
		}
		/// <summary>
		/// Provide a compact error description
		/// </summary>
		public override string CompactErrorText
		{
			get
			{
				return ResourceStrings.ModelErrorFactTypeRequiresReadingMessageCompact;
			}
		}
		/// <summary>
		/// Sets regenerate to ModelNameChange | OwnerNameChange
		/// </summary>
		public override RegenerateErrorTextEvents RegenerateEvents
		{
			get
			{
				return RegenerateErrorTextEvents.ModelNameChange | RegenerateErrorTextEvents.OwnerNameChange;
			}
		}
		#endregion // Base overrides
	}
	#endregion // class FactTypeRequiresReadingError
	#region class FactTypeRequiresInternalUniquenessConstraintError
	[ModelErrorDisplayFilter(typeof(FactTypeDefinitionErrorCategory))]
	partial class FactTypeRequiresInternalUniquenessConstraintError
	{
		#region Base overrides
		/// <summary>
		/// Creates error text for when a fact lacks an internal uniqueness constraint.
		/// </summary>
		public override void GenerateErrorText()
		{
			IModelErrorDisplayContext context = FactType;
			ErrorText = Utility.UpperCaseFirstLetter(string.Format(CultureInfo.InvariantCulture, ResourceStrings.ModelErrorFactTypeRequiresInternalUniquenessConstraintMessage, context != null ? (context.ErrorDisplayContext ?? "") : ""));
		}
		/// <summary>
		/// Provide a compact error description
		/// </summary>
		public override string CompactErrorText
		{
			get
			{
				return ResourceStrings.ModelErrorFactTypeRequiresInternalUniquenessConstraintCompactMessage;
			}
		}
		/// <summary>
		/// Sets regenerate to ModelNameChange | OwnerNameChange
		/// </summary>
		public override RegenerateErrorTextEvents RegenerateEvents
		{
			get
			{
				return RegenerateErrorTextEvents.ModelNameChange | RegenerateErrorTextEvents.OwnerNameChange;
			}
		}
		#endregion // Base overrides
	}
	#endregion // class FactTypeRequiresInternalUniquenessConstraintError
	#region class NMinusOneError
	[ModelErrorDisplayFilter(typeof(FactTypeDefinitionErrorCategory))]
	public partial class NMinusOneError
	{
		#region Base overrides
		/// <summary>
		/// Generate text for the error
		/// </summary>
		public override void GenerateErrorText()
		{
			UniquenessConstraint iuc = Constraint;
			FactType factType = iuc.FactTypeCollection[0];
			ErrorText = Utility.UpperCaseFirstLetter(string.Format(CultureInfo.InvariantCulture, ResourceStrings.ModelErrorNMinusOneRuleInternalSpan, iuc.Name, ((IModelErrorDisplayContext)factType).ErrorDisplayContext ?? "", factType.RoleCollection.Count - 1));
		}
		/// <summary>
		/// Provide a compact error description
		/// </summary>
		public override string CompactErrorText
		{
			get
			{
				return string.Format(CultureInfo.InvariantCulture, ResourceStrings.ModelErrorNMinusOneRuleInternalSpanCompact, Constraint.FactTypeCollection[0].RoleCollection.Count - 1);
			}
		}

		/// <summary>
		/// Regenerate the error text when the constraint name changes
		/// </summary>
		public override RegenerateErrorTextEvents RegenerateEvents
		{
			get
			{
				return RegenerateErrorTextEvents.OwnerNameChange | RegenerateErrorTextEvents.ModelNameChange;
			}
		}
		#endregion // Base overrides
	}
	#endregion //class NMinusOneError

	#endregion // FactType Model Validation Errors
}
