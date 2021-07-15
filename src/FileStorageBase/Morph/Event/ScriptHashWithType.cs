namespace Neo.FileStorage.Morph.Event
{
    public class ScriptHashWithType
    {
        public UInt160 ScriptHashValue;
        public string Type;

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            ScriptHashWithType oth = (ScriptHashWithType)obj;
            return oth.ScriptHashValue == ScriptHashValue && oth.Type == Type;
        }

        public override int GetHashCode()
        {
            return ScriptHashValue.GetHashCode() + Type.GetHashCode();
        }
    }
}
