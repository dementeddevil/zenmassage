﻿using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNet.Mvc;
using Swashbuckle.SwaggerGen.Annotations;
using Zen.Massage.Application;
using Zen.Massage.Domain.BookingBoundedContext;
using Zen.Massage.Domain.UserBoundedContext;

namespace Zen.Massage.Site.Controllers.V1
{
    /// <summary>
    /// Booking endpoint
    /// </summary>
    /// <remarks>
    /// This endpoint exposes information regarding bookings.
    /// Ultimately access to the information here will be limited by user id
    /// as supplied by an OAuth2 claim from our identity server but for now
    /// it is wide open!
    /// </remarks>
    [Route("api/v1/bookings")]
    public class BookingApiControllerV1 : Controller
    {
        private readonly IMapper _mapper;
        private readonly IBookingReadRepository _bookingReadRepository;
        private readonly IBookingCommandService _bookingCommandService;

        public BookingApiControllerV1(
            MapperConfiguration mapperConfiguration,
            IBookingReadRepository bookingReadRepository,
            IBookingCommandService bookingCommandService)
        {
            _mapper = mapperConfiguration.CreateMapper();
            _bookingReadRepository = bookingReadRepository;
            _bookingCommandService = bookingCommandService;
        }

        /// <summary>
        /// Gets bookings associated with current logged on user
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("/")]
        [SwaggerOperation("GetBookings")]
        [SwaggerResponse(HttpStatusCode.OK, "Bookings retrieved")]
        [SwaggerResponse(HttpStatusCode.BadRequest, "Failed to retrieve bookings")]
        public async Task<IActionResult> GetBookings(CancellationToken cancellationToken)
        {
            try
            {
                // Retrieve user id claim from current user
                var userId = User.GetUserIdClaim();
                if (userId == Guid.Empty)
                {
                    // User does not have necessary claim
                    return HttpBadRequest();
                }

                // Consider an API that allows definition of cutoff
                // Also move meat of this method into service
                var cutoffDate = DateTime.UtcNow;

                // Fetch bookings as a customer
                var bookings = await _bookingReadRepository
                    .GetFutureBookingsForCustomer(new CustomerId(userId), cutoffDate, cancellationToken)
                    .ConfigureAwait(true);

                // If user is therapist then fetch bookings as therapist too
                if (User.HasTherapistClaim())
                {
                    var therapistBookings = await _bookingReadRepository
                        .GetFutureBookingsForTherapist(new TherapistId(userId), cutoffDate, cancellationToken)
                        .ConfigureAwait(true);

                    // Amalgamate into single collection
                    // TODO: Consider sort order...
                    bookings = bookings.Concat(therapistBookings);
                }

                var mappedBookings = bookings
                    .Select(b => _mapper.Map<BookingItemDto>(b));
                return Ok(mappedBookings);
            }
            catch (Exception exception)
            {
                return HttpBadRequest($"Failed to get bookings for user: {exception.Message}");
            }
        }

        /// <summary>
        /// Gets a single booking using the booking id.
        /// </summary>
        /// <param name="bookingId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("{bookingId:guid}")]
        [SwaggerOperation("GetBooking")]
        [SwaggerResponse(HttpStatusCode.OK, "Booking retrieved")]
        [SwaggerResponse(HttpStatusCode.BadRequest, "Failed to retrieve booking")]
        public async Task<IActionResult> GetBooking(Guid bookingId, CancellationToken cancellationToken)
        {
            try
            {
                // Retrieve user id claim from current user
                var userId = User.GetUserIdClaim();
                if (userId == Guid.Empty)
                {
                    // User does not have necessary claim
                    return HttpBadRequest();
                }

                // Get the associated booking
                var booking = await _bookingReadRepository
                    .GetBooking(new BookingId(bookingId), true, cancellationToken)
                    .ConfigureAwait(true);
                if (booking == null)
                {
                    // Booking not found
                    return HttpNotFound();
                }

                // Associated user must be customer or therapist
                if (booking.CustomerId.Id != userId)
                {
                    // Limit information returned to only therapist data
                    booking = booking.LimitToTherapist(new TherapistId(userId));
                    if (!booking.TherapistBookings.Any())
                    {
                        // Booking is not known to the current caller as customer or therapist
                        return HttpNotFound();
                    }
                }

                // Map whatever we have
                var mappedBooking = _mapper.Map<BookingItemDto>(booking);
                return Ok(mappedBooking);
            }
            catch (Exception exception)
            {
                return HttpBadRequest($"Failed to retrieve booking: {exception.Message}");
            }
        }

        /// <summary>
        /// Creates a new booking
        /// </summary>
        /// <param name="booking">The booking information</param>
        /// <returns>
        /// HTTP Created with the booking id.
        /// </returns>
        /// <remarks>
        /// The url that represents the booking object via this API is returned
        /// in the location header of the response.
        /// </remarks>
        [HttpPost]
        [Route("/")]
        [SwaggerOperation("CreateBooking")]
        [SwaggerResponse(HttpStatusCode.OK, "Booking created")]
        [SwaggerResponse(HttpStatusCode.BadRequest, "Failed to create booking")]
        public IActionResult CreateBooking(
            [FromBody]CreateBookingDto booking)
        {
            try
            {
                // Retrieve user id claim from current user
                var userId = User.GetUserIdClaim();
                if (userId == Guid.Empty)
                {
                    // User does not have necessary claim
                    return HttpBadRequest();
                }

                var bookingId = _bookingCommandService.Create(new CustomerId(userId), booking.ProposedTime, booking.Duration);

                // TODO: Fabricate URL with correct domain
                return Created(new Uri($"http://localhost:1282/api/v1/bookings/{bookingId.Id:D}/"), bookingId.Id);
            }
            catch (Exception exception)
            {
                return HttpBadRequest($"Failed to create booking: {exception.Message}");
            }
        }

        /// <summary>
        /// Opens a provisional booking for tender by therapists
        /// </summary>
        /// <param name="bookingId"></param>
        /// <returns></returns>
        [HttpPatch]
        [Route("{bookingId:guid}/tender")]
        [SwaggerOperation("TenderBooking")]
        [SwaggerResponse(HttpStatusCode.OK, "Booking tendered")]
        [SwaggerResponse(HttpStatusCode.BadRequest, "Failed to tender booking")]
        public IActionResult TenderBooking(Guid bookingId)
        {
            try
            {
                // Retrieve user id claim from current user
                var userId = User.GetUserIdClaim();
                if (userId == Guid.Empty)
                {
                    // User does not have necessary claim
                    return HttpBadRequest();
                }

                _bookingCommandService.Tender(new BookingId(bookingId), new CustomerId(userId));
                return Ok();
            }
            catch (Exception exception)
            {
                return HttpBadRequest($"Failed to tender booking: {exception.Message}");
            }
        }

        /// <summary>
        /// Bids on a tendered booking
        /// </summary>
        /// <param name="bookingId">Booking identifier</param>
        /// <param name="placeBid">Bid placement information</param>
        /// <returns></returns>
        [HttpPatch]
        [Route("{bookingId:guid}/bid")]
        [SwaggerOperation("PlaceBid")]
        [SwaggerResponse(HttpStatusCode.OK, "Bid placed")]
        [SwaggerResponse(HttpStatusCode.BadRequest, "Failed to place bid")]
        public IActionResult PlaceBid(
            Guid bookingId,
            [FromBody] PlaceBookingBidDto placeBid)
        {
            try
            {
                // Retrieve user id claim from current user
                var userId = User.GetUserIdClaim();
                if (userId == Guid.Empty || !User.HasTherapistClaim())
                {
                    // User does not have necessary claims
                    return HttpBadRequest();
                }

                _bookingCommandService.PlaceBid(
                    new BookingId(bookingId),
                    new TherapistId(userId),
                    placeBid.ProposedTime);
                return Ok();
            }
            catch (Exception exception)
            {
                return HttpBadRequest($"Failed to place bid on booking: {exception.Message}");
            }
        }

        /// <summary>
        /// Accepts a bid offered by a therapist
        /// </summary>
        /// <param name="bookingId">Booking identifier</param>
        /// <param name="therapistId">Therapist identifier</param>
        /// <returns></returns>
        [HttpPatch]
        [Route("{bookingId:guid}/accept/{therapistId:guid}")]
        [SwaggerOperation("AcceptBid")]
        [SwaggerResponse(HttpStatusCode.OK, "Booking accepted")]
        [SwaggerResponse(HttpStatusCode.BadRequest, "Failed to accept bid")]
        public IActionResult AcceptBid(Guid bookingId, Guid therapistId)
        {
            try
            {
                // Retrieve user id claim from current user
                var userId = User.GetUserIdClaim();
                if (userId == Guid.Empty)
                {
                    // User does not have necessary claim
                    return HttpBadRequest();
                }

                _bookingCommandService.AcceptBid(
                    new BookingId(bookingId),
                    new CustomerId(userId), 
                    new TherapistId(therapistId));
                return Ok();
            }
            catch (Exception exception)
            {
                return HttpBadRequest($"Failed to accept booking: {exception.Message}");
            }
        }

        /// <summary>
        /// Confirm an accepted bid on a booking
        /// </summary>
        /// <param name="bookingId">Booking identifier</param>
        /// <returns></returns>
        [HttpPatch]
        [Route("{bookingId:guid}/confirm")]
        [SwaggerOperation("ConfirmedBid")]
        [SwaggerResponse(HttpStatusCode.OK, "Booking confirmed")]
        [SwaggerResponse(HttpStatusCode.BadRequest, "Failed to confirm bid")]
        public IActionResult ConfirmBid(Guid bookingId)
        {
            try
            {
                // Retrieve user id claim from current user
                var userId = User.GetUserIdClaim();
                if (userId == Guid.Empty || !User.HasTherapistClaim())
                {
                    // User does not have necessary claims
                    return HttpBadRequest();
                }

                _bookingCommandService.ConfirmBid(
                    new BookingId(bookingId),
                    new TherapistId(userId));
                return Ok();
            }
            catch (Exception exception)
            {
                return HttpBadRequest($"Failed to confirm booking: {exception.Message}");
            }
        }

        /// <summary>
        /// Cancels a booking (if actioned by customer)
        /// </summary>
        /// <param name="bookingId">Booking identifier</param>
        /// <returns></returns>
        [HttpPatch]
        [Route("{bookingId:guid}/cancel/customer")]
        [SwaggerOperation("CancelBookingByCustomer")]
        [SwaggerResponse(HttpStatusCode.OK, "Booking cancelled")]
        [SwaggerResponse(HttpStatusCode.BadRequest, "Failed to cancel booking")]
        public IActionResult CancelBookingByCustomer(Guid bookingId)
        {
            try
            {
                // Retrieve user id claim from current user
                var userId = User.GetUserIdClaim();
                if (userId == Guid.Empty)
                {
                    // User does not have necessary claim
                    return HttpBadRequest();
                }

                _bookingCommandService.Cancel(
                    new BookingId(bookingId),
                    new CustomerId(userId),
                    string.Empty);
                return Ok();
            }
            catch (Exception exception)
            {
                return HttpBadRequest($"Failed to cancel booking: {exception.Message}");
            }
        }

        /// <summary>
        /// Cancels a booking (if actioned by therapist)
        /// </summary>
        /// <param name="bookingId">Booking identifier</param>
        /// <returns></returns>
        [HttpPatch]
        [Route("{bookingId:guid}/cancel/therapist")]
        [SwaggerOperation("CancelBookingByTherapist")]
        [SwaggerResponse(HttpStatusCode.OK, "Booking cancelled")]
        [SwaggerResponse(HttpStatusCode.BadRequest, "Failed to cancel booking")]
        public IActionResult CancelBookingByTherapist(Guid bookingId)
        {
            try
            {
                // Retrieve user id claim from current user
                var userId = User.GetUserIdClaim();
                if (userId == Guid.Empty || !User.HasTherapistClaim())
                {
                    // User does not have necessary claims
                    return HttpBadRequest();
                }

                _bookingCommandService.Cancel(
                    new BookingId(bookingId),
                    new TherapistId(userId),
                    string.Empty);
                return Ok();
            }
            catch (Exception exception)
            {
                return HttpBadRequest($"Failed to cancel booking: {exception.Message}");
            }
        }
    }
}
