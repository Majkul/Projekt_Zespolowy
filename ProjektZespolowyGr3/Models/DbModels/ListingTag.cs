namespace ProjektZespolowyGr3.Models.DbModels
{
    public class ListingTag
    {
        public int ListingId { get; set; }
        public Listing Listing { get; set; }

        public int TagId { get; set; }
        public Tag Tag { get; set; }
    }

}
