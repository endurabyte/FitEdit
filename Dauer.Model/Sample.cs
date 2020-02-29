namespace Dauer.Model
{
    public interface ISample
    {
        System.DateTime When { get; set; }
    }

    public abstract class Sample : ISample
    {
        public System.DateTime When { get; set; }
    }
}
