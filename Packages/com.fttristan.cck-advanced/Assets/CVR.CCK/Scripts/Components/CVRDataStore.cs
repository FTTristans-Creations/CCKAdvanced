//#define CVR_CLIENT
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;
using Unsafe = Unity.Collections.LowLevel.Unsafe.UnsafeUtility;

#if SCRIPTING_ENABLED
using MoonSharp.Interpreter;
using ABI_RC.Scripting.CVRSTL.Client;
using ABI_RC.Core.InteractionSystem;
using ABI.Scripting.CVRSTL.Common;
#endif

#if !CVR_CLIENT
using CVR.CCK;
#endif

namespace ABI.CCK.Components
{    
#if !CVR_CLIENT
    [AddComponentMenu("ChilloutVR/CVR Data Store")]
    [HelpURL(WebLinks.CCKDocsComponentsUrl + "data-store")]
    [CVRComponent(ComponentStatus.None)]
#endif
    public class CVRDataStore : MonoBehaviour, ISerializationCallbackReceiver
    {
        #region Serialized Fields
     
        /*public enum ExternalAccessLevel { NoAccess, ReadOnly, ReadWrite }
        [Tooltip("Defines the access level for external scripts.")]
        public ExternalAccessLevel externalAccess = ExternalAccessLevel.NoAccess;*/
        
        [SerializeField] private byte[] serializedBytes;
        [SerializeField] private Object[] serializedUnityObjects;
        
        public void OnBeforeSerialize() { /* not used */ }
        public void OnAfterDeserialize() => DeserializeRuntimeData(serializedBytes, serializedUnityObjects);
        
        #endregion Serialized Fields

        #region Runtime Data
        
        private readonly Dictionary<string, DataValue> runtimeData = new();
        
        #endregion Runtime Data

        #region Public API
        
        [PublicAPI]
        public bool SetValue<T>(string key, T value)
        {
            if (runtimeData.TryGetValue(key, out DataValue existingVal))
            {
                Type existingType = existingVal.GetValueType();
                Type newType = typeof(T);
                return existingType == newType && existingVal.SetValueSafe(value);
            }
            DataValue newVal = new();
            bool success = newVal.SetValue(value);
            if (success) runtimeData[key] = newVal;
            return success;
        }

        [PublicAPI]
        public bool SetValueForce<T>(string key, T value)
        {
            if (!runtimeData.TryGetValue(key, out DataValue val)) val = new();
            bool success = val.SetValue(value);
            if (success) runtimeData[key] = val;
            return success;
        }
        
        [PublicAPI]
        public bool TryGetValue<T>(string key, out T value)
        {
            if (runtimeData.TryGetValue(key, out DataValue val)) return val.TryGetValue(out value);
            value = default;
            return false;
        }
        
        [PublicAPI]
        public bool HasKey<T>(string key) => runtimeData.TryGetValue(key, out DataValue val) && val.IsSameType<T>();
        
        [PublicAPI]
        public bool HasKey(string key) => runtimeData.ContainsKey(key);
        
        [PublicAPI]
        public bool RemoveKey(string key) => runtimeData.Remove(key);
        
        [PublicAPI]
        public void Clear() => runtimeData.Clear();
        
        [PublicAPI]
        public string[] GetAllKeys()
        {
            string[] keys = new string[runtimeData.Count];
            runtimeData.Keys.CopyTo(keys, 0);
            return keys;
        }
        
        public Type GetValueType(string key) => runtimeData.TryGetValue(key, out DataValue val) ? val.GetValueType() : null;
        
#if SCRIPTING_ENABLED

        public (bool, DynValue) TryGetValue(InteractionContext context, string key)
        {
            if (!runtimeData.TryGetValue(key, out DataValue val)) return (false, DynValue.Nil);
            object value = val.GetValue();
            CVRLuaContext luaContext = context.SourceScript.Context;
            switch (value)
            {
                case Array array:
                {
                    DynValue[] elements = new DynValue[array.Length];
                    for (int i = 0; i < array.Length; i++) elements[i] = val.WrapValueForLua(luaContext, array.GetValue(i));
                    return (true, DynValue.NewTable(null, elements));
                }
                case null: return (false, DynValue.Nil);
                default: return (true, val.WrapValueForLua(luaContext, value));
            }
        }

        public bool SetValue(InteractionContext context, string key, DynValue dynValue)
        {
            object value = LuaAPITranslator.Unwrap(dynValue);
            if (runtimeData.TryGetValue(key, out DataValue existingVal))
            {
                Type existingType = existingVal.GetValueType();
                Type newType = value?.GetType();
                return existingType == newType && existingVal.SetValueSafe(value);
            }
            DataValue newVal = new();
            bool success = newVal.SetValue(value);
            if (success) runtimeData[key] = newVal;
            return success;
        }

        public bool SetValueForce(InteractionContext context, string key, DynValue dynValue)
        {
            object value = LuaAPITranslator.Unwrap(dynValue);
            if (!runtimeData.TryGetValue(key, out DataValue val)) val = new();
            bool success = val.SetValue(value);
            if (success) runtimeData[key] = val;
            return success;
        }

        public string GetValueType(InteractionContext context, string key)
        {
            Type type = GetValueType(key);
            return type?.FullName;
        }

        /// <summary>
        /// Checks if a key exists for Lua scripting.
        /// </summary>
        /// <param name="context">The interaction context</param>
        /// <param name="key">The key to check</param>
        /// <returns>True if key exists</returns>
        public bool HasKey(InteractionContext context, string key)
        {
            return HasKey(key);
        }

        public bool RemoveKey(InteractionContext context, string key)
        {
            return RemoveKey(key);
        }

        public string[] GetAllKeys(InteractionContext context)
        {
            return GetAllKeys();
        }

        public void Clear(InteractionContext context)
        {
            Clear();
        }

#endif
        
        #endregion Public API
        
        #region Deserialization
        
        private void DeserializeRuntimeData(byte[] bytes, Object[] objectArray)
        {
            runtimeData.Clear();

            if (bytes == null || bytes.Length == 0) return;

            int dataIndex = 0;
            int referenceIndex = 0;

            int count = BitConverter.ToInt32(bytes, dataIndex);
            dataIndex += 4;

            for (int i = 0; i < count; i++)
            {
                // Deserialize key
                string key = DeserializeString(bytes, ref dataIndex);
                
                // Deserialize type
                RuntimeDataType type = (RuntimeDataType)bytes[dataIndex++];

                // Deserialize directly into DataValue
                DataValue dataValue = DeserializeToDataValue(bytes, ref dataIndex, objectArray, ref referenceIndex, type);
                runtimeData[key] = dataValue;
            }
        }

        private static DataValue DeserializeToDataValue(byte[] data, ref int dataIndex, Object[] objectArray, ref int referenceIndex, RuntimeDataType type)
        {
            DataValue result = new();
            
            // Deserialize single values
            switch (type)
            {
                case RuntimeDataType.UnityObject:
                    result.unityObject = objectArray[referenceIndex++];
                    result.dataType = RuntimeDataType.UnityObject;
                    return result;
                case RuntimeDataType.String:
                    result.stringValue = DeserializeString(data, ref dataIndex);
                    result.dataType = RuntimeDataType.String;
                    return result;
                case RuntimeDataType.Int:
                    result.intValue = BitConverter.ToInt32(data, dataIndex); dataIndex += 4;
                    result.dataType = RuntimeDataType.Int;
                    return result;
                case RuntimeDataType.Float:
                    result.floatValue = BitConverter.ToSingle(data, dataIndex); dataIndex += 4;
                    result.dataType = RuntimeDataType.Float;
                    return result;
                case RuntimeDataType.Bool:
                    result.boolValue = BitConverter.ToBoolean(data, dataIndex); dataIndex += 1;
                    result.dataType = RuntimeDataType.Bool;
                    return result;
                case RuntimeDataType.Color:
                    float r = BitConverter.ToSingle(data, dataIndex); dataIndex += 4;
                    float g = BitConverter.ToSingle(data, dataIndex); dataIndex += 4;
                    float b = BitConverter.ToSingle(data, dataIndex); dataIndex += 4;
                    float a = BitConverter.ToSingle(data, dataIndex); dataIndex += 4;
                    result.colorValue = new Color(r, g, b, a);
                    result.dataType = RuntimeDataType.Color;
                    return result;
                case RuntimeDataType.LayerMask:
                    result.layerMaskValue = BitConverter.ToInt32(data, dataIndex); dataIndex += 4;
                    result.dataType = RuntimeDataType.LayerMask;
                    return result;
                case RuntimeDataType.Vector2:
                    float x2 = BitConverter.ToSingle(data, dataIndex); dataIndex += 4;
                    float y2 = BitConverter.ToSingle(data, dataIndex); dataIndex += 4;
                    result.vector2Value = new Vector2(x2, y2);
                    result.dataType = RuntimeDataType.Vector2;
                    return result;
                case RuntimeDataType.Vector3:
                    float x3 = BitConverter.ToSingle(data, dataIndex); dataIndex += 4;
                    float y3 = BitConverter.ToSingle(data, dataIndex); dataIndex += 4;
                    float z3 = BitConverter.ToSingle(data, dataIndex); dataIndex += 4;
                    result.vector3Value = new Vector3(x3, y3, z3);
                    result.dataType = RuntimeDataType.Vector3;
                    return result;
                case RuntimeDataType.Rect:
                    float rx = BitConverter.ToSingle(data, dataIndex); dataIndex += 4;
                    float ry = BitConverter.ToSingle(data, dataIndex); dataIndex += 4;
                    float rw = BitConverter.ToSingle(data, dataIndex); dataIndex += 4;
                    float rh = BitConverter.ToSingle(data, dataIndex); dataIndex += 4;
                    result.rectValue = new Rect(rx, ry, rw, rh);
                    result.dataType = RuntimeDataType.Rect;
                    return result;
                case RuntimeDataType.Bounds:
                    float bx = BitConverter.ToSingle(data, dataIndex); dataIndex += 4;
                    float by = BitConverter.ToSingle(data, dataIndex); dataIndex += 4;
                    float bz = BitConverter.ToSingle(data, dataIndex); dataIndex += 4;
                    float bsx = BitConverter.ToSingle(data, dataIndex); dataIndex += 4;
                    float bsy = BitConverter.ToSingle(data, dataIndex); dataIndex += 4;
                    float bsz = BitConverter.ToSingle(data, dataIndex); dataIndex += 4;
                    result.boundsValue = new Bounds(new Vector3(bx, by, bz), new Vector3(bsx, bsy, bsz));
                    result.dataType = RuntimeDataType.Bounds;
                    return result;
                case RuntimeDataType.Quaternion:
                    float qx = BitConverter.ToSingle(data, dataIndex); dataIndex += 4;
                    float qy = BitConverter.ToSingle(data, dataIndex); dataIndex += 4;
                    float qz = BitConverter.ToSingle(data, dataIndex); dataIndex += 4;
                    float qw = BitConverter.ToSingle(data, dataIndex); dataIndex += 4;
                    result.quaternionValue = new Quaternion(qx, qy, qz, qw);
                    result.dataType = RuntimeDataType.Quaternion;
                    return result;
                case RuntimeDataType.Vector2Int:
                    int x2i = BitConverter.ToInt32(data, dataIndex); dataIndex += 4;
                    int y2i = BitConverter.ToInt32(data, dataIndex); dataIndex += 4;
                    result.vector2IntValue = new Vector2Int(x2i, y2i);
                    result.dataType = RuntimeDataType.Vector2Int;
                    return result;
                case RuntimeDataType.Vector3Int:
                    int x3i = BitConverter.ToInt32(data, dataIndex); dataIndex += 4;
                    int y3i = BitConverter.ToInt32(data, dataIndex); dataIndex += 4;
                    int z3i = BitConverter.ToInt32(data, dataIndex); dataIndex += 4;
                    result.vector3IntValue = new Vector3Int(x3i, y3i, z3i);
                    result.dataType = RuntimeDataType.Vector3Int;
                    return result;
                case RuntimeDataType.RectInt:
                    int rix = BitConverter.ToInt32(data, dataIndex); dataIndex += 4;
                    int riy = BitConverter.ToInt32(data, dataIndex); dataIndex += 4;
                    int riw = BitConverter.ToInt32(data, dataIndex); dataIndex += 4;
                    int rih = BitConverter.ToInt32(data, dataIndex); dataIndex += 4;
                    result.rectIntValue = new RectInt(rix, riy, riw, rih);
                    result.dataType = RuntimeDataType.RectInt;
                    return result;
                case RuntimeDataType.BoundsInt:
                    int bix = BitConverter.ToInt32(data, dataIndex); dataIndex += 4;
                    int biy = BitConverter.ToInt32(data, dataIndex); dataIndex += 4;
                    int biz = BitConverter.ToInt32(data, dataIndex); dataIndex += 4;
                    int bisx = BitConverter.ToInt32(data, dataIndex); dataIndex += 4;
                    int bisy = BitConverter.ToInt32(data, dataIndex); dataIndex += 4;
                    int bisz = BitConverter.ToInt32(data, dataIndex); dataIndex += 4;
                    result.boundsIntValue = new BoundsInt(new Vector3Int(bix, biy, biz), new Vector3Int(bisx, bisy, bisz));
                    result.dataType = RuntimeDataType.BoundsInt;
                    return result;
                case RuntimeDataType.Vector4:
                    float x4 = BitConverter.ToSingle(data, dataIndex); dataIndex += 4;
                    float y4 = BitConverter.ToSingle(data, dataIndex); dataIndex += 4;
                    float z4 = BitConverter.ToSingle(data, dataIndex); dataIndex += 4;
                    float w4 = BitConverter.ToSingle(data, dataIndex); dataIndex += 4;
                    result.vector4Value = new Vector4(x4, y4, z4, w4);
                    result.dataType = RuntimeDataType.Vector4;
                    return result;
            }
            
            // Deserialize arrays
            int length = BitConverter.ToInt32(data, dataIndex);
            dataIndex += 4;

            switch (type)
            {
                case RuntimeDataType.UnityObjectArray:
                    result.unityObjectArray = new Object[length];
                    for (int i = 0; i < length; i++) result.unityObjectArray[i] = objectArray[referenceIndex++];
                    result.dataType = RuntimeDataType.UnityObjectArray;
                    break;
                case RuntimeDataType.StringArray:
                    result.stringValueArray = new string[length];
                    for (int i = 0; i < length; i++) result.stringValueArray[i] = DeserializeString(data, ref dataIndex);
                    result.dataType = RuntimeDataType.StringArray;
                    break;
                case RuntimeDataType.IntArray:
                    result.intValueArray = new int[length];
                    for (int i = 0; i < length; i++)
                    {
                        result.intValueArray[i] = BitConverter.ToInt32(data, dataIndex);
                        dataIndex += 4;
                    }
                    result.dataType = RuntimeDataType.IntArray;
                    break;
                case RuntimeDataType.FloatArray:
                    result.floatValueArray = new float[length];
                    for (int i = 0; i < length; i++)
                    {
                        result.floatValueArray[i] = BitConverter.ToSingle(data, dataIndex);
                        dataIndex += 4;
                    }
                    result.dataType = RuntimeDataType.FloatArray;
                    break;
                case RuntimeDataType.BoolArray:
                    result.boolValueArray = new bool[length];
                    for (int i = 0; i < length; i++)
                    {
                        result.boolValueArray[i] = BitConverter.ToBoolean(data, dataIndex); 
                        dataIndex += 1;
                    }
                    result.dataType = RuntimeDataType.BoolArray;
                    break;
                case RuntimeDataType.ColorArray:
                    result.colorValueArray = new Color[length];
                    for (int i = 0; i < length; i++)
                    {
                        float r = BitConverter.ToSingle(data, dataIndex); dataIndex += 4;
                        float g = BitConverter.ToSingle(data, dataIndex); dataIndex += 4;
                        float b = BitConverter.ToSingle(data, dataIndex); dataIndex += 4;
                        float a = BitConverter.ToSingle(data, dataIndex); dataIndex += 4;
                        result.colorValueArray[i] = new Color(r, g, b, a);
                    }
                    result.dataType = RuntimeDataType.ColorArray;
                    break;
                case RuntimeDataType.LayerMaskArray:
                    result.layerMaskValueArray = new LayerMask[length];
                    for (int i = 0; i < length; i++)
                    {
                        result.layerMaskValueArray[i] = BitConverter.ToInt32(data, dataIndex);
                        dataIndex += 4;
                    }
                    result.dataType = RuntimeDataType.LayerMaskArray;
                    break;
                case RuntimeDataType.Vector2Array:
                    result.vector2ValueArray = new Vector2[length];
                    for (int i = 0; i < length; i++)
                    {
                        float x = BitConverter.ToSingle(data, dataIndex); dataIndex += 4;
                        float y = BitConverter.ToSingle(data, dataIndex); dataIndex += 4;
                        result.vector2ValueArray[i] = new Vector2(x, y);
                    }
                    result.dataType = RuntimeDataType.Vector2Array;
                    break;
                case RuntimeDataType.Vector3Array:
                    result.vector3ValueArray = new Vector3[length];
                    for (int i = 0; i < length; i++)
                    {
                        float x = BitConverter.ToSingle(data, dataIndex); dataIndex += 4;
                        float y = BitConverter.ToSingle(data, dataIndex); dataIndex += 4;
                        float z = BitConverter.ToSingle(data, dataIndex); dataIndex += 4;
                        result.vector3ValueArray[i] = new Vector3(x, y, z);
                    }
                    result.dataType = RuntimeDataType.Vector3Array;
                    break;
                case RuntimeDataType.RectArray:
                    result.rectValueArray = new Rect[length];
                    for (int i = 0; i < length; i++)
                    {
                        float x = BitConverter.ToSingle(data, dataIndex); dataIndex += 4;
                        float y = BitConverter.ToSingle(data, dataIndex); dataIndex += 4;
                        float w = BitConverter.ToSingle(data, dataIndex); dataIndex += 4;
                        float h = BitConverter.ToSingle(data, dataIndex); dataIndex += 4;
                        result.rectValueArray[i] = new Rect(x, y, w, h);
                    }
                    result.dataType = RuntimeDataType.RectArray;
                    break;
                case RuntimeDataType.BoundsArray:
                    result.boundsValueArray = new Bounds[length];
                    for (int i = 0; i < length; i++)
                    {
                        float cx = BitConverter.ToSingle(data, dataIndex); dataIndex += 4;
                        float cy = BitConverter.ToSingle(data, dataIndex); dataIndex += 4;
                        float cz = BitConverter.ToSingle(data, dataIndex); dataIndex += 4;
                        float sx = BitConverter.ToSingle(data, dataIndex); dataIndex += 4;
                        float sy = BitConverter.ToSingle(data, dataIndex); dataIndex += 4;
                        float sz = BitConverter.ToSingle(data, dataIndex); dataIndex += 4;
                        result.boundsValueArray[i] = new Bounds(new Vector3(cx, cy, cz), new Vector3(sx, sy, sz));
                    }
                    result.dataType = RuntimeDataType.BoundsArray;
                    break;
                case RuntimeDataType.QuaternionArray:
                    result.quaternionValueArray = new Quaternion[length];
                    for (int i = 0; i < length; i++)
                    {
                        float x = BitConverter.ToSingle(data, dataIndex); dataIndex += 4;
                        float y = BitConverter.ToSingle(data, dataIndex); dataIndex += 4;
                        float z = BitConverter.ToSingle(data, dataIndex); dataIndex += 4;
                        float w = BitConverter.ToSingle(data, dataIndex); dataIndex += 4;
                        result.quaternionValueArray[i] = new Quaternion(x, y, z, w);
                    }
                    result.dataType = RuntimeDataType.QuaternionArray;
                    break;
                case RuntimeDataType.Vector2IntArray:
                    result.vector2IntValueArray = new Vector2Int[length];
                    for (int i = 0; i < length; i++)
                    {
                        int x = BitConverter.ToInt32(data, dataIndex); dataIndex += 4;
                        int y = BitConverter.ToInt32(data, dataIndex); dataIndex += 4;
                        result.vector2IntValueArray[i] = new Vector2Int(x, y);
                    }
                    result.dataType = RuntimeDataType.Vector2IntArray;
                    break;
                case RuntimeDataType.Vector3IntArray:
                    result.vector3IntValueArray = new Vector3Int[length];
                    for (int i = 0; i < length; i++)
                    {
                        int x = BitConverter.ToInt32(data, dataIndex); dataIndex += 4;
                        int y = BitConverter.ToInt32(data, dataIndex); dataIndex += 4;
                        int z = BitConverter.ToInt32(data, dataIndex); dataIndex += 4;
                        result.vector3IntValueArray[i] = new Vector3Int(x, y, z);
                    }
                    result.dataType = RuntimeDataType.Vector3IntArray;
                    break;
                case RuntimeDataType.RectIntArray:
                    result.rectIntValueArray = new RectInt[length];
                    for (int i = 0; i < length; i++)
                    {
                        int x = BitConverter.ToInt32(data, dataIndex); dataIndex += 4;
                        int y = BitConverter.ToInt32(data, dataIndex); dataIndex += 4;
                        int w = BitConverter.ToInt32(data, dataIndex); dataIndex += 4;
                        int h = BitConverter.ToInt32(data, dataIndex); dataIndex += 4;
                        result.rectIntValueArray[i] = new RectInt(x, y, w, h);
                    }
                    result.dataType = RuntimeDataType.RectIntArray;
                    break;
                case RuntimeDataType.BoundsIntArray:
                    result.boundsIntValueArray = new BoundsInt[length];
                    for (int i = 0; i < length; i++)
                    {
                        int px = BitConverter.ToInt32(data, dataIndex); dataIndex += 4;
                        int py = BitConverter.ToInt32(data, dataIndex); dataIndex += 4;
                        int pz = BitConverter.ToInt32(data, dataIndex); dataIndex += 4;
                        int sx = BitConverter.ToInt32(data, dataIndex); dataIndex += 4;
                        int sy = BitConverter.ToInt32(data, dataIndex); dataIndex += 4;
                        int sz = BitConverter.ToInt32(data, dataIndex); dataIndex += 4;
                        result.boundsIntValueArray[i] = new BoundsInt(new Vector3Int(px, py, pz), new Vector3Int(sx, sy, sz));
                    }
                    result.dataType = RuntimeDataType.BoundsIntArray;
                    break;
                case RuntimeDataType.Vector4Array:
                    result.vector4ValueArray = new Vector4[length];
                    for (int i = 0; i < length; i++)
                    {
                        float x = BitConverter.ToSingle(data, dataIndex); dataIndex += 4;
                        float y = BitConverter.ToSingle(data, dataIndex); dataIndex += 4;
                        float z = BitConverter.ToSingle(data, dataIndex); dataIndex += 4;
                        float w = BitConverter.ToSingle(data, dataIndex); dataIndex += 4;
                        result.vector4ValueArray[i] = new Vector4(x, y, z, w);
                    }
                    result.dataType = RuntimeDataType.Vector4Array;
                    break;
            }
            return result;
        }

        private static string DeserializeString(byte[] data, ref int dataIndex)
        {
            int length = BitConverter.ToInt32(data, dataIndex); dataIndex += 4;
            string result = Encoding.Unicode.GetString(data, dataIndex, length); dataIndex += length;
            return result;
        }
        
        #endregion Deserialization
        
        #region DataValue Union

        private const int DataTypeCount = 17;
        
        private enum RuntimeDataType : byte
        {
            UnityObject,
            String,
            Int,
            Float,
            Bool,
            Vector2,
            Vector2Int,
            Vector3,
            Vector3Int,
            Vector4,
            Quaternion,
            Rect,
            RectInt,
            Bounds,
            BoundsInt,
            Color,
            LayerMask,
            
            UnityObjectArray,
            StringArray,
            IntArray,
            FloatArray,
            BoolArray,
            Vector2Array,
            Vector2IntArray,
            Vector3Array,
            Vector3IntArray,
            Vector4Array,
            QuaternionArray,
            RectArray,
            RectIntArray,
            BoundsArray,
            BoundsIntArray,
            ColorArray,
            LayerMaskArray,
        }
        
        [StructLayout(LayoutKind.Explicit)]
        private struct DataValue
        {
            // Discriminator lives at the start; keep it separate from payload regions.
            [FieldOffset(0)] public RuntimeDataType dataType;

            // ---------------------------
            // Value-type union (blittable)
            // Reserve 24 bytes (largest of Bounds/BoundsInt).
            // Starts at offset 8 and occupies [8..31].
            // ---------------------------
            [FieldOffset(8)]  public int intValue;
            [FieldOffset(8)]  public float floatValue;
            [FieldOffset(8)]  public bool boolValue;                // will occupy 1 byte in this region
            [FieldOffset(8)]  public LayerMask layerMaskValue;      // int under the hood
            [FieldOffset(8)]  public Color colorValue;              // 16 bytes
            [FieldOffset(8)]  public Vector2 vector2Value;          // 8 bytes
            [FieldOffset(8)]  public Vector3 vector3Value;          // 12 bytes
            [FieldOffset(8)]  public Vector4 vector4Value;          // 16 bytes
            [FieldOffset(8)]  public Rect rectValue;                // 16 bytes
            [FieldOffset(8)]  public Bounds boundsValue;            // 24 bytes (Vector3 + Vector3)
            [FieldOffset(8)]  public Quaternion quaternionValue;    // 16 bytes
            [FieldOffset(8)]  public Vector2Int vector2IntValue;    // 8 bytes
            [FieldOffset(8)]  public Vector3Int vector3IntValue;    // 12 bytes
            [FieldOffset(8)]  public RectInt rectIntValue;          // 16 bytes
            [FieldOffset(8)]  public BoundsInt boundsIntValue;      // 24 bytes

            // ---------------------------
            // Reference-type union (managed)
            // Starts after value region at offset 32 so it NEVER overlaps with value bytes.
            // GC only sees references here; when storing values, these remain null.
            // ---------------------------
            [FieldOffset(32)] public Object unityObject;
            [FieldOffset(32)] public string stringValue;
            [FieldOffset(32)] public Object[] unityObjectArray;
            [FieldOffset(32)] public string[] stringValueArray;
            [FieldOffset(32)] public int[] intValueArray;
            [FieldOffset(32)] public float[] floatValueArray;
            [FieldOffset(32)] public bool[] boolValueArray;
            [FieldOffset(32)] public LayerMask[] layerMaskValueArray;
            [FieldOffset(32)] public Color[] colorValueArray;
            [FieldOffset(32)] public Vector2[] vector2ValueArray;
            [FieldOffset(32)] public Vector3[] vector3ValueArray;
            [FieldOffset(32)] public Vector4[] vector4ValueArray;
            [FieldOffset(32)] public Rect[] rectValueArray;
            [FieldOffset(32)] public Bounds[] boundsValueArray;
            [FieldOffset(32)] public Quaternion[] quaternionValueArray;
            [FieldOffset(32)] public Vector2Int[] vector2IntValueArray;
            [FieldOffset(32)] public Vector3Int[] vector3IntValueArray;
            [FieldOffset(32)] public RectInt[] rectIntValueArray;
            [FieldOffset(32)] public BoundsInt[] boundsIntValueArray;
            
            public bool TryGetValue<T>(out T value)
            {
                Type type = typeof(T);
                switch (dataType)
                {
                    case RuntimeDataType.String: if (type == typeof(string)) { value = (T)(object)stringValue; return true; } break;
                    case RuntimeDataType.Int: if (type == typeof(int)) { value = Unsafe.As<int, T>(ref intValue); return true; } break;
                    case RuntimeDataType.Float: if (type == typeof(float)) { value = Unsafe.As<float, T>(ref floatValue); return true; } break;
                    case RuntimeDataType.Bool: if (type == typeof(bool)) { value = Unsafe.As<bool, T>(ref boolValue); return true; } break;
                    case RuntimeDataType.Color: if (type == typeof(Color)) { value = Unsafe.As<Color, T>(ref colorValue); return true; } break;
                    case RuntimeDataType.LayerMask: if (type == typeof(LayerMask)) { value = Unsafe.As<LayerMask, T>(ref layerMaskValue); return true; } break;
                    case RuntimeDataType.Vector2: if (type == typeof(Vector2)) { value = Unsafe.As<Vector2, T>(ref vector2Value); return true; } break;
                    case RuntimeDataType.Vector3: if (type == typeof(Vector3)) { value = Unsafe.As<Vector3, T>(ref vector3Value); return true; } break;
                    case RuntimeDataType.Rect: if (type == typeof(Rect)) { value = Unsafe.As<Rect, T>(ref rectValue); return true; } break;
                    case RuntimeDataType.Bounds: if (type == typeof(Bounds)) { value = Unsafe.As<Bounds, T>(ref boundsValue); return true; } break;
                    case RuntimeDataType.Quaternion: if (type == typeof(Quaternion)) { value = Unsafe.As<Quaternion, T>(ref quaternionValue); return true; } break;
                    case RuntimeDataType.Vector2Int: if (type == typeof(Vector2Int)) { value = Unsafe.As<Vector2Int, T>(ref vector2IntValue); return true; } break;
                    case RuntimeDataType.Vector3Int: if (type == typeof(Vector3Int)) { value = Unsafe.As<Vector3Int, T>(ref vector3IntValue); return true; } break;
                    case RuntimeDataType.RectInt: if (type == typeof(RectInt)) { value = Unsafe.As<RectInt, T>(ref rectIntValue); return true; } break;
                    case RuntimeDataType.BoundsInt: if (type == typeof(BoundsInt)) { value = Unsafe.As<BoundsInt, T>(ref boundsIntValue); return true; } break;
                    case RuntimeDataType.Vector4: if (type == typeof(Vector4)) { value = Unsafe.As<Vector4, T>(ref vector4Value); return true; } break;
                    case RuntimeDataType.UnityObject: if (typeof(Object).IsAssignableFrom(type)) { value = (T)(object)unityObject; return true; } break;
                    case RuntimeDataType.StringArray: if (type == typeof(string[])) { value = (T)(object)stringValueArray; return true; } break;
                    case RuntimeDataType.IntArray: if (type == typeof(int[])) { value = (T)(object)intValueArray; return true; } break;
                    case RuntimeDataType.FloatArray: if (type == typeof(float[])) { value = (T)(object)floatValueArray; return true; } break;
                    case RuntimeDataType.BoolArray: if (type == typeof(bool[])) { value = (T)(object)boolValueArray; return true; } break;
                    case RuntimeDataType.ColorArray: if (type == typeof(Color[])) { value = (T)(object)colorValueArray; return true; } break;
                    case RuntimeDataType.LayerMaskArray: if (type == typeof(LayerMask[])) { value = (T)(object)layerMaskValueArray; return true; } break;
                    case RuntimeDataType.Vector2Array: if (type == typeof(Vector2[])) { value = (T)(object)vector2ValueArray; return true; } break;
                    case RuntimeDataType.Vector3Array: if (type == typeof(Vector3[])) { value = (T)(object)vector3ValueArray; return true; } break;
                    case RuntimeDataType.RectArray: if (type == typeof(Rect[])) { value = (T)(object)rectValueArray; return true; } break;
                    case RuntimeDataType.BoundsArray: if (type == typeof(Bounds[])) { value = (T)(object)boundsValueArray; return true; } break;
                    case RuntimeDataType.QuaternionArray: if (type == typeof(Quaternion[])) { value = (T)(object)quaternionValueArray; return true; } break;
                    case RuntimeDataType.Vector2IntArray: if (type == typeof(Vector2Int[])) { value = (T)(object)vector2IntValueArray; return true; } break;
                    case RuntimeDataType.Vector3IntArray: if (type == typeof(Vector3Int[])) { value = (T)(object)vector3IntValueArray; return true; } break;
                    case RuntimeDataType.RectIntArray: if (type == typeof(RectInt[])) { value = (T)(object)rectIntValueArray; return true; } break;
                    case RuntimeDataType.BoundsIntArray: if (type == typeof(BoundsInt[])) { value = (T)(object)boundsIntValueArray; return true; } break;
                    case RuntimeDataType.Vector4Array: if (type == typeof(Vector4[])) { value = (T)(object)vector4ValueArray; return true; } break;
                    case RuntimeDataType.UnityObjectArray: if (typeof(Object[]).IsAssignableFrom(type)) { value = (T)(object)unityObjectArray; return true; } break;
                }
                value = default;
                return false;
            }
            
            public bool SetValue<T>(T value)
            {
                Type type = typeof(T);
                if (type == typeof(Object)) { unityObject = value as Object; dataType = RuntimeDataType.UnityObject; return true; }
                if (type == typeof(string)) { stringValue = value as string; dataType = RuntimeDataType.String; return true; }
                if (type == typeof(int)) { intValue = Unsafe.As<T, int>(ref value); dataType = RuntimeDataType.Int; return true; }
                if (type == typeof(float)) { floatValue = Unsafe.As<T, float>(ref value); dataType = RuntimeDataType.Float; return true; }
                if (type == typeof(bool)) { boolValue = Unsafe.As<T, bool>(ref value); dataType = RuntimeDataType.Bool; return true; }
                if (type == typeof(Color)) { colorValue = Unsafe.As<T, Color>(ref value); dataType = RuntimeDataType.Color; return true; }
                if (type == typeof(LayerMask)) { layerMaskValue = Unsafe.As<T, LayerMask>(ref value); dataType = RuntimeDataType.LayerMask; return true; }
                if (type == typeof(Vector2)) { vector2Value = Unsafe.As<T, Vector2>(ref value); dataType = RuntimeDataType.Vector2; return true; }
                if (type == typeof(Vector3)) { vector3Value = Unsafe.As<T, Vector3>(ref value); dataType = RuntimeDataType.Vector3; return true; }
                if (type == typeof(Rect)) { rectValue = Unsafe.As<T, Rect>(ref value); dataType = RuntimeDataType.Rect; return true; }
                if (type == typeof(Bounds)) { boundsValue = Unsafe.As<T, Bounds>(ref value); dataType = RuntimeDataType.Bounds; return true; }
                if (type == typeof(Quaternion)) { quaternionValue = Unsafe.As<T, Quaternion>(ref value); dataType = RuntimeDataType.Quaternion; return true; }
                if (type == typeof(Vector2Int)) { vector2IntValue = Unsafe.As<T, Vector2Int>(ref value); dataType = RuntimeDataType.Vector2Int; return true; }
                if (type == typeof(Vector3Int)) { vector3IntValue = Unsafe.As<T, Vector3Int>(ref value); dataType = RuntimeDataType.Vector3Int; return true; }
                if (type == typeof(RectInt)) { rectIntValue = Unsafe.As<T, RectInt>(ref value); dataType = RuntimeDataType.RectInt; return true; }
                if (type == typeof(BoundsInt)) { boundsIntValue = Unsafe.As<T, BoundsInt>(ref value); dataType = RuntimeDataType.BoundsInt; return true; }
                if (type == typeof(Vector4)) { vector4Value = Unsafe.As<T, Vector4>(ref value); dataType = RuntimeDataType.Vector4; return true; }
                if (type == typeof(Object[])) { unityObjectArray = value as Object[]; dataType = RuntimeDataType.UnityObjectArray; return true; }
                if (type == typeof(string[])) { stringValueArray = value as string[]; dataType = RuntimeDataType.StringArray; return true; }
                if (type == typeof(int[])) { intValueArray = value as int[]; dataType = RuntimeDataType.IntArray; return true; }
                if (type == typeof(float[])) { floatValueArray = value as float[]; dataType = RuntimeDataType.FloatArray; return true; }
                if (type == typeof(bool[])) { boolValueArray = value as bool[]; dataType = RuntimeDataType.BoolArray; return true; }
                if (type == typeof(Color[])) { colorValueArray = value as Color[]; dataType = RuntimeDataType.ColorArray; return true; }
                if (type == typeof(LayerMask[])) { layerMaskValueArray = value as LayerMask[]; dataType = RuntimeDataType.LayerMaskArray; return true; }
                if (type == typeof(Vector2[])) { vector2ValueArray = value as Vector2[]; dataType = RuntimeDataType.Vector2Array; return true; }
                if (type == typeof(Vector3[])) { vector3ValueArray = value as Vector3[]; dataType = RuntimeDataType.Vector3Array; return true; }
                if (type == typeof(Rect[])) { rectValueArray = value as Rect[]; dataType = RuntimeDataType.RectArray; return true; }
                if (type == typeof(Bounds[])) { boundsValueArray = value as Bounds[]; dataType = RuntimeDataType.BoundsArray; return true; }
                if (type == typeof(Quaternion[])) { quaternionValueArray = value as Quaternion[]; dataType = RuntimeDataType.QuaternionArray; return true; }
                if (type == typeof(Vector2Int[])) { vector2IntValueArray = value as Vector2Int[]; dataType = RuntimeDataType.Vector2IntArray; return true; }
                if (type == typeof(Vector3Int[])) { vector3IntValueArray = value as Vector3Int[]; dataType = RuntimeDataType.Vector3IntArray; return true; }
                if (type == typeof(RectInt[])) { rectIntValueArray = value as RectInt[]; dataType = RuntimeDataType.RectIntArray; return true; }
                if (type == typeof(BoundsInt[])) { boundsIntValueArray = value as BoundsInt[]; dataType = RuntimeDataType.BoundsIntArray; return true; }
                if (type == typeof(Vector4[])) { vector4ValueArray = value as Vector4[]; dataType = RuntimeDataType.Vector4Array; return true; }
                return false;
            }
            
            public bool SetValueSafe<T>(T value)
            {
                Type type = typeof(T);
                switch (dataType)
                {
                    case RuntimeDataType.UnityObject: if (typeof(Object).IsAssignableFrom(type)) { unityObject = value as Object; return true; } break;
                    case RuntimeDataType.String: if (type == typeof(string)) { stringValue = value as string; return true; } break;
                    case RuntimeDataType.Int: if (type == typeof(int)) { intValue = Unsafe.As<T, int>(ref value); return true; } break;
                    case RuntimeDataType.Float: if (type == typeof(float)) { floatValue = Unsafe.As<T, float>(ref value); return true; } break;
                    case RuntimeDataType.Bool: if (type == typeof(bool)) { boolValue = Unsafe.As<T, bool>(ref value); return true; } break;
                    case RuntimeDataType.Color: if (type == typeof(Color)) { colorValue = Unsafe.As<T, Color>(ref value); return true; } break;
                    case RuntimeDataType.LayerMask: if (type == typeof(LayerMask)) { layerMaskValue = Unsafe.As<T, LayerMask>(ref value); return true; } break;
                    case RuntimeDataType.Vector2: if (type == typeof(Vector2)) { vector2Value = Unsafe.As<T, Vector2>(ref value); return true; } break;
                    case RuntimeDataType.Vector3: if (type == typeof(Vector3)) { vector3Value = Unsafe.As<T, Vector3>(ref value); return true; } break;
                    case RuntimeDataType.Vector4: if (type == typeof(Vector4)) { vector4Value = Unsafe.As<T, Vector4>(ref value); return true; } break;
                    case RuntimeDataType.Quaternion: if (type == typeof(Quaternion)) { quaternionValue = Unsafe.As<T, Quaternion>(ref value); return true; } break;
                    case RuntimeDataType.Vector2Int: if (type == typeof(Vector2Int)) { vector2IntValue = Unsafe.As<T, Vector2Int>(ref value); return true; } break;
                    case RuntimeDataType.Vector3Int: if (type == typeof(Vector3Int)) { vector3IntValue = Unsafe.As<T, Vector3Int>(ref value); return true; } break;
                    case RuntimeDataType.Rect: if (type == typeof(Rect)) { rectValue = Unsafe.As<T, Rect>(ref value); return true; } break;
                    case RuntimeDataType.Bounds: if (type == typeof(Bounds)) { boundsValue = Unsafe.As<T, Bounds>(ref value); return true; } break;
                    case RuntimeDataType.RectInt: if (type == typeof(RectInt)) { rectIntValue = Unsafe.As<T, RectInt>(ref value); return true; } break;
                    case RuntimeDataType.BoundsInt: if (type == typeof(BoundsInt)) { boundsIntValue = Unsafe.As<T, BoundsInt>(ref value); return true; } break;
                    case RuntimeDataType.UnityObjectArray: if (typeof(Object[]).IsAssignableFrom(type)) { unityObjectArray = value as Object[]; return true; } break;
                    case RuntimeDataType.StringArray: if (type == typeof(string[])) { stringValueArray = value as string[]; return true; } break;
                    case RuntimeDataType.IntArray: if (type == typeof(int[])) { intValueArray = value as int[]; return true; } break;
                    case RuntimeDataType.FloatArray: if (type == typeof(float[])) { floatValueArray = value as float[]; return true; } break;
                    case RuntimeDataType.BoolArray: if (type == typeof(bool[])) { boolValueArray = value as bool[]; return true; } break;
                    case RuntimeDataType.ColorArray: if (type == typeof(Color[])) { colorValueArray = value as Color[]; return true; } break;
                    case RuntimeDataType.LayerMaskArray: if (type == typeof(LayerMask[])) { layerMaskValueArray = value as LayerMask[]; return true; } break;
                    case RuntimeDataType.Vector2Array: if (type == typeof(Vector2[])) { vector2ValueArray = value as Vector2[]; return true; } break;
                    case RuntimeDataType.Vector3Array: if (type == typeof(Vector3[])) { vector3ValueArray = value as Vector3[]; return true; } break;
                    case RuntimeDataType.Vector4Array: if (type == typeof(Vector4[])) { vector4ValueArray = value as Vector4[]; return true; } break;
                    case RuntimeDataType.QuaternionArray: if (type == typeof(Quaternion[])) { quaternionValueArray = value as Quaternion[]; return true; } break;
                    case RuntimeDataType.Vector2IntArray: if (type == typeof(Vector2Int[])) { vector2IntValueArray = value as Vector2Int[]; return true; } break;
                    case RuntimeDataType.Vector3IntArray: if (type == typeof(Vector3Int[])) { vector3IntValueArray = value as Vector3Int[]; return true; } break;
                    case RuntimeDataType.RectArray: if (type == typeof(Rect[])) { rectValueArray = value as Rect[]; return true; } break;
                    case RuntimeDataType.BoundsArray: if (type == typeof(Bounds[])) { boundsValueArray = value as Bounds[]; return true; } break;
                    case RuntimeDataType.RectIntArray: if (type == typeof(RectInt[])) { rectIntValueArray = value as RectInt[]; return true; } break;
                    case RuntimeDataType.BoundsIntArray: if (type == typeof(BoundsInt[])) { boundsIntValueArray = value as BoundsInt[]; return true; } break;
                }
                return false;
            }

            public bool IsSameType<T>()
            {
                Type storedType = GetValueType();
                Type requestedType =  typeof(T);
                return storedType != null 
                    && storedType == requestedType
                    || (typeof(Object).IsAssignableFrom(requestedType) && typeof(Object).IsAssignableFrom(storedType));
            }

            public Type GetValueType() =>
                dataType switch
                {
                    RuntimeDataType.UnityObject => typeof(Object),
                    RuntimeDataType.String => typeof(string),
                    RuntimeDataType.Int => typeof(int),
                    RuntimeDataType.Float => typeof(float),
                    RuntimeDataType.Bool => typeof(bool),
                    RuntimeDataType.Color => typeof(Color),
                    RuntimeDataType.LayerMask => typeof(LayerMask),
                    RuntimeDataType.Vector2 => typeof(Vector2),
                    RuntimeDataType.Vector3 => typeof(Vector3),
                    RuntimeDataType.Vector4 => typeof(Vector4),
                    RuntimeDataType.Quaternion => typeof(Quaternion),
                    RuntimeDataType.Vector2Int => typeof(Vector2Int),
                    RuntimeDataType.Vector3Int => typeof(Vector3Int),
                    RuntimeDataType.Rect => typeof(Rect),
                    RuntimeDataType.Bounds => typeof(Bounds),
                    RuntimeDataType.RectInt => typeof(RectInt),
                    RuntimeDataType.BoundsInt => typeof(BoundsInt),
                    RuntimeDataType.UnityObjectArray => typeof(Object[]),
                    RuntimeDataType.StringArray => typeof(string[]),
                    RuntimeDataType.IntArray => typeof(int[]),
                    RuntimeDataType.FloatArray => typeof(float[]),
                    RuntimeDataType.BoolArray => typeof(bool[]),
                    RuntimeDataType.ColorArray => typeof(Color[]),
                    RuntimeDataType.LayerMaskArray => typeof(LayerMask[]),
                    RuntimeDataType.Vector2Array => typeof(Vector2[]),
                    RuntimeDataType.Vector3Array => typeof(Vector3[]),
                    RuntimeDataType.Vector4Array => typeof(Vector4[]),
                    RuntimeDataType.QuaternionArray => typeof(Quaternion[]),
                    RuntimeDataType.Vector2IntArray => typeof(Vector2Int[]),
                    RuntimeDataType.Vector3IntArray => typeof(Vector3Int[]),
                    RuntimeDataType.RectArray => typeof(Rect[]),
                    RuntimeDataType.BoundsArray => typeof(Bounds[]),
                    RuntimeDataType.RectIntArray => typeof(RectInt[]),
                    RuntimeDataType.BoundsIntArray => typeof(BoundsInt[]),
                    _ => null
                };

            #region Non-Generics

            public object GetValue() =>
                dataType switch
                {
                    RuntimeDataType.UnityObject => unityObject,
                    RuntimeDataType.String => stringValue,
                    RuntimeDataType.Int => intValue,
                    RuntimeDataType.Float => floatValue,
                    RuntimeDataType.Bool => boolValue,
                    RuntimeDataType.Color => colorValue,
                    RuntimeDataType.LayerMask => layerMaskValue,
                    RuntimeDataType.Vector2 => vector2Value,
                    RuntimeDataType.Vector3 => vector3Value,
                    RuntimeDataType.Vector4 => vector4Value,
                    RuntimeDataType.Quaternion => quaternionValue,
                    RuntimeDataType.Vector2Int => vector2IntValue,
                    RuntimeDataType.Vector3Int => vector3IntValue,
                    RuntimeDataType.Rect => rectValue,
                    RuntimeDataType.Bounds => boundsValue,
                    RuntimeDataType.RectInt => rectIntValue,
                    RuntimeDataType.BoundsInt => boundsIntValue,
                    RuntimeDataType.UnityObjectArray => unityObjectArray,
                    RuntimeDataType.StringArray => stringValueArray,
                    RuntimeDataType.IntArray => intValueArray,
                    RuntimeDataType.FloatArray => floatValueArray,
                    RuntimeDataType.BoolArray => boolValueArray,
                    RuntimeDataType.ColorArray => colorValueArray,
                    RuntimeDataType.LayerMaskArray => layerMaskValueArray,
                    RuntimeDataType.Vector2Array => vector2ValueArray,
                    RuntimeDataType.Vector3Array => vector3ValueArray,
                    RuntimeDataType.Vector4Array => vector4ValueArray,
                    RuntimeDataType.QuaternionArray => quaternionValueArray,
                    RuntimeDataType.Vector2IntArray => vector2IntValueArray,
                    RuntimeDataType.Vector3IntArray => vector3IntValueArray,
                    RuntimeDataType.RectArray => rectValueArray,
                    RuntimeDataType.BoundsArray => boundsValueArray,
                    RuntimeDataType.RectIntArray => rectIntValueArray,
                    RuntimeDataType.BoundsIntArray => boundsIntValueArray,
                    _ => null
                };

            public bool SetValue(object value)
            {
                if (value == null) return false;
                Type type = value.GetType();
                if (typeof(Object).IsAssignableFrom(type))
                {
                    unityObject = value as Object;
                    dataType = RuntimeDataType.UnityObject;
                }
                else if (type == typeof(string))
                {
                    stringValue = (string)value;
                    dataType = RuntimeDataType.String;
                }
                else if (type == typeof(int))
                {
                    intValue = (int)value;
                    dataType = RuntimeDataType.Int;
                }
                else if (type == typeof(float))
                {
                    floatValue = (float)value;
                    dataType = RuntimeDataType.Float;
                }
                else if (type == typeof(bool))
                {
                    boolValue = (bool)value;
                    dataType = RuntimeDataType.Bool;
                }
                else if (type == typeof(Color))
                {
                    colorValue = (Color)value;
                    dataType = RuntimeDataType.Color;
                }
                else if (type == typeof(LayerMask))
                {
                    layerMaskValue = (LayerMask)value;
                    dataType = RuntimeDataType.LayerMask;
                }
                else if (type == typeof(Vector2))
                {
                    vector2Value = (Vector2)value;
                    dataType = RuntimeDataType.Vector2;
                }
                else if (type == typeof(Vector3))
                {
                    vector3Value = (Vector3)value;
                    dataType = RuntimeDataType.Vector3;
                }
                else if (type == typeof(Vector4))
                {
                    vector4Value = (Vector4)value;
                    dataType = RuntimeDataType.Vector4;
                }
                else if (type == typeof(Quaternion))
                {
                    quaternionValue = (Quaternion)value;
                    dataType = RuntimeDataType.Quaternion;
                }
                else if (type == typeof(Vector2Int))
                {
                    vector2IntValue = (Vector2Int)value;
                    dataType = RuntimeDataType.Vector2Int;
                }
                else if (type == typeof(Vector3Int))
                {
                    vector3IntValue = (Vector3Int)value;
                    dataType = RuntimeDataType.Vector3Int;
                }
                else if (type == typeof(Rect))
                {
                    rectValue = (Rect)value;
                    dataType = RuntimeDataType.Rect;
                }
                else if (type == typeof(Bounds))
                {
                    boundsValue = (Bounds)value;
                    dataType = RuntimeDataType.Bounds;
                }
                else if (type == typeof(RectInt))
                {
                    rectIntValue = (RectInt)value;
                    dataType = RuntimeDataType.RectInt;
                }
                else if (type == typeof(BoundsInt))
                {
                    boundsIntValue = (BoundsInt)value;
                    dataType = RuntimeDataType.BoundsInt;
                }
                else if (typeof(Object[]).IsAssignableFrom(type))
                {
                    unityObjectArray = value as Object[];
                    dataType = RuntimeDataType.UnityObjectArray;
                }
                else if (type == typeof(string[]))
                {
                    stringValueArray = (string[])value;
                    dataType = RuntimeDataType.StringArray;
                }
                else if (type == typeof(int[]))
                {
                    intValueArray = (int[])value;
                    dataType = RuntimeDataType.IntArray;
                }
                else if (type == typeof(float[]))
                {
                    floatValueArray = (float[])value;
                    dataType = RuntimeDataType.FloatArray;
                }
                else if (type == typeof(bool[]))
                {
                    boolValueArray = (bool[])value;
                    dataType = RuntimeDataType.BoolArray;
                }
                else if (type == typeof(Color[]))
                {
                    colorValueArray = (Color[])value;
                    dataType = RuntimeDataType.ColorArray;
                }
                else if (type == typeof(LayerMask[]))
                {
                    layerMaskValueArray = (LayerMask[])value;
                    dataType = RuntimeDataType.LayerMaskArray;
                }
                else if (type == typeof(Vector2[]))
                {
                    vector2ValueArray = (Vector2[])value;
                    dataType = RuntimeDataType.Vector2Array;
                }
                else if (type == typeof(Vector3[]))
                {
                    vector3ValueArray = (Vector3[])value;
                    dataType = RuntimeDataType.Vector3Array;
                }
                else if (type == typeof(Vector4[]))
                {
                    vector4ValueArray = (Vector4[])value;
                    dataType = RuntimeDataType.Vector4Array;
                }
                else if (type == typeof(Quaternion[]))
                {
                    quaternionValueArray = (Quaternion[])value;
                    dataType = RuntimeDataType.QuaternionArray;
                }
                else if (type == typeof(Vector2Int[]))
                {
                    vector2IntValueArray = (Vector2Int[])value;
                    dataType = RuntimeDataType.Vector2IntArray;
                }
                else if (type == typeof(Vector3Int[]))
                {
                    vector3IntValueArray = (Vector3Int[])value;
                    dataType = RuntimeDataType.Vector3IntArray;
                }
                else if (type == typeof(Rect[]))
                {
                    rectValueArray = (Rect[])value;
                    dataType = RuntimeDataType.RectArray;
                }
                else if (type == typeof(Bounds[]))
                {
                    boundsValueArray = (Bounds[])value;
                    dataType = RuntimeDataType.BoundsArray;
                }
                else if (type == typeof(RectInt[]))
                {
                    rectIntValueArray = (RectInt[])value;
                    dataType = RuntimeDataType.RectIntArray;
                }
                else if (type == typeof(BoundsInt[]))
                {
                    boundsIntValueArray = (BoundsInt[])value;
                    dataType = RuntimeDataType.BoundsIntArray;
                }
                else
                {
                    return false;
                }
                return true;
            }

            public bool SetValueSafe(object value)
            {
                if (value == null) return false;
                Type valueType = value.GetType();
                switch (dataType)
                {
                    case RuntimeDataType.UnityObject:
                        if (!typeof(Object).IsAssignableFrom(valueType)) break;
                        unityObject = value as Object;
                        return true;
                    case RuntimeDataType.String:
                        if (valueType != typeof(string)) break;
                        stringValue = (string)value;
                        return true;
                    case RuntimeDataType.Int:
                        if (valueType != typeof(int)) break;
                        intValue = (int)value;
                        return true;
                    case RuntimeDataType.Float:
                        if (valueType != typeof(float)) break;
                        floatValue = (float)value;
                        return true;
                    case RuntimeDataType.Bool:
                        if (valueType != typeof(bool)) break;
                        boolValue = (bool)value;
                        return true;
                    case RuntimeDataType.Color:
                        if (valueType != typeof(Color)) break;
                        colorValue = (Color)value;
                        return true;
                    case RuntimeDataType.LayerMask:
                        if (valueType != typeof(LayerMask)) break;
                        layerMaskValue = (LayerMask)value;
                        return true;
                    case RuntimeDataType.Vector2:
                        if (valueType != typeof(Vector2)) break;
                        vector2Value = (Vector2)value;
                        return true;
                    case RuntimeDataType.Vector3:
                        if (valueType != typeof(Vector3)) break;
                        vector3Value = (Vector3)value;
                        return true;
                    case RuntimeDataType.Vector4:
                        if (valueType != typeof(Vector4)) break;
                        vector4Value = (Vector4)value;
                        return true;
                    case RuntimeDataType.Quaternion:
                        if (valueType != typeof(Quaternion)) break;
                        quaternionValue = (Quaternion)value;
                        return true;
                    case RuntimeDataType.Vector2Int:
                        if (valueType != typeof(Vector2Int)) break;
                        vector2IntValue = (Vector2Int)value;
                        return true;
                    case RuntimeDataType.Vector3Int:
                        if (valueType != typeof(Vector3Int)) break;
                        vector3IntValue = (Vector3Int)value;
                        return true;
                    case RuntimeDataType.Rect:
                        if (valueType != typeof(Rect)) break;
                        rectValue = (Rect)value;
                        return true;
                    case RuntimeDataType.Bounds:
                        if (valueType != typeof(Bounds)) break;
                        boundsValue = (Bounds)value;
                        return true;
                    case RuntimeDataType.RectInt:
                        if (valueType != typeof(RectInt)) break;
                        rectIntValue = (RectInt)value;
                        return true;
                    case RuntimeDataType.BoundsInt:
                        if (valueType != typeof(BoundsInt)) break;
                        boundsIntValue = (BoundsInt)value;
                        return true;
                    case RuntimeDataType.UnityObjectArray:
                        if (!typeof(Object[]).IsAssignableFrom(valueType)) break;
                        unityObjectArray = value as Object[];
                        return true;
                    case RuntimeDataType.StringArray:
                        if (valueType != typeof(string[])) break;
                        stringValueArray = (string[])value;
                        return true;
                    case RuntimeDataType.IntArray:
                        if (valueType != typeof(int[])) break;
                        intValueArray = (int[])value;
                        return true;
                    case RuntimeDataType.FloatArray:
                        if (valueType != typeof(float[])) break;
                        floatValueArray = (float[])value;
                        return true;
                    case RuntimeDataType.BoolArray:
                        if (valueType != typeof(bool[])) break;
                        boolValueArray = (bool[])value;
                        return true;
                    case RuntimeDataType.ColorArray:
                        if (valueType != typeof(Color[])) break;
                        colorValueArray = (Color[])value;
                        return true;
                    case RuntimeDataType.LayerMaskArray:
                        if (valueType != typeof(LayerMask[])) break;
                        layerMaskValueArray = (LayerMask[])value;
                        return true;
                    case RuntimeDataType.Vector2Array:
                        if (valueType != typeof(Vector2[])) break;
                        vector2ValueArray = (Vector2[])value;
                        return true;
                    case RuntimeDataType.Vector3Array:
                        if (valueType != typeof(Vector3[])) break;
                        vector3ValueArray = (Vector3[])value;
                        return true;
                    case RuntimeDataType.Vector4Array:
                        if (valueType != typeof(Vector4[])) break;
                        vector4ValueArray = (Vector4[])value;
                        return true;
                    case RuntimeDataType.QuaternionArray:
                        if (valueType != typeof(Quaternion[])) break;
                        quaternionValueArray = (Quaternion[])value;
                        return true;
                    case RuntimeDataType.Vector2IntArray:
                        if (valueType != typeof(Vector2Int[])) break;
                        vector2IntValueArray = (Vector2Int[])value;
                        return true;
                    case RuntimeDataType.Vector3IntArray:
                        if (valueType != typeof(Vector3Int[])) break;
                        vector3IntValueArray = (Vector3Int[])value;
                        return true;
                    case RuntimeDataType.RectArray:
                        if (valueType != typeof(Rect[])) break;
                        rectValueArray = (Rect[])value;
                        return true;
                    case RuntimeDataType.BoundsArray:
                        if (valueType != typeof(Bounds[])) break;
                        boundsValueArray = (Bounds[])value;
                        return true;
                    case RuntimeDataType.RectIntArray:
                        if (valueType != typeof(RectInt[])) break;
                        rectIntValueArray = (RectInt[])value;
                        return true;
                    case RuntimeDataType.BoundsIntArray:
                        if (valueType != typeof(BoundsInt[])) break;
                        boundsIntValueArray = (BoundsInt[])value;
                        return true;
                }
                
                return false;
            }

            #endregion Non-Generics

#if SCRIPTING_ENABLED
            public DynValue WrapValueForLua(CVRLuaContext context, object ourVal) =>
                dataType switch
                {
                    RuntimeDataType.Bool => DynValue.NewBoolean((bool)ourVal),
                    RuntimeDataType.Int => DynValue.NewNumber((int)ourVal),
                    RuntimeDataType.Float => DynValue.NewNumber((float)ourVal),
                    RuntimeDataType.String => DynValue.NewString((string)ourVal),
                    _ => ourVal != null ? LuaAPITranslator.Wrap(context, (Object)ourVal) : DynValue.Nil
                };
#endif
        }
        
        #endregion DataValue Union

#if UNITY_EDITOR
        #region Editor Only

        public enum EditorDataType : byte
        {
            UnityObject,
            String,
            Int,
            Float,
            Bool,
            Vector2,
            Vector2Int,
            Vector3,
            Vector3Int,
            Vector4,
            Quaternion,
            Rect,
            RectInt,
            Bounds,
            BoundsInt,
            Color,
            LayerMask,
        }
        
        [Serializable]
        public class KeyValueData
        {
            public string key;
            public EditorDataType type;
            public bool isArray;

            public Object[] unityObjectArray;
            public string[] stringArray;
            public int[] intArray;
            public float[] floatArray;
            public bool[] boolArray;
            public Color[] colorArray;
            public LayerMask[] layerMaskArray;
            public Vector2[] vector2Array;
            public Vector3[] vector3Array;
            public Rect[] rectArray;
            public Bounds[] boundsArray;
            public Quaternion[] quaternionArray;
            public Vector2Int[] vector2IntArray;
            public Vector3Int[] vector3IntArray;
            public RectInt[] rectIntArray;
            public BoundsInt[] boundsIntArray;
            public Vector4[] vector4Array;
        }
        
        [SerializeField] private List<KeyValueData> editorData = new();
        
        private void OnValidate()
        {
            List<Object> objectList = new List<Object>();
            List<byte> dataList = new List<byte>();
            
            SerializeEditorData(editorData, objectList, dataList);
            
            serializedUnityObjects = objectList.ToArray();
            serializedBytes = dataList.ToArray();
        }
        
        #endregion Editor Only
        
        #region Serialization
        
        private static void SerializeEditorData(List<KeyValueData> dataList, List<Object> objectList, List<byte> data)
        {
            data.AddRange(BitConverter.GetBytes(dataList.Count));
            
            foreach (KeyValueData kv in dataList)
            {
                // Serialize key
                SerializeString(data, kv.key ?? string.Empty);
                
                // Serialize type
                data.Add((byte)(kv.type + (byte)(kv.isArray ? DataTypeCount : 0)));
                
                // Serialize value
                SerializeValue(objectList, data, kv);
            }
        }

        private static void SerializeValue(List<Object> references, List<byte> data, KeyValueData kv)
        {
            if (kv.isArray)
            {
                int length = GetArrayLength(kv);
                data.AddRange(BitConverter.GetBytes(length));
                SerializeArrayValues(references, data, kv, length);
            }
            else
            {
                SerializeSingleValueDirect(references, data, kv);
            }
        }

        private static int GetArrayLength(KeyValueData kv)
        {
            return kv.type switch
            {
                EditorDataType.UnityObject => kv.unityObjectArray?.Length ?? 0,
                EditorDataType.String => kv.stringArray?.Length ?? 0,
                EditorDataType.Int => kv.intArray?.Length ?? 0,
                EditorDataType.Float => kv.floatArray?.Length ?? 0,
                EditorDataType.Bool => kv.boolArray?.Length ?? 0,
                EditorDataType.Color => kv.colorArray?.Length ?? 0,
                EditorDataType.LayerMask => kv.layerMaskArray?.Length ?? 0,
                EditorDataType.Vector2 => kv.vector2Array?.Length ?? 0,
                EditorDataType.Vector3 => kv.vector3Array?.Length ?? 0,
                EditorDataType.Rect => kv.rectArray?.Length ?? 0,
                EditorDataType.Bounds => kv.boundsArray?.Length ?? 0,
                EditorDataType.Quaternion => kv.quaternionArray?.Length ?? 0,
                EditorDataType.Vector2Int => kv.vector2IntArray?.Length ?? 0,
                EditorDataType.Vector3Int => kv.vector3IntArray?.Length ?? 0,
                EditorDataType.RectInt => kv.rectIntArray?.Length ?? 0,
                EditorDataType.BoundsInt => kv.boundsIntArray?.Length ?? 0,
                EditorDataType.Vector4 => kv.vector4Array?.Length ?? 0,
                _ => 0
            };
        }

        private static void SerializeArrayValues(List<Object> references, List<byte> data, KeyValueData kv, int length)
        {
            switch (kv.type)
            {
                case EditorDataType.UnityObject:
                    for (int i = 0; i < length; i++) references.Add(kv.unityObjectArray[i]); 
                    break;
                case EditorDataType.String:
                    for (int i = 0; i < length; i++) SerializeString(data, kv.stringArray[i] ?? string.Empty); 
                    break;
                case EditorDataType.Int:
                    for (int i = 0; i < length; i++) data.AddRange(BitConverter.GetBytes(kv.intArray[i]));
                    break;
                case EditorDataType.Float:
                    for (int i = 0; i < length; i++) data.AddRange(BitConverter.GetBytes(kv.floatArray[i]));
                    break;
                case EditorDataType.Bool:
                    for (int i = 0; i < length; i++) data.AddRange(BitConverter.GetBytes(kv.boolArray[i]));
                    break;
                case EditorDataType.Color:
                    for (int i = 0; i < length; i++)
                    {
                        Color color = kv.colorArray[i];
                        data.AddRange(BitConverter.GetBytes(color.r));
                        data.AddRange(BitConverter.GetBytes(color.g));
                        data.AddRange(BitConverter.GetBytes(color.b));
                        data.AddRange(BitConverter.GetBytes(color.a));
                    }
                    break;
                case EditorDataType.LayerMask:
                    for (int i = 0; i < length; i++) data.AddRange(BitConverter.GetBytes(kv.layerMaskArray[i].value));
                    break;
                case EditorDataType.Vector2:
                    for (int i = 0; i < length; i++)
                    {
                        Vector2 v = kv.vector2Array[i];
                        data.AddRange(BitConverter.GetBytes(v.x));
                        data.AddRange(BitConverter.GetBytes(v.y));
                    }
                    break;
                case EditorDataType.Vector3:
                    for (int i = 0; i < length; i++)
                    {
                        Vector3 v = kv.vector3Array[i];
                        data.AddRange(BitConverter.GetBytes(v.x));
                        data.AddRange(BitConverter.GetBytes(v.y));
                        data.AddRange(BitConverter.GetBytes(v.z));
                    }
                    break;
                case EditorDataType.Rect:
                    for (int i = 0; i < length; i++)
                    {
                        Rect rect = kv.rectArray[i];
                        data.AddRange(BitConverter.GetBytes(rect.x));
                        data.AddRange(BitConverter.GetBytes(rect.y));
                        data.AddRange(BitConverter.GetBytes(rect.width));
                        data.AddRange(BitConverter.GetBytes(rect.height));
                    }
                    break;
                case EditorDataType.Bounds:
                    for (int i = 0; i < length; i++)
                    {
                        Bounds bounds = kv.boundsArray[i];
                        data.AddRange(BitConverter.GetBytes(bounds.center.x));
                        data.AddRange(BitConverter.GetBytes(bounds.center.y));
                        data.AddRange(BitConverter.GetBytes(bounds.center.z));
                        data.AddRange(BitConverter.GetBytes(bounds.size.x));
                        data.AddRange(BitConverter.GetBytes(bounds.size.y));
                        data.AddRange(BitConverter.GetBytes(bounds.size.z));
                    }
                    break;
                case EditorDataType.Quaternion:
                    for (int i = 0; i < length; i++)
                    {
                        Quaternion quat = kv.quaternionArray[i];
                        data.AddRange(BitConverter.GetBytes(quat.x));
                        data.AddRange(BitConverter.GetBytes(quat.y));
                        data.AddRange(BitConverter.GetBytes(quat.z));
                        data.AddRange(BitConverter.GetBytes(quat.w));
                    }
                    break;
                case EditorDataType.Vector2Int:
                    for (int i = 0; i < length; i++)
                    {
                        Vector2Int v = kv.vector2IntArray[i];
                        data.AddRange(BitConverter.GetBytes(v.x));
                        data.AddRange(BitConverter.GetBytes(v.y));
                    }
                    break;
                case EditorDataType.Vector3Int:
                    for (int i = 0; i < length; i++)
                    {
                        Vector3Int v = kv.vector3IntArray[i];
                        data.AddRange(BitConverter.GetBytes(v.x));
                        data.AddRange(BitConverter.GetBytes(v.y));
                        data.AddRange(BitConverter.GetBytes(v.z));
                    }
                    break;
                case EditorDataType.RectInt:
                    for (int i = 0; i < length; i++)
                    {
                        RectInt rect = kv.rectIntArray[i];
                        data.AddRange(BitConverter.GetBytes(rect.x));
                        data.AddRange(BitConverter.GetBytes(rect.y));
                        data.AddRange(BitConverter.GetBytes(rect.width));
                        data.AddRange(BitConverter.GetBytes(rect.height));
                    }
                    break;
                case EditorDataType.BoundsInt:
                    for (int i = 0; i < length; i++)
                    {
                        BoundsInt bounds = kv.boundsIntArray[i];
                        data.AddRange(BitConverter.GetBytes(bounds.position.x));
                        data.AddRange(BitConverter.GetBytes(bounds.position.y));
                        data.AddRange(BitConverter.GetBytes(bounds.position.z));
                        data.AddRange(BitConverter.GetBytes(bounds.size.x));
                        data.AddRange(BitConverter.GetBytes(bounds.size.y));
                        data.AddRange(BitConverter.GetBytes(bounds.size.z));
                    }
                    break;
                case EditorDataType.Vector4:
                    for (int i = 0; i < length; i++)
                    {
                        Vector4 v = kv.vector4Array[i];
                        data.AddRange(BitConverter.GetBytes(v.x));
                        data.AddRange(BitConverter.GetBytes(v.y));
                        data.AddRange(BitConverter.GetBytes(v.z));
                        data.AddRange(BitConverter.GetBytes(v.w));
                    }
                    break;
            }
        }

        private static void SerializeSingleValueDirect(List<Object> references, List<byte> data, KeyValueData kv)
        {
            switch (kv.type)
            {
                case EditorDataType.UnityObject: 
                    references.Add(kv.unityObjectArray?[0]);
                    break;
                case EditorDataType.String:
                    SerializeString(data, kv.stringArray?[0] ?? string.Empty);
                    break;
                case EditorDataType.Int:
                    data.AddRange(BitConverter.GetBytes(kv.intArray?[0] ?? 0));
                    break;
                case EditorDataType.Float:
                    data.AddRange(BitConverter.GetBytes(kv.floatArray?[0] ?? 0f));
                    break;
                case EditorDataType.Bool:
                    data.AddRange(BitConverter.GetBytes(kv.boolArray?[0] ?? false));
                    break;
                case EditorDataType.Color:
                    Color color = kv.colorArray?[0] ?? Color.white;
                    data.AddRange(BitConverter.GetBytes(color.r));
                    data.AddRange(BitConverter.GetBytes(color.g));
                    data.AddRange(BitConverter.GetBytes(color.b));
                    data.AddRange(BitConverter.GetBytes(color.a));
                    break;
                case EditorDataType.LayerMask:
                    data.AddRange(BitConverter.GetBytes((kv.layerMaskArray?[0] ?? new LayerMask()).value));
                    break;
                case EditorDataType.Vector2:
                    Vector2 v2 = kv.vector2Array?[0] ?? Vector2.zero;
                    data.AddRange(BitConverter.GetBytes(v2.x));
                    data.AddRange(BitConverter.GetBytes(v2.y));
                    break;
                case EditorDataType.Vector3:
                    Vector3 v3 = kv.vector3Array?[0] ?? Vector3.zero;
                    data.AddRange(BitConverter.GetBytes(v3.x));
                    data.AddRange(BitConverter.GetBytes(v3.y));
                    data.AddRange(BitConverter.GetBytes(v3.z));
                    break;
                case EditorDataType.Rect:
                    Rect rect = kv.rectArray?[0] ?? new Rect();
                    data.AddRange(BitConverter.GetBytes(rect.x));
                    data.AddRange(BitConverter.GetBytes(rect.y));
                    data.AddRange(BitConverter.GetBytes(rect.width));
                    data.AddRange(BitConverter.GetBytes(rect.height));
                    break;
                case EditorDataType.Bounds:
                    Bounds bounds = kv.boundsArray?[0] ?? new Bounds();
                    data.AddRange(BitConverter.GetBytes(bounds.center.x));
                    data.AddRange(BitConverter.GetBytes(bounds.center.y));
                    data.AddRange(BitConverter.GetBytes(bounds.center.z));
                    data.AddRange(BitConverter.GetBytes(bounds.size.x));
                    data.AddRange(BitConverter.GetBytes(bounds.size.y));
                    data.AddRange(BitConverter.GetBytes(bounds.size.z));
                    break;
                case EditorDataType.Quaternion:
                    Quaternion quat = kv.quaternionArray?[0] ?? Quaternion.identity;
                    data.AddRange(BitConverter.GetBytes(quat.x));
                    data.AddRange(BitConverter.GetBytes(quat.y));
                    data.AddRange(BitConverter.GetBytes(quat.z));
                    data.AddRange(BitConverter.GetBytes(quat.w));
                    break;
                case EditorDataType.Vector2Int:
                    Vector2Int v2i = kv.vector2IntArray?[0] ?? Vector2Int.zero;
                    data.AddRange(BitConverter.GetBytes(v2i.x));
                    data.AddRange(BitConverter.GetBytes(v2i.y));
                    break;
                case EditorDataType.Vector3Int:
                    Vector3Int v3i = kv.vector3IntArray?[0] ?? Vector3Int.zero;
                    data.AddRange(BitConverter.GetBytes(v3i.x));
                    data.AddRange(BitConverter.GetBytes(v3i.y));
                    data.AddRange(BitConverter.GetBytes(v3i.z));
                    break;
                case EditorDataType.RectInt:
                    RectInt rectInt = kv.rectIntArray?[0] ?? new RectInt();
                    data.AddRange(BitConverter.GetBytes(rectInt.x));
                    data.AddRange(BitConverter.GetBytes(rectInt.y));
                    data.AddRange(BitConverter.GetBytes(rectInt.width));
                    data.AddRange(BitConverter.GetBytes(rectInt.height));
                    break;
                case EditorDataType.BoundsInt:
                    BoundsInt boundsInt = kv.boundsIntArray?[0] ?? new BoundsInt();
                    data.AddRange(BitConverter.GetBytes(boundsInt.position.x));
                    data.AddRange(BitConverter.GetBytes(boundsInt.position.y));
                    data.AddRange(BitConverter.GetBytes(boundsInt.position.z));
                    data.AddRange(BitConverter.GetBytes(boundsInt.size.x));
                    data.AddRange(BitConverter.GetBytes(boundsInt.size.y));
                    data.AddRange(BitConverter.GetBytes(boundsInt.size.z));
                    break;
                case EditorDataType.Vector4:
                    Vector4 v4 = kv.vector4Array?[0] ?? Vector4.zero;
                    data.AddRange(BitConverter.GetBytes(v4.x));
                    data.AddRange(BitConverter.GetBytes(v4.y));
                    data.AddRange(BitConverter.GetBytes(v4.z));
                    data.AddRange(BitConverter.GetBytes(v4.w));
                    break;
            }
        }
        
        private static void SerializeString(List<byte> data, string value)
        {
            byte[] bytes = Encoding.Unicode.GetBytes(value);
            data.AddRange(BitConverter.GetBytes(bytes.Length));
            data.AddRange(bytes);
        }
        
        #endregion Serialization
#endif
    }
}