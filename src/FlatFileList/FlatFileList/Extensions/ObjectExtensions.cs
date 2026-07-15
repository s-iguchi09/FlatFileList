using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatFileList.Extensions
{
    public static class ObjectExtensions
    {
        public static string ToWrappedString(this object val, string wrapText) => $"{wrapText}{val ?? string.Empty}{wrapText}";

        public static string ToWrappedStringInDoubleQuotes(this object val) => val.ToWrappedString(@"""");
        public static string ToWrappedStringInSingleQuotes(this object val) => val.ToWrappedString(@"'");
    }
}
