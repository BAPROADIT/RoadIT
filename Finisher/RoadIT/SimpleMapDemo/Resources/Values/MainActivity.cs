namespace ROADIT
{
    using System.Collections.Generic;

    using Android.App;
    using Android.Content;
    using Android.Gms.Common;
    using Android.OS;
    using Android.Util;
    using Android.Views;
    using Android.Widget;
    using AndroidUri = Android.Net.Uri;
	using Android.Hardware;
	using Android.Locations;
	using System;
	//using Android.Support.Design.Widget;
	using Android.Support.V4.App;
	using Android.Content.PM;

    [Activity(Label = "@string/app_name", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : ListActivity
    {
        public static readonly int InstallGooglePlayServicesId = 1000;
        public static readonly string Tag = "ROAD IT";

        //private List<SampleActivity> _activities;
        private bool _isGooglePlayServicesInstalled;

		static readonly int REQUEST_COARSELOCATION = 0;
		static readonly int REQUEST_FINELOCATION = 1;
		static readonly int REQUEST_INTERNET = 2;

		static string[] PERMISSIONS_CONTACT = {

			Android.Manifest.Permission.Internet,
			Android.Manifest.Permission.AccessCoarseLocation,
			Android.Manifest.Permission.AccessFineLocation,
		};


        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            switch (resultCode)
            {
                case Result.Ok:
                    // Try again.
                    _isGooglePlayServicesInstalled = true;
                    break;

                default:
                    Log.Debug("MainActivity", "Unknown resultCode {0} for request {1}", resultCode, requestCode);
                    break;
            }
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            _isGooglePlayServicesInstalled = TestIfGooglePlayServicesIsInstalled();
			//gps = new GPS((LocationManager)GetSystemService(LocationService), _gpsText);
            initLocationManager();
			RequestInternetPermission();

			//InitializeListView();
			SampleActivity activity = new SampleActivity(Resource.String.activity_label_mapwithmarkers, Resource.String.activity_description_mapwithmarkers, typeof(MapWithMarkersActivity));
			activity.Start(this);

        }

//        protected override void OnListItemClick(ListView l, View v, int position, long id)
//        {
//            if (position == 0)
//            {
//                AndroidUri geoUri = AndroidUri.Parse("geo:42.374260,-71.120824");
//                Intent mapIntent = new Intent(Intent.ActionView, geoUri);
//                StartActivity(mapIntent);
//                return;
//            }
//
//            //SampleActivity activity = _activities[position];
//            //activity.Start(this);
//        }

//        private void InitializeListView()
//        {
//            if (_isGooglePlayServicesInstalled)
//            {
////                _activities = new List<SampleActivity>
////                                  {
////                                      new SampleActivity(Resource.String.mapsAppText, Resource.String.mapsAppTextDescription, null),
////                                      new SampleActivity(Resource.String.activity_label_axml, Resource.String.activity_description_axml, typeof(BasicDemoActivity)),
////                                      new SampleActivity(Resource.String.activity_label_mapwithmarkers, Resource.String.activity_description_mapwithmarkers, typeof(MapWithMarkersActivity)),
////              	                      new SampleActivity(Resource.String.activity_label_mapwithoverlays, Resource.String.activity_description_mapwithoverlays, typeof(MapWithOverlaysActivity))
////                                  };
////
////                ListAdapter = new SimpleMapDemoActivityAdapter(this, _activities);
//
//            }
//            else
//            {
//                Log.Error("MainActivity", "Google Play Services is not installed");
//                ListAdapter = new SimpleMapDemoActivityAdapter(this, null);
//            }
//        }
//
        private bool TestIfGooglePlayServicesIsInstalled()
        {
			int queryResult = GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable (this);
            if (queryResult == ConnectionResult.Success)
            {
                Log.Info(Tag, "Google Play Services is installed on this device.");
                return true;
            }

			if (GoogleApiAvailability.Instance.IsUserResolvableError (queryResult))
            {
				string errorString = GoogleApiAvailability.Instance.GetErrorString(queryResult);
                Log.Error(Tag, "There is a problem with Google Play Services on this device: {0} - {1}", queryResult, errorString);
				Dialog errorDialog = GoogleApiAvailability.Instance.GetErrorDialog (this, queryResult, InstallGooglePlayServicesId);
                ErrorDialogFragment dialogFrag = new ErrorDialogFragment(errorDialog);

                dialogFrag.Show(FragmentManager, "GooglePlayServicesDialog");
            }
            return false;
        }

		public void initLocationManager()
		{
			if (ActivityCompat.CheckSelfPermission(this, Android.Manifest.Permission.AccessCoarseLocation) != (int)Android.Content.PM.Permission.Granted)
			{

				// CoarseLocation permission has not been granted
				RequestCoarsePermission();
			}
			if (ActivityCompat.CheckSelfPermission(this, Android.Manifest.Permission.AccessFineLocation) != (int)Android.Content.PM.Permission.Granted)
			{

				// FineLocation permission has not been granted
				RequestFinePermission();
			}
			if (ActivityCompat.CheckSelfPermission(this, Android.Manifest.Permission.Internet) != (int)Android.Content.PM.Permission.Granted)
			{

				// Internet permission has not been granted
				RequestInternetPermission();
			}

			//gps.InitializeLocationManager();
		}

		/**
     	* Requests the CoarseLocation permission.
		* If the permission has been denied previously, a SnackBar will prompt the user to grant the
		* permission, otherwise it is requested directly.
		*/
		void RequestCoarsePermission()
		{
			//Log.Info (TAG, "COARSE permission has NOT been granted. Requesting permission.");

			if (ActivityCompat.ShouldShowRequestPermissionRationale(this, Android.Manifest.Permission.AccessCoarseLocation))
			{
				// Provide an additional rationale to the user if the permission was not granted
				// and the user would benefit from additional context for the use of the permission.
				// For example if the user has previously denied the permission.
				//Log.Info (TAG, "Displaying COARSE permission rationale to provide additional context.");

				//Snackbar.Make(layout, Resource.String.hello,
				//	Snackbar.LengthIndefinite).SetAction(Resource.String.hello, new Action<View>(delegate (View obj)
				//	{
				//		ActivityCompat.RequestPermissions(this, new String[] { Android.Manifest.Permission.AccessCoarseLocation }, REQUEST_COARSELOCATION);
				//	})).Show();

				ActivityCompat.RequestPermissions(this, new String[] { Android.Manifest.Permission.AccessCoarseLocation }, REQUEST_COARSELOCATION);
			}
			else {
				// Camera permission has not been granted yet. Request it directly.
				ActivityCompat.RequestPermissions(this, new String[] { Android.Manifest.Permission.AccessCoarseLocation }, REQUEST_COARSELOCATION);
			}
		}

		/**
     	* Requests the FineLocation permission.
		* If the permission has been denied previously, a SnackBar will prompt the user to grant the
		* permission, otherwise it is requested directly.
		*/
		void RequestFinePermission()
		{
			//Log.Info (TAG, "Fine permission has NOT been granted. Requesting permission.");

			if (ActivityCompat.ShouldShowRequestPermissionRationale(this, Android.Manifest.Permission.AccessFineLocation))
			{
				// Provide an additional rationale to the user if the permission was not granted
				// and the user would benefit from additional context for the use of the permission.
				// For example if the user has previously denied the permission.
				//Log.Info (TAG, "Displaying Fine permission rationale to provide additional context.");

				//Snackbar.Make(layout, Resource.String.hello,
				//	Snackbar.LengthIndefinite).SetAction(Resource.String.hello, new Action<View>(delegate (View obj)
				//	{
				//		ActivityCompat.RequestPermissions(this, new String[] { Android.Manifest.Permission.AccessFineLocation }, REQUEST_FINELOCATION);
				//	})).Show();

				ActivityCompat.RequestPermissions(this, new String[] { Android.Manifest.Permission.AccessFineLocation }, REQUEST_FINELOCATION);

			}
			else {
				// Camera permission has not been granted yet. Request it directly.
				ActivityCompat.RequestPermissions(this, new String[] { Android.Manifest.Permission.AccessFineLocation }, REQUEST_FINELOCATION);
			}
		}

		/**
     	* Requests the Internet permission.
		* If the permission has been denied previously, a SnackBar will prompt the user to grant the
		* permission, otherwise it is requested directly.
		*/
		void RequestInternetPermission()
		{
			//Log.Info (TAG, "Internet permission has NOT been granted. Requesting permission.");

			ActivityCompat.RequestPermissions(this, new String[] { Android.Manifest.Permission.Internet }, REQUEST_INTERNET);

			if (ActivityCompat.ShouldShowRequestPermissionRationale(this, Android.Manifest.Permission.Internet))
			{
				// Provide an additional rationale to the user if the permission was not granted
				// and the user would benefit from additional context for the use of the permission.
				// For example if the user has previously denied the permission.
				//Log.Info (TAG, "Displaying Intenet permission rationale to provide additional context.");

				//Snackbar.Make(layout, Resource.String.hello,
				//	Snackbar.LengthIndefinite).SetAction(Resource.String.hello, new Action<View>(delegate (View obj)
				//	{
				//		ActivityCompat.RequestPermissions(this, new String[] { Android.Manifest.Permission.Internet }, REQUEST_INTERNET);
				//	})).Show();

				ActivityCompat.RequestPermissions(this, new String[] { Android.Manifest.Permission.Internet }, REQUEST_INTERNET);
			}
			else {
				// Camera permission has not been granted yet. Request it directly.
				ActivityCompat.RequestPermissions(this, new String[] { Android.Manifest.Permission.Internet }, REQUEST_INTERNET);
			}
		}
    }
}
