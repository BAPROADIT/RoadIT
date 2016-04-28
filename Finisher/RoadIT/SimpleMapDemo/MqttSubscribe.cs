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

namespace ROADIT
{
	[Activity(Label = "MqttSubscribe")]
	public class MqttSubscribe : Activity, IMqttCallback
	{
		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			// Create your application here
		}
		public void MessageArrived(string topic, MqttMessage message)
		{
			Log.Debug("MqttSubscribe", message.ToString());
			//string test = message.ToString();
			//Log.Debug("mqttzever", test);
			//MapWithMarkersActivity.MQTTin(test);
		}

		public void ConnectionLost(Throwable cause)
		{
		}

		public void DeliveryComplete(IMqttDeliveryToken token)
		{
		}

	}
}
