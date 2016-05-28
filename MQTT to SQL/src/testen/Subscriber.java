package testen;

import de.dobermai.eclipsemagazin.paho.client.util.Utils;

import java.io.IOException;

import org.eclipse.paho.client.mqttv3.MqttClient;
import org.eclipse.paho.client.mqttv3.MqttConnectOptions;
import org.eclipse.paho.client.mqttv3.MqttException;
//import deleteFiles.*;
import org.eclipse.paho.client.mqttv3.MqttMessage;
import org.eclipse.paho.client.mqttv3.MqttPersistenceException;
import org.eclipse.paho.client.mqttv3.MqttTopic;

public class Subscriber {
	// We have to generate a unique Client id.
	String clientId = Utils.getMacAddress() + "-sub";
	private static MqttClient mqttClient;

	public Subscriber() {
		Thread kill = new Thread(new Runnable() {
			public void run() {
				/*while (true) {
					try {
						activeTopics.killhandler();
					} catch (MqttException e1) {
						// TODO Auto-generated catch block
						e1.printStackTrace();
					}
					try {
						Thread.sleep(1000);
					} catch (InterruptedException e) {
						// TODO Auto-generated catch block
						e.printStackTrace();
					}
				}*/
			}
		});
		kill.start();
		try {

			// mqttClient = new MqttClient("tcp://iot.eclipse.org:1883",
			// clientId);
			mqttClient = new MqttClient("tcp://nasdenys.synology.me:1883", clientId);
		} catch (MqttException e) {
			e.printStackTrace();
			System.exit(1);
		}
	}

	// The subscriber start to receive
	public void start(int test, String topic) throws IOException {
		try {
			mqttClient.setCallback(new SubscribeCallback()); // Check if there
																// is something
																// new
			// mqttClient.connect();
			MqttConnectOptions options = new MqttConnectOptions();
			options.setCleanSession(true);
			options.setKeepAliveInterval(180);
			options.setUserName("fin");
			options.setPassword("fin".toCharArray());
			mqttClient.connect(options);
			mqttClient.subscribe(topic);
			test = 1; // test is 1 so it will pass the if-statement

			System.out.println("Subscriber is now listening to " + topic);

		} catch (MqttException e) {
			e.printStackTrace();
			System.exit(1);
		}
	}

	public static void main(String... args) throws IOException {
		int test = 0;
		String topic = "roadit/#";
		final Subscriber subscriber = new Subscriber();
		subscriber.start(test, topic);
	}
	
	public static void publishkill(String Topic, String message) throws MqttPersistenceException, MqttException{
		final MqttTopic topic = mqttClient.getTopic(Topic); // Set topic
		MqttMessage test = new MqttMessage();
		test.setQos(0);
		test.setPayload(message.getBytes());
		topic.publish(test);		
	
	}

}