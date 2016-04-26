package testen;

import java.util.*;
import java.time.*;
//import java.io.File;
import java.io.IOException;
//import java.io.PrintWriter;
//import java.nio.file.Files;
//import java.nio.file.Paths;
//import java.nio.file.StandardOpenOption;
//import deleteFiles.*;
import org.eclipse.paho.client.mqttv3.*;
import de.dobermai.eclipsemagazin.paho.client.util.Utils;
import java.sql.Connection;
import java.sql.DriverManager;
//import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.sql.SQLException;
import java.sql.Statement;
import java.sql.Time;
//import java.sql.Time;

public class Publisher {
	public static final String Topic = "fin";
	private static MqttClient client;

	private static void publish(String message) throws MqttException {
		final MqttTopic topic = client.getTopic(Topic); // Set topic
		//System.out.println("Published data. Topic: " + topic.getName() + "  Message: " + message);
		MqttMessage test = new MqttMessage();
		test.setQos(0);
		test.setPayload(message.getBytes());
		topic.publish(test);
	}

	public static void main(String[] args) throws IOException, ClassNotFoundException, InterruptedException {
		// final Publisher publisher = new Publisher();
		// publisher.start();
		Connection connect = null;
		Statement statement = null;
		// private PreparedStatement preparedStatement = null;
		ResultSet resultSet = null;
		String number = args[0];
		try {
			Class.forName("com.mysql.jdbc.Driver");
			// Setup the connection with the DB
			connect = DriverManager
					.getConnection("jdbc:mysql://nasdenys.synology.me/roadit?autoReconnect=true&useSSL=false&"
							+ "user=roadit&password=roadit");

			// Statements allow to issue SQL queries to the database
			statement = connect.createStatement();

			resultSet = statement.executeQuery("select * from mqtt where number= " + number);
			writeData(resultSet);

			// writeResultSet(resultSet);
			connect.close();

		} catch (SQLException ex) {
			// handle any errors
			System.out.println("SQLException: " + ex.getMessage());
			System.out.println("SQLState: " + ex.getSQLState());
			System.out.println("VendorError: " + ex.getErrorCode());
		}

		/*String clientId = Utils.getMacAddress() + "-pub"; // get unique ID try
		try {
			client = new MqttClient("tcp://iot.eclipse.org:1883", clientId);
			// initiliaze MQTTClient

		} catch (MqttException e) {
			e.printStackTrace();
			System.exit(1);
		}*/
		/*try {
			MqttConnectOptions options = new MqttConnectOptions();
			options.setCleanSession(false);
			options.setUserName("username");
			options.setPassword("password".toCharArray());
			client.connect(options); // connect to client

			// Publish data forever
			while (true) {

				Thread.sleep(500);
				publish("test");
			}
		} catch (MqttException e) {
			e.printStackTrace();
			System.exit(1);
		} catch (InterruptedException e) {
			e.printStackTrace();
		}*/

	}

	private static void writeData(ResultSet resultSet) throws SQLException, InterruptedException {
		//System.out.println("The columns in the table are: ");

		//System.out.println("Table: " + resultSet.getMetaData().getTableName(1));
		//for (int i = 1; i <= resultSet.getMetaData().getColumnCount(); i++) {
		//	System.out.println("Column " + i + " " + resultSet.getMetaData().getColumnName(i));

		//}
		float time;
		float oldtime = 0;
		boolean first = true;
		String clientId = Utils.getMacAddress() + "-pub"; // get unique ID try
		try {
			client = new MqttClient("tcp://iot.eclipse.org:1883", clientId);
			// initiliaze MQTTClient

		} catch (MqttException e) {
			e.printStackTrace();
			System.exit(1);
		}
		while (resultSet.next()) {
			System.out.println(resultSet.getString("tijd") + " \t" + resultSet.getString("number") + " \t"
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
			Thread.sleep((long) ((time - oldtime) * 1000));
			try {
				MqttConnectOptions options = new MqttConnectOptions();
				options.setCleanSession(false);
				options.setUserName("username");
				options.setPassword("password".toCharArray());
				client.connect(options); // connect to client
					publish(resultSet.getString("string"));
				client.disconnect();
			} catch (MqttException e) {
				e.printStackTrace();
				System.exit(1);
			}
			oldtime = time;

		}

	}

}