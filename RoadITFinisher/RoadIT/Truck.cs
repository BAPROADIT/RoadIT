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
		private bool nearest = false;
		private bool toReDraw = false;
		private Random rnd = new Random();
		private string[] colorarray = new string[] { "red", "blue", "black", "purple" };

		public Truck(LatLng location, int id)
		{
			this.location = location;
			color = colorarray[rnd.Next(0, colorarray.Length)];
			this.id = id;
			locstring = location.Latitude.ToString().Replace(",", ".") + "," + location.Longitude.ToString().Replace(",", ".");//+ "," + id;
			polylineOptions = new PolylineOptions();
		}

		public void setNearest(bool nearest)
		{
			this.nearest = nearest;
		}

		public bool getNearest()
		{
			return nearest;
		}

		public void setToReDraw(bool toReDraw)
		{
			this.toReDraw = toReDraw;
		}

		public bool getToReDraw()
		{
			return toReDraw;
		}

		public string getcolor()
		{
			return color;
		}

		public void setcolor(string color)
		{
			this.color = color;
		}

		public int getid()
		{
			return id;
		}

		public void setDur(int duration)
		{
			this.duration = duration;
		}

		public int getDur()
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

		public LatLng getLocation()
		{
			return location;
		}

		public void setLocation(LatLng location)
		{
			this.location = location;
			locstring = location.Latitude.ToString().Replace(",", ".") + "," + location.Longitude.ToString().Replace(",", ".");
		}

		public void display()
		{
			Log.Debug("truckdisploc", locstring);
			Log.Debug("truckdispnearest", nearest.ToString());
			Log.Debug("truckdisptodraw", toReDraw.ToString());
			Log.Debug("truckdispcolor", color);
			Log.Debug("truckdispid", id.ToString());
		}


	}
}

