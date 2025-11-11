namespace ProjektZespolowyGr3.Models.DbModels
{
    public class ListingPhoto
    {
        public int Id { get; set; }
        public bool IsFeatured { get; set; } = false;

        public int ListingId { get; set; }
        public Listing Listing { get; set; }

        public int UploadId { get; set; }
        public Upload Upload { get; set; }
    }

}
