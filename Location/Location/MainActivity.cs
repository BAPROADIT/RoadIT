using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Locations;
using Android.Util;
using Java.Lang;

using Org.Eclipse.Paho.Client.Mqttv3;
using Org.Eclipse.Paho.Client.Mqttv3.Internal;
using Org.Eclipse.Paho.Client.Mqttv3.Logging;
using Org.Eclipse.Paho.Client.Mqttv3.Persist;
using Org.Eclipse.Paho.Client.Mqttv3.Util;

namespace Location
{
	[Activity (Label = "MQTT", MainLauncher = true)]
	public class MainActivity : Activity, ILocationListener
	{
		LocationManager locMgr;
		Button button;
		TextView latitude;
		TextView longitude;
		TextView provider;

		public static string broker       = "tcp://iot.eclipse.org:1883";
		public static string clientId     = "JavaSample";

		public static MemoryPersistence persistence = new MemoryPersistence();
		public static MqttClient Client=new MqttClient(broker, clientId, persistence);
		static string messagebutton  = null;
		string locationstring= "Leeg";
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			// Set our view from the "main" layout resource
			SetContentView (Resource.Layout.Main);
			button = FindViewById<Button> (Resource.Id.myButton);
			latitude = FindViewById<TextView> (Resource.Id.latitude);
			longitude = FindViewById<TextView> (Resource.Id.longitude);
			provider = FindViewById<TextView> (Resource.Id.provider);
			Client.SetCallback(new MqttSubscribe());
			initmqtt ();
		}
		public static void initmqtt(){
			
				Log.Debug ("MQTT", "init");
				try {
					Client.Connect ();
					Client.Subscribe("fin");
				} catch (MqttException me) {
				Log.Debug ("MQTT init error: ", me.ToString());

				}
		}
		protected override void OnStart ()
		{
			base.OnStart ();
		}
		protected override void OnResume ()
		{
			base.OnResume (); 
			locMgr = GetSystemService (Context.LocationService) as LocationManager;

			button.Click += delegate {
				button.Text =  messagebutton;

				if (locMgr.AllProviders.Contains (LocationManager.NetworkProvider)
					&& locMgr.IsProviderEnabled (LocationManager.NetworkProvider)) {
					locMgr.RequestLocationUpdates (LocationManager.NetworkProvider, 2000, 1, this);
				} else {
					Toast.MakeText (this, "The Network Provider does not exist or is not enabled!", ToastLength.Long).Show ();
				}


				MQTTPublish (locationstring);
			};
		}

		protected override void OnPause ()
		{
			base.OnPause ();
			//locMgr.RemoveUpdates (this);
			}

		protected override void OnStop ()
		{
			base.OnStop ();
		}

		public void OnLocationChanged (Android.Locations.Location location)
		{
			//Log.Debug (tag, "Location changed");
			latitude.Text = "Latitude: " + location.Latitude.ToString();
			longitude.Text = "Longitude: " + location.Longitude.ToString();
			//provider.Text = "Provider: " + location.Provider.ToString();
			locationstring = location.Latitude.ToString () +","+ location.Longitude.ToString ();

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
			} catch(MqttException me) {
				me.PrintStackTrace();
			}
		}

		public void OnProviderDisabled (string provider)
		{
		}
		public void OnProviderEnabled (string provider)
		{
		}
		public void OnStatusChanged (string provider, Availability status, Bundle extras)
		{
		}
		public static void popup(string message){
			messagebutton=message;

		}
	}
}


