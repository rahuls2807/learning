# Labour booking product research

Research date: 2026-10-02. This is a feature-pattern review of publicly accessible product pages, not an exhaustive audit of mobile apps or paid workflows.

## Public product patterns observed

- [Daily Labour](https://dailylabour.app/) positions itself around instant discovery of local labour, workers, contractors, service agencies, technicians, and service professionals.
- Its public site describes one-time, one-day, part-time, and full-time providers; browsing service providers; selecting and connecting with a provider; discussing work requirements and payments; issue resolution; and work orders for contractors and agencies.
- [Urban Company bookings](https://www.urbancompany.com/bookings) starts the booking flow with phone-number verification and links to its terms and privacy policy. Its public service pages position the product as a way to book local professionals.

## Changes applied to WorkerBookingSystem

- Bind a new booking to the authenticated client's own account instead of trusting a client ID submitted by the browser.
- Require a client role to create a booking and restrict booking history to clients/admins; client users can only see their own history.
- Reject past/invalid booking windows, inactive workers, overlapping non-cancelled jobs, and times outside a worker's configured availability for that weekday.
- Preserve legacy behavior for weekdays with no availability records: those days are not treated as explicitly blocked. Once a worker publishes availability records for a weekday, a booking must fit an enabled window.

## Potential next product increments

1. Surface genuinely bookable time slots in worker search and the booking form, refreshing them after another client books.
2. Add location/service-area search and travel-distance constraints.
3. Support one-off, full-day, part-time, and multi-worker work orders for contractors/agencies.
4. Add richer job progress, arrival updates, issue reporting, and customer support escalation.
5. Expand worker verification and make payment/commission terms visible before booking.

External products and their capabilities may change; verify current workflows and legal/commercial requirements before adopting them.
