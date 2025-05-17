namespace WasteManagement.Models
{
    public class Point
    {
        public string? UserEmail { get; set; } 
        public double Points { get; set; } = 0; 
  

        public Point(string userEmail, double points, string description)
        {
            UserEmail = userEmail;
            Points = points;
      
        }

        public Point() { }
    }
}