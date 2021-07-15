using Neo.FileStorage.API.Cryptography;
using Neo.FileStorage.API.Session;
using System;
using System.Security.Cryptography;

namespace Neo.FileStorage.Storage.Services
{
    public abstract class SignService
    {
        public ECDsa Key { get; init; }

        protected IResponse HandleUnaryRequest(IRequest request, Func<IRequest, IResponse> handler)
        {
            if (!request.VerifyRequest()) throw new Exception($"{nameof(SignService)} invalid {request.GetType()}");
            var resp = handler(request);
            Key.SignResponse(resp);
            return resp;
        }

        protected Action<IResponse> HandleServerStreamRequest(IRequest request, Action<IResponse> handler)
        {
            if (!request.VerifyRequest()) throw new Exception($"{nameof(SignService)} invalid {request.GetType()}");
            return resp =>
            {
                Key.SignResponse(resp);
                handler(resp);
            };
        }

        protected RequestSignStream CreateRequestStreamer(Action<IRequest> sender, Func<IResponse> closer)
        {
            return new()
            {
                Key = Key,
                Sender = sender,
                Closer = closer,
            };
        }
    }

    public class RequestSignStream : IRequestStream
    {
        public ECDsa Key { get; init; }

        public Action<IRequest> Sender { get; init; }

        public Func<IResponse> Closer { get; init; }

        public void Send(IRequest request)
        {
            if (!request.VerifyRequest()) throw new Exception($"{nameof(RequestSignStream)} invalid {request.GetType()}");
            Sender(request);
        }

        public IResponse Close()
        {
            var resp = Closer();
            Key.SignResponse(resp);
            return resp;
        }
    }
}
