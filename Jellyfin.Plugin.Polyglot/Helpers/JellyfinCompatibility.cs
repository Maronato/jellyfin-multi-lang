using System.Reflection;
using MediaBrowser.Model.Tasks;

namespace Jellyfin.Plugin.Polyglot.Helpers;

/// <summary>
/// Helper methods for maintaining compatibility across different Jellyfin versions.
/// </summary>
public static class JellyfinCompatibility
{
    /// <summary>
    /// Creates a daily TaskTriggerInfo compatible with 10.10.x (string) and 10.11+ (enum).
    /// </summary>
    public static TaskTriggerInfo CreateDailyTrigger(long timeOfDayTicks)
    {
        var trigger = new TaskTriggerInfo { TimeOfDayTicks = timeOfDayTicks };
        SetTriggerType(trigger, "DailyTrigger");
        return trigger;
    }

    /// <summary>
    /// Creates an interval TaskTriggerInfo compatible with 10.10.x (string) and 10.11+ (enum).
    /// </summary>
    public static TaskTriggerInfo CreateIntervalTrigger(long intervalTicks)
    {
        var trigger = new TaskTriggerInfo { IntervalTicks = intervalTicks };
        SetTriggerType(trigger, "IntervalTrigger");
        return trigger;
    }

    /// <summary>
    /// Copies a property using reflection, trying multiple names for renamed properties.
    /// </summary>
    public static bool TryCopyProperty<T>(T source, T target, params string[] propertyNames)
        where T : class
    {
        if (source == null || target == null || propertyNames.Length == 0)
        {
            return false;
        }

        var type = typeof(T);
        foreach (var propName in propertyNames)
        {
            var prop = type.GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
            if (prop?.CanRead == true && prop.CanWrite)
            {
                try
                {
                    prop.SetValue(target, prop.GetValue(source));
                    return true;
                }
                catch
                {
                    continue;
                }
            }
        }

        return false;
    }

    private static void SetTriggerType(TaskTriggerInfo trigger, string triggerTypeName)
    {
        var typeProperty = typeof(TaskTriggerInfo).GetProperty("Type", BindingFlags.Public | BindingFlags.Instance);
        if (typeProperty == null)
        {
            return;
        }

        var propertyType = typeProperty.PropertyType;
        if (propertyType == typeof(string))
        {
            typeProperty.SetValue(trigger, triggerTypeName);
        }
        else if (propertyType.IsEnum && Enum.TryParse(propertyType, triggerTypeName, out var enumValue))
        {
            typeProperty.SetValue(trigger, enumValue);
        }
    }
}

