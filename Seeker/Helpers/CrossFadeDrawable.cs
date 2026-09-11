using Android.Content.Res;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using System;

namespace Seeker.Helpers
{
    /// <summary>
    /// A two-layer crossfade for VectorDrawables (TransitionDrawable does not)
    /// Also keeps track of state (so no reverse() twice issue, lack of idempotency), 
    ///   we just tell it which way we want it to go and it heads there
    /// </summary>
    public class CrossFadeDrawable : Drawable
    {
        private readonly Drawable first;
        private readonly Drawable second;

        private float progress; // 0 is first drawable
        private float animateFrom;
        private float animateTo;
        private long animateStartMs = -1;
        private int animateDurationMs;
        private bool animating;

        private int alpha = 255;

        public CrossFadeDrawable(Drawable first, Drawable second)
        {
            this.first = first.Mutate();
            this.second = second.Mutate();
        }

        public bool ShowingSecond => animating ? animateTo >= 1f : progress >= 1f;

        /// <summary>
        /// Crossfades to the second layer (<paramref name="show"/> true) or back to the first, over
        /// <paramref name="durationMs"/>; 0 jumps. No-op when already at, or animating towards, that state.
        /// Interrupting an animation in flight continues from the current frame.
        /// </summary>
        public void ShowSecond(bool show, int durationMs)
        {
            float target = show ? 1f : 0f;
            if (animating ? animateTo == target : progress == target)
            {
                return;
            }
            if (durationMs <= 0)
            {
                animating = false;
                progress = target;
                InvalidateSelf();
                return;
            }
            animateFrom = progress;
            animateTo = target;
            animateDurationMs = (int)(durationMs * Math.Abs(target - progress));
            animateStartMs = -1; // stamped on the first frame drawn, like TransitionDrawable
            animating = true;
            InvalidateSelf();
        }

        public override void Draw(Canvas canvas)
        {
            if (animating)
            {
                long now = SystemClock.UptimeMillis();
                if (animateStartMs < 0)
                {
                    animateStartMs = now;
                }
                float t = animateDurationMs <= 0 ? 1f : Math.Min(1f, (now - animateStartMs) / (float)animateDurationMs);
                progress = animateFrom + (animateTo - animateFrom) * t;
                if (t >= 1f)
                {
                    animating = false;
                }
            }

            int secondAlpha = (int)(progress * alpha + 0.5f);
            DrawLayer(canvas, first, alpha - secondAlpha);
            DrawLayer(canvas, second, secondAlpha);

            if (animating)
            {
                InvalidateSelf();
            }
        }

        private static void DrawLayer(Canvas canvas, Drawable layer, int layerAlpha)
        {
            if (layerAlpha <= 0)
            {
                return;
            }
            layer.SetAlpha(layerAlpha);
            layer.Draw(canvas);
        }

        public override int IntrinsicWidth => Math.Max(first.IntrinsicWidth, second.IntrinsicWidth);

        public override int IntrinsicHeight => Math.Max(first.IntrinsicHeight, second.IntrinsicHeight);

        protected override void OnBoundsChange(Rect bounds)
        {
            base.OnBoundsChange(bounds);
            first.SetBounds(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom);
            second.SetBounds(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom);
        }

        public override void SetAlpha(int alpha)
        {
            if (this.alpha != alpha)
            {
                this.alpha = alpha;
                InvalidateSelf();
            }
        }

        public override int Alpha => alpha;

        public override void SetColorFilter(ColorFilter colorFilter)
        {
            first.SetColorFilter(colorFilter);
            second.SetColorFilter(colorFilter);
            InvalidateSelf();
        }

        public override void SetTintList(ColorStateList tint)
        {
            first.SetTintList(tint);
            second.SetTintList(tint);
            InvalidateSelf();
        }

        public override void SetTintMode(PorterDuff.Mode tintMode)
        {
            first.SetTintMode(tintMode);
            second.SetTintMode(tintMode);
            InvalidateSelf();
        }

        public override bool SetVisible(bool visible, bool restart)
        {
            first.SetVisible(visible, restart);
            second.SetVisible(visible, restart);
            return base.SetVisible(visible, restart);
        }

        public override int Opacity => (int)Format.Translucent;
    }
}
