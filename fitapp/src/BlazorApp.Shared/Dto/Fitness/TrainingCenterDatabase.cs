using System.Collections.Generic;

namespace BlazorApp.Shared.Dto.Fitness
{
    public class TrainingCenterDatabase
    {
        public Author Author { get; set; }
        public List<Activity> Activities { get; set; } = new List<Activity>();
    }
}
