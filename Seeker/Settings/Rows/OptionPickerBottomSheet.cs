using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.BottomSheet;
using Google.Android.Material.RadioButton;
using System;
using System.Collections.Generic;

namespace Seeker.Settings.Rows
{
    public sealed class OptionPickerBottomSheet : BottomSheetDialogFragment
    {
        public sealed class Option
        {
            public string Label { get; }
            public int IntValue { get; }
            public object ObjectValue { get; }
            public Option(string label, int value) { Label = label; IntValue = value; ObjectValue = value; }
            public Option(string label, object value) { Label = label; ObjectValue = value; IntValue = 0; }
        }

        private string _title;
        private List<Option> _options;
        private int _selectedIndex;
        private Action<Option> _onSelected;

        public static void ShowPresetInts(ISettingsHost host, ValueRow row, IList<int> values,
            Func<int> getter, Action<int> setter)
        {
            var ctx = host.Activity;
            var current = getter();
            var opts = new List<Option>(values.Count);
            int selected = -1;
            for (int i = 0; i < values.Count; i++)
            {
                opts.Add(new Option(values[i].ToString(), values[i]));
                if (values[i] == current) selected = i;
            }
            var sheet = new OptionPickerBottomSheet
            {
                _title = row.ResolveTitle(ctx),
                _options = opts,
                _selectedIndex = selected,
                _onSelected = o => {
                    setter(o.IntValue);
                    host.NotifyRowChanged(row.Id);
                }
            };
            sheet.Show(((AndroidX.AppCompat.App.AppCompatActivity)host.Activity).SupportFragmentManager, "OptionPicker");
        }

        public static void ShowOptions<T>(ISettingsHost host, ValueRow row,
            IList<(string label, T value)> options, Func<T> getter, Action<T> setter)
        {
            var ctx = host.Activity;
            var current = getter();
            var opts = new List<Option>(options.Count);
            int selected = -1;
            for (int i = 0; i < options.Count; i++)
            {
                opts.Add(new Option(options[i].label, options[i].value));
                if (EqualityComparer<T>.Default.Equals(options[i].value, current)) selected = i;
            }
            var sheet = new OptionPickerBottomSheet
            {
                _title = row.ResolveTitle(ctx),
                _options = opts,
                _selectedIndex = selected,
                _onSelected = o => {
                    setter((T)o.ObjectValue);
                    host.NotifyRowChanged(row.Id);
                }
            };
            sheet.Show(((AndroidX.AppCompat.App.AppCompatActivity)host.Activity).SupportFragmentManager, "OptionPicker");
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.bottom_sheet_option_picker, container, false);
            // Process death restore: _options and the _onSelected delegate are non-serializeable
            if (_options == null || _onSelected == null)
            {
                DismissAllowingStateLoss();
                return view;
            }
            view.FindViewById<TextView>(Resource.Id.sheetTitle).Text = _title ?? string.Empty;
            var list = view.FindViewById<RecyclerView>(Resource.Id.sheetList);
            list.SetLayoutManager(new LinearLayoutManager(Context));
            list.SetAdapter(new InternalAdapter(_options, _selectedIndex, idx =>
            {
                _onSelected?.Invoke(_options[idx]);
                Dismiss();
            }));
            return view;
        }

        private sealed class InternalAdapter : RecyclerView.Adapter
        {
            private readonly List<Option> _items;
            private int _selected;
            private readonly Action<int> _onClick;

            public InternalAdapter(List<Option> items, int selected, Action<int> onClick)
            {
                _items = items; _selected = selected; _onClick = onClick;
            }
            public override int ItemCount => _items.Count;
            public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
            {
                var v = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.bottom_sheet_option_picker_item, parent, false);
                return new VH(v);
            }
            public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
            {
                var vh = (VH)holder;
                vh.Label.Text = _items[position].Label;
                vh.Radio.Checked = position == _selected;
                vh.ItemView.Click -= vh.Handler;
                vh.Handler = (s, e) =>
                {
                    int oldSel = _selected;
                    _selected = vh.BindingAdapterPosition;
                    if (oldSel >= 0) NotifyItemChanged(oldSel);
                    NotifyItemChanged(_selected);
                    _onClick(_selected);
                };
                vh.ItemView.Click += vh.Handler;
            }
            private sealed class VH : RecyclerView.ViewHolder
            {
                public MaterialRadioButton Radio;
                public TextView Label;
                public EventHandler Handler;
                public VH(View v) : base(v)
                {
                    Radio = v.FindViewById<MaterialRadioButton>(Resource.Id.optionRadio);
                    Label = v.FindViewById<TextView>(Resource.Id.optionLabel);
                }
            }
        }
    }
}
