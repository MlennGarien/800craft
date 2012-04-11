// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> A string metadata entry. </summary>
    /// <typeparam name="TValue"> Value type. Must be a reference type. </typeparam>
    [DebuggerDisplay( "Count = {Count}" )]
    public struct MetadataEntry<TValue> where TValue : class {
        string group;
        [NotNull]
        public string Group {
            get { return group; }
            set {
                if( value == null ) throw new ArgumentNullException( "value" );
                group = value;
            }
        }

        string key;
        [NotNull]
        public string Key {
            get { return key; }
            set {
                if( value == null ) throw new ArgumentNullException( "value" );
                key = value;
            }
        }

        TValue value;
        [NotNull]
        public TValue Value {
            get { return value; }
            set {
                if( value == this.value ) throw new ArgumentNullException( "value" );
                this.value = value;
            }
        }
    }


    /// <summary> A collection of metadata entries, addressable by pairs of string group/key names.
    /// Group names, key names, and values may not be null. </summary>
    /// <typeparam name="TValue"> Value type. Must be a reference type. </typeparam>
    public sealed class MetadataCollection<TValue> : ICollection<MetadataEntry<TValue>>, ICollection, ICloneable, INotifiesOnChange where TValue : class {

        readonly Dictionary<string, Dictionary<string, TValue>> store = new Dictionary<string, Dictionary<string, TValue>>();

        /// <summary> Creates an empty MetadataCollection. </summary>
        public MetadataCollection() { }


        /// <summary> Creates a copy of the given MetadataCollection. Copies all entries within. </summary>
        public MetadataCollection( [NotNull] MetadataCollection<TValue> other )
            : this() {
            if( other == null ) throw new ArgumentNullException( "other" );
            lock( other.syncRoot ) {
                foreach( var group in store ) {
                    foreach( var key in group.Value ) {
                        Add( group.Key, key.Key, key.Value );
                    }
                }
            }
        }


        /// <summary> Adds a new entry to the collection.
        /// Throws ArgumentException if an entry with the same group/key already exists. </summary>
        /// <param name="group"> Group name. Cannot be null. </param>
        /// <param name="key"> Key name. Cannot be null. </param>
        /// <param name="value"> Value. Cannot be null. </param>
        public void Add( [NotNull] string group, [NotNull] string key, [NotNull] TValue value ) {
            if( group == null ) throw new ArgumentNullException( "group" );
            if( key == null ) throw new ArgumentNullException( "key" );
            if( value == null ) throw new ArgumentNullException( "value" );
            lock( syncRoot ) {
                if( !store.ContainsKey( group ) ) {
                    store.Add( group, new Dictionary<string, TValue>() );
                }
                store[group].Add( key, value );
                RaiseChangedEvent();
            }
        }


        /// <summary> Removes entry with the specified group/key from the collection. </summary>
        /// <param name="group"> Group name. Cannot be null. </param>
        /// <param name="key"> Key name. Cannot be null. </param>
        /// <returns> True if the entry was located and removed. False if no entry was found. </returns>
        public bool Remove( [NotNull] string group, [NotNull] string key ) {
            if( group == null ) throw new ArgumentNullException( "group" );
            if( key == null ) throw new ArgumentNullException( "key" );
            Dictionary<string, TValue> pair;
            lock( syncRoot ) {
                if( !store.TryGetValue( group, out pair ) ) return false;
                if( pair.Remove( key ) ) {
                    RaiseChangedEvent();
                    return true;
                } else {
                    return false;
                }
            }
        }


        #region Count / Group Count / Key Count

        /// <summary> The total number of entries in this collection. </summary>
        public int Count {
            get {
                lock( syncRoot ) {
                    return store.Sum( group => group.Value.Count );
                }
            }
        }


        /// <summary> Number of groups in this collection. </summary>
        public int GroupCount {
            get { return store.Count; }
        }


        /// <summary> Number of keys within a given group. Throws KeyNotFoundException if no such group exists. </summary>
        /// <param name="group"> Group name. Cannot be null. </param>
        /// <returns> Number of keys within the specified group. </returns>
        public int GetKeyCount( [NotNull] string group ) {
            if( group == null ) throw new ArgumentNullException( "group" );
            return store[group].Count;
        }

        #endregion


        #region Index / Get / Set

        /// <summary> Gets or sets the value of a given entry.
        /// If the specified key/value pair is not found, a get operation throws a KeyNotFoundException,
        /// and a set operation creates a new element with the specified group/key. </summary>
        /// <param name="group"> The group of the value to get or set. </param>
        /// <param name="key"> The key of the value to get or set. </param>
        public TValue this[[NotNull] string group, [NotNull] string key] {
            get {
                if( group == null ) throw new ArgumentNullException( "group" );
                if( key == null ) throw new ArgumentNullException( "key" );
                return GetValue( group, key );
            }
            set {
                if( group == null ) throw new ArgumentNullException( "group" );
                if( key == null ) throw new ArgumentNullException( "key" );
                SetValue( group, key, value );
            }
        }


        TValue GetValue( [NotNull] string group, [NotNull] string key ) {
            if( group == null ) throw new ArgumentNullException( "group" );
            if( key == null ) throw new ArgumentNullException( "key" );
            lock( syncRoot ) {
                return store[group][key];
            }
        }


        void SetValue( [NotNull] string group, [NotNull] string key, [NotNull] TValue value ) {
            if( group == null ) throw new ArgumentNullException( "group" );
            if( key == null ) throw new ArgumentNullException( "key" );
            if( value == null ) throw new ArgumentNullException( "value" );
            lock( syncRoot ) {
                bool raiseChangedEvent = false;
                if( !store.ContainsKey( group ) ) {
                    store.Add( group, new Dictionary<string, TValue>() );
                    raiseChangedEvent = true;
                }
                if( !store[group].ContainsKey( key ) || store[group][key] != value ) {
                    raiseChangedEvent = true;
                }
                store[group][key] = value;
                if( raiseChangedEvent ) RaiseChangedEvent();
            }
        }


        public MetadataEntry<TValue> Get( [NotNull] string group, [NotNull] string key ) {
            if( group == null ) throw new ArgumentNullException( "group" );
            if( key == null ) throw new ArgumentNullException( "key" );
            lock( syncRoot ) {
                return new MetadataEntry<TValue> {
                    Group = group,
                    Key = key,
                    Value = store[group][key]
                };
            }
        }


        public void Set( MetadataEntry<TValue> entry ) {
            SetValue( entry.Group, entry.Key, entry.Value );
        }

        #endregion


        #region Contains Group / Key / Value

        public bool ContainsGroup( [NotNull] string group ) {
            if( group == null ) throw new ArgumentNullException( "group" );
            lock( syncRoot ) {
                return store.ContainsKey( group );
            }
        }


        public bool ContainsKey( [NotNull] string group, [NotNull] string key ) {
            if( group == null ) throw new ArgumentNullException( "group" );
            if( key == null ) throw new ArgumentNullException( "key" );
            lock( syncRoot ) {
                return store.ContainsKey( group ) &&
                       store[group].ContainsKey( key );
            }
        }


        public bool ContainsValue( [NotNull] TValue value ) {
            lock( syncRoot ) {
                // ReSharper disable LoopCanBeConvertedToQuery
                foreach( var group in store ) {
                    foreach( var key in group.Value ) {
                        if( value.Equals( key.Value ) ) {
                            return true;
                        }
                    }
                }
                // ReSharper restore LoopCanBeConvertedToQuery
            }
            return false;
        }


        public bool ContainsValue( [NotNull] TValue value, IEqualityComparer<TValue> comparer ) {
            lock( syncRoot ) {
                // ReSharper disable LoopCanBeConvertedToQuery
                foreach( var group in store ) {
                    foreach( var key in group.Value ) {
                        if( comparer.Equals( key.Value, value ) ) {
                            return true;
                        }
                    }
                }
                // ReSharper restore LoopCanBeConvertedToQuery
            }
            return false;
        }

        #endregion


        public bool TryGetValue( [NotNull] string group, [NotNull] string key, out TValue value ) {
            if( group == null ) throw new ArgumentNullException( "group" );
            if( key == null ) throw new ArgumentNullException( "key" );
            Dictionary<string, TValue> pair;
            lock( syncRoot ) {
                if( !store.TryGetValue( group, out pair ) ) {
                    value = null;
                    return false;
                }
                return pair.TryGetValue( key, out value );
            }
        }


        /// <summary> Enumerates a group of keys. </summary>
        /// <remarks> Lock SyncRoot if this is used in a loop. </remarks>
        public IEnumerable<MetadataEntry<TValue>> GetGroup( [NotNull] string group ) {
            if( group == null ) throw new ArgumentNullException( "group" );
            Dictionary<string, TValue> groupDic;
            if( store.TryGetValue( group, out groupDic ) ) {
                lock( syncRoot ) {
                    foreach( var key in groupDic ) {
                        yield return new MetadataEntry<TValue> {
                            Group = group,
                            Key = key.Key,
                            Value = key.Value
                        };
                    }
                }
            } else {
                throw new KeyNotFoundException( "No group found with the given name." );
            }
        }


        #region ICollection<MetadataEntry> Members

        public void Add( MetadataEntry<TValue> item ) {
            Add( item.Group, item.Key, item.Value );
        }


        public void Clear() {
            lock( syncRoot ) {
                bool raiseEvent = (store.Count > 0);
                store.Clear();
                if( raiseEvent ) RaiseChangedEvent();
            }
        }


        public bool Contains( [NotNull] MetadataEntry<TValue> item ) {
            return ContainsKey( item.Group, item.Key );
        }


        public void CopyTo( [NotNull] MetadataEntry<TValue>[] array, int arrayIndex ) {
            if( array == null ) throw new ArgumentNullException( "array" );

            if( arrayIndex < 0 || arrayIndex >= array.Length ) {
                throw new ArgumentOutOfRangeException( "arrayIndex" );
            }

            lock( syncRoot ) {
                if( array.Length < arrayIndex + Count ) {
                    throw new ArgumentOutOfRangeException( "array" );
                }

                int i = 0;
                foreach( var group in store ) {
                    foreach( var pair in group.Value ) {
                        array[i] = new MetadataEntry<TValue> {
                            Group = group.Key,
                            Key = pair.Key,
                            Value = pair.Value
                        };
                        i++;
                    }
                }
            }
        }


        bool ICollection<MetadataEntry<TValue>>.IsReadOnly {
            get { return false; }
        }


        public bool Remove( MetadataEntry<TValue> item ) {
            return Remove( item.Group, item.Key );
        }

        #endregion


        #region IEnumerable<MetadataEntry> Members

        /// <summary> Enumerates all keys in this collection. </summary>
        /// <remarks> Lock SyncRoot if this is used in a loop. </remarks>
        public IEnumerator<MetadataEntry<TValue>> GetEnumerator() {
            // ReSharper disable LoopCanBeConvertedToQuery
            foreach( var group in store ) {
                foreach( var key in group.Value ) {
                    yield return new MetadataEntry<TValue> {
                        Group = group.Key,
                        Key = key.Key,
                        Value = key.Value
                    };
                }
            }
            // ReSharper restore LoopCanBeConvertedToQuery
        }

        #endregion


        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        #endregion


        #region ICollection Members

        public void CopyTo( [NotNull] Array array, int index ) {
            if( array == null ) throw new ArgumentNullException( "array" );
            var castArray = array as MetadataEntry<TValue>[];
            if( castArray == null ) {
                throw new ArgumentException( "Array must be of type MetadataEntry[]", "array" );
            }
            CopyTo( castArray, index );
        }


        public bool IsSynchronized {
            get { return true; }
        }


        readonly object syncRoot = new object();
        /// <summary> Internal lock object used by this collection to ensure thread safety. </summary>
        public object SyncRoot {
            get { return syncRoot; }
        }

        #endregion


        public object Clone() {
            return new MetadataCollection<TValue>( this );
        }


        public event EventHandler Changed;


        void RaiseChangedEvent() {
            var h = Changed;
            if( h != null ) h( null, EventArgs.Empty );
        }
    }
}