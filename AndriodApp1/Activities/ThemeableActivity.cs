using AndriodApp1.Extensions.SearchResponseExtensions;
using AndriodApp1.Helpers;
using Android;
using Android.Animation;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Net;
using Android.Net.Wifi;
using Android.OS;
using Android.Provider;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Android.Support.V4.Provider;
using Android.Support.V4.View;
using Android.Support.V7.App;
using Android.Util;
using Android.Views;
using Android.Widget;
using Common;
using Java.IO;
using SlskHelp;
using Soulseek;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using static Android.Provider.DocumentsContract;
using log = Android.Util.Log;

namespace AndriodApp1
{
    //TODOORG seperate class
    public class ThemeableActivity : AppCompatActivity
    {
        private WeakReference<ThemeableActivity> ourWeakRef;
        protected override void OnDestroy()
        {
            base.OnDestroy();
            SeekerApplication.Activities.Remove(ourWeakRef);
            if (SeekerApplication.Activities.Count == 0)
            {
                MainActivity.LogDebug("----- On Destory ------ Last Activity ------");
                TransfersFragment.SaveTransferItems(SoulSeekState.SharedPreferences, true);
            }
            else
            {
                MainActivity.LogDebug("----- On Destory ------ NOT Last Activity ------");
                TransfersFragment.SaveTransferItems(SoulSeekState.SharedPreferences, false, 0);
            }
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            SeekerApplication.SetActivityTheme(this);
            ourWeakRef = new WeakReference<ThemeableActivity>(this, false);

            SeekerApplication.Activities.Add(ourWeakRef);
            base.OnCreate(savedInstanceState);
        }

        protected override void AttachBaseContext(Context @base)
        {
            if (!SeekerApplication.HasProperPerAppLanguageSupport() && SoulSeekState.Language != SoulSeekState.FieldLangAuto)
            {
                var config = new Android.Content.Res.Configuration();
                config.Locale = SeekerApplication.LocaleFromString(SoulSeekState.Language);
                var baseContext = @base.CreateConfigurationContext(config);
                base.AttachBaseContext(baseContext);
            }
            else
            {
                base.AttachBaseContext(@base);
            }

        }

    }
}