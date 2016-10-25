using System;
using System.Messaging;

namespace Messaging.Msmq
{

    public interface IRequester
    {
        Message Send(Message message);
        Message Receive(string messageId);
    }

    /// <summary>
    /// Example of MSMQ request and response based communication
    /// Where the requester sends a message to request queue and provides
    /// the details of the response queue.
    /// The handler processes the message and creates relation between 
    /// request and response message by setting CorrelationId and sends message to response queue
    /// Requester again query the response queue based on CorrelationId and retrieves the message
    /// </summary>
    public class MsmqRequestResponse : IRequester
    {
        public bool Transactional { get; set; }
        private MessageQueue RequestQueue { get; set; }
        private MessageQueue ResponseQueue { get; set; }

        public MsmqRequestResponse(
            String requestQueueName, 
            String responseQueueName)
        {
            if (string.IsNullOrWhiteSpace(requestQueueName))
            {
                throw new ArgumentException("requestQueueName is required.");
            }
            if (string.IsNullOrWhiteSpace(responseQueueName))
            {
                throw new ArgumentException("responseQueueName is required.");
            }

            CreateQueueIfNotExists(requestQueueName);
            CreateQueueIfNotExists(responseQueueName);

            RequestQueue = new MessageQueue(requestQueueName);
            ResponseQueue = new MessageQueue(responseQueueName);

            ResponseQueue.MessageReadPropertyFilter.SetAll();
            ((XmlMessageFormatter)ResponseQueue.Formatter).TargetTypeNames = new string[] { "System.String,mscorlib" };

        }

        public Message Send(Message message)
        {
            if (message==null)
            {
                throw new ArgumentNullException("message");
            }

            message.ResponseQueue = this.ResponseQueue;
            RequestQueue.Send(message);
            return message;
        }

        public Message Receive(string messageId)
        {
            while (true)
            {
                try
                {
                    ResponseQueue.MessageReadPropertyFilter.CorrelationId = true;
                    // Query based on the specific message id
                    Message replyMessage = ResponseQueue.ReceiveByCorrelationId(messageId);
                    if (replyMessage != null)
                    {
                        return replyMessage;
                    }
                }
                catch (Exception e)
                {
                    if (e.Message.Contains("Message requested was not found in the queue specified."))
                    {
                        // Ignore this message
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        private void CreateQueueIfNotExists(string queueName)
        {
            if (!MessageQueue.Exists(queueName))
            {
                MessageQueue.Create(queueName, Transactional);
            }
        }
    }
}