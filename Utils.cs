namespace VainBotTwitch
{
    public static class Utils
    {
        public static string ToDisplayString(this decimal count)
        {
            var display = count.GetNumberString() + " ";

            if (count == 1)
                display += "slothy";
            else
                display += "slothies";

            return display;
        }

        static string GetNumberString(this decimal num)
        {
            if ((int)num == num)
                return ((int)num).ToString();
            else if (num == 3.14M)
                return "π";
            else
                return num.ToString();
        }
    }
}
