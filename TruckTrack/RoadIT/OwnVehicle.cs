using Android.App;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.OS;
using Android.Runtime; 
using Android.Locations;
using Android.Util;
using Android.Widget;
using Android.Content;
using Android.Views;
using System.Threading;
using System; 
using System.Collections.Generic; 
using System.Linq; 
using System.Text; 

using Newtonsoft.Json.Linq;
using Org.Eclipse.Paho.Client.Mqttv3;
using Org.Eclipse.Paho.Client.Mqttv3.Persist;
using System.Net.NetworkInformation;

namespace RoadIT
{
	//prevents activity from restarting when screen orientation changes
	[Activity(Label = "OwnVehicle", ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation)]
	public class OwnVehicle : Activity, ILocationListener
	{
		LatLng finisherloc = new LatLng(0, 0);
		GoogleMap map;
		MapFragment mapFragment;
		LocationManager locMgr;
		string ownlocstring;
		string durationString;
		JObject _Jobj;
		public static string broker = "", topicpub="", topicsub="", username="", pass="";
		bool truckbool;

		List<PartnerVehicle> partnerlist = new List<PartnerVehicle>();

		public static MemoryPersistence persistence = new MemoryPersistence();

		public static MqttClient Client;
		bool firstloc = true;
		
		public void OnLocationChanged(Android.Locations.Location location)
		{
			finisherloc = new LatLng(location.Latitude, location.Longitude);
			if (firstloc == true)
			{
				InitMapFragment();
				InitMarkers();
				ZoomOnLoc();
				firstloc = false;
			}
			ownlocstring = location.Latitude.ToString().Replace(",", ".") + "," + location.Longitude.ToString().Replace(",", ".");
			Thread PublishMQTT = new Thread(() => MQTTPublish(ownlocstring));
			PublishMQTT.Start();
		}

		public void MQTTPublish(string content)
		{
			int qos = 2;

			try
			{
				byte[] bytes = System.Text.Encoding.ASCII.GetBytes(content);
				MqttMessage message = new MqttMessage(bytes);
				message.Qos = qos;
				Client.Publish(topicpub, message);
				Log.Debug("MQTTPublish", message.ToString());
			}
			catch (MqttException me)
			{
				me.PrintStackTrace();
			}
		}

		public void MQTTupdate(string mqttmessage, string topic)
		{
			Char delimiter = ',';
			String[] substrings = mqttmessage.Split(delimiter);

			if (substrings.Length == 2) {
				try {
					//TODO todouble kapot nederlands?? punten verdwijnen?
					Log.Debug ("mqttsubstring0", Convert.ToDouble (substrings [0]).ToString ());
					Log.Debug ("mqttsubstring1", Convert.ToDouble (substrings [1]).ToString ());
					//topic for finisher: roadit/truck/name/#
					//topic for truck: roadit/fin/name
					String id = "finisher";
					if (truckbool == false) {
						Char delimitertopic = '/';
						String[] subtopics = topic.Split (delimitertopic);
						id = subtopics [3];
					}
					Console.WriteLine ("id: " + id);
					bool exists = false;
					foreach (PartnerVehicle aPartnerVehicle in partnerlist) {
						if (aPartnerVehicle.getid () == id) {
							exists = true;
							aPartnerVehicle.setLocation (new LatLng (Convert.ToDouble (substrings [0]), Convert.ToDouble (substrings [1])));
							Thread mapAPICall2 = new Thread (() => mapAPICall (aPartnerVehicle));
							mapAPICall2.Start ();
						}
					}
					if (exists == false) {
						partnerlist.Add (new PartnerVehicle (new LatLng (Convert.ToDouble (substrings [0]), Convert.ToDouble (substrings [1])), id));

						Thread PublishMQTT = new Thread(() => MQTTPublish(ownlocstring));
						PublishMQTT.Start();

						Thread mapAPICall3 = new Thread (() => mapAPICall (partnerlist.Find (t => t.getid () == id)));
						mapAPICall3.Start ();
					}
					Log.Debug ("partnerlistelements", partnerlist.Count ().ToString ());
					Log.Debug ("MQTTinput", "Accept");
				} catch {
					Log.Debug("MQTTinput", "input not right");
				}
			} else {
				Log.Debug("MQTTinput", "input not right");
			}
		}

		protected override void OnCreate(Bundle bundle)
		{
			
			base.OnCreate(bundle);
			Log.Debug("Finisher", "OnCreate called");
			string temp = Intent.GetStringExtra ("broker") ?? null;
			broker = "tcp://" + temp + ":1883";
			string name = Intent.GetStringExtra ("name") ?? null;
			string truck = Intent.GetStringExtra("truck") ?? null;
			//string titlestring="";
			if (truck == "true") {
				//titlestring = "Truck";
				topicpub="roadit/truck/"+name+"/"+GetMacAddress();//TODO unieke nummer
				topicsub="roadit/fin/"+name;
				truckbool = true;
			} else {
				topicpub="roadit/fin/"+name;
				topicsub="roadit/truck/"+name+"/#";
				//titlestring = "Finisher";
				truckbool = false;
			}
			TextView title = FindViewById<TextView>(Resource.Id.textView1);

			username = Intent.GetStringExtra ("username") ?? GetMacAddress();
			//username = GetMacAddress();

			//pass = Intent.GetStringExtra ("pass") ?? null;
			Console.WriteLine (broker + " "+ name+ " "+ username+" "+ pass);
			SetContentView(Resource.Layout.Map);
			//indication truck/finisher
			TextView ind = FindViewById<TextView>(Resource.Id.durationText);
			if (truckbool == true)
			{
				ind.Text = "Truck";
			}
			else
			{
				ind.Text = "Finisher";
			}

			// initialize location manager
			locMgr = GetSystemService(Context.LocationService) as LocationManager;

			if (locMgr.AllProviders.Contains(LocationManager.NetworkProvider)
				&& locMgr.IsProviderEnabled(LocationManager.NetworkProvider))
			{
				//TODO
				//ACCURACY OF LOCATIONUPDATE

				//parameters LocationManager.NETWORK_PROVIDER, MIN_TIME, MIN_DISTANCE, mLocationListener);
				//mintime in sec*1000 -> 20s
				//mindistance in meters (float) -> 10m
				locMgr.RequestLocationUpdates(LocationManager.NetworkProvider, 20000, 10, this);
			}
			else {
				Toast.MakeText(this, "Please switch on your location service!", ToastLength.Long).Show();
			}

			InitMapFragment();
			Client = makeClient ();
			Client.SetCallback(new MqttSubscribe(this));
			ConfigMQTT();
		}

		public MqttClient makeClient(){
			return new MqttClient(broker, username, persistence);
		}

		protected override void OnResume()
		{
			base.OnResume();
			Log.Debug("Finisher", "OnResume called");


			Button stopbutton = FindViewById<Button>(Resource.Id.stopbutton);
			stopbutton.Click += (sender, e) =>
			{
				//TODO send kill me signal
				//Client.Disconnect;
				SampleActivity activitysetup = new SampleActivity(1, 2, typeof(MainActivity));
				activitysetup.Start(this);
			};
		}

		public 	void ConfigMQTT()
		{
			try
			{
				Client.Connect();
				Client.Subscribe(topicsub);
				Log.Debug("MqttSubscribe", "connect topic: "+topicsub);
			}
			catch (MqttException me)
			{
				Log.Debug("MqttSubscribe", "(re)connect failed"+ me.ToString());
			}
		}


		protected override void OnStart()
		{
			base.OnStart();
			Log.Debug("Finisher", "OnStart called: ");
		}

		public void InitMapFragment()
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

		void ZoomOnLoc()
		{
			CameraPosition.Builder builder = CameraPosition.InvokeBuilder();
			builder.Target(finisherloc);
			builder.Zoom(12);
			builder.Bearing(0);
			builder.Tilt(0);
			CameraPosition cameraPosition = builder.Build();

			// AnimateCamera provides a smooth, animation effect while moving
			// the camera to the the position.
			map.AnimateCamera(CameraUpdateFactory.NewCameraPosition(cameraPosition));
		}

		public void InitMarkers()
		{
			map = mapFragment.Map;
			map.MyLocationEnabled = true;
		}

		public void OnProviderDisabled(string provider)
		{
			Log.Debug("Finisher", provider + " disabled by user");
		}

		public void OnProviderEnabled(string provider)
		{
			Log.Debug("Finisher", provider + " enabled by user");
		}

		public void OnStatusChanged(string provider, Availability status, Bundle extras)
		{
			Log.Debug("Finisher", provider + " availability has changed to " + status.ToString());
		}

		public void getDuration(PartnerVehicle partnervehicle)
		{
			int dur = getDistanceTo();
			int min = int.MaxValue;

			partnervehicle.setDur(dur);

			//TODO CLEANUP

			Log.Debug("durationmin", min.ToString());
			Log.Debug("durationdur", dur.ToString());

			//set nearest to false for every truck
			foreach (PartnerVehicle aPartnerVehicle in partnerlist)
			{
				aPartnerVehicle.setNearest(false);
			}

			//sort on duration -> nearest truck first in list
			partnerlist.Sort((x, y) => x.getDur().CompareTo(y.getDur()));
			partnerlist.First().setNearest(true);

			////redraw
			//foreach (Truck aTruck2 in trucklist)
			//{
			//	//truck was previously the nearest-> has to be redrawn in original color
			//	if (aTruck2.getNearest() == false && aTruck2.getToReDraw() == true)
			//	{
			//		aTruck2.setToReDraw(false);
			//		Thread drawRouteThread2 = new Thread(() => drawRoute(aTruck2));
			//		drawRouteThread2.Start();
			//	}
			//}

			////true for first element
			//trucklist.First().setToReDraw(true);

			if (truckbool == false)
			{
				durationString = "ETA of nearest truck: " + partnerlist.First().getDur() + "s";
			}
			else
			{
				durationString = "ETA at finisher: " + partnerlist.First().getDur() + "s";
			}

			
			TextView durationtextfield = FindViewById<TextView>(Resource.Id.durationText);

			//update textfield in main UI thread
			RunOnUiThread(() => durationtextfield.Text = durationString);

			Thread drawRouteThread = new Thread(() => drawRoute(partnervehicle));
			drawRouteThread.Start();
		}

		public void updateUI()
		{
			BitmapDescriptor truck = BitmapDescriptorFactory.FromResource(Resource.Drawable.truck);
			BitmapDescriptor finisher = BitmapDescriptorFactory.FromResource(Resource.Drawable.finisher);
			map.Clear();

			//temp variable -> no chance of changes in foreach
			List<PartnerVehicle> listtemp = partnerlist;

			Log.Debug("updateUI", "list of partners");

			foreach (PartnerVehicle aPartnerVehicle in listtemp)
			{
				aPartnerVehicle.display();
				map.AddPolyline(aPartnerVehicle.getPolylineOptions());
				MarkerOptions markerpartner = new MarkerOptions();
				markerpartner.SetPosition(aPartnerVehicle.getLocation());
				if (truckbool == false)
				{
					markerpartner.SetIcon(truck);
					markerpartner.SetTitle("Truck " + aPartnerVehicle.getid() + " arrives in: " + aPartnerVehicle.getDur() + "s");
				}
				else
				{
					markerpartner.SetIcon(finisher);
					markerpartner.SetTitle("Arriving at Finisher" + aPartnerVehicle.getid() + "in: " + aPartnerVehicle.getDur() + "s");
				}

				map.AddMarker(markerpartner);
			}
		}

		public int getDistanceTo()
		{
			//System.Threading.Thread.Sleep(50);

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

		void mapAPICall(PartnerVehicle partnervehicle)
		{
			string origin = partnervehicle.getlocstring();
			try
			{	
				string url = "http://maps.googleapis.com/maps/api/directions/json?origin=" + origin + "&destination=" + ownlocstring + "&sensor=false";
				string requesturl = url; string content = fileGetJSON(requesturl);;
				_Jobj = JObject.Parse(content);
				Log.Debug("apicall", content.ToString());
			}
			catch { }

			Thread durationThread = new Thread(() => getDuration(partnervehicle));
			durationThread.Start();
		}

		void drawRoute(PartnerVehicle partnervehicle)
		{
			Log.Debug("http", "drawroutestart");

			PolylineOptions temppoly = new PolylineOptions();

			if (truckbool == false)
			{
				//variable colours for different trucks

				//green for nearest truck
				if (partnervehicle.getNearest() == true)
				{
					Log.Debug("drawroute", "nearest");
					temppoly.InvokeColor(0x6600cc00);
				}
				else if (partnervehicle.getcolor() == "blue")
				{
					Log.Debug("drawroute", partnervehicle.getid() + partnervehicle.getcolor() + "moet blue zijn");
					temppoly.InvokeColor(0x66000099);
				}
				else if (partnervehicle.getcolor() == "red")
				{
					Log.Debug("drawroute", partnervehicle.getid() + partnervehicle.getcolor() + "moet red zijn");
					temppoly.InvokeColor(0x66ff0000);
				}
				else if (partnervehicle.getcolor() == "black")
				{
					Log.Debug("drawroute", partnervehicle.getid() + partnervehicle.getcolor() + "moet black zijn");
					temppoly.InvokeColor(0x66000000);
				}
				else if (partnervehicle.getcolor() == "purple")
				{
					Log.Debug("drawroute", partnervehicle.getid() + partnervehicle.getcolor() + "moet purple zijn");
					temppoly.InvokeColor(0x669933ff);
				}
				else
				{
					//blue
					Log.Debug("drawroute", "else");
					temppoly.InvokeColor(0x66000099);
				}
			}

			else
			{
				//route from truck to finisher in blue
				temppoly.InvokeColor(0x66000099);
			}

			temppoly.InvokeWidth(13);

			try
			{
				string polyPoints;
				polyPoints = (string)_Jobj.SelectToken("routes[0].overview_polyline.points");
				List<LatLng> drawCoordinates;
				drawCoordinates = DecodePolylinePoints(polyPoints);
				foreach (var position in drawCoordinates)
				{
					temppoly.Add(new LatLng(position.Latitude, position.Longitude));
				}
				partnervehicle.setPolylineOptions(temppoly);
			}
			catch
			{}

			//draw route in main UI thread
			RunOnUiThread(() => updateUI());
		}

		List<LatLng> DecodePolylinePoints(string encodedPoints)
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
				Log.Debug("Polyline", ex.ToString());// logo it
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
		public string GetMacAddress()
		{
			foreach (var netInterface in NetworkInterface.GetAllNetworkInterfaces()) 
			{
				if (netInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
					netInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet) 
				{
					var address = netInterface.GetPhysicalAddress();
					return BitConverter.ToString(address.GetAddressBytes());

				}
			}

			return "NoMac";
		}
	}
}