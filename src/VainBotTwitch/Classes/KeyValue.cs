namespace VainBotTwitch.Classes
{
    public class KeyValue
    {
        public KeyValue()
        {
        }

        public KeyValue(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public string Key { get; set; }

        public string Value { get; set; }
    }
}
