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

namespace RoadIT
{
	//prevents activity from restarting when screen orientation changes
	[Activity(Label = "Road_it", ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation)]
	public class Finisher : Activity, ILocationListener
	{
		LatLng finisherloc = new LatLng(0, 0);
		GoogleMap map;
		MapFragment mapFragment;
		LocationManager locMgr;
		string ownlocstring;
		string durationString;
		const string tag = "Finisher";
		JObject _Jobj;
		public static string broker = "", topic="", username="", pass="";

		List<Truck> trucklist = new List<Truck>();

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
			Thread PublishMQTT = new Thread(() => MQTTPublish(ownlocstring + ",0"));
			PublishMQTT.Start();
		}

		public void MQTTPublish(string content)
		{

			string topic = "fin";
			int qos = 2;
			MemoryPersistence persistence = new MemoryPersistence();

			try
			{
				byte[] bytes = System.Text.Encoding.ASCII.GetBytes(content);
				MqttMessage message = new MqttMessage(bytes);
				message.Qos = qos;
				Client.Publish(topic, message);
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
			if (substrings.Length == 3)
			{
				try
				{
					if (Convert.ToDouble(substrings[2]) != 0)
					{
						//TODO todouble kapot nederlands?? punten verdwijnen?
						Log.Debug("mqttsubstring0", Convert.ToDouble(substrings[0]).ToString());
						Log.Debug("mqttsubstring1", Convert.ToDouble(substrings[1]).ToString());

						int id = Int32.Parse(substrings[2]);
						bool exists = false;
						foreach (Truck aTruck in trucklist)
						{
							if (aTruck.getid() == id)
							{
								exists = true;
								aTruck.setLocation(new LatLng(Convert.ToDouble(substrings[0]), Convert.ToDouble(substrings[1])));
								Thread mapAPICall2 = new Thread(() => mapAPICall(aTruck));
								mapAPICall2.Start();
							}
						}
						if (exists == false)
						{
							trucklist.Add(new Truck(new LatLng(Convert.ToDouble(substrings[0]), Convert.ToDouble(substrings[1])), id));

							Thread mapAPICall3 = new Thread(() => mapAPICall(trucklist.Find(t => t.getid() == id)));
							mapAPICall3.Start();
						}
						Log.Debug("trucklistelements", trucklist.Count().ToString());
						Log.Debug("MQTTinput", "Accept");
					}
				}
				catch
				{
					Log.Debug("MQTTinput", "input not right");
				}
			}
		}

		protected override void OnCreate(Bundle bundle)
		{
			
			base.OnCreate(bundle);
			Log.Debug(tag, "OnCreate called");
			string temp = Intent.GetStringExtra ("broker") ?? null;
			broker = "tcp://" + temp + ":1883";
			string name = Intent.GetStringExtra ("name") ?? null;
			string truck = Intent.GetStringExtra("truck") ?? null;
			Console.WriteLine(truck);
			topic = name;
			username = Intent.GetStringExtra ("username") ?? null;
			pass = Intent.GetStringExtra ("pass") ?? null;
			Console.WriteLine (broker + " "+ name+ " "+ username+" "+ pass);
			SetContentView(Resource.Layout.Finisher);
			InitMapFragment();
			SetupAnimateToButton();
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
			Log.Debug(tag, "OnResume called");


			/*Button stopbutton = FindViewById<Button>(Resource.Id.stopbutton);
			stopbutton.Click += (sender, e) =>
			{
				SampleActivity activitysetup = new SampleActivity(1, 2, typeof(MainActivity));
				activitysetup.Start(this);

				//Thread Stopmain = new Thread(() => OnStop());
				//Stopmain.Start();
			};*/
			// initialize location manager
			locMgr = GetSystemService(Context.LocationService) as LocationManager;

			if (locMgr.AllProviders.Contains(LocationManager.NetworkProvider)
				&& locMgr.IsProviderEnabled(LocationManager.NetworkProvider))
			{
				locMgr.RequestLocationUpdates(LocationManager.NetworkProvider, 2000, 1, this);
			}
			else {
				Toast.MakeText(this, "Please switch on your location service!", ToastLength.Long).Show();
			}
		}

		public static void ConfigMQTT()
		{
			
			try
			{
				Client.Connect();
				Client.Subscribe("fin");
				Log.Debug("MqttSubscribe", "connect");
			}
			catch (MqttException me)
			{
				Log.Debug("MqttSubscribe", "(re)connect failed"+ me.ToString());
			}
		}


		protected override void OnStart()
		{
			base.OnStart();
			Log.Debug(tag, "OnStart called: ");
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

		public void SetupAnimateToButton()
		{
			Button RouteButton = FindViewById<Button>(Resource.Id.routeButton);
			RouteButton.Click += (sender, e) =>
			{
				Thread PublishMQTT = new Thread(() => MQTTPublish("51.2074277,4.2935036,1"));
				PublishMQTT.Start();
				Thread.Sleep(50);
				Thread PublishMQTT2 = new Thread(() => MQTTPublish("51.1074277,5.135036,2"));
				PublishMQTT2.Start();
				Thread.Sleep(50);
				Thread PublishMQTT3 = new Thread(() => MQTTPublish("51.0074277,5.335036,3"));
				PublishMQTT3.Start();
				Thread.Sleep(50);
				Thread PublishMQTT4 = new Thread(() => MQTTPublish("50.8074277,4.555036,4"));
				PublishMQTT4.Start();
				Thread.Sleep(50);
			};
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

		public void getDuration(Truck truck)
		{
			int dur = getDistanceTo();
			int min = int.MaxValue;

			truck.setDur(dur);

			//TODO CLEANUP

			Log.Debug("durationmin", min.ToString());
			Log.Debug("durationdur", dur.ToString());

			//set nearest to false for every truck
			foreach (Truck aTruck in trucklist)
			{
				aTruck.setNearest(false);
			}

			//sort on duration -> nearest truck first in list
			trucklist.Sort((x, y) => x.getDur().CompareTo(y.getDur()));
			trucklist.First().setNearest(true);

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

			durationString = "ETA of nearest truck: " + trucklist.First().getDur() + "s";
			TextView durationtextfield = FindViewById<TextView>(Resource.Id.durationText);

			//update textfield in main UI thread
			RunOnUiThread(() => durationtextfield.Text = durationString);

			Thread drawRouteThread = new Thread(() => drawRoute(truck));
			drawRouteThread.Start();


		}

		public void updateUI()
		{
			BitmapDescriptor truck = BitmapDescriptorFactory.FromResource(Resource.Drawable.truck);
			map.Clear();

			//temp variable -> no chance of changes in foreach
			List<Truck> listtemp = trucklist;

			Log.Debug("updateUI", "list of trucks");

			foreach (Truck aTruck in listtemp)
			{
				aTruck.display();
				map.AddPolyline(aTruck.getPolylineOptions());
				MarkerOptions markertruck = new MarkerOptions();
				markertruck.SetPosition(aTruck.getLocation());
				markertruck.SetTitle("Truck " + aTruck.getid() + " arrives in: " + aTruck.getDur() + "s");
				markertruck.SetIcon(truck);
				map.AddMarker(markertruck);
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

		void mapAPICall(Truck truck)
		{
			string origin = truck.getlocstring();
			try
			{	
				string url = "http://maps.googleapis.com/maps/api/directions/json?origin=" + origin + "&destination=" + ownlocstring + "&sensor=false";
				string requesturl = url; string content = fileGetJSON(requesturl);;
				_Jobj = JObject.Parse(content);
				Log.Debug("apicall", content.ToString());
			}
			catch { }

			Thread durationThread = new Thread(() => getDuration(truck));
			durationThread.Start();
		}

		void drawRoute(Truck truck)
		{
			Log.Debug("http", "drawroutestart");

			PolylineOptions temppoly = new PolylineOptions();

			//green for nearest truck
			if (truck.getNearest() == true)
			{
				Log.Debug("drawroute", "nearest");
				temppoly.InvokeColor(0x6600cc00);
			}
			else if (truck.getcolor() == "blue")
			{
				Log.Debug("drawroute", truck.getid() + truck.getcolor() + "moet blue zijn");
				temppoly.InvokeColor(0x66000099);
			}
			else if (truck.getcolor() == "red")
			{
				Log.Debug("drawroute", truck.getid() + truck.getcolor() + "moet red zijn");
				temppoly.InvokeColor(0x66ff0000);
			}
			else if (truck.getcolor() == "black")
			{
				Log.Debug("drawroute", truck.getid() + truck.getcolor() + "moet black zijn");
				temppoly.InvokeColor(0x66000000);
			}
			else if (truck.getcolor() == "purple")
			{
				Log.Debug("drawroute", truck.getid() + truck.getcolor() + "moet purple zijn");
				temppoly.InvokeColor(0x669933ff);
			}
			else
			{
				//blue
				Log.Debug("drawroute", "else");
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
				truck.setPolylineOptions(temppoly);
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
	}
}