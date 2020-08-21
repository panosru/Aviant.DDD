namespace Aviant.DDD.Application.Processors
{
    using System.Threading;
    using System.Threading.Tasks;

    public abstract class RequestPreProcessor<TRequest> : IRequestPreProcessor<TRequest>
        where TRequest : notnull
    {
        public abstract Task Process(TRequest request, CancellationToken cancellationToken);
    }
}