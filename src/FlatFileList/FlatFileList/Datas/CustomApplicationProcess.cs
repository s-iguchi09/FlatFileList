using FlatFileList.Extensions;
using Reactive.Bindings;

namespace FlatFileList.Datas
{
    public class CustomApplicationProcess
    {
        public ReactivePropertySlim<string> Extension { get; } = new(string.Empty);
        public ReactivePropertySlim<string> Application { get; } = new(string.Empty);
        public ReactivePropertySlim<string> Args { get; } = new(string.Empty);
        public ReactivePropertySlim<bool> IsEnabled { get; } = new(true);

        public CustomApplicationProcess()
            : this(true, string.Empty, string.Empty, string.Empty)
        {

        }

        public CustomApplicationProcess(bool isEnabled, string extension, string application, string args)
        {
            Extension.Value = extension;
            Application.Value = application;
            Args.Value = args;
            IsEnabled.Value = isEnabled;
        }

        public string ToSettingParameter()
        {
            return $"{IsEnabled.Value},{Extension.Value},{Application.Value},{Args.Value}";
        }

        public static CustomApplicationProcess CreateFromSetting(string setting)
        {
            var items = setting?.Split(',').AsEnumerable() ?? Enumerable.Empty<string>();
            return new(items.FirstOrDefault(string.Empty).ToBoolean(false), items.Skip(1).FirstOrDefault(string.Empty), items.Skip(2).FirstOrDefault(string.Empty), items.Skip(3).FirstOrDefault(string.Empty));
        }
    }
}
