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
        Lose,
        Void
    }

    public enum SlothyBetStatus
    {
        /// <summary>
        /// Betting is currently closed and no bets are placed to be processed in the future.
        /// </summary>
        Closed,

        /// <summary>
        /// Betting is currently open.
        /// </summary>
        Open,

        /// <summary>
        /// Betting is closed, but bets have been placed to be processed in the future.
        /// </summary>
        InProgress
    }
}
