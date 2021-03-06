using System;
using System.Runtime.Serialization;

namespace Zen.Massage.Domain.UserBoundedContext
{
    /// <summary>
    /// <c>CustomerId</c> simple object to encapsulate a customer id.
    /// </summary>
    [Serializable]
    [DataContract]
    public struct CustomerId
    {
        /// <summary>
        /// The empty.
        /// </summary>
        public static readonly CustomerId Empty = new CustomerId(Guid.Empty);

        /// <summary>
        /// Initialises a new instance of the <see cref="CustomerId" /> struct.
        /// </summary>
        /// <param name="id">
        /// The id.
        /// </param>
        public CustomerId(Guid id)
            : this()
        {
            Id = id;
        }

        /// <summary>
        /// Gets the id.
        /// </summary>
        [DataMember(IsRequired = true)]
        public Guid Id { get; private set; }

        /// <summary>
        /// The ==.
        /// </summary>
        /// <param name="lhs">
        /// The lhs.
        /// </param>
        /// <param name="rhs">
        /// The rhs.
        /// </param>
        /// <returns>
        /// </returns>
        public static bool operator ==(CustomerId lhs, CustomerId rhs)
        {
            return lhs.Equals(rhs);
        }

        /// <summary>
        /// The !=.
        /// </summary>
        /// <param name="lhs">
        /// The lhs.
        /// </param>
        /// <param name="rhs">
        /// The rhs.
        /// </param>
        /// <returns>
        /// </returns>
        public static bool operator !=(CustomerId lhs, CustomerId rhs)
        {
            return !lhs.Equals(rhs);
        }

        /// <summary>
        /// The equals.
        /// </summary>
        /// <param name="obj">
        /// The obj.
        /// </param>
        /// <returns>
        /// The <see cref="bool" />.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (obj is CustomerId)
            {
                var rhs = (CustomerId)obj;
                if (rhs.Id == Id)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// The get hash code.
        /// </summary>
        /// <returns>
        /// The <see cref="int" />.
        /// </returns>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        /// <summary>
        /// The to string.
        /// </summary>
        /// <returns>
        /// The <see cref="string" />.
        /// </returns>
        public override string ToString()
        {
            return $"CustomerId:{Id}";
        }
    }
}