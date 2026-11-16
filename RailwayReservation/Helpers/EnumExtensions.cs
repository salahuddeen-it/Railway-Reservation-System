using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace RailwayReservation.Helpers
{
    /// <summary>Shows the [Display(Name=...)] text of an enum, e.g. AC3 -> "AC 3 Tier (3A)".</summary>
    public static class EnumExtensions
    {
        public static string GetDisplayName(this Enum value)
        {
            if (value == null) return string.Empty;
            MemberInfo member = value.GetType().GetMember(value.ToString()).FirstOrDefault();
            if (member == null) return value.ToString();
            var attr = member.GetCustomAttribute<DisplayAttribute>();
            return attr != null ? attr.GetName() : value.ToString();
        }
    }
}