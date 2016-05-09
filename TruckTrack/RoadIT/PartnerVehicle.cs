using System;
using Android.Gms.Maps.Model;
using Android.Util;

namespace RoadIT
{
	public class PartnerVehicle
	{
		private LatLng location;
		private string color;
		private int duration;
		private string id;
		private string locstring;
		private PolylineOptions polylineOptions;
		private bool nearest = false;
		private bool toReDraw = false;
		private Random rnd = new Random();
		private string[] colorarray = new string[] { "red", "blue", "black", "purple" };

		public PartnerVehicle(LatLng location, string id)
		{
			this.location = location;
			color = colorarray[rnd.Next(0, colorarray.Length)];
			this.id = id;
			locstring = location.Latitude.ToString().Replace(",", ".") + "," + location.Longitude.ToString().Replace(",", ".");
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

		public string getid()
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
			Log.Debug("disploc", locstring);
			Log.Debug("dispnearest", nearest.ToString());
			Log.Debug("disptodraw", toReDraw.ToString());
			Log.Debug("dispcolor", color);
			Log.Debug("dispid", id);
		}


	}
}

