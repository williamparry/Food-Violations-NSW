using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using HtmlAgilityPack;
using Newtonsoft.Json;
using System.IO;
using System.Threading;

namespace Scraper
{

	

	class Program
	{


        public static List<StoreDTO> StoresList = new List<StoreDTO>();

		public static bool IsProcessing = true;

		static string BASE = "http://www.foodauthority.nsw.gov.au/penalty-notices/";
		static int navCount = 0;
		static int navProcess = 0;

		static void Main(string[] args)
		{

			Thread geocodeThread = new Thread(new ThreadStart(Geocode.Work));

			geocodeThread.Start();

			while (!geocodeThread.IsAlive) ;

			HtmlWeb client = new HtmlWeb();
			HtmlDocument doc = client.Load(BASE + "default.aspx?template=results&abc=show&abc-field=Offence_City&startswith=A,Offence_City");
			HtmlNodeCollection nav = doc.DocumentNode.SelectNodes("//div[@id='abc']//a");

			navCount = nav.Count;

			foreach (HtmlNode navLink in nav)
			{
				//if(navProcess == 1) break;

				navProcess++;

				Console.WriteLine("Processing " + navProcess + "/" + navCount);

				string suburbURL = navLink.Attributes["href"].Value;
				HtmlNodeCollection suburb = GetSuburb(suburbURL);

				if(suburb == null) continue;


				int storeCount = suburb.Count;
				int storeProcess = 0;
				foreach (HtmlNode stores in suburb)
				{

					storeProcess++;

					Console.WriteLine("\t> Processing " + storeProcess + "/" + storeCount);

					string storeDetailsURL = stores.Attributes["href"].Value;
					HtmlNode storeDetails = GetDetails(storeDetailsURL);

                    string storeKey = storeDetails.SelectSingleNode("//tr[2]/td[2]").InnerText;

                    StoreDTO processStore = StoresList.Find(item => item.TradeName == storeKey);

                    if (processStore == null)
                    {
                        lock (StoresList)
                        {

                            processStore = new StoreDTO
                            {
                                TradeName = storeKey,
                                Address = storeDetails.SelectSingleNode("//tr[3]/td[2]").InnerText,
                                Council = storeDetails.SelectSingleNode("//tr[4]/td[2]").InnerText,
                            };

                            StoresList.Add(processStore);
                        }
                    }

                    OffencesDTO offence = new OffencesDTO
                    {
                        PenaltyNoticeNumber = storeDetails.SelectSingleNode("//tr[1]/td[2]").InnerText,
                        DateAlleged = storeDetails.SelectSingleNode("//tr[5]/td[2]").InnerText,
                        OffenceCode = storeDetails.SelectSingleNode("//tr[6]/td[2]").InnerText,
                        NatureCircumstances = storeDetails.SelectSingleNode("//tr[7]/td[2]").InnerText,
                        PenaltyAmount = storeDetails.SelectSingleNode("//tr[8]/td[2]").InnerText,
                        NamePartyServed = storeDetails.SelectSingleNode("//tr[9]/td[2]").InnerText,
                        DatePenaltyNoticeServed = storeDetails.SelectSingleNode("//tr[10]/td[2]").InnerText,
                        IssuedBy = storeDetails.SelectSingleNode("//tr[11]/td[2]").InnerText,
                        Notes = storeDetails.SelectSingleNode("//tr[12]/td[2]").InnerText
                    };

                    processStore.Offences.Add(offence);
                    

				}

			}

			IsProcessing = false;
			geocodeThread.Join();

			Console.WriteLine("Waiting for geocoder");
			File.WriteAllText("data.json", JsonConvert.SerializeObject(StoresList));
			Console.WriteLine("Finished");
			Console.ReadLine();

		}

		static HtmlNodeCollection GetSuburb(string URL)
		{
			HtmlWeb client = new HtmlWeb();
			string suburbURL = System.Net.WebUtility.HtmlDecode(BASE + URL);
			HtmlDocument doc = client.Load(suburbURL);
			return doc.DocumentNode.SelectNodes("//table[@id='myTable']/tbody/tr/td[4]/a");
		}
		
		static HtmlNode GetDetails(string URL)
		{
			HtmlWeb client = new HtmlWeb();
			string storeURL = System.Net.WebUtility.HtmlDecode(BASE + URL);
			HtmlDocument doc = client.Load(storeURL);

			return doc.DocumentNode.SelectSingleNode("//table[@class='table-data-pd']");
		}

	}

	public static class Geocode
	{
        public static int Processed = 0;

		public static void Work() {
			
			WebClient client = new WebClient();

			while (Program.IsProcessing)
			{
				if (Program.StoresList.Count > Processed)
				{

                    lock (Program.StoresList)
                    {
                        StoreDTO store = Program.StoresList.ElementAt(Processed++);

                        string geocodeURL = "http://maps.googleapis.com/maps/api/geocode/json?address=" + store.Address + ",NSW, Australia&sensor=false";

                        dynamic latLong = JsonConvert.DeserializeObject(client.DownloadString(geocodeURL));

                        if (latLong["results"].Count == 0) continue;

                        store.Lat = Convert.ToString(latLong["results"][0]["geometry"]["location"]["lat"]);
                        store.Lng = Convert.ToString(latLong["results"][0]["geometry"]["location"]["lng"]);

                    }
                    Console.WriteLine("\t\t >> Geocoding processed " + Processed);

				}
			}
		}
	}

}
