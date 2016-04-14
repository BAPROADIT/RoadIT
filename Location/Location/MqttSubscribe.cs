
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
using Java.Lang;
using Android.Util;

using Org.Eclipse.Paho.Client.Mqttv3;
using Org.Eclipse.Paho.Client.Mqttv3.Internal;
using Org.Eclipse.Paho.Client.Mqttv3.Logging;
using Org.Eclipse.Paho.Client.Mqttv3.Persist;
using Org.Eclipse.Paho.Client.Mqttv3.Util;

namespace Location
{
	[Activity (Label = "MqttSubscribe")]			
	public class MqttSubscribe : Activity, IMqttCallback
	{
		protected override void OnCreate (Bundle savedInstanceState)
		{
			base.OnCreate (savedInstanceState);

			// Create your application here
		}
		public void MessageArrived(string topic, MqttMessage message) {
			string test = "Bericht: "+ message.ToString();
			MainActivity.popup (test);
			Log.Debug ("MQTT", test);
		}

		public void ConnectionLost(Throwable cause) {
			Log.Debug ("MQTT", "ConnectionLost: "+cause.Message.ToString());
			MainActivity.initmqtt ();
		}

		public void DeliveryComplete(IMqttDeliveryToken token) {
			Log.Debug ("MQTT", "Delivery");
		}

	}
}

