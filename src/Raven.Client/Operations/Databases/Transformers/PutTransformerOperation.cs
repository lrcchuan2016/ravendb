using System;
using System.Net.Http;
using Raven.Client.Blittable;
using Raven.Client.Commands;
using Raven.Client.Data.Transformers;
using Raven.Client.Document;
using Raven.Client.Http;
using Raven.Client.Indexing;
using Raven.Client.Json;
using Sparrow.Json;

namespace Raven.Client.Operations.Databases.Transformers
{
    public class PutTransformerOperation : IAdminOperation<PutTransformerResult>
    {
        private readonly string _transformerName;
        private readonly TransformerDefinition _transformerDefinition;

        public PutTransformerOperation(string transformerName, TransformerDefinition transformerDefinition)
        {
            if (transformerName == null)
                throw new ArgumentNullException(nameof(transformerName));
            if (transformerDefinition == null)
                throw new ArgumentNullException(nameof(transformerDefinition));

            _transformerName = transformerName;
            _transformerDefinition = transformerDefinition;
        }

        public RavenCommand<PutTransformerResult> GetCommand(DocumentConventions conventions, JsonOperationContext context)
        {
            return new PutTransformerCommand(conventions, context, _transformerName, _transformerDefinition);
        }

        private class PutTransformerCommand : RavenCommand<PutTransformerResult>
        {
            private readonly JsonOperationContext _context;
            private readonly string _transformerName;
            private readonly BlittableJsonReaderObject _transformerDefinition;

            public PutTransformerCommand(DocumentConventions conventions, JsonOperationContext context, string transformerName, TransformerDefinition transformerDefinition)
            {
                if (conventions == null)
                    throw new ArgumentNullException(nameof(conventions));
                if (context == null)
                    throw new ArgumentNullException(nameof(context));
                if (transformerName == null)
                    throw new ArgumentNullException(nameof(transformerName));
                if (transformerDefinition == null)
                    throw new ArgumentNullException(nameof(transformerDefinition));

                _context = context;
                _transformerName = transformerName;
                _transformerDefinition = new EntityToBlittable(null).ConvertEntityToBlittable(transformerDefinition, conventions, _context);
            }

            public override HttpRequestMessage CreateRequest(ServerNode node, out string url)
            {
                url = $"{node.Url}/databases/{node.Database}/transformers?name=" + Uri.EscapeUriString(_transformerName);

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Put,
                    Content = new BlittableJsonContent(stream =>
                    {
                        _context.Write(stream, _transformerDefinition);
                    })
                };

                return request;
            }

            public override void SetResponse(BlittableJsonReaderObject response, bool fromCache)
            {
                Result = JsonDeserializationClient.PutTransformerResult(response);
            }

            public override bool IsReadRequest => false;
        }
    }
}