using System.Reflection;
using InventorySystem.Items.Autosync;
using PlayerRoles.Subroutines;

namespace Capy.Core.Extensions;

public static class DummyExtensions {
    public static bool TryRunItemAction<T>(T item, ActionName actionName, bool isClick) where T : AutosyncItem {
        if (item == null)
            return false;
        
        try {
            PropertyInfo? isDummyProp = item.GetType().GetProperty("IsEmulatedDummy", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (isDummyProp != null && !(bool)isDummyProp.GetValue(item))
                return false;

            object? dummyEmulator = item.GetType().GetProperty("DummyEmulator", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(item)
                ?? item.GetType().GetField("DummyEmulator", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(item);
                
            if (dummyEmulator == null)
                return false;

            MethodInfo? addEntry = dummyEmulator.GetType().GetMethod("AddEntry", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            addEntry?.Invoke(dummyEmulator, new object[] { actionName, isClick });
            return true;
        }
        catch {
            return false;
        }
    }
    
    public static bool TryStopItemAction<T>(T item, ActionName actionName) where T : AutosyncItem {
        if (item == null)
            return false;
            
        try {
            PropertyInfo? isDummyProp = item.GetType().GetProperty("IsEmulatedDummy", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (isDummyProp != null && !(bool)isDummyProp.GetValue(item))
                return false;

            object? dummyEmulator = item.GetType().GetProperty("DummyEmulator", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(item)
                ?? item.GetType().GetField("DummyEmulator", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(item);
                
            if (dummyEmulator == null)
                return false;

            MethodInfo? removeEntry = dummyEmulator.GetType().GetMethod("RemoveEntry", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            removeEntry?.Invoke(dummyEmulator, new object[] { actionName });
            return true;
        }
        catch {
            return false;
        }
    }
    
    public static bool TryRunRoleAction<T>(T subroutine, ActionName actionName, bool isClick) where T : SubroutineBase {
        if (subroutine == null)
            return false;
            
        try {
            object? role = subroutine.GetType().GetProperty("Role", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(subroutine);
            if (role == null) return false;

            PropertyInfo? isDummyProp = role.GetType().GetProperty("IsEmulatedDummy", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (isDummyProp != null && !(bool)isDummyProp.GetValue(role))
                return false;

            object? dummyEmulator = subroutine.GetType().GetProperty("DummyEmulator", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(subroutine)
                ?? subroutine.GetType().GetField("DummyEmulator", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(subroutine);
                
            if (dummyEmulator == null)
                return false;

            MethodInfo? addEntry = dummyEmulator.GetType().GetMethod("AddEntry", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            addEntry?.Invoke(dummyEmulator, new object[] { actionName, isClick });
            return true;
        }
        catch {
            return false;
        }
    }
    
    public static bool TryStopRoleAction<T>(T subroutine, ActionName actionName) where T : SubroutineBase {
        if (subroutine == null)
            return false;
            
        try {
            object? role = subroutine.GetType().GetProperty("Role", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(subroutine);
            if (role == null) return false;

            PropertyInfo? isDummyProp = role.GetType().GetProperty("IsEmulatedDummy", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (isDummyProp != null && !(bool)isDummyProp.GetValue(role))
                return false;

            object? dummyEmulator = subroutine.GetType().GetProperty("DummyEmulator", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(subroutine)
                ?? subroutine.GetType().GetField("DummyEmulator", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(subroutine);
                
            if (dummyEmulator == null)
                return false;

            MethodInfo? removeEntry = dummyEmulator.GetType().GetMethod("RemoveEntry", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            removeEntry?.Invoke(dummyEmulator, new object[] { actionName });
            return true;
        }
        catch {
            return false;
        }
    }
}
