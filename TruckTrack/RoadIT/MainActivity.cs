using Android.App;
using Android.Widget;
using Android.Hardware;
using Android.Locations;
using Android.Content;
using Android.OS;
using Android.Gms.Common;
using Android.Util;
using System;
using Android.Support.V4.App;
using Android.Support.Design.Widget;
using Android.Views;

using System.Threading;

namespace RoadIT
{
	/**
	 * MainActivity is used to configure the truck/finisher. It also requests permissions and checks if google play services is installed.
	 */
	[Activity(Label = "TruckTrack", MainLauncher = true, Icon = "@drawable/trucktrackicon", ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation)]
	public class MainActivity : Activity
	{
		public static readonly int InstallGooglePlayServicesId = 1000;
		private bool _isGooglePlayServicesInstalled;
		static readonly int REQUEST_COARSELOCATION = 0;
		static readonly int REQUEST_FINELOCATION = 1;
		static readonly int REQUEST_INTERNET = 2;
		float floatloadpermeter=0;
		View layout;

		//oncreate is called when the app is started
		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);

			//check if google play services is installed, necessary for using the google maps API
			_isGooglePlayServicesInstalled = TestIfGooglePlayServicesIsInstalled();

			//settingsview to configure paramaters of truck/finisher
			SetContentView(Resource.Layout.Setup);

			//init locationmanager
			initLocationManager();

			//request permission
			RequestInternetPermission();

			RadioButton finisher = FindViewById<RadioButton> (Resource.Id.radioButtonfinisher);
			SeekBar loadpermeter = FindViewById<SeekBar> (Resource.Id.seekBarLoadPerMeter);
			TextView loadtext = FindViewById<TextView> (Resource.Id.textViewloadpermeter);
			loadpermeter.Visibility = ViewStates.Gone;
			loadtext.Visibility = ViewStates.Gone;

			//loadpermeter seekbaar only visible if finisher is checked
			finisher.CheckedChange += delegate(object sender, CompoundButton.CheckedChangeEventArgs e) {
				if (finisher.Checked==true){
					loadpermeter.Visibility= ViewStates.Visible;
					loadtext.Visibility= ViewStates.Visible;
				} else{
					loadpermeter.Visibility = ViewStates.Gone;
					loadtext.Visibility = ViewStates.Gone;
				}
			};
			loadpermeter.ProgressChanged += delegate(object sender, SeekBar.ProgressChangedEventArgs e) {
				floatloadpermeter = loadpermeter.Progress;
				floatloadpermeter = floatloadpermeter/100;
				loadtext.Text = "Load per meter: "+ floatloadpermeter.ToString("0.00")+"%/m";
			};
			loadpermeter.Progress = 250;
			confirmSettings();
		}

		//confirm settings and pass to ownVehicle activity when startbutton is pressed
		public void confirmSettings()
		{
			Button startButton = FindViewById<Button>(Resource.Id.startButton);
			startButton.Click += delegate {
				EditText broker = FindViewById<EditText>(Resource.Id.editTextbroker);
				string brokerstring = broker.Text;

				EditText name = FindViewById<EditText>(Resource.Id.editTextname);
				string namestring = name.Text;

				EditText username = FindViewById<EditText>(Resource.Id.editTextusername);
				string usernamestring = username.Text;

				EditText pass = FindViewById<EditText>(Resource.Id.editTextpassword);
				string passtring = username.Text;

				RadioButton truckfin = FindViewById<RadioButton>(Resource.Id.radiotruck);
				string truck;
				if (truckfin.Checked==true){
					truck = "true";
				}else{
					truck="false";
				}

				//create ownvehicle activity
				SampleActivity activityfin = new SampleActivity(1, 2, typeof(OwnVehicle));
				//create var to give activity extra parameters
				var ownvec = new Intent(this, typeof(OwnVehicle));
				ownvec.PutExtra("broker",brokerstring );
				ownvec.PutExtra("name",namestring );
				ownvec.PutExtra("username",usernamestring );
				ownvec.PutExtra("pass",passtring );
				ownvec.PutExtra("truck",truck);
				ownvec.PutExtra("loadpermeter", floatloadpermeter.ToString("0.00"));
				//start ownvehicle activity
				StartActivity(ownvec);
			};
		}


		//check if google play services is installed
		private bool TestIfGooglePlayServicesIsInstalled()
		{
			int queryResult = GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(this);
			if (queryResult == ConnectionResult.Success)
			{
				Log.Info("Road IT", "Google Play Services is installed on this device.");
				return true;
			}

			if (GoogleApiAvailability.Instance.IsUserResolvableError(queryResult))
			{
				string errorString = GoogleApiAvailability.Instance.GetErrorString(queryResult);
				Log.Error("Road IT", "There is a problem with Google Play Services on this device: {0} - {1}", queryResult, errorString);
				Dialog errorDialog = GoogleApiAvailability.Instance.GetErrorDialog(this, queryResult, InstallGooglePlayServicesId);
			}
			return false;
		}


		//locationmanager checks for coarse, fine and internet permission
		public void initLocationManager()
		{
			//coarse location
			if (ActivityCompat.CheckSelfPermission(this, Android.Manifest.Permission.AccessCoarseLocation) != (int)Android.Content.PM.Permission.Granted)
			{

				// CoarseLocation permission has not been granted
				RequestCoarsePermission();
			}
			//fine location
			if (ActivityCompat.CheckSelfPermission(this, Android.Manifest.Permission.AccessFineLocation) != (int)Android.Content.PM.Permission.Granted)
			{

				// FineLocation permission has not been granted
				RequestFinePermission();
			}
			//internet
			if (ActivityCompat.CheckSelfPermission(this, Android.Manifest.Permission.Internet) != (int)Android.Content.PM.Permission.Granted)
			{

				// Internet permission has not been granted
				RequestInternetPermission();
			}
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

				Snackbar
					.Make(layout, "Message sent", Snackbar.LengthLong)
  					.SetAction("OK", (view) => { ActivityCompat.RequestPermissions(this, new String[] { Android.Manifest.Permission.AccessCoarseLocation }, REQUEST_COARSELOCATION); })
  					.Show(); 
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

			if (ActivityCompat.ShouldShowRequestPermissionRationale(this, Android.Manifest.Permission.AccessCoarseLocation))
			{
				// Provide an additional rationale to the user if the permission was not granted
				// and the user would benefit from additional context for the use of the permission.
				// For example if the user has previously denied the permission.
				//Log.Info (TAG, "Displaying Fine permission rationale to provide additional context.");

				Snackbar
					.Make(layout, "Message sent", Snackbar.LengthLong)
  					.SetAction("OK", (view) => {ActivityCompat.RequestPermissions(this, new String[] { Android.Manifest.Permission.AccessFineLocation }, REQUEST_FINELOCATION); })
  					.Show();
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

			if (ActivityCompat.ShouldShowRequestPermissionRationale(this, Android.Manifest.Permission.AccessCoarseLocation))
			{
				// Provide an additional rationale to the user if the permission was not granted
				// and the user would benefit from additional context for the use of the permission.
				// For example if the user has previously denied the permission.
				//Log.Info (TAG, "Displaying Intenet permission rationale to provide additional context.");

				Snackbar
					.Make(layout, "Message sent", Snackbar.LengthLong)
  					.SetAction("OK", (view) => { ActivityCompat.RequestPermissions(this, new String[] { Android.Manifest.Permission.Internet }, REQUEST_INTERNET); })
  					.Show();
			}
			else {
				// Camera permission has not been granted yet. Request it directly.
				ActivityCompat.RequestPermissions(this, new String[] { Android.Manifest.Permission.Internet }, REQUEST_INTERNET);
			}
		}


	}
}


