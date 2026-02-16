using Android.Content;
using Android.OS;
using AndroidX.AppCompat.App;
using System;
using Seeker.Helpers;

using Common;
namespace Seeker
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