using Android.Animation;
using Android.OS;
using Android.Views;
using AndroidX.ViewPager.Widget;
using Google.Android.Material.BottomNavigation;
using Seeker.Helpers;

namespace Seeker
{
    public partial class MainActivity
    {
        private class ListenerKeyboard : Java.Lang.Object, ViewTreeObserver.IOnGlobalLayoutListener
        {   //oh so it just needs the Java.Lang.Object and then you can make it like a Java Anon Class where you only implement that one thing that you truly need
            //Since C# doesn't support anonymous classes

            private bool alreadyOpen;
            private const int defaultKeyboardHeightDP = 100;
            private int EstimatedKeyboardDP = defaultKeyboardHeightDP + (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop ? 48 : 0); //api 21
            private View parentView;
            private Android.Graphics.Rect rect = new Android.Graphics.Rect();
            public ListenerKeyboard(View _parentView)
            {
                parentView = _parentView;
            }

            public void OnGlobalLayout() //this is technically overridden and it will be called, its just weird due to the java IJavaObject, IDisposable, IJavaPeerable stuff.
            {
                int estimatedKeyboardHeight = (int)Android.Util.TypedValue.ApplyDimension(Android.Util.ComplexUnitType.Dip, EstimatedKeyboardDP, parentView.Resources.DisplayMetrics);
                parentView.GetWindowVisibleDisplayFrame(rect);//getWindowVisibleDisplayFrame(rect);
                int heightDiff = parentView.RootView.Height - (rect.Bottom - rect.Top);
                bool isShown = heightDiff >= estimatedKeyboardHeight;

                if (isShown == alreadyOpen)
                {
                    Logger.Debug("Keyboard state - Ignoring global layout change...");
                    return;
                }
                alreadyOpen = isShown;
                KeyBoardVisibilityChanged?.Invoke(null, isShown);
                //onKeyboardVisibilityListener.onVisibilityChanged(isShown);
            }
        }

        private class BottomNavigationViewAnimationListener : Java.Lang.Object, Android.Animation.Animator.IAnimatorListener
        {
            public void OnAnimationCancel(Animator animation)
            {

            }

            public void OnAnimationEnd(Animator animation)
            {
                SeekerState.MainActivityRef.FindViewById<BottomNavigationView>(Resource.Id.navigation).Visibility = ViewStates.Gone;
            }

            public void OnAnimationRepeat(Animator animation)
            {
                //throw new NotImplementedException();
            }

            public void OnAnimationStart(Animator animation)
            {
                //throw new NotImplementedException();
            }
        }

        public class OnPageChangeLister1 : Java.Lang.Object, ViewPager.IOnPageChangeListener
        {
            public void OnPageScrolled(int position, float positionOffset, int positionOffsetPixels)
            {
                //throw new NotImplementedException();
            }

            public void OnPageScrollStateChanged(int state)
            {
                //throw new NotImplementedException();
            }

            public void OnPageSelected(int position)
            {
                BottomNavigationView navigator = SeekerState.MainActivityRef?.FindViewById<BottomNavigationView>(Resource.Id.navigation);
                if (position != -1 && navigator != null)
                {
                    AndroidX.AppCompat.View.Menu.MenuBuilder menu = navigator.Menu as AndroidX.AppCompat.View.Menu.MenuBuilder;

                    menu.GetItem(position).SetCheckable(true); //this is necessary if side scrolling...
                    menu.GetItem(position).SetChecked(true);
                }
            }
        }
    }
}
