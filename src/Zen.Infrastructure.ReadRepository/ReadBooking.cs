using System;
using System.Collections.Generic;
using System.Linq;
using Zen.Infrastructure.ReadRepository.DataAccess;
using Zen.Massage.Domain.BookingBoundedContext;
using Zen.Massage.Domain.UserBoundedContext;

namespace Zen.Infrastructure.ReadRepository
{
    public class ReadBooking : IReadBooking
    {
        private readonly DbBooking _innerBooking;

        public ReadBooking(DbBooking booking)
        {
            _innerBooking = booking;
            BookingId = new BookingId(_innerBooking.BookingId);
            CustomerId = new CustomerId(_innerBooking.CustomerId);

            TherapistBookings = _innerBooking
                .TherapistBookings
                .Select(tb => (IReadTherapistBooking)new ReadTherapistBooking(tb))
                .ToList()
                .AsReadOnly();
        }

        public BookingId BookingId { get; private set; }

        public CustomerId CustomerId { get; private set; }

        public BookingStatus Status => _innerBooking.Status;

        public DateTime ProposedTime => _innerBooking.ProposedTime;

        public TimeSpan Duration => _innerBooking.Duration;

        public ICollection<IReadTherapistBooking> TherapistBookings { get; private set; }

        public IReadBooking LimitToTherapist(TherapistId therapistId)
        {
            return new LimitedReadBooking(this, therapistId);
        }
    }
}