using NServiceBus;
using SampleApp.Messages.File;
using System;
using System.Threading.Tasks;

namespace SampleApp
{
    class FileSaga : Saga<FileSaga.FileSagaData>, IAmStartedByMessages<StartFileImportMessage>
    {
        public Task Handle(StartFileImportMessage message, IMessageHandlerContext context)
        {
            Console.WriteLine("Got a start message");
            return Task.CompletedTask;
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<FileSagaData> mapper)
        {
            mapper.ConfigureMapping<StartFileImportMessage>(m => m.FileName)
                .ToSaga(s => s.FileName);
        }

        public class FileSagaData : ContainSagaData
        {
            public string FileName { get; set; }
        }
    }
}
