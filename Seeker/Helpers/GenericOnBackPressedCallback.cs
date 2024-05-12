using AndroidX.Activity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seeker.Helpers
{
    public class GenericOnBackPressedCallback : OnBackPressedCallback
    {
        Action<OnBackPressedCallback> onBackPressed;
        public GenericOnBackPressedCallback(bool enabled, Action<OnBackPressedCallback> onBackPressed) : base(enabled)
        {
            this.onBackPressed = onBackPressed;
        }

        public override void HandleOnBackPressed()
        {
            onBackPressed(this);
        }
    }
}
