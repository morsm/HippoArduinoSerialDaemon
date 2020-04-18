using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;


namespace Termors.Serivces.HippoArduinoSerialDaemon
{
    public class WebApiController : ApiController
    {
        [Route("webapi/html"), HttpGet]
        public HttpResponseMessage GetWebPage()
        {
            var result = WebPageGenerator.GenerateWebPage();

            var res = Request.CreateResponse(HttpStatusCode.OK);
            res.Content = new StringContent(result.ToString(), System.Text.Encoding.UTF8, "text/html");

            return res;
        }


        [Route("webapi/temp"), HttpGet]
        public async Task<Temperature> GetTemperature()
        {
            Temperature temp = new Temperature();

            string tmpStr = await SerialDaemon.Instance.SendCommand("?T");
            string humStr = await SerialDaemon.Instance.SendCommand("?H");

            temp.TempCelsius = Double.Parse(tmpStr, CultureInfo.InvariantCulture);
            temp.RelHumidity = Double.Parse(humStr, CultureInfo.InvariantCulture);

            return temp;
        }

        [Route("webapi/relaystatus"), HttpGet]
        public async Task<int[]> GetRelayStatus()
        {
            int[] statuses = new int[4];

            for (int i = 0; i < 4; i++)
            {
                string tmpStr = await SerialDaemon.Instance.SendCommand("?" + (i+1));
                statuses[i] = Int32.Parse(tmpStr, CultureInfo.InvariantCulture);
            }

            return statuses;
        }

        [Route("webapi/setrelay/{relay}/{onoff}"), HttpGet]
        public async Task SetRelay(int relay, int onoff)
        {
            string command = (onoff == 0 ? "O" : "C") + (relay + 1);

            await SerialDaemon.Instance.SendCommand(command);
        }

    }
}
