using Android.Content;
using Android.Graphics;
using Android.Views;
using Android.Widget;
using MySqlConnector;
using System.Collections.Generic;

namespace FinalProject
{
    public class AddImageAdapter : BaseAdapter
    {
        Context context;
        public static List<Bitmap> images = new List<Bitmap>();

        public AddImageAdapter(Context c)
        {
            context = c;
        }

        public override int Count { get { return images.Count; } }

        public override Java.Lang.Object GetItem(int position)
        {
            return null;
        }

        public override long GetItemId(int position)
        {
            return 0;
        }

        // create a new ImageView for each item referenced by the Adapter
        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            ImageView i = new ImageView(context);

            i.SetImageBitmap(images[position]);
            i.LayoutParameters = new Gallery.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
            i.SetScaleType(ImageView.ScaleType.FitXy);
            i.SetAdjustViewBounds(true);

            return i;
        }
    }
}