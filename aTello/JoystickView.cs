using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Java.Lang;

namespace aTello
{
    public class JoystickView : View
    {
        private Paint circlePaint;
        private Paint handlePaint;
        private double touchX, touchY;
        private int innerPadding;
        private int handleRadius;
        private int handleInnerBoundaries;
        public float curX, curY;

        public JoystickView(Context context, IAttributeSet attrs) :
            base(context, attrs)
        {
            Initialize();
        }

        public JoystickView(Context context, IAttributeSet attrs, int defStyle) :
            base(context, attrs, defStyle)
        {
            Initialize();
        }

        private void Initialize()
        {
            initJoystickView();
        }
        private void initJoystickView()
        {
            //SetFocusable(ViewFocusability.Focusable);

            circlePaint = new Paint(PaintFlags.AntiAlias);
            circlePaint.Color = (Color.ParseColor("#55aaaaaa"));
            circlePaint.StrokeWidth = 1;
            circlePaint.SetStyle(Paint.Style.FillAndStroke);

            handlePaint = new Paint(PaintFlags.AntiAlias);
            handlePaint.Color = (Color.ParseColor("#551111aa"));
            handlePaint.StrokeWidth = (1);
            handlePaint.SetStyle(Paint.Style.FillAndStroke);

            handlePaint.TextSize=(64);


            innerPadding = 10;
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            // Here we make sure that we have a perfect circle
            int measuredWidth = measure(widthMeasureSpec);
            int measuredHeight = measure(heightMeasureSpec);
            int d = System.Math.Min(measuredWidth, measuredHeight);

            handleRadius = (int)(d * 0.20);
            handleInnerBoundaries = handleRadius;

            SetMeasuredDimension(d, d);
        }

        private int measure(int measureSpec)
        {
            int result = 0;
            var specMode = MeasureSpec.GetMode(measureSpec);
            int specSize = MeasureSpec.GetSize(measureSpec);
            if (specMode == MeasureSpecMode.Unspecified)
            {
                result = 200;
            }
            else
            {
                result = specSize;
            }
            return result;
        }

        protected override void OnDraw(Canvas canvas)
        {
            int px = MeasuredWidth / 2;
            int py = MeasuredHeight / 2;
            int radius = System.Math.Min(px, py);

            //background
            canvas.DrawCircle(px, py, radius - innerPadding, circlePaint);

            //thumb
            canvas.DrawCircle((int)touchX + px, (int)touchY + py,
                    handleRadius, handlePaint);

            //canvas.DrawText(string.Format("X:{0:0.00} Y:{1:0.00} ",curX,curY), 10, 100, handlePaint);

            canvas.Save();
        }

        public override bool OnTouchEvent(MotionEvent xevent)
        {
            var actionType = xevent.Action;
            if (actionType == MotionEventActions.Move)
            {
                int px = MeasuredWidth / 2;
                int py = MeasuredHeight / 2;
                int radius = System.Math.Min(px, py) - handleInnerBoundaries;

                touchX = (xevent.GetX() - px);
                touchX = System.Math.Max(System.Math.Min(touchX, radius), -radius);

                touchY = (xevent.GetY() - py);
                touchY = System.Math.Max(System.Math.Min(touchY, radius), -radius);

                curX = ((float)touchX / radius);
                curY = ((float)touchY / radius);

                //Console.WriteLine("X:" + (touchX / radius * sensitivity) + "|Y:" + (touchY / radius * sensitivity));

                //if (listener != null)
                {
                    //    listener.OnMoved((int)(touchX / radius * sensitivity), (int)(touchY / radius * sensitivity));
                }

                Invalidate();
            }
            else if (actionType == MotionEventActions.Up)
            {

                curX = 0;
                curY = 0;
                returnHandleToCenter();
                //Console.WriteLine("X:" + touchX + "|Y:" + touchY);
            }
            return true;
        }

        private void returnHandleToCenter()
        {
            Handler handler = new Handler();
            int numberOfFrames = 5;
            double intervalsX = (0 - touchX) / numberOfFrames;
            double intervalsY = (0 - touchY) / numberOfFrames;

            for (int i = 0; i < numberOfFrames; i++)
            {
                handler.PostDelayed(() =>
                {
                    {
                        touchX += intervalsX;
                        touchY += intervalsY;
                        Invalidate();
                    }
                }, i * 40);
            }

            //if (listener != null)
            {
            //    listener.OnReleased();
            }
        }
    }
}

