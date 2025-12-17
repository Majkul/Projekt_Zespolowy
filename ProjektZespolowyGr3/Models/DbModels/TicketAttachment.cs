namespace ProjektZespolowyGr3.Models.DbModels
{
    public class TicketAttachment
    {
        public int Id { get; set; }

        public int TicketId { get; set; }
        public Ticket Ticket { get; set; }

        public int UploadId { get; set; }
        public Upload Upload { get; set; }
    }

}
