using Android.App;
using Android.OS;
using Java.Lang;
using Android.Util;
using Org.Eclipse.Paho.Client.Mqttv3;

namespace RoadIT
{
	//[Activity(Label = "MqttSubscribe")]
	public class MqttSubscribe : Activity, IMqttCallback
	{
		OwnVehicle ownvec;
		public MqttSubscribe(OwnVehicle myvec)
		{
			ownvec = myvec;
		}

		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);
			Log.Debug("MqttSubscribe", "create");	
		}
		public void MessageArrived(string topic, MqttMessage message)
		{
			Log.Debug("MqttSubscribe", "Topic: "+topic+"Msg: "+ message.ToString());
			string messagestring = message.ToString();
			ownvec.MQTTupdate(messagestring, topic);
		}

		public void ConnectionLost(Throwable cause)
		{
			Log.Debug("MqttSubscribe", "connectionlost");
			ownvec.ConfigMQTT();
		}

		public void DeliveryComplete(IMqttDeliveryToken token)
		{
			Log.Debug("MqttSubscribe", "deliverycomplete");
		}

	}
}
