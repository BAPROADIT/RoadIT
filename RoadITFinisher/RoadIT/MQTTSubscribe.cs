using Android.App;
using Android.OS;
using Java.Lang;
using Android.Util;
using Org.Eclipse.Paho.Client.Mqttv3;

namespace RoadIT
{
	[Activity(Label = "MqttSubscribe")]
	public class MqttSubscribe : Activity, IMqttCallback
	{
		Finisher fin = new Finisher();
		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);
			Log.Debug("MqttSubscribe", "create");
			// Create your application here
		}
		public void MessageArrived(string topic, MqttMessage message)
		{
			Log.Debug("MqttSubscribe", message.ToString());
			string test = message.ToString();
			fin.MQTTupdate(test);
		}

		public void ConnectionLost(Throwable cause)
		{
			Log.Debug("MqttSubscribe", "connectionlost");
			Finisher.ConfigMQTT();
		}

		public void DeliveryComplete(IMqttDeliveryToken token)
		{
			Log.Debug("MqttSubscribe", "deliverycomplete");
		}

	}
}
