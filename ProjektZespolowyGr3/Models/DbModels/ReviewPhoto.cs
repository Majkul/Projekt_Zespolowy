namespace ProjektZespolowyGr3.Models.DbModels
{
    public class ReviewPhoto
    {
        public int Id { get; set; }

        public int ReviewId { get; set; }
        public Review Review { get; set; }

        public int UploadId { get; set; }
        public Upload Upload { get; set; }
    }

}
