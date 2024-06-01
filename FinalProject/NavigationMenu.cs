using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V4.View;
using Android.Support.V4.Widget;
using Android.Support.V7.App;
using Android.Text;
using Android.Text.Style;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FinalProject
{
    class NavigationMenu : AppCompatActivity, NavigationView.IOnNavigationItemSelectedListener, DrawerLayout.IDrawerListener
    {
        public delegate void BaseOnBackPressed();

        private Context context;
        
        public NavigationMenu(Context context)
        {
            this.context = context;

            this.Init();
        }

        private void Init()
        {
            AppCompatActivity app = (AppCompatActivity)context;

            Android.Support.V7.Widget.Toolbar toolbar = app.FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            app.SetSupportActionBar(toolbar);

            DrawerLayout drawer = app.FindViewById<DrawerLayout>(Resource.Id.drawer_layout);

            drawer.AddDrawerListener(this);

            ActionBarDrawerToggle toggle = new ActionBarDrawerToggle(app, drawer, toolbar, Resource.String.navigation_drawer_open, Resource.String.navigation_drawer_close);
            drawer.AddDrawerListener(toggle);
            toggle.SyncState();

            NavigationView navigationView = GetNavigationView();
            navigationView.SetNavigationItemSelectedListener(this);

            this.OverrideItemsVisibility(navigationView.Menu);
            this.SetLogoutColor(navigationView);
            this.SetTitles();
        }

        public NavigationView GetNavigationView()
        {
            return ((AppCompatActivity)context).FindViewById<NavigationView>(Resource.Id.nav_view);
        }

        /* 
         * The attribute 'title' is used as a custom flag
         * to decide whether the item should be visible at the condition
         * of whether a user is currently logged in: 
         * format is: "visibility_value|actual_title"
         * "visibility_value" will be removed at run-time.
         * '0' is neutral and its default value - always displays the item.
         * '-1' only displays if NOT logged in.
         * '1' only displays if LOGGED IN.
         */
        private void OverrideItemsVisibility(IMenu menu)
        {
            for (int i = 0; i < menu.Size(); i++)
            {
                IMenuItem item = menu.GetItem(i);

                if (item.HasSubMenu)
                {
                    OverrideItemsVisibility(item.SubMenu);
                    continue;
                }
                
                string[] exploded_string = (item.TitleFormatted.ToString()).Split('|');

                // Format the title by removing the visiblity value + |.
                //
                // This if statement is required for items who didn't specify any
                // visiblity value at all, therefore they don't have a splitter. 
                if (exploded_string.Length > 1)
                {
                    item.SetTitle(exploded_string[1]);
                }

                // Item visibility value is neutral. skip it.
                if (!int.TryParse(exploded_string[0], out int visibility_value) || visibility_value == 0)
                {
                    continue;
                }

                bool is_logged_in = RuntimeClient.IsLoggedIn();
                bool is_item_visible = (is_logged_in && visibility_value == 1) || (!is_logged_in && visibility_value == -1);

                item.SetVisible(is_item_visible);
            }
        }

        private void SetLogoutColor(NavigationView navigationView)
        {
            // Find and "red color" specifically the logout item by the following steps:
            // 1. Find the user control sub menu, where the logout item is located.
            // 2. Red color it using the spannable string library.
            IMenuItem logout_item = navigationView.Menu.FindItem(Resource.Id.nav_usercontrol).SubMenu.FindItem(Resource.Id.nav_logout);

            SpannableString spanString = new SpannableString(logout_item.TitleFormatted);
            spanString.SetSpan(new ForegroundColorSpan(new Color(234, 129, 136)), 0, spanString.Length(), 0);
            logout_item.SetTitle(spanString);
        }

        private void SetTitles()
        {
            NavigationView navigation_view = GetNavigationView();

            // It's safe to assume '0' is a valid index since
            // there is a static xml header compiled into the nav view.
            View header = navigation_view.GetHeaderView(0);

            TextView tv_name = header.FindViewById<TextView>(Resource.Id.tvName);
            TextView tv_phone = header.FindViewById<TextView>(Resource.Id.tvPhone);
            TextView tv_balance = header.FindViewById<TextView>(Resource.Id.tvBalance);

            if (!RuntimeClient.IsLoggedIn())
            {
                tv_name.Text = "Hello Guest!";
                tv_phone.Text = "Sign up for access to app features";
                tv_balance.Text = "";
                return;
            }
            
            Client runtime_client = RuntimeClient.Get();
            tv_name.Text = $"Hello {runtime_client.FirstName}!";
            tv_phone.Text = $"0{runtime_client.PhoneNumber}";

            tv_balance.Text = $"Balance: {runtime_client.Coins.ToString("N0")} coins";
            tv_balance.PaintFlags = Android.Graphics.PaintFlags.UnderlineText;
        }

        // Used whenever the drawer is being opened.
        private void UpdateBalance()
        {
            // It's safe to assume '0' is a valid index since
            // there is a static xml header compiled into the nav view.
            TextView tv_balance = GetNavigationView().GetHeaderView(0).FindViewById<TextView>(Resource.Id.tvBalance);

            if (!RuntimeClient.IsLoggedIn())
            {
                tv_balance.Text = "";
                return;
            }

            Client runtime_client = RuntimeClient.Get();
            tv_balance.Text = $"Balance: {runtime_client.Coins.ToString("N0")} coins";
            tv_balance.PaintFlags = Android.Graphics.PaintFlags.UnderlineText;
        }

        // Called once the user has triggered the "back" operation by left swiping, etc..
        // If the navigation menu is open, override the action and close it first.
        // 
        // Retrieves true if successfully closed the drawer, 
        // or false if didn't.
        public bool OnBackPressed(BaseOnBackPressed base_on_back_pressed)
        {
            AppCompatActivity app = ((AppCompatActivity)context);

            DrawerLayout drawer = app.FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
            if (!drawer.IsDrawerOpen(GravityCompat.Start))
            {
                base_on_back_pressed();
                return false;
            }

            drawer.CloseDrawer(GravityCompat.Start);
            return true;
        }

        public bool OnNavigationItemSelected(IMenuItem item)
        {
            AppCompatActivity app = ((AppCompatActivity)context);

            int id = item.ItemId;
            
            switch (id)
            {
                case Resource.Id.nav_ah:
                {
                    // Handle the login action
                    context.StartActivity(new Intent(context, typeof(MainActivity)));

                    break;
                }
                case Resource.Id.nav_login:
                {
                    // Handle the login action
                    context.StartActivity(new Intent(context, typeof(LoginActivity)));

                    break;
                }
                case Resource.Id.nav_listauction:
                {
                    context.StartActivity(new Intent(context, typeof(ListAuctionActivity)));

                    break;
                }
                case Resource.Id.nav_myaccount:
                {
                    context.StartActivity(new Intent(context, typeof(AccountActivity)));

                    break;
                }
                case Resource.Id.nav_logout:
                {
                    // Delete the current runtime client.
                    RuntimeClient.Erase();

                    // Send the user back to the main activity page.
                    // Reloads the page if user is already on main page.
                    context.StartActivity(new Intent(context, typeof(MainActivity)));
                    ((AppCompatActivity)context).FinishAffinity();

                    break;
                }
            }

            DrawerLayout drawer = app.FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
            drawer.CloseDrawer(GravityCompat.Start);
            return false;
        }

        private int ItemIdFromActivity(AppCompatActivity context)
        {
            switch (context)
            {
                case MainActivity main: return Resource.Id.nav_ah;
                case AuctionOverviewActivity auction: return Resource.Id.nav_ah; 
                case LoginActivity login:return Resource.Id.nav_login;
                case ListAuctionActivity list_auction: return Resource.Id.nav_listauction;
                case AccountActivity account: return Resource.Id.nav_myaccount;

                default: return 0;
            }
        }

        private static float lastSlideOffset = 0f;
        public void OnDrawerSlide(View drawerView, float slideOffset)
        {
            float localLastSlideOffset = lastSlideOffset;
            lastSlideOffset = slideOffset;

            // The menu is sliding inwards, meaning it's being closed.
            if (localLastSlideOffset > slideOffset)
            {
                return;
            }

            // Only let it pass once.
            if (localLastSlideOffset != 0)
            {
                return;
            }

            AppCompatActivity app = ((AppCompatActivity)context);
            NavigationView navigationView = GetNavigationView();

            int item_id = ItemIdFromActivity(app);
            if (item_id != 0)
            {
                navigationView.SetCheckedItem(item_id);
            }

            this.UpdateBalance();
        }

        // Unused interface methods that we must "implement".
        public void OnDrawerOpened(View drawerView) {}
        public void OnDrawerClosed(View drawerView) {}
        public void OnDrawerStateChanged(int newState) {}
    }
}