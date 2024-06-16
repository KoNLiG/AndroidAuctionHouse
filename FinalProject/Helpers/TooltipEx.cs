using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Com.Tomergoldst.Tooltips;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace FinalProject
{
    class TooltipEx : ToolTip.Builder
    {
        private static ToolTipsManager tooltips_mgr;

        public ToolTip.Builder builder;

        private int timeout;

        // New feature: 'timeout'
        // Amount for seconds for the tooltip view to disappear, 0 to last forever (default).
        public TooltipEx(Context context, View anchorView, ViewGroup root, string message, int position, int timeout = 0) : base(context, anchorView, root, message, position)
        {
            InitializeMgr((ToolTipsManager.ITipListener)context);

            builder = new ToolTip.Builder(context, anchorView, root, message, position);

            this.timeout = timeout;
        }

        public void Display()
        {
            View tooltip_view = tooltips_mgr.Show(builder.Build());

            if (timeout != 0)
            {
                // * 1000 for miliseconds to seconds.
                Timer t = new Timer(TimerCallback, tooltip_view, timeout * 1000, 0);
            }
        }

        // Attempts to initialize the tooltips manager only if we haven't.
        private static void InitializeMgr(ToolTipsManager.ITipListener listener)
        {
            if (tooltips_mgr == null)
            {
                tooltips_mgr = new ToolTipsManager(listener);
            }
        }

        private static void TimerCallback(Object o)
        {
            View tooltip_view = (View)o;

            tooltip_view.PostDelayed(() => {
                tooltips_mgr.Dismiss(tooltip_view, false);
            }, 100);
        }
    }
}