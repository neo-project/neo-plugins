using System;
using System.Collections.Generic;
using Neo.FileStorage.API.Acl;
using Neo.FileStorage.API.Object;
using Neo.FileStorage.API.Refs;
using Neo.FileStorage.API.Session;
using Neo.FileStorage.Storage.LocalObjectStorage.Engine;
using static Neo.FileStorage.API.Acl.EACLRecord.Types;
using FSObject = Neo.FileStorage.API.Object;


namespace Neo.FileStorage.Storage.Services.Object.Acl.EAcl
{
    public class HeaderSource : IHeaderSource
    {
        private readonly ILocalHeadSource localStorage;
        private readonly IRequest req;
        private readonly IResponse resp;
        private readonly Address address;

        public HeaderSource(ILocalHeadSource local_storage, Address address, IRequest request, IResponse response)
        {
            localStorage = local_storage;
            this.address = address;
            req = request;
            resp = response;
        }

        public IEnumerable<XHeader> HeadersOfType(HeaderType type)
        {
            switch (type)
            {
                case HeaderType.Request:
                    {
                        return GetRequestXHeaders();
                    }
                case HeaderType.Object:
                    return ObjectHeaders();
                default:
                    return null;
            };
        }

        private List<XHeader> GetRequestXHeaders()
        {
            List<XHeader> result = new();
            for (var meta = req.MetaHeader; meta is not null; meta = meta.Origin)
            {
                result.AddRange(meta.XHeaders);
            }
            return result;
        }

        private List<XHeader> ObjectHeaders()
        {
            Address addr = address ?? new();
            if (resp is not null)
            {
                switch (resp)
                {
                    case GetResponse getResponse:
                        if (getResponse.Body.ObjectPartCase == GetResponse.Types.Body.ObjectPartOneofCase.Init)
                        {
                            var init = getResponse.Body.Init;
                            var obj = new FSObject.Object
                            {
                                ObjectId = init.ObjectId,
                                Header = init.Header,
                            };
                            return HeadersFromObject(obj, addr);
                        }
                        break;
                    case HeadResponse headResponse:
                        {
                            var obj = new FSObject.Object();
                            var hdr = new Header();
                            switch (headResponse.Body.HeadCase)
                            {
                                case HeadResponse.Types.Body.HeadOneofCase.ShortHeader:
                                    {
                                        var shortHeader = headResponse.Body.ShortHeader;
                                        hdr.ContainerId = addr.ContainerId;
                                        hdr.Version = shortHeader.Version;
                                        hdr.CreationEpoch = shortHeader.CreationEpoch;
                                        hdr.OwnerId = shortHeader.OwnerId;
                                        hdr.ObjectType = shortHeader.ObjectType;
                                        hdr.PayloadLength = shortHeader.PayloadLength;
                                        break;
                                    }
                                case HeadResponse.Types.Body.HeadOneofCase.Header:
                                    {
                                        hdr = headResponse.Body.Header.Header;
                                        break;
                                    }
                                default:
                                    throw new InvalidOperationException(nameof(ObjectHeaders) + " invalid head response type");
                            }
                            obj.Header = hdr;
                            return HeadersFromObject(obj, addr);
                        }
                    default:
                        return LocalObjectHeaders(address);
                }
            }
            else
            {
                switch (req)
                {
                    case GetRequest _:
                    case HeadRequest _:
                        return LocalObjectHeaders(address);
                    case DeleteRequest _:
                    case GetRangeRequest _:
                    case GetRangeHashRequest _:
                        return AddressHeaders(address);
                    case PutRequest putRequest:
                        if (putRequest.Body.ObjectPartCase == PutRequest.Types.Body.ObjectPartOneofCase.Init)
                        {
                            var init = putRequest.Body.Init;
                            var obj = new FSObject.Object
                            {
                                ObjectId = init.ObjectId,
                                Header = init.Header,
                            };
                            if (addr is null)
                            {
                                addr = new()
                                {
                                    ContainerId = init.Header.ContainerId,
                                    ObjectId = init.ObjectId,
                                };
                            }
                            return HeadersFromObject(obj, addr);
                        }
                        break;
                    case SearchRequest searchRequest:
                        return new() { ContainerIDHeader(searchRequest.Body.ContainerId) };
                    default:
                        throw new InvalidOperationException(nameof(ObjectHeaders) + " unexpected message type");
                }
            }
            return new();
        }

        private List<XHeader> LocalObjectHeaders(Address address)
        {
            try
            {
                var obj = localStorage.Head(address);
                return HeadersFromObject(obj, address);
            }
            catch (Exception)
            {
                return AddressHeaders(address);
            }
        }

        private List<XHeader> HeadersFromObject(FSObject.Object obj, Address address)
        {
            var headers = new List<XHeader>();
            while (obj != null)
            {
                headers.Add(ContainerIDHeader(address.ContainerId));
                headers.Add(new XHeader
                {
                    Key = Filter.FilterObjectOwnerID,
                    Value = obj.OwnerId.ToBase58String(),
                });
                headers.Add(new XHeader
                {
                    Key = Filter.FilterObjectCreationEpoch,
                    Value = obj.CreationEpoch.ToString(),
                });
                headers.Add(new XHeader
                {
                    Key = Filter.FilterObjectPayloadLength,
                    Value = obj.PayloadSize.ToString(),
                });
                headers.Add(ObjectIDHeader(address.ObjectId));
                // TODO: add others fields after neofs-api#84

                foreach (var attr in obj.Attributes)
                {
                    headers.Add(new XHeader
                    {
                        Key = attr.Key,
                        Value = attr.Value,
                    });
                }
                obj = obj.Parent;
            }
            return headers;
        }

        private XHeader ContainerIDHeader(ContainerID cid)
        {
            return new()
            {
                Key = Filter.FilterObjectContainerID,
                Value = cid.ToBase58String(),
            };
        }

        private XHeader ObjectIDHeader(ObjectID oid)
        {
            return new()
            {
                Key = Filter.FilterObjectID,
                Value = oid.ToBase58String(),
            };
        }

        private List<XHeader> AddressHeaders(Address address)
        {
            List<XHeader> result = new()
            {
                ContainerIDHeader(address.ContainerId),
            };
            if (address.ObjectId is not null) result.Add(ObjectIDHeader(address.ObjectId));
            return result;
        }
    }
}
