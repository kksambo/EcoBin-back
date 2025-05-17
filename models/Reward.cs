namespace WasteManagement.Models
{
    public class Reward
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public double PointsRequired { get; set; }
        public string? Description { get; set; }

        public double Amount { get; set; }  

        public string? UserEmail { get; set; } 

        public Reward(string name, double pointsRequired, string description,String userEmail)
        {
            Name = name;
            PointsRequired = pointsRequired;
            Description = description;
            UserEmail = userEmail; 
            Amount = pointsRequired;
        }

  



        public Reward() { }
    }
}