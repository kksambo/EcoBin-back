namespace WasteManagement.Models
{
    public class SmartBin
    {
        public int Id { get; set; }
        public string binCode { get; set; }

        public string location { get; set; }
        public string availability { get; set; } 
        public double capacity { get; set; }
        public double currentWeight { get; set; }

        public SmartBin(string binCode, string location, string availability, double capacity, double currentWeight)
        {
            this.binCode = binCode;
            this.location = location;
            this.availability = availability;
            this.capacity = capacity;
            this.currentWeight = currentWeight;
        }


        public SmartBin() { }
    }
}