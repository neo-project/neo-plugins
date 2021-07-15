using System;
using System.Collections.Generic;
using System.Linq;
using Neo.FileStorage.API.Cryptography;
using Neo.FileStorage.API.Object;
using Neo.FileStorage.API.Refs;
using Neo.FileStorage.API.StorageGroup;
using Neo.FileStorage.API.Tombstone;
using Neo.FileStorage.Morph.Invoker;
using FSObject = Neo.FileStorage.API.Object.Object;

namespace Neo.FileStorage.Core.Object
{
    public class ObjectValidator
    {
        private readonly IObjectDeleteHandler deleteHandler;
        private readonly MorphInvoker morphClient;

        public ObjectValidator(IObjectDeleteHandler handler, MorphInvoker client)
        {
            deleteHandler = handler;
            morphClient = client;
        }

        public bool Validate(FSObject obj)
        {
            if (obj is null)
                return false;
            else if (obj.ObjectId is null)
                return false;
            else if (obj.Header is null || obj.Header.ContainerId is null)
                return false;

            while (obj != null)
            {
                obj = obj.Parent;
                if (!ValidateSignatureKey(obj)) return false;
                if (!CheckExpiration(obj)) return false;
                if (!obj.CheckVerificationFields()) return false;
            }
            return true;
        }

        private bool ValidateSignatureKey(FSObject obj)
        {
            var token = obj.SessionToken;
            var key = obj.Signature.Key;

            if (token is null || !token.Body.SessionKey.Equals(key))
                return obj.OwnerId.Equals(key.ToByteArray().PublicKeyToOwnerID());

            // TODO: perform token verification
            return true;
        }

        public bool CheckExpiration(FSObject obj)
        {
            try
            {
                var expire = ExpirationEpochAttributeValue(obj);
                if (expire < CurrentEpoch()) return false;
            }
            catch (Exception e)
            {
                Utility.Log(nameof(ObjectValidator), LogLevel.Warning, e.Message);
                return false;
            };
            return true;
        }

        private ulong ExpirationEpochAttributeValue(FSObject obj)
        {
            var expires = obj.Attributes.Where(p => p.Key == Header.Types.Attribute.SysAttributeExpEpoch).ToList();
            if (!expires.Any()) throw new InvalidOperationException();
            return ulong.Parse(expires[0].Value);
        }

        public bool ValidateContent(FSObject obj)
        {
            switch (obj.ObjectType)
            {
                case ObjectType.Tombstone:
                    if (!obj.Payload.Any()) return false;
                    Tombstone tombstone = Tombstone.Parser.ParseFrom(obj.Payload);
                    ulong exp;
                    try
                    {
                        exp = ExpirationEpochAttributeValue(obj);
                    }
                    catch (Exception e)
                    {
                        Utility.Log(nameof(ObjectValidator), LogLevel.Warning, e.Message);
                        return false;
                    }
                    if (exp != tombstone.ExpirationEpoch)
                        return false;
                    var cid = obj.ContainerId;
                    var id_list = tombstone.Members.ToList();
                    List<Address> address_list = new();
                    address_list.Add(obj.Address);
                    foreach (var id in id_list)
                    {
                        if (id is null) return false;
                        Address address = new(cid, id);
                        address_list.Add(address);
                    }
                    if (deleteHandler != null)
                        deleteHandler.DeleteObjects(address_list.ToArray());
                    break;
                case ObjectType.StorageGroup:
                    if (!obj.Payload.Any()) return false;
                    StorageGroup sg = StorageGroup.Parser.ParseFrom(obj.Payload.ToByteArray());
                    foreach (var id in sg.Members)
                    {
                        if (id is null) return false;
                    }
                    break;
            }
            return true;
        }

        private ulong CurrentEpoch()
        {
            return morphClient.Epoch();
        }
    }
}
