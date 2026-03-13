using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Views;

namespace Seeker.Transfers
{
    public class SegmentedProgressBar : View
    {
        private readonly Paint paintSucceeded = new Paint(PaintFlags.AntiAlias);
        private readonly Paint paintInProgress = new Paint(PaintFlags.AntiAlias);
        private readonly Paint paintNotYetDownloaded = new Paint(PaintFlags.AntiAlias);
        private readonly Paint paintFailed = new Paint(PaintFlags.AntiAlias);

        private int succeeded, inProgress, notYetDownloaded, failed;

        private readonly Path clipPath = new Path();
        private float cornerRadius;

        public SegmentedProgressBar(Context context) : base(context)
        {
            Init();
        }

        public SegmentedProgressBar(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            Init();
        }

        public SegmentedProgressBar(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
        {
            Init();
        }

        private void Init()
        {
            cornerRadius = TypedValue.ApplyDimension(ComplexUnitType.Dip, 4, Resources.DisplayMetrics);
            ResolveColors();
        }

        private void ResolveColors()
        {
            var resources = Context.Resources;
            var theme = Context.Theme;

            paintSucceeded.Color = new Color(resources.GetColor(Resource.Color.transferChipCompletedText, theme));
            paintInProgress.Color = new Color(resources.GetColor(Resource.Color.transferChipDownloadingText, theme));
            paintNotYetDownloaded.Color = new Color(resources.GetColor(Resource.Color.transferProgressTrack, theme));
            paintFailed.Color = new Color(resources.GetColor(Resource.Color.transferChipFailedText, theme));
        }

        public void SetSegments(int succeeded, int inProgress, int notYetDownloaded, int failed)
        {
            if (this.succeeded == succeeded && this.inProgress == inProgress &&
                this.notYetDownloaded == notYetDownloaded && this.failed == failed)
            {
                return;
            }

            this.succeeded = succeeded;
            this.inProgress = inProgress;
            this.notYetDownloaded = notYetDownloaded;
            this.failed = failed;
            Invalidate();
        }

        protected override void OnSizeChanged(int w, int h, int oldw, int oldh)
        {
            base.OnSizeChanged(w, h, oldw, oldh);
            clipPath.Reset();
            clipPath.AddRoundRect(0, 0, w, h, cornerRadius, cornerRadius, Path.Direction.Cw);
        }

        protected override void OnDraw(Canvas canvas)
        {
            base.OnDraw(canvas);

            int w = Width;
            int h = Height;
            if (w == 0 || h == 0)
            {
                return;
            }

            canvas.Save();
            canvas.ClipPath(clipPath);

            // Grey background (not yet downloaded)
            canvas.DrawRect(0, 0, w, h, paintNotYetDownloaded);

            int total = succeeded + inProgress + notYetDownloaded + failed;
            if (total <= 0)
            {
                canvas.Restore();
                return;
            }

            // Green (succeeded) from left
            float x = 0;
            if (succeeded > 0)
            {
                float segW = (float)succeeded / total * w;
                canvas.DrawRect(x, 0, x + segW, h, paintSucceeded);
                x += segW;
            }

            // Blue (in progress) next
            if (inProgress > 0)
            {
                float segW = (float)inProgress / total * w;
                canvas.DrawRect(x, 0, x + segW, h, paintInProgress);
            }

            // Red (failed) from right
            if (failed > 0)
            {
                float segW = (float)failed / total * w;
                canvas.DrawRect(w - segW, 0, w, h, paintFailed);
            }

            canvas.Restore();
        }
    }
}
