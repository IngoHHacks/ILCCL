namespace ILCCL.Utils;

public static class ReflectionUtils
{
    public static T GetFieldValue<T>(Type type, string fieldName, object instance)
    {
        return (T) AccessTools.Field(type, fieldName).GetValue(instance);
    }
    
    public static void SetFieldValue<T>(Type type, string fieldName, object instance, T value)
    {
        AccessTools.Field(type, fieldName).SetValue(instance, value);
    }
}