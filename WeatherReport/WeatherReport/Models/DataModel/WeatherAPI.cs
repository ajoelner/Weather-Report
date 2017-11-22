using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;

namespace WeatherReport.Models.DataModel
{
    public class WeatherAPI
    {
        public void CurrentWeatherByCityID(string city, string state, string country)
        {
            string cityID = FindCityIDFromCityNameAndStateAndCountry(city, state, country);

            if (cityID != null)
            {
                HttpWebRequest WebReq = (HttpWebRequest)WebRequest.Create(string.Format("http://api.openweathermap.org/data/2.5/weather?id=" + cityID + "&appid=" + ConfigurationManager.AppSettings["WEATHERAPIKEY"].ToString()));

                WebReq.Method = "GET";

                HttpWebResponse WebResp = (HttpWebResponse)WebReq.GetResponse();

                if (WebResp.StatusCode.ToString() == "OK")
                {
                    string jsonString;

                    using (Stream stream = WebResp.GetResponseStream())   //modified from your code since the using statement disposes the stream automatically when done
                    {
                        StreamReader reader = new StreamReader(stream, System.Text.Encoding.UTF8);
                        jsonString = reader.ReadToEnd();
                    }

                    List WeatherReport = JsonConvert.DeserializeObject<List>(jsonString);
                }
            }
            else
            {
                //Validation for if cityID is null
            }
        }

        /// <summary>
        /// Calls the google geocoding API to gather the Lat and Lon of the city state and country being searched for.
        /// Than I sort out the full list of cities to only the cities that lay on the same latitude as each other.
        /// Than Calculate the distance between the searhced cities lat and long to a city which our weather API supports to gather the closest weather station.
        /// </summary>
        /// <param name="cityName"></param>
        /// <param name="stateAbr"></param>
        /// <param name="country"></param>
        /// <returns></returns>
        private string FindCityIDFromCityNameAndStateAndCountry(string cityName, string stateAbr, string country)
        {
            string cityID = null;
            string latStr = null;
            double lngHoldForDistanceCalc = 0.00;
            double latHoldForDistanceCalc = 0.00;

            //GeoCoding api by sending state and city to get the lat and long of the city requesting weather from
            HttpWebRequest WebReq = (HttpWebRequest)WebRequest.Create(string.Format("https://maps.googleapis.com/maps/api/geocode/json?address=" + cityName + "," + stateAbr + "|country:" + country + "&key=" + ConfigurationManager.AppSettings["GOOGLEGEOCODINGAPIKEY"].ToString()));

            WebReq.Method = "GET";
            HttpWebResponse WebResp = (HttpWebResponse)WebReq.GetResponse();

            if (WebResp.StatusCode.ToString() == "OK")
            {
                string jsonString;

                using (Stream stream = WebResp.GetResponseStream())   //modified from your code since the using statement disposes the stream automatically when done
                {
                    StreamReader reader = new StreamReader(stream, System.Text.Encoding.UTF8);
                    jsonString = reader.ReadToEnd();
                }

                RootObject2 cityInformation = JsonConvert.DeserializeObject<RootObject2>(jsonString);

                //Checking if city results are found
                if (cityInformation.results.ToString() != "ZERO_RESULTS")
                {
                    latHoldForDistanceCalc = cityInformation.results[0].geometry.location.lat;

                    //cutting the lat before the decimal to use as my search aid to collect all cities which lays on the same lat as the city were trying to gather weather for.
                    latStr = cityInformation.results[0].geometry.location.lat.ToString("#.000");
                    if (latStr.Contains("."))
                    {
                        latStr = latStr.Substring(0, latStr.IndexOf("."));
                    }

                    lngHoldForDistanceCalc = cityInformation.results[0].geometry.location.lng;

                    //go though city list json and find the city by city name and match up the city with the lat and lng to make sure we are pulling the correct city
                    if (File.Exists((AppDomain.CurrentDomain.BaseDirectory + "city.list.json")))
                    {
                        // read JSON directly from a file
                        List<City> citiesJson;
                        using (StreamReader r = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + "city.list.json"))
                        {
                            string json = r.ReadToEnd();
                            citiesJson = JsonConvert.DeserializeObject<List<City>>(json);
                        }

                        //If latitude is a single digit format with just one diget this will eliminate a leading 0
                        //laditudes should not have leading zeros with the API I am using
                        string latFormat = null;

                        if (latStr.Length == 2 && latStr.Contains("-"))
                        {
                            latFormat = "0";
                        }
                        else
                        {
                            latFormat = "00";
                        }

                        //gathering a list of cities from API cities list which lay on the same latitude as the city being searched for.
                        var listOfCitiesWhichMatchLat = citiesJson.Where(c => Math.Floor(c.coord.lat).ToString(latFormat) == latStr);

                        //Loops though all the cities which match the same lat as the city were seaarching for than calculating the distance from our searched city to a city which has a supported
                        //weather station this weather API has data for. this will be able to help me accuratly aquire the closest weather station to the city the user wants weather info for.
                        int count = 0;
                        double holdDistance = 0.00;
                        double distance = 0.00;
                        City holdCity = null;
                        foreach (City item in listOfCitiesWhichMatchLat)
                        {
                            distance = calculate(latHoldForDistanceCalc, lngHoldForDistanceCalc, item.coord.lat, item.coord.lon);

                            if (count == 0)
                            {
                                holdDistance = distance;
                            }
                            else
                            {
                                //Holds on to the closest city by KM to the city that was being searched for.
                                if (distance <= holdDistance)
                                {
                                    holdDistance = distance;
                                    holdCity = item;
                                }
                            }
                            count++;
                        }

                        //grabs the city ID from the city searhced for for easier pull from the weather API.
                        cityID = holdCity.id.ToString();
                    }
                    else
                    {
                        //Validation for if city json isnt found
                    }
                }
                else
                {
                    //Validation for if ZERO results pull back
                }
            }

            return cityID;
        }

        /// <summary>
        /// Calculates the distance in km between 2 map coordinates
        /// </summary>
        /// <param name="lat1"></param>
        /// <param name="lon1"></param>
        /// <param name="lat2"></param>
        /// <param name="lon2"></param>
        /// <returns></returns>
        private double calculate(double lat1, double lon1, double lat2, double lon2)
        {
            var R = 6372.8; // In kilometers
            var dLat = toRadians(lat2 - lat1);
            var dLon = toRadians(lon2 - lon1);
            lat1 = toRadians(lat1);
            lat2 = toRadians(lat2);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(lat1) * Math.Cos(lat2);
            var c = 2 * Math.Asin(Math.Sqrt(a));
            return R * 2 * Math.Asin(Math.Sqrt(a));
        }

        private double toRadians(double angle)
        {
            return Math.PI * angle / 180.0;
        }
    }
}

public class Main
{
    public double temp { get; set; }
    public double temp_min { get; set; }
    public double temp_max { get; set; }
    public double pressure { get; set; }
    public double sea_level { get; set; }
    public double grnd_level { get; set; }
    public int humidity { get; set; }
    public double temp_kf { get; set; }
}

public class Weather
{
    public int id { get; set; }
    public string main { get; set; }
    public string description { get; set; }
    public string icon { get; set; }
}

public class Clouds
{
    public int all { get; set; }
}

public class Wind
{
    public double speed { get; set; }
    public double deg { get; set; }
}

public class Rain
{
    public double __invalid_name__3h { get; set; }
}

public class Snow
{
    public double __invalid_name__3h { get; set; }
}

public class Sys
{
    public string pod { get; set; }
}

public class List
{
    public int dt { get; set; }
    public Main main { get; set; }
    public List<Weather> weather { get; set; }
    public Clouds clouds { get; set; }
    public Wind wind { get; set; }
    public Rain rain { get; set; }
    public Snow snow { get; set; }
    public Sys sys { get; set; }
    public string dt_txt { get; set; }
}

public class Coord
{
    public double lat { get; set; }
    public double lon { get; set; }
}

public class City
{
    public int id { get; set; }
    public string name { get; set; }
    public Coord coord { get; set; }
    public string country { get; set; }
}

public class RootObject
{
    public string cod { get; set; }
    public double message { get; set; }
    public int cnt { get; set; }
    public List<List> list { get; set; }
    public City city { get; set; }
}

//These are the classes needed to deserilize google maps Geocoding
public class AddressComponent
{
    public string long_name { get; set; }
    public string short_name { get; set; }
    public List<string> types { get; set; }
}

public class Northeast
{
    public double lat { get; set; }
    public double lng { get; set; }
}

public class Southwest
{
    public double lat { get; set; }
    public double lng { get; set; }
}

public class Bounds
{
    public Northeast northeast { get; set; }
    public Southwest southwest { get; set; }
}

public class Location
{
    public double lat { get; set; }
    public double lng { get; set; }
}

public class Northeast2
{
    public double lat { get; set; }
    public double lng { get; set; }
}

public class Southwest2
{
    public double lat { get; set; }
    public double lng { get; set; }
}

public class Viewport
{
    public Northeast2 northeast { get; set; }
    public Southwest2 southwest { get; set; }
}

public class Geometry
{
    public Bounds bounds { get; set; }
    public Location location { get; set; }
    public string location_type { get; set; }
    public Viewport viewport { get; set; }
}

public class Result
{
    public List<AddressComponent> address_components { get; set; }
    public string formatted_address { get; set; }
    public Geometry geometry { get; set; }
    public string place_id { get; set; }
    public List<string> types { get; set; }
}

public class RootObject2
{
    public List<Result> results { get; set; }
    public string status { get; set; }
}