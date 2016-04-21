package testen;

//import java.io.File;
import java.io.IOException;
//import java.io.PrintWriter;
//import java.nio.file.Files;
//import java.nio.file.Paths;
//import java.nio.file.StandardOpenOption;
//import deleteFiles.*;
import org.eclipse.paho.client.mqttv3.*;
import de.dobermai.eclipsemagazin.paho.client.util.Utils;

public class Publisher {
    public static final String Topic = "fin";

    private MqttClient client;

    public Publisher() {}
    //The publisher starts
    private void start() {
    	 String clientId = Utils.getMacAddress() + "-pub";	//get unique ID


         try {
         	client = new MqttClient("tcp://iot.eclipse.org:1883", clientId); //initiliaze MQTTClient

         } catch (MqttException e) {
             e.printStackTrace();
             System.exit(1);
         }
        try {
            MqttConnectOptions options = new MqttConnectOptions();
            options.setCleanSession(false);
            options.setUserName("klaas"); 
            options.setPassword("test".toCharArray()); 
            //options.setWill(client.getTopic("fin"), "I'm gone :(".getBytes(), 0, false);
            client.connect(options);	//connect to client
            
            //Publish data forever
            while (true) {
                publish();
                Thread.sleep(500);
            }
        } catch (MqttException e) {
            e.printStackTrace();
            System.exit(1);
        } catch (InterruptedException e) {
            e.printStackTrace();
        }
    }

    //Generate a random value for the temperature and publish it
    private void publish() throws MqttException {
        final MqttTopic temperatureTopic = client.getTopic(Topic); //Set topic

        final int temperatureNumber = Utils.createRandomNumberBetween(20, 30);
        final String temperature = temperatureNumber+"";

        temperatureTopic.publish(new MqttMessage(temperature.getBytes()));

        System.out.println("Published data. Topic: " + temperatureTopic.getName() + "  Message: " + temperature);
    }

    public static void main(String... args) throws IOException {
        final Publisher publisher = new Publisher();
        publisher.start();
    }
}