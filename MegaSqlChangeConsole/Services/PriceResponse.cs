namespace BingXB.Model.PriceResponses
{
    public class Datum
    {
        public string symbol { get; set; }
        public List<Trade> trades { get; set; }
    }

    public class PriceResponse
    {
        public int code { get; set; }
        public long timestamp { get; set; }
        public List<Datum> data { get; set; }
    }

    public class Trade
    {
        public long timestamp { get; set; }
        public string tradeId { get; set; }
        public string price { get; set; }
        public string amount { get; set; }
        public string type { get; set; }
        public string volume { get; set; }
    }
}
