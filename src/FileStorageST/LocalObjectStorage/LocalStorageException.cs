#nullable enable
using System;
using Neo.FileStorage.API.Object;

namespace Neo.FileStorage.Storage.LocalObjectStorage
{
    public class LocalStorageException : Exception
    {
        public LocalStorageException() : base() { }
        public LocalStorageException(string? message) : base(message) { }
    }

    public class ObjectAlreadyRemovedException : LocalStorageException
    {
        public ObjectAlreadyRemovedException() : base() { }
        public ObjectAlreadyRemovedException(string? message) : base(message) { }
    }

    public class SplitInfoException : LocalStorageException
    {
        public SplitInfo SplitInfo;

        public SplitInfoException(SplitInfo sp) : base() { SplitInfo = sp; }
        public SplitInfoException(SplitInfo sp, string? message) : base(message) { SplitInfo = sp; }
    }

    public class RangeOutOfBoundsException : LocalStorageException
    {
        public RangeOutOfBoundsException() : base() { }
        public RangeOutOfBoundsException(string? message) : base(message) { }
    }

    public class ObjectNotFoundException : LocalStorageException
    {
        public ObjectNotFoundException() : base() { }
        public ObjectNotFoundException(string? message) : base(message) { }
    }

    public class BlobFullException : LocalStorageException
    {
        public BlobFullException() : base() { }
        public BlobFullException(string? message) : base(message) { }
    }

    public class ObjectSizeExceedLimitException : LocalStorageException
    {
        public ObjectSizeExceedLimitException() : base() { }
        public ObjectSizeExceedLimitException(string? message) : base(message) { }
    }

    public class ObjectFileNotFoundException : LocalStorageException
    {
        public ObjectFileNotFoundException() : base() { }
        public ObjectFileNotFoundException(string? message) : base(message) { }
    }

    public class UnknownObjectTypeException : LocalStorageException
    {
        public UnknownObjectTypeException() : base() { }
        public UnknownObjectTypeException(string? message) : base(message) { }
    }

    public class IterateBreakException : LocalStorageException
    {
        public IterateBreakException() : base() { }
        public IterateBreakException(string? message) : base(message) { }
    }
}
