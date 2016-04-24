using System;
using Android.Gms.Maps.Model;
using Android.Util;

namespace RoadIT
{
	public class Truck
	{
		private LatLng location;
		private string color;
		private int duration;
		private int id;
		private string locstring;
		private PolylineOptions polylineOptions;
		private MarkerOptions marker;
		private string[] colorarray = new string[] { "red", "blue" };

		public Truck(LatLng location, int id)
		{
			this.location = location;
			color = colorarray[id-1];
			this.id = id;
			locstring = location.Latitude.ToString().Replace(",", ".") + "," + location.Longitude.ToString().Replace(",", ".");//+ "," + id;
			polylineOptions = new PolylineOptions();
		}

		public string getcolor()
		{
			return color;
		}

		public int getid()
		{
			return id;
		}

		public void setDuration(int duration)
		{
			this.duration = duration;
		}

		public int getDuration()
		{
			return duration;
		}

		public string getlocstring()
		{
			return locstring;
		}

		public void setPolylineOptions(PolylineOptions poly)
		{
			polylineOptions = poly;
		}

		public PolylineOptions getPolylineOptions()
		{
			return polylineOptions;
		}

		public MarkerOptions getMarker()
		{
			return marker;
		}

		public LatLng getLocation()
		{
			return location;
		}

		public void display()
		{
			Log.Debug("truckdisploc", locstring);
			Log.Debug("truckdispcoolor", color);
			Log.Debug("truckdsipid", id.ToString());
		}


	}
}

