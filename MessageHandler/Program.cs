using System;
using System.Collections.Generic;
using System.Linq;
using System.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MessageHandler
{
    class Program
    {
        static void Main(string[] args)
        {
            var handler = new MessageHandler();
            handler.ReceiveAcknowledgment(@".\private$\requestQueue");
        }

        /// <summary>
        /// This is a test class that processes the messages from the request queue
        /// </summary>
        public class MessageHandler
        {
            public void ReceiveAcknowledgment(string queuePath)
            {
                bool found = false;
                MessageQueue queue = new MessageQueue(queuePath)
                {
                    MessageReadPropertyFilter =
                    {
                        CorrelationId = true,
                        Acknowledgment = true
                    }
                };

                try
                {
                    while (true)
                    {
                        if (MessageQueue.Exists(queuePath))
                        {
                            Thread.Sleep(10000); // Simulating delay

                            var message = queue.Receive();
                            if (message != null)
                            {
                                // Process the message => Do something interesting!!!
                                Console.WriteLine("Acknowledgment Message from Handler ...");
                                Console.WriteLine("Correlation Id: " + message.CorrelationId);
                                var messageId = message.Id;
                                Console.WriteLine("Received Message Id: " + messageId);
                                Console.WriteLine("Acknowledgment Type: " + message.Acknowledgment);

                                // Send response
                                var responseQueue = message.ResponseQueue;
                                var response = new Message("Happy response for message " + messageId)
                                {
                                    CorrelationId = messageId // relate the response to message
                                };
                                responseQueue.Send(response);
                            }
                        }

                    }
                }
                catch (InvalidOperationException e)
                {
                    // This exception would be thrown if there is no (further) acknowledgment message
                    // with the specified correlation Id. Only output a message if there are no messages;
                    // not if the loop has found at least one.
                    if (found == false)
                    {
                        Console.WriteLine(e.Message);
                    }

                    // Handle other causes of invalid operation exception.
                }

            }
        }

    }
}
