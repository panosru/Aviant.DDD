namespace Aviant.DDD.Core.ValueObjects
{
    public interface IValueObject
    {
        public bool Equals(ValueObject obj);

        public int GetHashCode();

        public int HashValue(int seed, object value);
    }
}