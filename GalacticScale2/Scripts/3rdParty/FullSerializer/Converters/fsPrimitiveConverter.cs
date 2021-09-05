﻿using System;

namespace GSSerializer.Internal
{
    public class fsPrimitiveConverter : fsConverter
    {
        public override bool CanProcess(Type type)
        {
            return type.Resolve().IsPrimitive || type == typeof(string) || type == typeof(decimal);
        }

        public override bool RequestCycleSupport(Type storageType)
        {
            return false;
        }

        public override bool RequestInheritanceSupport(Type storageType)
        {
            return false;
        }

        private static bool UseBool(Type type)
        {
            return type == typeof(bool);
        }

        private static bool UseInt64(Type type)
        {
            return type == typeof(sbyte) || type == typeof(byte) || type == typeof(short) || type == typeof(ushort) || type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong);
        }

        private static bool UseDouble(Type type)
        {
            return type == typeof(float) || type == typeof(double) || type == typeof(decimal);
        }

        private static bool UseString(Type type)
        {
            return type == typeof(string) || type == typeof(char);
        }

        public override fsResult TrySerialize(object instance, out fsData serialized, Type storageType)
        {
            var instanceType = instance.GetType();

            if (Serializer.Config.Serialize64BitIntegerAsString && (instanceType == typeof(long) || instanceType == typeof(ulong)))
            {
                serialized = new fsData((string)Convert.ChangeType(instance, typeof(string)));
                return fsResult.Success;
            }

            if (UseBool(instanceType))
            {
                serialized = new fsData((bool)instance);
                return fsResult.Success;
            }

            if (UseInt64(instanceType))
            {
                serialized = new fsData((long)Convert.ChangeType(instance, typeof(long)));
                return fsResult.Success;
            }

            if (UseDouble(instanceType))
            {
                // Casting from float to double introduces floating point jitter,
                // ie, 0.1 becomes 0.100000001490116. Casting to decimal as an
                // intermediate step removes the jitter. Not sure why.
                if (instance.GetType() == typeof(float) &&
                    // Decimal can't store
                    // float.MinValue/float.MaxValue/float.PositiveInfinity/float.NegativeInfinity/float.NaN
                    // - an exception gets thrown in that scenario.
                    (float)instance != float.MinValue && (float)instance != float.MaxValue && !float.IsInfinity((float)instance) && !float.IsNaN((float)instance))
                {
                    serialized = new fsData((double)(decimal)(float)instance);
                    return fsResult.Success;
                }

                serialized = new fsData((double)Convert.ChangeType(instance, typeof(double)));
                return fsResult.Success;
            }

            if (UseString(instanceType))
            {
                serialized = new fsData((string)Convert.ChangeType(instance, typeof(string)));
                return fsResult.Success;
            }

            serialized = null;
            return fsResult.Fail("Unhandled primitive type " + instance.GetType());
        }

        public override fsResult TryDeserialize(fsData storage, ref object instance, Type storageType)
        {
            var result = fsResult.Success;

            if (UseBool(storageType))
            {
                if ((result += CheckType(storage, fsDataType.Boolean)).Succeeded) instance = storage.AsBool;
                return result;
            }

            if (UseDouble(storageType) || UseInt64(storageType))
            {
                if (storage.IsDouble)
                    instance = Convert.ChangeType(storage.AsDouble, storageType);
                else if (storage.IsInt64)
                    instance = Convert.ChangeType(storage.AsInt64, storageType);
                else if (storage.IsString && (Serializer.Config.Serialize64BitIntegerAsString && (storageType == typeof(long) || storageType == typeof(ulong)) || Serializer.Config.CoerceStringsToNumbers))
                    instance = Convert.ChangeType(storage.AsString, storageType);
                else
                    return fsResult.Fail(GetType().Name + " expected number but got " + storage.Type + " in " + storage);
                return fsResult.Success;
            }

            if (UseString(storageType))
            {
                if ((result += CheckType(storage, fsDataType.String)).Succeeded) instance = storage.AsString;
                return result;
            }

            return fsResult.Fail(GetType().Name + ": Bad data; expected bool, number, string, but got " + storage);
        }
    }
}