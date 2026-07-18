using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatFileList.Extensions
{
    /// <summary>
    /// 任意のオブジェクトを指定文字で囲んだ文字列に変換する拡張メソッドを提供する。
    /// </summary>
    public static class ObjectExtensions
    {
        /// <summary>
        /// 値の文字列表現を指定した文字列で前後から囲む。値が <see langword="null"/> の場合は空文字として扱う。
        /// </summary>
        /// <param name="val">対象の値。</param>
        /// <param name="wrapText">前後に付与する文字列。</param>
        /// <returns>囲んだ文字列。</returns>
        public static string ToWrappedString(this object val, string wrapText) => $"{wrapText}{val ?? string.Empty}{wrapText}";

        /// <summary>
        /// 値の文字列表現を二重引用符で囲む。
        /// </summary>
        /// <param name="val">対象の値。</param>
        /// <returns>二重引用符で囲んだ文字列。</returns>
        public static string ToWrappedStringInDoubleQuotes(this object val) => val.ToWrappedString(@"""");

        /// <summary>
        /// 値の文字列表現を単一引用符で囲む。
        /// </summary>
        /// <param name="val">対象の値。</param>
        /// <returns>単一引用符で囲んだ文字列。</returns>
        public static string ToWrappedStringInSingleQuotes(this object val) => val.ToWrappedString(@"'");
    }
}
