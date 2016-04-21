package testen;


import de.dobermai.eclipsemagazin.paho.client.util.Utils;

import java.io.IOException;

import org.eclipse.paho.client.mqttv3.MqttClient;
import org.eclipse.paho.client.mqttv3.MqttConnectOptions;
import org.eclipse.paho.client.mqttv3.MqttException;
//import deleteFiles.*;

public class Subscriber {
    //We have to generate a unique Client id.
    String clientId = Utils.getMacAddress() + "-sub";
    private MqttClient mqttClient;

    public Subscriber() {

        try {

        	mqttClient = new MqttClient("tcp://iot.eclipse.org:1883", clientId);
        } catch (MqttException e) {
            e.printStackTrace();
            System.exit(1);
        }
    }

    //The subscriber start to receive
    public void start(int test, String topic) throws IOException {
        try {
            mqttClient.setCallback(new SubscribeCallback()); //Check if there is something new
           // mqttClient.connect();
            MqttConnectOptions options = new MqttConnectOptions();
            options.setCleanSession(true); 
            options.setKeepAliveInterval(180); 
            options.setUserName("jef"); 
            options.setPassword("test".toCharArray()); 
        	mqttClient.connect(options);
            mqttClient.subscribe(topic);
            test = 1; //test is 1 so it will pass the if-statement

            System.out.println("Subscriber is now listening to "+topic);

        } catch (MqttException e) {
            e.printStackTrace();
            System.exit(1);
        }
    }
 
    public static void main(String... args) throws IOException{
    	int test = 0;
    	String topic = "fin";
        final Subscriber subscriber = new Subscriber();
        subscriber.start(test,topic);
    }

}