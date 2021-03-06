﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Zen.Infrastructure.ReadRepository.DataAccess;
using Zen.Massage.Domain.BookingBoundedContext;
using Zen.Massage.Domain.GeneralBoundedContext;
using Zen.Massage.Domain.UserBoundedContext;

namespace Zen.Infrastructure.ReadRepository
{
    public class BookingReadRepository : IBookingReadRepository
    {
        private readonly string _connectionString;

        public BookingReadRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<IReadBooking> GetBookingAsync(
            TenantId tenantId,
            BookingId bookingId,
            bool includeTherapists,
            CancellationToken cancellationToken)
        {
            using (var context = CreateBookingEntityContext())
            {
                // Determine depth of information we will be returning
                IQueryable<DbBooking> query;
                if (includeTherapists)
                {
                    query = context.Bookings.Include("TherapistBookings");
                }
                else
                {
                    query = context.Bookings;
                }

                // Issue async query
                var result = await query
                    .Where(b => b.TenantId == tenantId.Id && b.BookingId == bookingId.Id)
                    .FirstOrDefaultAsync(cancellationToken)
                    .ConfigureAwait(false);
                if (result == null)
                {
                    return null;
                }

                // Returned booking wrapped in helper
                return new ReadBooking(result);
            }
        }

        public async Task<IEnumerable<IReadBooking>> GetFutureOpenBookingsAsync(
            TenantId tenantId,
            DateTime cutoffDate,
            BookingStatus status,
            CancellationToken cancellationToken)
        {
            using (var context = CreateBookingEntityContext())
            {
                var now = DateTime.UtcNow;
                var query = await context.Bookings
                    .Include(b => b.TherapistBookings)
                    .Where(b =>
                        b.TenantId == tenantId.Id &&
                        b.ProposedTime > now && b.ProposedTime < cutoffDate &&
                        b.Status != BookingStatus.Provisional &&
                        b.Status != BookingStatus.CancelledByClient)
                    .OrderBy(b => b.ProposedTime)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);
                return query
                    .Select(b => new ReadBooking(b));
            }
        }

        public async Task<IEnumerable<IReadBooking>> GetFutureBookingsForCustomerAsync(
            TenantId tenantId,
            CustomerId customerId,
            DateTime cutoffDate,
            CancellationToken cancellationToken)
        {
            using (var context = CreateBookingEntityContext())
            {
                var now = DateTime.UtcNow;
                var query = await context.Bookings
                    .Include(b => b.TherapistBookings)
                    .Where(b =>
                        b.TenantId == tenantId.Id &&
                        b.ProposedTime > now && b.ProposedTime < cutoffDate &&
                        b.CustomerId == customerId.Id &&
                        b.Status != BookingStatus.Provisional &&
                        b.Status != BookingStatus.CancelledByClient)
                    .OrderBy(b => b.ProposedTime)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);
                return query
                    .Select(b => new ReadBooking(b));
            }
        }

        public async Task<IEnumerable<IReadBooking>> GetFutureBookingsForTherapistAsync(
            TenantId tenantId,
            TherapistId therapistId,
            DateTime cutoffDate,
            CancellationToken cancellationToken)
        {
            using (var context = CreateBookingEntityContext())
            {
                var now = DateTime.UtcNow;
                var query = await context.TherapistBookings
                    .Include(tb => tb.Booking)
                    .Where(tb =>
                        tb.Booking.ProposedTime > now && tb.Booking.ProposedTime < cutoffDate &&
                        tb.TherapistId == therapistId.Id &&
                        tb.Booking.TenantId == tenantId.Id &&
                        tb.Booking.Status != BookingStatus.Provisional &&
                        tb.Booking.Status != BookingStatus.CancelledByClient)
                    .Select(tb => tb.Booking)
                    .OrderBy(b => b.ProposedTime)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);
                return query
                    .Select(b => new ReadBooking(b));
            }
        }

        public async Task AddBookingAsync(
            TenantId tenantId,
            BookingId bookingId,
            CustomerId clientId,
            DateTime proposedTime,
            TimeSpan duration,
            CancellationToken cancellationToken)
        {
            using (var context = CreateBookingEntityContext())
            {
                var booking =
                    new DbBooking
                    {
                        BookingId = bookingId.Id,
                        TenantId = tenantId.Id,
                        CustomerId = clientId.Id,
                        Status = BookingStatus.Provisional,
                        ProposedTime = proposedTime,
                        Duration = duration
                    };
                context.Bookings.Add(booking);
                await context
                    .SaveChangesAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        public async Task UpdateBookingAsync(
            TenantId tenantId,
            BookingId bookingId,
            BookingStatus? status,
            DateTime? proposedTime,
            TimeSpan? duration,
            CancellationToken cancellationToken)
        {
            using (var context = CreateBookingEntityContext())
            {
                var booking = await context.Bookings
                    .FirstOrDefaultAsync(b => b.BookingId == bookingId.Id, cancellationToken)
                    .ConfigureAwait(false);
                if (status.HasValue)
                {
                    booking.Status = status.Value;
                }
                if (proposedTime.HasValue)
                {
                    booking.ProposedTime = proposedTime.Value;
                }
                if (duration.HasValue)
                {
                    booking.Duration = duration.Value;
                }
            }
        }

        public async Task AddTherapistBookingAsync(
            TenantId tenantId,
            BookingId bookingId,
            TherapistId therapistId,
            BookingStatus status,
            DateTime proposedTime,
            CancellationToken cancellationToken)
        {
            using (var context = CreateBookingEntityContext())
            {
                var booking = await context.Bookings
                    .FirstOrDefaultAsync(b => b.BookingId == bookingId.Id, cancellationToken)
                    .ConfigureAwait(false);
                var therapist =
                    new DbTherapistBooking
                    {
                        TherapistBookingId = Guid.NewGuid(),
                        BookingId = bookingId.Id,
                        TherapistId = therapistId.Id,
                        ProposedTime = proposedTime,
                        Status = status,
                        Booking = booking
                    };
                context.TherapistBookings.Add(therapist);
                await context
                    .SaveChangesAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        public async Task UpdateTherapistBookingAsync(
            TenantId tenantId,
            BookingId bookingId,
            TherapistId therapistId,
            BookingStatus? status,
            DateTime? proposedTime,
            CancellationToken cancellationToken)
        {
            using (var context = CreateBookingEntityContext())
            {
                var therapist = await context.TherapistBookings
                    .FirstOrDefaultAsync(tb =>
                        tb.BookingId == bookingId.Id &&
                        tb.TherapistId == therapistId.Id,
                        cancellationToken)
                    .ConfigureAwait(false);
                if (therapist != null)
                {
                    if (status.HasValue)
                    {
                        therapist.Status = status.Value;
                    }
                    if (proposedTime.HasValue)
                    {
                        therapist.ProposedTime = proposedTime.Value;
                    }

                    await context
                        .SaveChangesAsync(cancellationToken)
                        .ConfigureAwait(false);
                }
            }
        }

        private BookingEntityContext CreateBookingEntityContext()
        {
            return new BookingEntityContext(_connectionString);
        }
    }
}

