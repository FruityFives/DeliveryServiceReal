using System.Collections.Generic;
using System.Linq;

namespace WorkerService.Models
{
    public class BookingRepository
    {
        private readonly List<ShippingrequestDTO> _bookings = new();

        public void Put(ShippingrequestDTO booking)
        {
            _bookings.Add(booking);
        }

        public List<ShippingrequestDTO> GetAll()
        {
            return _bookings.OrderBy(b => b.Date).ToList();
        }
    }
}