using Android.App;
using Android.Content;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.Content;
using Android.Text;
using Android.Views;
using Android.Widget;
using Java.Lang;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// Source: https://stackoverflow.com/a/49494031
// Slightly modified.
namespace FinalProject
{
    public class ClearableEditText
    {
        Drawable imgX;
        EditText edit_text;

        public ClearableEditText(Context context, EditText edit_text)
        {
            this.edit_text = edit_text;
            imgX = ContextCompat.GetDrawable(context, Android.Resource.Drawable.PresenceOffline);
            imgX.SetBounds(0, 0, imgX.IntrinsicWidth, imgX.IntrinsicHeight);
            manageClearButton();
            edit_text.SetOnTouchListener(new TouchHelper(edit_text, imgX));
            edit_text.AddTextChangedListener(new TextListener(this));
            edit_text.SetPadding(edit_text.PaddingLeft, edit_text.PaddingTop, 32, edit_text.PaddingBottom);
        }

        public void manageClearButton()
        {
            if (string.IsNullOrEmpty(edit_text.Text))
                removeClearButton();
            else
                addClearButton();
        }
        public void addClearButton()
        {
            edit_text.SetCompoundDrawables(edit_text.GetCompoundDrawables()[0],
                    edit_text.GetCompoundDrawables()[1],
                    imgX,
                    edit_text.GetCompoundDrawables()[3]);
        }
        public void removeClearButton()
        {
            edit_text.SetCompoundDrawables(edit_text.GetCompoundDrawables()[0],
                    edit_text.GetCompoundDrawables()[1],
                    null,
                    edit_text.GetCompoundDrawables()[3]);
        }
    }

    public class TouchHelper : Java.Lang.Object, View.IOnTouchListener
    {
        EditText Editext;
        public ClearableEditText objClearable { get; set; }
        Drawable imgX;
        public TouchHelper(EditText editext, Drawable imgx)
        {
            Editext = editext;
            objClearable = objClearable;
            imgX = imgx;
        }
        public bool OnTouch(View v, MotionEvent e)
        {
            EditText et = Editext;

            if (et.GetCompoundDrawables()[2] == null)
                return false;
            // Only do this for up touches
            if (e.Action != MotionEventActions.Up)
                return false;
            // Is touch on our clear button?
            if (e.GetX() > et.Width - et.PaddingRight - imgX.IntrinsicWidth)
            {
                Editext.Text = string.Empty;
                if (objClearable != null)
                    objClearable.removeClearButton();

            }
            return false;
        }
    }

    public class TextListener : Java.Lang.Object, ITextWatcher
    {
        public ClearableEditText objClearable { get; set; }
        public TextListener(ClearableEditText objRef)
        {
            objClearable = objRef;
        }

        public void AfterTextChanged(IEditable s)
        {

        }

        public void BeforeTextChanged(ICharSequence s, int start, int count, int after)
        {

        }

        public void OnTextChanged(ICharSequence s, int start, int before, int count)
        {
            if (objClearable != null)
                objClearable.manageClearButton();
        }
    }
}