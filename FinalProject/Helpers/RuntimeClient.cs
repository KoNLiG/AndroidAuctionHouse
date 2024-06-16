using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace FinalProject
{
    static class RuntimeClient
    {
        // Client phone number that's used to retrieve data from the database regarding clients.
        // 0 if no runtime client is available.
        private static int client_phone_number;

        // Cookie that stores the runtime client phone number.
        private static ISharedPreferences sp;

        // Attempts to load the last runtime client. 
        // Sets 'client' to null if there wasn't any.
        // Note: first 'Load' call must be from MainActivity.
        public static void Load(Context context = null)
        {
            // Initialize 'sp' if haven't.
            if (context != null && sp == null)
            {
                sp = context.GetSharedPreferences("runtime_client", FileCreationMode.Private);
            }

            if ((client_phone_number = sp.GetInt("phone_number", 0)) == 0)
            {
                // No runtime client, don't continue.
                return;
            }

            // There was a runtime client that doesn't exist anymore.
            if (new Client(client_phone_number).PhoneNumber == 0)
            {
                Erase();
            }
        }

        // Stores a run-time client.
        public static void Save(int client_phone_number)
        {
            ISharedPreferencesEditor editor = sp.Edit();
            editor.PutInt("phone_number", client_phone_number);
            editor.Commit();

            // Load the client that we just saved.
            RuntimeClient.Load();
        }

        public static void Erase()
        {
            ISharedPreferencesEditor editor = sp.Edit();
            editor.Clear();
            editor.Commit();

            // (un)load the client that we just deleted.
            RuntimeClient.Load();
        }

        // Retrieves the run-time client object.
        // Don't store this client locally, since its data can and will change.
        public static Client Get()
        {
            // No runtime client, don't continue.
            if (client_phone_number == 0)
            {
                return null;
            }

            // 'Client' constructor essentially retrieves all the relevant data
            // regarding the runtime client from the database.
            return new Client(client_phone_number);
        }

        public static bool IsLoggedIn()
        {
            return Get() != null;
        }
    }
}