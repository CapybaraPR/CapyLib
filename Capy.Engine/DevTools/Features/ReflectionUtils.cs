using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Capy.Engine.DevTools.Features;

public static class ReflectionUtils {
    public static T GetFieldValue<T>(object obj, string fieldName) {
        FieldInfo field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        if (field == null)
            throw new Exception($"Field '{fieldName}' not found in type '{obj.GetType().FullName}'");

        return (T)field.GetValue(obj);
    }

    public static void SetFieldValue(object obj, string fieldName, object value) {
        FieldInfo field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        if (field == null)
            throw new Exception($"Field '{fieldName}' not found in type '{obj.GetType().FullName}'");

        field.SetValue(obj, value);
    }

    public static T GetPropertyValue<T>(object obj, string propertyName) {
        PropertyInfo property = obj.GetType().GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        if (property == null)
            throw new Exception($"Property '{propertyName}' not found in type '{obj.GetType().FullName}'");

        return (T)property.GetValue(obj);
    }

    public static void SetPropertyValue(object obj, string propertyName, object value) {
        PropertyInfo property = obj.GetType().GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        if (property == null)
            throw new Exception($"Property '{propertyName}' not found in type '{obj.GetType().FullName}'");

        property.SetValue(obj, value);
    }

    public static object InvokeMethod(object obj, string methodName, params object[] parameters) {
        MethodInfo method = obj.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        if (method == null)
            throw new Exception($"Method '{methodName}' not found in type '{obj.GetType().FullName}'");

        return method.Invoke(obj, parameters);
    }

    public static T InvokeMethod<T>(object obj, string methodName, params object[] parameters) {
        object result = InvokeMethod(obj, methodName, parameters);
        return (T)result;
    }

    public static T CreateInstance<T>(Type type, params object[] constructorArgs) {
        if (type == null)
            throw new Exception($"Type not found");

        return (T)Activator.CreateInstance(type, constructorArgs);
    }

    public static object CreateInstance(Type type, params object[] constructorArgs) {
        if (type == null)
            throw new Exception($"Type not found");

        return Activator.CreateInstance(type, constructorArgs);
    }

    public static List<TBase> FindTypes<TBase>(Assembly assembly) {
        Type baseType = typeof(TBase);
        List<TBase> types = [];

        foreach (Type? type in assembly.GetTypes()) {
            if (baseType.IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface) {
                TBase instance = (TBase)Activator.CreateInstance(type);
                types.Add(instance);
            }
        }

        return types;
    }

    public static MethodBuilder CreateProperty(TypeBuilder typeBuilder, string propertyName, Type propertyType, bool set, bool final = false, bool newslot = false) {
        FieldBuilder fieldBuilder = typeBuilder.DefineField($"<{propertyName}>k__BackingField", propertyType, FieldAttributes.Private);

        PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);

        MethodAttributes attributes = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;

        if (final)
            attributes |= MethodAttributes.Final;

        if (newslot)
            attributes |= MethodAttributes.NewSlot;

        MethodBuilder getMethodBuilder = typeBuilder.DefineMethod($"get_{propertyName}", attributes, propertyType, Type.EmptyTypes);
        ILGenerator getIL = getMethodBuilder.GetILGenerator();
        getIL.Emit(OpCodes.Ldarg_0);
        getIL.Emit(OpCodes.Ldfld, fieldBuilder);
        getIL.Emit(OpCodes.Ret);
        propertyBuilder.SetGetMethod(getMethodBuilder);

        if (set) {
            MethodBuilder setMethodBuilder = typeBuilder.DefineMethod($"set_{propertyName}", attributes, null, [propertyType]);
            ILGenerator setIL = setMethodBuilder.GetILGenerator();
            setIL.Emit(OpCodes.Ldarg_0);
            setIL.Emit(OpCodes.Ldarg_1);
            setIL.Emit(OpCodes.Stfld, fieldBuilder);
            setIL.Emit(OpCodes.Ret);
            propertyBuilder.SetSetMethod(setMethodBuilder);
        }

        return getMethodBuilder;
    }
}