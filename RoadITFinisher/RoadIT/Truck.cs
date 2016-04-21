using System;
using Android.Gms.Maps.Model;
using Android.Util;

namespace RoadIT
{
	public class Truck
	{
		private LatLng location;
		private string color;
		private int id;
		private string locstring;
		private PolylineOptions polylineOptions;
		private MarkerOptions marker;

		public Truck(LatLng location, string color, int id)
		{
			this.location = location;
			this.color = color;
			this.id = id;
			locstring = location.Latitude.ToString().Replace(",", ".") + "," + location.Longitude.ToString().Replace(",", ".") + "," + id;
		}

		public string getcolor()
		{
			return color;
		}

		public int getid()
		{
			return id;
		}

		public string getlocstring()
		{
			return locstring;
		}

		public void display()
		{
			Log.Debug("truckdisploc", locstring);
			Log.Debug("truckdispcoolor", color);
			Log.Debug("truckdsipid", id.ToString());
		}


	}
}

