// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Text;
using JetBrains.Annotations;

// ReSharper disable ClassCanBeSealed.Global
namespace fCraft {
    /// <summary> Specialized data structure for partial-matching of large sparse sets of words.
    /// Used as a searchable index of players for PlayerDB. </summary>
    /// <typeparam name="T"> Payload type (reference types only). </typeparam>
    [DebuggerDisplay( "Count = {Count}" )]
    public class Trie<T> : IDictionary<string, T>, IDictionary, ICloneable where T : class {
        const byte LeafNode = 254,
                   MultiNode = 255;

        const string InconsistentStateMessage = "Inconsistent state";

        TrieNode root = new TrieNode();

        int version;


        /// <summary> Creates a new empty trie. </summary>
        public Trie() {
            Count = 0;
            keys = new TrieKeyCollection( this );
            values = new TrieValueCollection( this );
        }


        /// <summary> Creates a new trie from an existing dictionary. Values are shallowly copied. </summary>
        /// <param name="dictionary"> Source dictionary to copy from. </param>
        public Trie( [NotNull] IEnumerable<KeyValuePair<string, T>> dictionary )
            : this() {
            if( dictionary == null ) throw new ArgumentNullException( "dictionary" );
            foreach( var pair in dictionary ) {
                Add( pair.Key, pair.Value );
            }
        }


        // Find a node that exactly matches the given key
        [CanBeNull]
        TrieNode GetNode( [NotNull] string key ) {
            if( key == null ) throw new ArgumentNullException( "key" );

            TrieNode temp = root;
            for( int i = 0; i < key.Length; i++ ) {
                int code = CharToCode( key[i] );
                switch( temp.Tag ) {
                    case LeafNode:
                        return null;

                    case MultiNode:
                        if( temp.Children[code] == null ) {
                            return null;
                        } else {
                            temp = temp.Children[code];
                            break;
                        }

                    default:
                        if( temp.Tag != code ) {
                            return null;
                        } else {
                            temp = temp.Children[0];
                            break;
                        }
                }
            }
            return temp;
        }


        /// <summary> Checks whether the trie contains a given value.
        /// This method uses the value enumerator and runs in O(n). </summary>
        /// <param name="value"> Value to search for. </param>
        /// <returns> True if the trie contains at least one copy of the value. </returns>
        public bool ContainsValue( [NotNull] T value ) {
            // ReSharper restore UnusedMember.Global
            if( value == null ) throw new ArgumentNullException( "value" );
            return Values.Contains( value );
        }


        /// <summary> Searches for payloads with keys that start with keyPart, returning just one or none of the matches. </summary>
        /// <param name="keyPart"> Partial or full key. </param>
        /// <param name="payload"> Payload object to output (will be set to null if no single match was found). </param>
        /// <returns>
        /// If no matches were found, returns true and sets payload to null.
        /// If one match was found, returns true and sets payload to the value.
        /// If more than one match was found, returns false and sets payload to null.
        /// </returns>
        public bool GetOneMatch( [NotNull] string keyPart, out T payload ) {
            if( keyPart == null ) throw new ArgumentNullException( "keyPart" );
            TrieNode node = GetNode( keyPart );

            if( node == null ) {
                payload = null;
                return true; // no matches
            }

            if( node.Payload != null ) {
                payload = node.Payload;
                return true; // exact match

            } else if( node.Tag == MultiNode ) {
                payload = null;
                return false; // multiple matches
            }

            // either partial match, or multiple matches
            while( true ) {
                switch( node.Tag ) {
                    case LeafNode:
                        // found a singular match
                        payload = node.Payload;
                        return true;

                    case MultiNode:
                        // ran into multiple matches
                        payload = null;
                        return false;

                    default:
                        // go deeper
                        node = node.Children[0];
                        break;
                }
            }
        }


        /// <summary> Finds a list of payloads with keys that start with keyPart, up to a specified limit. Autocompletes. </summary>
        /// <param name="keyPart"> Partial or full key. </param>
        /// <param name="limit"> Limit on the number of payloads to find/return. </param>
        /// <returns> List of matches (if there are no matches, length is zero). </returns>
        public List<T> GetList( [NotNull] string keyPart, int limit ) {
            if( keyPart == null ) throw new ArgumentNullException( "keyPart" );
            List<T> results = new List<T>();

            TrieNode startingNode = GetNode( keyPart );
            if( startingNode != null ) {
                startingNode.GetAllChildren( results, limit );
            }

            return results;
        }


        /// <summary> Adds a new object by key. </summary>
        /// <param name="key"> Full key. </param>
        /// <param name="payload"> Object associated with the key. </param>
        /// <param name="overwriteOnDuplicate"> Whether to overwrite the value in case this key already exists. </param>
        /// <returns> True if object was added, false if an entry for this key already exists. </returns>
        public bool Add( [NotNull] string key, [NotNull] T payload, bool overwriteOnDuplicate ) {
            if( key == null ) throw new ArgumentNullException( "key" );
            if( payload == null ) throw new ArgumentNullException( "payload" );

            if( key.Length == 0 ) {
                if( root.Payload != null ) {
                    if( overwriteOnDuplicate ) {
                        root.Payload = payload;
                        version++;
                    }
                    return false;
                }
                Count++;
                root.Payload = payload;
                return true;
            }

            TrieNode temp = root;
            for( int i = 0; i < key.Length; i++ ) {
                int code = CharToCode( key[i] );

                switch( temp.Tag ) {
                    case LeafNode:
                        temp.LeafToSingle( (byte)code );
                        temp.Children[0] = new TrieNode();
                        temp = temp.Children[0];
                        break;

                    case MultiNode:
                        if( temp.Children[code] == null ) {
                            temp.Children[code] = new TrieNode();
                        }
                        temp = temp.Children[code];
                        break;

                    default:
                        if( temp.Tag != code ) {
                            temp.SingleToMulti();
                            temp.Children[code] = new TrieNode();
                            temp = temp.Children[code];
                        } else {
                            temp = temp.Children[0];
                        }
                        break;
                }
            }

            if( temp.Payload != null ) {
                if( overwriteOnDuplicate ) {
                    temp.Payload = payload;
                    version++;
                }
                return false;
            } else {
                temp.Payload = payload;
                version++;
                Count++;
                return true;
            }
        }


        /// <summary> Get payload for an exact key (no autocompletion). </summary>
        /// <param name="key"> Full key. </param>
        /// <returns> Payload object, if found. Null if not found. </returns>
        [CanBeNull]
        public T Get( [NotNull] string key ) {
            if( key == null ) throw new ArgumentNullException( "key" );
            TrieNode node = GetNode( key );
            if( node != null ) {
                return node.Payload;
            } else {
                return null;
            }
        }


        #region Key Encoding / Decoding

        // Decodes ASCII into internal letter code.
        protected int CharToCode( char ch ) {
            if( ch >= 'a' && ch <= 'z' )
                return ch - 'a';
            else if( ch >= 'A' && ch <= 'Z' )
                return ch - 'A';
            else if( ch >= '0' && ch <= '9' )
                return ch - '0' + 26;
            else
                return 36;
        }


        protected char CodeToChar( int code ) {
            if( code < 26 )
                return (char)(code + 'a');
            if( code >= 26 && code < 36 )
                return (char)(code + '0');
            else
                return '_';
        }


        protected char CanonicizeChar( char ch ) {
            if( ch >= 'a' && ch <= 'z' || ch >= '0' && ch <= '9' || ch == '_' )
                return ch;
            else if( ch >= 'A' && ch <= 'Z' )
                return (char)(ch - ('A' - 'a'));
            else
                return '_';
        }


        protected string CanonicizeKey( string key ) {
            StringBuilder sb = new StringBuilder( key );
            for( int i = 0; i < sb.Length; i++ ) {
                sb[i] = CanonicizeChar( sb[i] );
            }
            return sb.ToString();
        }

        #endregion


        #region Subset Enumerators

        /// <summary> Finds a subset of values whose keys start with a given prefix. </summary>
        /// <param name="prefix"> Key prefix. </param>
        /// <returns> Enumeration of values. </returns>
        public IEnumerable<T> ValuesStartingWith( string prefix ) {
            return values.StartingWith( prefix );
        }


        /// <summary> Finds a subset of keys that start with a given prefix. </summary>
        /// <param name="prefix"> Key prefix. </param>
        /// <returns> Enumeration of keys. </returns>
        public IEnumerable<string> KeysStartingWith( string prefix ) {
            return keys.StartingWith( prefix );
        }


        /// <summary> Finds a subset of key/value pairs that start with a given prefix. </summary>
        /// <param name="prefix"> Key prefix. </param>
        /// <returns> Enumeration of key/value pairs. </returns>
        public IEnumerable<KeyValuePair<string, T>> StartingWith( string prefix ) {
            return new TrieSubset( this, prefix );
        }


        /// <summary> A subset of trie's key/value pairs that start with a certain prefix. </summary>
        public class TrieSubset : IEnumerable<KeyValuePair<string, T>> {
            readonly Trie<T> trie;
            readonly string prefix;

            public TrieSubset( Trie<T> trie, string prefix ) {
                this.trie = trie;
                this.prefix = prefix;
            }


            public IEnumerator<KeyValuePair<string, T>> GetEnumerator() {
                TrieNode node = trie.GetNode( prefix );
                return new TrieEnumerator( node, trie, trie.CanonicizeKey( prefix ) );
            }


            IEnumerator IEnumerable.GetEnumerator() {
                return GetEnumerator();
            }
        }

        #endregion


        #region Enumerator Base

        class EnumeratorBase {

            // Starting node ("root" of the trie/subtrie)
            protected readonly TrieNode StartingNode;

            // Current node (presumably with payload)
            protected TrieNode CurrentNode;

            // Index of the child in the current node
            protected int CurrentIndex;

            // Version of collection when we started iterating. Used to keep track of collection changes.
            protected int StartingVersion;

            // Trie from which our nodes originate (used in conjunction with startingVersion to check for modification).
            protected readonly Trie<T> BaseTrie;

            protected StringBuilder CurrentKeyName;

            protected readonly string BasePrefix;


            // A couple stacks to keep track of our position in the trie
            protected readonly Stack<TrieNode> Parents = new Stack<TrieNode>();
            protected readonly Stack<int> ParentIndices = new Stack<int>();


            protected EnumeratorBase( [NotNull] TrieNode node, [NotNull] Trie<T> trie, [NotNull] string prefix ) {
                if( node == null ) throw new ArgumentNullException( "node" );
                if( trie == null ) throw new ArgumentNullException( "trie" );
                if( prefix == null ) throw new ArgumentNullException( "prefix" );
                StartingNode = node;
                BaseTrie = trie;
                BasePrefix = prefix;
                CurrentKeyName = new StringBuilder( BasePrefix );
                StartingVersion = BaseTrie.version;
            }


            protected bool MoveNextInternal() {
                if( StartingNode == null ) return false;
                if( BaseTrie.version != StartingVersion ) {
                    ThrowCollectionModifiedException();
                }
                if( CurrentNode == null ) {
                    CurrentNode = StartingNode;
                    if( CurrentNode.Payload != null ) {
                        return true;
                    }
                }
                return FindNextPayload();
            }


            protected void ResetInternal() {
                Parents.Clear();
                ParentIndices.Clear();
                CurrentNode = null;
                StartingVersion = BaseTrie.version;
                CurrentKeyName = new StringBuilder( BasePrefix );
            }


            protected bool FindNextPayload() {
            continueLoop:
                switch( CurrentNode.Tag ) {
                    case MultiNode:
                        while( CurrentIndex < CurrentNode.Children.Length ) {
                            if( CurrentNode.Children[CurrentIndex] != null ) {
                                MoveDown( CurrentNode.Children[CurrentIndex], CurrentIndex );
                                if( CurrentNode.Payload != null ) {
                                    return true;
                                } else {
                                    goto continueLoop;
                                }
                            } else {
                                CurrentIndex++;
                            }
                        }
                        if( !MoveUp() ) return false;
                        goto continueLoop;

                    case LeafNode:
                        if( !MoveUp() ) return false;
                        goto continueLoop;

                    default:
                        if( CurrentIndex == 0 ) {
                            MoveDown( CurrentNode.Children[0], CurrentNode.Tag );
                            if( CurrentNode.Payload != null ) {
                                return true;
                            }
                        } else {
                            if( !MoveUp() ) return false;
                        }
                        goto continueLoop;

                }
            }


            // Pops the nearest parent from the stack (moving up the trie)
            protected bool MoveUp() {
                if( Parents.Count == 0 ) {
                    return false;
                } else {
                    CurrentKeyName.Remove( CurrentKeyName.Length - 1, 1 );
                    CurrentNode = Parents.Pop();
                    CurrentIndex = ParentIndices.Pop();
                    return true;
                }
            }


            // Pushes current node onto the stack, and makes the given node current.
            protected void MoveDown( TrieNode node, int index ) {
                CurrentKeyName.Append( BaseTrie.CodeToChar( index ) );
                Parents.Push( CurrentNode );
                ParentIndices.Push( CurrentIndex + 1 );
                CurrentNode = node;
                CurrentIndex = 0;
            }


            protected static void ThrowCollectionModifiedException() {
                throw new InvalidOperationException( "Trie was modified since enumeration started." );
            }
        }

        #endregion


        #region IDictionary<string,T> Members

        readonly TrieKeyCollection keys;
        public ICollection<string> Keys {
            get { return keys; }
        }


        readonly TrieValueCollection values;
        public ICollection<T> Values {
            get { return values; }
        }


        /// <summary> Adds a new object by key. If an entry for this key already exists, it is NOT overwritten. </summary>
        /// <param name="key"> Full key. </param>
        /// <param name="payload"> Object associated with the key. </param>
        /// <returns> True if object was added, false if an entry for this key already exists. </returns>
        public void Add( string key, T payload ) {
            if( !Add( key, payload, false ) ) {
                throw new ArgumentException( "Duplicate key.", "key" );
            }
        }


        /// <summary> Tries to get a value by full key. </summary>
        /// <param name="key"> Full key to search for. </param>
        /// <param name="result"> Result. </param>
        /// <returns> True of a value was found for this key. </returns>
        public bool TryGetValue( string key, out T result ) {
            TrieNode node = GetNode( key );
            if( node == null ) {
                result = null;
                return false;
            } else {
                result = node.Payload;
                return (result != null);
            }
        }


        public T this[string key] {
            get {
                return Get( key );
            }
            set {
                Add( key, value, true );
            }
        }


        /// <summary> Checks whether the trie contains a given full key. </summary>
        /// <param name="key"> Full key to search for. </param>
        /// <returns> True if the trie contains a given key. </returns>
        public bool ContainsKey( string key ) {
            TrieNode node = GetNode( key );
            return (node != null && node.Payload != null);
        }


        /// <summary> Removes an entry by key. </summary>
        /// <param name="key"> Key for the entry to remove. </param>
        /// <returns> True if the entry was removed, false if no entry was found for this key. </returns>
        public bool Remove( [NotNull] string key ) {
            if( key == null ) throw new ArgumentNullException( "key" );
            if( key.Length == 0 ) {
                if( root.Payload == null ) return false;
                root.Payload = null;
                Count--;
                version++;
                return true;
            }

            // find parents
            TrieNode temp = root;
            Stack<TrieNode> parents = new Stack<TrieNode>();
            for( int i = 0; i < key.Length; i++ ) {
                int code = CharToCode( key[i] );
                switch( temp.Tag ) {
                    case LeafNode:
                        return false;

                    case MultiNode:
                        if( temp.Children[code] == null ) {
                            return false;
                        } else {
                            parents.Push( temp );
                            temp = temp.Children[code];
                            break;
                        }

                    default:
                        if( temp.Tag != code ) {
                            return false;
                        } else {
                            parents.Push( temp );
                            temp = temp.Children[0];
                            break;
                        }
                }
            }

            // reduce parents
            temp.Payload = null;
            Count--;
            version++;
            while( parents.Count > 0 ) {
                TrieNode parent = parents.Pop();
                switch( parent.Tag ) {
                    case LeafNode:
                        throw new Exception( InconsistentStateMessage );

                    case MultiNode:
                        parent.MultiToSingle();
                        return true;

                    default:
                        parent.SingleToLeaf();
                        break;
                }
                if( parent.Payload != null ) {
                    break;
                }
            }
            return true;
        }


        /// <summary> Removes all keys/values from the trie, making it empty. </summary>
        public void Clear() {
            root = new TrieNode();
            Count = 0;
            version = 0;
        }

        #endregion


        #region IDictionary Members

        public bool IsFixedSize { get { return false; } }


        ICollection IDictionary.Values {
            get {
                return (ICollection)Values;
            }
        }


        ICollection IDictionary.Keys {
            get {
                return (ICollection)Keys;
            }
        }


        object IDictionary.this[[NotNull] object key] {
            get {
                if( key == null ) {
                    throw new ArgumentNullException( "key" );
                }
                string castKey = key as string;
                if( castKey == null ) {
                    throw new ArgumentException( "Key must be of type String.", "key" );
                }
                return this[castKey];
            }
            set {
                if( key == null ) {
                    throw new ArgumentNullException( "key" );
                }
                string castKey = key as string;
                if( castKey == null ) {
                    throw new ArgumentException( "Key must be of type String.", "key" );
                }
                if( value == null ) {
                    throw new ArgumentNullException( "value" );
                }
                T castValue = value as T;
                if( castValue == null ) {
                    throw new ArgumentException( "Value must be of type " + typeof( T ).Name, "value" );
                }
                this[castKey] = castValue;
            }
        }


        void IDictionary.Remove( [NotNull] object key ) {
            if( key == null ) {
                throw new ArgumentNullException( "key" );
            }
            string castKey = key as string;
            if( castKey == null ) {
                throw new ArgumentException( "Key must be of type String.", "key" );
            }
            Remove( castKey );
        }


        void IDictionary.Add( [NotNull] object key, [NotNull] object value ) {
            if( key == null ) {
                throw new ArgumentNullException( "key" );
            }
            string castKey = key as string;
            if( castKey == null ) {
                throw new ArgumentException( "Key must be of type String.", "key" );
            }
            if( value == null ) {
                throw new ArgumentNullException( "value" );
            }
            T castValue = value as T;
            if( castValue == null ) {
                throw new ArgumentException( "Value must be of type " + typeof( T ).Name, "value" );
            }
            Add( castKey, castValue );
        }


        bool IDictionary.Contains( [NotNull] object key ) {
            if( key == null ) {
                throw new ArgumentNullException( "key" );
            }
            string castKey = key as string;
            if( castKey == null ) {
                throw new ArgumentException( "Key must be of type String.", "key" );
            }

            return ContainsKey( castKey );
        }


        IDictionaryEnumerator IDictionary.GetEnumerator() {
            return new TrieDictionaryEnumerator( root, this, "" );
        }


        sealed class TrieDictionaryEnumerator : EnumeratorBase, IDictionaryEnumerator {

            public TrieDictionaryEnumerator( TrieNode node, Trie<T> trie, string prefix )
                : base( node, trie, prefix ) {
            }


            public object Key {
                get {
                    if( CurrentNode == null || CurrentNode.Payload == null ) {
                        throw new InvalidOperationException();
                    }
                    return CurrentKeyName.ToString();
                }
            }


            public object Value {
                get {
                    if( CurrentNode == null || CurrentNode.Payload == null ) {
                        throw new InvalidOperationException();
                    }
                    return CurrentNode.Payload;
                }
            }


            public DictionaryEntry Entry {
                get {
                    if( CurrentNode == null || CurrentNode.Payload == null ) {
                        throw new InvalidOperationException();
                    }
                    return new DictionaryEntry( CurrentKeyName.ToString(), CurrentNode.Payload );
                }
            }


            object IEnumerator.Current {
                get {
                    return Entry;
                }
            }


            public bool MoveNext() {
                return MoveNextInternal();
            }


            public void Reset() {
                ResetInternal();
            }
        }

        #endregion


        #region ValueCollection

        [DebuggerDisplay( "Count = {Count}" )]
        public class TrieValueCollection : ICollection<T>, ICollection {
            readonly Trie<T> trie;


            public TrieValueCollection( [NotNull] Trie<T> trie ) {
                if( trie == null ) throw new ArgumentNullException( "trie" );
                this.trie = trie;
            }


            public int Count { get { return trie.Count; } }


            public bool IsReadOnly { get { return true; } }


            public bool IsSynchronized { get { return false; } }


            public object SyncRoot { get { return trie.syncRoot; } }


            public void CopyTo( [NotNull] Array array, int index ) {
                if( array == null ) throw new ArgumentNullException( "array" );
                if( index < 0 || index > array.Length ) throw new ArgumentOutOfRangeException( "index" );

                T[] castArray = array as T[];
                if( castArray == null ) {
                    throw new ArgumentException( "Array must be of type " + typeof( T ).Name + "[]" );
                }

                int i = index;
                foreach( T element in this ) {
                    castArray[i] = element;
                    i++;
                }
            }


            public void CopyTo( [NotNull] T[] array, int index ) {
                if( array == null ) throw new ArgumentNullException( "array" );
                if( index < 0 || index > array.Length ) throw new ArgumentOutOfRangeException( "index" );

                int i = index;
                foreach( T element in this ) {
                    array[i] = element;
                    i++;
                }
            }


            public bool Contains( T value ) {
                return (this as IEnumerable<T>).Contains( value );
            }


            #region Unsupported members (Add/Remove/Clear)

            const string ReadOnlyMessage = "Trie value collection is read-only";


            public void Add( T value ) {
                throw new NotSupportedException( ReadOnlyMessage );
            }


            public bool Remove( T value ) {
                throw new NotSupportedException( ReadOnlyMessage );
            }


            public void Clear() {
                throw new NotSupportedException( ReadOnlyMessage );
            }

            #endregion


            public IEnumerable<T> StartingWith( string prefix ) {
                return new TrieValueSubset( trie, prefix );
            }


            public class TrieValueSubset : IEnumerable<T> {
                readonly Trie<T> trie;
                readonly string prefix;

                public TrieValueSubset( [NotNull] Trie<T> trie, [NotNull] string prefix ) {
                    if( trie == null ) throw new ArgumentNullException( "trie" );
                    if( prefix == null ) throw new ArgumentNullException( "prefix" );
                    this.trie = trie;
                    this.prefix = prefix;
                }


                public IEnumerator<T> GetEnumerator() {
                    TrieNode node = trie.GetNode( prefix );
                    return new TrieValueEnumerator( node, trie, trie.CanonicizeKey( prefix ) );
                }


                IEnumerator IEnumerable.GetEnumerator() {
                    return GetEnumerator();
                }
            }


            #region TrieValueEnumerator

            public IEnumerator<T> GetEnumerator() {
                return new TrieValueEnumerator( trie.root, trie, "" );
            }


            IEnumerator IEnumerable.GetEnumerator() {
                return new TrieValueEnumerator( trie.root, trie, "" );
            }


            sealed class TrieValueEnumerator : EnumeratorBase, IEnumerator<T> {

                public TrieValueEnumerator( TrieNode node, Trie<T> trie, string prefix )
                    : base( node, trie, prefix ) {
                }


                public T Current {
                    get {
                        if( CurrentNode == null || CurrentNode.Payload == null ) {
                            throw new InvalidOperationException();
                        }
                        return CurrentNode.Payload;
                    }
                }


                object IEnumerator.Current {
                    get {
                        if( CurrentNode == null || CurrentNode.Payload == null ) {
                            throw new InvalidOperationException();
                        }
                        return CurrentNode.Payload;
                    }
                }


                public bool MoveNext() {
                    return MoveNextInternal();
                }


                public void Reset() {
                    ResetInternal();
                }


                void IDisposable.Dispose() { }
            }

            #endregion
        }

        #endregion


        #region KeyCollection

        [DebuggerDisplay( "Count = {Count}" )]
        public class TrieKeyCollection : ICollection<string>, ICollection {
            readonly Trie<T> trie;


            public TrieKeyCollection( [NotNull] Trie<T> trie ) {
                if( trie == null ) throw new ArgumentNullException( "trie" );
                this.trie = trie;
            }


            public int Count { get { return trie.Count; } }


            public bool IsReadOnly { get { return true; } }


            public bool IsSynchronized { get { return false; } }


            public object SyncRoot { get { return trie.syncRoot; } }


            public void CopyTo( [NotNull] Array array, int index ) {
                if( array == null ) throw new ArgumentNullException( "array" );
                if( index < 0 || index > array.Length ) throw new ArgumentOutOfRangeException( "index" );

                string[] castArray = array as string[];
                if( castArray == null ) {
                    throw new ArgumentException( "Array must be of type String[]" );
                }

                int i = index;
                foreach( string element in this ) {
                    castArray[i] = element;
                    i++;
                }
            }


            public void CopyTo( [NotNull] string[] array, int index ) {
                if( array == null ) throw new ArgumentNullException( "array" );
                if( index < 0 || index > array.Length ) throw new ArgumentOutOfRangeException( "index" );

                int i = index;
                foreach( string element in this ) {
                    array[i] = element;
                    i++;
                }
            }


            public bool Contains( string value ) {
                return trie.ContainsKey( value );
            }


            #region Unsupported members (Add/Remove/Clear)

            const string ReadOnlyMessage = "Trie value collection is read-only";


            public void Add( string value ) {
                throw new NotSupportedException( ReadOnlyMessage );
            }


            public bool Remove( string value ) {
                throw new NotSupportedException( ReadOnlyMessage );
            }


            public void Clear() {
                throw new NotSupportedException( ReadOnlyMessage );
            }

            #endregion


            public IEnumerable<string> StartingWith( string prefix ) {
                return new TrieKeySubset( trie, prefix );
            }


            public class TrieKeySubset : IEnumerable<string> {
                readonly Trie<T> trie;
                readonly string prefix;

                public TrieKeySubset( [NotNull] Trie<T> trie, [NotNull] string prefix ) {
                    if( trie == null ) throw new ArgumentNullException( "trie" );
                    if( prefix == null ) throw new ArgumentNullException( "prefix" );
                    this.trie = trie;
                    this.prefix = prefix;
                }


                public IEnumerator<string> GetEnumerator() {
                    TrieNode node = trie.GetNode( prefix );
                    return new TrieKeyEnumerator( node, trie, trie.CanonicizeKey( prefix ) );
                }


                IEnumerator IEnumerable.GetEnumerator() {
                    return GetEnumerator();
                }
            }


            #region TrieKeyEnumerator

            public IEnumerator<string> GetEnumerator() {
                return new TrieKeyEnumerator( trie.root, trie, "" );
            }


            IEnumerator IEnumerable.GetEnumerator() {
                return new TrieKeyEnumerator( trie.root, trie, "" );
            }


            sealed class TrieKeyEnumerator : EnumeratorBase, IEnumerator<string> {

                public TrieKeyEnumerator( TrieNode node, Trie<T> trie, string prefix )
                    : base( node, trie, prefix ) {
                }


                public string Current {
                    get {
                        if( CurrentNode == null || CurrentNode.Payload == null ) {
                            throw new InvalidOperationException();
                        }
                        return CurrentKeyName.ToString();
                    }
                }


                object IEnumerator.Current {
                    get {
                        return Current;
                    }
                }


                public bool MoveNext() {
                    return MoveNextInternal();
                }


                public void Reset() {
                    ResetInternal();
                }


                void IDisposable.Dispose() { }
            }

            #endregion
        }

        #endregion


        #region IEnumerable<KeyValuePair<string,T>> Members

        public IEnumerator<KeyValuePair<string, T>> GetEnumerator() {
            return new TrieEnumerator( root, this, "" );
        }


        IEnumerator IEnumerable.GetEnumerator() {
            return new TrieEnumerator( root, this, "" );
        }


        sealed class TrieEnumerator : EnumeratorBase, IEnumerator<KeyValuePair<string, T>> {

            public TrieEnumerator( TrieNode node, Trie<T> trie, string prefix )
                : base( node, trie, prefix ) {
            }


            public KeyValuePair<string, T> Current {
                get {
                    if( CurrentNode == null || CurrentNode.Payload == null ) {
                        throw new InvalidOperationException();
                    }
                    return new KeyValuePair<string, T>( CurrentKeyName.ToString(), CurrentNode.Payload );
                }
            }


            object IEnumerator.Current {
                get {
                    return Current;
                }
            }


            public bool MoveNext() {
                return MoveNextInternal();
            }


            public void Reset() {
                ResetInternal();
            }


            void IDisposable.Dispose() { }
        }

        #endregion


        #region ICollection<KeyValuePair<string,T>> Members


        public int Count { get; private set; }


        public bool IsReadOnly { get { return false; } }


        public void Add( KeyValuePair<string, T> pair ) {
            Add( pair.Key, pair.Value );
        }


        public bool Contains( KeyValuePair<string, T> pair ) {
            TrieNode node = GetNode( pair.Key );
            if( node == null ) return false;
            if( node.Payload == null ) return false;
            return node.Payload.Equals( pair.Value );
        }


        public bool Remove( KeyValuePair<string, T> pair ) {
            if( Contains( pair ) ) {
                return Remove( pair.Key );
            } else {
                return false;
            }
        }


        public void CopyTo( [NotNull] KeyValuePair<string, T>[] pairArray, int index ) {
            if( pairArray == null ) throw new ArgumentNullException( "pairArray" );
            if( index < 0 || index > pairArray.Length ) throw new ArgumentOutOfRangeException( "index" );

            int i = index;
            foreach( var pair in this ) {
                pairArray[i] = pair;
                i++;
            }
        }

        #endregion


        #region ICollection Members

        public bool IsSynchronized { get { return false; } }


        readonly object syncRoot = new object();
        public object SyncRoot { get { return syncRoot; } }


        public void CopyTo( [NotNull] Array pairArray, int index ) {
            if( pairArray == null ) throw new ArgumentNullException( "pairArray" );
            if( index < 0 || index > pairArray.Length ) throw new ArgumentOutOfRangeException( "index" );

            var castPairArray = pairArray as KeyValuePair<string, T>[];
            if( castPairArray == null ) {
                throw new ArgumentException( "Array must be of type KeyValuePair<string," + typeof( T ).Name + ">[]" );
            }

            int i = index;
            foreach( var pair in this ) {
                castPairArray[i] = pair;
                i++;
            }
        }


        #endregion


        #region ICloneable Members

        public object Clone() {
            return new Trie<T>( this );
        }

        #endregion


        sealed class TrieNode {
            const int ChildCount = 37;


            // Tag identifies TrieNode as being either a LeafNode,
            // a MultiNode, or a single-child node.
            public byte Tag = LeafNode;

            // Children. May be null (if LeafNode),
            // TrieNode[ChildCount] (if MultiNode),
            // or TrieNode[1] (if single-child node)
            public TrieNode[] Children;

            // May be null (if MultiNode or single-child node)
            [CanBeNull]
            public T Payload;


            public void LeafToSingle( byte charCode ) {
                if( Children != null || Tag != LeafNode ) {
                    throw new Exception( InconsistentStateMessage );
                }
                Children = new TrieNode[1];
                Tag = charCode;
            }


            public void SingleToLeaf() {
                if( Children == null || Children.Length != 1 || Tag >= ChildCount ) {
                    throw new Exception( InconsistentStateMessage );
                }
                if( Children[0].Tag == LeafNode ) {
                    Children = null;
                    Tag = LeafNode;
                }
            }


            public void SingleToMulti() {
                if( Children == null || Children.Length != 1 || Tag >= ChildCount ) {
                    throw new Exception( InconsistentStateMessage );
                }
                TrieNode oldNode = Children[0];
                Children = new TrieNode[ChildCount];
                Children[Tag] = oldNode;
                Tag = MultiNode;
            }


            public void MultiToSingle() {
                if( Children == null || Children.Length != ChildCount || Tag != MultiNode ) {
                    throw new Exception( InconsistentStateMessage );
                }
                int index = -1;

                // remove empty children
                for( int i = 0; i < Children.Length; i++ ) {
                    if( Children[i] != null &&
                        Children[i].Tag == LeafNode &&
                        Children[i].Payload == null ) {

                        Children[i] = null;

                    } else if( index != -1 ) {
                        index = i;

                    } else {
                        return;
                    }
                }

                if( index == -1 ) {
                    throw new Exception( InconsistentStateMessage );
                } else {
                    // if there's just one, convert to single
                    Children = new[] { Children[index] };
                    Tag = (byte)index;
                }
            }


            public bool GetAllChildren( ICollection<T> list, int limit ) {
                if( list.Count >= limit ) return false;
                if( Payload != null ) {
                    list.Add( Payload );
                }
                if( Children == null ) return true;

                switch( Tag ) {
                    case MultiNode:
                        // ReSharper disable LoopCanBeConvertedToQuery
                        for( int i = 0; i < Children.Length; i++ ) {
                            if( Children[i] == null ) continue;
                            if( !Children[i].GetAllChildren( list, limit ) ) return false;
                        }
                        // ReSharper restore LoopCanBeConvertedToQuery
                        return true;

                    case LeafNode:
                        return true;

                    default:
                        return Children[0].GetAllChildren( list, limit );
                }
            }
        }
    }
}