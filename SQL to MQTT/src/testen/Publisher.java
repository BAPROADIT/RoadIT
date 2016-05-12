package testen;
//import java.io.File;
import java.io.IOException;
import org.eclipse.paho.client.mqttv3.*;
import de.dobermai.eclipsemagazin.paho.client.util.Utils;
import java.sql.Connection;
import java.sql.DriverManager;
//import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.sql.SQLException;
import java.sql.Statement;
//import java.sql.Time;

public class Publisher {
	private static MqttClient client;
	
	private static void publish(String message, String Topic) throws MqttException {
		final MqttTopic topic = client.getTopic(Topic); // Set topic
		MqttMessage test = new MqttMessage();
		test.setQos(0);
		test.setPayload(message.getBytes());
		topic.publish(test);
	}

	public static void main(String[] args) throws IOException, ClassNotFoundException, InterruptedException {
		Connection connect = null;
		Statement statement = null;
		ResultSet resultSet = null;
		String topic = args[0];
		try {
			Class.forName("com.mysql.jdbc.Driver");
			// Setup the connection with the DB
			connect = DriverManager
					.getConnection("jdbc:mysql://nasdenys.synology.me/roadit?autoReconnect=true&useSSL=false&"
							+ "user=roadit&password=roadit");

			// Statements allow to issue SQL queries to the database
			statement = connect.createStatement();

			resultSet = statement.executeQuery("select * from roadit where tijd Between '2016-05-10 13:14:02' And '2016-05-10 14:10:08';");//
			writeData(resultSet);
			System.out.println("Done");

			// writeResultSet(resultSet);
			connect.close();

		} catch (SQLException ex) {
			// handle any errors
			System.out.println("SQLException: " + ex.getMessage());
			System.out.println("SQLState: " + ex.getSQLState());
			System.out.println("VendorError: " + ex.getErrorCode());
		}
	}

	private static void writeData(ResultSet resultSet) throws SQLException, InterruptedException {
		float time;
		float factor = (float) 1;
		float oldtime = 0;
		boolean first = true;
		String clientId = Utils.getMacAddress() + "-pub"; // get unique ID try
		try {
			client = new MqttClient("tcp://nasdenys.synology.me:1883", clientId);
			// initiliaze MQTTClient

		} catch (MqttException e) {
			e.printStackTrace();
			System.exit(1);
		}
		while (resultSet.next()) {
			System.out.println(resultSet.getString("tijd") + " \t" + resultSet.getString("topic") + " \t"
					+ resultSet.getString("string"));
			String[] parts = resultSet.getString("tijd").toString().split("-");
			// 2016-04-26 12:32:20.0
			// Year, Month
			// Integer.parseInt(parts[0]);
			// Integer.parseInt(parts[1]);
			String[] parts2 = parts[2].toString().split(" ");
			// Day
			// Integer.parseInt(parts2[0]);
			String[] parts3 = parts2[1].toString().split(":");
			// Hour, Minute, Second
			// Integer.parseInt(parts3[0]);
			// Integer.parseInt(parts3[1]);
			// Integer.parseInt(parts3[2]);

			time = Integer.parseInt(parts3[0]) * 3600 + Integer.parseInt(parts3[1]) * 60 + Float.parseFloat(parts3[2]);
			if (first == true) {
				oldtime = time;
				first = false;
			}

			System.out.println("Sleep (s):" + (time - oldtime));
			Thread.sleep((long) ((time - oldtime) * 1000*factor));
			try {
				MqttConnectOptions options = new MqttConnectOptions();
				options.setCleanSession(false);
				options.setUserName("simulator");
				options.setPassword("simulator".toCharArray());
				client.connect(options); // connect to client
					publish(resultSet.getString("string"), resultSet.getString("topic"));
				client.disconnect();
			} catch (MqttException e) {
				e.printStackTrace();
				System.exit(1);
			}
			oldtime = time;

		}

	}

}