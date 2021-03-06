namespace Rachis.Messages
{
    public class AppendEntriesResponse : BaseMessage
    {
        public long CurrentTerm { get; set; }

        public long LastLogIndex { get; set; }

        public bool Success { get; set; }

        public string Message { get; set; }
        public string LeaderId { get; set; }
    }
}
