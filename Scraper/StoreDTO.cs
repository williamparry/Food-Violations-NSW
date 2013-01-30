using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scraper
{
    public class StoreDTO : IEquatable<StoreDTO>
	{
		public string TradeName;
		public string Address;
		public string Lat;
		public string Lng;
		public string Council;
        public List<OffencesDTO> Offences = new List<OffencesDTO>();

        public bool Equals(StoreDTO other)
        {
            return this.TradeName == other.TradeName;
        }
    }
}
