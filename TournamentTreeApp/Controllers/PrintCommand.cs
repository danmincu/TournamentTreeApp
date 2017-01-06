using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TournamentsTreeApp.Controllers
{
    public class PrintCommand
    {
        public PrintCommand()
        {
            this.Documents = new List<Subdocument>();
        }
        public List<Subdocument> Documents { set; get; }
        public string Name { set; get; }
    }

    public class Subdocument
    {
        public string AddressBase64 { set; get; }
        public string Address { set; get; }
        public string PageSize { set; get; }
        public string Orientation { set; get; }

        public double MarginAll { set; get; }
        public double MarginTop { set; get; }
        public double MarginBottom { set; get; }
        public double MarginLeft { set; get; }
        public double MarginRight { set; get; }

        public double ZoomFactor { set; get; }

        public string GetAddress()
        {
            return this.Address ?? Base64Decode(this.AddressBase64);
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }
    }
}