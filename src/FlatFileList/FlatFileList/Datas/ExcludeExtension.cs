using FlatFileList.Extensions;
using Reactive.Bindings;

namespace FlatFileList.Datas
{
    public class ExcludeExtension(string extensionText,bool enabled = true)
    {
        public ReactivePropertySlim<bool> Enabled { get; } = new(enabled);
        public string ExtensionText { get; } = extensionText;
        public ReactiveCommand RemoveCommand { get; } = new();

        public ExcludeExtension()
            : this(string.Empty)
        {
        }

        public string ToSettingParameter()
        {
            return $"{Enabled.Value},{ExtensionText}";
        }

        public static ExcludeExtension CreateFromSetting(string setting)
        {
            var items = setting?.Split(',').AsEnumerable() ?? Enumerable.Empty<string>();
            return new(items.Skip(1).FirstOrDefault(string.Empty), items.FirstOrDefault(string.Empty).ToBoolean(false));
        }
    }
}
