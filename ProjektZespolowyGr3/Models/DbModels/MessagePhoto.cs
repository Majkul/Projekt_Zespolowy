namespace ProjektZespolowyGr3.Models.DbModels
{
    public class MessagePhoto
    {
        public int Id { get; set; }

        public int MessageId { get; set; }
        public Message Message { get; set; } = null!;

        public int UploadId { get; set; }
        public Upload Upload { get; set; } = null!;
    }

}
