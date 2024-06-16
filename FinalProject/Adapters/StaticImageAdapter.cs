using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FinalProject
{
    class StaticImageAdapter : BaseAdapter
    {
        Context context;
        private List<Bitmap> images;

        public StaticImageAdapter(Context c, List<Bitmap> images)
        {
            context = c;
            this.images = images;
        }

        public override int Count 
        { 
            get 
            {
                if (images == null)
                {
                    return 1; // 1 for the default "unknown" image.
                }

                return images.Count;
            } 
        }

        public override Java.Lang.Object GetItem(int position)
        {
            return images[position];
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        // create a new ImageView for each item referenced by the Adapter
        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            ImageView i = new ImageView(context);

            if (images != null)
            { 
                i.SetImageBitmap(images[position]);
            }
            else
            {
                i.SetImageResource(Resource.Drawable.unknown_item_image);
            }

            i.LayoutParameters = new Gallery.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
            i.SetScaleType(ImageView.ScaleType.FitXy);
            i.SetAdjustViewBounds(true);

            return i;
        }
    }
}