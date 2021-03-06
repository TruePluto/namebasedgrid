﻿/*
------------------------------------------------------------------------------
This source file is a part of Name-Based Grid.

Copyright (c) 2015 Florian Haag

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
------------------------------------------------------------------------------
 */
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;

namespace NameBasedGrid
{
	/// <summary>
	/// A collection of <see cref="ColumnOrRowBase">column or row definitions</see>.
	/// </summary>
	public sealed class ColumnOrRowList : ObservableCollection<ColumnOrRowBase>
	{
		/// <summary>
		/// An object that listens to changes of properties of <see cref="ColumnOrRowBase"/> instances.
		/// </summary>
		private sealed class PropertyChangeListener : IColumnOrRowPropertyChangeListener
		{
			/// <summary>
			/// Initializes a new instance.
			/// </summary>
			/// <param name="owner">The object that gets notified about the processed changes.</param>
			/// <exception cref="ArgumentNullException"><paramref name="owner"/> is <see langword="null"/>.</exception>
			public PropertyChangeListener(ColumnOrRowList owner)
			{
				if (owner == null) {
					throw new ArgumentNullException("owner");
				}
				
				this.owner = owner;
			}
			
			/// <summary>
			/// The object that gets notified about the processed changes.
			/// </summary>
			private readonly ColumnOrRowList owner;
			
			/// <summary>
			/// Executes a method for each physical column or row.
			/// </summary>
			/// <param name="action">The method to execute once for each physical column or row.
			///   It receives the physical index and the <see cref="ColumnOrRow"/> object in its parameters.</param>
			/// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
			private void ForEachColumnOrRow(Action<int, ColumnOrRow> action)
			{
				if (action == null) {
					throw new ArgumentNullException("action");
				}
				
				int physicalIndex = 0;
				for (int i = 0; i < owner.Count; i++) {
					var cr = owner[i] as ColumnOrRow;
					if (cr != null) {
						action(physicalIndex, cr);
						physicalIndex++;
					}
				}
			}
			
			/// <summary>
			/// Processes a change notification.
			/// </summary>
			/// <param name="columnOrRow">The <see cref="ColumnOrRowBase"/> instance whose property value was changed.
			///   This must not be <see langword="null"/>.</param>
			/// <param name="property">The modified property.</param>
			public void PropertyChanged(ColumnOrRowBase columnOrRow, ColumnOrRowProperty property)
			{
				switch (property) {
					case ColumnOrRowProperty.Name:
						owner.InvalidateMaps();
						goto case ColumnOrRowProperty.ExtendTo;
					case ColumnOrRowProperty.StartAt:
					case ColumnOrRowProperty.ExtendTo:
						owner.UpdatePlacement();
						break;
					case ColumnOrRowProperty.Size:
						ForEachColumnOrRow((idx, cr) => {
						                   	if (cr == columnOrRow) {
						                   		owner.controller.SetSize(idx, cr.Size);
						                   	}
						                   });
						break;
					case ColumnOrRowProperty.MinSize:
						ForEachColumnOrRow((idx, cr) => {
						                   	if (cr == columnOrRow) {
						                   		owner.controller.SetMinSize(idx, cr.MinSize);
						                   	}
						                   });
						break;
					case ColumnOrRowProperty.MaxSize:
						ForEachColumnOrRow((idx, cr) => {
						                   	if (cr == columnOrRow) {
						                   		owner.controller.SetMaxSize(idx, cr.MaxSize);
						                   	}
						                   });
						break;
					case ColumnOrRowProperty.SharedSizeGroup:
						ForEachColumnOrRow((idx, cr) => {
						                   	if (cr == columnOrRow) {
						                   		owner.controller.SetSharedSizeGroup(idx, cr.SharedSizeGroup);
						                   	}
						                   });
						break;
				}
			}
		}
		
		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		/// <param name="controller">The controller used by the new instance.</param>
		/// <exception cref="ArgumentNullException"><paramref name="controller"/> is <see langword="null"/>.</exception>
		internal ColumnOrRowList(IColumnOrRowListController controller)
		{
			if (controller == null) {
				throw new ArgumentNullException("controller");
			}
			
			this.controller = controller;
			this.propertyChangeListener = new PropertyChangeListener(this);
			this.sourceListChangeListener = new SourceListChangeListener(this);
		}
		
		/// <summary>
		/// The controller used by this instance.
		/// </summary>
		private readonly IColumnOrRowListController controller;
		
		/// <summary>
		/// Maps column or row names to their respective <see cref="ColumnOrRowBase"/> definition objects. 
		/// </summary>
		/// <seealso cref="indexMap"/>
		/// <seealso cref="BuildMaps"/>
		/// <seealso cref="InvalidateMaps"/>
		private Dictionary<string, ColumnOrRowBase> namesMap;
		
		/// <summary>
		/// Maps column or row names to the physical indices in the internal <see cref="System.Windows.Controls.Grid"/>.
		/// </summary>
		/// <seealso cref="namesMap"/>
		/// <seealso cref="BuildMaps"/>
		/// <seealso cref="InvalidateMaps"/>
		private Dictionary<string, int> indexMap;
		
		/// <summary>
		/// Invalidates <see cref="namesMap"/> and <see cref="indexMap"/>.
		/// </summary>
		/// <seealso cref="BuildMaps"/>
		private void InvalidateMaps()
		{
			namesMap = null;
			indexMap = null;
		}
		
		/// <summary>
		/// Rebuilds <see cref="namesMap"/> and <see cref="indexMap"/> based on the current content of the list.
		/// </summary>
		/// <seealso cref="InvalidateMaps"/>
		private void BuildMaps()
		{
			namesMap = new Dictionary<string, ColumnOrRowBase>();
			indexMap = new Dictionary<string, int>();
			int currentIndex = 0;
			for (int i = 0; i < this.Count; i++) {
				var cr = this[i];
				string name = cr.Name;
				
				var physicalColumnOrRow = cr as ColumnOrRow;
				if (physicalColumnOrRow != null) {
					if (name != null) {
						indexMap[name] = currentIndex;
					}
					currentIndex++;
				}
				
				if (name != null) {
					namesMap[name] = cr;
				}
			}
		}
		
		/// <summary>
		/// Retrieves the physical index of a column or row based on its index in this list.
		/// </summary>
		/// <param name="index">The index of the column or row.</param>
		/// <returns>The physical index.</returns>
		/// <remarks>
		/// <para>This method retrieves the physical index of a column or row.
		///   The physical index is the index of the actual column or row definition in the internal <see cref="System.Windows.Controls.Grid"/> control.</para>
		/// </remarks>
		private int GetPhysicalIndex(int index)
		{
			int result = 0;
			for (int i = index - 1; i >= 0; i--) {
				if (this[i] is ColumnOrRow) {
					result++;
				}
			}
			return result;
		}
		
		/// <summary>
		/// The property change listener connected to this instance.
		/// </summary>
		private readonly PropertyChangeListener propertyChangeListener;
		
		/// <summary>
		/// Processes the insertion of an item.
		/// </summary>
		/// <param name="index">The index at which the item was inserted.</param>
		/// <param name="item">The newly inserted item.</param>
		/// <exception cref="ArgumentNullException"><paramref name="item"/> is <see langword="null"/>.</exception>
		/// <exception cref="InvalidOperationException">The method is invoked while the list retrieves its content from a source list.</exception>
		protected override void InsertItem(int index, ColumnOrRowBase item)
		{
			CheckModificationsAllowed();
			if (item == null) {
				throw new ArgumentNullException("item");
			}
			
			base.InsertItem(index, item);
			
			item.AddPropertyChangeListener(propertyChangeListener);
			
			var colOrRow = item as ColumnOrRow;
			if (colOrRow != null) {
				controller.ColumnOrRowInserted(GetPhysicalIndex(index), colOrRow);
			}
			InvalidateMaps();
			UpdatePlacement();
		}
		
		/// <summary>
		/// Processes the assignment of an item.
		/// </summary>
		/// <param name="index">The index to which the item was assigned.</param>
		/// <param name="item">The newly assigned item.</param>
		/// <exception cref="ArgumentNullException"><paramref name="item"/> is <see langword="null"/>.</exception>
		/// <exception cref="InvalidOperationException">The method is invoked while the list retrieves its content from a source list.</exception>
		protected override void SetItem(int index, ColumnOrRowBase item)
		{
			CheckModificationsAllowed();
			if (item == null) {
				throw new ArgumentNullException("item");
			}
			
			this[index].RemovePropertyChangeListener(propertyChangeListener);
			
			int physicalIndex = GetPhysicalIndex(index);
			
			var oldColumnOrRow = this[index] as ColumnOrRow;
			if (oldColumnOrRow != null) {
				controller.ColumnOrRowRemoved(physicalIndex);
			}
			
			base.SetItem(index, item);
			
			this[index].AddPropertyChangeListener(propertyChangeListener);
			
			var newColumnOrRow = this[index] as ColumnOrRow;
			if (newColumnOrRow != null) {
				controller.ColumnOrRowInserted(physicalIndex, newColumnOrRow);
			}
			
			InvalidateMaps();
			UpdatePlacement();
		}
		
		/// <summary>
		/// Processes the removal of all items.
		/// </summary>
		/// <exception cref="InvalidOperationException">The method is invoked while the list retrieves its content from a source list.</exception>
		protected override void ClearItems()
		{
			CheckModificationsAllowed();
			
			int physicalColumnOrRowCount = 0;
			for (int i = this.Count - 1; i >= 0; i--) {
				this[i].RemovePropertyChangeListener(propertyChangeListener);
				
				if (this[i] is ColumnOrRow) {
					physicalColumnOrRowCount++;
				}
			}
			for (int i = physicalColumnOrRowCount - 1; i >= 0; i--) {
				controller.ColumnOrRowRemoved(i);
			}
			
			base.ClearItems();
			
			InvalidateMaps();
		}
		
		/// <summary>
		/// Processes the removal of an item at a specific position.
		/// </summary>
		/// <param name="index">The index to remove.</param>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not a valid position in the collection.</exception>
		/// <exception cref="InvalidOperationException">The method is invoked while the list retrieves its content from a source list.</exception>
		protected override void RemoveItem(int index)
		{
			CheckModificationsAllowed();
			
			this[index].RemovePropertyChangeListener(propertyChangeListener);
			
			if (this[index] is ColumnOrRow) {
				controller.ColumnOrRowRemoved(GetPhysicalIndex(index));
			}
			
			base.RemoveItem(index);
			
			InvalidateMaps();
			UpdatePlacement();
		}
		
		/// <summary>
		/// Moves an item in the list.
		/// </summary>
		/// <param name="oldIndex">The old index of the item.</param>
		/// <param name="newIndex">The new index of the item.</param>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="oldIndex"/> is not a valid index in the collection.</exception>
		protected override void MoveItem(int oldIndex, int newIndex)
		{
			if (oldIndex == newIndex) {
				return;
			}
			
			CheckModificationsAllowed();
			
			var movedItem = this[oldIndex] as ColumnOrRow;
			if (movedItem != null) {
				int actualOldIndex = GetPhysicalIndex(oldIndex);
				int actualNewIndex = GetPhysicalIndex(newIndex) - (oldIndex > newIndex ? 0 : 1);
				if (actualOldIndex == actualNewIndex) {
					return;
				}
				
				controller.ColumnOrRowRemoved(actualOldIndex);
				base.MoveItem(oldIndex, newIndex);
				controller.ColumnOrRowInserted(actualNewIndex, movedItem);
				
				InvalidateMaps();
				UpdatePlacement();
			} else {
				base.MoveItem(oldIndex, newIndex);
			}
		}
		
		/// <summary>
		/// Updates the placement of all visual elements in the <see cref="T:NameBasedGrid.NameBasedGrid"/>.
		/// </summary>
		/// <remarks>
		/// <para>This method updates the positioning of all visual elements in the <see cref="T:NameBasedGrid.NameBasedGrid"/> along the dimension specified by this list.
		///   The name maps are refreshed before this operation takes place.</para>
		/// </remarks>
		internal void UpdatePlacement()
		{
			if (namesMap == null) {
				BuildMaps();
			}
			
			foreach (var child in controller.AllElements) {
				DoUpdatePlacement(child);
			}
		}
		
		/// <summary>
		/// Updates the placement of a given visual element.
		/// </summary>
		/// <param name="element">The element whose position needs to be updated.</param>
		/// <exception cref="ArgumentNullException"><paramref name="element"/> is <see langword="null"/>.</exception>
		/// <remarks>
		/// <para>This method updates the positioning of <paramref name="element"/> along the dimension specified by this list.
		///   The name maps are refreshed before this operation takes place.</para>
		/// </remarks>
		internal void UpdatePlacement(UIElement element)
		{
			if (element == null) {
				throw new ArgumentNullException("element");
			}
			
			if (namesMap == null) {
				BuildMaps();
			}
			
			DoUpdatePlacement(element);
		}
		
		/// <summary>
		/// Updates the placement of a given visual element based on the current state.
		/// </summary>
		/// <param name="element">The element whose position needs to be updated.
		///   This must not be <see langword="null"/>.</param>
		/// <remarks>
		/// <para>This method updates the positioning of <paramref name="element"/> along the dimension specified by this list.
		///   The name maps are assumed to be up to date.</para>
		/// <para>This method is used by both <see cref="UpdatePlacement()"/> and <see cref="UpdatePlacement(UIElement)"/>.</para>
		/// </remarks>
		private void DoUpdatePlacement(UIElement element)
		{
			string cr1, cr2;
			controller.GetAssignedColumnOrRow(element, out cr1, out cr2);
			
			int fromIndex, toIndex;
			GetPhysicalRange(cr1, cr2, out fromIndex, out toIndex);
			
			controller.SetPhysicalIndex(element, fromIndex, toIndex - fromIndex + 1);
		}
		
		/// <summary>
		/// Determines the physical column or row indices spanned by a range of two column or row names.
		/// </summary>
		/// <param name="columnOrRow1">A column or row name.</param>
		/// <param name="columnOrRow2">Another column or row name.</param>
		/// <param name="fromIndex">The lower retrieved index.</param>
		/// <param name="toIndex">The higher retrieved index.</param>
		private void GetPhysicalRange(string columnOrRow1, string columnOrRow2, out int fromIndex, out int toIndex)
		{
			if (columnOrRow1 != null) {
				if ((columnOrRow2 != null) && (columnOrRow1 != columnOrRow2)) {
					int fromIndex1, fromIndex2, toIndex1, toIndex2;
					GetPhysicalRange(columnOrRow1, out fromIndex1, out toIndex1);
					GetPhysicalRange(columnOrRow2, out fromIndex2, out toIndex2);
					fromIndex = Math.Min(fromIndex1, fromIndex2);
					toIndex = Math.Max(toIndex1, toIndex2);
				} else {
					GetPhysicalRange(columnOrRow1, out fromIndex, out toIndex);
				}
			} else {
				if (columnOrRow2 != null) {
					GetPhysicalRange(columnOrRow2, out fromIndex, out toIndex);
				} else {
					fromIndex = 0;
					toIndex = 0;
				}
			}
		}
		
		/// <summary>
		/// Determines the physical column or row indices spanned by a given column or row name.
		/// </summary>
		/// <param name="columnOrRow">The column or row name.</param>
		/// <param name="fromIndex">The lower retrieved index.</param>
		/// <param name="toIndex">The higher retrieved index.</param>
		private void GetPhysicalRange(string columnOrRow, out int fromIndex, out int toIndex)
		{
			if (namesMap == null) {
				throw new InvalidOperationException("The internal maps are not ready.");
			}
			
			var foundNames = new HashSet<string>();
			var physicalIndices = new List<int>();
			
			var nextNames = new Queue<string>();
			nextNames.Enqueue(columnOrRow);
			
			while (nextNames.Count > 0) {
				string cr = nextNames.Dequeue();
				if (!foundNames.Contains(cr)) {
					int physicalIndex;
					if (indexMap.TryGetValue(cr, out physicalIndex)) {
						physicalIndices.Add(physicalIndex);
					} else {
						ColumnOrRowBase crDef;
						if (namesMap.TryGetValue(cr, out crDef)) {
							var virtualDef = crDef as VirtualColumnOrRow;
							if (virtualDef != null) {
								string start = virtualDef.StartAt;
								if (start != null) {
									nextNames.Enqueue(start);
								}
								
								string end = virtualDef.ExtendTo;
								if ((end != null) && (end != start)) {
									nextNames.Enqueue(end);
								}
							}
						}
					}
					
					foundNames.Add(cr);
				}
			}
			
			if (physicalIndices.Count > 0) {
				fromIndex = int.MaxValue;
				toIndex = int.MinValue;
				
				foreach (int idx in physicalIndices) {
					if (idx < fromIndex) {
						fromIndex = idx;
					}
					if (idx > toIndex) {
						toIndex = idx;
					}
				}
			} else {
				fromIndex = 0;
				toIndex = 0;
			}
		}
		
		/// <summary>
		/// Updates the size of all physical columns or rows in the list.
		/// </summary>
		internal void UpdateSize()
		{
			int physicalIndex = 0;
			for (int i = 0; i < this.Count; i++) {
				var cr = this[i] as ColumnOrRow;
				if (cr != null) {
					this.controller.SetSize(physicalIndex, cr.Size);
					physicalIndex++;
				}
			}
		}
		
		#region source list
		/// <summary>
		/// A <see cref="IWeakEventListener">weak event listener</see> that processes changes to the source list.
		/// </summary>
		private sealed class SourceListChangeListener : IWeakEventListener
		{
			/// <summary>
			/// Initializes a new instance.
			/// </summary>
			/// <param name="owner">The list that changes in the source list are replicated to.</param>
			/// <exception cref="ArgumentNullException"><paramref name="owner"/> is <see langword="null"/>.</exception>
			public SourceListChangeListener(ColumnOrRowList owner)
			{
				if (owner == null) {
					throw new ArgumentNullException("owner");
				}
				
				this.owner = owner;
			}
			
			/// <summary>
			/// The list that changes in the source list are replicated to.
			/// </summary>
			private readonly ColumnOrRowList owner;
			
			/// <summary>
			/// Processes the event.
			/// </summary>
			/// <param name="managerType">The type of the weak event manager.
			///   This argument will be ignored.</param>
			/// <param name="sender">The object that issued the event.
			///   This argument will be ignored.</param>
			/// <param name="e">An object that contains some information about the event.</param>
			/// <returns><see langword="true"/> if <paramref name="e"/> could be cast to <see cref="NotifyCollectionChangedEventArgs"/>, otherwise <see langword="false"/>.</returns>
			public bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
			{
				var typedE = e as NotifyCollectionChangedEventArgs;
				if (typedE != null) {
					ProcessEvent(typedE);
					return true;
				} else {
					return false;
				}
			}
			
			/// <summary>
			/// Processes the event based on a <see cref="NotifyCollectionChangedEventArgs"/> instance.
			/// </summary>
			/// <param name="e">An object providing some information about the event.
			///   This must not be <see langword="null"/>.</param>
			private void ProcessEvent(NotifyCollectionChangedEventArgs e)
			{
				switch (e.Action) {
					case NotifyCollectionChangedAction.Add:
						owner.lockForSourceList = false;
						try {
							for (int i = 0; i < e.NewItems.Count; i++) {
								owner.Insert(e.NewStartingIndex + i, PrepareSourceListItem(e.NewItems[i]));
							}
						}
						finally {
							owner.lockForSourceList = true;
						}
						break;
					case NotifyCollectionChangedAction.Move:
						owner.lockForSourceList = false;
						try {
							if (e.OldStartingIndex < e.NewStartingIndex) {
								for (int i = 0; i < e.OldItems.Count; i++) {
									owner.Move(e.OldStartingIndex, e.NewStartingIndex);
								}
							} else {
								for (int i = 0; i < e.OldItems.Count; i++) {
									owner.Move(e.OldStartingIndex - i, e.NewStartingIndex + i);
								}
							}
						}
						finally {
							owner.lockForSourceList = true;
						}
						break;
					case NotifyCollectionChangedAction.Remove:
						owner.lockForSourceList = false;
						try {
							for (int i = 0; i < e.OldItems.Count; i++) {
								owner.RemoveAt(e.OldStartingIndex);
							}
						}
						finally {
							owner.lockForSourceList = true;
						}
						break;
					case NotifyCollectionChangedAction.Replace:
						owner.lockForSourceList = false;
						try {
							for (int i = 0; i < e.NewItems.Count; i++) {
								owner[e.NewStartingIndex + i] = PrepareSourceListItem(e.NewItems[i]);
							}
						}
						finally {
							owner.lockForSourceList = true;
						}
						break;
					case NotifyCollectionChangedAction.Reset:
						owner.ReloadSourceList();
						break;
				}
			}
		}
		
		/// <summary>
		/// The <see cref="SourceListChangeListener"/> instance linked to the current list instance.
		/// </summary>
		private readonly SourceListChangeListener sourceListChangeListener;
		
		/// <summary>
		/// The list that serves as the source for the items in this list, or <see langword="null"/>.
		/// </summary>
		private System.Collections.IEnumerable sourceList;
		
		/// <summary>
		/// Indicates whether modifying members throw an <see cref="InvalidOperationException"/> when invoked.
		/// </summary>
		/// <seealso cref="CheckModificationsAllowed"/>
		private bool lockForSourceList;
		
		/// <summary>
		/// Checks whether the list can currently be modified, otherwise throws an exception.
		/// </summary>
		/// <exception cref="InvalidOperationException">The method is invoked while the list retrieves its content from a source list.</exception>
		/// <seealso cref="lockForSourceList"/>
		private void CheckModificationsAllowed()
		{
			if (lockForSourceList) {
				throw new InvalidOperationException("The list cannot be modified while a source list is assigned.");
			}
		}
		
		/// <summary>
		/// Assigns an enumeration that serves as a source for this list.
		/// </summary>
		/// <param name="sourceList">The new source, or <see langword="null"/> if the current list should manage its own items.</param>
		internal void SetSourceList(System.Collections.IEnumerable sourceList)
		{
			if (this.sourceList == sourceList) {
				return;
			}
			
			var observable = this.sourceList as INotifyCollectionChanged;
			if (observable != null) {
				CollectionChangedEventManager.RemoveListener(observable, this.sourceListChangeListener);
			}
			
			lockForSourceList = false;
			this.Clear();
			this.sourceList = sourceList;
			
			if (sourceList != null) {
				ReloadSourceList();
				
				observable = this.sourceList as INotifyCollectionChanged;
				if (observable != null) {
					CollectionChangedEventManager.AddListener(observable, this.sourceListChangeListener);
				}
			}
		}
		
		/// <summary>
		/// Adds all items found in the source list to the current list.
		/// </summary>
		/// <remarks>
		/// <para>This method appends all items (processed by <see cref="PrepareSourceListItem"/>) found in the source list to the current list.
		///   If no source list is set, this method must not be called.</para>
		/// </remarks>
		/// <seealso cref="sourceList"/>
		private void ReloadSourceList()
		{
			lockForSourceList = false;
			try {
				foreach (var item in sourceList) {
					this.Add(PrepareSourceListItem(item));
				}
			}
			finally {
				lockForSourceList = true;
			}
		}
		
		/// <summary>
		/// Creates a <see cref="ColumnOrRowBase"/> instance based on an <see cref="object"/> retrieved from <see cref="sourceList"/>.
		/// </summary>
		/// <param name="item">The <see cref="object"/> (possibly <see langword="null"/>) returned from the source list.</param>
		/// <returns>The appropriate <see cref="ColumnOrRowBase"/> instance.</returns>
		/// <remarks>
		/// <para>This method converts an <see cref="object"/> value retrieved from <see cref="sourceList"/> to a <see cref="ColumnOrRowBase"/> instance.
		///   In the current implementation, the following conversions will be done:</para>
		/// <list type="table">
		///   <listheader>
		///     <term><see cref="object"/> type</term>
		///     <description>Resulting <see cref="ColumnOrRowBase"/> instance</description>
		///   </listheader>
		///   <item>
		///     <term><see langword="null"/></term>
		///     <description>A <see cref="ColumnOrRow"/> without any specific settings.</description>
		///   </item>
		///   <item>
		///     <term><see cref="GridLength"/></term>
		///     <description>A <see cref="ColumnOrRow"/> with the given size.</description>
		///   </item>
		///   <item>
		///     <term><see cref="Nullable{GridLength}"/></term>
		///     <description>A <see cref="ColumnOrRow"/> with the given size.</description>
		///   </item>
		///   <item>
		///     <term><seealso cref="ColumnOrRowBase"/> (including subtypes)</term>
		///     <description>The unmodified object passed to <paramref name="item"/>.</description>
		///   </item>
		///   <item>
		///     <term>otherwise</term>
		///     <description>A <see cref="ColumnOrRow"/> whose <see cref="ColumnOrRowBase.Name"/> is set to the value returned by the <see cref="object.ToString"/> method of <paramref name="item"/>.</description>
		///   </item>
		/// </list>
		/// </remarks>
		private static ColumnOrRowBase PrepareSourceListItem(object item)
		{
			if (item == null) {
				return new ColumnOrRow();
			} else if (item is GridLength) {
				return new ColumnOrRow() {
					Size = (GridLength)item
				};
			} else if (item is GridLength?) {
				return new ColumnOrRow() {
					Size = (GridLength?)item
				};
			} else {
				ColumnOrRowBase cr = item as ColumnOrRowBase;
				if (cr != null) {
					return cr;
				} else {
					return new ColumnOrRow() {
						Name = item.ToString()
					};
				}
			}
		}
		#endregion
	}
}
