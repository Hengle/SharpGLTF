﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SharpGLTF.SchemaReflection
{
    /// <summary>
    /// Base class for all schema Types
    /// </summary>
    public abstract partial class SchemaType
    {
        #region constructor

        protected SchemaType(Context ctx) { _Owner = ctx; }

        #endregion

        #region data

        /// <summary>
        /// context where this type is stored
        /// </summary>
        private readonly Context _Owner;        

        /// <summary>
        /// identifier used for serialization and deserialization
        /// </summary>
        public abstract string PersistentName { get; }

        #endregion

        #region properties

        public String Description { get; set; }

        public Context Owner => _Owner;

        #endregion
    }

    [System.Diagnostics.DebuggerDisplay("{PersistentName}")]
    public sealed class StringType : SchemaType
    {
        #region constructor

        internal StringType(Context ctx) : base(ctx) { }

        #endregion

        #region properties

        public override string PersistentName => typeof(String).Name;

        #endregion
    }

    [System.Diagnostics.DebuggerDisplay("{PersistentName}")]
    public sealed class ObjectType : SchemaType
    {
        #region constructor

        internal ObjectType(Context ctx) : base(ctx) { }

        #endregion

        #region properties

        public override string PersistentName => typeof(Object).Name;

        #endregion
    }

    [System.Diagnostics.DebuggerDisplay("{PersistentName}")]
    public sealed class BlittableType : SchemaType
    {
        #region constructor        

        internal BlittableType(Context ctx, TypeInfo t, bool isNullable) : base(ctx)
        {
            if (t == typeof(String).GetTypeInfo()) isNullable = false;

            _Type = t;
            _IsNullable = isNullable;
        }

        #endregion

        #region data

        // https://en.wikipedia.org/wiki/Blittable_types

        private readonly TypeInfo _Type;
        private readonly Boolean _IsNullable;

        #endregion

        #region properties

        public TypeInfo DataType => _Type;

        public bool IsNullable => _IsNullable;

        public override string PersistentName => _IsNullable ? $"{_Type.Name}?" : _Type.Name;

        #endregion
    }    

    [System.Diagnostics.DebuggerDisplay("enum {PersistentName}")]
    public sealed class EnumType : SchemaType
    {
        #region constructor

        internal EnumType(Context ctx, string name, bool isNullable) : base(ctx)
        {
            _PersistentName = name;
            _IsNullable = isNullable;
        }

        #endregion

        #region data

        private readonly String _PersistentName;
        private readonly Boolean _IsNullable;

        private bool _UseIntegers;

        private readonly Dictionary<string, int> _Values = new Dictionary<string, int>();

        #endregion

        #region properties

        public bool IsNullable => _IsNullable;

        public override string PersistentName => _PersistentName;
        
        public bool UseIntegers { get => _UseIntegers; set => _UseIntegers = value; }

        public SchemaType ItemType => UseIntegers ? (SchemaType)Owner.UseBlittable(typeof(int).GetTypeInfo()) : Owner.UseString();

        public IEnumerable<KeyValuePair<string, int>> Values => _Values;

        #endregion

        #region API

        public void SetValue(string key, int val) { _Values[key] = val; }

        #endregion
    }

    [System.Diagnostics.DebuggerDisplay("{PersistentName}")]
    public sealed class ArrayType : SchemaType
    {
        #region constructor

        internal ArrayType(Context ctx, SchemaType element) : base(ctx)
        {
            _ItemType = element;
        }

        #endregion

        #region data

        private readonly SchemaType _ItemType;

        public SchemaType ItemType => _ItemType;

        #endregion

        #region properties

        public override string PersistentName => $"{_ItemType.PersistentName}[]";

        #endregion
    }    

    [System.Diagnostics.DebuggerDisplay("{PersistentName}")]
    public sealed class DictionaryType : SchemaType
    {
        #region lifecycle

        internal DictionaryType(Context ctx, SchemaType key,SchemaType val) : base(ctx)
        {            
            _KeyType = key;
            _ValueType = val;
        }

        #endregion

        #region data
        
        private readonly SchemaType _KeyType;
        private readonly SchemaType _ValueType;

        #endregion

        #region properties

        public SchemaType KeyType => _KeyType;

        public SchemaType ValueType => _ValueType;

        public override string PersistentName => $"<{_KeyType.PersistentName},{_ValueType.PersistentName}>[]";

        #endregion       
    }

    [System.Diagnostics.DebuggerDisplay("{FieldType.PersistentName} {PersistentName}")]
    public sealed class FieldInfo
    {
        #region lifecycle

        internal FieldInfo(ClassType owner, string name)
        {
            _Owner = owner;
            _PersistentName = name;
        }

        #endregion

        #region data

        private readonly ClassType _Owner;
        private readonly String _PersistentName;
        private SchemaType _FieldType;

        private Object _DefaultValue;
        private Object _MinimumValue;
        private Object _MaximumValue;

        private int _MinItems;
        private int _MaxItems;

        #endregion

        #region properties

        public ClassType DeclaringClass => _Owner;

        public String Description { get; set; }

        public string PersistentName => _PersistentName;                

        public SchemaType FieldType { get => _FieldType; set => _FieldType = value; }

        public Object DefaultValue { get => _DefaultValue; set => _DefaultValue = value; }
        public Object MinimumValue { get => _MinimumValue; set => _MinimumValue = value; }
        public Object MaximumValue { get => _MaximumValue; set => _MaximumValue = value; }

        public int MinItems { get => _MinItems; set => _MinItems = value; }
        public int MaxItems { get => _MaxItems; set => _MaxItems = value; }

        #endregion

        #region fluent api

        public FieldInfo SetDataType(SchemaType type) { _FieldType = type; return this; }

        public FieldInfo SetDataType(Type type, bool isNullable)
        {
            _FieldType = DeclaringClass.Owner.UseBlittable(type.GetTypeInfo(), isNullable);
            return this;
        }

        public FieldInfo RemoveDefaultValue() { _DefaultValue = null; return this; }

        public FieldInfo SetDefaultValue(string defval) { _DefaultValue = defval; return this; }

        public FieldInfo SetLimits(Decimal? min, Decimal? max) { _MinimumValue = min; _MaximumValue = max; return this; }

        public FieldInfo SetItemsRange(int min, int max = int.MaxValue) { _MinItems = min; _MaxItems = max; return this; }

        #endregion

        #region comparer helper

        private sealed class _Comparer : IEqualityComparer<FieldInfo>
        {
            public bool Equals(FieldInfo x, FieldInfo y)
            {
                return x._PersistentName == y._PersistentName;
            }

            public int GetHashCode(FieldInfo obj)
            {
                return obj._PersistentName.GetHashCode();
            }
        }

        private static readonly _Comparer _DefaultComparer = new _Comparer();

        public static IEqualityComparer<FieldInfo> Comparer => _DefaultComparer;        

        #endregion
    }

    [System.Diagnostics.DebuggerDisplay("class {PersistentName} : {BaseClass.PersistentName}")]
    public sealed class ClassType : SchemaType
    {
        #region constructor

        internal ClassType(Context ctx, string name) : base(ctx)
        {
            _PersistentName = name;            
        }

        #endregion

        #region data

        private readonly String _PersistentName;        

        private readonly HashSet<FieldInfo> _Fields = new HashSet<FieldInfo>(FieldInfo.Comparer);

        private ClassType _BaseClass;
        
        public override string PersistentName => _PersistentName;

        public ClassType BaseClass { get => _BaseClass; set => _BaseClass = value; }

        public IEnumerable<FieldInfo> Fields => _Fields;

        #endregion

        #region API

        public FieldInfo UseField(string name)
        {
            var f = new FieldInfo(this, name);

            _Fields.Add(f);

            return _Fields.FirstOrDefault(item => item.PersistentName == name);
        }

        #endregion
    }

    /// <summary>
    /// not used
    /// </summary>
    public sealed class ReferenceType : SchemaType
    {
        #region constructor

        internal ReferenceType(Context ctx, SchemaType refType) : base(ctx)
        {
            _ReferencedType = refType;
        }

        #endregion

        #region data

        // In code it has the representation List<Node>();
        // In serialization, it has the representation List<int>();

        private readonly SchemaType _ReferencedType;

        public override string PersistentName => throw new NotImplementedException();

        #endregion
    }

        

}