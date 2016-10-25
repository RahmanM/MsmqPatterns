using System;
using System.Messaging;

namespace Messaging.Msmq
{
    public interface IMsmqHelper
    {
        void Send(object message);
        void BeginTransaction();
        void CommitTransaction();
        object Receive();
    }

    /// <summary>
    /// Wrapper around MSMQ that enables to create queue if not exists, handle transactional and durable queues
    /// This is an example of Fire-and-Forget pattern
    /// </summary>
    public class MsmqFireAndForgetHelper : IMsmqHelper
    {
        private readonly string QueueName;
        private readonly bool Transactional;
        private bool IsPersistent { get; set; }

        public MsmqFireAndForgetHelper(
            string queueName,
            bool transactional)
        {
            QueueName = queueName;
            Transactional = transactional;

            CreateQueueIfNotExists(QueueName);
        }

        public MsmqFireAndForgetHelper(
            string queueName,
            bool transactional,
            bool isPersistent)
            : this(queueName, transactional)
        {
            this.IsPersistent = isPersistent;
        }

        public void Send(object message)
        {
            SendMessage(message, this.Transactional);
        }

        public void BeginTransaction()
        {
            if (QueueTransaction != null)
            {
                throw new InvalidOperationException(Resource.MsmqHelper_BeginTransaction_Transaction_is_already_initialized___);
            }

            QueueTransaction = new MessageQueueTransaction();
            QueueTransaction.Begin();
        }

        public MessageQueueTransaction QueueTransaction { get; set; }

        public void CommitTransaction()
        {
            if (QueueTransaction == null)
            {
                throw new InvalidOperationException(Resource.MsmqHelper_CommitTransaction_Please_use_BeginTransaction_before_committing_the_transaction);
            }

            QueueTransaction.Commit();
        }

        public object Receive()
        {
            if (!MessageQueue.Exists(QueueName)) return null;
            using (var queue = new MessageQueue(QueueName))
            {
                // define the formatter to get the message back otherwise message will be null!!
                queue.Formatter = new XmlMessageFormatter(new Type[] { typeof(String) });

                if (this.Transactional)
                {
                    // If transactional queue then transaction is required
                    if (QueueTransaction == null)
                    {
                        throw new InvalidOperationException(Resource.MsmqHelper_SendMessage_Please_use_BeginTransaction_before_sending_the_message);
                    }

                    var message = queue.Receive(QueueTransaction);
                    if (message != null) return message.Body;
                }
                else
                {
                    var message = queue.Receive();
                    if (message != null) return message.Body;
                }
            }

            return null;
        }

        #region Helpers

        private void CreateQueueIfNotExists(string queueName)
        {
            if (!MessageQueue.Exists(queueName))
            {
                MessageQueue.Create(queueName, Transactional);
            }
        }

        private void SendMessage(object message, bool withTransaction)
        {
            if (MessageQueue.Exists(QueueName))
            {
                using (var queue = new MessageQueue(QueueName))
                {
                    queue.DefaultPropertiesToSend.Recoverable = this.IsPersistent;

                    if (withTransaction)
                    {
                        if (QueueTransaction == null)
                        {
                            throw new InvalidOperationException(Resource.MsmqHelper_SendMessage_Please_use_BeginTransaction_before_sending_the_message);
                        }

                        queue.Send(message, QueueTransaction);
                    }
                    else
                    {
                        queue.Send(message);
                    }
                }
            }
        }

        #endregion

    }

}