using System;
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
//using System.Json;
using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
//using Java.Lang;

using Org.Eclipse.Paho.Client.Mqttv3;
using Org.Eclipse.Paho.Client.Mqttv3.Internal;
using Org.Eclipse.Paho.Client.Mqttv3.Logging;
using Org.Eclipse.Paho.Client.Mqttv3.Persist;
using Org.Eclipse.Paho.Client.Mqttv3.Util;

namespace RoadIT
{
	[Activity(Label = "Truck")]
	public class Truck : Activity, ILocationListener
	{
		static LatLng finisherloc = new LatLng(51.229241, 4.404648);
		static LatLng cineloc = new LatLng(51.2354242, 4.4105663);
		static LatLng truckloc = new LatLng(0, 0);
		GoogleMap map;
		MapFragment mapFragment;
		LocationManager locMgr;
		string ownlocstring;
		static string finisherstring;
		string cinestring;
		static string varloc = "";
		string durationString;
		JObject _Jobj;
		string tag = "MainActivity";
		PolylineOptions polylineOptions = new PolylineOptions();

		MarkerOptions markerfinisher = new MarkerOptions();
		MarkerOptions markertruck = new MarkerOptions();
		public static string broker = "tcp://iot.eclipse.org:1883";
		public static string clientId = "JavaSample";

		public static MemoryPersistence persistence = new MemoryPersistence();
		public static MqttClient Client = new MqttClient(broker, clientId, persistence);

		bool firstloc = true;

		public static void MQTTupdate(string mqttmessage){
			Char delimiter = ',';
			String[] substrings = mqttmessage.Split(delimiter);
			if (substrings.Length == 3) {
					try{
					if (Convert.ToDouble (substrings [2]) == 0) {
						finisherloc.Latitude = Convert.ToDouble (substrings [0]);
						finisherloc.Longitude = Convert.ToDouble (substrings [1]);
						finisherstring = mqttmessage;
						Log.Debug ("MQTTinput", "Accept");
					}
					} 
					catch{
						Log.Debug ("MQTTinput", "input not right");
					}

			}
		}

		public void OnLocationChanged(Android.Locations.Location location)
		{
			//Toast.MakeText(this, "Location changed", ToastLength.Long).Show();
			truckloc = new LatLng(location.Latitude, location.Longitude);
			if (firstloc == true)
			{
				InitMarkers();
				ZoomOnLoc();
				locsToString();
				firstloc = false;
			}
			locsToString();
			RefreshMarkers();

			Thread MapsAPICallThread = new Thread(() => mapAPICall(finisherstring, "red"));
			MapsAPICallThread.Start();


			////multithreaded method call, prevents app stutters
			//ThreadStart getDurationThreadStart = new ThreadStart(getDuration);
			//Thread getDurationThread = new Thread(getDurationThreadStart);
			//getDurationThread.Start();

			////ThreadStart drawRouteThreadStart = new ThreadStart(drawRoute(ownlocstring,truckstring));
			//Thread drawRouteThread = new Thread(() => drawRoute(finisherstring, "red"));
			//drawRouteThread.Start();
			//MQTTPublish (location.Latitude.ToString () + "," + location.Longitude.ToString () + ",1");

		}

		public void MQTTPublish(string content) {

			string topic        = "fin";
			int qos             = 2;
			MemoryPersistence persistence = new MemoryPersistence();

			try {
				byte[] bytes =  System.Text.Encoding.ASCII.GetBytes(content);
				MqttMessage message = new MqttMessage(bytes);
				message.Qos=qos;
				Client.Publish(topic, message);
				Log.Debug("MQTTPublish", message.ToString());
			} catch(MqttException me) {
				me.PrintStackTrace();
			}
		}

		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);
			Log.Debug(tag, "OnCreate called");
			SetContentView(Resource.Layout.Main);
			InitMapFragment();
			//SetupAnimateToButton();
			Client.SetCallback(new MqttSubscribe());
			ConfigMQTT();
		}

		protected override void OnResume()
		{
			base.OnResume();
			Log.Debug(tag, "OnResume called");

			// initialize location manager
			locMgr = GetSystemService(Context.LocationService) as LocationManager;

			// pass in the provider (GPS),
			// the minimum time between updates (in seconds),
			// the minimum distance the user needs to move to generate an update (in meters),
			// and an ILocationListener (recall that this class impletents the ILocationListener interface)
			if (locMgr.AllProviders.Contains(LocationManager.NetworkProvider)
				&& locMgr.IsProviderEnabled(LocationManager.NetworkProvider))
			{
				locMgr.RequestLocationUpdates(LocationManager.NetworkProvider, 2000, 1, this);
			}
			else {
				Toast.MakeText(this, "The Network Provider does not exist or is not enabled!", ToastLength.Long).Show();
			}
		}

		public static void ConfigMQTT()
		{
			try
			{
				Client.Connect();
				Client.Subscribe("fin");
				Log.Debug("MqttSubscribe", "connect");
				//Toast.MakeText(this, "Subscribe(\"fin\")!", ToastLength.Long).Show();

			}
			catch (MqttException me)
			{
				Log.Debug("MqttSubscribe", "(re)connect failed: "+me);
				//Toast.MakeText(this, "Error: Subscribe(\"fin\")!\n" + me, ToastLength.Long).Show();

			}
		}


		protected override void OnStart()
		{
			base.OnStart();
			Log.Debug(tag, "OnStart called");
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
			Button RouteButton = FindViewById<Button>(Resource.Id.routeButton);
			RouteButton.Click += (sender, e) =>
			{
				Thread mapAPICall2 = new Thread(() => mapAPICall(varloc, "blue"));
				mapAPICall2.Start();
			};
		}

		//niet meer nodig
		private void ZoomOnLoc()
		{
			CameraPosition.Builder builder = CameraPosition.InvokeBuilder();
			builder.Target(truckloc);
			builder.Zoom(12);
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
			BitmapDescriptor truck = BitmapDescriptorFactory.FromResource(Resource.Drawable.finisher);
			markertruck.SetPosition(truckloc);
			markertruck.SetTitle("Truck");
			markertruck.SetIcon(truck);
			map.AddMarker(markertruck);

			//blue location
			map.MyLocationEnabled = true;
		}

		private void RefreshMarkers()
		{
			map.Clear();
			markertruck.SetPosition(finisherloc);
			map.AddMarker(markertruck);
		}

		public void OnProviderDisabled(string provider)
		{
			Log.Debug(tag, provider + " disabled by user");
		}
		public void OnProviderEnabled(string provider)
		{
			Log.Debug(tag, provider + " enabled by user");
		}
		public void OnStatusChanged(string provider, Availability status, Bundle extras)
		{
			Log.Debug(tag, provider + " availability has changed to " + status.ToString());
		}

		private void locsToString()
		{
			ownlocstring = truckloc.Latitude.ToString() + "," + truckloc.Longitude.ToString();
			finisherstring = finisherloc.Latitude.ToString() + "," + finisherloc.Longitude.ToString();
			cinestring = cineloc.Latitude.ToString() + "," + cineloc.Longitude.ToString();
		}

		private void getDuration()
		{
			//animateButton.Text = "Duration: " + getDistanceTo(ownlocstring,finisherstring);
			durationString = "Duration to finisher: " + getDistanceTo(ownlocstring, finisherstring) + "s";

			TextView durationtextfield = FindViewById<TextView>(Resource.Id.durationText);

			//update textfield in main UI thread
			RunOnUiThread(() => durationtextfield.Text = durationString);
		}

		public int getDistanceTo(string origin, string destination)
		{
			System.Threading.Thread.Sleep(50);

			int duration = -1;
			try
			{
				duration = (int)_Jobj.SelectToken("routes[0].legs[0].duration.value");
				return duration;
			}
			catch
			{
				return duration;
			}
		}

		void updateUI()
		{
			map.Clear();
			map.AddMarker(markertruck);
			map.AddPolyline(polylineOptions);
		}

		void mapAPICall(string destination, string color)
		{
			System.Threading.Thread.Sleep(50);

			try
			{
				string url = "http://maps.googleapis.com/maps/api/directions/json?origin=" + ownlocstring + "&destination=" + destination + "&sensor=false";
				string requesturl = url; string content = fileGetJSON(requesturl);
				//Log.Debug("httpcontent", content);
				_Jobj = JObject.Parse(content);
			}
			catch { }

			getDuration();
			drawRoute(color);
			//draw route in main UI thread
			RunOnUiThread(() => updateUI());
		}

		void drawRoute(string color)
		{
			Log.Debug("http", "drawroutestart");

			//TODO zorgen dat andere routes niet weggaa
			polylineOptions = new PolylineOptions();
			if (color == "blue")
			{
				polylineOptions.InvokeColor(0x66000099);
			}
			else if (color == "red")
			{
				polylineOptions.InvokeColor(0x66ff0000);
			}
			else
			{
				polylineOptions.InvokeColor(0x66000099);
			}

			polylineOptions.InvokeWidth(9);
			try
			{
				string polyPoints = (string)_Jobj.SelectToken("routes[0].overview_polyline.points");
				List<LatLng> drawCoordinates;
				drawCoordinates = DecodePolylinePoints(polyPoints);
				foreach (var position in drawCoordinates)
				{
					polylineOptions.Add(new LatLng(position.Latitude, position.Longitude));
				}
			}
			catch
			{ }


		}

		private List<LatLng> DecodePolylinePoints(string encodedPoints)
		{
			if (encodedPoints == null || encodedPoints == "") return null;
			List<LatLng> poly = new List<LatLng>();
			char[] polylinechars = encodedPoints.ToCharArray();
			int index = 0;

			int currentLat = 0;
			int currentLng = 0;
			int next5bits;
			int sum;
			int shifter;

			try
			{
				while (index < polylinechars.Length)
				{
					// calculate next latitude
					sum = 0;
					shifter = 0;
					do
					{
						next5bits = (int)polylinechars[index++] - 63;
						sum |= (next5bits & 31) << shifter;
						shifter += 5;
					} while (next5bits >= 32 && index < polylinechars.Length);

					if (index >= polylinechars.Length)
						break;

					currentLat += (sum & 1) == 1 ? ~(sum >> 1) : (sum >> 1);

					//calculate next longitude
					sum = 0;
					shifter = 0;
					do
					{
						next5bits = (int)polylinechars[index++] - 63;
						sum |= (next5bits & 31) << shifter;
						shifter += 5;
					} while (next5bits >= 32 && index < polylinechars.Length);

					if (index >= polylinechars.Length && next5bits >= 32)
						break;

					currentLng += (sum & 1) == 1 ? ~(sum >> 1) : (sum >> 1);

					double latdouble = Convert.ToDouble(currentLat) / 100000.0;
					double lngdouble = Convert.ToDouble(currentLng) / 100000.0;
					LatLng p = new LatLng(latdouble, lngdouble);
					poly.Add(p);
				}
			}
			catch (Exception ex)
			{
				// logo it
				Log.Debug("Main","Error: " + ex.ToString());
			}
			return poly;
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

