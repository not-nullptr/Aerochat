namespace Aerochat.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class MultiStringToIntAction : Attribute
    {
        public Func<List<string>> GetDisplayNames;

        public MultiStringToIntAction(string action)
        {
            switch (action) {
                case "FetchInputDevices":
                    GetDisplayNames = Aerovoice.Recorders.InputDevices.FetchInputDevices;
                    break;
                default:
                    throw new ArgumentException("Invalid action specified for MultiStringToIntAction attribute.");
            }
        }
    }
}
