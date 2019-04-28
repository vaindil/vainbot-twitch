using System;

namespace VainBotTwitch.Classes
{
    public class SlothyBetRecord
    {
        public string UserId { get; set; }

        public SlothyBetType BetType { get; set; }

        public decimal Amount { get; set; }

        public string BetTypeString
        {
            get
            {
                return Enum.GetName(typeof(SlothyBetType), BetType).ToLower();
            }
        }
    }

    public enum SlothyBetType
    {
        Win,
        Lose
    }
}
