package testen;

import java.util.ArrayList;

import org.eclipse.paho.client.mqttv3.MqttException;
import org.eclipse.paho.client.mqttv3.MqttPersistenceException;


public class activeTopics {
	static ArrayList<topic> topics = new ArrayList<topic>();

	public static void newMessage(String topic, String Message) {

		if (Message.equals("killme")) {
			for (int index = 0; index < topics.size(); index++) {
				topic test = topics.get(index);
				if (test.topic.equals(topic)) {
					topics.remove(index);
				}
			}
		} else {
			boolean replace = false;
			topic temptopic = new topic(topic, 0);
			for (int index = 0; index < topics.size(); index++) {
				topic test = topics.get(index);
				if (test.topic.equals(topic)) {
					topics.set(index, temptopic);
					replace = true;
				}
			}
			if (replace == false) {
				topics.add(temptopic);
			}
		}
		/*for (int index = 0; index < topics.size(); index++) {
			System.out.println(topics.get(index).topic);
		}*/

	}

	public static void killhandler() throws MqttPersistenceException, MqttException {
		//System.out.println("Topics ");
		for (int index = 0; index < topics.size(); index++) {
			//System.out.println("Check "+topics.get(index).topic +"\tTime: "+topics.get(index).time);
			String[] parts = topics.get(index).topic.split("/");
			if (parts[1].equals("truck")) {
				topics.get(index).time++;
				if (topics.get(index).time >= 60) {
					System.out.println("kill"+topics.get(index).topic);
					Subscriber.publishkill(topics.get(index).topic, "killme");
				}
			}

		}
	}

}
