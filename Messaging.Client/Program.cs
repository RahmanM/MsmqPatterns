using System;
using System.Messaging;
using System.Threading;
using Messaging.Msmq;

namespace Messaging.Client
{
    public class Program
    {
        static void Main(string[] args)
        {
            FireAndForgetExample();

            RequestResponseExample();

            Console.ReadLine();

        }

        private static void RequestResponseExample()
        {
            // Send the request to request Queue and provide the response queue in the message
            // Handler will pick the message from the Request Queue
            // Handler will process the message and send message and set the correlatedId based on the message of the request message id
            // Requester will read the response Queue and read the response based on the correlated id.
            var requester = new MsmqRequestResponse(@".\private$\requestQueue", @".\private$\responseQueue");

            var message = new Message
            {
                Body = "Hello World"
            };

            var sentMessage = requester.Send(message);

            Console.WriteLine("Sent request");
            Console.WriteLine(@"	Time:       {0}", DateTime.Now.ToString("HH:mm:ss.ffffff"));
            Console.WriteLine(@"	Message ID: {0}", sentMessage.Id);
            Console.WriteLine(@"	Correl. ID: {0}", sentMessage.CorrelationId);
            Console.WriteLine(@"	Reply to:   {0}", sentMessage.ResponseQueue.Path);
            Console.WriteLine(@"	Contents:   {0}", sentMessage.Body.ToString());

            Thread.Sleep(15000); // Simulating delay

            var receivedMessage = requester.Receive(sentMessage.Id);
            Console.WriteLine(@"Received reply");
            Console.WriteLine(@"	Time:       {0}", DateTime.Now.ToString("HH:mm:ss.ffffff"));
            Console.WriteLine(@"	Correl. ID: {0}", receivedMessage.CorrelationId);
            Console.WriteLine(@"	Reply to:   {0}", "<n/a>");
            Console.WriteLine(@"	Contents:   {0}", receivedMessage.Body.ToString());

        }

        private static void FireAndForgetExample()
        {
            var message = new Message()
            {
                Body = "rahman.mahmoodi@gmail.com"
            };

            var helper = new MsmqFireAndForgetHelper(@".\private$\unsubscribenewsletter", true);

            helper.BeginTransaction();
            helper.Send(message);
            helper.CommitTransaction();

            var reader = new MsmqFireAndForgetHelper(@".\private$\unsubscribenewsletter", true);
            reader.BeginTransaction();
            var result = reader.Receive();
            Console.WriteLine(result);
            reader.CommitTransaction();
            
            Console.WriteLine("Message is sent to Queue");
        }
    }
   
}
