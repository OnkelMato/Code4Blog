using System.Buffers;
using MimeKit;
using SmtpServer;
using SmtpServer.ComponentModel;
using SmtpServer.Protocol;
using SmtpServer.Storage;

namespace SimpleSmtpServer
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Starting SMTP Server");

            var options = new SmtpServerOptionsBuilder()
                .ServerName("localhost")
                .Port(25)
                .Build();

            // this is used to intercept the processing and inject hooks
            var serviceProvider = new ServiceProvider();
            serviceProvider.Add(new SampleMessageStore());

            // let us start the server and wait
            var smtpServer = new SmtpServer.SmtpServer(options, serviceProvider);
            await smtpServer.StartAsync(CancellationToken.None);
        }
    }

    internal class SampleMessageStore : MessageStore
    {
        public override async Task<SmtpResponse> SaveAsync(
            ISessionContext context,
            IMessageTransaction transaction,
            ReadOnlySequence<byte> buffer,
            CancellationToken cancellationToken)
        {

            await using var stream = new MemoryStream();

            var position = buffer.GetPosition(0);
            while (buffer.TryGet(ref position, out var memory))
            {
                await stream.WriteAsync(memory, cancellationToken);
            }

            stream.Position = 0;

            var message = await MimeKit.MimeMessage.LoadAsync(stream, cancellationToken);
            Console.WriteLine("Mail received: " + message.Subject);
            foreach (var attachment in message.Attachments)
            {
                Console.WriteLine($"...Attachment found: {(attachment as MimePart)?.FileName} - {(attachment as MimePart)?.ContentType.MimeType}");
            }

            return SmtpResponse.Ok;
        }
    }
}
