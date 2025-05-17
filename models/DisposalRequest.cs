namespace WasteManagement.Models
{
    public class DepositRequest
    {
     

        public int Id { get; set; }
        public int BinId { get; set; }
        public double Weight { get; set; }
        public DateTime RequestDate { get; set; } = DateTime.Now;
        public bool IsApproved { get; set; } = true;

        public DepositRequest(int binId, double weight)
        {
            BinId = binId;
            Weight = weight;
        }

        public DepositRequest() { }


    }
  
}