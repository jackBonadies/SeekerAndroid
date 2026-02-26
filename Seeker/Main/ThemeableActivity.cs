using Android.Content;
using Android.Hardware.Lights;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.Core.View;
using Common;
using Seeker.Helpers;
using System;
namespace Seeker
{
    public class ThemeableActivity : AppCompatActivity
    {
        private WeakReference<ThemeableActivity> ourWeakRef;
        protected override void OnDestroy()
        {
            base.OnDestroy();
            SeekerApplication.Activities.Remove(ourWeakRef);
            if (SeekerApplication.Activities.Count == 0)
            {
                Logger.Debug("----- On Destory ------ Last Activity ------");
                TransfersFragment.SaveTransferItems(SeekerState.SharedPreferences, true);
            }
            else
            {
                Logger.Debug("----- On Destory ------ NOT Last Activity ------");
                TransfersFragment.SaveTransferItems(SeekerState.SharedPreferences, false, 0);
            }
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            SeekerApplication.SetActivityTheme(this);
            ourWeakRef = new WeakReference<ThemeableActivity>(this, false);

            SeekerApplication.Activities.Add(ourWeakRef);
            
            // the equivalent to calling edgeToEdge = true so api < 35 behaves the same
            WindowCompat.SetDecorFitsSystemWindows(Window!, false);
            Window!.SetStatusBarColor(Android.Graphics.Color.Transparent);
            Window!.SetNavigationBarColor(Android.Graphics.Color.Transparent);
            base.OnCreate(savedInstanceState);
        }

        protected override void AttachBaseContext(Context @base)
        {
            if (!SeekerApplication.HasProperPerAppLanguageSupport() && PreferencesState.Language != PreferencesState.FieldLangAuto)
            {
                var config = new Android.Content.Res.Configuration();
                config.Locale = SeekerApplication.LocaleFromString(PreferencesState.Language);
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