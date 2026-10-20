using MongoDB.Bson.Serialization.Attributes;

namespace Ticketing.Common.Messages
{
    public abstract class Message
    {
        [BsonId]
        public string Id {get; set;} 

        protected Message(){}

    }
    
}


