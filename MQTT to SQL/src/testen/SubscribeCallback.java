package testen;

//import java.beans.Statement;
import java.sql.Connection;
import java.sql.DriverManager;
//import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.sql.SQLException;
import java.sql.Statement;
//import java.util.Date;

import org.eclipse.paho.client.mqttv3.*;

public class SubscribeCallback implements MqttCallback {
	Connection connect = null;
	Statement statement = null;
	// private PreparedStatement preparedStatement = null;
	ResultSet resultSet = null;

	@Override
	public void connectionLost(Throwable cause) {
		// This is called when the connection is lost. We could reconnect here.
	}

	@Override
	// Gives a sign when a message arrives and writes it to the file logData
	public void messageArrived(String topic, MqttMessage message) throws Exception {
		System.out.println("Message arrived. Topic: " + topic + "  Message: " + message.toString());
		activeTopics.newMessage(topic, message.toString());
		try {//To SQL
			Class.forName("com.mysql.jdbc.Driver");
			// Setup the connection with the DB
			connect = DriverManager.getConnection("jdbc:mysql://nasdenys.synology.me/roadit?autoReconnect=true&useSSL=false&" + "user=roadit&password=roadit");

			// Statements allow to issue SQL queries to the database
			 statement = connect.createStatement();
			// Result set get the result of the SQL query
			statement.executeUpdate("INSERT INTO roadit(string, topic) VALUES (\""+message.toString()+"\",\""+topic+"\");");
			
		} catch (SQLException ex) {
			// handle any errors
			System.out.println("SQLException: " + ex.getMessage());
			System.out.println("SQLState: " + ex.getSQLState());
			System.out.println("VendorError: " + ex.getErrorCode());
		}
	}

	@Override
	public void deliveryComplete(IMqttDeliveryToken token) {
		// no-op
	}
}