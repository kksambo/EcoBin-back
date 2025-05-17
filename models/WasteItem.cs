namespace WasteManagement.Models
{
    public class WasteItem
    {
        public int Id { get; set; }
       
        public string Type { get; set; }

        public int weight { get; set; }

        public WasteItem(string type, int weight)
        {
            Type = type;
            this.weight = weight;
        }

        public WasteItem() { } 
    }
}