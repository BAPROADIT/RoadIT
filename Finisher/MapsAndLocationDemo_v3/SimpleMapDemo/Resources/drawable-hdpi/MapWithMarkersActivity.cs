namespace SimpleMapDemo
{
    using Android.App;
    using Android.Gms.Maps;
    using Android.Gms.Maps.Model;
    using Android.OS;
	using Android.Locations;
	using Android.Util;
    using Android.Widget;
	using Android.Content;
	using Android.Runtime;
	using Android.Views;
	using System.Json;
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using Newtonsoft.Json;
	using Newtonsoft.Json.Linq;

	[Activity(Label = "ROAD IT", MainLauncher = true)]

	public class MapWithMarkersActivity : Activity, ILocationListener
    {
        private static readonly LatLng truck1loc = new LatLng(51.229241, 4.404648);
		private LatLng finisherloc = new LatLng(0,0);
		private GoogleMap map;
        private MapFragment mapFragment;
		private LocationManager locMgr;
		string tag = "MainActivity";
		MarkerOptions markerfinisher = new MarkerOptions();
		MarkerOptions markertruck = new MarkerOptions();
		Boolean firstloc = true;

		public void OnLocationChanged(Android.Locations.Location location)
		{
			finisherloc = new LatLng(location.Latitude,location.Longitude);
			if(firstloc == true)
			{
				InitMarkers();
				ZoomOnLoc();
				firstloc = false;
			}
			RefreshMarkers();
			getDuration();
		}

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
			Log.Debug (tag, "OnCreate called");		
            SetContentView(Resource.Layout.MapLayout);
            InitMapFragment();
			SetupAnimateToButton();
        }

        protected override void OnResume()
        {
            base.OnResume();
			Log.Debug (tag, "OnResume called");

			// initialize location manager
			locMgr = GetSystemService (Context.LocationService) as LocationManager;

			// pass in the provider (GPS),
			// the minimum time between updates (in seconds),
			// the minimum distance the user needs to move to generate an update (in meters),
			// and an ILocationListener (recall that this class impletents the ILocationListener interface)
			if (locMgr.AllProviders.Contains (LocationManager.NetworkProvider)
				&& locMgr.IsProviderEnabled (LocationManager.NetworkProvider)) {
				locMgr.RequestLocationUpdates (LocationManager.NetworkProvider, 2000, 1, this);
			} else {
				Toast.MakeText (this, "The Network Provider does not exist or is not enabled!", ToastLength.Long).Show ();
			}
        }

		protected override void OnStart ()
		{
			base.OnStart ();
			Log.Debug (tag, "OnStart called");
		}

        private void InitMapFragment()
        {
            mapFragment = FragmentManager.FindFragmentByTag("map") as MapFragment;

            if (mapFragment == null)
            {
                GoogleMapOptions mapOptions = new GoogleMapOptions()
					.InvokeMapType(GoogleMap.MapTypeNormal)
					.InvokeZoomControlsEnabled(true)
					.InvokeCompassEnabled(true);
				
                FragmentTransaction fragTx = FragmentManager.BeginTransaction();
                mapFragment = MapFragment.NewInstance(mapOptions);
                fragTx.Add(Resource.Id.map, mapFragment, "map");
                fragTx.Commit();
            }
				
        }

        private void SetupAnimateToButton()
        {
            Button animateButton = FindViewById<Button>(Resource.Id.animateButton);
            animateButton.Click += (sender, e) =>{
				getDuration();				
			};
        }

		private void getDuration()
		{
			string ownlocstring = finisherloc.Latitude.ToString() + "," + finisherloc.Longitude.ToString();
			string truckstring = truck1loc.Latitude.ToString() + "," + truck1loc.Longitude.ToString();
			//animateButton.Text = "Duration: " + getDistanceTo(ownlocstring,truckstring);
			TextView textfield = FindViewById<TextView>(Resource.Id.textView1);
			textfield.Text = "Duration from truck to finisher: " + getDistanceTo(ownlocstring,truckstring) + "s";
		}

		//niet meer nodig
		private void ZoomOnLoc()
		{
			CameraPosition.Builder builder = CameraPosition.InvokeBuilder();
			builder.Target(finisherloc);
			builder.Zoom(14);
			builder.Bearing(0);
			builder.Tilt(0);
			CameraPosition cameraPosition = builder.Build();

			// AnimateCamera provides a smooth, animation effect while moving
			// the camera to the the position.
			map.AnimateCamera(CameraUpdateFactory.NewCameraPosition(cameraPosition));
		}

		private void InitMarkers()
		{
			map = mapFragment.Map;

			BitmapDescriptor truck = BitmapDescriptorFactory.FromResource(Resource.Drawable.truck);

			markertruck.SetPosition(truck1loc);
			markertruck.SetTitle("Truck");
			markertruck.SetIcon(truck);
			map.AddMarker(markertruck);

			//blauw bolletje + zoomfit knopje
			map.MyLocationEnabled = true;
		}

		private void RefreshMarkers()
        {
			map.Clear ();
			markertruck.SetPosition(truck1loc);
			map.AddMarker(markertruck);
        }

		public void OnProviderDisabled (string provider)
		{
			Log.Debug (tag, provider + " disabled by user");
		}
		public void OnProviderEnabled (string provider)
		{
			Log.Debug (tag, provider + " enabled by user");
		}
		public void OnStatusChanged (string provider, Availability status, Bundle extras)
		{
			Log.Debug (tag, provider + " availability has changed to " + status.ToString());
		}

		public int getDistanceTo(string origin, string destination)
		{
			System.Threading.Thread.Sleep(1000);
			int duration = -1;
			string url = "http://maps.googleapis.com/maps/api/directions/json?origin=" + origin + "&destination=" + destination + "&sensor=false";
			string requesturl = url;string content = fileGetJSON(requesturl);
			JObject _Jobj = JObject.Parse(content);
			try
			{
				duration = (int)_Jobj.SelectToken("routes[0].legs[0].duration.value");
				Toast.MakeText (this, duration, ToastLength.Long).Show ();
				return duration;

			}
			catch
			{
				return duration;
			}
		}
		protected string fileGetJSON(string fileName)
		{
			string _sData = string.Empty;
			string me = string.Empty;
			try
			{
				if (fileName.ToLower().IndexOf("http:") > -1)
				{
					System.Net.WebClient wc = new System.Net.WebClient();
					byte[] response = wc.DownloadData(fileName);
					_sData = System.Text.Encoding.ASCII.GetString(response);

				}
				else
				{
					System.IO.StreamReader sr = new System.IO.StreamReader(fileName);
					_sData = sr.ReadToEnd();
					sr.Close();
				}
			}
			catch { _sData = "unable to connect to server "; }
			return _sData;
		}
	}
}

