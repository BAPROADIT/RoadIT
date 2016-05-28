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
using Android.Support.V4.App;
using Android.Graphics;

using Newtonsoft.Json.Linq;
using Org.Eclipse.Paho.Client.Mqttv3;
using Org.Eclipse.Paho.Client.Mqttv3.Persist;
using System.Net.NetworkInformation;

namespace RoadIT
{
	/**
	 * OwnVehicle is the most important activity when the app is fully started. 
	 * If the use choses finisher, this class is a finisher and otherwise it is a truck.
	 * The layout consists mainly of a map showing the partnervehicles and an indicator of the time it takes for the nearest truck to arrive.
	 * This class holds a list of partnervehicles, of which it checks the duration and route.
	 */
	//prevents activity from restarting when screen orientation changes
	[Activity(Label = "OwnVehicle", ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation)]
	public class OwnVehicle : Activity, ILocationListener
	{
		LatLng ownloc = new LatLng(0, 0);
		GoogleMap map;
		MapFragment mapFragment;
		LocationManager locMgr;
		string ownlocstring;
		string durationString;
		JObject _Jobj;
		public static string broker = "", topicpub="", topicsub="", username="", pass="";
		bool truckbool;
		String id;
		int partnerduration;
		float Load=0, Speed=0, LoadPerMeter;
		string suggestString;
		SeekBar SliderLoad;
		TextView textViewLoad;
		SeekBar SliderSpeed;
		TextView textViewSpeed;
		TextView textViewMovefasterslower;
		Button stopbutton;
		NotificationManagerCompat notificationManager;
		NotificationCompat.Builder builder;
		Notification notification;
		public static MemoryPersistence persistence = new MemoryPersistence();
		public static MqttClient Client;
		bool firstloc = true;
		int casenotification = 0;
		//quality of service 2 to make sure message arrives
		int qos = 2;

		//list of PartnerVehicles
		List<PartnerVehicle> partnerlist = new List<PartnerVehicle>();

		//oncreate is called when the activity starts and initialises what needs to be initialised.
		protected override void OnCreate(Bundle bundle)
		{
			Log.Debug("Finisher", "OnCreate called");
			base.OnCreate(bundle);

			//init notification
			notificationManager = NotificationManagerCompat.From(this);

			LoadPerMeter = float.Parse(Intent.GetStringExtra("loadpermeter"));

			//broker IP UA broker
			string temp = "146.175.139.65";
			temp="nasdenys.synology.me";
			broker = "tcp://" + temp + ":1883";
			string name = Intent.GetStringExtra("name") ?? null;
			string truck = Intent.GetStringExtra("truck") ?? null;

			//init topics for truck/finisher
			if (truck == "true")
			{
				topicpub = "roadit/truck/" + name + "/" + GetMacAddress();
				topicsub = "roadit/fin/" + name;
				truckbool = true;
			}
			else {
				topicpub = "roadit/fin/" + name;
				topicsub = "roadit/truck/" + name + "/#";
				truckbool = false;
			}

			//get MAC adres, which is used as a unique ID
			username = Intent.GetStringExtra("username") ?? GetMacAddress();

			//pass = Intent.GetStringExtra ("pass") ?? null;
			Console.WriteLine(broker + " " + name + " " + username + " " + pass);

			//set layout
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
				/**
				 * ACCURACY OF LOCATIONUPDATE
				 * parameters LocationManager.NETWORK_PROVIDER, MIN_TIME, MIN_DISTANCE, mLocationListener)
				 * mintime in sec*1000 -> 20s 
				 * mindistance in meters (float) -> 20m
				 */
				locMgr.RequestLocationUpdates(LocationManager.NetworkProvider, 1, 1, this);
			}
			else {
				Toast.MakeText(this, "Please switch on your location service!", ToastLength.Long).Show();
			}

			//init map
			InitMapFragment();

			//init MQTT
			Client = makeClient();
			Client.SetCallback(new MqttSubscribe(this));
			ConfigMQTT();

			//get seekbar values
			SliderLoad = FindViewById<SeekBar>(Resource.Id.seekBarLoad);
			textViewLoad = FindViewById<TextView>(Resource.Id.textViewLoad);
			SliderSpeed = FindViewById<SeekBar>(Resource.Id.seekBarSpeed);
			textViewSpeed = FindViewById<TextView>(Resource.Id.textViewSpeed);
			textViewMovefasterslower = FindViewById<TextView>(Resource.Id.textViewMovefasterslower);

			//start thread to simulate load emptying
			Thread thread1 = new Thread(new ThreadStart(eatLoad));
			thread1.Start();

			//init stopbutton, when pressed kill the activity and kill MQTT services
			stopbutton = FindViewById<Button>(Resource.Id.stopbutton);
			stopbutton.Click += (sender, e) =>
			{
				Kill();
			};

			//update sliders
			SliderLoad.ProgressChanged += delegate (object sender, SeekBar.ProgressChangedEventArgs e)
			{
				Load = e.Progress;
				Load = Load / 1000;
				updateSeekbars();
			};
			SliderSpeed.ProgressChanged += delegate (object sender, SeekBar.ProgressChangedEventArgs e)
			{
				Speed = e.Progress;
				Speed = Speed / 100;
				updateSeekbars();
			};
			//init sliders
			SliderLoad.Progress = 100000;
			SliderSpeed.Progress = 20;
		}

		//onresume is called after oncreate and whenever the user reopens the app if it is running in the background
		protected override void OnResume()
		{
			base.OnResume();
			Log.Debug("Finisher", "OnResume called");
		}

		protected override void OnStart()
		{
			base.OnStart();
			Log.Debug("Finisher", "OnStart called: ");
		}

		//called whenever location is changed
		public void OnLocationChanged(Android.Locations.Location location)
		{
			//own location is updated
			ownloc = new LatLng(location.Latitude, location.Longitude);

			//When location is initialized an gps position is found, init markers and map
			if (firstloc == true)
			{
				InitMapFragment();
				InitMarkers();
				ZoomOnLoc();
				firstloc = false;
			}

			//loc to string
			ownlocstring = location.Latitude.ToString().Replace(",", ".") + "," + location.Longitude.ToString().Replace(",", ".");

			//publish own location in a thread to prevent stutters
			Thread PublishMQTT = new Thread(() => MQTTPublish(ownlocstring));
			PublishMQTT.Start();

			//truck needs to update its route and duration when its location changes
			if (truckbool == true)
			{
				foreach (PartnerVehicle aPartnerVehicle in partnerlist)
				{
					if (aPartnerVehicle.getid() == id)
					{
						Thread mapAPICall2 = new Thread(() => mapAPICall(aPartnerVehicle));
						mapAPICall2.Start();
					}
				}
			}
		}

		//create notification for phone and android wear
		private void CreateNotification(Intent intent)
		{
			var style = new NotificationCompat.BigTextStyle().BigText(durationString);

			//wearable notification
			var wearableExtender = new NotificationCompat.WearableExtender()
				.SetBackground(BitmapFactory.DecodeResource(this.Resources, Resource.Drawable.trucktrackbackground));

			// Instantiate the builder and set notification elements:
			builder = new NotificationCompat.Builder(this)
				.SetContentTitle(suggestString)
				.SetContentText(durationString)
				.SetSmallIcon(Resource.Drawable.trucktrackicon)
				.SetStyle(style)
				//2 for max priority
				.SetPriority(2)
				//2 for vibration
				.SetDefaults(2)
				//0x1 for sound
				.SetDefaults(0x1)
				//enable wearable extender
				.Extend(wearableExtender);

			// Build the notification:
			notification = builder.Build();

			// Publish the notification:
			const int notificationId = 0;
			notificationManager.Notify(notificationId, notification);

		}

		//kill the activity and start setup activity, disconnect MQTT client
		public void Kill()
		{
			//killsignal, remove me from list
			Thread PublishMQTT = new Thread(() => MQTTPublish("killme"));
			PublishMQTT.Start();

			//start setupactivity
			SampleActivity activitysetup = new SampleActivity(1, 2, typeof(MainActivity));
			activitysetup.Start(this);
			if (truckbool == true)
			{
				Toast.MakeText(this, "Truck stopped. Finisher will not receive updates. ", ToastLength.Long).Show();
			}
			else
			{
				Toast.MakeText(this, "Finisher stopped.", ToastLength.Long).Show();
			}

			try
			{
				Client.Disconnect();
			}
			catch { }

			//stop this activity
			this.FinishActivity(1);
		}

		//thread to simulate the depleting of the finisher's load
		public  void eatLoad()
		{
			//if truck remove sliders
			if (truckbool == true) {
				SliderLoad.Visibility = ViewStates.Gone;
				SliderSpeed.Visibility = ViewStates.Gone;
				textViewLoad.Visibility = ViewStates.Gone;
				textViewSpeed.Visibility = ViewStates.Gone;
				textViewMovefasterslower.Visibility = ViewStates.Gone;
			} 
			else 
			{
				while (true)
				{	//wait a sec
					Thread.Sleep (1000);
					//reduce load
					if (Load > 0) {
						Load = Load - (Speed * LoadPerMeter);
						//update sliders
						RunOnUiThread (() => {
							this.updateSeekbars ();
						});
					}
					//calculate recommend speed
					float meterToGo = Load / LoadPerMeter;
					float timeToGo = meterToGo / Speed;
					float recommendSpeed = meterToGo / partnerduration;
					//temp variable to check if case is changed so notification can be sent
					int prevcasenotification = casenotification;
					if (timeToGo > partnerduration) {
					casenotification = 1;
						//Move faster
						suggestString = "You can go faster, " + recommendSpeed.ToString ("0.00") + "m/s";
					} else if (timeToGo < partnerduration) {
							//Move Slower
							casenotification = 2;
							suggestString = "Move slower, go " + recommendSpeed.ToString ("0.00") + "m/s";

					} else {
						casenotification = 3;
						//Go on
						suggestString = "Correct speed.";
					}
					
					//check for changes
					if (casenotification != prevcasenotification && recommendSpeed.ToString("0.00") != "Infinity") {
							CreateNotification (this.Intent);
					}
				}
			}
		}

		//update the notification without vibrating/sound, this way the duration showed is always up to date
		private void UpdateNotification()
		{
			var style = new NotificationCompat.BigTextStyle().BigText(durationString);

			//wearable notification
			var wearableExtender = new NotificationCompat.WearableExtender()
				.SetBackground(BitmapFactory.DecodeResource(this.Resources, Resource.Drawable.trucktrackbackground));

			// Instantiate the builder and set notification elements:
			builder = new NotificationCompat.Builder(this)
				.SetContentTitle(suggestString)
				.SetContentText(durationString)
				.SetSmallIcon(Resource.Drawable.trucktrackicon)
				.SetStyle(style)
				.Extend(wearableExtender);

			// Build the notification:
			notification = builder.Build();

			// Publish the notification:
			const int notificationId = 0;
			notificationManager.Notify(notificationId, notification);
		}

		//Connecting to MQTT client
		public void ConfigMQTT()
		{
			try
			{
				Client.Connect();
				Client.Subscribe(topicsub);
				Log.Debug("MqttSubscribe", "connect topic: " + topicsub);
			}
			catch (MqttException me)
			{
				Log.Debug("MqttSubscribe", "(re)connect failed" + me.ToString());
			}
		}

		//publish any message
		public void MQTTPublish(string content)
		{
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

		public MqttClient makeClient(){
			return new MqttClient(broker, username, persistence);
		}

		//Decipher the incoming message and take the right action
		public void MQTTupdate(string mqttmessage, string topic)
		{
			Char delimiter = ',';
			String[] substrings = mqttmessage.Split(delimiter);

			//if killme remove this vehicle from the list
			if (mqttmessage == "killme")
			{
				if (truckbool == false)
				{
					Char delimitertopic = '/';
					String[] subtopics = topic.Split(delimitertopic);
					id = subtopics[3];
				}

				//iterate list in reverse to remove partnervehicle
				for (int i = partnerlist.Count - 1; i >= 0; i--)
				{
					bool exists = false;
					if (partnerlist[i].getid() == id && exists == false)
					{
						exists = true;
						partnerlist.RemoveAt(i);
						RunOnUiThread(() => updateUI());
					}
				}
			}

			else if (substrings.Length == 2)
			{
				try
				{
					//split string of message
					Log.Debug("mqttsubstring0", Convert.ToDouble(substrings[0]).ToString());
					Log.Debug("mqttsubstring1", Convert.ToDouble(substrings[1]).ToString());
					//topic for finisher: roadit/truck/name/#
					//topic for truck: roadit/fin/name
					if (truckbool == false)
					{
						Char delimitertopic = '/';
						String[] subtopics = topic.Split(delimitertopic);
						id = subtopics[3];
					}
					Console.WriteLine("id: " + id);
					bool exists = false;
					foreach (PartnerVehicle aPartnerVehicle in partnerlist)
					{
						if (aPartnerVehicle.getid() == id)
						{
							//if vehicle exists, set its location to new update location + new API call to update on map + duration
							exists = true;
							aPartnerVehicle.setLocation(new LatLng(Convert.ToDouble(substrings[0]), Convert.ToDouble(substrings[1])));
							Thread mapAPICall2 = new Thread(() => mapAPICall(aPartnerVehicle));
							mapAPICall2.Start();
						}
					}
					if (exists == false)
					{
						//if new vehicle add to list of vehicles
						partnerlist.Add(new PartnerVehicle(new LatLng(Convert.ToDouble(substrings[0]), Convert.ToDouble(substrings[1])), id));

						//publish own location to make sure new vehicle knows where you are
						Thread PublishMQTT = new Thread(() => MQTTPublish(ownlocstring));
						PublishMQTT.Start();

						//API call to update on map + duration
						Thread mapAPICall3 = new Thread(() => mapAPICall(partnerlist.Find(t => t.getid() == id)));
						mapAPICall3.Start();
					}

					Log.Debug("partnerlistelements", partnerlist.Count().ToString());
					Log.Debug("MQTTinput", "Accept");
				}
				catch
				{
					Log.Debug("MQTTinput", "input not right");
				}
			}
			else {
				Log.Debug("MQTTinput", "input not right");
			}
		}

		//init the map and set its parameters
		public void InitMapFragment()
		{
			mapFragment = FragmentManager.FindFragmentByTag("map") as MapFragment;

			if (mapFragment == null)
			{
				GoogleMapOptions mapOptions = new GoogleMapOptions()
					.InvokeMapType(GoogleMap.MapTypeNormal)
					.InvokeZoomControlsEnabled(true)
					.InvokeCompassEnabled(true);
				
				Android.App.FragmentTransaction fragTx = FragmentManager.BeginTransaction();
				mapFragment = MapFragment.NewInstance(mapOptions);
				fragTx.Add(Resource.Id.map, mapFragment, "map");
					fragTx.Commit();
			}

		}

		//zoom on own location with animation
		void ZoomOnLoc()
		{
			CameraPosition.Builder builder = CameraPosition.InvokeBuilder();
			builder.Target(ownloc);
			builder.Zoom(12);
			builder.Bearing(0);
			builder.Tilt(0);
			CameraPosition cameraPosition = builder.Build();

			// AnimateCamera provides a smooth, animation effect while moving
			// the camera to the the position.
			map.AnimateCamera(CameraUpdateFactory.NewCameraPosition(cameraPosition));
		}

		//init map
		public void InitMarkers()
		{
			map = mapFragment.Map;
			map.MyLocationEnabled = true;
			map.BuildingsEnabled = true;
		}

		//update the values of the seekbars
		public void updateSeekbars(){
			textViewLoad.Text = "Load: "+Load.ToString("0.000")+"%";
			textViewSpeed.Text = "Speed: "+Speed.ToString("0.000") + "m/s";
			SliderLoad.Progress = (int)( Load*1000);
			SliderSpeed.Progress = (int)(Speed * 100);
			if (partnerduration != 0) {
				textViewMovefasterslower.Text = suggestString;
			}
		}	

		//API call to google->gets a JSON packet which is deciphered
		void mapAPICall(PartnerVehicle partnervehicle)
		{
			string placeofvehicle = partnervehicle.getlocstring();
			try
			{
				string url = "";
				if (truckbool == false)
				{
					//route from trucks place to own finishers location
					url = "http://maps.googleapis.com/maps/api/directions/json?origin=" + placeofvehicle + "&destination=" + ownlocstring + "&sensor=false";
				}
				else
				{
					//route from own trucks place to finishers location
					url = "http://maps.googleapis.com/maps/api/directions/json?origin=" + ownlocstring + "&destination=" + placeofvehicle + "&sensor=false";
				}
				
				string requesturl = url; string content = fileGetJSON(requesturl);;
				_Jobj = JObject.Parse(content);
				Log.Debug("apicall", content.ToString());
			}
			catch { }

			//start durationthread
			Thread durationThread = new Thread(() => getDuration(partnervehicle));
			durationThread.Start();
		}

		public void getDuration(PartnerVehicle partnervehicle)
		{
			//get duration from JSON
			int dur = getDistanceTo();
			int min = int.MaxValue;

			//set duration of partnervehicle
			partnervehicle.setDur(dur);

			Log.Debug("durationmin", min.ToString());
			Log.Debug("durationdur", dur.ToString());

			//set nearest to false for every truck
			foreach (PartnerVehicle aPartnerVehicle in partnerlist)
			{
				aPartnerVehicle.setNearest(false);
			}

			//temp value so change of nearest truck can easily be found
			PartnerVehicle test = partnerlist.First();

			//sort on duration -> nearest truck first in list
			partnerlist.Sort((x, y) => x.getDur().CompareTo(y.getDur()));
			partnerlist.First().setNearest(true);

			//check if new truck is newest truck, if yes update previous nearest truck so its route is not green
			if (test != partnerlist.First())
			{
				Thread mapAPICall3 = new Thread(() => mapAPICall(test));
				mapAPICall3.Start();
			}

			//seconds to TimeSpan
			TimeSpan t = TimeSpan.FromSeconds(partnerlist.First().getDur());

			//time to hours/minutes/seconds
			string time;
			partnerduration = partnerlist.First().getDur();
			if (partnerduration <= 60)
			{
				time = t.ToString(@"ss") + "s";
			}
			else if (partnerduration <= 3600)
			{
				time = t.ToString(@"mm\mss") + "s";
			}
			else
			{
				time = t.ToString(@"hh\hmm\mss") + "s";
			}

			if (truckbool == false)
			{
				durationString = "ETA of nearest truck: " + time;

			}
			else
			{
				durationString = "ETA at finisher: " + time;
			}

			TextView durationtextfield = FindViewById<TextView>(Resource.Id.durationText);

			//update textfield in main UI thread
			RunOnUiThread(() => durationtextfield.Text = durationString);

			//draw the route
			Thread drawRouteThread = new Thread(() => drawRoute(partnervehicle));
			drawRouteThread.Start();

			//update notification so duration stays updated
			UpdateNotification();
		}

		//get duration from json object
		public int getDistanceTo()
		{
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

		//draw route from polylines in json object
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

				//other colors for normal route
				else if (partnervehicle.getcolor() == "blue")
				{
					temppoly.InvokeColor(0x66000099);
				}
				else if (partnervehicle.getcolor() == "red")
				{
					temppoly.InvokeColor(0x66ff0000);
				}
				else if (partnervehicle.getcolor() == "black")
				{
					temppoly.InvokeColor(0x66000000);
				}
				else if (partnervehicle.getcolor() == "purple")
				{
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

			//setwidth 
			temppoly.InvokeWidth(13);

			//get polypoints from json object
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

		//updates the markers and route
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
				//add polylines from all vehicles to map
				map.AddPolyline(aPartnerVehicle.getPolylineOptions());
				//add markers and set positions
				MarkerOptions markerpartner = new MarkerOptions();
				markerpartner.SetPosition(aPartnerVehicle.getLocation());
				if (truckbool == false)
				{
					//truckmarker
					markerpartner.SetIcon(truck);
					markerpartner.SetTitle("Truck " + aPartnerVehicle.getid() + " arrives in: " + aPartnerVehicle.getDur() + "s");
				}
				else
				{
					//finisher marker
					markerpartner.SetIcon(finisher);
					markerpartner.SetTitle("Arriving at Finisher " + aPartnerVehicle.getid() + "in: " + aPartnerVehicle.getDur() + "s");
				}
				map.AddMarker(markerpartner);
			}
		}

		//gets mac adres from device which is used as a unique ID
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

		//decode polypoints from json object
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

		//get JSON object
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

		//called when provider is disabled
		public void OnProviderDisabled(string provider)
		{
			Log.Debug("Finisher", provider + " disabled by user");
		}

		//called when provider is enabled
		public void OnProviderEnabled(string provider)
		{
			Log.Debug("Finisher", provider + " enabled by user");
		}

		//called when status of provider is changed
		public void OnStatusChanged(string provider, Availability status, Bundle extras)
		{
			Log.Debug("Finisher", provider + " availability has changed to " + status.ToString());
		}
	}
}