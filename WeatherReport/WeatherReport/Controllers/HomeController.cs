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

            //If the forcast button is clicked without setting a marker an error will show.
            if (model.Lat != null && model.Lng != null)
            {
                forcast.CurrentWeatherByLatLong(model.Lat, model.Lng);

                //Adding WeatherAPI object to a session to be able to display the weather to the view
                Session["WeatherReport"] = forcast;

                //setting a view bag to send the last location the user requested weather for so on post back I can set the map to the same location
                ViewBag.cords = "{ lat:" + model.Lat + ", lng: " + model.Lng + " }";

                return View("WeatherByLocationCoords", model);
            }
            else
            {
                ModelState.AddModelError("", "Location was not found.");

                return View("Map", model);
            }
        }
    }
}