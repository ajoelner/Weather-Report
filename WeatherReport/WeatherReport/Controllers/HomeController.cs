using System.Web.Mvc;
using WeatherReport.Models.DataModel;
using WeatherReport.Models.ViewModel;

namespace WeatherReport.Controllers
{
    public class HomeController : Controller
    {
        // GET: Home
        public ActionResult Index()
        {
            return View("Map");
        }

        public ActionResult WeatherByLocation(LocationViewModel model)
        {
            WeatherAPI forcast = new WeatherAPI();
            forcast.CurrentWeatherByLatLong(model.Lat, model.Lng);

            //setting a view bag to send the last location the user requested weather for so on post back I can set the map to the same location
            ViewBag.cords = "{ lat:" + model.Lat + ", lng: " + model.Lng + " }";

            return View("WeatherByLocationCoords", model);
        }
    }
}