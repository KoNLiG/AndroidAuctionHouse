using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// text color: 139, 177, 189
// dark background colors:
// 28, 30, 31
// 66, 66, 66
// light background colors:
// 63, 113, 132
// 0, 133, 159
namespace FinalProject
{
    class BackgroundManager
    {
        private Context context;
        private GradientDrawable background;

        static Random random = new Random();

        private readonly int[] light_mins = { 0, 113, 132 };
        private readonly int[] light_maxs = { 63, 133, 159 };
        private readonly int[] dark_mins = { 20, 20, 20 };
        private readonly int[] dark_maxs = { 66, 66, 66 };

        public BackgroundManager(Context context)
        {
            this.context = context;

            this.Init();
        }

        private void Init()
        {
            this.background = CreateDrawable();

            this.ApplyToLayout();
            this.ApplyToHeader();
        }

        private GradientDrawable CreateDrawable()
        {
            GradientDrawable.Orientation[] orientations = GradientDrawable.Orientation.Values();
            GradientDrawable.Orientation rnd_orientation = orientations[random.Next(0, orientations.Length)];

            int[] light_color = GenerateColor(light_mins, light_maxs);
            int[] dark_color = GenerateColor(dark_mins, dark_maxs);

            // First element is light color, second element is dark color.
            Color[] colors = { new Color(light_color[0], light_color[1], light_color[2]), new Color(dark_color[0], dark_color[1], dark_color[2]) };
            colors = RandomizeWithOrderByAndRandom(colors);

            return new GradientDrawable(rnd_orientation, new int[] { colors[0], colors[1] });
        }

        private void ApplyToLayout()
        {
            AppCompatActivity app = (AppCompatActivity)context;

            ViewGroup layout = app.FindViewById<ViewGroup>(Resource.Id.drawer_layout);

            layout.Background = CloneGradientDrawable(this.background);
        }

        private void ApplyToHeader()
        {
            AppCompatActivity app = (AppCompatActivity)context;

            NavigationView navigation_view = app.FindViewById<NavigationView>(Resource.Id.nav_view);

            // It's safe to assume '0' is a valid index since
            // there is a static xml header compiled into the nav view.
            View header = navigation_view.GetHeaderView(0);

            GradientDrawable background = CloneGradientDrawable(this.background);

            background.SetOrientation(FlipOrientation(background.GetOrientation()));

            header.Background = background;
        }

        // Generates a randomized RGB color by the given mins and maxs.
        public int[] GenerateColor(int[] mins, int[] maxs)
        {
            int[] colors = new int[3];

            for (int i = 0; i < 3; i++)
            {
                colors[i] = random.Next(mins[i], maxs[i]);
            }

            return colors;
        }

        public static Color[] RandomizeWithOrderByAndRandom(Color[] array)
        {
            return array.OrderBy(x => random.Next()).ToArray();
        }

        private GradientDrawable CloneGradientDrawable(GradientDrawable drawable)
        {
            return new GradientDrawable(drawable.GetOrientation(), drawable.GetColors());
        }

        private GradientDrawable.Orientation FlipOrientation(GradientDrawable.Orientation orientation)
        {
            // GradientDrawable.Orientation[] orientations = GradientDrawable.Orientation.Values();
            GradientDrawable.Orientation[] orientations = 
            { 
                GradientDrawable.Orientation.BlTr,
                GradientDrawable.Orientation.BottomTop,
                GradientDrawable.Orientation.BrTl,
                GradientDrawable.Orientation.LeftRight,
                GradientDrawable.Orientation.RightLeft,
                GradientDrawable.Orientation.TlBr,
                GradientDrawable.Orientation.TopBottom,
                GradientDrawable.Orientation.TrBl
            };

            int idx = Array.IndexOf(orientations, orientation);

            return orientations[orientations.Length - 1 - idx];
        }
    }
}